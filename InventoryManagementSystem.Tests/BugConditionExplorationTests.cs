using Xunit;
using FsCheck;
using FsCheck.Xunit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Headless;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InventoryManagementSystem.Tests;

/// <summary>
/// Bug Condition Exploration Tests for XAML Type Resolution Failure
/// 
/// **Validates: Requirements 2.1, 2.2, 2.3**
/// 
/// CRITICAL: These tests are EXPECTED TO FAIL on unfixed code.
/// Test failure confirms the bug exists and surfaces counterexamples.
/// DO NOT attempt to fix the test or code when it fails.
/// 
/// The test encodes the expected behavior - it will validate the fix when it passes after implementation.
/// </summary>
public class BugConditionExplorationTests
{
    /// <summary>
    /// All View files that use the using: namespace syntax and are affected by the bug
    /// </summary>
    private static readonly string[] AffectedViewFiles = new[]
    {
        "InventoryManagementSystem.Shared/UI/Views/POSView.axaml",
        "InventoryManagementSystem.Shared/UI/Views/DashboardView.axaml",
        "InventoryManagementSystem.Shared/UI/Views/InventoryView.axaml",
        "InventoryManagementSystem.Shared/UI/Views/ReportsView.axaml",
        "InventoryManagementSystem.Shared/UI/Views/AnalyticsView.axaml",
        "InventoryManagementSystem.Shared/UI/Views/UsersView.axaml",
        "InventoryManagementSystem.Shared/UI/Views/SettingsView.axaml",
        "InventoryManagementSystem.Shared/UI/Views/LicenseView.axaml",
        "InventoryManagementSystem.Shared/UI/Views/LoginView.axaml",
        "InventoryManagementSystem.Shared/UI/Views/MainView.axaml",
        "InventoryManagementSystem.Shared/UI/Views/MainWindow.axaml"
    };

    /// <summary>
    /// Generator for affected view file paths
    /// Scoped to the concrete failing cases to ensure reproducibility
    /// </summary>
    public static Arbitrary<string> ViewFilePathGenerator()
    {
        return Gen.Elements(AffectedViewFiles).ToArbitrary();
    }

    /// <summary>
    /// **Property 1: Bug Condition** - XAML Type Resolution Failure with using: Syntax
    /// 
    /// **Validates: Requirements 2.1, 2.2, 2.3**
    /// 
    /// **CRITICAL**: This test MUST FAIL on unfixed code - failure confirms the bug exists
    /// **DO NOT attempt to fix the test or the code when it fails**
    /// **NOTE**: This test encodes the expected behavior - it will validate the fix when it passes after implementation
    /// 
    /// **GOAL**: Surface counterexamples that demonstrate the bug exists
    /// 
    /// **Scoped PBT Approach**: For deterministic bugs, scope the property to the concrete failing case(s) to ensure reproducibility
    /// 
    /// Test that application crashes when loading any View with `using:` namespace syntax
    /// Verify error message contains "Unable to resolve type vm:*ViewModel" (or other ViewModel types)
    /// Test concrete failing cases: POSView.axaml, DashboardView.axaml, InventoryView.axaml (all 11 Views)
    /// 
    /// The test assertions match the Expected Behavior Properties from design:
    /// XAML parser successfully resolves ViewModel types at runtime
    /// </summary>
    [Property(Arbitrary = new[] { typeof(BugConditionExplorationTests) }, MaxTest = 11)]
    public Property XamlTypeResolutionWithUsingNamespaceSyntax_ShouldSucceed(string viewFilePath)
    {
        // Arrange: Initialize Avalonia headless for testing
        var testResult = new TestResult
        {
            ViewFile = viewFilePath,
            TypeResolved = false,
            ViewLoaded = false,
            ErrorMessage = null,
            StackTrace = null
        };

        try
        {
            // Initialize Avalonia if not already initialized
            if (Application.Current == null)
            {
                AppBuilder.Configure<App>()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                    .SetupWithoutStarting();
            }

            // Act: Attempt to load the XAML view
            // This will trigger the XAML parser to resolve the vm: namespace
            var viewType = GetViewTypeFromPath(viewFilePath);
            
            if (viewType != null)
            {
                var view = Activator.CreateInstance(viewType) as Control;
                
                if (view != null)
                {
                    testResult.ViewLoaded = true;
                    testResult.TypeResolved = true;
                }
            }
        }
        catch (Exception ex)
        {
            // Capture the error for counterexample documentation
            testResult.ErrorMessage = ex.Message;
            testResult.StackTrace = ex.StackTrace;
            testResult.TypeResolved = false;
            testResult.ViewLoaded = false;
        }

        // Assert: Expected behavior - XAML parser successfully resolves ViewModel types at runtime
        // On UNFIXED code, this will FAIL and surface counterexamples
        // On FIXED code, this will PASS and validate the fix
        return (testResult.TypeResolved && testResult.ViewLoaded)
            .Label($"View '{viewFilePath}' should load successfully with resolved ViewModel types")
            .When(AffectedViewFiles.Contains(viewFilePath))
            .Classify(testResult.TypeResolved, "Type Resolved")
            .Classify(testResult.ViewLoaded, "View Loaded")
            .Classify(!string.IsNullOrEmpty(testResult.ErrorMessage), "Error Occurred")
            .Collect($"View: {System.IO.Path.GetFileName(viewFilePath)}")
            .Collect(testResult.ErrorMessage != null ? "Failed" : "Passed");
    }

    /// <summary>
    /// Helper method to get the View type from the file path
    /// </summary>
    private Type? GetViewTypeFromPath(string viewFilePath)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(viewFilePath);
        var typeName = $"InventoryManagementSystem.UI.Views.{fileName}";
        
        // Try to load from Shared assembly
        var sharedAssembly = typeof(InventoryManagementSystem.UI.Views.POSView).Assembly;
        return sharedAssembly.GetType(typeName);
    }

    /// <summary>
    /// Test result structure for documenting counterexamples
    /// </summary>
    private class TestResult
    {
        public string ViewFile { get; set; } = string.Empty;
        public bool TypeResolved { get; set; }
        public bool ViewLoaded { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
    }
}
