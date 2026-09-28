import contextlib
from typing import Annotated

from fastapi import Depends
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.config import Settings, get_settings
from app.core.database import get_session
from app.core.security import InvalidTokenError, read_access_token
from app.errors import ErrorResponse, NotAuthenticatedError
from app.models import User

SessionDep = Annotated[AsyncSession, Depends(get_session)]
SettingsDep = Annotated[Settings, Depends(get_settings)]

_bearer_scheme = HTTPBearer(auto_error=False, description="`accessToken` from login/registration")


async def get_current_user(
    session: SessionDep,
    settings: SettingsDep,
    credentials: Annotated[HTTPAuthorizationCredentials | None, Depends(_bearer_scheme)],
) -> User:
    """The logged in user. Tokens of deleted users are rejected too."""
    user = None
    if credentials is not None:
        with contextlib.suppress(InvalidTokenError):
            user = await session.get(User, read_access_token(settings, credentials.credentials))
    if user is None:
        raise NotAuthenticatedError
    return user


CurrentUser = Annotated[User, Depends(get_current_user)]

# OpenAPI documentation for routers whose endpoints all need a logged in user.
REQUIRES_LOGIN = {401: {"model": ErrorResponse, "description": "`not_authenticated`"}}
