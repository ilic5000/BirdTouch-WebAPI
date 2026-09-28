"""Contacts a user saved (per mode)."""

import uuid
from collections.abc import Sequence
from datetime import datetime

from sqlalchemy import Row, delete, select
from sqlalchemy.dialects.postgresql import insert
from sqlalchemy.ext.asyncio import AsyncSession

from app.errors import CannotSaveSelfError, UserNotFoundError
from app.models import PROFILE_MODELS, BusinessProfile, Mode, PrivateProfile, SavedContact, User


async def list_contacts(
    session: AsyncSession, user_id: uuid.UUID, mode: Mode
) -> Sequence[Row[tuple[PrivateProfile | BusinessProfile, datetime]]]:
    """(profile, saved at) of the user's contacts in the mode, oldest first."""
    profile = PROFILE_MODELS[mode]
    query = (
        select(profile, SavedContact.created_at)
        .join(SavedContact, SavedContact.contact_id == profile.user_id)
        .where(SavedContact.user_id == user_id, SavedContact.mode == mode)
        .order_by(SavedContact.created_at)
    )
    return (await session.execute(query)).all()


async def save_contact(
    session: AsyncSession, user_id: uuid.UUID, mode: Mode, contact_id: uuid.UUID
) -> None:
    """Idempotent: saving an already saved contact does nothing."""
    if contact_id == user_id:
        raise CannotSaveSelfError
    if await session.get(User, contact_id) is None:
        raise UserNotFoundError
    await session.execute(
        insert(SavedContact)
        .values(user_id=user_id, mode=mode, contact_id=contact_id)
        .on_conflict_do_nothing()
    )
    await session.commit()


async def remove_contact(
    session: AsyncSession, user_id: uuid.UUID, mode: Mode, contact_id: uuid.UUID
) -> None:
    """Idempotent: removing a contact that is not saved does nothing."""
    await session.execute(
        delete(SavedContact).where(
            SavedContact.user_id == user_id,
            SavedContact.mode == mode,
            SavedContact.contact_id == contact_id,
        )
    )
    await session.commit()
