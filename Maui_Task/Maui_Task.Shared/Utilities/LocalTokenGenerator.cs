using System;
using System.Security.Cryptography;

namespace Maui_Task.Shared.Utilities
{
    public static class LocalTokenGenerator
    {
        public static string Generate()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }
    }
}
