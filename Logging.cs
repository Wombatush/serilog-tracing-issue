using System.Reflection;
using Serilog;
using Serilog.Enrichers.Span;
using SerilogTracing;

namespace SerilogTrace;

public static class Logging
{
    public static LoggerConfiguration Configure(this LoggerConfiguration self)
    {
        self = self
            .MinimumLevel.Debug()
            // .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
            // .MinimumLevel.Override("System", LogEventLevel.Debug)
            // .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Debug)
            // .MinimumLevel.Override("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogEventLevel.Information)
            //.Filter.ByExcluding(Matching.WithProperty<string>("SourceContext", name => name == "Microsoft.Azure.Functions.Worker"))
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", Assembly.GetEntryAssembly()?.GetName().Name!)
            .Enrich.WithProperty("Environment", "LOCAL")
            .Enrich.WithSpan()
            .WriteTo.Console()
            .WriteTo.Trace()
            .WriteTo.AzureApp();

        // var seqUrl = Environment.GetEnvironmentVariable(WellKnownSettings.Logging.Seq.Url);
        // if (!string.IsNullOrEmpty(seqUrl))
        // {
        //     var seqKey = Environment.GetEnvironmentVariable(WellKnownSettings.Logging.Seq.Key);
        //     self = self.WriteTo.Seq(seqUrl, apiKey: seqKey);
        // }

        self = self.WriteTo.Seq("http://localhost:5341");
        
        return self;
    }
        
    public static IDisposable UseSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .Configure()
            .CreateBootstrapLogger();

        return new CloseAndFlush();
    }

    private sealed class CloseAndFlush : IDisposable
    {
        private readonly IDisposable _tracer = new ActivityListenerConfiguration()
            // .Instrument.AspNetCoreRequests()
            // .Instrument.SqlClientCommands()
            .TraceToSharedLogger();
        
        public void Dispose()
        {
            _tracer.Dispose();
            
            Log.CloseAndFlush();
        }
    }
}