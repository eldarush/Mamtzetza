using Prometheus;

namespace Mamtzetza;

public static class MamtzetzaMetrics
{
    public static readonly Counter MessagesReceivedTotal = Metrics.CreateCounter(
        "mamtzetza_messages_received_total",
        "Total number of incoming OmegaSolider messages received from RabbitMQ.");

    public static readonly Counter MessagesProcessedTotal = Metrics.CreateCounter(
        "mamtzetza_messages_processed_total",
        "Total number of FireflyExpert messages successfully transformed and published.");

    public static readonly Counter MessageProcessingErrorsTotal = Metrics.CreateCounter(
        "mamtzetza_processing_errors_total",
        "Total number of message processing failures.");

    public static readonly Counter ExternalApiCallsTotal = Metrics.CreateCounter(
        "mamtzetza_external_api_calls_total",
        "Total number of external enrichment API calls made.",
        new[] { "status" });

    public static readonly Histogram ProcessingDurationSeconds = Metrics.CreateHistogram(
        "mamtzetza_processing_duration_seconds",
        "Histogram of processing durations for transforming soldiers into fireflies.",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(start: 0.001, factor: 2, count: 10)
        });
}
