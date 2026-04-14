using BasculaInterface.Views.PopUps;

namespace BasculaInterface.Views;

public partial class EditSettingsView : ContentPage
{
	public EditSettingsView()
	{
		InitializeComponent();

        LoadPreferences();
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
    private void CheckBoxOnlyFinished_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("OnlyFinished", e.Value);
        if (e.Value)
        {
            //uncheck everything else
            CheckBoxSecondaryTerminal.IsChecked = false;
            CheckBoxManualWeight.IsChecked = false;
            CheckBoxRequirePartner.IsChecked = false;
            CheckBoxOnlyPedidos.IsChecked = false;
            CheckBoxBypasTurn.IsChecked = false;

            //disable everything else
            CheckBoxSecondaryTerminal.IsEnabled = false;
            CheckBoxManualWeight.IsEnabled = false;
            CheckBoxRequirePartner.IsEnabled = false;
            CheckBoxOnlyPedidos.IsEnabled = false;
            CheckBoxBypasTurn.IsEnabled = false;
        }
        else
        {
            //enable everything else
            CheckBoxSecondaryTerminal.IsEnabled = true;
            CheckBoxManualWeight.IsEnabled = true;
            CheckBoxRequirePartner.IsEnabled = true;
            CheckBoxOnlyPedidos.IsEnabled = true;
            CheckBoxBypasTurn.IsEnabled = true;
        }
    }

    private void LoadPreferences()
    {
        CheckBoxSecondaryTerminal.IsChecked = Preferences.Get("SecondaryTerminal", false);
        CheckBoxManualWeight.IsChecked = Preferences.Get("ManualWeight", false);
        CheckBoxRequirePartner.IsChecked = Preferences.Get("RequirePartner", false);
        CheckBoxOnlyPedidos.IsChecked = Preferences.Get("OnlyPedidos", false);
        CheckBoxBypasTurn.IsChecked = Preferences.Get("BypasTurn", false);
        CheckBoxOnlyFinished.IsChecked = Preferences.Get("OnlyFinished", false);
        CheckBoxShowDocumentTypes.IsChecked = Preferences.Get("ShowDocumentTypeFilter", false);
        EntryDocumentTypes.Text = Preferences.Get("PreferedDocumentType", string.Empty);
        EntryPurchaseExternalTarget.Text = Preferences.Get("PurchaseExternalTarget", string.Empty);
        CheckBoxFilterNull.IsChecked = Preferences.Get("FilterNull", false);
        EntryHost.Text = Preferences.Get("HostUrl", "bascula.cpe");

        // Load theme preference (0 = System, 1 = Light, 2 = Dark)
        PickerTheme.SelectedIndex = Preferences.Get("AppTheme", 0);

        //TODO: add this thing to settings lul
        Preferences.Set("FilterClasif6", true);
    }

    private void SetPreferences()
    {
        Preferences.Set("SecondaryTerminal", CheckBoxSecondaryTerminal.IsChecked);
        Preferences.Set("ManualWeight", CheckBoxManualWeight.IsChecked);
        Preferences.Set("RequirePartner", CheckBoxRequirePartner.IsChecked);
        Preferences.Set("OnlyPedidos", CheckBoxOnlyPedidos.IsChecked);
        Preferences.Set("BypasTurn", CheckBoxBypasTurn.IsChecked);
        Preferences.Set("OnlyFinished", CheckBoxOnlyFinished.IsChecked);
        Preferences.Set("ShowDocumentTypeFilter", CheckBoxShowDocumentTypes.IsChecked);
        Preferences.Set("PreferedDocumentType", EntryDocumentTypes.Text);
        Preferences.Set("PurchaseExternalTarget", EntryPurchaseExternalTarget.Text);
        Preferences.Set("FilterNull", CheckBoxFilterNull.IsChecked);
    }

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

        WaitPopUp.Show("Guardando configuraci�n, espere");
        try
        {
            if (EntryHost.Text.Contains("http://"))
            {
                Preferences.Set("HostUrl", EntryHost.Text);
            }
            else
            {
                Preferences.Set("HostUrl", "http://" + EntryHost.Text + "/");
            }

            if (EntryHost.Text == string.Empty)
            {
                await DisplayAlert("Error", "Por favor, ingrese la URL del host.", "OK");
                return;
            }

            SetPreferences();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al tratar de guardar la configuraci�n: " + ex.Message, "OK");
        }
        finally
        {
            WaitPopUp.Hide();
        }

        await Shell.Current.Navigation.PopAsync();
    }

    private void CheckBoxShowDocumentTypes_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("ShowDocumentTypeFilter", e.Value);
    }

    private void EntryDocumentTypes_TextChanged(object sender, TextChangedEventArgs e)
    {
        Entry entry = (Entry)sender;

        if (string.IsNullOrEmpty(entry.Text))
            return;

        if (!int.TryParse(entry.Text, out _))
        {
            entry.Text = e.OldTextValue;
            return;
        }

        Preferences.Set("PreferedDocumentType", entry.Text);
    }

    private void CheckBoxDontFilterNull_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("FilterNull", e.Value);
    }

    private void PickerTheme_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (PickerTheme.SelectedIndex < 0)
            return;

        Preferences.Set("AppTheme", PickerTheme.SelectedIndex);

        Application.Current!.UserAppTheme = PickerTheme.SelectedIndex switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.Unspecified // System default
        };
    }

    private void EntryPurchaseExternalTarget_TextChanged(object sender, TextChangedEventArgs e)
    {
        Entry entry = (Entry)sender;

        if (string.IsNullOrEmpty(entry.Text))
            return;

        if (!int.TryParse(entry.Text, out _))
        {
            entry.Text = e.OldTextValue;
            return;
        }

        Preferences.Set("PurchaseExternalTarget", entry.Text);
    }
}
