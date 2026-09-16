# DocFlow

A small demo app showing an event-driven document indexing pipeline: create a Verfahren/Projekt, it gets indexed into Elasticsearch asynchronously via RabbitMQ, and the UI shows live status updates via SignalR.

## What's happening in the code

1. **DocFlow.Client** (Blazor WASM) — form to create Verfahren/Projekte, a search box, and a table listing all entries with a live status pill (`Wird indexiert...` / `Fertig`).
2. **DocFlow.Api** — REST API (MediatR + FluentValidation). On create, it stores the entity in memory and publishes an event (`VerfahrenErstelltEvent` / `ProjektErstelltEvent`) to RabbitMQ.
3. **DocFlow.SearchIndexer** — a worker service that consumes those events, indexes the document into Elasticsearch, then publishes a confirmation event (`VerfahrenIndiziertEvent` / `ProjektIndiziertEvent`) back to RabbitMQ.
4. **DocFlow.Api** (`IndexStatusListener`) — consumes the confirmation event, marks the entity as `Fertig`, and pushes a SignalR message (`StatusHub`) to connected clients — no polling needed, the UI updates instantly.
5. **DocFlow.Messaging** / **DocFlow.Shared** — shared event contracts, DTOs, and RabbitMQ topology (exchange/queue/routing key names) used across all projects.

Everything is published to one topic exchange (`dokumente`), with Verfahren and Projekt events routed via different routing keys to the same queues.

## Tech stack

- .NET 10, ASP.NET Core, Blazor WebAssembly
- MediatR, FluentValidation
- RabbitMQ (event bus)
- Elasticsearch (search index)
- SignalR (real-time status push to the client)

## Running it

### 1. Start RabbitMQ and Elasticsearch (Docker)

First time (creates the containers):

```bash
docker run -d --name RabbitQM -p 5672:5672 -p 15672:15672 rabbitmq:3-management
docker run -d --name ElasticSearch -p 9200:9200 -e "discovery.type=single-node" -e "xpack.security.enabled=false" -e "xpack.security.http.ssl.enabled=false" -e "ES_JAVA_OPTS=-Xms512m -Xmx512m" docker.elastic.co/elasticsearch/elasticsearch:8.15.0
```

Next time (containers already exist, just start them):

```bash
docker start RabbitQM ElasticSearch
```

RabbitMQ management UI: http://localhost:15672 (guest/guest)
Elasticsearch: http://localhost:9200

> Note: neither container has a mounted volume, so `docker rm` (not just stop/start) wipes all data — queues and indices are recreated empty on next run.

### 2. Run the services

Start each in its own terminal (or via Visual Studio, F5 with multiple startup projects):

```bash
dotnet run --project DocFlow.Api
dotnet run --project DocFlow.SearchIndexer
dotnet run --project DocFlow.Client
```

- Api: https://localhost:7133
- Client: https://localhost:7166 (or http://localhost:5166)

### 3. Try it

Open the Client, create a new Verfahren or Projekt, and watch the status column go from `Wird indexiert...` to `Fertig` automatically once the SearchIndexer finishes.
