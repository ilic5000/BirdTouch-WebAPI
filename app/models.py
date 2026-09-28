"""Database schema (SQLAlchemy models).

Alembic generates migrations by comparing these models with the database, so every change here
needs a migration (see README, "Database migrations").
"""

import uuid
from datetime import date, datetime
from enum import StrEnum

from sqlalchemy import (
    DateTime,
    Double,
    Enum,
    ForeignKey,
    Index,
    MetaData,
    String,
    Text,
    func,
    select,
)
from sqlalchemy.orm import DeclarativeBase, Mapped, column_property, mapped_column


class Mode(StrEnum):
    """The role in which a user is visible to others. Each user has a profile per mode."""

    PRIVATE = "private"
    BUSINESS = "business"


class Base(DeclarativeBase):
    # Predictable constraint names, so migrations can refer to them.
    metadata = MetaData(
        naming_convention={
            "ix": "ix_%(table_name)s_%(column_0_N_name)s",
            "uq": "uq_%(table_name)s_%(column_0_N_name)s",
            "ck": "ck_%(table_name)s_%(constraint_name)s",
            "fk": "fk_%(table_name)s_%(column_0_name)s_%(referred_table_name)s",
            "pk": "pk_%(table_name)s",
        }
    )
    type_annotation_map = {  # noqa: RUF012
        str: Text(),
        float: Double(),
        datetime: DateTime(timezone=True),
        Mode: Enum(
            Mode,
            name="mode",
            native_enum=False,
            length=16,
            values_callable=lambda modes: [mode.value for mode in modes],
        ),
    }


def _user_fk() -> ForeignKey:
    return ForeignKey("users.id", ondelete="CASCADE")


def _now() -> Mapped[datetime]:
    return mapped_column(server_default=func.now())


class User(Base):
    __tablename__ = "users"

    id: Mapped[uuid.UUID] = mapped_column(primary_key=True, default=uuid.uuid7)
    username: Mapped[str] = mapped_column(String(64))
    # Lower-cased username; usernames are unique regardless of case.
    normalized_username: Mapped[str] = mapped_column(String(64), unique=True)
    password_hash: Mapped[str]
    failed_login_count: Mapped[int] = mapped_column(default=0)
    lockout_end: Mapped[datetime | None]
    created_at: Mapped[datetime] = _now()


class ProfilePicture(Base):
    """Picture of a user's profile in a mode."""

    __tablename__ = "profile_pictures"

    user_id: Mapped[uuid.UUID] = mapped_column(_user_fk(), primary_key=True)
    mode: Mapped[Mode] = mapped_column(primary_key=True)
    content_type: Mapped[str] = mapped_column(String(32))
    data: Mapped[bytes]
    updated_at: Mapped[datetime] = _now()


def _picture_updated_at(user_id: Mapped[uuid.UUID], mode: Mode) -> Mapped[datetime | None]:
    """When the profile's picture was last changed (None when it has none)."""
    return column_property(
        select(ProfilePicture.updated_at)
        .where(ProfilePicture.user_id == user_id, ProfilePicture.mode == mode)
        .correlate_except(ProfilePicture)
        .scalar_subquery()
    )


class PrivateProfile(Base):
    """What a user shows about themselves in private mode. Created with the user."""

    __tablename__ = "private_profiles"

    user_id: Mapped[uuid.UUID] = mapped_column(_user_fk(), primary_key=True)
    first_name: Mapped[str | None]
    last_name: Mapped[str | None]
    email: Mapped[str | None]
    phone_number: Mapped[str | None]
    date_of_birth: Mapped[date | None]
    address: Mapped[str | None]
    description: Mapped[str | None]
    facebook_url: Mapped[str | None]
    twitter_url: Mapped[str | None]
    linkedin_url: Mapped[str | None]
    updated_at: Mapped[datetime] = _now()

    picture_updated_at = _picture_updated_at(user_id, Mode.PRIVATE)


class BusinessProfile(Base):
    """What a user shows about their business in business mode. Created with the user."""

    __tablename__ = "business_profiles"

    user_id: Mapped[uuid.UUID] = mapped_column(_user_fk(), primary_key=True)
    company_name: Mapped[str | None]
    email: Mapped[str | None]
    phone_number: Mapped[str | None]
    website: Mapped[str | None]
    address: Mapped[str | None]
    description: Mapped[str | None]
    updated_at: Mapped[datetime] = _now()

    picture_updated_at = _picture_updated_at(user_id, Mode.BUSINESS)


PROFILE_MODELS: dict[Mode, type[PrivateProfile] | type[BusinessProfile]] = {
    Mode.PRIVATE: PrivateProfile,
    Mode.BUSINESS: BusinessProfile,
}


class Visibility(Base):
    """A user visible to others in a mode, at their last reported location."""

    __tablename__ = "visibilities"
    __table_args__ = (Index(None, "updated_at"),)

    user_id: Mapped[uuid.UUID] = mapped_column(_user_fk(), primary_key=True)
    mode: Mapped[Mode] = mapped_column(primary_key=True)
    latitude: Mapped[float]
    longitude: Mapped[float]
    updated_at: Mapped[datetime]


class SavedContact(Base):
    """A user that another user saved to their contacts, in a mode."""

    __tablename__ = "saved_contacts"

    user_id: Mapped[uuid.UUID] = mapped_column(_user_fk(), primary_key=True)
    mode: Mapped[Mode] = mapped_column(primary_key=True)
    contact_id: Mapped[uuid.UUID] = mapped_column(_user_fk(), primary_key=True, index=True)
    created_at: Mapped[datetime] = _now()
