namespace Elmish.Uno.Samples.SubModelSelectedItemCascading;

using Microsoft.UI.Xaml.Controls;
using Elmish.Uno;
using ElmishProgram = Elmish.Uno.Samples.SubModelSelectedItemCascading.Program;

public partial class SubModelSelectedItemPage : Page
{
    public SubModelSelectedItemPage()
    {
        InitializeComponent();
        this.DataContext = new ElmishProgram.SubModelSelectedItemCascadingViewModel(this.DispatcherQueue);
    }
}
