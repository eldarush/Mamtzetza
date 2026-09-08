# OmegaFireflyComponent

A demo microservice designed for teaching integration testing with **QaaS** (Quality-as-a-Service).

The service ingests binary serialized **Protocol Buffers** messages (`OmegaSolider`), transforms the data according to business and game logic, optionally enriches the entity using an external HTTP API governed by a feature flag, and emits a transformed Protocol Buffers message (`FireflyExpert`) to RabbitMQ.

---

## Architecture & Workflow

```mermaid
flowchart LR
    subgraph RabbitMQ
        IN[Queue: omega-solider-input-queue<br/>Exchange: omega-solider-input]
        OUT[Exchange: firefly-expert-output]
    end

    subgraph OmegaFireflyComponent
        WORKER[OmegaFireflyWorker] -->|Deserialize| LOGIC[FireflyTransformer]
        LOGIC -->|Serialize| PUB[Publish FireflyExpert]
    end

    subgraph External Mock API [Optional: ENABLE_EXTERNAL_API=true]
        LOGIC -.->|GET /api/v1/buff/{soldierId}| API_GET[Buff Endpoint]
        LOGIC -.->|POST /api/v1/funny-title| API_POST[Title Endpoint]
    end

    IN --> WORKER
    PUB --> OUT
```

---

## Message Schemas

The service uses the [`OmegaSolider`](https://github.com/eldarush/OmegaSolider) protobuf package.

### Input: `OmegaSolider`
```protobuf
message OmegaSolider {
    string soldier_id = 1;     // Unique identifier (e.g. "SOL-001")
    string codename = 2;       // Soldier's field callsign (e.g. "Bravo-1")
    int32 rank_level = 3;      // Rank level (1 to 5)
    int32 bravery_points = 4;  // Bravery score (e.g. 50)
    string favorite_snack = 5; // Tactical ration (e.g. "Quantum Doritos")
}
```

### Output: `FireflyExpert`
```protobuf
message FireflyExpert {
    string soldier_id = 1;         // Preserved from OmegaSolider
    string codename = 2;           // Preserved from OmegaSolider
    int32 rank_level = 3;          // Preserved from OmegaSolider
    int32 glow_intensity = 4;      // Calculated power/luminescence
    string expertise = 5;          // "Expert in {favorite_snack} Logistics"
    string comedic_buff = 6;       // Comedic enhancement string
    string secret_mission = 7;     // "Operation Glow-{soldier_id}"
    int64 processed_at_unix_ms = 8;// Millisecond timestamp
}
```

---

## Business Transformation Logic

1. **Base Glow Calculation**:
   $$\text{GlowIntensity} = (\text{RankLevel} \times 10) + (\text{BraveryPoints} \times 2)$$
2. **Metadata Formulation**:
   - `Expertise`: `Expert in {FavoriteSnack} Logistics`
   - `SecretMission`: `Operation Glow-{SoldierId}`
3. **Feature Flag (`ENABLE_EXTERNAL_API`)**:
   - **When disabled (`false`)**:
     - `ComedicBuff` is set to `"Unbuffed Normal Firefly (No API)"`.
     - `GlowIntensity` remains at base glow.
     - No outbound HTTP requests are made.
   - **When enabled (`true`)**:
     - Sends `GET /api/v1/buff/{soldierId}`: retrieves `buffName` and `bonusGlow`.
     - Sends `POST /api/v1/funny-title` with `{ soldierId, codename, favoriteSnack }`: retrieves `title` and `funnyLore`.
     - `GlowIntensity += bonusGlow`.
     - `ComedicBuff = $"{buffName} - {title}"`.

---

## Configuration

All options can be configured via environment variables:

| Variable | Default Value | Description |
| :--- | :--- | :--- |
| `RABBITMQ_HOST` | `127.0.0.1` | RabbitMQ broker address |
| `RABBITMQ_PORT` | `5672` | RabbitMQ broker port |
| `RABBITMQ_USERNAME` | `admin` | RabbitMQ username |
| `RABBITMQ_PASSWORD` | `admin` | RabbitMQ password |
| `INPUT_EXCHANGE` | `omega-solider-input` | RabbitMQ exchange to bind input queue to |
| `INPUT_QUEUE` | `omega-solider-input-queue` | RabbitMQ queue consumed by this worker |
| `OUTPUT_EXCHANGE` | `firefly-expert-output` | RabbitMQ exchange where output is published |
| `ENABLE_EXTERNAL_API` | `false` | Feature flag to enable external HTTP API enrichment |
| `EXTERNAL_API_BASE_URL` | `http://127.0.0.1:8080` | Base URL of the external HTTP service |

---

## Running Locally

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- RabbitMQ broker running on `127.0.0.1:5672`

### Run Service
```bash
dotnet restore
dotnet run --project src/OmegaFireflyComponent/OmegaFireflyComponent.csproj
```

### Run Unit Tests
```bash
dotnet test
```

---

## Running with Docker

### Build Image
```bash
docker build -t omega-firefly-component:latest .
```

### Run Container
```bash
docker run -d \
  --name omega-firefly-component \
  -e RABBITMQ_HOST=host.docker.internal \
  -e ENABLE_EXTERNAL_API=true \
  -e EXTERNAL_API_BASE_URL=http://host.docker.internal:8080 \
  omega-firefly-component:latest
```
