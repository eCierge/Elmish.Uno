namespace Elmish.Uno.Samples.TwoWaySeq;

using Microsoft.UI.Xaml.Controls;
using Elmish.Uno;
using ElmishProgram = Elmish.Uno.Samples.TwoWaySeq.Program;

public partial class TwoWaySeqPage : Page
{
    public TwoWaySeqPage()
    {
        InitializeComponent();
        DataContext = new ElmishProgram.ViewModel(DispatcherQueue);
        //UnoProgram.StartElmishLoop(this, ElmishProgram.Program);
    }
}
