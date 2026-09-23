using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class CustomerService
    {
        private readonly DatabaseService _databaseService;

        public CustomerService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _databaseService.Connection.Table<Customer>()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _databaseService.Connection.FindAsync<Customer>(id);
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            customer.CreatedAt = DateTime.Now;
            customer.IsActive = true;
            await _databaseService.Connection.InsertAsync(customer);
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await _databaseService.Connection.UpdateAsync(customer);
        }

        public async Task DeleteCustomerAsync(int id)
        {
            var customer = await _databaseService.Connection.FindAsync<Customer>(id);
            if (customer == null) return;
            customer.IsActive = false;
            await _databaseService.Connection.UpdateAsync(customer);
        }

        public async Task<Customer> EnsureWalkInCustomerAsync()
        {
            var all = await _databaseService.Connection.Table<Customer>().ToListAsync();
            var walkIn = all.FirstOrDefault(c => c.Name == "Walk-in Customer");
            if (walkIn != null)
            {
                return walkIn;
            }

            var customer = new Customer
            {
                Name = "Walk-in Customer",
                Phone = "0000000000",
                Email = "walkin@pos.com",
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            await _databaseService.Connection.InsertAsync(customer);
            return customer;
        }
    }
}
