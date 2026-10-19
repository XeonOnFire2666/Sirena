# Sirena API

A REST API built with ASP.NET Core (Minimal API) for **Sirena**, a productivity tracker with Desktop and Mobile (Flutter) clients. The backend handles Google sign-in, storage of workspaces with todo items, and daily activity statistics per application.

## Features

- **Google sign-in**: verifies the Google ID token and issues its own pair of JWT access / refresh tokens.
- **Token refresh**: the refresh token is rotated on every refresh.
- **Workspaces and todos**: create, update, upsert and delete workspaces together with their task lists (sync is based on `UpdatedAt`, with soft deletion via `IsDeleted`).
- **Daily statistics**: time tracking per application/domain (`ActionTag`), `StudyUnits`, `GitUnits`, productivity percentage; separate queries for Desktop and Mobile.
- **Automatic task rollover**: a background service moves incomplete tasks from past workspaces into an identical workspace for today, once an hour.
- **Automatic migrations**: the app applies EF Core migrations on startup.
- **OpenAPI**: the schema is exposed via `app.MapOpenApi()`.

## Tech stack

- .NET 10 / ASP.NET Core Minimal API
- Entity Framework Core 10 + Npgsql (PostgreSQL)
- JWT Bearer authentication
- Google.Apis.Auth (Google ID token verification)
- BCrypt.Net-Next
- Docker

## Architecture

The `SirenaAPI.sln` solution is split into layers:

| Project | Purpose |
|---|---|
| `web` (`SirenaAPI.API`) | Entry point: `Program.cs`, DI setup, authentication, JSON options, `appsettings` |
| `web.Adapters` | Minimal API endpoints (`UsersEndpoints`, `WorkspaceEndpoint`, `DayStatsEndpoint`) |
| `web.Application` | Services: `UserService`, `WorkspaceService`, `TodoItemService`, `DayStatsService` |
| `web.Logic` | Entities (`User`, `Workspace`, `TodoItem`, `DayStats`, `ActionTag`, `RefreshToken`), contracts, interfaces, exceptions |
| `web.Infrastructure` | `SirenaDbContext`, EF configurations, repositories, migrations, `JwtProvider`, `TodoRolloverBackgroundService` |

> The solution also contains `web.Core`, a template console project ("Hello, World!"). It does not affect the API.

## Quick start

### Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- PostgreSQL
- Google OAuth Client IDs for your clients (Desktop / Flutter Android / Flutter Web)

### Configuration

Copy `web/appsettings.Example.json` to `web/appsettings.json` (or use User Secrets / environment variables) and fill in the values:

```json
{
  "ConnectionStrings": {
    "MainString": "Host=localhost;Port=5432;Database=sirena;Username=postgres;Password=..."
  },
  "RealGoogleOptions": {
    "DesktopClientID": "...",
    "FlutterAndroidClientID": "...",
    "FlutterWebClientID": "..."
  },
  "JwtOptions": {
    "SecretKey": "a-long-random-string",
    "Issuer": "SirenaAPI",
    "Audience": "SirenaClients",
    "AccessTokenExpiresMinutes": "20",
    "RefreshTokenExpiresDays": "30"
  }
}
```

> ⚠️ `appsettings.Example.json` in the repository is slightly out of sync with the code: it lists `DekstopClientID` / `MobileClientID` and has no `Issuer` / `Audience`, while the code reads `DesktopClientID`, `FlutterAndroidClientID`, `FlutterWebClientID`, `Issuer` and `Audience`. Use the keys shown above.

### Run

```bash
dotnet restore
dotnet run --project web/SirenaAPI.API.csproj
```

On startup `Program.cs` calls `Database.Migrate()` and applies all migrations to the database from `MainString`. The server listens on `http://0.0.0.0:5203` (set in `Program.cs` via `UseUrls`).

### Docker

```bash
docker build -t sirena-api .
docker run -p 5203:5203 \
  -e ConnectionStrings__MainString="Host=...;Database=sirena;Username=...;Password=..." \
  -e RealGoogleOptions__DesktopClientID="..." \
  -e RealGoogleOptions__FlutterAndroidClientID="..." \
  -e RealGoogleOptions__FlutterWebClientID="..." \
  -e JwtOptions__SecretKey="..." \
  -e JwtOptions__Issuer="SirenaAPI" \
  -e JwtOptions__Audience="SirenaClients" \
  sirena-api
```

The port is hardcoded as `5203` in `Program.cs`, so publish exactly that one (the `Dockerfile` declares `EXPOSE 8080/8081`).

## API

Endpoints marked 🔒 require the header `Authorization: Bearer <accessToken>`.

### Authentication and users

| Method | Path | Description |
|---|---|---|
| POST | `/auth/google` | Sign in with a Google ID token (`LoginRequest.GoogleTokenId`). New users are registered automatically. Returns the user, access token and refresh token |
| POST | `/users/refresh` | Exchange a refresh token (`{ "token": "<guid>" }`) for a new token pair |
| GET | `/users/getbyname` | Look up a user by name (the name is sent in the request body; the result is cached for 10 seconds) |

### Workspaces 🔒

| Method | Path | Description |
|---|---|---|
| GET | `/workspace/getall` | All workspaces of the current user (the id is taken from the JWT) |
| POST | `/workspace/create` | Create a workspace with todos |
| PUT | `/workspace/upsert/{id}` | Create or update; the route `id` must match the body `id`. Errors: 400, 403, 409 |
| POST | `/workspace/update` | Update a workspace |
| DELETE | `/workspace/delete` | Delete a workspace by `id` |

Example `CreateWorkspaceDto` body:

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "workspaceTypeId": "study",
  "name": "Study",
  "colorHex": "#8AB4F8",
  "date": "2026-10-19",
  "todos": [
    {
      "id": "b1b2c3d4-0000-4000-8000-000000000001",
      "title": "Read chapter 3",
      "startTime": "2026-10-19T09:00:00Z",
      "endTime": "2026-10-19T10:00:00Z",
      "completeDate": null,
      "isCompleted": false,
      "isDeleted": false,
      "updatedAt": "2026-10-19T08:00:00Z"
    }
  ],
  "updatedAt": "2026-10-19T08:00:00Z"
}
```

If `name` is empty, `workspaceTypeId` is used; if `colorHex` is empty, `#8AB4F8` is applied.

### Statistics 🔒

| Method | Path | Description |
|---|---|---|
| GET | `/stats/getall?userId=` | All statistics for a user |
| GET | `/stats/getbydate?userId=&date=` | Statistics for a day |
| GET | `/stats/getalldesktop?userId=` | All records made on Desktop |
| GET | `/stats/getallmobile?userId=` | All records made on Mobile |
| GET | `/stats/getbydatedesktop?userId=&date=` | For a day (Desktop) |
| GET | `/stats/getbydatemobile?userId=&date=` | For a day (Mobile) |
| POST | `/stats/create` | Create a day record (the `tags` list must not be empty) |
| POST | `/stats/update` | Upsert a day's statistics |

How `/stats/update` works: tags are matched by the stable `TagId` (one application has one `TagId` across all days). If the tag already exists for that day, `RecordedMinutes`, `ColorHex`, `AppTitle` and `Domain` are updated; the client sends the accumulated value, not a delta. `StudyUnits`, `GitUnits` and `ProductivityPercent` are simply overwritten with the client's values.

`ActionTag` model: `id`, `tagId`, `dateTime`, `appTitle`, `domain`, `recordedMinutes`, `platform`, `colorHex`.

## Background jobs

`TodoRolloverBackgroundService` runs every hour and:

1. finds non-deleted workspaces dated before today (UTC) that contain incomplete tasks;
2. looks for a workspace for today with the same name and type for the same user, and creates it if missing;
3. moves the tasks there, keeping the hours and minutes of `StartTime` / `EndTime`, and updates `UpdatedAt` so clients pick up the changes on sync.

## Migrations

Migrations live in `web.Infrastructure/Migrations`. To add a new migration:

```bash
dotnet ef migrations add <Name> \
  --project web.Infrastructure/SirenaAPI.Infrastructure.csproj \
  --startup-project web/SirenaAPI.API.csproj
```

There is no need to apply them manually; this happens when the application starts.

## Repository structure

```
Sirena/
├── SirenaAPI.sln
├── Dockerfile
├── web/                  # API host (Program.cs, appsettings, JWT setup)
├── web.Adapters/         # Minimal API endpoints
├── web.Application/      # Application services
├── web.Logic/            # Entities, contracts, interfaces
├── web.Infrastructure/   # EF Core, repositories, JWT, background services, migrations
└── web.Core/             # Template console project
```