"""Test setup.

API tests run against a real PostgreSQL (connection settings from the environment / .env,
e.g. the one started by `docker compose up -d birdtouch-db`). A separate database named
`<POSTGRES_DB>_test` is recreated for every test run and migrated with Alembic.
"""

import asyncio
import os
from collections.abc import AsyncIterator

import httpx
import pytest
from alembic import command
from alembic.config import Config

from app.core.config import get_settings

os.environ["POSTGRES_DB"] = f"{get_settings().postgres_db}_test"
get_settings.cache_clear()


async def _recreate_database() -> None:
    import asyncpg

    settings = get_settings()
    connection = await asyncpg.connect(
        host=settings.postgres_host,
        port=settings.postgres_port,
        user=settings.postgres_user,
        password=settings.postgres_password.get_secret_value(),
        database="postgres",
    )
    try:
        await connection.execute(f'DROP DATABASE IF EXISTS "{settings.postgres_db}" WITH (FORCE)')
        await connection.execute(f'CREATE DATABASE "{settings.postgres_db}"')
    finally:
        await connection.close()


@pytest.fixture(scope="session")
def database() -> None:
    try:
        asyncio.run(_recreate_database())
    except OSError as error:
        pytest.skip(f"PostgreSQL is not reachable: {error}")
    command.upgrade(Config("alembic.ini"), "head")


@pytest.fixture(scope="session")
async def client(database: None) -> AsyncIterator[httpx.AsyncClient]:
    from app.main import app  # Imported late: settings must point at the test database first.

    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        yield client
