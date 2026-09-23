using CommunityToolkit.Mvvm.ComponentModel;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class AttendanceRowViewModel : ViewModelBase
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;

        [ObservableProperty]
        private string _status = "Present";

        [ObservableProperty]
        private string _notes = string.Empty;
    }
}
