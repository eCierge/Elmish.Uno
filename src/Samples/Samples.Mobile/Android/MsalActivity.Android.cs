using Android.App;
using Android.Content;

using Microsoft.Identity.Client;

namespace SuperCharge.Client
{
    [Activity(Label = "MsalActivity", Exported = true)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
        DataHost = Constants.AppHost,
        DataScheme = Constants.AppScheme)]
    public class MsalActivity : BrowserTabActivity { }
}
