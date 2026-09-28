from typing import Annotated

from fastapi import APIRouter, Query, status

from app.api.deps import SessionDep, SettingsDep
from app.core.security import create_access_token
from app.errors import ErrorResponse
from app.models import User
from app.schemas.auth import (
    LoginRequest,
    Me,
    RegisterRequest,
    TokenResponse,
    Username,
    UsernameAvailability,
)
from app.services import accounts

router = APIRouter(prefix="/auth", tags=["auth"])


def _token_response(settings: SettingsDep, user: User) -> TokenResponse:
    token, expires_at = create_access_token(settings, user.id)
    return TokenResponse(access_token=token, expires_at=expires_at, user=Me.model_validate(user))


@router.post(
    "/register",
    status_code=status.HTTP_201_CREATED,
    responses={409: {"model": ErrorResponse, "description": "`username_taken`"}},
)
async def register(
    request: RegisterRequest, session: SessionDep, settings: SettingsDep
) -> TokenResponse:
    """Creates a user (with empty private and business profiles) and logs them in."""
    return _token_response(settings, await accounts.register(session, request))


@router.post(
    "/login",
    responses={
        401: {"model": ErrorResponse, "description": "`invalid_credentials` (also when locked out)"}
    },
)
async def login(request: LoginRequest, session: SessionDep, settings: SettingsDep) -> TokenResponse:
    """After 5 failed attempts in a row the account is locked for 5 minutes."""
    user = await accounts.authenticate(session, request.username, request.password)
    return _token_response(settings, user)


@router.get("/username-availability")
async def username_availability(
    username: Annotated[Username, Query()], session: SessionDep
) -> UsernameAvailability:
    available = await accounts.is_username_available(session, username)
    return UsernameAvailability(username=username, available=available)
