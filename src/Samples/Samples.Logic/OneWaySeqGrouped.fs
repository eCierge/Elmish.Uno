module Elmish.Uno.Samples.OneWaySeqGrouped.Program

open System
open Elmish
open Elmish.Uno
open Microsoft.Extensions.Logging

let groups = [ "A"; "B"; "C" ]

let random = Random()

type Item =
  { Group : string
    Id: int }

let createItem id =
  { Group = groups.[random.Next(0, groups.Length)];
    Id = id }

type Model =
  { Items: Item array
    Events: EventHolder }

and EventHolder = {
  GroupChangedEvent: Event<EventArgs>
}
with
  [<CLIEvent>]
  member this.GroupChanged = this.GroupChangedEvent.Publish

type Msg =
  | ChangeGroup
  | RaiseGroupChangedEvent
  | AddItem

let initial =
  let eventHolder = { GroupChangedEvent = Event<EventArgs>() }
  { Items = [| 10..-1..1 |] |> Array.map createItem; Events = eventHolder }

let init () = initial, Cmd.none

let update msg m =
  match msg with
  | AddItem ->
    let id = m.Items.Length + 1
    let item = createItem id
    { m with Items = m.Items |> Array.append [| item |] }, Cmd.none
  | ChangeGroup ->
    let index = random.Next(0, m.Items.Length)
    let item =
      { m.Items[index] with Group = groups.[Random().Next(0, groups.Length)] }
    m.Items[index] <- item
    m, Cmd.ofMsg RaiseGroupChangedEvent
  | RaiseGroupChangedEvent ->
    m.Events.GroupChangedEvent.Trigger EventArgs.Empty
    m, Cmd.none

[<CompiledName "Bindings">]
let bindings : Binding<Model, Msg> list = [
  "Items" //|> Binding.oneWaySeq (_.Items, (fun i1 i2 -> i1.Id = i2.Id), _.Id, _.Group)
  // In order to force the collection view to reevaluate groups we need to update the collection each time
  // with static binding this can be achieved by raising PropertyChanged only when needed
  |> Binding.oneWaySeqLazy (id, (fun c1 c2 -> false), _.Items, (fun i1 i2 -> i1.Id = i2.Id), _.Id, _.Group)
  "AddItem" |> Binding.cmd AddItem
  "ChangeGroup" |> Binding.cmd ChangeGroup
  "Events" |> Binding.oneWay _.Events
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
