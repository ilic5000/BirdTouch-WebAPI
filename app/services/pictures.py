import uuid
from datetime import UTC, datetime

from sqlalchemy import delete
from sqlalchemy.dialects.postgresql import insert
from sqlalchemy.ext.asyncio import AsyncSession

from app.errors import PictureNotFoundError, UnsupportedPictureTypeError
from app.models import Mode, ProfilePicture

# Recognized by their first bytes; the Content-Type sent by the client is not trusted.
_SIGNATURES = {
    b"\xff\xd8\xff": "image/jpeg",
    b"\x89PNG\r\n\x1a\n": "image/png",
}
SUPPORTED_CONTENT_TYPES = ("image/jpeg", "image/png", "image/webp")


def detect_content_type(data: bytes) -> str:
    for signature, content_type in _SIGNATURES.items():
        if data.startswith(signature):
            return content_type
    if data[:4] == b"RIFF" and data[8:12] == b"WEBP":
        return "image/webp"
    raise UnsupportedPictureTypeError


async def set_picture(session: AsyncSession, user_id: uuid.UUID, mode: Mode, data: bytes) -> None:
    values = {
        "content_type": detect_content_type(data),
        "data": data,
        "updated_at": datetime.now(UTC),
    }
    await session.execute(
        insert(ProfilePicture)
        .values(user_id=user_id, mode=mode, **values)
        .on_conflict_do_update(index_elements=["user_id", "mode"], set_=values)
    )
    await session.commit()


async def delete_picture(session: AsyncSession, user_id: uuid.UUID, mode: Mode) -> None:
    await session.execute(
        delete(ProfilePicture).where(ProfilePicture.user_id == user_id, ProfilePicture.mode == mode)
    )
    await session.commit()


async def get_picture(session: AsyncSession, user_id: uuid.UUID, mode: Mode) -> ProfilePicture:
    picture = await session.get(ProfilePicture, (user_id, mode))
    if picture is None:
        raise PictureNotFoundError
    return picture
