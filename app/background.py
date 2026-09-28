import asyncio
import logging
from datetime import timedelta

from app.core.config import Settings
from app.core.database import get_session_factory
from app.services.visibility import remove_inactive

logger = logging.getLogger(__name__)


async def remove_inactive_users_periodically(settings: Settings) -> None:
    """Hides users who stopped reporting their location. Runs until cancelled."""
    interval = timedelta(minutes=settings.remove_inactive_users_run_every_minutes)
    older_than = timedelta(hours=settings.remove_inactive_users_older_than_hours)
    logger.info("Hiding users inactive for more than %s, checking every %s", older_than, interval)
    while True:
        try:
            async with get_session_factory()() as session:
                if removed := await remove_inactive(session, older_than):
                    logger.info("Hid %d inactive users", removed)
        except Exception:
            logger.exception("Hiding inactive users failed")
        await asyncio.sleep(interval.total_seconds())
