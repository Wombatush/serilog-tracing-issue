using System.Diagnostics;
using Grpc.Core;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SerilogTrace;

public static class Program
{
    private static readonly ActivitySource ActivitySource = new(typeof(Program).Assembly.GetName().Name!); 
    
    public static async Task Main()
    {
        using (Logging.UseSerilog())
        {
            try
            {
                var host = new HostBuilder()
                    /*
                     * Configure additional logging.
                     */
                    .UseSerilog((context, services, configuration) =>
                    {
                        configuration.Configure();
                        configuration.ReadFrom.Configuration(context.Configuration);
                        configuration.ReadFrom.Services(services);
                        
                        var telemetry = services.GetService<TelemetryConfiguration>();
                        if (telemetry != null)
                        {
                            configuration.WriteTo.ApplicationInsights(telemetry, TelemetryConverter.Traces);
                        }
                    })
                    /*
                     * Configure application using .json documents.
                     */
                    /*
                     * Azure Console overrides settings via environment variables.
                     */
                    .ConfigureAppConfiguration(x => x
                        .AddEnvironmentVariables())
                    /*
                     * Configure the hosting.
                     */
                    .ConfigureFunctionsWorkerDefaults()
                    .ConfigureServices((context, services) =>
                    {
                        services.AddHttpClient();
                        
                        services.AddSingleton(ActivitySource);
                    })
                    .Build();

                await host.RunAsync()
                    .ConfigureAwait(false);
            }
            catch (RpcException e)
            {
                // This exception is caused by the VNet during redeployment
                // https://github.com/Azure/azure-functions-dotnet-worker/issues/518
                Log.Debug(e, "Fatal unhandled exception occurred, the process is terminated");
                throw;
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Fatal unhandled exception occurred, the process is terminated");
                throw;
            }          
        }
    }
}
