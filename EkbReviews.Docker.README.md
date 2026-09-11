# EkbReviews: Docker Compose

The Compose stack starts PostgreSQL, Ollama, a one-time `gpt-oss` model download, and the Worker.

## Start

```powershell
$env:TWOGIS_API_KEY = "your-2gis-api-key"
docker compose up --build
```

On its first run, the `gpt-oss` service downloads `gpt-oss:20b`. This model needs substantial disk space and RAM/GPU memory. Set `OLLAMA_MODEL` before startup to use another compatible Ollama model:

```powershell
$env:OLLAMA_MODEL = "gpt-oss:20b"
docker compose up --build
```

## Local CI commands

```powershell
dotnet restore EkbReviews.sln
dotnet build EkbReviews.sln --no-restore
dotnet test EkbReviews.sln --no-build
docker compose config
docker compose down
```

PostgreSQL is exposed at `localhost:5432`; Ollama is exposed at `http://localhost:11434`.
