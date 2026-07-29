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
