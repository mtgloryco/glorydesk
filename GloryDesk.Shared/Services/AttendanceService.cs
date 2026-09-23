using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class AttendanceService
    {
        private readonly DatabaseService _databaseService;
        private readonly AuditService? _auditService;

        public AttendanceService(DatabaseService databaseService, AuditService? auditService = null)
        {
            _databaseService = databaseService;
            _auditService = auditService;
        }

        /// <summary>Marks (or overwrites) one employee's attendance for a single day.</summary>
        public async Task<AttendanceRecord> MarkAttendanceAsync(AttendanceRecord record, string username)
        {
            var day = record.Date.Date;
            var existing = await _databaseService.Connection.Table<AttendanceRecord>()
                .FirstOrDefaultAsync(a => a.EmployeeId == record.EmployeeId && a.Date == day);

            if (existing != null)
            {
                existing.Status = record.Status;
                existing.ClockIn = record.ClockIn;
                existing.ClockOut = record.ClockOut;
                existing.LocationId = record.LocationId;
                existing.Notes = record.Notes;
                existing.SourceLeaveRequestId = record.SourceLeaveRequestId;
                SyncMetadataHelper.Touch(existing);
                await _databaseService.Connection.UpdateAsync(existing);

                if (_auditService != null)
                {
                    await _auditService.LogActionAsync(username, "Update", "AttendanceRecord", existing.Id, ToAuditSnapshot(existing));
                }

                return existing;
            }

            record.Date = day;
            record.CreatedAt = DateTime.Now;
            SyncMetadataHelper.Touch(record);
            await _databaseService.Connection.InsertAsync(record);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "Create", "AttendanceRecord", record.Id, ToAuditSnapshot(record));
            }

            return record;
        }

        public async Task MarkBulkAttendanceAsync(IEnumerable<AttendanceRecord> records, string username)
        {
            foreach (var record in records)
            {
                await MarkAttendanceAsync(record, username);
            }
        }

        public async Task<List<AttendanceRecord>> GetAttendanceForDateAsync(DateTime date, int? locationId = null)
        {
            var day = date.Date;
            var query = _databaseService.Connection.Table<AttendanceRecord>().Where(a => a.Date == day);
            var list = await query.ToListAsync();
            return locationId.HasValue ? list.Where(a => a.LocationId == locationId.Value).ToList() : list;
        }

        public async Task<List<AttendanceRecord>> GetAttendanceHistoryAsync(int employeeId, DateTime from, DateTime to)
        {
            var fromDay = from.Date;
            var toDay = to.Date;
            var list = await _databaseService.Connection.Table<AttendanceRecord>()
                .Where(a => a.EmployeeId == employeeId && a.Date >= fromDay && a.Date <= toDay)
                .ToListAsync();
            return list.OrderBy(a => a.Date).ToList();
        }

        public async Task<AttendanceSummary> GetMonthlySummaryAsync(int employeeId, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var records = await GetAttendanceHistoryAsync(employeeId, from, to);

            return new AttendanceSummary
            {
                EmployeeId = employeeId,
                Year = year,
                Month = month,
                PresentDays = records.Count(r => r.Status == "Present"),
                AbsentDays = records.Count(r => r.Status == "Absent"),
                LateDays = records.Count(r => r.Status == "Late"),
                HalfDays = records.Count(r => r.Status == "HalfDay"),
                OnLeaveDays = records.Count(r => r.Status == "OnLeave"),
                MarkedDays = records.Count
            };
        }

        /// <summary>Creates (or overwrites) OnLeave attendance rows for every day of an approved leave request.</summary>
        public async Task ApplyApprovedLeaveAsync(LeaveRequest leaveRequest, int locationId, string username)
        {
            for (var day = leaveRequest.StartDate.Date; day <= leaveRequest.EndDate.Date; day = day.AddDays(1))
            {
                await MarkAttendanceAsync(new AttendanceRecord
                {
                    EmployeeId = leaveRequest.EmployeeId,
                    Date = day,
                    Status = "OnLeave",
                    LocationId = locationId,
                    SourceLeaveRequestId = leaveRequest.Id
                }, username);
            }
        }

        private static object ToAuditSnapshot(AttendanceRecord record) => new
        {
            record.EmployeeId,
            record.Date,
            record.Status,
            record.LocationId
        };
    }

    public class AttendanceSummary
    {
        public int EmployeeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public int HalfDays { get; set; }
        public int OnLeaveDays { get; set; }
        public int MarkedDays { get; set; }
    }
}
