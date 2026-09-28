import logging

from fastapi import APIRouter, status

from app.api.deps import REQUIRES_LOGIN, CurrentUser, SessionDep
from app.schemas.auth import Me
from app.services import accounts

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/me", tags=["me"], responses=REQUIRES_LOGIN)


@router.get("")
async def get_me(user: CurrentUser) -> Me:
    return Me.model_validate(user)


@router.delete("", status_code=status.HTTP_204_NO_CONTENT)
async def delete_me(user: CurrentUser, session: SessionDep) -> None:
    """Permanently deletes the account and everything about it."""
    await accounts.delete_user(session, user.id)
    logger.info("User %s deleted their account", user.id)
