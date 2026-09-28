import uuid

from fastapi import APIRouter, Request, Response, status

from app.api.deps import REQUIRES_LOGIN, CurrentUser, SessionDep, SettingsDep
from app.errors import ErrorResponse, PictureTooLargeError
from app.models import Mode
from app.services import pictures

router = APIRouter(tags=["pictures"], responses=REQUIRES_LOGIN)

_binary = {"schema": {"type": "string", "format": "binary"}}


async def _read_body(request: Request, max_bytes: int) -> bytes:
    body = bytearray()
    async for chunk in request.stream():
        body += chunk
        if len(body) > max_bytes:
            raise PictureTooLargeError
    return bytes(body)


@router.put(
    "/me/pictures/{mode}",
    status_code=status.HTTP_204_NO_CONTENT,
    openapi_extra={
        "requestBody": {
            "required": True,
            "description": "The image file itself (not JSON, not base64).",
            "content": dict.fromkeys(pictures.SUPPORTED_CONTENT_TYPES, _binary),
        }
    },
    responses={
        413: {"model": ErrorResponse, "description": "`picture_too_large`"},
        415: {"model": ErrorResponse, "description": "`unsupported_picture_type`"},
    },
)
async def upload_picture(
    mode: Mode, request: Request, user: CurrentUser, session: SessionDep, settings: SettingsDep
) -> None:
    """Sets the picture of the user's profile in the mode. JPEG, PNG or WebP, max 5 MB."""
    data = await _read_body(request, settings.max_picture_bytes)
    await pictures.set_picture(session, user.id, mode, data)


@router.delete("/me/pictures/{mode}", status_code=status.HTTP_204_NO_CONTENT)
async def delete_picture(mode: Mode, user: CurrentUser, session: SessionDep) -> None:
    await pictures.delete_picture(session, user.id, mode)


@router.get(
    "/users/{user_id}/pictures/{mode}",
    response_class=Response,
    responses={
        200: {"content": dict.fromkeys(pictures.SUPPORTED_CONTENT_TYPES, _binary)},
        404: {"model": ErrorResponse, "description": "`picture_not_found`"},
    },
)
async def get_picture(
    user_id: uuid.UUID, mode: Mode, user: CurrentUser, session: SessionDep
) -> Response:
    """Picture of any user's profile. Use the `pictureUrl` from profiles, which is cacheable."""
    picture = await pictures.get_picture(session, user_id, mode)
    return Response(
        picture.data,
        media_type=picture.content_type,
        # pictureUrl changes whenever the picture changes, so a response never goes stale.
        headers={"Cache-Control": "private, max-age=31536000, immutable"},
    )
