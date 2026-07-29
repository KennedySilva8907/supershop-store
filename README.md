# SuperShop

Online store built with ASP.NET Core 10 and React.

## Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 10, EF Core, PostgreSQL |
| Auth | ASP.NET Core Identity, JWT with refresh tokens |
| Frontend | React 19, Vite, TypeScript, Tailwind CSS |
| Images | Cloudinary |
| Tests | xUnit, Testcontainers |

## Structure

```
backend/     ASP.NET Core solution (Domain, Application, Infrastructure, Api)
frontend/    React application
assets/      Brand assets
```

## Requirements

- .NET SDK 10
- Node 22 or later
- Docker Desktop, for PostgreSQL and integration tests

## Getting started

```bash
docker compose up -d
cd backend && dotnet run --project src/SuperShop.Api
cd frontend && npm install && npm run dev
```

## Configuration

No secrets in the repository. `appsettings.json` ships with empty keys.
Development uses User Secrets, production uses platform environment variables.

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL |
| `Jwt__Secret` | Token signing, 32 bytes minimum |
| `Cloudinary__Url` | Image storage |
| `Email__ApiKey` | Transactional email |
| `Admin__Email` / `Admin__Password` | Admin seed |
