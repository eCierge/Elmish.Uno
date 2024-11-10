module rec Elmish.Uno.Samples.SubModelSelectedItemCascading.Program

open System
open System.Collections
open System.Collections.Generic
open System.Collections.ObjectModel
open Serilog
open Serilog.Extensions.Logging
open Elmish
open Elmish.Uno

type Entity = { Id : int; Name : string }

type Item = string

type IModel =
  abstract Items : Item list
  abstract SelectedItem : Item voption

type Model = {
  Items : Item list
  SelectionModel : SubModel1 voption
} with

  static member Init () = {
    Items = [ 0..10 ] |> List.map (fun i -> sprintf "Floor %i" i)
    SelectionModel = ValueNone
  }
  member model.Select (item : Item) = { model with SelectionModel = SubModel1.Init item |> ValueSome }
  member model.ClearSelection () = { model with SelectionModel = ValueNone }

  interface IModel with
    member model.Items = model.Items
    member model.SelectedItem = model.SelectionModel |> ValueOption.map _.SelectedItem
  member model.Models = [
    yield model :> IModel
    yield!
      model.SelectionModel
      |> ValueOption.map (fun m -> m :> IModel)
      |> ValueOption.toList
    yield!
      model.SelectionModel
      |> ValueOption.bind _.SelectionModel
      |> ValueOption.map (fun m -> m :> IModel)
      |> ValueOption.toList
  ]
  //interface IReadonlyList<IModel> with
  interface IEnumerable<IModel> with
    member model.GetEnumerator () = (model.Models |> Seq.rev).GetEnumerator ()
  interface IEnumerable with
    member model.GetEnumerator () = (model.Models |> Seq.rev).GetEnumerator ()

and SubModel1 = {
  SelectedItem : Item
  Items : Item list
  SelectionModel : SubModel2 voption
} with

  static member Init (selectedItem : Item) : SubModel1 = {
    SelectedItem = selectedItem
    Items = [ 0..10 ] |> List.map (fun i -> sprintf "Area %i" i)
    SelectionModel = ValueNone
  }
  member model.Select (item : Item) = { model with SelectionModel = SubModel2.Init item |> ValueSome }
  member model.ClearSelection () = { model with SelectionModel = ValueNone }

  interface IModel with
    member model.Items = model.Items
    member model.SelectedItem = model.SelectionModel |> ValueOption.map _.SelectedItem

and SubModel2 = {
  SelectedItem : Item
  Items : Item list
  SelectionModel : Item voption
} with

  static member Init (selectedItem : Item) : SubModel2 = {
    SelectedItem = selectedItem
    Items = [ 0..10 ] |> List.map (fun i -> sprintf "Room %i" i)
    SelectionModel = ValueNone
  }
  member model.Select (item : Item) = { model with SelectionModel = ValueSome item }
  member model.ClearSelection () = { model with SelectionModel = ValueNone }

  interface IModel with
    member model.Items = model.Items
    member model.SelectedItem = model.SelectionModel

let init () = Model.Init ()

type Msg =
  | FloorsMsg of int * FloorsMsg
  | AreasMsg of int * AreasMsg
  | RoomsMsg of int * RoomsMsg

and FloorsMsg =
  | SelectFloor of Item
  | ClearFloorSelection

and AreasMsg =
  | SelectArea of Item
  | ClearAreaSelection

and RoomsMsg =
  | SelectRoom of Item
  | ClearRoomSelection

let update msg (m : Model) =
  match msg with
  | FloorsMsg (index, (FloorsMsg.SelectFloor item)) -> m.Select item
  | FloorsMsg (index, FloorsMsg.ClearFloorSelection) -> m.ClearSelection ()
  | FloorsMsg (index, _) -> m // Use floor program and update model for other local messages
  | AreasMsg (index, (AreasMsg.SelectArea item)) -> { m with SelectionModel = m.SelectionModel.Value.Select item |> ValueSome }
  | AreasMsg (index, AreasMsg.ClearAreaSelection) -> {
      m with
          SelectionModel = m.SelectionModel.Value.ClearSelection () |> ValueSome
    }
  | AreasMsg (index, _) -> m // Use area program and update model for other local messages
  | RoomsMsg (index, (RoomsMsg.SelectRoom item)) -> {
      m with
          SelectionModel =
            m.SelectionModel
            |> ValueOption.map (fun sm -> { sm with SelectionModel = sm.SelectionModel.Value.Select item |> ValueSome })
    }
  | RoomsMsg (index, RoomsMsg.ClearRoomSelection) -> {
      m with
          SelectionModel =
            m.SelectionModel
            |> ValueOption.map (fun sm -> {
              sm with
                  SelectionModel = sm.SelectionModel.Value.ClearSelection () |> ValueSome
            })
    }
  | RoomsMsg (index, _) -> m // Use room program and update model for other local messages

module Floor =

  type public Program () =

    member p.Init () = Model.Init (), Cmd.none

    member p.Update msg (m : Model) =
      // Other properties update logic here
      m, msg

  module Bindings =

    let private viewModel = Unchecked.defaultof<SubViewModel>

    let itemsBinding = BindingT.oneWaySeq ((fun (m : Model) -> m.Items), (=), id) (nameof viewModel.Items)

    let selectedItemBinding =
      let get (m : Model) = m.SelectionModel |> ValueOption.map _.SelectedItem
      let set floor =
        match floor with
        | ValueSome f -> SelectFloor f
        | ValueNone -> ClearFloorSelection
      BindingT.twoWayOptObj (get, set) (nameof viewModel.SelectedItem)

  type SubViewModel (args) =
    inherit ViewModelBase<Model, FloorsMsg> (args)

    new (args : ViewModelArgs<obj, obj>)
      =
      let args = args |> ViewModelArgs.map unbox<Model> unbox
      SubViewModel args

    member _.SelectedItem
      with get () = base.Get<Item> (Bindings.selectedItemBinding)
      and set (value) = base.Set<Item> (Bindings.selectedItemBinding, value)

    member _.Items = base.Get (Bindings.itemsBinding)

    interface IViewModel<obj, obj> with
      member vm.CurrentModel = (vm :> IViewModel<Model, FloorsMsg>).CurrentModel
      member vm.UpdateModel msg = (vm :> IViewModel<Model, FloorsMsg>).UpdateModel (unbox msg)

module Area =

  type public Program () =

    member p.Init (item : Item) = SubModel1.Init item, Cmd.none

    member p.Update msg (m : SubModel1) =
      // Other properties update logic here
      m, msg

  module Bindings =

    let private viewModel = Unchecked.defaultof<SubViewModel>

    let itemsBinding =
      BindingT.oneWaySeq ((fun (m : SubModel1) -> m.Items), (=), id) (nameof viewModel.Items)

    let selectedItemBinding =
      let get (m : SubModel1) = m.SelectionModel |> ValueOption.map _.SelectedItem
      let set area =
        match area with
        | ValueSome a -> SelectArea a
        | ValueNone -> ClearAreaSelection
      BindingT.twoWayOptObj (get, set) (nameof viewModel.SelectedItem)

  type SubViewModel (args) =
    inherit ViewModelBase<SubModel1, AreasMsg> (args)

    new (args : ViewModelArgs<obj, obj>)
      =
      let args = args |> ViewModelArgs.map unbox<SubModel1> unbox
      SubViewModel args

    member _.SelectedItem
      with get () = base.Get<Item> (Bindings.selectedItemBinding)
      and set (value) = base.Set<Item> (Bindings.selectedItemBinding, value)

    member _.Items = base.Get (Bindings.itemsBinding)

    interface IViewModel<obj, obj> with
      member vm.CurrentModel = (vm :> IViewModel<SubModel1, AreasMsg>).CurrentModel
      member vm.UpdateModel msg = (vm :> IViewModel<SubModel1, AreasMsg>).UpdateModel (unbox msg)

module Room =

  type public Program () =

    member p.Init (item : Item) = SubModel2.Init item, Cmd.none

    member p.Update msg (m : SubModel2) =
      // Other properties update logic here
      m, msg

  module Bindings =

    let private viewModel = Unchecked.defaultof<SubViewModel>

    let itemsBinding =
      BindingT.oneWaySeq ((fun (m : SubModel2) -> m.Items), (=), id) (nameof viewModel.Items)

    let selectedItemBinding =
      let get (m : SubModel2) = m.SelectionModel
      let set room =
        match room with
        | ValueSome r -> SelectRoom r
        | ValueNone -> ClearRoomSelection
      BindingT.twoWayOptObj (get, set) (nameof viewModel.SelectedItem)

  type SubViewModel (args) =
    inherit ViewModelBase<SubModel2, RoomsMsg> (args)

    new (args : ViewModelArgs<obj, obj>)
      =
      let args = args |> ViewModelArgs.map unbox<SubModel2> unbox
      SubViewModel args

    member _.SelectedItem
      with get () = base.Get<Item> (Bindings.selectedItemBinding)
      and set (value) = base.Set<Item> (Bindings.selectedItemBinding, value)

    member _.Items = base.Get (Bindings.itemsBinding)

    interface IViewModel<obj, obj> with
      member vm.CurrentModel = (vm :> IViewModel<SubModel2, RoomsMsg>).CurrentModel
      member vm.UpdateModel msg = (vm :> IViewModel<SubModel2, RoomsMsg>).UpdateModel (unbox msg)


module Bindings =

  let private viewModel = Unchecked.defaultof<SubModelSelectedItemCascadingViewModel>

  let itemsBinding =

    let createViewModel (args : ViewModelArgs<obj, obj>) : IViewModel<obj, obj> =
      let modelType = args.InitialModel.GetType ()
      if modelType = typeof<Model> then
        Floor.SubViewModel args
      elif modelType = typeof<SubModel1> then
        Area.SubViewModel args
      elif modelType = typeof<SubModel2> then
        Room.SubViewModel args
      else
        failwithf "Unknown model type: %A" modelType

    //let mapVmMsg (index, msg : obj) : Msg =
    //    match msg with
    //    | :? Floor.Msg as m -> FloorMsg (index, m)
    //    | :? Area.Msg as m -> AreaMsg (index, m)
    //    | :? Room.Msg as m -> RoomMsg (index, m)
    //    | _ -> failwithf "Unknown message type: %A" msg

    let mapVmMsg (index, msg : obj) : Msg =
      match index with
      | 0 -> FloorsMsg (index, msg :?> FloorsMsg)
      | 1 -> AreasMsg (index, msg :?> AreasMsg)
      | 2 -> RoomsMsg (index, msg :?> RoomsMsg)
      | _ -> failwithf "Unknown message type: %A" msg

    BindingT.subModelSeq createViewModel (nameof viewModel.Items)
    |> Binding.mapModel (fun (m : Model) -> m.Models |> Seq.cast<obj> |> Seq.rev)
    |> Binding.addLazy (fun (m1 : Model) (m2 : Model) -> m1.Items = m2.Items)
    |> Binding.mapMsg (fun (i, msg) -> mapVmMsg (i, msg))

type SubModelSelectedItemCascadingViewModel (args) =
  inherit ViewModelBase<Model, Msg> (args)

  new (dispatcher) as vm
    =
    let args =
      let program = UnoProgram.mkSimpleT init update (fun _ -> vm :> IViewModel<Model, Msg>)
      UnoProgram.createVmArgs dispatcher (Func<_> (fun () -> vm :> IViewModel<Model, Msg>)) program
    SubModelSelectedItemCascadingViewModel args

  member _.Items = base.Get (Bindings.itemsBinding)
