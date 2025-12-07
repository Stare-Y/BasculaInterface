using BasculaInterface.Views.PopUps;

namespace BasculaInterface.Views;

public partial class EditSettingsView : ContentPage
{
	public EditSettingsView()
	{
		InitializeComponent();

        //LoadPreferences();
	}

    private void CheckBoxSecondaryTerminal_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("SecondaryTerminal", e.Value);
    }

    private void CheckBoxManualWeight_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("ManualWeight", e.Value);
    }

    private void CheckBoxRequirePartner_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("RequirePartner", e.Value);
    }

    private void CheckBoxOnlyPedidos_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("OnlyPedidos", e.Value);
    }

    private void CheckBoxBypasTurn_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("BypasTurn", e.Value);
    }

    //private void LoadPreferences()
    //{
    //    CheckBoxSecondaryTerminal.IsChecked = Preferences.Get("SecondaryTerminal", false);
    //    CheckBoxManualWeight.IsChecked = Preferences.Get("ManualWeight", false);
    //    CheckBoxRequirePartner.IsChecked = Preferences.Get("RequirePartner", false);
    //    CheckBoxOnlyPedidos.IsChecked = Preferences.Get("OnlyPedidos", false);
    //    CheckBoxBypasTurn.IsChecked = Preferences.Get("BypasTurn", false);
    //    EntryHost.Text = Preferences.Get("HostUrl", "bascula.cpe");

    //    //TODO: add this thing to settings lul
    //    Preferences.Set("FilterClasif6", true);
    //}

    //private void SetPreferences()
    //{
    //    Preferences.Set("SecondaryTerminal", CheckBoxSecondaryTerminal.IsChecked);
    //    Preferences.Set("ManualWeight", CheckBoxManualWeight.IsChecked);
    //    Preferences.Set("RequirePartner", CheckBoxRequirePartner.IsChecked);
    //    Preferences.Set("OnlyPedidos", CheckBoxOnlyPedidos.IsChecked);
    //    Preferences.Set("BypasTurn", CheckBoxBypasTurn.IsChecked);
    //}

    private async void BtnCancel_Clicked(object sender, EventArgs e)
    {
        await BtnCancel.ScaleTo(1.1, 100);
        await BtnCancel.ScaleTo(1.0, 100);

        await Shell.Current.Navigation.PopAsync();
    }

    private async void BtnSaveSettings_Clicked(object sender, EventArgs e)
    {
        await BtnSaveSettings.ScaleTo(1.1, 100);
        await BtnSaveSettings.ScaleTo(1.0, 100);

        //WaitPopUp.Show("Guardando configuración, espere");
        //try
        //{
        //    Preferences.Set("HostUrl", EntryHost.Text);
        //    SetPreferences();
        //}
        //catch (Exception ex)
        //{
        //    await DisplayAlert("Error", "Error al tratar de guardar la configuración: " + ex.Message, "OK");
        //}
        //finally
        //{
        //    WaitPopUp.Hide();
        //}

        //await Shell.Current.Navigation.PopAsync();
    }
}
