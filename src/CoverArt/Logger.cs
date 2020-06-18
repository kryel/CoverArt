using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace CoverArt
{
    internal class Logger
    {
        private static readonly ILoggerFactory LoggerFactory = new LoggerFactory();

        public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

        public static void Init()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            LoggerFactory.AddNLog();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static void Shutdown()
        {
            LoggerFactory.Dispose();
        }
    }
}
