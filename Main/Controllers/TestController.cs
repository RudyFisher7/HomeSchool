using Microsoft.AspNetCore.Mvc;
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
    }
}
