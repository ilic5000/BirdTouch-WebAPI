"""Who is visible to others, where, and who is near whom."""

import uuid
from collections.abc import Sequence
from datetime import UTC, datetime, timedelta

from sqlalchemy import ColumnElement, Row, delete, func, or_, select
from sqlalchemy.dialects.postgresql import insert
from sqlalchemy.ext.asyncio import AsyncSession

from app.errors import NotVisibleError
from app.models import BusinessProfile, Mode, PrivateProfile, Visibility

EARTH_RADIUS_KM = 6371.0088


async def list_visibility(session: AsyncSession, user_id: uuid.UUID) -> Sequence[Visibility]:
    return (
        await session.scalars(
            select(Visibility).where(Visibility.user_id == user_id).order_by(Visibility.mode)
        )
    ).all()


async def set_location(
    session: AsyncSession, user_id: uuid.UUID, mode: Mode, latitude: float, longitude: float
) -> Visibility:
    """Makes the user visible in the mode at the location (or refreshes the location)."""
    values = {"latitude": latitude, "longitude": longitude, "updated_at": datetime.now(UTC)}
    visibility = await session.scalar(
        insert(Visibility)
        .values(user_id=user_id, mode=mode, **values)
        .on_conflict_do_update(index_elements=["user_id", "mode"], set_=values)
        .returning(Visibility)
    )
    await session.commit()
    assert visibility is not None
    return visibility


async def hide(session: AsyncSession, user_id: uuid.UUID, mode: Mode) -> None:
    await session.execute(
        delete(Visibility).where(Visibility.user_id == user_id, Visibility.mode == mode)
    )
    await session.commit()


async def find_nearby(
    session: AsyncSession, user_id: uuid.UUID, mode: Mode, radius_km: float | None, limit: int
) -> Sequence[Row[tuple[PrivateProfile | BusinessProfile, float, float, float]]]:
    """(profile, distance in km, latitude, longitude) of other users visible in the mode nearby,
    nearest first.

    `radius_km=None` searches without a distance limit.

    Profiles without enough information to be useful are left out: private profiles need
    a name and a way to get in touch, business profiles a company name and an email.
    """
    me = await session.get(Visibility, (user_id, mode))
    if me is None:
        raise NotVisibleError

    distance = _distance_km(me.latitude, me.longitude, Visibility.latitude, Visibility.longitude)
    if mode is Mode.PRIVATE:
        profile = PrivateProfile
        useful = [
            or_(_has_text(PrivateProfile.first_name), _has_text(PrivateProfile.last_name)),
            or_(
                _has_text(PrivateProfile.email),
                _has_text(PrivateProfile.phone_number),
                _has_text(PrivateProfile.facebook_url),
                _has_text(PrivateProfile.twitter_url),
                _has_text(PrivateProfile.linkedin_url),
            ),
        ]
    else:
        profile = BusinessProfile
        useful = [_has_text(BusinessProfile.company_name), _has_text(BusinessProfile.email)]

    query = (
        select(profile, distance, Visibility.latitude, Visibility.longitude)
        .join(Visibility, Visibility.user_id == profile.user_id)
        .where(Visibility.mode == mode, Visibility.user_id != user_id)
        .where(*([] if radius_km is None else [distance <= radius_km]))
        .where(*useful)
        .order_by(distance)
        .limit(limit)
    )
    return (await session.execute(query)).all()


async def remove_inactive(session: AsyncSession, older_than: timedelta) -> int:
    """Hides users whose location was not refreshed for `older_than`."""
    result = await session.execute(
        delete(Visibility).where(Visibility.updated_at < datetime.now(UTC) - older_than)
    )
    await session.commit()
    return result.rowcount


def _has_text(column: ColumnElement[str | None]) -> ColumnElement[bool]:
    return func.coalesce(column, "") != ""


def _distance_km(
    latitude: float,
    longitude: float,
    latitude_column: ColumnElement[float],
    longitude_column: ColumnElement[float],
) -> ColumnElement[float]:
    """Great-circle distance (haversine formula), computed by the database."""
    half_delta_latitude = func.radians(latitude_column - latitude) / 2
    half_delta_longitude = func.radians(longitude_column - longitude) / 2
    a = func.power(func.sin(half_delta_latitude), 2) + func.cos(func.radians(latitude)) * func.cos(
        func.radians(latitude_column)
    ) * func.power(func.sin(half_delta_longitude), 2)
    return 2 * EARTH_RADIUS_KM * func.asin(func.least(1.0, func.sqrt(a)))
