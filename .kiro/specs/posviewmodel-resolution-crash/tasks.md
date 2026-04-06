# Implementation Plan

- [-] 1. Write bug condition exploration test
  - **Property 1: Bug Condition** - XAML Type Resolution Failure with using: Syntax
  - **CRITICAL**: This test MUST FAIL on unfixed code - failure confirms the bug exists
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: This test encodes the expected behavior - it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate the bug exists
  - **Scoped PBT Approach**: For deterministic bugs, scope the property to the concrete failing case(s) to ensure reproducibility
  - Test that application crashes when loading any View with `using:` namespace syntax
  - Verify error message contains "Unable to resolve type vm:POSViewModel" (or other ViewModel types)
  - Test concrete failing cases: POSView.axaml, DashboardView.axaml, InventoryView.axaml (all 11 Views)
  - The test assertions should match the Expected Behavior Properties from design: XAML parser successfully resolves ViewModel types at runtime
  - Run test on UNFIXED code
  - **EXPECTED OUTCOME**: Test FAILS (this is correct - it proves the bug exists)
  - Document counterexamples found: which Views crash, exact error messages, stack traces
  - Mark task complete when test is written, run, and failure is documented
  - _Requirements: 2.1, 2.2, 2.3_

- [ ] 2. Write preservation property tests (BEFORE implementing fix)
  - **Property 2: Preservation** - Existing Functionality Unchanged
  - **IMPORTANT**: Follow observation-first methodology
  - Observe behavior on UNFIXED code for non-buggy inputs (Views that don't use `using:` syntax, or functionality that doesn't depend on namespace resolution)
  - Write property-based tests capturing observed behavior patterns from Preservation Requirements
  - Test data bindings: verify all bindings display correct values (product lists, totals, dashboard metrics)
  - Test command bindings: verify all commands execute correctly (AddToCart, Checkout, navigation)
  - Test design-time support: verify XAML previewer works (if Views can be opened without crashing)
  - Test compiled bindings: verify x:DataType provides type safety
  - Property-based testing generates many test cases for stronger guarantees
  - Run tests on UNFIXED code (may need to test on Views without the bug, or test functionality in isolation)
  - **EXPECTED OUTCOME**: Tests PASS (this confirms baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [ ] 3. Fix XAML namespace resolution for all 11 View files

  - [ ] 3.1 Update POSView.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:POSViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input.containsNamespaceDeclaration("xmlns:vm=\"using:InventoryManagementSystem.UI.ViewModels\"") AND input.viewModelAssembly != input.viewAssembly_
    - _Expected_Behavior: XAML parser successfully resolves vm:POSViewModel at runtime without crashes_
    - _Preservation: Data bindings, command bindings, design-time support remain unchanged_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.2 Update DashboardView.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:DashboardViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input uses using: syntax for cross-assembly ViewModels_
    - _Expected_Behavior: XAML parser successfully resolves vm:DashboardViewModel at runtime without crashes_
    - _Preservation: Dashboard metrics, modals, overlays remain functional_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.3 Update InventoryView.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:InventoryViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input uses using: syntax for cross-assembly ViewModels_
    - _Expected_Behavior: XAML parser successfully resolves vm:InventoryViewModel at runtime without crashes_
    - _Preservation: Product grid, stock management, CRUD operations remain functional_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.4 Update ReportsView.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:ReportsViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input uses using: syntax for cross-assembly ViewModels_
    - _Expected_Behavior: XAML parser successfully resolves vm:ReportsViewModel at runtime without crashes_
    - _Preservation: Report generation and display remain functional_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.5 Update AnalyticsView.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:AnalyticsViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input uses using: syntax for cross-assembly ViewModels_
    - _Expected_Behavior: XAML parser successfully resolves vm:AnalyticsViewModel at runtime without crashes_
    - _Preservation: Analytics charts and metrics remain functional_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.6 Update UsersView.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:UsersViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input uses using: syntax for cross-assembly ViewModels_
    - _Expected_Behavior: XAML parser successfully resolves vm:UsersViewModel at runtime without crashes_
    - _Preservation: User management functionality remains functional_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.7 Update SettingsView.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:SettingsViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input uses using: syntax for cross-assembly ViewModels_
    - _Expected_Behavior: XAML parser successfully resolves vm:SettingsViewModel at runtime without crashes_
    - _Preservation: Settings configuration remains functional_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.8 Update LicenseView.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:LicenseViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input uses using: syntax for cross-assembly ViewModels_
    - _Expected_Behavior: XAML parser successfully resolves vm:LicenseViewModel at runtime without crashes_
    - _Preservation: License management remains functional_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.9 Update LoginView.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:LoginViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input uses using: syntax for cross-assembly ViewModels_
    - _Expected_Behavior: XAML parser successfully resolves vm:LoginViewModel at runtime without crashes_
    - _Preservation: Login functionality remains functional_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.10 Update MainView.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:MainViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input uses using: syntax for cross-assembly ViewModels_
    - _Expected_Behavior: XAML parser successfully resolves vm:MainViewModel at runtime without crashes_
    - _Preservation: Main navigation sidebar remains functional_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.11 Update MainWindow.axaml namespace declaration
    - Replace `xmlns:vm="using:InventoryManagementSystem.UI.ViewModels"` with `xmlns:vm="clr-namespace:InventoryManagementSystem.UI.ViewModels;assembly=InventoryManagementSystem.Shared"`
    - Verify x:DataType="vm:MainViewModel" remains unchanged
    - Verify all binding expressions continue to use vm: prefix correctly
    - _Bug_Condition: isBugCondition(input) where input uses using: syntax for cross-assembly ViewModels_
    - _Expected_Behavior: XAML parser successfully resolves vm:MainViewModel at runtime without crashes_
    - _Preservation: Main window functionality remains functional_
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.12 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - XAML Type Resolution Success with clr-namespace Syntax
    - **IMPORTANT**: Re-run the SAME test from task 1 - do NOT write a new test
    - The test from task 1 encodes the expected behavior
    - When this test passes, it confirms the expected behavior is satisfied
    - Run bug condition exploration test from step 1
    - Verify application launches without crashes
    - Verify all 11 Views load successfully when navigated to
    - Verify no "Unable to resolve type" errors occur
    - **EXPECTED OUTCOME**: Test PASSES (confirms bug is fixed)
    - _Requirements: 2.1, 2.2, 2.3_

  - [ ] 3.13 Verify preservation tests still pass
    - **Property 2: Preservation** - Existing Functionality Unchanged
    - **IMPORTANT**: Re-run the SAME tests from task 2 - do NOT write new tests
    - Run preservation property tests from step 2
    - Verify data bindings display correct values across all Views
    - Verify command bindings execute correctly (AddToCart, Checkout, navigation, etc.)
    - Verify design-time support works in XAML previewer
    - Verify compiled bindings provide type safety (x:DataType works correctly)
    - **EXPECTED OUTCOME**: Tests PASS (confirms no regressions)
    - Confirm all tests still pass after fix (no regressions)
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [ ] 4. Checkpoint - Ensure all tests pass
  - Verify application launches successfully without crashes
  - Verify all 11 Views can be navigated to without errors
  - Verify all data bindings work correctly across all Views
  - Verify all command bindings execute correctly
  - Verify design-time support works in Avalonia XAML previewer
  - Ask the user if questions arise or if additional testing is needed
