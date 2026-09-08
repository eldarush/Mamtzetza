# Mamtzetza

**Mamtzetza** is an event-driven worker microservice built on .NET 10. It consumes Protobuf military soldier records from a RabbitMQ input queue, transforms and enriches them into elite firefly expert units, and publishes the resulting Protobuf records to a RabbitMQ output exchange. When enabled via a feature flag, it queries an external HTTP REST API to apply comedic buffs and titles.

---

## 1. What It Does

1. **Consumes Messages**: Listens to an incoming RabbitMQ queue for serialized `OmegaSolider` Protobuf messages.
2. **Computes Transformation**:
   - Base Glow Intensity: $\text{RankLevel} \times 10 + \text{BraveryPoints} \times 2$.
   - Expertise: `"Expert in {FavoriteSnack} Logistics"`.
   - Secret Mission: `"Operation Glow-{SoldierId}"`.
3. **Optional External API Enrichment** (controlled by `ENABLE_EXTERNAL_API`):
   - When **disabled** (`false`): Comedic buff is set to `"Unbuffed Normal Firefly (No API)"`.
   - When **enabled** (`true`):
     - Calls `GET /api/v1/buff/{soldierId}` to obtain a randomized RPG buff and bonus glow points.
     - Calls `POST /api/v1/funny-title` with soldier metadata to obtain a customized humorous title.
     - Glow Intensity is incremented by `bonusGlow`, and Comedic Buff is formatted as `"{buffName} - {title}"`.
4. **Publishes Results**: Emits serialized `FireflyExpert` Protobuf messages to the destination exchange.

---

## 2. Input / Output (I/O) Layout

```mermaid
flowchart LR
    subgraph Input ["RabbitMQ Input"]
        InEx["Exchange: omega-solider-input"] --> InQ["Queue: omega-solider-input-queue"]
    end

    subgraph Service ["Mamtzetza Service"]
        InQ --> Worker["MamtzetzaWorker"]
        Worker --> Logic["Transformation Logic"]
        Logic --> Worker
    end

    subgraph ExternalAPI ["External HTTP REST API (Optional)"]
        Logic -.->|GET /api/v1/buff/{soldierId}| ExtBuff["Buff Endpoint"]
        Logic -.->|POST /api/v1/funny-title| ExtTitle["Title Endpoint"]
        ExtBuff -.->|Bonus Glow & Buff Name| Logic
        ExtTitle -.->|Humorous Title| Logic
    end

    subgraph Output ["RabbitMQ Output"]
        Worker --> OutEx["Exchange: firefly-expert-output"]
    end
```

### Input Specification
- **Protocol**: AMQP 0-9-1 (RabbitMQ)
- **Exchange**: `omega-solider-input` (Direct)
- **Queue**: `omega-solider-input-queue`
- **Routing Key**: `/` (or default binding)
- **Serialization**: Google Protocol Buffers (`OmegaSolider.Messages.OmegaSolider`)
- **Schema**:
  | Field | Type | Description |
  | :--- | :--- | :--- |
  | `soldier_id` | `string` | Unique soldier identifier (e.g. `SOL-001`) |
  | `codename` | `string` | Soldier codename |
  | `rank_level` | `int32` | Rank level (1 to 10) |
  | `bravery_points` | `int32` | Bravery points earned (0 to 100) |
  | `favorite_snack` | `string` | Fuel of choice (e.g. `Quantum Doritos`) |

### Output Specification
- **Protocol**: AMQP 0-9-1 (RabbitMQ)
- **Exchange**: `firefly-expert-output` (Direct)
- **Serialization**: Google Protocol Buffers (`OmegaSolider.Messages.FireflyExpert`)
- **Schema**:
  | Field | Type | Description |
  | :--- | :--- | :--- |
  | `soldier_id` | `string` | Identifier matching the input soldier |
  | `codename` | `string` | Preserved codename |
  | `rank_level` | `int32` | Preserved rank level |
  | `glow_intensity` | `int32` | Computed glow intensity (Base + API bonus) |
  | `expertise` | `string` | `"Expert in {favorite_snack} Logistics"` |
  | `comedic_buff` | `string` | Buff string from API or default fallback |
  | `secret_mission` | `string` | `"Operation Glow-{soldier_id}"` |
  | `processed_at_unix_ms` | `int64` | Processing timestamp in Unix epoch milliseconds |

### External HTTP API Contract (when `ENABLE_EXTERNAL_API=true`)
1. **GET `/api/v1/buff/{soldierId}`**
   - **Response**: `application/json`
     ```json
     {
       "soldierId": "SOL-001",
       "buffName": "Quantum Disco Sparkles",
       "bonusGlow": 50
     }
     ```
2. **POST `/api/v1/funny-title`**
   - **Request**: `application/json`
     ```json
     {
       "soldierId": "SOL-001",
       "codename": "Sparky",
       "favoriteSnack": "Quantum Doritos"
     }
     ```
   - **Response**: `application/json`
     ```json
     {
       "title": "Supreme Commander of Quantum Doritos",
       "funnyLore": "Fights crime with crunch."
     }
     ```

---

## 3. Configuration

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
     -e RABBITMQ_HOST=rabbitmq \
     -e RABBITMQ_PORT=5672 \
     -e RABBITMQ_USERNAME=admin \
     -e RABBITMQ_PASSWORD=admin \
     -e ENABLE_EXTERNAL_API=true \
     -e EXTERNAL_API_BASE_URL=http://mocker:8080 \
     mamtzetza:latest
   ```

### Option B: Docker Compose Example

```yaml
services:
  mamtzetza:
    image: mamtzetza:latest
    build: .
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
