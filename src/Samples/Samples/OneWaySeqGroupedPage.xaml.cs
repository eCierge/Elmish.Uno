namespace Elmish.Uno.Samples.OneWaySeqGrouped;

using Microsoft.UI.Xaml.Controls;
using Elmish.Uno;
using ElmishProgram = Elmish.Uno.Samples.OneWaySeqGrouped.Program;
using Microsoft.UI.Xaml.Data;

public partial class OneWaySeqGroupedPage : Page
{
    public OneWaySeqGroupedPage()
    {
        InitializeComponent();
        DataContext = new ElmishProgram.ViewModel(DispatcherQueue);
        //UnoProgram.StartElmishLoop(this, ElmishProgram.Program);
    }
}
