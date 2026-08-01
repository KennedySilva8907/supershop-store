# Oracle Cloud deployment

The API runs on an Always Free Ampere A1 instance in Frankfurt, next to the
database. The machine never sleeps, so there is no cold start to pay on the
first request.

Caddy sits in front and terminates TLS, renewing certificates on its own. It
sends `X-Forwarded-Proto`, which the API reads to decide whether the refresh
cookie may cross sites.

## The instance

Create an **Ampere A1 Flex** compute instance, Ubuntu 24.04, in Germany
Central. Two cores and twelve gigabytes are within the Always Free allowance
and are far more than this needs.

ARM instances are in demand and the console often answers `Out of capacity`.
Retrying in another availability domain, or later in the day, is the usual
remedy.

## The machine

```bash
git clone https://github.com/KennedySilva8907/supershop-store.git
cd supershop-store/deploy/oracle
./setup.sh
```

Then log out and back in, so the docker group applies.

Two firewalls have to allow 80 and 443. `setup.sh` handles the one on the
machine. The other is the subnet security list in the Oracle console, under
Networking, and has to be edited there.

## Configuration

```bash
cp .env.example .env
```

Fill it in and keep it on the machine. It holds every secret and is never
committed.

`API_DOMAIN` is `api.silva.dev`. It has to resolve to the instance before the
first start, because Caddy asks Let's Encrypt for a certificate as it boots
and the request fails without a working record.

The record is an `A` pointing at the instance public address, added wherever
the domain's nameservers are, which for `silva.dev` is the registrar rather
than Vercel.

```bash
nslookup api.silva.dev
```

## Running

```bash
docker compose up -d --build
```

The image is built on the machine, which is ARM, so no cross compilation is
needed.

Migrations and seeding stay explicit:

```bash
docker compose run --rm api dotnet SuperShop.Api.dll --seed
```

## Updating

```bash
git pull
docker compose up -d --build
```
