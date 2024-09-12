using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Policy;

namespace Main.Controllers
{
    [ApiController]
    [Route("test")]
    public class TestController : Controller
    {
        private class TestModel
        {
            public string ValueString { get; set; } = string.Empty;
            public double ValueDouble { get; set; }
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("debugjson")]
        public IActionResult DebugJson()
        {
            var result = new JsonResult(
                new TestModel()
                {
                    ValueString = "this is a test",
                    ValueDouble = 42.1
                });

            return result;
        }

        [HttpGet]
        [Route("debugstatuscodes")]
        public IActionResult DebugStatusCodes(string statusCode)
        {
            var result = new StatusCodeResult((int)HttpStatusCode.InternalServerError);

            var code = HttpStatusCode.InternalServerError;
            if (Enum.TryParse(statusCode, out code))
            {
                result = new StatusCodeResult((int)code);
            }

            return result;
        }

        [HttpGet]
        [Route("debugtimeout")]
        public IActionResult DebugTimeout(int timeoutSeconds)
        {
            Task.Delay(new TimeSpan(0, 0, timeoutSeconds));

            return new StatusCodeResult((int)HttpStatusCode.OK);
        }
    }
}
