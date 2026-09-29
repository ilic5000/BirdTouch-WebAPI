import uuid
from datetime import date, datetime
from typing import ClassVar, Self

from pydantic import EmailStr, Field

from app.models import BusinessProfile as BusinessProfileModel
from app.models import Mode
from app.models import PrivateProfile as PrivateProfileModel
from app.schemas.base import ApiModel, LongText, RequestModel, ShortText


def picture_url(user_id: uuid.UUID, mode: Mode, updated_at: datetime | None) -> str | None:
    """URL of the picture. It changes whenever the picture changes, so it can be cached forever."""
    if updated_at is None:
        return None
    return f"/api/v1/users/{user_id}/pictures/{mode}?v={int(updated_at.timestamp() * 1000)}"


class _Profile(ApiModel):
    mode: ClassVar[Mode]
    picture_url: str | None = Field(
        description="Relative URL of the picture (needs the Authorization header), or null."
    )
    updated_at: datetime

    @classmethod
    def from_model(cls, profile: PrivateProfileModel | BusinessProfileModel) -> Self:
        values = {
            name: getattr(profile, name) for name in cls.model_fields if name != "picture_url"
        }
        url = picture_url(profile.user_id, cls.mode, profile.picture_updated_at)
        return cls.model_validate({**values, "picture_url": url})


class PrivateProfile(_Profile):
    mode = Mode.PRIVATE

    first_name: str | None
    last_name: str | None
    email: str | None
    phone_number: str | None
    date_of_birth: date | None
    address: str | None
    description: str | None
    facebook_url: str | None
    twitter_url: str | None
    linkedin_url: str | None


class BusinessProfile(_Profile):
    mode = Mode.BUSINESS

    company_name: str | None
    email: str | None
    phone_number: str | None
    website: str | None
    address: str | None
    description: str | None


class PrivateProfileUpdate(RequestModel):
    """Only the fields that are sent are changed; send null to clear a field."""

    first_name: ShortText | None = None
    last_name: ShortText | None = None
    email: EmailStr | None = None
    phone_number: ShortText | None = None
    date_of_birth: date | None = None
    address: ShortText | None = None
    description: LongText | None = None
    facebook_url: ShortText | None = None
    twitter_url: ShortText | None = None
    linkedin_url: ShortText | None = None


class BusinessProfileUpdate(RequestModel):
    """Only the fields that are sent are changed; send null to clear a field."""

    company_name: ShortText | None = None
    email: EmailStr | None = None
    phone_number: ShortText | None = None
    website: ShortText | None = None
    address: ShortText | None = None
    description: LongText | None = None


class NearbyUser[P: _Profile](ApiModel):
    user_id: uuid.UUID
    distance_km: float
    # Where the user is visible (their last location update), e.g. to show them on a map.
    latitude: float
    longitude: float
    profile: P


class Contact[P: _Profile](ApiModel):
    user_id: uuid.UUID
    saved_at: datetime
    profile: P
