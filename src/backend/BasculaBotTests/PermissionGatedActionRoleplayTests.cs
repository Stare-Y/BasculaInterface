using FlaUI.Core.AutomationElements;
using Core.Domain.Entities.Identity;

namespace BasculaBotTests
{
    /// <summary>Group 6: permission-gated action roleplays — self-authorize gate (6.1-6.3) and
    /// manual weight capture (6.4-6.5).</summary>
    [Collection("BotSuite")]
    public class PermissionGatedActionRoleplayTests
    {
        private readonly BotUserProvisioningFixture _fixture;

        public PermissionGatedActionRoleplayTests(BotUserProvisioningFixture fixture)
        {
            _fixture = fixture;
        }

        private static string ReadText(AutomationElement element)
        {
            try
            {
                if (element.Patterns.Value.IsSupported)
                    return element.Patterns.Value.Pattern.Value.ValueOrDefault ?? string.Empty;
            }
            catch { }
            try { return element.Name ?? string.Empty; } catch { return string.Empty; }
        }

        /// <summary>
        /// Opens two app instances on the same VM: session A opens WeightingScreen and leaves it
        /// open — its keepalive loop holds the server-side per-device weight lock, and both
        /// instances share the same deviceId on this machine (BasculaViewModel.CanWeight() keys
        /// on DeviceInfo.Name), so this genuinely makes the scale report busy for session B.
        /// Session B then enables BypasTurn — the only in-app path to AuthorizeTurnBypassPopUp at
        /// all (see tasks.md 2.1/6.1's amendment: it can only be toggled on EditSettingsView,
        /// confirmed with the project owner) — and, logged in as <paramref name="actingRole"/>,
        /// attempts to start a new weighing while the lock is held, submitting its own
        /// credentials at the gate.
        /// </summary>
        private void RunSelfAuthorizeGateScenario(Role actingRole, bool expectAuthorized)
        {
            using AppDriver lockHolder = AppDriver.Launch();
            Window lockWindow = RoleplaySupport.GetMainWindow(lockHolder);
            RoleplaySupport.LoginAs(lockHolder, lockWindow, BotUserTemplates.ForRole(Role.Operator), _fixture.RolePassword);
            AutomationElement lockHolderBtn = AppDriver.WaitForElement(lockWindow, "BtnNewWeighProcess", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException("BtnNewWeighProcess never appeared for the lock-holding session.");
            AppDriver.Click(lockHolderBtn);
            AppDriver.WaitForElement(lockWindow, "EntryVehiclePlate", RoleplaySupport.DefaultWait);
            // Left open deliberately - its keepalive loop is what makes CanWeight() report busy
            // for the second session below.

            using AppDriver app = AppDriver.Launch();
            Window window = RoleplaySupport.GetMainWindow(app);

            AutomationElement btnSettings = AppDriver.WaitForElement(window, "BtnSettings", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException("MainPage's BtnSettings never appeared.");
            AppDriver.Click(btnSettings);
            AutomationElement checkBoxBypasTurnElement = AppDriver.WaitForElement(window, "CheckBoxBypasTurn", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException("EditSettingsView's CheckBoxBypasTurn never appeared.");
            CheckBox checkBoxBypasTurn = checkBoxBypasTurnElement.AsCheckBox();
            if (checkBoxBypasTurn.IsChecked != true)
                checkBoxBypasTurn.Toggle(); // sets Preferences["BypasTurn"] = true immediately (CheckedChanged)
            AutomationElement backButton = AppDriver.WaitForElement(window, "EditSettings_BtnCancel", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException("EditSettingsView's Cancel button never appeared.");
            AppDriver.Click(backButton);

            BotUserTemplate template = BotUserTemplates.ForRole(actingRole);
            RoleplaySupport.LoginAs(app, window, template, _fixture.RolePassword);

            AutomationElement btnNewWeighProcess = AppDriver.WaitForElement(window, "BtnNewWeighProcess", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException($"BtnNewWeighProcess never appeared for {actingRole}.");
            AppDriver.Click(btnNewWeighProcess);

            AutomationElement identifierEntry = AppDriver.WaitForElement(window, "AuthorizeTurnBypass_IdentifierEntry", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException(
                    $"Self-authorize gate never appeared for {actingRole} - the scale wasn't reported busy. Diagnostics: {app.WriteDiagnostics()}");
            AutomationElement passwordEntry = AppDriver.WaitForElement(window, "AuthorizeTurnBypass_PasswordEntry", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException("Self-authorize gate's password entry never appeared.");
            AutomationElement btnConfirm = AppDriver.WaitForElement(window, "btnConfirm", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException("Self-authorize gate's confirm button never appeared.");

            AppDriver.TypeText(identifierEntry, template.UserCode);
            AppDriver.TypeText(passwordEntry, _fixture.RolePassword);
            AppDriver.Click(btnConfirm);

            // The popup itself closes on confirm regardless of outcome (AuthorizeTurnBypassPopUp's
            // OnPopupAcceptClicked always resolves immediately) - the actual authorization result
            // shows up afterward, either as WeightingScreen opening (authorized) or an "Error"/
            // "Bascula ocupada" DisplayAlert (rejected).
            bool authorized = RoleplaySupport.RaceOutcome(window, "EntryVehiclePlate", "OK");
            Assert.Equal(expectAuthorized, authorized);
        }

        [Theory]
        [InlineData(Role.Supervisor)]
        [InlineData(Role.Sudo)]
        public void Role_with_gate_access_self_authorizes_successfully(Role role) =>
            RunSelfAuthorizeGateScenario(role, expectAuthorized: true);

        [Theory]
        [InlineData(Role.Operator)]
        [InlineData(Role.CustomerService)]
        public void Role_without_gate_access_is_rejected(Role role) =>
            RunSelfAuthorizeGateScenario(role, expectAuthorized: false);

        // --- 6.4 / 6.5: manual weight capture availability ---------------------------------

        [Theory]
        [InlineData(Role.Supervisor)]
        [InlineData(Role.Sudo)]
        public void Role_with_manual_capture_permission_can_type_a_manual_weight(Role role)
        {
            using AppDriver app = AppDriver.Launch();
            Window window = RoleplaySupport.GetMainWindow(app);
            RoleplaySupport.LoginAs(app, window, BotUserTemplates.ForRole(role), _fixture.RolePassword);

            AutomationElement btnNewWeighProcess = AppDriver.WaitForElement(window, "BtnNewWeighProcess", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException($"BtnNewWeighProcess never appeared for {role}.");
            AppDriver.Click(btnNewWeighProcess);
            RoleplaySupport.TryClickNativeDialogButton(window, "OK"); // dismiss a stale-lock error, if any, from a prior scenario

            AppDriver.WaitForElement(window, "EntryVehiclePlate", RoleplaySupport.DefaultWait);
            AutomationElement checkBox = AppDriver.WaitForElement(window, "CheckBoxUseManual", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException($"CheckBoxUseManual should be available for {role}. Diagnostics: {app.WriteDiagnostics()}");
            Assert.True(RoleplaySupport.IsVisible(checkBox));

            checkBox.AsCheckBox().Toggle();

            AutomationElement entryLabel = AppDriver.WaitForElement(window, "EntryLabel", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException("EntryLabel never appeared after enabling manual capture.");
            // A plain integer, not "123.45": WeightingScreen.PesoLabel_TextChanged double.TryParses
            // and reverts the WHOLE field on every keystroke that doesn't parse - a trailing bare
            // decimal point ("123.") mid-typing can fail that parse and corrupt character-by-
            // character input. Every prefix of "123" parses fine, sidestepping it entirely.
            AppDriver.TypeText(entryLabel, "123");

            Assert.Contains("123", ReadText(entryLabel));
        }

        // Customer Service is intentionally excluded here: their PedidosOnly flow never opens
        // WeightingScreen at all (BtnNewWeighProcess is hidden; products are attached via
        // PickQuantityPopUp, not weighed) - CheckBoxUseManual is structurally unreachable for
        // that role, not something a permission check hides. Operator is the meaningful case:
        // they DO reach WeightingScreen (Main-mode default) but lack CanCaptureWeightManually.
        [Fact]
        public void Operator_has_no_manual_capture_toggle()
        {
            using AppDriver app = AppDriver.Launch();
            Window window = RoleplaySupport.GetMainWindow(app);
            RoleplaySupport.LoginAs(app, window, BotUserTemplates.ForRole(Role.Operator), _fixture.RolePassword);

            AutomationElement btnNewWeighProcess = AppDriver.WaitForElement(window, "BtnNewWeighProcess", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException("BtnNewWeighProcess never appeared for Operator.");
            AppDriver.Click(btnNewWeighProcess);
            RoleplaySupport.TryClickNativeDialogButton(window, "OK");

            AppDriver.WaitForElement(window, "EntryVehiclePlate", RoleplaySupport.DefaultWait);
            AutomationElement? checkBox = AppDriver.FindByAutomationId(window, "CheckBoxUseManual");
            Assert.True(RoleplaySupport.IsHiddenOrAbsent(checkBox),
                $"CheckBoxUseManual should not be available for Operator. Diagnostics: {app.WriteDiagnostics()}");
        }
    }
}
