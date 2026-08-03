# SuperShop

[![CI](https://github.com/KennedySilva8907/supershop-store/actions/workflows/ci.yml/badge.svg)](https://github.com/KennedySilva8907/supershop-store/actions/workflows/ci.yml)

Online store built with ASP.NET Core 10 and React.

**[supershop.pt](https://supershop.pt)** · API at
[api.supershop.pt](https://api.supershop.pt/health/ready)

The API stops when nothing is using it, so the first request after a quiet
period takes a few seconds.

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
| `RateLimit__AuthPermitPerMinute` | Sign in attempts per minute per IP, default 5 |
| `Auth__FrontendOnAnotherSite` | `true` only when the store is not on a subdomain of the API's site |

The refresh cookie is `SameSite=Strict`, which is what the store and the API
sharing `supershop.pt` allows, and it is the strongest of the three: the
browser refuses to send it on any request that starts somewhere else.

Put the store on a different site and that same setting silently stops the
session from renewing, because a `Strict` cookie never crosses sites. That is
what `Auth__FrontendOnAnotherSite` is for, and it only takes effect over HTTPS,
since `SameSite=None` requires `Secure`.

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

### Images

Cloudinary builds each size the first time somebody asks for it. Measured on a
catalogue image, same URL, from the same machine:

| | |
| --- | --- |
| Size already cached | 40 to 77 ms |
| Size never requested before | 529 ms |
| The same one immediately after | 41 ms |

On a shop with little traffic the cache expires, so the first visitor after a
quiet stretch pays that on every image on the page. Asking for all of them once
puts them back:

```bash
./scripts/warm-images.sh
```

Worth running after adding products, and after changing the widths in
`cloudinary.ts`, since a new width has never been built for any image.

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

Both halves publish themselves on a merge to `main`: the store through Vercel,
the API through the `Deploy API` job, which only runs after the other three
pass. It waits for `/health/ready` afterwards, so a deployment that starts but
never answers fails the run instead of going quietly.

For a while only the store published itself, and the API had to be pushed by
hand. That is how production ended up with a store calling endpoints the API
did not have yet.

The job needs `FLY_API_TOKEN` in the repository secrets. It is scoped to this
one application rather than the whole account:

```bash
fly tokens create deploy --app supershop-api --name github-actions
```
