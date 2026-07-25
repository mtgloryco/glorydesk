using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class PaymentService
    {
        private readonly DatabaseService _databaseService;
        private readonly AuditService? _auditService;
        private readonly CurrencyService? _currencyService;
        private readonly SettingsService? _settingsService;

        public PaymentService(
            DatabaseService databaseService,
            AuditService? auditService = null,
            CurrencyService? currencyService = null,
            SettingsService? settingsService = null)
        {
            _databaseService = databaseService;
            _auditService = auditService;
            _currencyService = currencyService;
            _settingsService = settingsService;
        }

        // Bank CRUD
        public async Task<List<Bank>> GetAllBanksAsync()
        {
            return await _databaseService.Connection.Table<Bank>()
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task AddBankAsync(Bank bank)
        {
            await _databaseService.Connection.InsertAsync(bank);
        }

        public async Task UpdateBankAsync(Bank bank)
        {
            await _databaseService.Connection.UpdateAsync(bank);
        }

        public async Task DeleteBankAsync(int bankId)
        {
            var bank = await _databaseService.Connection.FindAsync<Bank>(bankId);
            if (bank != null)
            {
                await _databaseService.Connection.DeleteAsync(bank);
            }
        }

        // Bank Account CRUD
        public async Task<List<BankAccount>> GetAllBankAccountsAsync()
        {
            return await _databaseService.Connection.Table<BankAccount>()
                .ToListAsync();
        }

        public async Task AddBankAccountAsync(BankAccount account)
        {
            await _databaseService.Connection.InsertAsync(account);
        }

        public async Task UpdateBankAccountAsync(BankAccount account)
        {
            await _databaseService.Connection.UpdateAsync(account);
        }

        public async Task DeleteBankAccountAsync(int accountId)
        {
            var account = await _databaseService.Connection.FindAsync<BankAccount>(accountId);
            if (account != null)
            {
                await _databaseService.Connection.DeleteAsync(account);
            }
        }

        // --- Invoice payments (partial / full) ---

        public async Task<List<InvoicePayment>> GetPaymentsForDocumentAsync(string documentType, int documentId)
        {
            return await _databaseService.Connection.Table<InvoicePayment>()
                .Where(p => !p.IsDeleted && p.DocumentType == documentType && p.DocumentId == documentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<decimal> GetDocumentTotalAsync(string documentType, int documentId)
        {
            if (documentType == "SalesOrder")
            {
                var so = await _databaseService.Connection.FindAsync<SalesOrder>(documentId);
                return so?.TotalAmount ?? 0;
            }

            if (documentType == "PurchaseOrder")
            {
                var po = await _databaseService.Connection.FindAsync<PurchaseOrder>(documentId);
                return po?.TotalAmount ?? 0;
            }

            return 0;
        }

        public async Task<decimal> GetDocumentCreditsAsync(string documentType, int documentId)
        {
            if (documentType == "SalesOrder")
            {
                var notes = await _databaseService.Connection.Table<CreditNote>()
                    .Where(c => !c.IsDeleted && c.Status == "Posted")
                    .ToListAsync();
                return notes
                    .Where(c => c.AppliedToSalesOrderId == documentId)
                    .Sum(c => c.AppliedAmount > 0 ? c.AppliedAmount : c.Amount);
            }

            if (documentType == "PurchaseOrder")
            {
                var notes = await _databaseService.Connection.Table<DebitNote>()
                    .Where(d => !d.IsDeleted && d.Status == "Posted")
                    .ToListAsync();
                return notes
                    .Where(d => d.AppliedToPurchaseOrderId == documentId)
                    .Sum(d => d.AppliedAmount > 0 ? d.AppliedAmount : d.Amount);
            }

            return 0;
        }

        public async Task<decimal> GetAmountPaidAsync(string documentType, int documentId)
        {
            var payments = await GetPaymentsForDocumentAsync(documentType, documentId);
            return payments.Sum(p => p.Amount);
        }

        public async Task<decimal> GetOpenBalanceAsync(string documentType, int documentId)
        {
            var total = await GetDocumentTotalAsync(documentType, documentId);
            var credits = await GetDocumentCreditsAsync(documentType, documentId);
            var paid = await GetAmountPaidAsync(documentType, documentId);
            return Math.Max(0, total - credits - paid);
        }

        public async Task<InvoicePayment> RecordInvoicePaymentAsync(
            string documentType,
            int documentId,
            decimal amount,
            string paymentMethod,
            string username,
            int? bankAccountId = null,
            string reference = "",
            DateTime? paymentDate = null)
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException("Payment amount must be greater than zero.");
            }

            var openBalance = await GetOpenBalanceAsync(documentType, documentId);
            if (amount > openBalance + 0.01m)
            {
                throw new InvalidOperationException($"Payment amount ({amount:N2}) exceeds open balance ({openBalance:N2}).");
            }

            string currency = "RWF";
            string docNumber = string.Empty;
            if (documentType == "SalesOrder")
            {
                var so = await _databaseService.Connection.FindAsync<SalesOrder>(documentId)
                    ?? throw new InvalidOperationException("Sales order not found.");
                if (so.BillingStatus != "Invoiced")
                {
                    throw new InvalidOperationException("Invoice must be posted before recording payment.");
                }

                currency = so.Currency;
                docNumber = so.SONumber;
            }
            else if (documentType == "PurchaseOrder")
            {
                var po = await _databaseService.Connection.FindAsync<PurchaseOrder>(documentId)
                    ?? throw new InvalidOperationException("Purchase order not found.");
                if (po.BillingStatus != "Billed")
                {
                    throw new InvalidOperationException("Vendor bill must be posted before recording payment.");
                }

                currency = po.Currency;
                docNumber = po.PONumber;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported document type: {documentType}");
            }

            var payment = new InvoicePayment
            {
                PaymentNumber = await GeneratePaymentNumberAsync(),
                DocumentType = documentType,
                DocumentId = documentId,
                Amount = amount,
                Currency = currency,
                PaymentDate = paymentDate ?? DateTime.Now,
                PaymentMethod = paymentMethod,
                BankAccountId = bankAccountId,
                Reference = reference,
                CreatedByUsername = username
            };

            await _databaseService.Connection.RunInTransactionAsync(conn =>
            {
                conn.Insert(payment);
                PostPaymentJournalEntry(conn, payment, docNumber);
            });

            await TryPostFxDifferenceAsync(payment, docNumber, username);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "PaymentRecorded", documentType, documentId, payment);
            }

            return payment;
        }

        private async Task TryPostFxDifferenceAsync(InvoicePayment payment, string docNumber, string username)
        {
            if (_currencyService == null || _settingsService == null) return;

            var baseCurrency = _settingsService.CurrentSettings.CurrencySymbol ?? "RWF";
            if (string.Equals(payment.Currency, baseCurrency, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                // Invoice/document currency amount converted at payment-date rate vs order-date rate approximation:
                // use payment amount in doc currency → base at payment date, compare to same amount at a stored rate of 1:1 baseline using latest rate before payment.
                var rateToday = await _currencyService.GetRateAsync(payment.Currency, baseCurrency, payment.PaymentDate);
                // Without historical invoice rate stored, treat "document rate" as rate on document date when available.
                DateTime docDate = payment.PaymentDate;
                if (payment.DocumentType == "SalesOrder")
                {
                    var so = await _databaseService.Connection.FindAsync<SalesOrder>(payment.DocumentId);
                    if (so != null) docDate = so.OrderDate;
                }
                else if (payment.DocumentType == "PurchaseOrder")
                {
                    var po = await _databaseService.Connection.FindAsync<PurchaseOrder>(payment.DocumentId);
                    if (po != null) docDate = po.OrderDate;
                }

                var rateAtDoc = await _currencyService.GetRateAsync(payment.Currency, baseCurrency, docDate);
                var baseAtDoc = Math.Round(payment.Amount * rateAtDoc, 4);
                var baseAtPay = Math.Round(payment.Amount * rateToday, 4);
                var diff = baseAtPay - baseAtDoc;
                if (Math.Abs(diff) < 0.01m) return;

                await _databaseService.Connection.RunInTransactionAsync(conn =>
                {
                    PostFxJournal(conn, payment, docNumber, diff, baseCurrency);
                });

                if (_auditService != null)
                {
                    await _auditService.LogActionAsync(username, "FxGainLossPosted", payment.DocumentType, payment.DocumentId,
                        new { payment.PaymentNumber, diff, baseCurrency });
                }
            }
            catch
            {
                // Missing exchange rates — skip FX posting without failing the payment
            }
        }

        private static void PostFxJournal(SQLite.SQLiteConnection conn, InvoicePayment payment, string docNumber, decimal diff, string baseCurrency)
        {
            var journal = conn.Table<Journal>().FirstOrDefault(j => j.SequencePrefix == "EXCH")
                ?? conn.Table<Journal>().FirstOrDefault(j => j.Type == "Miscellaneous");
            if (journal == null) return;

            var gain = conn.Table<Account>().FirstOrDefault(a => a.Code == "491000");
            var loss = conn.Table<Account>().FirstOrDefault(a => a.Code == "591000");
            var ar = conn.Table<Account>().FirstOrDefault(a => a.Code == "111000");
            var ap = conn.Table<Account>().FirstOrDefault(a => a.Code == "201000");
            if (gain == null || loss == null) return;

            var entryCount = conn.Table<JournalEntry>().Count(e => e.JournalId == journal.Id);
            var entry = new JournalEntry
            {
                EntryNumber = $"{journal.SequencePrefix}/{DateTime.Now.Year}/{(entryCount + 1):D5}",
                JournalId = journal.Id,
                Date = payment.PaymentDate,
                Reference = $"FX on payment {payment.PaymentNumber} - {docNumber}",
                State = "Posted"
            };
            conn.Insert(entry);

            var abs = Math.Abs(diff);
            var isGain = diff > 0;
            var counterpartId = payment.DocumentType == "SalesOrder" ? (ar?.Id ?? 3) : (ap?.Id ?? 7);
            var fxAccountId = isGain ? gain.Id : loss.Id;

            if (payment.DocumentType == "SalesOrder")
            {
                // Gain: Dr AR, Cr FX Gain  | Loss: Dr FX Loss, Cr AR
                if (isGain)
                {
                    conn.Insert(new JournalLine { JournalEntryId = entry.Id, AccountId = counterpartId, Label = "FX gain", Debit = abs, Credit = 0 });
                    conn.Insert(new JournalLine { JournalEntryId = entry.Id, AccountId = fxAccountId, Label = "FX gain", Debit = 0, Credit = abs });
                }
                else
                {
                    conn.Insert(new JournalLine { JournalEntryId = entry.Id, AccountId = fxAccountId, Label = "FX loss", Debit = abs, Credit = 0 });
                    conn.Insert(new JournalLine { JournalEntryId = entry.Id, AccountId = counterpartId, Label = "FX loss", Debit = 0, Credit = abs });
                }
            }
            else
            {
                if (isGain)
                {
                    conn.Insert(new JournalLine { JournalEntryId = entry.Id, AccountId = fxAccountId, Label = "FX gain", Debit = 0, Credit = abs });
                    conn.Insert(new JournalLine { JournalEntryId = entry.Id, AccountId = counterpartId, Label = "FX gain", Debit = abs, Credit = 0 });
                }
                else
                {
                    conn.Insert(new JournalLine { JournalEntryId = entry.Id, AccountId = counterpartId, Label = "FX loss", Debit = 0, Credit = abs });
                    conn.Insert(new JournalLine { JournalEntryId = entry.Id, AccountId = fxAccountId, Label = "FX loss", Debit = abs, Credit = 0 });
                }
            }
        }

        public async Task<int> RevalueOpenForeignBalancesAsync(string username)
        {
            if (_currencyService == null || _settingsService == null)
            {
                throw new InvalidOperationException("Currency service is not configured.");
            }

            var baseCurrency = _settingsService.CurrentSettings.CurrencySymbol ?? "RWF";
            var asOf = DateTime.Today;
            var posted = 0;

            var openSales = await _databaseService.Connection.Table<SalesOrder>()
                .Where(s => !s.IsDeleted && s.BillingStatus == "Invoiced")
                .ToListAsync();

            foreach (var so in openSales)
            {
                if (string.Equals(so.Currency, baseCurrency, StringComparison.OrdinalIgnoreCase)) continue;
                var open = await GetOpenBalanceAsync("SalesOrder", so.Id);
                if (open <= 0) continue;

                try
                {
                    var rateDoc = await _currencyService.GetRateAsync(so.Currency, baseCurrency, so.OrderDate);
                    var rateNow = await _currencyService.GetRateAsync(so.Currency, baseCurrency, asOf);
                    var diff = Math.Round(open * (rateNow - rateDoc), 4);
                    if (Math.Abs(diff) < 0.01m) continue;

                    var paymentStub = new InvoicePayment
                    {
                        PaymentNumber = $"REVAL-{so.SONumber}",
                        DocumentType = "SalesOrder",
                        DocumentId = so.Id,
                        Amount = open,
                        Currency = so.Currency,
                        PaymentDate = asOf
                    };
                    await _databaseService.Connection.RunInTransactionAsync(conn =>
                    {
                        PostFxJournal(conn, paymentStub, so.SONumber, diff, baseCurrency);
                    });
                    posted++;
                }
                catch
                {
                    // skip docs without rates
                }
            }

            if (_auditService != null && posted > 0)
            {
                await _auditService.LogActionAsync(username, "FxRevaluation", "ExchangeRate", 0, new { posted, asOf });
            }

            return posted;
        }

        private void PostPaymentJournalEntry(SQLite.SQLiteConnection conn, InvoicePayment payment, string docNumber)
        {
            var journalType = payment.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase) ? "Cash" : "Bank";
            var journal = conn.Table<Journal>().FirstOrDefault(j => j.Type == journalType)
                ?? conn.Table<Journal>().FirstOrDefault(j => j.Type == "Bank");

            if (journal == null)
            {
                return;
            }

            var entryCount = conn.Table<JournalEntry>().Count(e => e.JournalId == journal.Id);
            var entry = new JournalEntry
            {
                EntryNumber = $"{journal.SequencePrefix}/{DateTime.Now.Year}/{(entryCount + 1):D5}",
                JournalId = journal.Id,
                Date = payment.PaymentDate,
                Reference = $"Payment {payment.PaymentNumber} - {docNumber}",
                State = "Posted"
            };
            conn.Insert(entry);

            var bankAccount = conn.Table<Account>().FirstOrDefault(a => a.Code == "102000");
            var cashAccount = conn.Table<Account>().FirstOrDefault(a => a.Code == "101000");
            var arAccount = conn.Table<Account>().FirstOrDefault(a => a.Code == "111000");
            var apAccount = conn.Table<Account>().FirstOrDefault(a => a.Code == "201000");

            int liquidityAccountId = payment.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)
                ? (cashAccount?.Id ?? journal.DefaultAccountId ?? 1)
                : (bankAccount?.Id ?? journal.DefaultAccountId ?? 2);

            if (payment.DocumentType == "SalesOrder")
            {
                int receivableId = arAccount?.Id ?? 3;
                conn.Insert(new JournalLine
                {
                    JournalEntryId = entry.Id,
                    AccountId = liquidityAccountId,
                    Label = $"Customer payment - {docNumber}",
                    Debit = payment.Amount,
                    Credit = 0
                });
                conn.Insert(new JournalLine
                {
                    JournalEntryId = entry.Id,
                    AccountId = receivableId,
                    Label = $"Customer payment - {docNumber}",
                    Debit = 0,
                    Credit = payment.Amount
                });
            }
            else
            {
                int payableId = apAccount?.Id ?? 7;
                conn.Insert(new JournalLine
                {
                    JournalEntryId = entry.Id,
                    AccountId = payableId,
                    Label = $"Vendor payment - {docNumber}",
                    Debit = payment.Amount,
                    Credit = 0
                });
                conn.Insert(new JournalLine
                {
                    JournalEntryId = entry.Id,
                    AccountId = liquidityAccountId,
                    Label = $"Vendor payment - {docNumber}",
                    Debit = 0,
                    Credit = payment.Amount
                });
            }
        }

        private async Task<string> GeneratePaymentNumberAsync()
        {
            var year = DateTime.Now.Year;
            var count = await _databaseService.Connection.Table<InvoicePayment>().CountAsync();
            return $"PAY-{year}-{(count + 1):D5}";
        }

        // --- Bank reconciliation ---

        public async Task<BankStatement> ImportBankStatementAsync(
            int bankAccountId,
            DateTime statementDate,
            decimal openingBalance,
            decimal closingBalance,
            IEnumerable<(DateTime date, string description, decimal amount, string reference)> lines,
            string reference = "")
        {
            var statement = new BankStatement
            {
                BankAccountId = bankAccountId,
                StatementDate = statementDate,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                Reference = reference,
                ImportedAt = DateTime.Now
            };

            await _databaseService.Connection.RunInTransactionAsync(conn =>
            {
                conn.Insert(statement);
                foreach (var line in lines)
                {
                    conn.Insert(new BankStatementLine
                    {
                        BankStatementId = statement.Id,
                        TransactionDate = line.date,
                        Description = line.description,
                        Amount = line.amount,
                        Reference = line.reference
                    });
                }
            });

            return statement;
        }

        public async Task MatchPaymentToStatementLineAsync(int paymentId, int statementLineId, string username)
        {
            var payment = await _databaseService.Connection.FindAsync<InvoicePayment>(paymentId)
                ?? throw new InvalidOperationException("Payment not found.");
            var line = await _databaseService.Connection.FindAsync<BankStatementLine>(statementLineId)
                ?? throw new InvalidOperationException("Bank statement line not found.");

            if (line.IsReconciled)
            {
                throw new InvalidOperationException("Statement line is already reconciled.");
            }

            if (Math.Abs(line.Amount) != payment.Amount)
            {
                throw new InvalidOperationException("Payment amount does not match statement line amount.");
            }

            payment.BankStatementLineId = statementLineId;
            line.IsReconciled = true;
            line.MatchedPaymentId = paymentId;

            await _databaseService.Connection.UpdateAsync(payment);
            await _databaseService.Connection.UpdateAsync(line);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "BankReconciled", "InvoicePayment", paymentId,
                    new { paymentId, statementLineId });
            }
        }

        public async Task<List<ReconciliationCandidate>> GetUnreconciledPaymentsAsync(int? bankAccountId = null)
        {
            var payments = await _databaseService.Connection.Table<InvoicePayment>()
                .Where(p => !p.IsDeleted && p.BankStatementLineId == null)
                .ToListAsync();

            if (bankAccountId.HasValue)
            {
                payments = payments.Where(p => p.BankAccountId == bankAccountId || p.BankAccountId == null).ToList();
            }

            return payments.Select(p => new ReconciliationCandidate
            {
                Payment = p,
                Label = $"{p.PaymentNumber} ({p.DocumentType})",
                Amount = p.Amount,
                Date = p.PaymentDate,
                IsMatched = false
            }).OrderByDescending(c => c.Date).ToList();
        }

        public async Task<List<ReconciliationCandidate>> GetUnreconciledStatementLinesAsync(int bankAccountId)
        {
            var statements = await _databaseService.Connection.Table<BankStatement>()
                .Where(s => s.BankAccountId == bankAccountId)
                .ToListAsync();
            var statementIds = statements.Select(s => s.Id).ToHashSet();

            var lines = await _databaseService.Connection.Table<BankStatementLine>()
                .Where(l => !l.IsReconciled)
                .ToListAsync();

            lines = lines.Where(l => statementIds.Contains(l.BankStatementId)).ToList();

            return lines.Select(l => new ReconciliationCandidate
            {
                StatementLine = l,
                Label = l.Description,
                Amount = l.Amount,
                Date = l.TransactionDate,
                IsMatched = false
            }).OrderByDescending(c => c.Date).ToList();
        }

        public async Task<List<BankStatement>> GetBankStatementsAsync(int bankAccountId)
        {
            return await _databaseService.Connection.Table<BankStatement>()
                .Where(s => s.BankAccountId == bankAccountId)
                .OrderByDescending(s => s.StatementDate)
                .ToListAsync();
        }

        public static List<(DateTime date, string description, decimal amount, string reference)> ParseBankStatementCsv(string csvContent)
        {
            var results = new List<(DateTime date, string description, decimal amount, string reference)>();
            if (string.IsNullOrWhiteSpace(csvContent)) return results;

            var lines = csvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("Date", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("Transaction", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // header
                }

                var parts = SplitCsvLine(line);
                if (parts.Count < 3) continue;

                if (!DateTime.TryParse(parts[0], out var date)) continue;
                var description = parts[1].Trim();
                if (!decimal.TryParse(parts[2].Replace(",", ""), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var amount)
                    && !decimal.TryParse(parts[2], out amount))
                {
                    continue;
                }

                var reference = parts.Count > 3 ? parts[3].Trim() : string.Empty;
                results.Add((date, description, amount, reference));
            }

            return results;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;
            foreach (var ch in line)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            result.Add(current.ToString());
            return result;
        }

        public async Task<BankStatement> ImportBankStatementCsvAsync(
            int bankAccountId,
            string csvContent,
            DateTime? statementDate = null,
            decimal openingBalance = 0,
            decimal closingBalance = 0,
            string reference = "CSV Import")
        {
            var lines = ParseBankStatementCsv(csvContent);
            if (lines.Count == 0)
            {
                throw new InvalidOperationException("No valid CSV rows found. Expected: Date,Description,Amount,Reference");
            }

            return await ImportBankStatementAsync(
                bankAccountId,
                statementDate ?? lines.Max(l => l.date).Date,
                openingBalance,
                closingBalance,
                lines,
                reference);
        }

        public async Task<List<BankMatchSuggestion>> SuggestMatchesAsync(int bankAccountId, int dateWindowDays = 5)
        {
            var payments = await GetUnreconciledPaymentsAsync(bankAccountId);
            var lines = await GetUnreconciledStatementLinesAsync(bankAccountId);
            var suggestions = new List<BankMatchSuggestion>();

            foreach (var line in lines.Where(l => l.StatementLine != null))
            {
                var candidates = payments
                    .Where(p => p.Payment != null)
                    .Select(p =>
                    {
                        var amountScore = Math.Abs(Math.Abs(line.Amount) - p.Amount) < 0.01m ? 50 : 0;
                        var dayDiff = Math.Abs((line.Date.Date - p.Date.Date).TotalDays);
                        var dateScore = dayDiff <= dateWindowDays ? (int)(30 - dayDiff * 3) : 0;
                        var refScore = 0;
                        if (!string.IsNullOrWhiteSpace(line.StatementLine!.Reference)
                            && !string.IsNullOrWhiteSpace(p.Payment!.Reference)
                            && line.StatementLine.Reference.Contains(p.Payment.Reference, StringComparison.OrdinalIgnoreCase))
                        {
                            refScore = 20;
                        }
                        else if (!string.IsNullOrWhiteSpace(line.Label)
                                 && line.Label.Contains(p.Payment!.PaymentNumber, StringComparison.OrdinalIgnoreCase))
                        {
                            refScore = 15;
                        }

                        return new { Payment = p, Score = amountScore + dateScore + refScore };
                    })
                    .Where(x => x.Score >= 50)
                    .OrderByDescending(x => x.Score)
                    .ToList();

                var best = candidates.FirstOrDefault();
                if (best != null)
                {
                    suggestions.Add(new BankMatchSuggestion
                    {
                        StatementLine = line,
                        Payment = best.Payment,
                        Score = best.Score,
                        Reason = best.Score >= 80 ? "Strong match" : "Suggested match"
                    });
                }
            }

            return suggestions.OrderByDescending(s => s.Score).ToList();
        }

        public async Task<int> AcceptSuggestedMatchesAsync(int bankAccountId, string username)
        {
            var suggestions = await SuggestMatchesAsync(bankAccountId);
            var matched = 0;
            var usedPayments = new HashSet<int>();
            var usedLines = new HashSet<int>();

            foreach (var suggestion in suggestions)
            {
                var paymentId = suggestion.Payment?.Payment?.Id ?? 0;
                var lineId = suggestion.StatementLine?.StatementLine?.Id ?? 0;
                if (paymentId <= 0 || lineId <= 0) continue;
                if (!usedPayments.Add(paymentId) || !usedLines.Add(lineId)) continue;

                try
                {
                    await MatchPaymentToStatementLineAsync(paymentId, lineId, username);
                    matched++;
                }
                catch
                {
                    // Skip conflicts
                }
            }

            return matched;
        }
    }

    public class BankMatchSuggestion
    {
        public ReconciliationCandidate? StatementLine { get; set; }
        public ReconciliationCandidate? Payment { get; set; }
        public int Score { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
