using System.IO;
using Microsoft.Maui.Storage;
using Maui_Task.Shared.Services.Interfaces;

namespace Maui_Task.Services
{
    public class MauiDatabasePathProvider : IDatabasePathProvider
    {
        public string GetDatabasePath()
        {
            var dbName = "taskflow.db";
            return Path.Combine(FileSystem.AppDataDirectory, dbName);
        }
    }
}
