"""User accounts: registration, login and removal."""

import unicodedata
import uuid
from datetime import UTC, datetime, timedelta

from sqlalchemy import delete, select
from sqlalchemy.exc import IntegrityError
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.security import hash_password, verify_password
from app.errors import InvalidCredentialsError, UsernameTakenError
from app.models import BusinessProfile, PrivateProfile, User
from app.schemas.auth import RegisterRequest

MAX_FAILED_LOGINS = 5
LOCKOUT_DURATION = timedelta(minutes=5)


def normalize_username(username: str) -> str:
    return unicodedata.normalize("NFC", username).lower()


async def find_user_by_username(session: AsyncSession, username: str) -> User | None:
    return await session.scalar(
        select(User).where(User.normalized_username == normalize_username(username))
    )


async def register(session: AsyncSession, request: RegisterRequest) -> User:
    """Creates the user together with their (mostly empty) private and business profiles."""
    if await find_user_by_username(session, request.username):
        raise UsernameTakenError

    user = User(
        username=request.username,
        normalized_username=normalize_username(request.username),
        password_hash=hash_password(request.password),
    )
    session.add(user)
    try:
        await session.flush()
        session.add_all(
            [
                PrivateProfile(
                    user_id=user.id, first_name=request.first_name, last_name=request.last_name
                ),
                BusinessProfile(user_id=user.id),
            ]
        )
        await session.commit()
    except IntegrityError as error:  # The same username was registered concurrently.
        await session.rollback()
        raise UsernameTakenError from error
    await session.refresh(user)
    return user


async def authenticate(session: AsyncSession, username: str, password: str) -> User:
    """Checks the credentials. Too many failed attempts lock the account for a while."""
    user = await find_user_by_username(session, username)
    now = datetime.now(UTC)
    if user is None or (user.lockout_end is not None and user.lockout_end > now):
        raise InvalidCredentialsError

    valid, updated_hash = verify_password(password, user.password_hash)
    if not valid:
        user.failed_login_count += 1
        if user.failed_login_count >= MAX_FAILED_LOGINS:
            user.failed_login_count = 0
            user.lockout_end = now + LOCKOUT_DURATION
        await session.commit()
        raise InvalidCredentialsError

    if updated_hash is not None:
        user.password_hash = updated_hash
    user.failed_login_count = 0
    await session.commit()
    return user


async def is_username_available(session: AsyncSession, username: str) -> bool:
    return await find_user_by_username(session, username) is None


async def delete_user(session: AsyncSession, user_id: uuid.UUID) -> None:
    # Everything else about the user (profiles, pictures, visibility, contacts in both directions)
    # is deleted by the database (ON DELETE CASCADE).
    await session.execute(delete(User).where(User.id == user_id))
    await session.commit()
