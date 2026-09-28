import uuid
from datetime import datetime
from typing import Annotated, Literal

from pydantic import Field, StringConstraints

from app.schemas.base import ApiModel, RequestModel, ShortText

Username = Annotated[
    str,
    StringConstraints(min_length=3, max_length=64, pattern=r"^[A-Za-z0-9._@+-]+$"),
    Field(description="3-64 characters: letters, digits and `. _ @ + -`. Case-insensitive."),
]
Password = Annotated[str, StringConstraints(min_length=8, max_length=128)]


class RegisterRequest(RequestModel):
    username: Username
    password: Password
    first_name: ShortText | None = None
    last_name: ShortText | None = None


class LoginRequest(RequestModel):
    username: str
    password: str


class Me(ApiModel):
    id: uuid.UUID
    username: str
    created_at: datetime


class TokenResponse(ApiModel):
    access_token: str
    token_type: Literal["bearer"] = "bearer"
    expires_at: datetime
    user: Me


class UsernameAvailability(ApiModel):
    username: str
    available: bool
