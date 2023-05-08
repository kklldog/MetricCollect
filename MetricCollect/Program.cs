using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;

namespace MetricCollect
{
    public class Program
    {
        public static void Main(string[] args)
        {

            Task.Run(() =>
            {
                var providers = new List<EventPipeProvider>()
                {
                    new EventPipeProvider(
                        "System.Runtime",
                        EventLevel.Informational,
                        long.MaxValue,
                        new Dictionary<string, string>() {
                            { "EventCounterIntervalSec", "5" }
                        }
                                )
                };
                var processId = DiagnosticsClient.GetPublishedProcesses().First();
                var client = new DiagnosticsClient(processId);
                using (var session = client.StartEventPipeSession(providers))
                {
                    var source = new EventPipeEventSource(session.EventStream);
                    source.Dynamic.All += (TraceEvent obj) =>
                    {
                        if (obj.EventName.Equals("EventCounters"))
                        {
                            // I know this part is ugly. But this is all TraceEvent.
                            IDictionary<string, object> payloadVal = (IDictionary<string, object>)(obj.PayloadValue(0));
                            IDictionary<string, object> payloadFields = (IDictionary<string, object>)(payloadVal["Payload"]);
                            if (payloadFields["Name"].ToString().Equals("cpu-usage"))
                            {
                                Console.WriteLine(DateTime.Now);
                            }
                        }
                    };
                    try
                    {
                        source.Process();
                    }
                    catch { }

                }
            }
            );

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }

    }
}