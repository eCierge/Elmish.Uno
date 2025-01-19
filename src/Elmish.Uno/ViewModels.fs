﻿namespace Elmish.Uno

open System
open System.Dynamic
open System.Collections.Generic
open System.Collections.ObjectModel
open Microsoft.Extensions.Logging
open Microsoft.UI.Xaml.Data
open FsToolkit.ErrorHandling

open BindingVmHelpers

type private INotifyPropertyChanged = System.ComponentModel.INotifyPropertyChanged
type private PropertyChangedEventHandler = System.ComponentModel.PropertyChangedEventHandler
type private PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs

type private INotifyDataErrorInfo = System.ComponentModel.INotifyDataErrorInfo
type private DataErrorsChangedEventArgs = System.ComponentModel.DataErrorsChangedEventArgs

/// Represents all necessary data used to create a binding.
type Binding<'model, 'msg, 't> =
  internal
    { Name: string
      Data: BindingData<'model, 'msg, 't> }

type Binding<'model, 'msg> = Binding<'model, 'msg, objnull>


[<AutoOpen>]
module internal Helpers =

  let createBinding data name =
    { Name = name
      Data = data |> BindingData.boxT }

  let createBindingT data name =
    { Name = name
      Data = data }

  type SubModelSelectedItemLast with
    member this.CompareBindings() : Binding<'model, 'msg> -> Binding<'model, 'msg> -> int =
      fun a b -> this.Recursive(a.Data) - this.Recursive(b.Data)

type IViewModel<'model, 'msg> =
  abstract member CurrentModel: 'model
  abstract member UpdateModel: 'model -> unit

module internal IViewModel =
  let currentModel (vm: #IViewModel<'model, 'msg>) = vm.CurrentModel
  let updateModel (vm: #IViewModel<'model, 'msg>, m: 'model) = vm.UpdateModel(m)

type internal ViewModelHelper<'model, 'msg> =
  { GetSender: unit -> obj
    LoggingArgs: LoggingViewModelArgs
    Model: 'model
    Bindings: Map<string, VmBinding<'model, 'msg, objnull>>
    ValidationErrors: Map<string, string list ref>
    PropertyChanged: Event<PropertyChangedEventHandler, PropertyChangedEventArgs>
    ErrorsChanged: DelegateEvent<EventHandler<DataErrorsChangedEventArgs>> }

  member x.HasErrors =
    // WPF calls this too often, so don't log https://github.com/elmish/Elmish.Uno/issues/354
    x.ValidationErrors
    |> Seq.map (fun (Kvp(_, errors)) -> errors.Value)
    |> Seq.filter (not << List.isEmpty)
    |> (not << Seq.isEmpty)

  member x.GetErrors name =
    let name = name |> Option.ofObj |> Option.defaultValue "<null>" // entity-level errors are being requested when given null or ""  https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifydataerrorinfo.geterrors#:~:text=null%20or%20Empty%2C%20to%20retrieve%20entity-level%20errors
    x.LoggingArgs.log.LogTrace("[{BindingNameChain}] GetErrors {BindingName}", x.LoggingArgs.nameChain, name)
    x.ValidationErrors
    |> IReadOnlyDictionary.tryFind name
    |> ValueOption.map (fun errors -> errors.Value |> Seq.cast<obj>)
    |> ValueOption.defaultValue Seq.empty
    |> (fun x -> x)

  interface INotifyPropertyChanged with
    [<CLIEvent>]
    member x.PropertyChanged = x.PropertyChanged.Publish

  interface INotifyDataErrorInfo with
    [<CLIEvent>]
    member x.ErrorsChanged = x.ErrorsChanged.Publish
    member x.HasErrors = x.HasErrors
    member x.GetErrors name = x.GetErrors name

module internal ViewModelHelper =

  let create getSender args bindings validationErrors = {
    GetSender = getSender
    LoggingArgs = args.loggingArgs
    Model = args.initialModel
    ValidationErrors = validationErrors
    Bindings = bindings
    PropertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    ErrorsChanged = DelegateEvent<EventHandler<DataErrorsChangedEventArgs>>()
  }

  let empty getSender args =
    create getSender args Map.empty Map.empty

  let getEventsToRaise newModel helper =
    helper.Bindings
      |> Seq.collect (fun (Kvp (name, binding)) -> Update(helper.LoggingArgs, name).Recursive(helper.Model, newModel, binding))
      |> Seq.toList

  let raiseEvents hadErrors eventsToRaise helper =
    let {
      log = log
      nameChain = nameChain } = helper.LoggingArgs

    let raisePropertyChanged name =
      log.LogTrace("[{BindingNameChain}] PropertyChanged {BindingName}", nameChain, name)
      helper.PropertyChanged.Trigger(helper.GetSender (), PropertyChangedEventArgs name)
    let raiseCanExecuteChanged (cmd: Command) =
      cmd.RaiseCanExecuteChanged ()
    let raiseErrorsChanged name =
      log.LogTrace("[{BindingNameChain}] ErrorsChanged {BindingName}", nameChain, name)
      helper.ErrorsChanged.Trigger([| helper.GetSender (); box <| DataErrorsChangedEventArgs name |])

    eventsToRaise
    |> List.iter (function
      | ErrorsChanged name -> raiseErrorsChanged name
      | PropertyChanged name -> raisePropertyChanged name
      | CanExecuteChanged cmd -> cmd |> raiseCanExecuteChanged)

    if hadErrors <> helper.HasErrors then
      raisePropertyChanged (nameof helper.HasErrors)

  let getFunctionsForSubModelSelectedItem loggingArgs initializedBindings (name: string) =
    let log = loggingArgs.log
    initializedBindings
    |> IReadOnlyDictionary.tryFind name
    |> function
      | ValueSome b ->
        match FuncsFromSubModelSeqKeyed().Recursive(b |> MapOutputType.unboxVm) with
        | Some x -> ValueSome x
        | None -> log.LogError("SubModelSelectedItem binding referenced binding {SubModelSeqBindingName} but it is not a SubModelSeq binding", name)
                  ValueNone
      | ValueNone -> log.LogError("SubModelSelectedItem binding referenced binding {SubModelSeqBindingName} but no binding was found with that name", name)
                     ValueNone

type DynamicViewModel<'model, 'msg when 'model : not null and 'msg : not null>
      ( args: ViewModelArgs<'model, 'msg>,
        bindings: Binding<'model, 'msg> list)
      as this =
  inherit DynamicObject()

  let { initialModel = initialModel
        dispatch = dispatch
        loggingArgs = loggingArgs
      } = args

  let { log = log
        nameChain = nameChain
      } = loggingArgs

  let (bindings, validationErrors) =
    let initializeBinding initializedBindings binding =
      Initialize(loggingArgs, binding.Name, ViewModelHelper.getFunctionsForSubModelSelectedItem loggingArgs initializedBindings)
        .Recursive(initialModel, dispatch, (fun () -> this |> IViewModel.currentModel), binding.Data)

    log.LogTrace("[{BindingNameChain}] Initializing bindings", nameChain)

    let bindingDict = Dictionary<string, VmBinding<'model, 'msg, objnull>>(bindings.Length)
    let validationDict = Dictionary<string, string list ref>()

    let sortedBindings =
      bindings
      |> List.sortWith (SubModelSelectedItemLast().CompareBindings())
    for b in sortedBindings do
      if bindingDict.ContainsKey b.Name then
        log.LogError("Binding name {BindingName} is duplicated. Only the first occurrence will be used.", b.Name)
      else
        option {
          let! vmBinding = initializeBinding bindingDict b
          do bindingDict.Add(b.Name, vmBinding)
          let! errorList = FirstValidationErrors().Recursive(vmBinding)
          do validationDict.Add(b.Name, errorList)
          return ()
        } |> Option.defaultValue ()
    (bindingDict    |> Seq.map (|KeyValue|) |> Map.ofSeq,
     validationDict |> Seq.map (|KeyValue|) |> Map.ofSeq)

  let mutable helper =
    ViewModelHelper.create
      (fun () -> this)
      args
      bindings
      validationErrors

  member internal _.Bindings = bindings

  member internal _.CurrentModel : 'model = helper.Model

  interface IViewModel<'model, 'msg> with
    member _.CurrentModel : 'model = helper.Model

    member _.UpdateModel (newModel: 'model) : unit =
      let hadErrors = helper.HasErrors
      let eventsToRaise = ViewModelHelper.getEventsToRaise newModel helper
      helper <- { helper with Model = newModel }
      ViewModelHelper.raiseEvents hadErrors eventsToRaise helper

  member internal _.TryGetMemberCore (name: string, binding: VmBinding<'model, 'msg, objnull>, result: byref<objnull>) =
    try
      match Get(nameChain).Recursive(helper.Model, binding) with
      | Ok v ->
        result <- v
        true
      | Error e ->
        match e with
        | GetError.OneWayToSource -> log.LogError("[{BindingNameChain}] TryGetMember FAILED: Binding {BindingName} is read-only", nameChain, name)
        | GetError.SubModelSelectedItem d -> log.LogError("[{BindingNameChain}] TryGetMember FAILED: Failed to find an element of the SubModelSeq binding {SubModelSeqBindingName} with ID {ID} in the getter for the binding {BindingName}", d.NameChain, d.SubModelSeqBindingName, d.Id, name)
        | GetError.ToNullError (ValueOption.ToNullError.ValueCannotBeNull nonNullTypeName) -> log.LogError("[{BindingNameChain}] TryGetMember FAILED: Binding {BindingName} is null, but type {Type} is non-nullable", nameChain, name, nonNullTypeName)
        result <- null
        false
    with e ->
      log.LogError(e, "[{BindingNameChain}] TryGetMember FAILED: Exception thrown while processing binding {BindingName}", nameChain, name)
      reraise ()

  member internal vm.TryGetMemberCore (name: string, binding: VmBinding<'model, 'msg, objnull>) =
    let mutable result = null
    let _ = vm.TryGetMemberCore(name, binding, &result)
    result

  override vm.TryGetMember (binder, result) =
    let name = binder.Name
    log.LogTrace("[{BindingNameChain}] TryGetMember {BindingName}", nameChain, name)
    match bindings.TryGetValue name with
    | false, _ ->
        log.LogError("[{BindingNameChain}] TryGetMember FAILED: Property {BindingName} doesn't exist", nameChain, name)
        false
    | true, binding ->
      vm.TryGetMemberCore(name, binding, &result)

  member internal _.TrySetMemberCore (name, binding: VmBinding<'model, 'msg, objnull>, value) =
    try
      let success = Set(value).Recursive(helper.Model, binding)
      if not success then
        log.LogError("[{BindingNameChain}] TrySetMember FAILED: Binding {BindingName} is read-only", nameChain, name)
      success
    with e ->
      log.LogError(e, "[{BindingNameChain}] TrySetMember FAILED: Exception thrown while processing binding {BindingName}", nameChain, name)
      reraise ()

  override vm.TrySetMember (binder, value) =
    let name = binder.Name
    log.LogTrace("[{BindingNameChain}] TrySetMember {BindingName}", nameChain, name)
    match bindings.TryGetValue name with
    | false, _ ->
        log.LogError("[{BindingNameChain}] TrySetMember FAILED: Property {BindingName} doesn't exist", nameChain, name)
        false
    | true, binding ->
      vm.TrySetMemberCore(name, binding, value)


  override _.GetDynamicMemberNames () =
    log.LogTrace("[{BindingNameChain}] GetDynamicMemberNames", nameChain)
    bindings.Keys


  interface INotifyPropertyChanged with
    [<CLIEvent>]
    member _.PropertyChanged = (helper :> INotifyPropertyChanged).PropertyChanged

  interface INotifyDataErrorInfo with
    [<CLIEvent>]
    member _.ErrorsChanged = (helper :> INotifyDataErrorInfo).ErrorsChanged
    member _.HasErrors = (helper :> INotifyDataErrorInfo).HasErrors
    member _.GetErrors name = (helper :> INotifyDataErrorInfo).GetErrors name

  member private this.GetProperty(name : string) : ICustomProperty | null =
    if name = "CurrentModel" then DynamicCustomProperty<DynamicViewModel<'model,'msg>, obj>(name, fun vm -> vm.CurrentModel :> obj) :> _
    else
    match this.Bindings.TryGetValue name with
    | false, _ ->
      System.Diagnostics.Debugger.Break()
      null
    | true, binding ->
      GetCustomProperty(name).Recursive(binding, binding)

  interface ICustomPropertyProvider with

    member this.GetCustomProperty(name) = this.GetProperty(name)

    member this.GetIndexedProperty(name, _ : Type) = this.GetProperty(name)

    member this.GetStringRepresentation() = this.CurrentModel.ToString()

    member this.Type = this.CurrentModel.GetType()

and GetCustomProperty(name: string) =

  member internal _.Base (rootBinding: VmBinding<'model, 'msg, objnull>, vmBinding: BaseVmBinding<'bindingModel, 'bindingMsg, obj>) : ICustomProperty | null =
    match vmBinding with
    | OneWay _ -> DynamicCustomProperty<DynamicViewModel<'model,'msg>, obj | null>(name, fun vm -> vm.TryGetMemberCore(name, rootBinding)) :> _
    | TwoWay _ ->
      DynamicCustomProperty<DynamicViewModel<'model,'msg>, obj | null>(name,
        (fun vm -> vm.TryGetMemberCore(name, rootBinding)),
        (fun vm value -> vm.TrySetMemberCore(name, rootBinding, value) |> ignore)) :> _
    | OneWaySeq data ->
      match data.Values.GetCollection() with
      | :? System.Collections.IList ->
        DynamicCustomProperty<DynamicViewModel<'model,'msg>, System.Collections.IList>(name,
          fun vm -> vm.TryGetMemberCore(name, rootBinding) :?> _) :> _
      | :? System.Collections.IEnumerable ->
        DynamicCustomProperty<DynamicViewModel<'model,'msg>, System.Collections.IEnumerable>(name,
          fun vm -> vm.TryGetMemberCore(name, rootBinding) :?> _) :> _
      | c -> raise (NotSupportedException $"Type '{c.GetType().FullName}' is not a collection")
    | OneWaySeqGrouped data ->
      match data.Values.GetCollection() with
      | :? System.Collections.IList ->
        DynamicCustomProperty<DynamicViewModel<'model,'msg>, System.Collections.IList>(name,
          fun vm -> vm.TryGetMemberCore(name, rootBinding) :?> _) :> _
      | :? System.Collections.IEnumerable ->
        DynamicCustomProperty<DynamicViewModel<'model,'msg>, System.Collections.IEnumerable>(name,
          fun vm -> vm.TryGetMemberCore(name, rootBinding) :?> _) :> _
      | c -> raise (NotSupportedException $"Type '{c.GetType().FullName}' is not a collection")
    | TwoWaySeq data ->
      match data.Values.GetCollection() with
      | :? System.Collections.IList ->
        DynamicCustomProperty<DynamicViewModel<'model,'msg>, System.Collections.IList>(name,
          fun vm -> vm.TryGetMemberCore(name, rootBinding) :?> _) :> _
      | :? System.Collections.IEnumerable ->
        DynamicCustomProperty<DynamicViewModel<'model,'msg>, System.Collections.IEnumerable>(name,
          fun vm -> vm.TryGetMemberCore(name, rootBinding) :?> _) :> _
      | c -> raise (NotSupportedException $"Type '{c.GetType().FullName}' is not a collection")
    | Cmd _ ->
      DynamicCustomProperty<DynamicViewModel<'model,'msg>, System.Windows.Input.ICommand>(name,
        fun vm -> vm.TryGetMemberCore(name, rootBinding) :?> _) :> _
    | SubModel _
    | SubModelWin _ ->
      DynamicCustomProperty<DynamicViewModel<'model,'msg>, obj | null>(name,
        fun vm -> vm.TryGetMemberCore(name, rootBinding)) :> _
    | SubModelSeqUnkeyed _
    | SubModelSeqKeyed _ ->
      DynamicCustomProperty<DynamicViewModel<'model,'msg>, System.Collections.IList>(name,
        fun vm -> vm.TryGetMemberCore(name, rootBinding) :?> _) :> _
    | SubModelSelectedItem _ ->
      DynamicCustomProperty<DynamicViewModel<'model,'msg>, obj | null>(name,
        fun vm -> vm.TryGetMemberCore(name, rootBinding)) :> _

  member internal this.Recursive<'model, 'msg, 'bindingModel, 'bindingMsg when 'model : not null and 'msg : not null>
      (rootBinding: VmBinding<'model, 'msg, objnull>,
       binding: VmBinding<'bindingModel, 'bindingMsg, objnull>)
      : ICustomProperty | null =
    match binding with
    | BaseVmBinding b -> this.Base(rootBinding, b)
    | Cached b -> this.Recursive(rootBinding, b.Binding)
    | Validatation b -> this.Recursive(rootBinding, b.Binding)
    | Lazy b -> this.Recursive<'model, 'msg, obj, obj>(rootBinding, b.Binding)
    | AlterMsgStream b -> this.Recursive<'model, 'msg, obj, obj>(rootBinding, b.Binding)


open System.Runtime.CompilerServices
open System.Runtime.InteropServices

type ViewModelBase<'model, 'msg when 'model : not null and 'msg : not null>(args: ViewModelArgs<'model, 'msg>)
  as this =

  let mutable setBindings = Map.empty<String, VmBinding<'model, 'msg, obj>>

  let mutable helper = ViewModelHelper.empty (fun () -> this) args

  let { loggingArgs = loggingArgs
        dispatch = dispatch } = args
  let { log = log; nameChain = nameChain } = loggingArgs

  let initializeBinding initializedBindings binding =
    Initialize(loggingArgs, binding.Name, ViewModelHelper.getFunctionsForSubModelSelectedItem loggingArgs initializedBindings)
      .Recursive((this |> IViewModel.currentModel), dispatch, (fun () -> this |> IViewModel.currentModel), binding.Data)

  member _.NotifyPropertyChanged (name: string) =
    helper.PropertyChanged.Trigger(helper.GetSender (), PropertyChangedEventArgs name)

  member vm.Get<'T> ([<Optional; CallerMemberName>] memberName: string) =
    fun (binding: string -> Binding<'model, 'msg, 'T>) ->
      let result =
        voption {
          let! name = memberName |> ValueOption.ofObj
          let! vmBinding = voption {
            match helper.Bindings.TryGetValue name with
            | true, value ->
              return value |> MapOutputType.unboxVm
            | false, _ ->
              let binding = binding name
              let! vmBinding = binding |> initializeBinding helper.Bindings
              let newBindings = helper.Bindings.Add (name, vmBinding |> MapOutputType.boxVm)
              let newValidationErrors =
                FirstValidationErrors().Recursive(vmBinding)
                |> ValueOption.map (fun errorList -> helper.ValidationErrors.Add (name, errorList))
                |> ValueOption.defaultValue helper.ValidationErrors
              let hadErrors = helper.HasErrors
              helper <-
                { helper with
                    Bindings = newBindings
                    ValidationErrors = newValidationErrors }
              let hasErrors = helper.HasErrors
              if hasErrors <> hadErrors then
                // We use hasErrors instead of hadErrors because
                // we already raise the HasErrors property changed, so we don't need to raise it again
                ViewModelHelper.raiseEvents hasErrors [PropertyChanged (nameof vm.HasErrors)] helper
              return vmBinding
          }
          return Get(nameChain).Recursive(helper.Model, vmBinding)
        }
      match result with
      | ValueNone ->
        log.LogError("[{BindingNameChain}] Get FAILED: Binding {BindingName} could not be constructed", nameChain, memberName)
        failwithf $"[%s{nameChain}] Get FAILED: Binding {memberName} could not be constructed"
      | ValueSome (Error e) ->
        match e with
        | GetError.OneWayToSource -> log.LogError("[{BindingNameChain}] Get FAILED: Binding {BindingName} is read-only", nameChain, memberName)
        | GetError.SubModelSelectedItem d -> log.LogError("[{BindingNameChain}] Get FAILED: Failed to find an element of the SubModelSeq binding {SubModelSeqBindingName} with ID {ID} in the getter for the binding {BindingName}", d.NameChain, d.SubModelSeqBindingName, d.Id, memberName)
        | GetError.ToNullError (ValueOption.ToNullError.ValueCannotBeNull nonNullTypeName) -> log.LogError("[{BindingNameChain}] Get FAILED: Binding {BindingName} is null, but type {Type} is non-nullable", nameChain, memberName, nonNullTypeName)
        failwithf $"[%s{nameChain}] Get FAILED: Binding {memberName} returned an error {e}"
      | ValueSome (Ok r) -> r

  member vm.Get<'T> (binding: Binding<'model, 'msg, 'T>) =
    vm.Get<'T>(binding.Name) (fun _ -> binding)

  member _.Set<'T> (value: 'T, [<Optional; CallerMemberName>] memberName: string) =
    fun (binding: string -> Binding<'model, 'msg, 'T>) ->
      try
        let success =
          voption {
            let! name = memberName |> ValueOption.ofObj
            let! vmBinding = voption {
              match setBindings.TryGetValue name with
              | true, value ->
                return value |> MapOutputType.unboxVm
              | false, _ ->
                let binding = binding name
                let! vmBinding = initializeBinding helper.Bindings binding
                setBindings <- setBindings.Add (name, vmBinding |> MapOutputType.boxVm)
                return vmBinding
            }
            return Set(value).Recursive(helper.Model, vmBinding)
          }
        if success = ValueSome false then
          log.LogError("[{BindingNameChain}] Set FAILED: Binding {BindingName} is read-only", nameChain, memberName)
        else if success = ValueNone then
          log.LogError("[{BindingNameChain}] Set FAILED: Binding {BindingName} could not be constructed", nameChain, memberName)
      with e ->
        log.LogError(e, "[{BindingNameChain}] Set FAILED: Exception thrown while processing binding {BindingName}", nameChain, memberName)
        reraise ()

  member vm.Set<'T> (binding: Binding<'model, 'msg, 'T>, value: 'T) =
    vm.Set<'T>(value, binding.Name) (fun _ -> binding)

  abstract HasErrors : bool
  default _.HasErrors = helper.HasErrors
  member _.GetErrors name = helper.GetErrors name

  interface IViewModel<'model, 'msg> with
    member _.CurrentModel = helper.Model

    member _.UpdateModel(newModel: 'model) =
      let hadErrors = helper.HasErrors
      let eventsToRaise = ViewModelHelper.getEventsToRaise newModel helper
      helper <- { helper with Model = newModel }
      ViewModelHelper.raiseEvents hadErrors eventsToRaise helper

  interface INotifyPropertyChanged with
    [<CLIEvent>]
    member _.PropertyChanged = (helper :> INotifyPropertyChanged).PropertyChanged

  interface INotifyDataErrorInfo with
    [<CLIEvent>]
    member _.ErrorsChanged = (helper :> INotifyDataErrorInfo).ErrorsChanged
    member _.HasErrors = helper.HasErrors
    member _.GetErrors name = helper.GetErrors name
