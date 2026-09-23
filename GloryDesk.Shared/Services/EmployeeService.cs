using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class EmployeeService
    {
        private readonly DatabaseService _databaseService;
        private readonly AuditService? _auditService;

        public EmployeeService(DatabaseService databaseService, AuditService? auditService = null)
        {
            _databaseService = databaseService;
            _auditService = auditService;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync(bool includeInactive = false)
        {
            var query = _databaseService.Connection.Table<Employee>();
            if (!includeInactive)
            {
                query = query.Where(e => e.IsActive);
            }
            var list = await query.ToListAsync();
            return list.OrderBy(e => e.Name).ToList();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id) =>
            await _databaseService.Connection.FindAsync<Employee>(id);

        public async Task<List<Employee>> GetEmployeesByLocationAsync(int locationId)
        {
            var list = await _databaseService.Connection.Table<Employee>()
                .Where(e => e.LocationId == locationId && e.IsActive)
                .ToListAsync();
            return list.OrderBy(e => e.Name).ToList();
        }

        public async Task AddEmployeeAsync(Employee employee, string username)
        {
            employee.CreatedAt = DateTime.Now;
            employee.IsActive = employee.Status != "Terminated";
            SyncMetadataHelper.Touch(employee);
            await _databaseService.Connection.InsertAsync(employee);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "Create", "Employee", employee.Id, ToAuditSnapshot(employee));
            }
        }

        public async Task UpdateEmployeeAsync(Employee employee, string username)
        {
            var old = await GetEmployeeByIdAsync(employee.Id);

            // Status is the source of truth: editing Status back to Active/OnLeave
            // re-activates a previously terminated employee, and setting it to
            // Terminated deactivates them — this is the only way to reactivate.
            employee.IsActive = employee.Status != "Terminated";
            if (employee.Status == "Terminated")
            {
                employee.TerminationDate ??= DateTime.Now;
            }
            else
            {
                employee.TerminationDate = null;
            }

            SyncMetadataHelper.Touch(employee);
            await _databaseService.Connection.UpdateAsync(employee);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(
                    username, "Update", "Employee", employee.Id,
                    ToAuditSnapshot(employee),
                    old != null ? ToAuditSnapshot(old) : null);
            }
        }

        /// <summary>Soft-deactivate. Payroll and attendance history stays linked to the record.</summary>
        public async Task DeactivateEmployeeAsync(int employeeId, string username)
        {
            var employee = await GetEmployeeByIdAsync(employeeId);
            if (employee == null) return;

            employee.IsActive = false;
            employee.Status = "Terminated";
            employee.TerminationDate ??= DateTime.Now;
            SyncMetadataHelper.Touch(employee);
            await _databaseService.Connection.UpdateAsync(employee);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "Deactivate", "Employee", employee.Id, ToAuditSnapshot(employee));
            }
        }

        private static object ToAuditSnapshot(Employee employee) => new
        {
            employee.Name,
            employee.Position,
            employee.Department,
            employee.LocationId,
            employee.Status,
            employee.EmploymentType,
            employee.PayFrequency,
            employee.BaseSalary
        };
    }
}
