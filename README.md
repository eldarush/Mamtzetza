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

## 2. Input / Output (I/O) Layout

Mamtzetza operates as an event-driven stream processor connecting an incoming RabbitMQ queue to an outgoing RabbitMQ exchange:
- **Input Flow**: Consumes serialized `OmegaSolider` Protobuf messages from the queue `omega-solider-input-queue` (bound to direct exchange `omega-solider-input`).
- **Processing & Enrichment**: Computes the 5 derived attributes and base glow metrics from soldier attributes. When `ENABLE_EXTERNAL_API=true`, it calls an external HTTP REST service (HTTP GET for buff attributes and HTTP POST for title generation).
- **Output Flow**: Publishes the resulting `FireflyExpert` Protobuf message to the direct exchange `firefly-expert-output`.

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
  | `name` | `string` | Full name of the soldier |
  | `rank` | `string` | Military rank (e.g. `Captain`, `General`, `Sergeant`) |
  | `age` | `int32` | Age of the soldier |
  | `favorite_food` | `string` | Favorite food (e.g. `Shawarma`, `Spicy Nachos`) |
  | `favorite_tv_show` | `string` | Favorite TV show (e.g. `Star Trek: TNG`, `The Expanse`) |
  | `shoe_size` | `float` | European shoe size (e.g. `43.5`) |
  | `height` | `float` | Height in cm (e.g. `180.0`) |
  | `weight` | `float` | Weight in kg (e.g. `80.0`) |
  | `lucky_number` | `int32` | Personal lucky number |
  | `hobby` | `string` | Hobby (e.g. `Chess`, `Gaming`) |
  | `origin_planet` | `string` | Home planet (e.g. `Mars`, `Earth`, `Jupiter`) |

### Output Specification
- **Protocol**: AMQP 0-9-1 (RabbitMQ)
- **Exchange**: `firefly-expert-output` (Direct)
- **Serialization**: Google Protocol Buffers (`OmegaSolider.Messages.FireflyExpert`)
- **Schema**:
  | Field | Type | Description |
  | :--- | :--- | :--- |
  | `soldier_id` | `string` | Identifier matching the input soldier |
  | `name` | `string` | Preserved soldier name |
  | `rank` | `string` | Preserved military rank |
  | `age` | `int32` | Preserved age |
  | `favorite_food` | `string` | Preserved favorite food |
  | `favorite_tv_show` | `string` | Preserved favorite TV show |
  | `shoe_size` | `float` | Preserved shoe size |
  | `height` | `float` | Preserved height |
  | `weight` | `float` | Preserved weight |
  | `lucky_number` | `int32` | Preserved lucky number |
  | `hobby` | `string` | Preserved hobby |
  | `origin_planet` | `string` | Preserved origin planet |
  | `favorite_technology` | `string` | Deterministically derived from `favorite_tv_show` |
  | `favorite_team` | `string` | Deterministically derived from `origin_planet` |
  | `favorite_commander` | `string` | Deterministically derived from `rank` |
  | `favorite_woman` | `string` | Deterministically derived from `lucky_number % 5` |
  | `favorite_coding_language` | `string` | Deterministically derived from `age` |
  | `glow_intensity` | `int32` | Computed glow intensity (Base + API bonus) |
  | `comedic_buff` | `string` | Buff string from API or default fallback |
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
       "name": "John Doe",
       "favoriteFood": "Shawarma"
     }
     ```
   - **Response**: `application/json`
     ```json
     {
       "title": "Supreme Commander of Shawarma",
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
| `METRICS_PORT` | `int` | `9090` | HTTP port for Prometheus metrics scrape endpoint (`/metrics`) |

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
