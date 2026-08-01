# SuperShop

[![CI](https://github.com/KennedySilva8907/supershop-store/actions/workflows/ci.yml/badge.svg)](https://github.com/KennedySilva8907/supershop-store/actions/workflows/ci.yml)

Online store built with ASP.NET Core 10 and React.

## Stack

| Layer | Technology |
| --- | --- |
| API | ASP.NET Core 10, EF Core, PostgreSQL |
| Auth | ASP.NET Core Identity, JWT with refresh tokens |
| Frontend | React 19, Vite, TypeScript, Tailwind CSS |
| Images | Cloudinary |
| Tests | xUnit, Testcontainers |

## Structure

```text
backend/     ASP.NET Core solution (Domain, Application, Infrastructure, Api)
frontend/    React application
assets/      Brand assets
```

The frontend renders nothing yet. Tooling, brand tokens and the Cloudinary
helper are in place, and the pages arrive with the catalogue.

## Requirements

- .NET SDK 10
- Node 22 or later
- Docker Desktop, for PostgreSQL and integration tests

## Getting started

```bash
docker compose up -d
cd backend && dotnet run --project src/SuperShop.Api
cd frontend && npm ci && npm run dev
```

## Frontend dependencies

Use `npm ci`, not `npm install`, unless you are deliberately changing a
dependency.

Tailwind ships platform specific binaries. On Windows, `npm install` drops the
`wasm32-wasi` packages and the `@emnapi` entries they need, because they do not
apply to the local platform. The lock file then no longer matches what CI
resolves on Linux, and `npm ci` there fails with
`Missing: @emnapi/runtime from lock file`.

When a dependency really has to change, regenerate the lock file on Linux so it
carries every platform:

```bash
docker run --rm -v "$PWD:/app" -w /app node:22-slim \
  sh -c "rm -rf node_modules package-lock.json && npm install"
```

The result works on both systems. On Windows `npm ci` installs only the local
packages and leaves the lock file untouched.

## Configuration

No secrets in the repository. `appsettings.json` ships with empty keys.
Development uses User Secrets, production uses platform environment variables.

| Variable | Purpose |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | PostgreSQL |
| `Jwt__Secret` | Token signing, 32 bytes minimum |
| `Cloudinary__Url` | Image storage |
| `Email__ApiKey` | Transactional email |
| `Admin__Email` / `Admin__Password` | Admin seed |
| `RateLimit__AuthPermitPerMinute` | Sign in attempts per minute per IP, default 5 |

## Hosting and what free tiers cost

The API and the database must sit in the same region. A round trip across the
Atlantic adds 100 to 200 ms to every query, and a page making ten queries pays
that ten times.

Free plans suspend after a period without traffic. The first request afterwards
pays the wake-up:

| Layer | Suspends | Wake-up |
| --- | --- | --- |
| API on a free container host | after 15 minutes idle | 30 to 60 seconds |
| Neon free tier | after 5 minutes idle | under a second |

That first slow request is the hosting plan, not the application. Keeping one
instance always on removes it, which is what a paid tier buys.

Two things reduce it without paying: use a pooled connection string, so the
database is not reconnected on every request, and keep both halves in the same
region.

## Deployment

Migrations are applied by explicit command, never automatically on startup:

```bash
dotnet ef database update --project src/SuperShop.Infrastructure \
  --startup-project src/SuperShop.Api
```

The API ships as a container. It runs as an unprivileged user and exposes
`/health` for liveness and `/health/ready` for database readiness, which is the
one a platform should poll.
