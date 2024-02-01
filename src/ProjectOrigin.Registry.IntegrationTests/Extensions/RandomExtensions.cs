using System;

namespace ProjectOrigin.Registry.IntegrationTests.Extensions
{
    public static class RandomExtensions
    {
        public static string GenerateString(this Random r, int length = 10)
        {
            var bytes = new byte[length];
            r.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
