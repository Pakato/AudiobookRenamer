using AudioBookManager.Core.Telemetry;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Sentry;
using Sentry.OpenTelemetry;
using Sentry.Profiling;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioBookManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Init the Sentry SDK
            /*SentrySdk.Init(o =>
            {
                // Tells which project in Sentry to send events to:
                o.Dsn = "https://b948b2e518c63226438656e8375a5c32@sentry.pakato.org/2";
                // When configuring for the first time, to see what the SDK is doing:
                o.Debug = true;
                o.AutoSessionTracking = true;
                o.StackTraceMode = StackTraceMode.Enhanced;
                o.SendDefaultPii = true;
                o.ProfilesSampleRate = 1.0;
                o.TracesSampleRate = 1.0;
                o.AddIntegration(new ProfilingIntegration(
                    // During startup, wait up to 500ms to profile the app startup code.
                    // This could make launching the app a bit slower so comment it out if you
                    // prefer profiling to start asynchronously
                    //TimeSpan.FromMilliseconds(500)
                ));
                o.UseOpenTelemetry();
            });*/

            Log.Logger = new LoggerConfiguration()
            .Enrich.WithSpan()
            /*.WriteTo.Sentry(o =>
            {
                o.Dsn = "https://b948b2e518c63226438656e8375a5c32@sentry.pakato.org/2";
                o.InitializeSdk = false; // Already initialized above
                // Debug and higher are stored as breadcrumbs (default is Information)
                o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
                // Warning and higher is sent as event (default is Error)
                o.MinimumEventLevel = LogEventLevel.Warning;
            })*/
            .WriteTo.OpenTelemetry(o=>
            {
                o.Endpoint ="https://apm.pakato.org/api/3DS8VYPW966dkZSBJgNPp1hJIew";
                o.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf;
                o.Headers.Add("Authorization", "Basic cGFrYXRvQHBha2F0by5vcmc6S3JoUlBQS3U5VkdZMTFEaA==");
            })
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();

            // Configure WinForms to throw exceptions so Sentry can capture them.
            //Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

            // Configure OpenTelemetry Tracing
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(AudioBookTelemetry.ServiceName)
                //.AddSentry()
                .AddConsoleExporter()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri("https://apm.pakato.org/api/3DS8VYPW966dkZSBJgNPp1hJIew");
                    o.Protocol = OtlpExportProtocol.HttpProtobuf;
                    o.HttpClientFactory = () =>
                    {
                        HttpClient client = new HttpClient();
                        client.DefaultRequestHeaders.Add("Authorization", "Basic cGFrYXRvQHBha2F0by5vcmc6S3JoUlBQS3U5VkdZMTFEaA==");
                        return client;
                    };
                })
                .Build();

            // Configure OpenTelemetry Metrics
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter(AudioBookTelemetry.ServiceName)
                .AddConsoleExporter()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri("https://apm.pakato.org/api/3DS8VYPW966dkZSBJgNPp1hJIew");
                    o.Protocol = OtlpExportProtocol.HttpProtobuf;
                    o.HttpClientFactory = () =>
                    {
                        HttpClient client = new HttpClient();
                        client.DefaultRequestHeaders.Add("Authorization", "Basic cGFrYXRvQHBha2F0by5vcmc6S3JoUlBQS3U5VkdZMTFEaA==");
                        return client;
                    };
                })
                .Build();

            Log.Information("AudioBookManager iniciado - OpenTelemetry configurado (Tracing + Metrics + Logging)");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AudioBookManager());

            //meterProvider.ForceFlush();
            //tracerProvider.ForceFlush();
            Log.CloseAndFlush();
        }
    }
}
