using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels
{
    public partial class CustomersViewModel : ViewModelBase
    {
        private readonly CustomerService _customerService;
        private readonly DocumentAttachmentService? _attachmentService;

        public LanguageService Language { get; }

        [ObservableProperty]
        private ObservableCollection<Customer> _customers = new();

        [ObservableProperty]
        private ObservableCollection<DocumentAttachment> _customerAttachments = new();

        [ObservableProperty]
        private string _attachmentStatusMessage = string.Empty;

        [ObservableProperty]
        private Customer _currentCustomer = new();

        [ObservableProperty]
        private Customer? _selectedCustomer;

        [ObservableProperty]
        private bool _isDeleteConfirmationOpen;

        [ObservableProperty]
        private Customer? _customerToDelete;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _hasWebsite;

        [ObservableProperty]
        private bool _isFormVisible;

        public ObservableCollection<string> PaymentTermsOptions { get; } = new()
        {
            "Direct Payment",
            "Installment",
            "Payment on Delivery",
            "Other"
        };

        public CustomersViewModel(
            CustomerService customerService,
            LanguageService languageService,
            DocumentAttachmentService? attachmentService = null)
        {
            _customerService = customerService;
            Language = languageService;
            _attachmentService = attachmentService;
            LoadCustomersCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadCustomers()
        {
            var list = await _customerService.GetAllCustomersAsync();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.ToLower();
                list = list.Where(c =>
                    c.Name.ToLower().Contains(query) ||
                    c.ContactPerson.ToLower().Contains(query) ||
                    c.Phone.ToLower().Contains(query) ||
                    c.Email.ToLower().Contains(query) ||
                    c.Address.ToLower().Contains(query) ||
                    (c.TinNumber != null && c.TinNumber.ToLower().Contains(query)) ||
                    (c.WebsiteUrl != null && c.WebsiteUrl.ToLower().Contains(query))
                ).ToList();
            }

            Customers = new ObservableCollection<Customer>(list);
        }

        [RelayCommand]
        private async Task SaveCustomer()
        {
            if (CurrentCustomer.Id == 0)
            {
                await _customerService.AddCustomerAsync(CurrentCustomer);
            }
            else
            {
                await _customerService.UpdateCustomerAsync(CurrentCustomer);
            }

            CurrentCustomer = new Customer();
            IsFormVisible = false;
            await LoadCustomers();
        }

        [RelayCommand]
        private void EditCustomer(Customer? customer)
        {
            if (customer == null)
            {
                CurrentCustomer = new Customer();
                return;
            }
            CurrentCustomer = new Customer
            {
                Id = customer.Id,
                Name = customer.Name,
                ContactPerson = customer.ContactPerson,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                PaymentTerms = customer.PaymentTerms,
                TinNumber = customer.TinNumber,
                WebsiteUrl = customer.WebsiteUrl,
                IsActive = customer.IsActive,
                CreatedAt = customer.CreatedAt
            };
            IsFormVisible = true;
        }

        [RelayCommand]
        private void ShowAddCustomerForm()
        {
            CurrentCustomer = new Customer();
            IsFormVisible = true;
        }

        [RelayCommand]
        private void CloseForm()
        {
            IsFormVisible = false;
        }

        [RelayCommand]
        private void ConfirmDeleteCustomer(Customer? customer)
        {
            if (customer == null) return;
            CustomerToDelete = customer;
            IsDeleteConfirmationOpen = true;
        }

        [RelayCommand]
        private void CancelDelete()
        {
            IsDeleteConfirmationOpen = false;
            CustomerToDelete = null;
        }

        [RelayCommand]
        private async Task ExecuteDelete()
        {
            if (CustomerToDelete == null) return;

            await _customerService.DeleteCustomerAsync(CustomerToDelete.Id);
            IsDeleteConfirmationOpen = false;
            CustomerToDelete = null;
            SelectedCustomer = null;
            await LoadCustomers();
        }

        [RelayCommand]
        private void OpenWebsite()
        {
            if (SelectedCustomer == null || string.IsNullOrWhiteSpace(SelectedCustomer.WebsiteUrl)) return;
            try
            {
                var url = SelectedCustomer.WebsiteUrl;
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { /* Ignore process launch errors */ }
        }

        partial void OnSelectedCustomerChanged(Customer? value)
        {
            HasWebsite = value != null && !string.IsNullOrWhiteSpace(value.WebsiteUrl);
            _ = LoadCustomerAttachmentsAsync();
        }

        private async Task LoadCustomerAttachmentsAsync()
        {
            CustomerAttachments.Clear();
            if (_attachmentService == null || SelectedCustomer == null || SelectedCustomer.Id <= 0) return;
            var list = await _attachmentService.GetAttachmentsAsync("Customer", SelectedCustomer.Id);
            foreach (var a in list) CustomerAttachments.Add(a);
        }

        [RelayCommand]
        private async Task AddCustomerAttachment(string? filePath)
        {
            AttachmentStatusMessage = string.Empty;
            if (_attachmentService == null || SelectedCustomer == null || SelectedCustomer.Id <= 0 || string.IsNullOrWhiteSpace(filePath))
            {
                AttachmentStatusMessage = "Select a saved customer and a file first.";
                return;
            }

            try
            {
                await _attachmentService.AddAttachmentAsync(
                    "Customer",
                    SelectedCustomer.Id,
                    filePath,
                    UserSession.CurrentUser?.Username ?? "System");
                await LoadCustomerAttachmentsAsync();
                AttachmentStatusMessage = "Attachment added.";
            }
            catch (Exception ex)
            {
                AttachmentStatusMessage = ex.Message;
            }
        }

        [RelayCommand]
        private async Task RemoveCustomerAttachment(DocumentAttachment? attachment)
        {
            if (_attachmentService == null || attachment == null) return;
            await _attachmentService.DeleteAttachmentAsync(attachment.Id, UserSession.CurrentUser?.Username ?? "System");
            await LoadCustomerAttachmentsAsync();
        }

        [RelayCommand]
        private void OpenCustomerAttachment(DocumentAttachment? attachment)
        {
            if (_attachmentService == null || attachment == null) return;
            try { _attachmentService.OpenAttachment(attachment); }
            catch (Exception ex) { AttachmentStatusMessage = ex.Message; }
        }

        partial void OnSearchTextChanged(string value)
        {
            LoadCustomersCommand.Execute(null);
        }
    }
}
