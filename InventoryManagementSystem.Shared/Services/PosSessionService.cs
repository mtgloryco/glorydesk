using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    /// <summary>
    /// Cash-register shift management for POS: open with a starting float per payment method,
    /// attach every sale rung up to the currently open session, track manual cash in/out, and
    /// close by comparing what the till should have against what was actually counted.
    /// </summary>
    public class PosSessionService
    {
        private readonly DatabaseService _databaseService;

        public PosSessionService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<PosSession?> GetOpenSessionAsync()
        {
            return await _databaseService.Connection.Table<PosSession>()
                .Where(s => !s.IsDeleted && s.Status == "Open")
                .FirstOrDefaultAsync();
        }

        public async Task<PosSession> OpenSessionAsync(Dictionary<int, decimal> openingBalances, string username)
        {
            var existing = await GetOpenSessionAsync();
            if (existing != null)
            {
                throw new InvalidOperationException($"Session {existing.SessionNumber} is already open. Close it before opening a new one.");
            }

            var count = await _databaseService.Connection.Table<PosSession>().CountAsync();
            var session = new PosSession
            {
                SessionNumber = $"SESS-{DateTime.Now:yyyyMMdd}-{(count + 1):D4}",
                Status = "Open",
                OpenedByUsername = username,
                OpenedAt = DateTime.Now
            };
            await _databaseService.Connection.InsertAsync(session);

            foreach (var (paymentMethodId, opening) in openingBalances)
            {
                await _databaseService.Connection.InsertAsync(new PosSessionBalance
                {
                    PosSessionId = session.Id,
                    PosPaymentMethodId = paymentMethodId,
                    OpeningBalance = opening
                });
            }

            return session;
        }

        public async Task RecordCashMovementAsync(int sessionId, int paymentMethodId, string movementType, decimal amount, string reason, string username)
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException("Amount must be greater than zero.");
            }

            await _databaseService.Connection.InsertAsync(new PosCashMovement
            {
                PosSessionId = sessionId,
                PosPaymentMethodId = paymentMethodId,
                MovementType = movementType,
                Amount = amount,
                Reason = reason,
                Date = DateTime.Now,
                Username = username
            });
        }

        public async Task<List<PosCashMovement>> GetCashMovementsAsync(int sessionId)
        {
            return await _databaseService.Connection.Table<PosCashMovement>()
                .Where(m => !m.IsDeleted && m.PosSessionId == sessionId)
                .OrderByDescending(m => m.Date)
                .ToListAsync();
        }

        public async Task<PosSessionSummary> GetSessionSummaryAsync(int sessionId)
        {
            var session = await _databaseService.Connection.FindAsync<PosSession>(sessionId)
                ?? throw new InvalidOperationException("Session not found.");

            var balances = await _databaseService.Connection.Table<PosSessionBalance>()
                .Where(b => !b.IsDeleted && b.PosSessionId == sessionId)
                .ToListAsync();

            var methods = await _databaseService.Connection.Table<PosPaymentMethod>().ToListAsync();
            var movements = await GetCashMovementsAsync(sessionId);

            var orders = await _databaseService.Connection.Table<SalesOrder>()
                .Where(so => !so.IsDeleted && so.PosSessionId == sessionId)
                .ToListAsync();
            var orderIds = orders.Select(o => o.Id).ToList();

            var tenders = await _databaseService.Connection.Table<PosSalePayment>()
                .Where(p => !p.IsDeleted)
                .ToListAsync();
            var tendersForSession = tenders.Where(t => orderIds.Contains(t.SalesOrderId)).ToList();

            var customers = await _databaseService.Connection.Table<Customer>().ToListAsync();
            var orderItems = orders.Select(so => new SalesOrderListItem
            {
                SalesOrder = so,
                CustomerName = customers.FirstOrDefault(c => c.Id == so.CustomerId)?.Name ?? "Walk-in Customer"
            }).OrderByDescending(o => o.SalesOrder.OrderDate).ToList();

            var rows = new List<PosSessionBalanceRow>();
            foreach (var balance in balances)
            {
                var method = methods.FirstOrDefault(m => m.Id == balance.PosPaymentMethodId);
                rows.Add(new PosSessionBalanceRow
                {
                    PosPaymentMethodId = balance.PosPaymentMethodId,
                    PaymentMethodName = method?.Name ?? "Unknown",
                    OpeningBalance = balance.OpeningBalance,
                    SalesTotal = tendersForSession.Where(t => t.PosPaymentMethodId == balance.PosPaymentMethodId).Sum(t => t.Amount),
                    CashIn = movements.Where(m => m.PosPaymentMethodId == balance.PosPaymentMethodId && m.MovementType == "In").Sum(m => m.Amount),
                    CashOut = movements.Where(m => m.PosPaymentMethodId == balance.PosPaymentMethodId && m.MovementType == "Out").Sum(m => m.Amount),
                    CountedClosingBalance = balance.CountedClosingBalance
                });
            }

            return new PosSessionSummary
            {
                Session = session,
                Balances = rows,
                Orders = orderItems
            };
        }

        public async Task CloseSessionAsync(int sessionId, Dictionary<int, decimal> countedBalances, string username, string notes = "")
        {
            var session = await _databaseService.Connection.FindAsync<PosSession>(sessionId)
                ?? throw new InvalidOperationException("Session not found.");
            if (session.Status == "Closed")
            {
                throw new InvalidOperationException("Session is already closed.");
            }

            var balances = await _databaseService.Connection.Table<PosSessionBalance>()
                .Where(b => !b.IsDeleted && b.PosSessionId == sessionId)
                .ToListAsync();

            foreach (var balance in balances)
            {
                if (countedBalances.TryGetValue(balance.PosPaymentMethodId, out var counted))
                {
                    balance.CountedClosingBalance = counted;
                    await _databaseService.Connection.UpdateAsync(balance);
                }
            }

            session.Status = "Closed";
            session.ClosedByUsername = username;
            session.ClosedAt = DateTime.Now;
            session.Notes = notes;
            await _databaseService.Connection.UpdateAsync(session);
        }

        public async Task<List<PosSession>> GetAllSessionsAsync()
        {
            return await _databaseService.Connection.Table<PosSession>()
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.OpenedAt)
                .ToListAsync();
        }
    }
}
