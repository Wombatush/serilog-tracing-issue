using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Serilog;
using SerilogTracing;

namespace SerilogTrace;

public sealed class CheckFunction
{
    [Function(nameof(CheckFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "GET", Route = "check")]
        HttpRequestData request,
        FunctionContext executionContext)
    {
        using (var a = Log.ForContext(GetType()).StartActivity("IP Check"))
        {
            Log.Information("MIDDLE {Activity}", a.Activity?.Id);
            
            a.Complete();    
        }
            
        Log.Information("AFTER");

        await Task.CompletedTask;

        var response = request.CreateResponse(HttpStatusCode.OK);

        return response;
    }
}