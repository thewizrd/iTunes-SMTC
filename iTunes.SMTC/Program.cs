using iTunes.SMTC.Keys;
using Sentry.Extensibility;
using Sentry.Protocol;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.Json;

namespace iTunes.SMTC
{
    internal static class Program
    {
        private static Mutex _mutex = null;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            const string appName = "iTunes.SMTC";

            _mutex = new Mutex(true, appName, out bool createdNew);

            if (createdNew)
            {
                ApplicationConfiguration.Initialize();

                // Configure WinForms to throw exceptions so Sentry can capture them.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

                // Init the Sentry SDK
                var sentryOptions = new SentryOptions()
                {
                    // Tells which project in Sentry to send events to:
                    Dsn = SentryConfig.GetDsn(),
#if DEBUG
                    // When configuring for the first time, to see what the SDK is doing:
                    Debug = true,
#endif
                };

                // Limit exceptions captured
                sentryOptions.AddExceptionFilter(new ExceptionFilter());

                using (SentrySdk.Init(sentryOptions))
                {
                    Application.Run(new SettingsUi());
                }
            }
        }

        private class ExceptionFilter : IExceptionFilter
        {
            public bool Filter(Exception ex)
            {
                // Don't filter unhandled exceptions of any type
                if (ex.Data is not null && ex.Data.Contains(Mechanism.HandledKey) && ex.Data[Mechanism.HandledKey] is false)
                {
                    return false;
                }

                if (ex is IOException && ex.Message?.Contains("HTTP") == true)
                {
                    return true;
                }

                if (ex is JsonException || ex is HttpRequestException ||
                    ex is WebException || ex is COMException ||
                    ex is FileNotFoundException ||
                    ex is TaskCanceledException || ex is TimeoutException)
                {
                    return true;
                }

                return false;
            }
        }
    }
}