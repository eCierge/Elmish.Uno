module Elmish.Uno.Samples.Validation.Program

open System
open System.Linq
open Serilog
open Serilog.Extensions.Logging
open Elmish
open Elmish.Uno


module Result =

  module Error =

    let toList = function
      | Ok _ -> []
      | Error e -> [ e ]


let requireNotEmpty s =
  if String.IsNullOrEmpty s then Error "This field is required" else Ok s

let parseInt (s: string) =
  match Int32.TryParse s with
  | true, i -> Ok i
  | false, _ -> Error "Please enter a valid integer"

let requireExactly y x =
  if x = y then Ok x else Error <| sprintf "Please enter %A" y

let validateInt42 =
  requireNotEmpty
  >> Result.bind parseInt
  >> Result.bind (requireExactly 42)


let validatePassword (s: string) =
  [
    if s.All(fun c -> Char.IsDigit c |> not) then
      "Must contain a digit"
    if s.All(fun c -> Char.IsLower c |> not) then
      "Must contain a lowercase letter"
    if s.All(fun c -> Char.IsUpper c |> not) then
      "Must contain an uppercase letter"
  ]


type Model =
  { UpdateCount: int
    Value: string
    Password: string }

let initial =
  { UpdateCount = 0
    Value = ""
    Password = "" }

let init () = initial

type Msg =
  | NewValue of string
  | NewPassword of string
  | Submit

let increaseUpdateCount m =
  { m with UpdateCount = m.UpdateCount + 1 }

let update msg m =
  let m = increaseUpdateCount m
  match msg with
  | NewValue x -> { m with Value = x }
  | NewPassword x -> { m with Password = x }
  | Submit -> m

let errorOnEven m =
  if m.UpdateCount % 2 = 0 then
    [ "Even counts have this error" ]
  else
    []

let bindings : Binding<Model, Msg> list = [
  "UpdateCount"
    |> Binding.oneWay(fun m -> m.UpdateCount)
    |> Binding.addValidation errorOnEven
  "Value" |> Binding.twoWayValidate(
    (fun m -> m.Value),
    NewValue,
    (fun m -> validateInt42 m.Value))
  "Password" |> Binding.twoWayValidate(
    (fun m -> m.Password),
    NewPassword,
    (fun m -> validatePassword m.Password))
  "Submit" |> Binding.cmdIf(
    (fun _ -> Submit),
    (fun m -> (match validateInt42 m.Value with Ok _ -> true | Error _ -> false) && (validatePassword m.Password |> List.isEmpty)))
]

[<CompiledName("DesignModel")>]
let designModel = initial

[<CompiledName("Program")>]
let program =
  Program.mkSimpleUno init update bindings
  |> Program.withLogger (new SerilogLoggerFactory(logger))

[<CompiledName("Config")>]
let config = { ElmConfig.Default with LogConsole = true; Measure = true }
