# BirdTouch-WebAPI

BirdTouch-WebAPI is the server used by the [BirdTouch app](https://github.com/ilic5000/BirdTouch-Client) for Android and
iOS (Flutter).

It uses [FastAPI](https://fastapi.tiangolo.com/) (Python 3.14) and PostgreSQL 18. The database schema
is managed with [Alembic](https://alembic.sqlalchemy.org/).

## Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Configuration](#configuration)
- [API](#api)
- [Database](#database)
- [Development](#development)
- [Good to know](#good-to-know)

## Requirements

- Docker with Docker Compose v2
- Free ports on the Docker host: `4050` (API), `5432` (PostgreSQL) and `9090` (pgAdmin). All of them
  can be changed in `.env`.

## Installation

1. Create the configuration file and adjust it (see [Configuration](#configuration)):

   ```sh
   cp .env.example .env
   ```

2. Start everything:

   ```sh
   docker compose up -d --build
   ```

3. Check that all services are running:

   ```sh
   docker compose ps -a
   ```

   `database-migration` applies the database migrations and exits, so its state should be `exited (0)`.

The API is now available on port `4050`, e.g. <http://localhost:4050/health>. Interactive API
documentation is at <http://localhost:4050/docs>.

pgAdmin is available at <http://localhost:9090>, with the database server already registered.

| Service              | Description                                                                    |
| -------------------- | ------------------------------------------------------------------------------ |
| `birdtouch-api`      | The API, published on `API_PORT` (default `4050`)                              |
| `database-migration` | Runs `alembic upgrade head`, then exits. The API starts only after it succeeds |
| `birdtouch-db`       | PostgreSQL 18, published on `POSTGRES_PORT`. Data is in the `postgres-data` volume |
| `pgadmin`            | pgAdmin web UI for the database                                                |

## Configuration

All configuration is in the `.env` file, which is read both by Docker Compose and by the API.
[`.env.example`](.env.example) lists every setting with its default value. `.env` is not committed to git.

Before deploying to a real server, change:

- `POSTGRES_PASSWORD`: password of the database.
- `PGADMIN_DEFAULT_PASSWORD`: password for pgAdmin.
- `JWT_SECRET_KEY`: key used to sign login tokens. Generate one with
  `python -c "import secrets; print(secrets.token_urlsafe(48))"`. Changing it logs everybody out.

| Variable                                  | Default      | Description                                                         |
| ----------------------------------------- | ------------ | ------------------------------------------------------------------- |
| `POSTGRES_USER`                           | `postgres`   | Database user                                                       |
| `POSTGRES_PASSWORD`                       | (required)   | Database password                                                   |
| `POSTGRES_DB`                             | `birdtouch`  | Database name                                                       |
| `POSTGRES_HOST`                           | `localhost`  | Database host when the API runs outside Docker                      |
| `POSTGRES_PORT`                           | `5432`       | Host port of the database (Docker Compose publishes it there)       |
| `API_PORT`                                | `4050`       | Host port of the API                                                |
| `LOG_LEVEL`                               | `INFO`       | Python log level                                                    |
| `JWT_SECRET_KEY`                          | (required)   | Key for signing login tokens, at least 32 characters                |
| `JWT_ISSUER` / `JWT_AUDIENCE`             | `birdtouch`  | Issuer and audience of the tokens                                   |
| `JWT_LIFETIME_DAYS`                       | `30`         | How long a login is valid                                           |
| `MAX_PICTURE_BYTES`                       | `5242880`    | Maximum size of an uploaded profile picture (5 MB)                  |
| `REMOVE_INACTIVE_USERS_RUN_EVERY_MINUTES` | `5`          | How often to look for users who stopped sharing their location      |
| `REMOVE_INACTIVE_USERS_OLDER_THAN_HOURS`  | `24`         | Hours without a location update after which a user becomes invisible |
| `PGADMIN_*`                               |              | pgAdmin login and port                                              |

## API

REST API under `/api/v1`, JSON with camelCase keys. The full reference, with every request, response
and error, is the OpenAPI spec: `/docs` (Swagger UI), `/redoc` or `/openapi.json` on a running server.
[`changes-from-old-api.md`](changes-from-old-api.md) describes the API in detail, including how it
differs from the old .NET API.

| Method           | Path                                        | Description                                              |
| ---------------- | ------------------------------------------- | -------------------------------------------------------- |
| `GET`            | `/health`                                   | Health check                                             |
| `POST`           | `/api/v1/auth/register`                     | Create an account; returns a token                       |
| `POST`           | `/api/v1/auth/login`                        | Log in; returns a token                                  |
| `GET`            | `/api/v1/auth/username-availability`        | Is a username still free?                                |
| `GET`, `DELETE`  | `/api/v1/me`                                | The logged in user; delete the account                   |
| `GET`, `PATCH`   | `/api/v1/me/private-profile`                | Private profile (PATCH changes only the sent fields)     |
| `GET`, `PATCH`   | `/api/v1/me/business-profile`               | Business profile                                         |
| `PUT`, `DELETE`  | `/api/v1/me/pictures/{mode}`                | Upload (raw image bytes) or delete a profile picture     |
| `GET`            | `/api/v1/users/{userId}/pictures/{mode}`    | Download a picture (use `pictureUrl` from profiles)      |
| `GET`            | `/api/v1/me/visibility`                     | Modes in which the user is visible                       |
| `PUT`, `DELETE`  | `/api/v1/me/visibility/{mode}`              | Become visible at a location (or update it); hide        |
| `GET`            | `/api/v1/nearby/{private,business}`         | Visible users nearby (with location), nearest first      |
| `GET`            | `/api/v1/me/contacts/{private,business}`    | Saved contacts with their profiles                       |
| `PUT`, `DELETE`  | `/api/v1/me/contacts/{mode}/{userId}`       | Save or remove a contact                                 |

`{mode}` is `private` or `business`. All endpoints except `/health` and `/api/v1/auth/*` need an
`Authorization: Bearer <accessToken>` header.

Errors always have the same body: `{"code": "...", "message": "..."}` (plus `details` for
`422 validation_error`). `code` is stable and meant for clients.

## Database

The schema is defined by the SQLAlchemy models in [`app/models.py`](app/models.py):

| Table               | Content                                                                 |
| ------------------- | ----------------------------------------------------------------------- |
| `users`             | Accounts: username, Argon2 password hash, login lockout                 |
| `private_profiles`  | One per user: name, contact details, links                              |
| `business_profiles` | One per user: company name, contact details                             |
| `profile_pictures`  | Profile pictures, per user and mode                                     |
| `visibilities`      | Users currently visible, per mode, with their last location             |
| `saved_contacts`    | Contacts users saved, per mode                                          |

Deleting a user deletes all their rows (`ON DELETE CASCADE`).

### Migrations

Migration scripts are in [`migrations/versions`](migrations/versions). `docker compose up` applies
them automatically. To apply them without restarting the rest of the stack:

```sh
docker compose up database-migration --build
```

To change the schema:

1. Change the models in `app/models.py`.
2. Generate a migration script by comparing the models with the database:

   ```sh
   uv run alembic revision --autogenerate -m "add something"
   ```

3. Review the generated script. Autogenerate doesn't detect everything, e.g. renamed columns show
   up as a drop plus an add.
4. Apply it with `uv run alembic upgrade head`, and commit it together with the model change.

Never edit a migration that has already been applied somewhere; add a new one instead.

Other useful commands:

```sh
uv run alembic current            # revision the database is at
uv run alembic history            # all revisions
uv run alembic check              # fails if the models and the database differ
uv run alembic downgrade -1       # revert the last migration
uv run alembic upgrade head --sql # print the SQL instead of running it
```

## Development

You need [uv](https://docs.astral.sh/uv/) and Docker. uv installs the right Python version by itself.

```sh
cp .env.example .env
uv sync                                 # create .venv with all dependencies
docker compose up -d birdtouch-db       # only the database
uv run alembic upgrade head             # create/upgrade the schema
uv run uvicorn app.main:app --reload --port 4050
```

Outside Docker, the API reads `.env` from the current directory and connects to the database at
`POSTGRES_HOST:POSTGRES_PORT`. If port 5432 is already used on your machine (e.g. by another
PostgreSQL), set `POSTGRES_PORT` to a free port. Docker Compose then publishes the database there.

### Tests and linting

```sh
uv run pytest        # needs the database (docker compose up -d birdtouch-db)
uv run ruff check .  # lint
uv run ruff format . # format
```

The tests are end to end. They create a separate `<POSTGRES_DB>_test` database, apply the
migrations to it, and call the API over HTTP.

GitHub Actions runs the linter, the tests and a Docker build on every push and pull request
([`.github/workflows/ci.yml`](.github/workflows/ci.yml)). Dependabot proposes dependency updates weekly.

### Project structure

```text
app/
  main.py              app setup and startup/shutdown
  background.py        periodic hiding of inactive users
  errors.py            API errors and the error response format
  core/                configuration, database session, passwords and tokens
  models.py            database schema (SQLAlchemy models)
  schemas/             request and response bodies (pydantic)
  services/            business logic and database queries
  api/deps.py          shared dependencies (database session, logged in user)
  api/routes/          HTTP endpoints, one module per resource
migrations/            Alembic environment and migration scripts
tests/                 pytest tests
Dockerfile             image used by both the API and the migration service
docker-compose.yml     the whole stack
```

Dependencies are declared in `pyproject.toml` and locked in `uv.lock`. To update them, run
`uv lock --upgrade`, then run the tests.

## Good to know

### Obtaining the IP address of a WSL2 machine

If Docker runs in a Linux distribution under WSL2 on Windows and you want to reach the API from
elsewhere (e.g. from the BirdTouch client in an Android emulator), use the WSL2 machine's IP address:

1. Open the WSL2 Linux shell.
2. Run `ip addr show eth0`.
3. Take the `inet` address, e.g. `172.22.200.173`.
4. The API is available at `172.22.200.173:4050`.

### Serving the API from your PC

1. [Check whether the API port (by default 4050) is reachable from the internet](https://www.portchecktool.com/).
2. If it isn't, forward the port on your router and allow it through the firewall.
3. Optional: a dynamic DNS service such as [noip.com](https://www.noip.com/) keeps a hostname pointed
   at your changing IP address.
