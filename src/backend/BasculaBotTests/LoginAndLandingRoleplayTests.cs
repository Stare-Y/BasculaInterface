using FlaUI.Core.AutomationElements;
using Core.Domain.Entities.Identity;

namespace BasculaBotTests
{
    /// <summary>Group 4: login + role-derived landing screen roleplays (tasks 4.1-4.4).</summary>
    [Collection("BotSuite")]
    public class LoginAndLandingRoleplayTests : IDisposable
    {
        private readonly BotUserProvisioningFixture _fixture;
        private readonly AppDriver _app = AppDriver.Launch();

        public LoginAndLandingRoleplayTests(BotUserProvisioningFixture fixture)
        {
            _fixture = fixture;
        }

        private static bool SafeEnabled(AutomationElement? e) { try { return e is not null && e.IsEnabled; } catch { return false; } }

        [Theory]
        [InlineData(Role.Operator)]
        [InlineData(Role.Supervisor)]
        [InlineData(Role.Admin)]
        [InlineData(Role.Sudo)]
        public void Role_lands_on_PendingWeightsView_with_main_mode_buttons(Role role)
        {
            Window window = RoleplaySupport.GetMainWindow(_app);
            BotUserTemplate template = BotUserTemplates.ForRole(role);
            RoleplaySupport.LoginAs(_app, window, template, _fixture.RolePassword);

            AutomationElement? btnNewWeighProcess = AppDriver.WaitForElement(window, "BtnNewWeighProcess", RoleplaySupport.DefaultWait);
            AutomationElement? btnFinished = AppDriver.WaitForElement(window, "BtnFinished", RoleplaySupport.DefaultWait);

            Assert.True(RoleplaySupport.IsVisible(btnNewWeighProcess), $"BtnNewWeighProcess should be visible for {role}. Diagnostics: {_app.WriteDiagnostics()}");
            Assert.True(SafeEnabled(btnNewWeighProcess), $"BtnNewWeighProcess should be enabled for {role}.");
            Assert.True(RoleplaySupport.IsVisible(btnFinished), $"BtnFinished should be visible for {role}. Diagnostics: {_app.WriteDiagnostics()}");
            Assert.True(SafeEnabled(btnFinished), $"BtnFinished should be enabled for {role}.");
        }

        [Fact]
        public void DispatchingOperator_lands_with_main_mode_buttons_hidden()
        {
            Window window = RoleplaySupport.GetMainWindow(_app);
            BotUserTemplate template = BotUserTemplates.ForRole(Role.DispatchingOperator);
            RoleplaySupport.LoginAs(_app, window, template, _fixture.RolePassword);

            AutomationElement? btnNewWeighProcess = AppDriver.FindByAutomationId(window, "BtnNewWeighProcess");
            AutomationElement? btnFinished = AppDriver.FindByAutomationId(window, "BtnFinished");

            Assert.True(RoleplaySupport.IsHiddenOrAbsent(btnNewWeighProcess), $"BtnNewWeighProcess should be hidden for Dispatching Operator. Diagnostics: {_app.WriteDiagnostics()}");
            Assert.True(RoleplaySupport.IsHiddenOrAbsent(btnFinished), $"BtnFinished should be hidden for Dispatching Operator.");
        }

        [Fact]
        public void CustomerService_lands_with_weightless_pedido_button_swapped_in()
        {
            Window window = RoleplaySupport.GetMainWindow(_app);
            BotUserTemplate template = BotUserTemplates.ForRole(Role.CustomerService);
            RoleplaySupport.LoginAs(_app, window, template, _fixture.RolePassword);

            AutomationElement? btnNewWeightLessPedido = AppDriver.WaitForElement(window, "BtnNewWeightLessPedido", RoleplaySupport.DefaultWait);
            AutomationElement? btnNewWeighProcess = AppDriver.FindByAutomationId(window, "BtnNewWeighProcess");

            Assert.True(RoleplaySupport.IsVisible(btnNewWeightLessPedido), $"BtnNewWeightLessPedido should be visible for Customer Service. Diagnostics: {_app.WriteDiagnostics()}");
            Assert.True(RoleplaySupport.IsHiddenOrAbsent(btnNewWeighProcess), "BtnNewWeighProcess should be hidden for Customer Service.");
        }

        [Fact]
        public void MainMode_role_navigates_to_FinishedWeights_via_BtnFinished()
        {
            Window window = RoleplaySupport.GetMainWindow(_app);
            BotUserTemplate template = BotUserTemplates.ForRole(Role.Admin);
            RoleplaySupport.LoginAs(_app, window, template, _fixture.RolePassword);

            AutomationElement btnFinished = AppDriver.WaitForElement(window, "BtnFinished", RoleplaySupport.DefaultWait)
                ?? throw new InvalidOperationException($"BtnFinished never appeared. Diagnostics: {_app.WriteDiagnostics()}");
            AppDriver.Click(btnFinished);

            // FinishedWeights' "Volver" button is the only BtnExit carrying an AutomationId in
            // this batch (PendingWeightsView's own "Salir" button intentionally wasn't
            // instrumented — see tasks.md 2.2/2.3) - its presence is what tells the two screens
            // apart, since both reuse the same "PendingWeightsCollectionView" id.
            AutomationElement? btnExit = AppDriver.WaitForElement(window, "BtnExit", RoleplaySupport.DefaultWait);
            Assert.True(RoleplaySupport.IsVisible(btnExit), $"Should have navigated to FinishedWeights. Diagnostics: {_app.WriteDiagnostics()}");

            AutomationElement? collection = AppDriver.FindByAutomationId(window, "PendingWeightsCollectionView");
            Assert.NotNull(collection);
            // Assumes the target dev DB already has at least one concluded WeightEntry - this
            // suite doesn't create/conclude one itself before this check.
            Assert.True(collection!.FindAllChildren().Length > 0,
                $"FinishedWeights' collection view should be populated. Diagnostics: {_app.WriteDiagnostics()}");
        }

        public void Dispose() => _app.Dispose();
    }
}
