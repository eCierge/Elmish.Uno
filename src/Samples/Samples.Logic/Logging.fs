[<AutoOpen>]
module Logging

open Serilog
open Serilog.Extensions.Logging

let logger =
  LoggerConfiguration()
    .MinimumLevel.Override("Elmish.Uno.Update", Events.LogEventLevel.Verbose)
    .MinimumLevel.Override("Elmish.Uno.Bindings", Events.LogEventLevel.Verbose)
    .MinimumLevel.Override("Elmish.Uno.Performance", Events.LogEventLevel.Verbose)
    .WriteTo.Console()
    .CreateLogger()
