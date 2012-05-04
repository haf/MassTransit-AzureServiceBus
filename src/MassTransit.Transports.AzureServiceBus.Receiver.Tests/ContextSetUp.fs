module ContextSetUp

open NUnit.Framework

[<SetUpFixture>]
type SetUpFixture() =
  [<SetUp>]
  member x.``configure NLog`` () =
    NLog.Config.SimpleConfigurator.ConfigureForConsoleLogging()
    MassTransit.Logging.Logger.UseLogger(new MassTransit.NLogIntegration.Logging.NLogLogger());
