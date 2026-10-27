# Order API for Mango

## Building with Docker

The OrderAPI Dockerfile expects the OrderAPI repository directory to be the Docker build context. It uses BuildKit to mount the NuGet configuration as a secret during package restore.

### Prerequisites

- Docker with BuildKit support.
- .NET 10-compatible project dependencies.
- A NuGet configuration file that can restore all required packages, including `Mango.MessageBus` 3.0.0.
- PowerShell for the examples below.

Set `NUGET_CONFIG_PATH` to the absolute path of your local NuGet configuration file. The file must contain the package sources and any required authentication credentials. Do not commit credentials or secret configuration files.

```powershell
$env:NUGET_CONFIG_PATH = "C:\path\to\nuget.docker.config"
```

### Build the image directly

Run from the OrderAPI repository directory:

```powershell
docker build `
  --secret "id=nugetconfig,src=$env:NUGET_CONFIG_PATH" `
  -t mango-orderapi-local:dev `
  .
```

### Run the container directly

Provide the environment file containing the required OrderAPI settings:

```powershell
docker run --name mango-orderapi `
  --env-file .env `
  -p 5201:8080 `
  mango-orderapi-local:dev
```

### Build and run with Docker Compose

When working from the MangoForKube parent repository, build and start OrderAPI using the root Compose configuration:

```powershell
docker compose -f .\docker-compose.yaml build mango-order
docker compose -f .\docker-compose.yaml up -d mango-order
```

The Compose configuration supplies the `nugetconfig` BuildKit secret and uses `./Mango.Services.OrderAPI` as the build context. Ensure `NUGET_CONFIG_PATH` points to a valid local NuGet configuration file before running these commands.

To inspect the container:

```powershell
docker compose -f .\docker-compose.yaml ps mango-order
docker compose -f .\docker-compose.yaml logs --tail 60 mango-order
```
