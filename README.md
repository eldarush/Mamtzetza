# Mamtzetza

**Mamtzetza** is an event-driven worker microservice built on .NET 10. It consumes Protobuf military soldier records from a RabbitMQ input queue, transforms and enriches them into elite firefly expert units, and publishes the resulting Protobuf records to a RabbitMQ output exchange. When enabled via a feature flag, it queries an external HTTP REST API to apply comedic buffs and titles.

Mamtzetza is built for high-observability environments, emitting structured JSON logs for Fluentd/Fluent Bit/Elasticsearch and exposing Prometheus metrics at `/metrics`.

---

## 1. What It Does

1. **Consumes Messages**: Listens to an incoming RabbitMQ queue for serialized `OmegaSolider` Protobuf messages.
2. **Computes Deterministic Transformations**:
   - **`favorite_technology`**: Derived from `favorite_tv_show` (e.g. Star Trek/Wars -> `"Antimatter Warp Core"`, Expanse -> `"Epstein Fusion Drive"`, Matrix -> `"Neural Direct Link"`, Doctor Who -> `"TARDIS Chrono-Engine"`, Cyberpunk -> `"Sandevistan Neural Implant"`).
   - **`favorite_team`**: Derived from `origin_planet` (e.g. Mars -> `"Martian Dust Devils"`, Earth -> `"Terran Cyber Knights"`, Jupiter -> `"Great Red Spot Cyclones"`).
   - **`favorite_commander`**: Derived from `rank` (e.g. General/Commander -> `"General Kenobi"`, Captain -> `"Captain Jean-Luc Picard"`, Sergeant/Major -> `"Sergeant Avery Johnson"`).
   - **`favorite_woman`**: Derived from `lucky_number % 5` (0 -> `"Ada Lovelace"`, 1 -> `"Marie Curie"`, 2 -> `"Grace Hopper"`, 3 -> `"Margaret Hamilton"`, 4 -> `"Hedy Lamarr"`).
   - **`favorite_coding_language`**: Derived from `age` (<25 -> `"Rust"`, 25-34 -> `"C#"`, 35-44 -> `"Python"`, 45-54 -> `"C++"`, >=55 -> `"LISP"`).
   - **`glow_intensity`**: `(int)(height + weight * 0.5f) + (|lucky_number| % 10) + bonusGlow`.
3. **Optional External API Enrichment** (controlled by `ENABLE_EXTERNAL_API`):
   - When **disabled** (`false`): Comedic buff is set to `"Unbuffed Normal Firefly (No API)"` and `bonusGlow = 0`.
   - When **enabled** (`true`):
     - Calls `GET /api/v1/buff/{soldierId}` to obtain an RPG buff and bonus glow points.
     - Calls `POST /api/v1/funny-title` with soldier metadata to obtain a humorous title.
     - Glow Intensity is incremented by `bonusGlow`, and Comedic Buff is formatted as `"{buffName} - {title}"`.
     - In case of API failure, gracefully falls back to `"API Error Fallback: {message}"`.
4. **Publishes Results**: Emits serialized `FireflyExpert` Protobuf messages to the destination exchange.
5. **Observability**:
   - Structured JSON logs printed to stdout (easily ingested by Fluent Bit / Fluentd and forwarded to Elasticsearch).
   - Prometheus metrics server running on port `9090` (or `METRICS_PORT`), exposing counters and processing duration histograms.

---

## 2. Configuration

Configure **Mamtzetza** using environment variables:

| Variable | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `RABBITMQ_HOST` | `string` | `127.0.0.1` | Hostname or IP of the RabbitMQ broker |
| `RABBITMQ_PORT` | `int` | `5672` | RabbitMQ AMQP port |
| `RABBITMQ_USERNAME` | `string` | `admin` | RabbitMQ username |
| `RABBITMQ_PASSWORD` | `string` | `admin` | RabbitMQ password |
| `INPUT_EXCHANGE` | `string` | `omega-solider-input` | Exchange to consume from |
| `INPUT_QUEUE` | `string` | `omega-solider-input-queue` | Queue bound to the input exchange |
| `OUTPUT_EXCHANGE` | `string` | `firefly-expert-output` | Destination exchange for processed messages |
| `ENABLE_EXTERNAL_API` | `bool` | `false` | Feature flag toggling HTTP REST API enrichment |
| `EXTERNAL_API_BASE_URL` | `string` | `http://127.0.0.1:8080` | Base URL of the external REST API |
| `METRICS_PORT` | `int` | `9090` | HTTP port for Prometheus metrics scrape endpoint (`/metrics`) |

---

## 3. Observability Architecture: Structured Logging & Prometheus Metrics

Mamtzetza is engineered from the ground up to support modern cloud-native observability standards.

### 3.1 Structured JSON Logging

#### How It Is Configured in Code:
In `src/Mamtzetza/Program.cs`, the standard console logger is replaced with Microsoft's high-performance JSON console formatter:

```csharp
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false };
});
```

#### Output Schema:
Every log message emitted by `Mamtzetza` outputs as a single, unindented JSON line to `stdout`:

```json
{
  "Timestamp": "2026-09-15T12:52:57.658Z ",
  "EventId": 0,
  "LogLevel": "Information",
  "Category": "Mamtzetza.MamtzetzaWorker",
  "Message": "Published transformed FireflyExpert: SoldierId=SOL-001, Glow=227",
  "State": {
    "SoldierId": "SOL-001",
    "Glow": 227,
    "{OriginalFormat}": "Published transformed FireflyExpert: SoldierId={SoldierId}, Glow={Glow}"
  },
  "Scopes": []
}
```

#### How It Integrates with Fluent Bit, Fluentd, and Elasticsearch:
1. **Container Capture**: In Docker/Kubernetes, the runtime captures container `stdout`/`stderr` into standard JSON-file log buffers.
2. **Fluent Bit / Fluentd Parsing**: Because logs are already emitted in structured JSON format, the log collector does **not** need complex, brittle regex filters. Fluent Bit parses each line using its built-in JSON parser or reads Docker log metadata directly.
3. **Shipping to Elasticsearch**:
   In `observability/fluent-bit.conf`, logs matching `*` are shipped directly to Elasticsearch:
   ```ini
   [OUTPUT]
       Name             es
       Match            *
       Host             elasticsearch
       Port             9200
       Index            mamtzetza-logs
       Type             _doc
       Suppress_Type_Name On
   ```
4. **Searchability**: Fields like `State.SoldierId`, `LogLevel`, `Category`, and `Message` are indexed natively in Elasticsearch and can be queried instantly in Kibana (e.g. `State.SoldierId: "SOL-001" AND LogLevel: "Error"`).

---

### 3.2 Prometheus Metrics

#### How It Is Configured in Code:
Prometheus integration uses the official `prometheus-net` package. In `src/Mamtzetza/Program.cs`:

```csharp
// Read METRICS_PORT from environment (defaults to 9090)
if (Environment.GetEnvironmentVariable("METRICS_PORT") is { Length: > 0 } metricsPortStr && int.TryParse(metricsPortStr, out var metricsPort))
    componentOptions.MetricsPort = metricsPort;

// Start standalone Prometheus HTTP metric server on /metrics
var metricServer = new Prometheus.MetricServer(port: componentOptions.MetricsPort);
metricServer.Start();
```

#### Defined Operational Metrics (`src/Mamtzetza/MamtzetzaMetrics.cs`):

| Metric Name | Type | Description |
| :--- | :--- | :--- |
| `mamtzetza_messages_received_total` | Counter | Incremented every time an `OmegaSolider` Protobuf message is consumed from RabbitMQ. |
| `mamtzetza_messages_processed_total` | Counter | Incremented every time a transformed `FireflyExpert` is successfully published to RabbitMQ. |
| `mamtzetza_processing_errors_total` | Counter | Incremented on any unhandled processing or publishing failure. |
| `mamtzetza_external_api_calls_total` | Counter (labels: `status`) | Tracks external HTTP REST calls with labels `status="success"` or `status="failure"`. |
| `mamtzetza_processing_duration_seconds` | Histogram | Tracks transformation latency with exponential duration buckets ($0.001s$ to $\approx 1s$). |

#### How Prometheus Scrapes Mamtzetza:
In `observability/prometheus.yml`, Prometheus is configured with a scrape job targeting Mamtzetza:

```yaml
scrape_configs:
  - job_name: 'mamtzetza'
    scrape_interval: 5s
    static_configs:
      - targets: ['mamtzetza:9090']
        labels:
          app: 'mamtzetza'
```

#### Key PromQL Queries for Monitoring & Dashboards:
- **Throughput (Messages / Sec)**:
  `rate(mamtzetza_messages_processed_total[1m])`
- **Error Rate**:
  `rate(mamtzetza_processing_errors_total[1m])`
- **External API Degradation Ratio**:
  `rate(mamtzetza_external_api_calls_total{status="failure"}[1m]) / rate(mamtzetza_external_api_calls_total[1m])`
- **95th Percentile Processing Duration (p95)**:
  `histogram_quantile(0.95, rate(mamtzetza_processing_duration_seconds_bucket[5m]))`

---

## 4. Installation & Running

### Option A: Running with Docker

1. **Build the Docker image**:
   ```bash
   docker build -t mamtzetza:latest .
   ```

2. **Run the container**:
   ```bash
   docker run -d \
     --name mamtzetza \
     -p 9090:9090 \
     -e RABBITMQ_HOST=rabbitmq \
     -e RABBITMQ_PORT=5672 \
     -e RABBITMQ_USERNAME=admin \
     -e RABBITMQ_PASSWORD=admin \
     -e ENABLE_EXTERNAL_API=true \
     -e EXTERNAL_API_BASE_URL=http://mocker:8080 \
     -e METRICS_PORT=9090 \
     mamtzetza:latest
   ```

### Option B: Docker Compose Example

```yaml
services:
  mamtzetza:
    image: mamtzetza:latest
    build: .
    ports:
      - "9090:9090"
    environment:
      RABBITMQ_HOST: rabbitmq
      RABBITMQ_PORT: 5672
      RABBITMQ_USERNAME: admin
      RABBITMQ_PASSWORD: admin
      INPUT_EXCHANGE: omega-solider-input
      INPUT_QUEUE: omega-solider-input-queue
      OUTPUT_EXCHANGE: firefly-expert-output
      ENABLE_EXTERNAL_API: "true"
      EXTERNAL_API_BASE_URL: http://mocker:8080
      METRICS_PORT: 9090
    depends_on:
      rabbitmq:
        condition: service_healthy
```

### Option C: Running Locally via .NET CLI

1. **Ensure RabbitMQ is running**:
   ```bash
   docker run -d -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=admin -e RABBITMQ_DEFAULT_PASS=admin rabbitmq:3-management
   ```

2. **Run Mamtzetza**:
   ```bash
   dotnet run --project src/Mamtzetza/Mamtzetza.csproj
   ```

3. **Run Unit Tests**:
   ```bash
   dotnet test Mamtzetza.slnx
   ```
