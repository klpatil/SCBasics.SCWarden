using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warden;
using Warden.Core;
using Warden.Integrations.HttpApi;
using Warden.Watchers;
using Warden.Watchers.Disk;
using Warden.Watchers.MongoDb;
using Warden.Watchers.MsSql;
using Warden.Watchers.Performance;
using Warden.Watchers.Web;

namespace SCBasics.SCWarden.Service
{
    enum MessageType
    {
        Info,
        Warning,
        Error

    }
    public class WardenService
    {
        private static readonly IWarden Warden = ConfigureWarden();

        public async Task StartAsync()
        {
            LogMessage("Warden service has been started.", MessageType.Info);
            await Warden.StartAsync();
        }

        public async Task PauseAsync()
        {
            await Warden.PauseAsync();
            LogMessage("Warden service has been paused.", MessageType.Info);
        }

        public async Task StopAsync()
        {
            await Warden.StopAsync();
            LogMessage("Warden service has been stopped.", MessageType.Info);
        }

        private static IWarden ConfigureWarden()
        {
            var wardenConfiguration = WardenConfiguration
                .Create();
            if (SCWardenConfigurationManager.IsDiskWatcherEnabled)
            {
                wardenConfiguration.AddDiskWatcher(cfg =>
                  {

                      // Disk Watcher
                      //cfg.WithFilesToCheck(@"D:\Test\File1.txt", @"D:\Test\File2.txt")
                      cfg.WithPartitionsToCheck("D", @"E:\")
                   .WithDirectoriesToCheck(@"D:\Test");
                  });
            }
            if (SCWardenConfigurationManager.IsMongoDBWatcherEnabled)
            {

                // "mongodb://localhost:27017"
                wardenConfiguration.AddMongoDbWatcher(SCWardenConfigurationManager.MongoDBConnectionStringUrl.Url,
                    SCWardenConfigurationManager.MongoDBConnectionStringUrl.DatabaseName, cfg =>
                {
                    // TODO : CHECK STRING INTERPOLATION
                    cfg.WithQuery("OperationStatuses", "{\"InstanceName\": \"{SCWardenConfigurationManager.MongoInstanceNameToCheck}\"}")
                                   .EnsureThat(operationStatuses => operationStatuses.Any(oStatus =>
                                   oStatus.InstanceName == SCWardenConfigurationManager.MongoInstanceNameToCheck));
                });
            }
            if (SCWardenConfigurationManager.IsMSSQLWatcherEnabled)
            {

                wardenConfiguration.AddMsSqlWatcher(SCWardenConfigurationManager.MSSQLConnectionString,
                    cfg =>
                    {
                        cfg.WithQuery("SELECT [Name] FROM [Items] WHERE ID=@id", new Dictionary<string, object> { ["id"] = "11111111-1111-1111-1111-111111111111" })
                                   .EnsureThat(items => items.Any(item => item.Name == "sitecore"));
                    });
            }
            if (SCWardenConfigurationManager.IsPerformanceWatcherEnabled)
            {
                wardenConfiguration.AddPerformanceWatcher(cfg =>
                cfg.EnsureThat(usage => usage.Cpu < SCWardenConfigurationManager.CPUThreshold &&
                usage.Ram < SCWardenConfigurationManager.RAMThreshold),
                    hooks =>
                        hooks.OnCompleted(result => LogMessage(result.WatcherCheckResult.Description, MessageType.Info)));
            }
            //.AddProcessWatcher("mongod")
            //.AddRedisWatcher("localhost", 1, cfg =>
            //{
            //    cfg.WithQuery("get test")
            //        .EnsureThat(results => results.Any(x => x == "test-value"));
            //})
            if (SCWardenConfigurationManager.IsWebWatcherEnabled)
            {
                // TODO : Make it configurable
                wardenConfiguration.AddWebWatcher("http://sc82rev160617/sitecore/service/heartbeat.aspx", hooks =>
                 {
                     hooks.OnStartAsync(check => WebsiteHookOnStartAsync(check))
                         .OnSuccessAsync(check => WebsiteHookOnSuccessAsync(check))
                         .OnCompletedAsync(check => WebsiteHookOnCompletedAsync(check))
                         .OnFailureAsync(check => WebsiteHookOnFailureAsync(check));
                 }, interval: TimeSpan.FromMinutes(10)); // TODO : Configurable
            }
            //.AddServerWatcher("www.google.pl", 80)
            //.AddWebWatcher("http://httpstat.us/200", HttpRequest.Post("users", new { name = "test" },
            //    headers: new Dictionary<string, string>
            //    {
            //        ["User-Agent"] = "Warden",
            //        ["Authorization"] = "Token MyBase64EncodedString"
            //    }), cfg => cfg.EnsureThat(response => response.Headers.Any())
            //)
            //.IntegrateWithMsSql(@"Data Source=.\sqlexpress;Initial Catalog=MyDatabase;Integrated Security=True")
            //Set proper API key or credentials.
            //.IntegrateWithSendGrid("api-key", "noreply@system.com", cfg =>
            //{
            //    cfg.WithDefaultSubject("Monitoring status")
            //        .WithDefaultReceivers("admin@system.com");
            //})
            //.SetAggregatedWatcherHooks((hooks, integrations) =>
            //{
            //    hooks.OnFirstFailureAsync(result =>
            //        integrations.SendGrid().SendEmailAsync("Monitoring errors have occured."))
            //        .OnFirstSuccessAsync(results =>
            //            integrations.SendGrid().SendEmailAsync("Everything is up and running again!"));
            //})
            //Set proper URL of the Warden Web API

            if (SCWardenConfigurationManager.IsWebPanelIntegrationEnabled)
            {
                wardenConfiguration.IntegrateWithHttpApi(SCWardenConfigurationManager.WebPanelURL,
                    SCWardenConfigurationManager.WebPanelAPIKey,
                    SCWardenConfigurationManager.WebPanelOrganizationId);
            }

            wardenConfiguration.SetGlobalWatcherHooks(hooks =>
               {
                   hooks.OnStart(check => GlobalHookOnStart(check))
                       .OnFailure(result => GlobalHookOnFailure(result))
                       .OnSuccess(result => GlobalHookOnSuccess(result))
                       .OnCompleted(result => GlobalHookOnCompleted(result));
               });
            wardenConfiguration.SetHooks((hooks, integrations) =>
            {
                hooks.OnIterationCompleted(iteration => OnIterationCompleted(iteration))
                    //.OnIterationCompletedAsync(iteration => OnIterationCompletedCachetAsync(iteration, integrations.Cachet()))
                    //.OnIterationCompletedAsync(iteration =>
                    //    integrations.Slack().SendMessageAsync($"Iteration {iteration.Ordinal} has completed."))
                    .OnIterationCompletedAsync(iteration => integrations.HttpApi()
                        .PostIterationToWardenPanelAsync(iteration))
                    .OnError(exception => LogMessage(exception.ToString(), MessageType.Error));
                //.OnIterationCompletedAsync(
                //    iteration => OnIterationCompletedMsSqlAsync(iteration, integrations.MsSql()));
            });

            return WardenInstance.Create(wardenConfiguration.Build());
        }

        private static async Task WebsiteHookOnStartAsync(IWatcherCheck check)
        {
            LogMessage($"Invoking the hook OnStartAsync() by watcher: '{check.WatcherName}'.", MessageType.Info);
            await Task.FromResult(true);
        }


        private static async Task WebsiteHookOnSuccessAsync(IWardenCheckResult check)
        {
            var webWatcherCheckResult = (WebWatcherCheckResult)check.WatcherCheckResult;
            LogMessage("Invoking the hook OnSuccessAsync() " +
                              $"by watcher: '{webWatcherCheckResult.WatcherName}'.", MessageType.Info);
            await Task.FromResult(true);
        }

        private static async Task WebsiteHookOnCompletedAsync(IWardenCheckResult check)
        {
            LogMessage("Invoking the hook OnCompletedAsync() " +
                              $"by watcher: '{check.WatcherCheckResult.WatcherName}'.", MessageType.Info);
            await Task.FromResult(true);
        }

        private static async Task WebsiteHookOnFailureAsync(IWardenCheckResult check)
        {
            LogMessage("Invoking the hook OnFailureAsync() " +
                              $"by watcher: '{check.WatcherCheckResult.WatcherName}'.", MessageType.Info);
            await Task.FromResult(true);
        }

        private static void GlobalHookOnStart(IWatcherCheck check)
        {
            LogMessage("Invoking the global hook OnStart() " +
                              $"by watcher: '{check.WatcherName}'.", MessageType.Info);
        }

        private static void GlobalHookOnSuccess(IWardenCheckResult check)
        {
            LogMessage("Invoking the global hook OnSuccess() " +
                              $"by watcher: '{check.WatcherCheckResult.WatcherName}'.", MessageType.Info);
        }

        private static void GlobalHookOnCompleted(IWardenCheckResult check)
        {
            LogMessage("Invoking the global hook OnCompleted() " +
                              $"by watcher: '{check.WatcherCheckResult.WatcherName}'.", MessageType.Info);
        }

        private static void GlobalHookOnFailure(IWardenCheckResult check)
        {
            LogMessage("Invoking the global hook OnFailure() " +
                              $"by watcher: '{check.WatcherCheckResult.WatcherName}'.", MessageType.Info);
        }

        private static void OnIterationCompleted(IWardenIteration wardenIteration)
        {
            var newLine = Environment.NewLine;
            LogMessage($"{wardenIteration.WardenName} iteration {wardenIteration.Ordinal} has completed.", MessageType.Info);
            foreach (var result in wardenIteration.Results)
            {
                LogMessage($"Watcher: '{result.WatcherCheckResult.WatcherName}'{newLine}" +
                                  $"Description: {result.WatcherCheckResult.Description}{newLine}" +
                                  $"Is valid: {result.IsValid}{newLine}" +
                                  $"Started at: {result.StartedAt}{newLine}" +
                                  $"Completed at: {result.CompletedAt}{newLine}" +
                                  $"Execution time: {result.ExecutionTime}{newLine}", MessageType.Info);
            }
        }

        //private static async Task OnIterationCompletedMsSqlAsync(IWardenIteration wardenIteration,
        //    MsSqlIntegration integration)
        //{
        //    await integration.SaveIterationAsync(wardenIteration);
        //}

        //private static async Task OnIterationCompletedCachetAsync(IWardenIteration wardenIteration,
        //    CachetIntegration cachet)
        //{
        //    await cachet.SaveIterationAsync(wardenIteration);
        //}


        private static void LogMessage(string message, MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Info:
                    // TODO : Make it configurable
                    if (SCWardenConfigurationManager.IsInfoLoggingEnabled)
                        Sitecore.Diagnostics.Log.Info(message, typeof(WardenService));
                    break;
                case MessageType.Warning:
                    Sitecore.Diagnostics.Log.Warn(message, typeof(WardenService));
                    break;
                case MessageType.Error:
                    Sitecore.Diagnostics.Log.Error(message, typeof(WardenService));
                    break;
                default:
                    break;
            }
        }
    }

}
