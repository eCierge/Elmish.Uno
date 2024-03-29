module Elmish.Uno.Samples.SubModel.Program

open System
open Serilog
open Serilog.Extensions.Logging
open Elmish
open Elmish.Uno

module Counter = Elmish.Uno.Samples.SingleCounter.Program

module Clock =

  type TimeType =
    | Utc
    | Local

  type Model =
    { Time: DateTimeOffset
      TimeType: TimeType }

  let initial =
    { Time = DateTimeOffset.Now
      TimeType = Local }

  let init () = initial

  let getTime m =
    match m.TimeType with
    | Utc -> m.Time.UtcDateTime
    | Local -> m.Time.LocalDateTime

  type Msg =
    | Tick of DateTimeOffset
    | SetTimeType of TimeType

  let update msg m =
    match msg with
    | Tick t -> { m with Time = t }
    | SetTimeType t -> { m with TimeType = t }

  [<CompiledName("Bindings")>]
  let bindings () : Binding<Model, Msg> list = [
    "Time" |> Binding.oneWay getTime
    "IsLocal" |> Binding.oneWay (fun m -> m.TimeType = Local)
    "SetLocal" |> Binding.cmd (SetTimeType Local)
    "IsUtc" |> Binding.oneWay (fun m -> m.TimeType = Utc)
    "SetUtc" |> Binding.cmd (SetTimeType Utc)
  ]

  [<CompiledName("DesignModel")>]
  let designModel = initial


module CounterWithClock =

  type Model =
    { Counter: Counter.Model
      Clock: Clock.Model }

  let initial =
    { Counter = Counter.initial
      Clock = Clock.init () }

  let init () = initial

  type Msg =
    | CounterMsg of Counter.Msg
    | ClockMsg of Clock.Msg

  let update msg m =
    match msg with
    | CounterMsg msg -> { m with Counter = Counter.update msg m.Counter }
    | ClockMsg msg -> { m with Clock = Clock.update msg m.Clock }

  [<CompiledName("Bindings")>]
  let bindings () : Binding<Model, Msg> list = [
    "Counter" |> Binding.subModel((fun m -> m.Counter), snd, CounterMsg, fun () -> Counter.bindings)
    "Clock" |> Binding.subModel((fun m -> m.Clock), snd, ClockMsg, Clock.bindings)
  ]

  [<CompiledName("DesignModel")>]
  let designModel = initial

module App =

  type Model =
    { ClockCounter1: CounterWithClock.Model
      ClockCounter2: CounterWithClock.Model }

  let init () =
    { ClockCounter1 = CounterWithClock.init ()
      ClockCounter2 = CounterWithClock.init () }

  type Msg =
    | ClockCounter1Msg of CounterWithClock.Msg
    | ClockCounter2Msg of CounterWithClock.Msg

  let update msg m =
    match msg with
    | ClockCounter1Msg msg ->
        { m with ClockCounter1 = CounterWithClock.update msg m.ClockCounter1 }
    | ClockCounter2Msg msg ->
        { m with ClockCounter2 = CounterWithClock.update msg m.ClockCounter2 }

  let bindings : Binding<Model, Msg> list = [
    "ClockCounter1" |> Binding.subModel(
      (fun m -> m.ClockCounter1),
      snd,
      ClockCounter1Msg,
      CounterWithClock.bindings)

    "ClockCounter2" |> Binding.subModel(
      (fun m -> m.ClockCounter2),
      snd,
      ClockCounter2Msg,
      CounterWithClock.bindings)
  ]



let subscriptions (model: App.Model) : Sub<App.Msg> =
  let timerTickSub dispatch =
    let timer = new System.Timers.Timer(1000.)
    let disp = timer.Elapsed.Subscribe(fun _ ->
      let clockMsg =
        DateTimeOffset.Now
        |> Clock.Tick
        |> CounterWithClock.ClockMsg
      dispatch <| App.ClockCounter1Msg clockMsg
      dispatch <| App.ClockCounter2Msg clockMsg
    )
    timer.Start()
    disp

  [
    [ nameof timerTickSub ], timerTickSub
  ]

[<CompiledName("DesignModel")>]
let designModel : App.Model =
  { ClockCounter1 = CounterWithClock.initial
    ClockCounter2 = CounterWithClock.initial }

[<CompiledName("Program")>]
let program =
  Program.mkSimpleUno App.init App.update App.bindings
  |> Program.withSubscription subscriptions
  |> Program.withLogger (new SerilogLoggerFactory(logger))

[<CompiledName("Config")>]
let config = { ElmConfig.Default with LogConsole = true; Measure = true }
