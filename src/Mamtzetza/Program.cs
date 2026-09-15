using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mamtzetza;
using Prometheus;

var builder = Host.CreateApplicationBuilder(args);

// Configure Structured JSON Logging for Fluentd, Fluent Bit, and Elasticsearch
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false };
});

builder.Configuration.AddEnvironmentVariables();

var componentOptions = new ComponentOptions();
builder.Configuration.GetSection(ComponentOptions.SectionName).Bind(componentOptions);

// Direct environment variable overrides for simple container configuration
if (Environment.GetEnvironmentVariable("RABBITMQ_HOST") is { Length: > 0 } rabbitHost)
    componentOptions.RabbitMqHost = rabbitHost;

if (Environment.GetEnvironmentVariable("RABBITMQ_PORT") is { Length: > 0 } rabbitPortStr && int.TryParse(rabbitPortStr, out var rabbitPort))
    componentOptions.RabbitMqPort = rabbitPort;

if (Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") is { Length: > 0 } rabbitUser)
    componentOptions.RabbitMqUsername = rabbitUser;

if (Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") is { Length: > 0 } rabbitPass)
    componentOptions.RabbitMqPassword = rabbitPass;

if (Environment.GetEnvironmentVariable("INPUT_EXCHANGE") is { Length: > 0 } inputExchange)
    componentOptions.InputExchange = inputExchange;

if (Environment.GetEnvironmentVariable("INPUT_QUEUE") is { Length: > 0 } inputQueue)
    componentOptions.InputQueue = inputQueue;

if (Environment.GetEnvironmentVariable("OUTPUT_EXCHANGE") is { Length: > 0 } outputExchange)
    componentOptions.OutputExchange = outputExchange;

if (Environment.GetEnvironmentVariable("ENABLE_EXTERNAL_API") is { Length: > 0 } enableApiStr && bool.TryParse(enableApiStr, out var enableApi))
    componentOptions.EnableExternalApi = enableApi;

if (Environment.GetEnvironmentVariable("EXTERNAL_API_BASE_URL") is { Length: > 0 } apiUrl)
    componentOptions.ExternalApiBaseUrl = apiUrl;

if (Environment.GetEnvironmentVariable("METRICS_PORT") is { Length: > 0 } metricsPortStr && int.TryParse(metricsPortStr, out var metricsPort))
    componentOptions.MetricsPort = metricsPort;

builder.Services.Configure<ComponentOptions>(options =>
{
    options.RabbitMqHost = componentOptions.RabbitMqHost;
    options.RabbitMqPort = componentOptions.RabbitMqPort;
    options.RabbitMqUsername = componentOptions.RabbitMqUsername;
    options.RabbitMqPassword = componentOptions.RabbitMqPassword;
    options.InputExchange = componentOptions.InputExchange;
    options.InputQueue = componentOptions.InputQueue;
    options.InputRoutingKey = componentOptions.InputRoutingKey;
    options.OutputExchange = componentOptions.OutputExchange;
    options.OutputRoutingKey = componentOptions.OutputRoutingKey;
    options.EnableExternalApi = componentOptions.EnableExternalApi;
    options.ExternalApiBaseUrl = componentOptions.ExternalApiBaseUrl;
    options.MetricsPort = componentOptions.MetricsPort;
});

builder.Services.AddHttpClient<IFireflyTransformer, FireflyTransformer>(client =>
{
    client.BaseAddress = new Uri(componentOptions.ExternalApiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHostedService<MamtzetzaWorker>();

// Start Prometheus metrics HTTP server (exposes /metrics)
var metricServer = new MetricServer(port: componentOptions.MetricsPort);
metricServer.Start();

var host = builder.Build();
host.Run();
