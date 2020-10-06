# MD-Branch Updates
I just wanted to add some logging and experiment with measuring application exceptions and edges cases.

This logging will let us know how many users tried to do things they can't, and how many exceptions are being handled.

## Added 1 new helper class for logging exceptions and events
Helpers/TelemetryHelper.cs  -  Singleton helper class for logging in Azure.  Helper classes for logging exceptions and custom events.

## Added 2 new environment variables:
"TelemetryAppKey": "GUID"   -  This is the unique id of the Azure Application Insights instrument key.

"TelemetryENV": "DEV"  - This is the environment metadata associated with each log entry.

## Example C# code snipets:
TelemetryHelper.Singleton.LogEvent("BOT COMMAND", "loan", ctx);

TelemetryHelper.Singleton.LogEvent("BOT TASK", "task-loan-find-not", ctx);

TelemetryHelper.Singleton.LogException("task-loan-pay", e);









