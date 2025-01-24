﻿module Elmish.Uno.Tests.StaticViewModelTests

open System
open System.Collections.Concurrent
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.ComponentModel
open System.Windows.Input

open Xunit
open Hedgehog
open Swensen.Unquote

open Elmish.Uno

type internal TestVm<'model, 'msg, 'B1 when 'model : not null and 'msg : not null>(model, binding: Binding<'model,'msg>) as this =
  inherit ViewModelBase<'model, 'msg>({ initialModel = model; dispatch = (fun x -> this.Dispatch x); loggingArgs = LoggingViewModelArgs.none })

  let pcTriggers = ConcurrentDictionary<string, int>()
  let ecTriggers = ConcurrentDictionary<string, int>()
  let ccTriggers = ConcurrentDictionary<string, NotifyCollectionChangedEventArgs list>()
  let cecTriggers = ConcurrentDictionary<string, int>()
  let dispatchMsgs = ResizeArray<'msg> ()


  do
    (this :> INotifyPropertyChanged).PropertyChanged.Add (fun e ->
      pcTriggers.AddOrUpdate(e.PropertyName |> nonNull, 1, (fun _ count -> count + 1)) |> ignore
    )

    (this :> INotifyDataErrorInfo).ErrorsChanged.Add (fun e ->
      ecTriggers.AddOrUpdate(e.PropertyName |> nonNull, 1, (fun _ count -> count + 1)) |> ignore
    )

  member _.UpdateModel(m) = IViewModel.updateModel(this, m)

  static member GetPropertyName = nameof(Unchecked.defaultof<TestVm<'model, 'msg, 'B1>>.GetProperty)
  member _.GetProperty = base.Get<'B1>(binding |> Binding.unboxT)

  member private __.Dispatch x =
    dispatchMsgs.Add x

  member __.NumPcTriggersFor propName =
    pcTriggers.TryGetValue propName |> snd

  member __.NumEcTriggersFor propName =
    ecTriggers.TryGetValue propName |> snd

  member __.NumCcTriggersFor propName =
    ccTriggers.GetOrAdd(propName, []).Length

  member __.NumCecTriggersFor propName =
    cecTriggers.TryGetValue propName |> snd

  member __.Dispatches =
    dispatchMsgs |> Seq.toList

  member __.CcTriggersFor propName =
    ccTriggers.TryGetValue propName |> snd |> Seq.toList

  /// Starts tracking CollectionChanged triggers for the specified prop.
  /// Will cause the property to be retrieved.
  member this.TrackCcTriggersForGetProperty () =
    (this.GetProperty |> unbox<INotifyCollectionChanged>).CollectionChanged.Add
      (fun e ->
        ccTriggers.AddOrUpdate(
          TestVm<'model, 'msg, 'B1>.GetPropertyName,
          [e],
          (fun _ me -> e :: me)) |> ignore
      )

  /// Starts tracking CanExecuteChanged triggers for the specified prop.
  /// Will cause the property to be retrieved.
  member this.TrackCecTriggersForGetProperty () =
    (this.GetProperty |> unbox<ICommand>).CanExecuteChanged.Add
      (fun _ ->
        cecTriggers.AddOrUpdate(TestVm<'model, 'msg, 'B1>.GetPropertyName, 1, (fun _ count -> count + 1)) |> ignore
      )

module TestVm =

    let GetPropertyName = TestVm<_, _, _>.GetPropertyName

[<AutoOpen>]
module Helpers =


  let internal oneWay x = x |> Binding.oneWay
  let internal oneWayLazy x = x |> Func3.curry Binding.oneWayLazy
  let internal oneWaySeqLazy x = x |> Func5.curry Binding.oneWaySeqLazy
  let internal twoWay x = x |> Func2.curry Binding.twoWay
  let internal twoWayValidate
      name
      (get: 'model -> 'a)
      (set: 'a -> 'model -> 'msg)
      (validate: 'model -> string voption) =
    Binding.twoWayValidate (get, set, validate) name


  let internal cmd x = x |> Binding.Cmd.create



  let internal cmdParam
      name
      (exec: 'a -> 'model -> 'msg voption)
      (canExec: 'a -> 'model -> bool) =
    ({ Exec = unbox >> exec
       CanExec = unbox >> canExec }
     |> CmdData
     |> BaseBindingData
     |> createBinding) name


  let internal subModel
      name
      (getModel: 'model -> 'subModel voption)
      (toMsg: 'subMsg -> 'msg)
      (bindings: Binding<'subModel, 'subMsg> list)
      (sticky: bool) =
    Binding.subModelOpt(getModel, snd, toMsg, bindings, sticky) name


  let internal subModelSeq
      name
      (getModels: 'model -> 'subModel list)
      (getId: 'subModel -> 'id)
      (toMsg: 'id * 'subMsg -> 'msg)
      (bindings: Binding<'subModel, 'subMsg> list) =
    name
    |> Binding.subModelSeq (bindings = bindings, getId = getId)
    |> Binding.mapModel (fun m -> upcast getModels m)
    |> Binding.mapMsg toMsg



  let internal subModelSelectedItem
      name
      subModelSeqBindingName
      (get: 'model -> 'id voption)
      (set: 'id voption -> 'model -> 'msg) =
    Binding.subModelSelectedItem (subModelSeqBindingName, get, set) name


module OneWay =


  [<Fact>]
  let ``when retrieved, should always return the value returned by get`` () =
    Property.check <| property {
      let! m1 = GenX.auto<int>
      let! m2 = GenX.auto<int>

      let get = string<int>

      let binding = oneWay get TestVm.GetPropertyName
      let vm = TestVm(m1, binding)

      test <@ vm.GetProperty = get m1 @>

      vm.UpdateModel m2

      test <@ vm.GetProperty = get m2 @>
  }


  [<Fact>]
  let ``when model is updated, should trigger PC once iff the return value of get changes`` () =
    Property.check <| property {
      let! m1 = GenX.auto<int>
      let! m2 = GenX.auto<int>

      let get = string<int>

      let binding = oneWay get TestVm.GetPropertyName
      let vm = TestVm(m1, binding)
      let _ = vm.GetProperty

      vm.UpdateModel m2
      test <@ vm.NumPcTriggersFor TestVm.GetPropertyName = if get m1 = get m2 then 0 else 1 @>
  }

  [<Fact>]
  let ``on model increment, sticky-to-even binding returns even number`` () =
    let isEven x = x % 2 = 0

    let returnEven a =
      function
      | b when isEven b -> b
      | _ -> a

    Property.check <| property {
      let! m = GenX.auto<int>

      let binding = (oneWay id >> Binding.addSticky isEven) TestVm.GetPropertyName
      let vm = TestVm(m, binding)
      // Binding must be triggered first to memoize the value
      let _ = vm.GetProperty

      vm.UpdateModel (m + 1)
      test <@ vm.GetProperty = returnEven m (m + 1) @>
    }

module OneWayLazy =


  [<Fact>]
  let ``when retrieved initially, should return the value returned by map`` () =
    Property.check <| property {
      let! m = GenX.auto<int>

      let get = string<int>
      let equals = (=)
      let map = String.length

      let binding = oneWayLazy get equals map TestVm.GetPropertyName
      let vm = TestVm(m, binding)

      test <@ vm.GetProperty = (m |> get |> map) @>
  }


  [<Fact>]
  let ``when retrieved after update and equals returns false, should return the value returned by map`` () =
    Property.check <| property {
      let! m1 = GenX.auto<int>
      let! m2 = GenX.auto<int>

      let get = string<int>
      let equals _ _ = false
      let map = String.length

      let binding = oneWayLazy get equals map TestVm.GetPropertyName
      let vm = TestVm(m1, binding)
      vm.UpdateModel m2

      test <@ vm.GetProperty = (m2 |> get |> map) @>
  }


  [<Fact>]
  let ``when retrieved after update and equals returns true, should return the previous value returned by map`` () =
    Property.check <| property {
      let! m1 = GenX.auto<int>
      let! m2 = GenX.auto<int>

      let get = string<int>
      let equals _ _ = true
      let map = String.length

      let binding = oneWayLazy get equals map TestVm.GetPropertyName
      let vm = TestVm(m1, binding)
      let _ = vm.GetProperty  // populate cache
      vm.UpdateModel m2

      test <@ vm.GetProperty = (m1 |> get |> map) @>
  }


  [<Fact>]
  let ``when retrieved, updated, and retrieved again, should call map once after the update iff equals returns false`` () =
    Property.check <| property {
      let! m1 = GenX.auto<int>
      let! m2 = GenX.auto<int>
      let! eq = Gen.bool

      let get = string
      let equals _ _ = eq
      let map = InvokeTester String.length

      let binding = oneWayLazy get equals map.Fn TestVm.GetPropertyName
      let vm = TestVm(m1, binding)

      let _ = vm.GetProperty
      vm.UpdateModel m2
      map.Reset ()
      let _ = vm.GetProperty

      test <@ map.Count = if eq then 0 else 1 @>
  }


  [<Fact>]
  let ``map should never be called during model update`` () =
    Property.check <| property {
      let! m1 = GenX.auto<int>
      let! m2 = GenX.auto<int>

      let get = string
      let equals = (=)
      let map = InvokeTester String.length

      let binding = oneWayLazy get equals map.Fn TestVm.GetPropertyName
      let vm = TestVm(m1, binding)
      let _ = vm.GetProperty

      test <@ map.Count = 1 @>

      vm.UpdateModel m2

      test <@ map.Count = 1 @>
  }


  [<Fact>]
  let ``when retrieved several times between updates, map is called at most once`` () =
    Property.check <| property {
      let! m1 = GenX.auto<int>
      let! m2 = GenX.auto<int>

      let get = string
      let equals = (=)
      let map = InvokeTester String.length

      let binding = oneWayLazy get equals map.Fn TestVm.GetPropertyName
      let vm = TestVm(m1, binding)

      let _ = vm.GetProperty
      let _ = vm.GetProperty
      test <@ map.Count <= 1 @>

      map.Reset ()
      vm.UpdateModel m2
      let _ = vm.GetProperty
      let _ = vm.GetProperty
      test <@ map.Count <= 1 @>
    }


  [<Fact>]
  let ``when model is updated, should trigger PC once iff equals is false`` () =
    Property.check <| property {
      let! m1 = GenX.auto<int>
      let! m2 = GenX.auto<int>
      let! eq = Gen.bool

      let get = string
      let equals _ _ = eq
      let map = String.length

      let binding = oneWayLazy get equals map TestVm.GetPropertyName
      let vm = TestVm(m1, binding)
      let _ = vm.GetProperty
      vm.UpdateModel m2

      test <@ vm.NumPcTriggersFor TestVm.GetPropertyName = if not eq then 1 else 0 @>
  }
