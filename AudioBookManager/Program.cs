using OpenTelemetry;
using OpenTelemetry.Trace;
using Sentry;
using Sentry.OpenTelemetry;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioBookManager
{
    static class Program
    {
        public static readonly ActivitySource MainActivitySource = new ActivitySource("AudioBookManager");
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Init the Sentry SDK
            SentrySdk.Init(o =>
            {
                // Tells which project in Sentry to send events to:
                o.Dsn = "https://b948b2e518c63226438656e8375a5c32@sentry.pakato.org/2";
                // When configuring for the first time, to see what the SDK is doing:
                o.Debug = true;
                o.AutoSessionTracking = true;
                o.StackTraceMode = StackTraceMode.Enhanced;
                o.SendDefaultPii = true;
                o.TracesSampleRate = 1.0;
                o.UseOpenTelemetry();
            });
            Log.Logger = new LoggerConfiguration()
            .Enrich.WithSpan()
            .WriteTo.Sentry(o =>
            {
                o.Dsn = "https://b948b2e518c63226438656e8375a5c32@sentry.pakato.org/2";
                o.InitializeSdk = false; // Already initialized above
                // Debug and higher are stored as breadcrumbs (default is Information)
                o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
                // Warning and higher is sent as event (default is Error)
                o.MinimumEventLevel = LogEventLevel.Warning;
            })
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();

            // Configure WinForms to throw exceptions so Sentry can capture them.
            //Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(MainActivitySource.Name)
            .AddSentry().Build();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AudioBookManager());
            //tracerProvider.ForceFlush();
        }
    }
}
