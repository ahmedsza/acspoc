using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ACSFunctions
{
    public class demohttp
    {
        private readonly ILogger<demohttp> _logger;

        public demohttp(ILogger<demohttp> logger)
        {
            _logger = logger;
        }

        [Function("demohttp")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
