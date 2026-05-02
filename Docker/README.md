# SmartCanteen Docker Guide

## Overview

This folder contains `docker-compose.yml` to run:

- `postgres`: PostgreSQL database
- `api`: `SC.Api` container built from `SC.Api/Dockerfile`

## Prerequisites

- Docker Engine
- Docker Compose plugin (`docker compose`)

## Start The Stack

Run from repository root:

```zsh
docker compose -f Docker/docker-compose.yml up -d --build
```

Or run inside this folder:

```zsh
cd Docker
docker compose up -d --build
```

## Stop The Stack

```zsh
docker compose -f Docker/docker-compose.yml down
```

To remove database volume too:

```zsh
docker compose -f Docker/docker-compose.yml down -v
```

## Default Ports

- API: `http://localhost:8080`
- PostgreSQL: `localhost:5432`

## Connection String

`SC.Api` reads `ConnectionStrings:DefaultConnection` from:

- `SC.Api/appsettings.json`
- `SC.Api/appsettings.Development.json`

In Docker, compose overrides with:
`ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=smartcanteen;Username=smartcanteen;Password=smartcanteen;Include Error Detail=true`

## Quick Smoke Test

```zsh
docker compose -f Docker/docker-compose.yml ps
curl http://localhost:8080/weatherforecast
```

## Logs

```zsh
docker compose -f Docker/docker-compose.yml logs -f api
docker compose -f Docker/docker-compose.yml logs -f postgres
```

