using FlaUI.Core.AutomationElements;
using Core.Domain.Entities.Identity;

namespace BasculaBotTests
{
    /// <summary>
    /// Group 5: the cross-terminal weight-entry lifecycle, direct-pick (no pedido line) —
    /// tasks 5.1-5.6.
    ///
    /// Session-shape decision (task 5.6, design.md Open Question): implemented as ONE test
    /// method that runs the full 5.1-5.5 sequence end to end, but NOT as a single login — 5.4
    /// specifically requires a Dispatching Operator, a different role than the Main-mode role
    /// that births/assigns/attaches the product (5.1-5.3) and later concludes it (5.5), so a
    /// role switch is unavoidable there regardless of session shape. Chosen approach: relaunch
    /// the app (a fresh AppDriver + fresh login) at each role boundary rather than simulating an
    /// in-app logout — simpler than juggling logout UI that doesn't clearly exist, and each
    /// relaunch is already how every other roleplay in this suite starts. Steps 5.1-5.3 share one
    /// login (nothing there requires a role change), matching the "simpler single-session
    /// approach first" the open question asked for.
    /// </summary>
    [Collection("BotSuite")]
    public class WeightEntryLifecycleRoleplayTests
    {
        private readonly BotUserProvisioningFixture _fixture;

        public WeightEntryLifecycleRoleplayTests(BotUserProvisioningFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Direct_pick_weight_entry_lifecycle_end_to_end()
        {
            string plate = $"BOT{DateTime.UtcNow:HHmmss}";

            // --- 5.1: Main-mode role births a new WeightEntry -------------------------------
            using (AppDriver app = AppDriver.Launch())
            {
                Window window = RoleplaySupport.GetMainWindow(app);
                RoleplaySupport.LoginAs(app, window, BotUserTemplates.ForRole(Role.Admin), _fixture.RolePassword);

                AutomationElement btnNewWeighProcess = AppDriver.WaitForElement(window, "BtnNewWeighProcess", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException($"BtnNewWeighProcess never appeared. Diagnostics: {app.WriteDiagnostics()}");
                AppDriver.Click(btnNewWeighProcess);

                AutomationElement plateEntry = AppDriver.WaitForElement(window, "EntryVehiclePlate", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException($"WeightingScreen never appeared. Diagnostics: {app.WriteDiagnostics()}");
                AutomationElement notesEntry = AppDriver.WaitForElement(window, "EntryNotes", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("EntryNotes never appeared.");

                AppDriver.TypeText(plateEntry, plate);
                AppDriver.TypeText(notesEntry, "Bot Driver");

                AutomationElement btnCaptureNewWeight = AppDriver.WaitForElement(window, "BtnCaptureNewWeight", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("BtnCaptureNewWeight never appeared.");
                AppDriver.Click(btnCaptureNewWeight);

                // Only shown for a brand-new entry with no product yet (BruteWeight <= 0 &&
                // Product is null) - exactly this case. "No" either way doesn't block the flow.
                RoleplaySupport.TryClickNativeDialogButton(window, "No");

                AutomationElement? row = null;
                DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
                while (DateTime.UtcNow < deadline && row is null)
                {
                    row = RoleplaySupport.FindRowContaining(window, "BtnSeleccionar", plate);
                    if (row is null) Thread.Sleep(300);
                }
                Assert.True(row is not null, $"New pending entry with plate {plate} should appear in PendingWeightsView. Diagnostics: {app.WriteDiagnostics()}");

                // --- 5.2: assign the fixed test partner + an ExternalTargetBehavior ---------
                AppDriver.Click(row!);

                AutomationElement btnPickPartner = AppDriver.WaitForElement(window, "BtnPickPartner", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException($"DetailedWeightView's BtnPickPartner never appeared. Diagnostics: {app.WriteDiagnostics()}");
                AutomationElement btnNuevoProducto = AppDriver.WaitForElement(window, "BtnNuevoProducto", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("BtnNuevoProducto never appeared.");

                // Blocked: no partner yet. BtnNuevoProducto isn't disabled - it shows a
                // DisplayAlert("Error", ..., "OK") on click instead (confirmed with the project
                // owner; see tasks.md 2.5's amendment).
                bool openedWithNoPartner = RoleplaySupport.ClickAndRaceOutcome(window, btnNuevoProducto, "SearchBar", "OK");
                Assert.False(openedWithNoPartner, "Product picker should be blocked before a partner is selected.");

                AppDriver.Click(btnPickPartner);
                AutomationElement partnerSearchBar = AppDriver.WaitForElement(window, "SearchBar", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("PartnerSelectView's SearchBar never appeared.");
                RoleplaySupport.Search(partnerSearchBar, "Claro Cervantes");

                AutomationElement partnerRow = AppDriver.WaitForElement(window, "selectedResult", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException($"No PartnerSelectView result for 'Claro Cervantes'. Diagnostics: {app.WriteDiagnostics()}");
                AppDriver.Click(partnerRow);
                RoleplaySupport.ConfirmSelectionDialog(window);

                // Blocked: partner set, but no ExternalTargetBehavior yet.
                AutomationElement btnNuevoProducto2 = AppDriver.WaitForElement(window, "BtnNuevoProducto", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("BtnNuevoProducto never reappeared after picking a partner.");
                bool openedWithNoTargetBehavior = RoleplaySupport.ClickAndRaceOutcome(window, btnNuevoProducto2, "SearchBar", "Ok");
                Assert.False(openedWithNoTargetBehavior, "Product picker should still be blocked before an ExternalTargetBehavior is selected.");

                AutomationElement targetBehaviorElement = AppDriver.WaitForElement(window, "PickerTargetBehavior", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("PickerTargetBehavior never appeared.");
                ComboBox targetBehaviorPicker = targetBehaviorElement.AsComboBox();
                targetBehaviorPicker.Select(0); // any option — no specific value matters (design.md)

                // Now unblocked: both partner and ExternalTargetBehavior are set.
                AutomationElement btnNuevoProducto3 = AppDriver.WaitForElement(window, "BtnNuevoProducto", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("BtnNuevoProducto never reappeared after picking an ExternalTargetBehavior.");
                bool openedWithBothSet = RoleplaySupport.ClickAndRaceOutcome(window, btnNuevoProducto3, "SearchBar", "Ok");
                Assert.True(openedWithBothSet, $"Product picker should open once partner and ExternalTargetBehavior are both set. Diagnostics: {app.WriteDiagnostics()}");

                // --- 5.3: pick a product and attach it directly (no pedido line) ------------
                AutomationElement productSearchBar = AppDriver.WaitForElement(window, "SearchBar", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("ProductSelectView's SearchBar never appeared.");
                // Broad single-letter search - this batch has no fixed test product the way
                // "Claro Cervantes" is a fixed test partner, so this picks whatever the target
                // catalog's first match is. May need a narrower term once run against the real
                // dev DB (task 7.1).
                RoleplaySupport.Search(productSearchBar, "a");

                AutomationElement productRow = AppDriver.WaitForElement(window, "selectedResult", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException($"No ProductSelectView results for search 'a'. Diagnostics: {app.WriteDiagnostics()}");
                AppDriver.Click(productRow);
                RoleplaySupport.ConfirmSelectionDialog(window);

                AutomationElement quantityEntry = AppDriver.WaitForElement(window, "QuantityEntry", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("PickQuantityPopUp's QuantityEntry never appeared.");
                AppDriver.TypeText(quantityEntry, "10");
                AutomationElement confirmQuantity = AppDriver.WaitForElement(window, "PickQuantity_btnConfirm", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("PickQuantityPopUp's confirm button never appeared.");
                AppDriver.Click(confirmQuantity);

                AutomationElement? detailsCollection = AppDriver.WaitForElement(window, "CollectionViewWeightDetails", RoleplaySupport.DefaultWait);
                Assert.True(detailsCollection is not null && detailsCollection.FindAllChildren().Length > 0,
                    $"WeightEntry should have at least one WeightDetail after attaching a product. Diagnostics: {app.WriteDiagnostics()}");
            }

            // --- 5.4: Dispatching Operator completes the two-step product capture ----------
            using (AppDriver app = AppDriver.Launch())
            {
                Window window = RoleplaySupport.GetMainWindow(app);
                RoleplaySupport.LoginAs(app, window, BotUserTemplates.ForRole(Role.DispatchingOperator), _fixture.RolePassword);

                AutomationElement row = AppDriver.WaitForElement(window, "PendingWeightsCollectionView", RoleplaySupport.DefaultWait) is null
                    ? throw new InvalidOperationException("PendingWeightsView never appeared for Dispatching Operator.")
                    : (RoleplaySupport.FindRowContaining(window, "BtnSeleccionar", plate)
                        ?? throw new InvalidOperationException($"Entry {plate} not found for Dispatching Operator. Diagnostics: {app.WriteDiagnostics()}"));
                AppDriver.Click(row);

                AutomationElement detailsCollectionElement = AppDriver.WaitForElement(window, "CollectionViewWeightDetails", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException($"DetailedWeightView never appeared. Diagnostics: {app.WriteDiagnostics()}");
                // No per-row AutomationId exists on CollectionViewWeightDetails' own row
                // template (only the collection itself and its "mark as loaded" button are
                // instrumented — tasks.md 2.5) - the single attached detail is its first child.
                AutomationElement[] detailRows = detailsCollectionElement.FindAllChildren();
                Assert.True(detailRows.Length > 0, "Expected the attached WeightDetail row to be present.");
                AppDriver.Click(detailRows[0]);

                AutomationElement btnSetTaraInicial = AppDriver.WaitForElement(window, "BtnSetTaraInicial", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException($"BtnSetTaraInicial never appeared (SecondaryTare should be unset). Diagnostics: {app.WriteDiagnostics()}");
                AppDriver.Click(btnSetTaraInicial);
                RoleplaySupport.TryClickNativeDialogButton(window, "OK"); // "Tara Establecida" confirmation

                AutomationElement? btnSetTaraInicialAfter = AppDriver.FindByAutomationId(window, "BtnSetTaraInicial");
                Assert.True(RoleplaySupport.IsHiddenOrAbsent(btnSetTaraInicialAfter), "BtnSetTaraInicial should be gone once the tare is set.");

                AutomationElement btnCaptureFinalWeight = AppDriver.WaitForElement(window, "BtnCaptureNewWeight", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException("BtnCaptureNewWeight never appeared after setting the tare.");
                AppDriver.Click(btnCaptureFinalWeight);

                AutomationElement markAsLoaded = AppDriver.WaitForElement(window, "BtnMarkAsLoaded", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException($"BtnMarkAsLoaded never appeared once tare and weight were both captured. Diagnostics: {app.WriteDiagnostics()}");
                AppDriver.Click(markAsLoaded);

                AutomationElement? markAsLoadedAfter = AppDriver.FindByAutomationId(window, "BtnMarkAsLoaded");
                Assert.True(RoleplaySupport.IsHiddenOrAbsent(markAsLoadedAfter),
                    $"Mark-as-loaded control should disappear once the detail's IsLoaded becomes true. Diagnostics: {app.WriteDiagnostics()}");
            }

            // --- 5.5: Main-mode role concludes the entry ------------------------------------
            using (AppDriver app = AppDriver.Launch())
            {
                Window window = RoleplaySupport.GetMainWindow(app);
                RoleplaySupport.LoginAs(app, window, BotUserTemplates.ForRole(Role.Admin), _fixture.RolePassword);

                AutomationElement row = AppDriver.WaitForElement(window, "PendingWeightsCollectionView", RoleplaySupport.DefaultWait) is null
                    ? throw new InvalidOperationException("PendingWeightsView never appeared.")
                    : (RoleplaySupport.FindRowContaining(window, "BtnSeleccionar", plate)
                        ?? throw new InvalidOperationException($"Entry {plate} not found before conclusion. Diagnostics: {app.WriteDiagnostics()}"));
                AppDriver.Click(row);

                AutomationElement btnFinishWeight = AppDriver.WaitForElement(window, "BtnFinishWeight", RoleplaySupport.DefaultWait)
                    ?? throw new InvalidOperationException($"BtnFinishWeight never became visible once every detail was loaded. Diagnostics: {app.WriteDiagnostics()}");
                AppDriver.Click(btnFinishWeight);
                RoleplaySupport.TryClickNativeDialogButton(window, "OK"); // "Proceso de pesaje concluido" confirmation

                AutomationElement? stillPending = AppDriver.WaitForElement(window, "PendingWeightsCollectionView", RoleplaySupport.DefaultWait);
                Assert.NotNull(stillPending); // back on PendingWeightsView

                AutomationElement? concludedRow = RoleplaySupport.FindRowContaining(window, "BtnSeleccionar", plate);
                Assert.True(concludedRow is null,
                    $"Concluded entry {plate} should no longer appear in PendingWeightsView. Diagnostics: {app.WriteDiagnostics()}");
            }
        }
    }
}
