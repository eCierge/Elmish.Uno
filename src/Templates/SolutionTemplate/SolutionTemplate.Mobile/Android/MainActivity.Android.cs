namespace SolutionTemplate;

using Android.App;
using Android.Content;
using Android.Views;

using Microsoft.Identity.Client;

[Activity(
        MainLauncher = true,
        ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
        WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden
    )]
public class MainActivity : global::Windows.UI.Xaml.ApplicationActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
    }
}
