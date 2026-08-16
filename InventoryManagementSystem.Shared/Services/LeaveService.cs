using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class LeaveService
    {
        private readonly DatabaseService _databaseService;
        private readonly AttendanceService _attendanceService;
        private readonly AuditService? _auditService;

        public LeaveService(DatabaseService databaseService, AttendanceService attendanceService, AuditService? auditService = null)
        {
            _databaseService = databaseService;
            _attendanceService = attendanceService;
            _auditService = auditService;
        }

        public async Task<List<LeaveType>> GetLeaveTypesAsync()
        {
            var list = await _databaseService.Connection.Table<LeaveType>()
                .Where(t => t.IsActive)
                .ToListAsync();
            return list.OrderBy(t => t.Name).ToList();
        }

        public async Task<LeaveRequest> SubmitLeaveRequestAsync(LeaveRequest request, string username)
        {
            if (request.EndDate.Date < request.StartDate.Date)
            {
                throw new InvalidOperationException("Leave end date cannot be before the start date.");
            }

            request.StartDate = request.StartDate.Date;
            request.EndDate = request.EndDate.Date;
            request.DaysCount = (request.EndDate - request.StartDate).Days + 1;
            request.Status = "Pending";
            request.RequestedByUsername = username;
            request.RequestedAt = DateTime.Now;
            request.CreatedAt = DateTime.Now;
            SyncMetadataHelper.Touch(request);

            await _databaseService.Connection.InsertAsync(request);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "Submit", "LeaveRequest", request.Id, ToAuditSnapshot(request));
            }

            return request;
        }

        public async Task<LeaveRequest> ApproveLeaveRequestAsync(int leaveRequestId, int locationId, string reviewerUsername, string reviewNotes = "")
        {
            var request = await _databaseService.Connection.FindAsync<LeaveRequest>(leaveRequestId)
                          ?? throw new InvalidOperationException("Leave request not found.");

            if (request.Status != "Pending")
            {
                throw new InvalidOperationException($"Leave request is already {request.Status}.");
            }

            request.Status = "Approved";
            request.ReviewedByUsername = reviewerUsername;
            request.ReviewedAt = DateTime.Now;
            request.ReviewNotes = reviewNotes;
            SyncMetadataHelper.Touch(request);
            await _databaseService.Connection.UpdateAsync(request);

            // Mark the days as OnLeave on the attendance register so there's no double-entry.
            await _attendanceService.ApplyApprovedLeaveAsync(request, locationId, reviewerUsername);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(reviewerUsername, "Approve", "LeaveRequest", request.Id, ToAuditSnapshot(request));
            }

            return request;
        }

        public async Task<LeaveRequest> RejectLeaveRequestAsync(int leaveRequestId, string reviewerUsername, string reviewNotes = "")
        {
            var request = await _databaseService.Connection.FindAsync<LeaveRequest>(leaveRequestId)
                          ?? throw new InvalidOperationException("Leave request not found.");

            if (request.Status != "Pending")
            {
                throw new InvalidOperationException($"Leave request is already {request.Status}.");
            }

            request.Status = "Rejected";
            request.ReviewedByUsername = reviewerUsername;
            request.ReviewedAt = DateTime.Now;
            request.ReviewNotes = reviewNotes;
            SyncMetadataHelper.Touch(request);
            await _databaseService.Connection.UpdateAsync(request);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(reviewerUsername, "Reject", "LeaveRequest", request.Id, ToAuditSnapshot(request));
            }

            return request;
        }

        public async Task<List<LeaveRequest>> GetPendingLeaveRequestsAsync()
        {
            var list = await _databaseService.Connection.Table<LeaveRequest>()
                .Where(r => r.Status == "Pending")
                .ToListAsync();
            return list.OrderBy(r => r.StartDate).ToList();
        }

        public async Task<List<LeaveRequest>> GetLeaveRequestsForEmployeeAsync(int employeeId)
        {
            var list = await _databaseService.Connection.Table<LeaveRequest>()
                .Where(r => r.EmployeeId == employeeId)
                .ToListAsync();
            return list.OrderByDescending(r => r.StartDate).ToList();
        }

        /// <summary>Default entitlement for the year minus days already used in Approved requests.</summary>
        public async Task<int> GetLeaveBalanceAsync(int employeeId, int leaveTypeId, int year)
        {
            var leaveType = await _databaseService.Connection.FindAsync<LeaveType>(leaveTypeId);
            if (leaveType == null) return 0;

            var used = await GetUsedDaysAsync(employeeId, leaveTypeId, year);
            return leaveType.DefaultDaysPerYear - used;
        }

        public async Task<List<(LeaveType Type, int Entitled, int Used, int Balance)>> GetLeaveBalancesForEmployeeAsync(int employeeId, int year)
        {
            var types = await GetLeaveTypesAsync();
            var result = new List<(LeaveType, int, int, int)>();

            foreach (var type in types)
            {
                var used = await GetUsedDaysAsync(employeeId, type.Id, year);
                result.Add((type, type.DefaultDaysPerYear, used, type.DefaultDaysPerYear - used));
            }

            return result;
        }

        private async Task<int> GetUsedDaysAsync(int employeeId, int leaveTypeId, int year)
        {
            var yearStart = new DateTime(year, 1, 1);
            var yearEnd = new DateTime(year, 12, 31);

            var approved = await _databaseService.Connection.Table<LeaveRequest>()
                .Where(r => r.EmployeeId == employeeId
                            && r.LeaveTypeId == leaveTypeId
                            && r.Status == "Approved"
                            && r.StartDate >= yearStart
                            && r.StartDate <= yearEnd)
                .ToListAsync();

            return approved.Sum(r => r.DaysCount);
        }

        private static object ToAuditSnapshot(LeaveRequest request) => new
        {
            request.EmployeeId,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate,
            request.DaysCount,
            request.Status
        };
    }
}
