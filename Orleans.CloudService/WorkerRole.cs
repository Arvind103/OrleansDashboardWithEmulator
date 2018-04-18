using Microsoft.WindowsAzure.ServiceRuntime;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TestGrains;

namespace Orleans.CloudService
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private ISiloHost siloHots;
        int siloPort = 11111;
        int gatewayPort = 30000;

        public override void Run()
        {
            Trace.TraceInformation("Orleans.CloudService is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("Orleans.CloudService has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Orleans.CloudService is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Orleans.CloudService has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            var siloPort = 11111;
            int gatewayPort = 30000;
            var siloAddress = IPAddress.Loopback;

            var silo =
                new SiloHostBuilder()
                    .UseDashboard(options =>
                    {
                        options.HostSelf = true;
                        options.HideTrace = false;
                    })
                    .UseDevelopmentClustering(options => options.PrimarySiloEndpoint = new IPEndPoint(siloAddress, siloPort))
                    .UseInMemoryReminderService()
                    .ConfigureEndpoints(siloAddress, siloPort, gatewayPort)
                    .Configure<ClusterOptions>(options => options.ClusterId = "helloworldcluster")
                    .ConfigureApplicationParts(appParts => appParts.AddApplicationPart(typeof(TestCalls).Assembly))
                    .Build();

            await silo.StartAsync();

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }

            await silo.StopAsync();
        }
    }
}
