using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class PaymentService
    {
        private readonly DatabaseService _databaseService;

        public PaymentService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
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
    }
}
