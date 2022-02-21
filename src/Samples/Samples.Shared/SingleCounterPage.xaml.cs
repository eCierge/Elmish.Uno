using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using ElmishProgram = Elmish.Uno.Samples.SingleCounter.Program;

namespace Elmish.Uno.Samples.SingleCounter
{
    public partial class SingleCounterPage : Page
    {
        public SingleCounterPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Contract.Assume(e != null);

            var parameters = e.Parameter as IReadOnlyDictionary<string, object>;
            var count = Convert.ToInt32(parameters?["count"], CultureInfo.InvariantCulture);
            ViewModel.StartLoop(ElmishProgram.Config, this, Elmish.ProgramModule.runWith, ElmishProgram.Program, count);
        }
    }
}
