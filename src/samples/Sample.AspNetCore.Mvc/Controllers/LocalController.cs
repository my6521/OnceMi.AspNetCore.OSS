using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OnceMi.AspNetCore.OSS;

namespace Sample.AspNetCore.Mvc.Controllers
{
    public class LocalController : BaseOSSController
    {
        private readonly ILogger<LocalController> _logger;

        public LocalController(ILogger<LocalController> logger
            , IOSSServiceFactory ossServiceFactory) : base(logger)
        {
            _logger = logger;
            _ossService = ossServiceFactory.Create("local");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}