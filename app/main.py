import asyncio
import contextlib
import logging
from collections.abc import AsyncIterator

from fastapi import FastAPI

from app.api.routes import api_router
from app.background import remove_inactive_users_periodically
from app.core.config import get_settings
from app.core.database import get_engine
from app.errors import register_error_handlers


@contextlib.asynccontextmanager
async def _lifespan(app: FastAPI) -> AsyncIterator[None]:
    cleanup = asyncio.create_task(remove_inactive_users_periodically(get_settings()))
    yield
    cleanup.cancel()
    with contextlib.suppress(asyncio.CancelledError):
        await cleanup
    await get_engine().dispose()


def create_app() -> FastAPI:
    logging.basicConfig(
        level=get_settings().log_level.upper(),
        format="%(asctime)s %(levelname)s [%(name)s] %(message)s",
    )
    app = FastAPI(
        title="BirdTouch API",
        description="Backend for the BirdTouch mobile app (Android and iOS).",
        version="1.0.0",
        lifespan=_lifespan,
    )
    register_error_handlers(app)
    app.include_router(api_router)

    @app.get("/health", tags=["health"])
    async def health() -> dict[str, str]:
        return {"status": "ok"}

    return app


app = create_app()
