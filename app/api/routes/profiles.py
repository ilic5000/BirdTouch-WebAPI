from fastapi import APIRouter

from app.api.deps import REQUIRES_LOGIN, CurrentUser, SessionDep
from app.models import Mode
from app.schemas.profiles import (
    BusinessProfile,
    BusinessProfileUpdate,
    PrivateProfile,
    PrivateProfileUpdate,
)
from app.services import profiles

router = APIRouter(prefix="/me", tags=["profiles"], responses=REQUIRES_LOGIN)


@router.get("/private-profile")
async def get_private_profile(user: CurrentUser, session: SessionDep) -> PrivateProfile:
    return PrivateProfile.from_model(await profiles.get_profile(session, user.id, Mode.PRIVATE))


@router.patch("/private-profile")
async def update_private_profile(
    update: PrivateProfileUpdate, user: CurrentUser, session: SessionDep
) -> PrivateProfile:
    """Changes only the fields that are sent. Send null to clear a field."""
    profile = await profiles.update_profile(session, user.id, Mode.PRIVATE, update)
    return PrivateProfile.from_model(profile)


@router.get("/business-profile")
async def get_business_profile(user: CurrentUser, session: SessionDep) -> BusinessProfile:
    return BusinessProfile.from_model(await profiles.get_profile(session, user.id, Mode.BUSINESS))


@router.patch("/business-profile")
async def update_business_profile(
    update: BusinessProfileUpdate, user: CurrentUser, session: SessionDep
) -> BusinessProfile:
    """Changes only the fields that are sent. Send null to clear a field."""
    profile = await profiles.update_profile(session, user.id, Mode.BUSINESS, update)
    return BusinessProfile.from_model(profile)
