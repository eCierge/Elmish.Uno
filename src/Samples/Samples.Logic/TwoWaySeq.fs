module Elmish.Uno.Samples.TwoWaySeq.Program

open System
open System.Collections.Specialized
open Elmish
open Elmish.Uno

type Model =
  { Tokens: string list }

type Msg =
  | AddToken
  | TokensChanged of NotifyCollectionChangedEventArgs

let initial =
  { Tokens = [ "Plumbing"; "Electricity" ] }

let init () = initial, Cmd.none

let update msg m =
  match msg with
  | AddToken -> { m with Tokens = "New Token" :: m.Tokens }, Cmd.none
  | TokensChanged e ->
    match e.Action with
    | NotifyCollectionChangedAction.Add ->
      let newTokens = e.NewItems |> Seq.cast<string> |> List.ofSeq
      { m with Tokens = newTokens @ m.Tokens }, Cmd.none
    | NotifyCollectionChangedAction.Remove ->
      let removedTokens = e.OldItems |> Seq.cast<string> |> List.ofSeq
      { m with Tokens = m.Tokens |> List.filter (fun t -> not (List.contains t removedTokens)) }, Cmd.none
    | _ -> m, Cmd.none


[<CompiledName "Bindings">]
let bindings : Binding<Model, Msg> list = [
  "Tokens" |> Binding.twoWaySeq ((_.Tokens >> Seq.ofList), (=), id, TokensChanged)
  "AddToken" |> Binding.cmd AddToken
]

[<CompiledName("DesignInstance")>]
let designInstance = ViewModel.designInstance initial bindings

[<CompiledName("Program")>]
let program =
  //UnoProgram.mkSimple init update bindings
  UnoProgram.mkProgram init update bindings

type ViewModel(dispatcher) as vm =
  inherit DynamicViewModel<Model, Msg>(
    UnoProgram.createVmArgs dispatcher (Func<_>(fun () -> vm)) program,
    bindings
  )
