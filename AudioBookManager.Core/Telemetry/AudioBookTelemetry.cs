using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AudioBookManager.Core.Telemetry
{
    /// <summary>
    /// Centralized telemetry for the AudioBookManager solution.
    /// Provides ActivitySource for distributed tracing and Meter for metrics.
    /// </summary>
    public static class AudioBookTelemetry
    {
        public const string ServiceName = "AudioBookManager";
        public const string ServiceVersion = "1.0.0";

        /// <summary>
        /// ActivitySource for distributed tracing across the solution.
        /// </summary>
        public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

        /// <summary>
        /// Meter for recording metrics across the solution.
        /// </summary>
        public static readonly Meter Meter = new(ServiceName, ServiceVersion);

        // --- Counters ---
        public static readonly Counter<long> BooksLoaded = Meter.CreateCounter<long>(
            "audiobook.books.loaded",
            description: "Total number of books loaded");

        public static readonly Counter<long> BooksProcessed = Meter.CreateCounter<long>(
            "audiobook.books.processed",
            description: "Total number of books processed (renamed)");

        public static readonly Counter<long> FilesTagged = Meter.CreateCounter<long>(
            "audiobook.files.tagged",
            description: "Total number of audio files tagged");

        public static readonly Counter<long> FileTagErrors = Meter.CreateCounter<long>(
            "audiobook.files.tag_errors",
            description: "Total number of file tagging errors");

        public static readonly Counter<long> BookProcessingErrors = Meter.CreateCounter<long>(
            "audiobook.books.processing_errors",
            description: "Total number of books that finished GenerateFolder with at least one error in the ErrorStack");

        public static readonly Counter<long> GoodreadsSearches = Meter.CreateCounter<long>(
            "audiobook.goodreads.searches",
            description: "Total number of Goodreads searches performed");

        public static readonly Counter<long> GoodreadsSearchHits = Meter.CreateCounter<long>(
            "audiobook.goodreads.search_hits",
            description: "Total number of Goodreads searches with results");

        public static readonly Counter<long> GoodreadsSearchMisses = Meter.CreateCounter<long>(
            "audiobook.goodreads.search_misses",
            description: "Total number of Goodreads searches without results");

        public static readonly Counter<long> GoodreadsErrors = Meter.CreateCounter<long>(
            "audiobook.goodreads.errors",
            description: "Total number of Goodreads scraping errors");

        // --- Histograms ---
        public static readonly Histogram<double> BookProcessingDuration = Meter.CreateHistogram<double>(
            "audiobook.books.processing_duration_ms",
            unit: "ms",
            description: "Duration of book processing operations in milliseconds");

        public static readonly Histogram<double> GoodreadsScrapeDuration = Meter.CreateHistogram<double>(
            "audiobook.goodreads.scrape_duration_ms",
            unit: "ms",
            description: "Duration of Goodreads scraping operations in milliseconds");

        // --- UpDownCounters ---
        public static readonly UpDownCounter<long> ActiveOperations = Meter.CreateUpDownCounter<long>(
            "audiobook.active_operations",
            description: "Number of currently active operations");
    }
}
