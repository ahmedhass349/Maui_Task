using Maui_Task.Shared.Services.Interfaces;
using System.IO;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Services
{
    public class WebDatabasePathProvider : IDatabasePathProvider
    {
        private readonly IWebHostEnvironment _env;

        public WebDatabasePathProvider(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string GetDatabasePath()
        {
            return Path.Combine(_env.ContentRootPath, "Maui_Task.Shared.db");
        }
    }
}
