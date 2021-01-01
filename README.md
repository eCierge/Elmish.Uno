WinUI/Uno Platfrom done the Elmish Way
=======================

<img src="https://raw.githubusercontent.com/xperiandri/Elmish.Uno/master/logo/elmish-uno-logo-ghreadme.png" width="300" align="right" />

[![NuGet version](https://img.shields.io/nuget/v/Elmish.Uno.svg)](https://www.nuget.org/packages/Elmish.Uno) [![NuGet downloads](https://img.shields.io/nuget/dt/Elmish.Uno.svg)](https://www.nuget.org/packages/Elmish.Uno) [![Build status](https://github.com/xperiandri/Elmish.Uno/actions/workflows/continuous_integration.yml/badge.svg)](https://github.com/xperiandri/Elmish.Uno/actions/workflows/continuous_integration.yml)

**The good parts of MVVM (the data bindings) with the simplicity and robustness of an MVU architecture for the rest of your app. Never write an overly-complex ViewModel class again!**

### Elevator pitch

Elmish.Uno is a **production-ready** library that allows you to write Uno Platform apps with the robust, simple, well-known, and battle-tested MVU architecture, while still allowing you to use all your XAML knowledge and tooling to create UIs.

Some benefits of MVU you’ll get with Elmish.Uno include:

* Simple-to-understand, unidirectional data flow
* Single source of truth for all the state in your app
* Simple async/IO
* Immutable data
* Pure functions
* Great testability
* Simple optimization
* 78% more rockets 🚀

Even with static views, your central model/update code can follow an idiomatic Elmish/MVU architecture. You could, if you wanted, use the same model/update code to implement an app using a dynamic UI library such as [Fabulous](https://github.com/fsprojects/Fabulous) or [Fable.React](https://github.com/fable-compiler/fable-react), by just rewriting the “U” part of MVU.

**Static XAML views is a feature, not a limitation. See the FAQ for several unique benefits to this approach!**

Elmish.Uno uses [Elmish](https://github.com/elmish/elmish), an F# implementation of the MVU message loop.

Big thanks to [@MrMattSim](https://github.com/MrMattSim) for the wonderful logo!

Recommended resources
---------------------

* The [Elmish.Uno tutorial](https://github.com/xperiandri/Elmish.Uno/blob/master/TUTORIAL.md) explains how to use Elmish.Uno, starting with general Elmish/MVU concepts and ending with complex optimizations.
* The [Elmish.Uno binding reference](https://github.com/xperiandri/Elmish.Uno/blob/master/REFERENCE.md) explains Elmish.Uno's bindings and library functions for modifying bindings.
* The [Elmish docs site](https://elmish.github.io/elmish) also explains the general MVU architecture and principles.
* The [Elmish.Uno samples](https://github.com/xperiandri/Elmish.Uno/tree/master/src/Samples) provide many concrete usage examples.
* Blog posts:
   * [Getting Elmish in .NET with Elmish.Uno](https://medium.com/swlh/getting-elmish-in-net-with-elmish-uno-cd44e3eddc27) ("getting started" guide by Matt Eland)
* Elm resources may also provide some guidance, but note that not everything is relevant. A significant difference between “normal” Elm architecture and Elmish.Uno is that in Elmish.Uno, the views are statically defined using XAML, and the “view” function does not render views, but set up bindings. See the [tutorial](https://github.com/elmish/Elmish.Uno/blob/master/TUTORIAL.md) for details.
  * [Official Elm guide](https://guide.elm-lang.org)
  * Two talks: [Summarising Elm scaling strategy](https://dev.to/elmupdate/summarising-elm-scaling-strategy-1bjn)
  * Reddit: [Resources regarding scaling Elm apps](https://www.reddit.com/r/elm/comments/65s0g4/resources_regarding_scaling_elm_apps/)
  * Reddit: [How to structure Elm with multiple models](https://www.reddit.com/r/elm/comments/5jd2xn/how_to_structure_elm_with_multiple_models/dbuu0m4/)
  * Reddit: [Elm Architecture with a Redux-like store pattern](https://www.reddit.com/r/elm/comments/5xdl9z/elm_architecture_with_a_reduxlike_store_pattern/)

Getting started with Elmish.Uno
-------------------------------

See the [SingleCounter](https://github.com/elmish/Elmish.Uno/tree/master/src/Samples) sample for a very simple app. The central points are (assuming up-to-date VS2019):

1. Create an F# Class Library. If targeting .NET 5 or .NET Core, the project file should look like this:

   ```fsproj
   <Project Sdk="Microsoft.NET.Sdk">

     <PropertyGroup>
       <TargetFramework>net8.0-windows10.0.xxxxx</TargetFramework>  <!-- Or another target framework -->
     </PropertyGroup>

     <!-- other stuff -->
   ```

   If targeting .NET Framework (4.6.1 or later), replace the first line with

   ```fsproj
   <Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
   ```

2. Add NuGet reference to package `Elmish.Uno`.

3. Define the model that describes your app’s state and a function that initializes it:

   ```F#
   type Model =
     { Count: int
       StepSize: int }

   let init () =
     { Count = 0
       StepSize = 1 }
   ```

4. Define the various messages that can change your model:

   ```F#
   type Msg =
     | Increment
     | Decrement
     | SetStepSize of int
   ```

5. Define an `update` function that takes a message and a model and returns an updated model:

   ```F#
   let update msg m =
     match msg with
     | Increment -> { m with Count = m.Count + m.StepSize }
     | Decrement -> { m with Count = m.Count - m.StepSize }
     | SetStepSize x -> { m with StepSize = x }
   ```

6. Define the “view” function using the `Bindings` module. This is the central public API of Elmish.Uno.

   Normally in Elm/Elmish this function is called `view` and would take a model and a dispatch function (to dispatch new messages to the update loop) and return the UI (e.g. a HTML DOM to be rendered), but in Elmish.Uno this function is in general only run once and simply sets up bindings that XAML-defined views can use. Therefore, let’s call it `bindings` instead of `view`.

   ```F#
   open Elmish.Uno
   
   let bindings () =
     [
       "CounterValue" |> Binding.oneWay (fun m -> m.Count)
       "Increment" |> Binding.cmd (fun m -> Increment)
       "Decrement" |> Binding.cmd (fun m -> Decrement)
       "StepSize" |> Binding.twoWay(
         (fun m -> float m.StepSize),
         (fun newVal m -> int newVal |> SetStepSize))
     ]
   ```

   The strings identify the binding names to be used in the XAML views. The [Binding module](https://github.com/elmish/Elmish.Uno/blob/master/src/Elmish.Uno/Binding.fs) has many functions to create various types of bindings.


   Alternatively, use statically-typed view models in order to get better IDE support in the XAML.

   ```f#
   open Elmish.Uno

   type CounterViewModel(args) =
     inherit ViewModelBase<Model, Msg>(args)

     member _.CounterValue = base.Get() (Binding.OneWayT.id >> Binding.mapModel (fun m -> m.Count))
     member _.Increment = base.Get() (Binding.CmdT.setAlways Counter.Increment)
     member _.Decrement = base.Get() (Binding.CmdT.setAlways Counter.Decrement)
     member _.StepSize
       with get() = base.Get() (Binding.OneWayT.id >> Binding.mapModel (fun m -> m.StepSize))
       and set(v) = base.Set(v) (Binding.OneWayToSourceT.id >> Binding.mapMsg Counter.Msg.SetStepSize)
   ```

7. Create a function that accepts the app’s main window (to be created) and configures and starts the Elmish loop for the window with your `init`, `update` and `bindings`:

   ```F#
   open Elmish.Uno

   let main window =
     Program.mkSimple init update bindings
     |> Program.runElmishLoop window
   ```

   Alternatively, use a statically-typed view model at the top level.

   ```F#
   open Elmish.Uno

   let main window =
     Program.mkSimpleT init update CounterViewModel
     |> Program.runElmishLoop window
   ```

   In the code above, `Program.runElmishLoop` will set the window’s `DataContext` to the specified bindings and start the Elmish dispatch loop for the window.

8. Create a Uno Platform app project (using the Visual Studio template called `Uno App (.NET)`). This will be your entry point and contain the XAML views. Add a reference to the F# project, and make the following changes in the `csproj` file:

   * Currently, the core Elmish logs are only output to the console. If you want a console window for displaying Elmish logs, change `<OutputType>WinExe</OutputType>` to `<OutputType>Exe</OutputType>` and add `<DisableWinExeOutputInference>true</DisableWinExeOutputInference>`.
   * If the project file starts with the now legacy `<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">`, change it to `<Project Sdk="Microsoft.NET.Sdk">`
   * Change the target framework to match the one used in the F# project (e.g. `net5.0-windows`).

   Make the following changes to `App.xaml.cs` to initialize Elmish when the app starts:

   ```c#
   public partial class App : Application
   {
     public App()
     {
       this.Activated += StartElmish;
     }

     private void StartElmish(object sender, EventArgs e)
     {
       this.Activated -= StartElmish;
       Program.main(MainWindow);
     }

   }
   ```

9. Define your views and bindings in XAML:

   ```xaml
   <Window
       x:Class="MyNamespace.MainWindow"
       xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
     <StackPanel Orientation="Horizontal">
       <TextBlock Text="{Binding CounterValue}" />
       <Button Command="{Binding Decrement}" Content="-" />
       <Button Command="{Binding Increment}" Content="+" />
       <TextBlock Text="{Binding StepSize}" />
       <Slider Value="{Binding StepSize}" TickFrequency="1" Minimum="1" Maximum="10" />
     </StackPanel>
   </Window>
   ```

10. Profit! :)

Further resources:

* The [Elmish.Uno tutorial](https://github.com/elmish/Elmish.Uno/blob/master/TUTORIAL.md) provides information on general MVU/Elmish concepts and how they apply to Elmish.Uno, as well as the various Elmish.Uno bindings.
* The [samples](https://github.com/elmish/Elmish.Uno/tree/master/src/Samples) are complete, working mini-apps demonstrating selected aspects of Elmish.Uno.
* If you'd like to contribute, please read and follow the [Contributor guidelines](https://github.com/elmish/Elmish.Uno/blob/master/.github/CONTRIBUTING.md).

FAQ
---

#### Static views in MVU? Isn’t that just a half-baked solution that only exists due to a lack of better alternatives?

Not at all! 🙂

It’s true that static views aren’t as composable as dynamic views. It’s also true that at the time of writing, there are no solid, production-ready dynamic UI libraries for Uno Platform (though there are no lack of half-finished attempts or proof-of-concepts: [Elmish.Uno.Dynamic](https://github.com/cmeeren/Elmish.Uno.Dynamic), [Fabulous.WPF](https://github.com/TimLariviere/Fabulous.WPF), [Skylight](https://github.com/gerardtoconnor/Skylight), [Uil](https://github.com/elmish/Uil)). Heck, it’s even true that Elmish.Uno was originally created with static views due to the difficulty of creating a dynamic UI library, as described in [issue #1](https://github.com/elmish/Elmish.Uno/issues/1).

However, Elmish.Uno’s static-view-based solution has several unique benefits:

- You can use your existing XAML and MVVM knowledge (that is, the best part of MVVM – the UI bindings – without having to deal with `NavigationService`s, `ViewModelLocator`s, state synchronization, `INotifyPropertyChanged`, etc.)
- Huge mindshare – there are tons of relevant XAML and MVVM resources on the net which can help with the UI and data binding part if you get stuck
- Automatic support for all 3rd party WPF UI libraries like [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit), since it just uses XAML and bindings (support for 3rd party libraries is commonly a major pain point for dynamic UI solutions)
- You can use the XAML designer (including design-time data binding)
- Automatically puts all the power of WPF at your fingertips, whereas dynamic UI solutions have [inherent limitations](https://github.com/cmeeren/Elmish.WPF.Dynamic/tree/e9f04b6e330754f045df093368fa4917c892399d#current-limitations) that are not easy to work around

In short, for WPF apps, a solution based on static XAML views is currently the way to go.

#### Do I have to use the project structure outlined above?

Not at all. The above example, as well as the samples, keep all non-UI code in a single project for simplicity, and all the XAML in a C# project for better tooling.

An alternative with a clearer separation of UI and core logic can be implemented by splitting the F# project into two projects:

* A core library containing the model definitions and `update` functions.
  * This library can include a reference to Elmish (e.g. for the `Cmd` module helpers), but not to Elmish.Uno, which depends on Uno and has a UI-centered API (specifying bindings). This will ensure your core logic (such as the `update` function) is free from any UI concerns, and allow you to re-use the core library should you want to port your app to another Elmish-based solution (e.g. Fable.React).
* An Elmish.Uno project that contains the `bindings` (or `view`) function and the call to `Program.runElmishLoop`.
  * This project would reference the core library and `Elmish.Uno`.

Another alternative is to turn the sample code on its head and have the F# project be a console app containing your entry point (with a call to `Program.runWindow`) and referencing the C#/XAML project (instead of the other way around, as demonstrated above).

In general, you have a large amount of freedom in how you structure your solution and what kind of entry point you use.

#### How can I test commands? What is the CmdMsg pattern?

Since the commands (`Cmd<Msg>`) returned by `init` and `update` are lists of functions, they are not particularly testable. A general pattern to get around this is to replace the commands with pure data that are transformed to the actual commands elsewhere:

* Create a `CmdMsg` union type with cases for each command you want to execute in the app.
* Make `init` and `update` return `model * CmdMsg list` instead of `model * Cmd<Msg>`. Since `init` and `update` now return data, they are much easier to test.
* Create a trivial/too-boring-to-test `cmdMsgToCmd` function that transforms a `CmdMsg` to the corresponding `Cmd`.
* Finally, create “normal” versions of `init` and `update` that you can use when creating `Program`. Elmish.Uno provides `Program.mkProgramUnoWithCmdMsg` that does this for you (but there’s no magic going on – it’s really easy to do yourself).

The [FileDialogsCmdMsg sample](https://github.com/elmish/Elmish.Uno/tree/master/src/Samples) demonstrates this approach. For more information, see the [Fabulous documentation](https://fsprojects.github.io/Fabulous/Fabulous.XamarinForms/update.html#replacing-commands-with-command-messages-for-better-testability). For reference, here is [the discussion that led to this pattern](https://github.com/fsprojects/Fabulous/pull/320#issuecomment-491522737).

#### Can I use design-time view models?

Yes. Assuming you have a C# XAML and entry point project referencing the F# project, simply use `ViewModel.designInstance` (e.g. in the F# project) to create a view model instance that your XAML can use at design-time:

```F#
module MyAssembly.DesignViewModels
let myVm = ViewModel.designInstance myModel myBindings
```

Then use the following attributes wherever you need a design-time VM:

```XAML
<Window
    ...
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:vm="clr-namespace:MyAssembly;assembly=MyAssembly"
    mc:Ignorable="d"
    d:DataContext="{x:Static vm:DesignViewModels.myVm}">
```

When targeting legacy .NET Framework, “Project code” must be enabled in the XAML designer for this to work.

If you are using static view models, make sure that the View Model type is in a namespace and add a default constructor that passes a model into `ViewModelArgs.simple`:

```F#
namespace ViewModels

type [<AllowNullLiteral>] AppViewModel (args) =
  inherit ViewModelBase<AppModel, AppMsg>(args)
  
  new() = AppViewModel(App.init () |> ViewModelArgs.simple)
```

Then use the following attributes just like you would in a normal C# MVVM project:

```XAML
<Window
    ...
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:vm="clr-namespace:ViewModels.SubModelStatic;assembly=MyAssembly"
    mc:Ignorable="d"
    d:DataContext="{d:DesignInstance Type=vm:AppViewModel, IsDesignTimeCreatable=True}">
```

##### .NET Core 3 workaround

When targeting .NET Core 3, a bug in the XAML designer causes design-time data to not be displayed through `DataContext` bindings. See [this issue](https://developercommunity.visualstudio.com/content/problem/1133390/design-time-data-in-datacontext-binding-not-displa.html) for details. One workaround is to add a `d:DataContext` binding alongside your normal `DataContext` binding. Another workaround is to change

```xaml
<local:MyControl DataContext="{Binding Child}" />
```

to

```xaml
<local:MyControl
  DataContext="{Binding Child}"
  d:DataContext="{Binding DataContext.Child,
                          RelativeSource={RelativeSource AncestorType=T}}" />
```

where `T` is the type of the parent object that contains `local:MyControl` (or a more distant ancestor, though there are issues with using `Window` as the type).

#### Can I open new windows/dialogs?

Sure! Just use `Binding.subModelWin`. It works like `Binding.subModel`, but has a `WindowState` wrapper around the returned model to control whether the window is closed, hidden, or visible. You can use both modal and non-modal windows/dialogs, and everything is a part of the Elmish core loop. Check out the [NewWindow sample](https://github.com/elmish/Elmish.Uno/tree/master/src/Samples).

Note that if you use `App.xaml` startup, you may want to set `ShutdownMode="OnMainWindowClose"` in `App.xaml` if that’s the desired behavior.

#### Can I bind to events and use behaviors?

Sure! Check out the [EventBindingsAndBehaviors sample](https://github.com/elmish/Elmish.Uno/tree/master/src/Samples). Note that you have to install the NuGet package `Microsoft.Xaml.Behaviors.WinUI`.

#### How can I control logging?

Elmish.Uno uses `Microsoft.Extensions.Logging`. To see Elmish.Uno output in your favorite logging framework, use `UnoProgram.withLogger` to pass an `ILoggerFactory`:

```f#
UnoProgram.mkSimple init update bindings
|> UnoProgram.withLogger yourLoggerFactory
|> UnoProgram.runWindow window
```

For example, in Serilog, you need to install Serilog.Extensions.Logging and instantiate `SerilogLoggerFactory`. The samples demonstrate this.

Elmish.Uno logs to these categories:

* `Elmish.Uno.Update`: Logs exceptions (Error level) and messages/models (Trace/Verbose level) during `update`.
* `Elmish.Uno.Bindings`: Logs events related to bindings. Some logging is done at the Error level (e.g. developer errors such as duplicated binding names, using non-existent bindings in XAML, etc.), but otherwise it’s generally just Trace/Verbose for when you really want to see everything that’s happening (triggering `PropertyChanged`, Uno Platform getting/setting bindings, etc.)
* `Elmish.Uno.Performance`: Logs the performance of the functions you pass when creating bindings (`get`, `set`, `map`, `equals`, etc.) at the Trace/Verbose level. Use `UnoProgram.withPerformanceLogThreshold` to set the minimum duration to log.

The specific method of controlling what Elmish.Uno logs depends on your logging framework. For Serilog you can use `.MinimumLevel.Override(...)` to specify the minimum log level per category, like this:

```f#
myLoggerConfiguration
  .MinimumLevel.Override("Elmish.Uno.Bindings", LogEventLevel.Verbose)
  ...
```
