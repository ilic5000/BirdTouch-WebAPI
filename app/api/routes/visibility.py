from typing import Annotated

from fastapi import APIRouter, Query, status

from app.api.deps import REQUIRES_LOGIN, CurrentUser, SessionDep
from app.errors import ErrorResponse
from app.models import Mode
from app.schemas.profiles import BusinessProfile, NearbyUser, PrivateProfile
from app.schemas.visibility import Location, VisibilityStatus
from app.services import visibility

router = APIRouter(tags=["visibility"], responses=REQUIRES_LOGIN)

RadiusKm = Annotated[
    float, Query(alias="radiusKm", gt=0, le=500, description="Search radius in kilometers")
]
Limit = Annotated[int, Query(ge=1, le=200, description="Maximum number of users returned")]
_NOT_VISIBLE = {409: {"model": ErrorResponse, "description": "`not_visible`"}}


@router.get("/me/visibility")
async def get_visibility(user: CurrentUser, session: SessionDep) -> list[VisibilityStatus]:
    """The modes in which the user is currently visible, with their last location."""
    return [
        VisibilityStatus.model_validate(v)
        for v in await visibility.list_visibility(session, user.id)
    ]


@router.put("/me/visibility/{mode}")
async def set_visibility(
    mode: Mode, location: Location, user: CurrentUser, session: SessionDep
) -> VisibilityStatus:
    """Makes the user visible in the mode at the location, or updates the location.

    Call it periodically while the user wants to stay visible. Users whose location isn't
    updated for a while (24 hours by default) are hidden automatically.
    """
    result = await visibility.set_location(
        session, user.id, mode, location.latitude, location.longitude
    )
    return VisibilityStatus.model_validate(result)


@router.delete("/me/visibility/{mode}", status_code=status.HTTP_204_NO_CONTENT)
async def hide(mode: Mode, user: CurrentUser, session: SessionDep) -> None:
    await visibility.hide(session, user.id, mode)


@router.get("/nearby/private", responses=_NOT_VISIBLE)
async def nearby_private_users(
    user: CurrentUser, session: SessionDep, radius_km: RadiusKm, limit: Limit = 50
) -> list[NearbyUser[PrivateProfile]]:
    """Users visible in private mode around the user's location, nearest first.

    The user has to be visible in private mode themselves.
    """
    rows = await visibility.find_nearby(session, user.id, Mode.PRIVATE, radius_km, limit)
    return [
        NearbyUser[PrivateProfile](
            user_id=profile.user_id,
            distance_km=distance,
            profile=PrivateProfile.from_model(profile),
        )
        for profile, distance in rows
    ]


@router.get("/nearby/business", responses=_NOT_VISIBLE)
async def nearby_business_users(
    user: CurrentUser, session: SessionDep, radius_km: RadiusKm, limit: Limit = 50
) -> list[NearbyUser[BusinessProfile]]:
    """Users visible in business mode around the user's location, nearest first.

    The user has to be visible in business mode themselves.
    """
    rows = await visibility.find_nearby(session, user.id, Mode.BUSINESS, radius_km, limit)
    return [
        NearbyUser[BusinessProfile](
            user_id=profile.user_id,
            distance_km=distance,
            profile=BusinessProfile.from_model(profile),
        )
        for profile, distance in rows
    ]
