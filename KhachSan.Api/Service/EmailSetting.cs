using Microsoft.AspNetCore.Mvc;

namespace KhachSan.Api.Service
{
    public class EmailSetting : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public class EmailSettings
        {
            public string SmtpServer { get; set; } = string.Empty;
            public int SmtpPort { get; set; }
            public string SenderEmail { get; set; } = string.Empty;
            public string SenderPassword { get; set; } = string.Empty;
            public string SenderName { get; set; } = string.Empty;
        }
    }
}
