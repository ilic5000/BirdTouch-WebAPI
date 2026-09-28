import uuid
from datetime import UTC, datetime

from sqlalchemy.ext.asyncio import AsyncSession

from app.models import PROFILE_MODELS, BusinessProfile, Mode, PrivateProfile
from app.schemas.profiles import BusinessProfileUpdate, PrivateProfileUpdate


async def get_profile(
    session: AsyncSession, user_id: uuid.UUID, mode: Mode
) -> PrivateProfile | BusinessProfile:
    # Every user has both profiles: they are created at registration and deleted with the user.
    return await session.get_one(PROFILE_MODELS[mode], user_id)


async def update_profile(
    session: AsyncSession,
    user_id: uuid.UUID,
    mode: Mode,
    update: PrivateProfileUpdate | BusinessProfileUpdate,
) -> PrivateProfile | BusinessProfile:
    """Changes the fields that were sent in the request."""
    profile = await get_profile(session, user_id, mode)
    for field in update.model_fields_set:
        setattr(profile, field, getattr(update, field))
    profile.updated_at = datetime.now(UTC)
    await session.commit()
    await session.refresh(profile)  # Reloads picture_updated_at, which a flush expires.
    return profile
