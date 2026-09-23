using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.UI.ViewModels;

public partial class SetupWizardViewModel : ViewModelBase
{
    private readonly IndustryTemplateService _industryTemplateService;
    private readonly SettingsService _settingsService;
    private readonly CustomFieldService _customFieldService;
    private readonly Action _onCompleted;

    public LanguageService Language { get; }

    [ObservableProperty]
    private int _currentStep = 1;

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
    }

    // Step 1 — Business info
    [ObservableProperty]
    private string _storeName = string.Empty;

    [ObservableProperty]
    private string _storeAddress = string.Empty;

    [ObservableProperty]
    private string _currencySymbol = string.Empty;

    // Step 2 — Industry template picker
    public ObservableCollection<IndustryTemplate> AvailableTemplates { get; } = new();

    [ObservableProperty]
    private IndustryTemplate? _selectedTemplate;

    partial void OnSelectedTemplateChanged(IndustryTemplate? value)
    {
        OnPropertyChanged(nameof(TemplateSummary));
        ErrorMessage = string.Empty;
    }

    // Step 3 — Confirmation summary
    public string TemplateSummary
    {
        get
        {
            if (SelectedTemplate == null)
            {
                return "No industry template will be applied. You can add categories, custom fields and terminology manually later from Settings.";
            }

            var categories = SelectedTemplate.DefaultCategories.Count > 0
                ? string.Join(", ", SelectedTemplate.DefaultCategories)
                : "None";

            var fields = SelectedTemplate.SuggestedCustomFields.Count > 0
                ? string.Join(", ", SelectedTemplate.SuggestedCustomFields.Select(f => f.FieldLabel))
                : "None";

            var terminology = SelectedTemplate.TerminologyOverrides.Count > 0
                ? string.Join(", ", SelectedTemplate.TerminologyOverrides.Select(t => $"{t.Key} → {t.Value}"))
                : "None";

            var modules = SelectedTemplate.EnabledModules.Where(m => m.Value).Select(m => m.Key).ToList();
            var modulesText = modules.Count > 0 ? string.Join(", ", modules) : "None";

            return $"Categories to add: {categories}\nCustom fields to add: {fields}\nTerminology changes: {terminology}\nModules enabled: {modulesText}";
        }
    }

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public SetupWizardViewModel(
        IndustryTemplateService industryTemplateService,
        SettingsService settingsService,
        CustomFieldService customFieldService,
        LanguageService languageService,
        Action onCompleted)
    {
        _industryTemplateService = industryTemplateService;
        _settingsService = settingsService;
        _customFieldService = customFieldService;
        _onCompleted = onCompleted;
        Language = languageService;

        var s = _settingsService.CurrentSettings;
        _storeName = s.StoreName;
        _storeAddress = s.StoreAddress;
        _currencySymbol = s.CurrencySymbol;

        foreach (var template in IndustryTemplateService.GetAvailableTemplates())
        {
            AvailableTemplates.Add(template);
        }
    }

    [RelayCommand]
    private void Next()
    {
        ErrorMessage = string.Empty;

        if (CurrentStep == 1)
        {
            if (string.IsNullOrWhiteSpace(StoreName))
            {
                ErrorMessage = "Please enter your store/business name.";
                return;
            }

            CurrentStep = 2;
        }
        else if (CurrentStep == 2)
        {
            if (SelectedTemplate == null)
            {
                ErrorMessage = "Please select an industry template, or use \"Skip / I'll configure manually\" below.";
                return;
            }

            CurrentStep = 3;
        }
    }

    [RelayCommand]
    private void Back()
    {
        ErrorMessage = string.Empty;
        if (CurrentStep > 1)
        {
            CurrentStep--;
        }
    }

    private void SaveBusinessInfoIntoSettings()
    {
        var s = _settingsService.CurrentSettings;
        s.StoreName = string.IsNullOrWhiteSpace(StoreName) ? s.StoreName : StoreName.Trim();
        s.StoreAddress = StoreAddress?.Trim() ?? s.StoreAddress;
        s.CurrencySymbol = string.IsNullOrWhiteSpace(CurrencySymbol) ? s.CurrencySymbol : CurrencySymbol.Trim();
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            SaveBusinessInfoIntoSettings();

            if (SelectedTemplate != null)
            {
                await _industryTemplateService.ApplyTemplateAsync(SelectedTemplate.Key, _settingsService, _customFieldService);
            }
            else
            {
                _settingsService.CurrentSettings.BusinessType = "custom";
                _settingsService.CurrentSettings.SetupCompleted = true;
            }

            // ApplyTemplateAsync already persists settings, but save again to be safe
            // for the no-template path and to guarantee store info is written.
            _settingsService.SaveSettings();

            _onCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to complete setup: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SkipSetup()
    {
        SaveBusinessInfoIntoSettings();

        _settingsService.CurrentSettings.BusinessType = "custom";
        _settingsService.CurrentSettings.SetupCompleted = true;
        _settingsService.SaveSettings();

        _onCompleted?.Invoke();
    }
}
