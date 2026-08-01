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
that ten times. Both halves run in Frankfurt.

Fly has no free allowance for new organisations, so idle time is billed. The
machine therefore stops when nothing is using it and starts again on the next
request, which costs a few seconds on that request and almost nothing a month.
Keeping it always on removes the wait and costs about $3.32 a month.

`stop` rather than `suspend`. Suspending resumes faster, but the machine clock
drifts across a suspend, and tokens are validated against it. Stopping gives a
fresh process, a fresh connection pool and a correct clock.

A pooled connection string keeps the database from being reconnected on every
request, which matters more than either of these.

## Deployment

Migrations are applied by explicit command, never automatically on startup:

```bash
dotnet ef database update --project src/SuperShop.Infrastructure \
  --startup-project src/SuperShop.Api
```

The catalogue and the administrator are seeded by the same principle, with an
explicit command against the deployed image:

```bash
dotnet SuperShop.Api.dll --seed
```

It is idempotent. Products already present are left alone, and the
administrator is created only if the e-mail is not taken.

The API ships as a container. It runs as an unprivileged user and exposes
`/health` for liveness and `/health/ready` for database readiness, which is the
one a platform should poll.
