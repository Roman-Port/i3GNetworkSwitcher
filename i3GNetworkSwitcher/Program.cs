using i3GNetworkSwitcher.Core;
using i3GNetworkSwitcher.Core.Controller;
using i3GNetworkSwitcher.Core.Notifier;
using i3GNetworkSwitcher.Core.Scheduler;
using i3GNetworkSwitcher.Web;
using i3GNetworkSwitcher.Web.Handlers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher
{
    internal class Program
    {
        private static NetworkData data;
        private static NetworkController controller;
        private static INetworkNotifier notifier;
        private static NetworkScheduler scheduler;
        private static NetworkWebServer http;

        private static Task httpTask;
        private static Task schTask;

        private static readonly TimeSpan CACHE_TIME_RESOURCE = TimeSpan.FromDays(60);
        private static readonly TimeSpan CACHE_TIME_APPLICATION = TimeSpan.FromMinutes(10);

        static void Main(string[] args)
        {
            //Decode arguments
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Must pass config filename as argument. Exiting...");
                return;
            }

            //Load config
            string configFilename = args[0];
            Console.WriteLine($"Loading config from \"{configFilename}\"...");
            try
            {
                data = ConfigInflater.InflateConfig(configFilename);
            } catch (Exception ex)
            {
                Console.WriteLine($"Error initializing config: {ex.Message}");
                return;
            }
            Console.WriteLine($"Inflated config successfully; {data.Sites.Length} sites loaded.");

            //Initialize controller
            controller = new NetworkController(data.Sites);

            //Initialize notifier
            if (data.Email != null)
            {
                Console.WriteLine("Initializing email notifications...");
                notifier = new NetworkEmailer(data.Email);
            } else
            {
                Console.WriteLine("Email notifications disabled.");
                notifier = new ConsoleNotifier();
            }
            

            //Initialize scheduler
            scheduler = new NetworkScheduler(controller, data.EventsFilename, TimeSpan.FromHours(-1));
            scheduler.OnScheduledEventExecuted += Scheduler_OnScheduledEventExecuted;
            scheduler.OnScheduledEventFailed += Scheduler_OnScheduledEventFailed;
            schTask = scheduler.RunAsync(CancellationToken.None);

            //Set up web server
            http = new NetworkWebServer(data.ListenPort);
            http.Start();
            http.AddHandler("/api/info", new ApiInfoHandler(controller));
            http.AddHandler("/api/events", new ApiEventsHandler(controller, scheduler));
            http.AddHandler("/api/modify", new ApiModifyHandler(controller));
            http.AddHandler("/api/status", new ApiStatusHandler(controller));
            http.AddHandler("/", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.index.html", "text/html", CACHE_TIME_APPLICATION));
            http.AddHandler("/assets/style.css", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.style.css", "text/css", CACHE_TIME_APPLICATION));
            http.AddHandler("/assets/main.js", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.main.js", "text/javascript", CACHE_TIME_APPLICATION));
            http.AddHandler("/assets/error.svg", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.error.svg", "image/svg+xml", CACHE_TIME_RESOURCE));
            http.AddHandler("/assets/rx.svg", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.rx.svg", "image/svg+xml", CACHE_TIME_RESOURCE));
            http.AddHandler("/assets/tx.svg", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.tx.svg", "image/svg+xml", CACHE_TIME_RESOURCE));
            http.AddHandler("/assets/close.svg", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.close.svg", "image/svg+xml", CACHE_TIME_RESOURCE));
            http.AddHandler("/assets/loading.svg", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.loading.svg", "image/svg+xml", CACHE_TIME_RESOURCE));
            http.AddHandler("/assets/to.svg", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.to.svg", "image/svg+xml", CACHE_TIME_RESOURCE));
            http.AddHandler("/assets/edit.svg", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.edit.svg", "image/svg+xml", CACHE_TIME_RESOURCE));
            http.AddHandler("/assets/delete.svg", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.delete.svg", "image/svg+xml", CACHE_TIME_RESOURCE));
            http.AddHandler("/assets/add.svg", new ResourceHandler("i3GNetworkSwitcher.Web.Resources.add.svg", "image/svg+xml", CACHE_TIME_RESOURCE));
            httpTask = http.RunAsync(CancellationToken.None);

            //Run
            Console.WriteLine("Ready!");
            Task.WaitAll(httpTask, schTask);
        }

        private static async Task Scheduler_OnScheduledEventExecuted(NetworkScheduler scheduler, ScheduledCommandEvent evt)
        {
            //Log in console
            Console.WriteLine($"[TASK] Executing \"{evt.Description}\"...");

            //Send alert
            notifier.SendAlert(AlertLevel.INFO, $"SIMULCAST CHANGE - {evt.Description}", $"Executing command:\r\n{evt.CreateInfo(controller)}");
        }

        private static async Task Scheduler_OnScheduledEventFailed(NetworkScheduler scheduler, ScheduledCommandEvent evt, Exception ex)
        {
            //Send alert
            notifier.SendAlert(AlertLevel.INFO, $"SIMULCAST ERROR - {evt.Description}", $"Executing command:\r\n{evt.CreateInfo(controller)}\r\nFAILED: {ex.Message}\r\n{ex.StackTrace}");
        }
    }
}
