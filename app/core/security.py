"""Password hashing (Argon2) and JWT access tokens."""

import uuid
from datetime import UTC, datetime, timedelta

import jwt
from pwdlib import PasswordHash

from app.core.config import Settings

_password_hash = PasswordHash.recommended()
_JWT_ALGORITHM = "HS256"


class InvalidTokenError(Exception):
    pass


def hash_password(password: str) -> str:
    return _password_hash.hash(password)


def verify_password(password: str, password_hash: str) -> tuple[bool, str | None]:
    """Returns whether the password matches, and a new hash if the stored one is outdated."""
    return _password_hash.verify_and_update(password, password_hash)


def create_access_token(settings: Settings, user_id: uuid.UUID) -> tuple[str, datetime]:
    """Returns the token and when it expires."""
    now = datetime.now(UTC).replace(microsecond=0)
    expires_at = now + timedelta(days=settings.jwt_lifetime_days)
    payload = {
        "sub": str(user_id),
        "iat": now,
        "exp": expires_at,
        "iss": settings.jwt_issuer,
        "aud": settings.jwt_audience,
    }
    token = jwt.encode(payload, settings.jwt_secret_key.get_secret_value(), _JWT_ALGORITHM)
    return token, expires_at


def read_access_token(settings: Settings, token: str) -> uuid.UUID:
    """Validates the token and returns the id of the user it was issued to."""
    try:
        payload = jwt.decode(
            token,
            settings.jwt_secret_key.get_secret_value(),
            algorithms=[_JWT_ALGORITHM],
            audience=settings.jwt_audience,
            issuer=settings.jwt_issuer,
            options={"require": ["sub", "exp", "iss", "aud"]},
        )
        return uuid.UUID(payload["sub"])
    except (jwt.PyJWTError, ValueError) as error:
        raise InvalidTokenError from error
