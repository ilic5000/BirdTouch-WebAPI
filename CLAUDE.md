# BirdTouch-WebAPI

FastAPI backend (Python 3.14, SQLAlchemy 2 async + asyncpg, PostgreSQL 18, Alembic) for the
BirdTouch Android app. README.md has the full docs. `changes-from-old-api.md` documents the API
for the client that is being rewritten.

## Commands

```sh
uv sync                                   # install dependencies into .venv
docker compose up -d birdtouch-db         # database only (host port = POSTGRES_PORT in .env)
uv run alembic upgrade head               # apply migrations
uv run uvicorn app.main:app --reload --port 4050
uv run pytest                             # needs the database; uses a separate <db>_test database
uv run ruff check . && uv run ruff format .
docker compose up -d --build              # whole stack
```

## Architecture

- `app/api/routes/*`: thin HTTP handlers, one module per resource, all under `/api/v1`.
- `app/services/*`: business logic and queries. Services commit the session themselves and raise
  the errors from `app/errors.py`.
- `app/errors.py`: `ApiError` subclasses (HTTP status + stable `code`). All errors are returned
  as `{"code", "message", "details"?}`.
- `app/models.py`: the database schema. `app/schemas/*`: request and response bodies.
  - Responses inherit `ApiModel` (camelCase JSON).
  - Requests inherit `RequestModel` (camelCase only; unknown keys are rejected).
- `app/api/deps.py`: `SessionDep`, `SettingsDep`, `CurrentUser` (valid JWT of an existing user),
  and `REQUIRES_LOGIN` (the 401 response to document on protected routers).

## Rules

- **The API is used by the mobile client.** Any change to paths, fields, status codes or error
  codes must also be reflected in `changes-from-old-api.md` (while the client is being rewritten)
  and in the README's API table. Prefer adding over renaming or removing.
- New error cases get their own `ApiError` subclass with a stable `code`, which is documented in
  the route's `responses=`.
- **Schema changes:** edit `app/models.py`, run `uv run alembic revision --autogenerate -m "..."`,
  review the script, and commit both. Never edit a migration that has already been applied.
  `uv run alembic check` must pass.
- Tests are end-to-end against a real PostgreSQL (`tests/test_api.py`). Add a test there for every
  endpoint change.
- Configuration comes only from environment variables / `.env` (`app/core/config.py`). Document
  new settings in `.env.example` and in the README.
