module Elmish.Uno.Samples.FileDialogs.Program

open System
open System.IO
open System.Threading
open System.Windows
open Serilog
open Serilog.Extensions.Logging
open Elmish
open Elmish.Uno

type Model =
  { CurrentTime: DateTimeOffset
    Text: string
    StatusMsg: string }


let initial =
  { CurrentTime = DateTimeOffset.Now
    Text = ""
    StatusMsg = "" }

let init () = initial, []

type Msg =
  | SetTime of DateTimeOffset
  | SetText of string
  | RequestSave
  | RequestLoad
  | SaveSuccess
  | LoadSuccess of string
  | SaveCanceled
  | LoadCanceled
  | SaveFailed of exn
  | LoadFailed of exn


let save text =
  //CoreApplication.GetCurrentView().DispatcherQueue.TryEnqueue(fun () ->
    let picker = new Windows.Storage.Pickers.FileSavePicker()
    let fileTypeChoices = picker.FileTypeChoices
    do fileTypeChoices.Add("Plain Text", [|".txt"|])
    do fileTypeChoices.Add("Markdown"  , [|".md" |])
    //let guiCtx = SynchronizationContext.Current
    async {
      //do! Async.SwitchToContext guiCtx
      let! file = picker.PickSaveFileAsync().AsTask()
      match file with
      | null -> return SaveCanceled
      | _ ->
        // Prevent updates to the remote version of the file until
        // we finish making changes and call CompleteUpdatesAsync.
        Windows.Storage.CachedFileManager.DeferUpdates(file)
        // write to file
        do! Windows.Storage.FileIO.WriteTextAsync(file, text).AsTask()
        // Let Windows know that we're finished changing the file so
        // the other app can update the remote version of the file.
        // Completing updates may require Windows to ask for user input.
        let! status = Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file).AsTask();
        if status = Windows.Storage.Provider.FileUpdateStatus.Complete
        then return SaveSuccess
        else return SaveFailed (Exception(status.ToString()))
    }
  //).AsTask().AsAsync()


let load () =
  //CoreApplication.GetCurrentView().DispatcherQueue.TryEnqueue(fun () ->
    let picker = new Windows.Storage.Pickers.FileOpenPicker()
    let fileTypeFilter = picker.FileTypeFilter
    do fileTypeFilter.Add(".txt")
    do fileTypeFilter.Add(".md")
    //let guiCtx = SynchronizationContext.Current
    async {
      //do! Async.SwitchToContext guiCtx
      let! file = picker.PickSingleFileAsync().AsTask()
      match file with
      | null -> return LoadCanceled
      | _ ->
        let! contents = Windows.Storage.FileIO.ReadTextAsync(file).AsTask()
        return LoadSuccess contents
    else return LoadCanceled
    }
  //).AsTask().AsAsync()

let update msg m =
  match msg with
  | SetTime t -> { m with CurrentTime = t }, Cmd.none
  | SetText s -> { m with Text = s}, Cmd.none
  | RequestSave -> m, Cmd.OfAsync.either save m.Text id SaveFailed
  | RequestLoad -> m, Cmd.OfAsync.either load () id LoadFailed
  | SaveSuccess -> { m with StatusMsg = sprintf "Successfully saved at %O" DateTimeOffset.Now }, Cmd.none
  | LoadSuccess s -> { m with Text = s; StatusMsg = sprintf "Successfully loaded at %O" DateTimeOffset.Now }, Cmd.none
  | SaveCanceled -> { m with StatusMsg = "Saving canceled" }, Cmd.none
  | LoadCanceled -> { m with StatusMsg = "Loading canceled" }, Cmd.none
  | SaveFailed ex -> { m with StatusMsg = sprintf "Saving failed with exception %s: %s" (ex.GetType().Name) ex.Message }, Cmd.none
  | LoadFailed ex -> { m with StatusMsg = sprintf "Loading failed with exception %s: %s" (ex.GetType().Name) ex.Message }, Cmd.none


let bindings : Binding<Model, Msg> list = [
  "CurrentTime" |> Binding.oneWay (fun m -> m.CurrentTime)
  "Text" |> Binding.twoWay ((fun m -> m.Text), SetText)
  "StatusMsg" |> Binding.twoWay ((fun m -> m.StatusMsg), SetText)
  "Save" |> Binding.cmd RequestSave
  "Load" |> Binding.cmd RequestLoad
]


let subscriptions (model: Model) : Sub<Msg> =
  let timerTickSub dispatch =
  let timer = new Timers.Timer(1000.)
    let disp = timer.Elapsed.Subscribe(fun _ -> dispatch (SetTime DateTimeOffset.Now))
  timer.Start()
    disp

  [
    [ nameof timerTickSub ], timerTickSub
  ]

[<CompiledName("DesignModel")>]
let designModel = initial

[<CompiledName("Program")>]
let program =
  Program.mkProgramUno init update bindings
  |> Program.withSubscription subscription
  |> Program.withLogger (new SerilogLoggerFactory(logger))

[<CompiledName("Config")>]
let config = { ElmConfig.Default with LogConsole = true }