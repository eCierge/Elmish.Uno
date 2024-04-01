module Elmish.Uno.Samples.SubModelOpt.Program

open System
open Serilog
open Serilog.Extensions.Logging
open Elmish
open Elmish.Uno


module Form1 =

  type Model =
    { Text: string }

  type Msg =
    | SetText of string
    | Submit

  let initial =
    { Text = "" }

  let update msg m =
    match msg with
    | SetText s -> { m with Text = s }
    | Submit -> m  // handled by parent

  [<CompiledName("Bindings")>]
  let bindings : Binding<Model, Msg> list = [
    "Text" |> Binding.twoWay ((fun m -> m.Text), SetText)
    "Submit" |> Binding.cmd Submit
  ]

  [<CompiledName("DesignInstance")>]
  let designInstance = ViewModel.designInstance initial bindings


module Form2 =

  type Model =
    { Text1: string
      Text2: string }

  type Msg =
    | SetText1 of string
    | SetText2 of string
    | Submit

  let initial =
    { Text1 = ""
      Text2 = "" }

  let update msg m =
    match msg with
    | SetText1 s -> { m with Text1 = s }
    | SetText2 s -> { m with Text2 = s }
    | Submit -> m  // handled by parent

  [<CompiledName("Bindings")>]
  let bindings : Binding<Model, Msg> list = [
    "Text1" |> Binding.twoWay ((fun m -> m.Text1), SetText1)
    "Text2" |> Binding.twoWay ((fun m -> m.Text2), SetText2)
    "Submit" |> Binding.cmd Submit
  ]

  [<CompiledName("DesignInstance")>]
  let designInstance = ViewModel.designInstance initial bindings

module App =

  type Dialog =
    | Form1 of Form1.Model
    | Form2 of Form2.Model

  type Model =
    { Dialog: Dialog option }

  let initial =
    { Dialog = None }

  let init () = initial

  type Msg =
    | ShowForm1
    | ShowForm2
    | Form1Msg of Form1.Msg
    | Form2Msg of Form2.Msg

  let update msg m =
    match msg with
    | ShowForm1 -> { m with Dialog = Some <| Form1 Form1.initial }
    | ShowForm2 -> { m with Dialog = Some <| Form2 Form2.initial }
    | Form1Msg Form1.Submit -> { m with Dialog = None }
    | Form1Msg msg' ->
        match m.Dialog with
        | Some (Form1 m') -> { m with Dialog = Form1.update msg' m' |> Form1 |> Some }
        | _ -> m
    | Form2Msg Form2.Submit -> { m with Dialog = None }
    | Form2Msg msg' ->
        match m.Dialog with
        | Some (Form2 m') -> { m with Dialog = Form2.update msg' m' |> Form2 |> Some }
        | _ -> m

  let bindings : Binding<Model, Msg> list = [
    "ShowForm1" |> Binding.cmd ShowForm1

    "ShowForm2" |> Binding.cmd ShowForm2

    "DialogVisible" |> Binding.oneWay (fun m -> m.Dialog.IsSome)

    "Form1Visible" |> Binding.oneWay
      (fun m -> match m.Dialog with Some (Form1 _) -> true | _ -> false)

    "Form2Visible" |> Binding.oneWay
      (fun m -> match m.Dialog with Some (Form2 _) -> true | _ -> false)

    "Form1"
      |> Binding.SubModel.opt Form1.bindings
      |> Binding.mapModel (fun m -> match m.Dialog with Some (Form1 m') -> Some m' | _ -> None)
      |> Binding.mapMsg Form1Msg

    "Form2"
      |> Binding.SubModel.opt Form2.bindings
      |> Binding.mapModel (fun m -> match m.Dialog with Some (Form2 m') -> Some m' | _ -> None)
      |> Binding.mapMsg Form2Msg
  ]


[<CompiledName("DesignInstance")>]
let designInstance = ViewModel.designInstance App.initial App.bindings

[<CompiledName("Program")>]
let program =
  UnoProgram.mkSimple App.init App.update App.bindings
  |> UnoProgram.withLogger (new SerilogLoggerFactory(logger))
