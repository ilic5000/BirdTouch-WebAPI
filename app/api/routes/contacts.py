import uuid

from fastapi import APIRouter, status

from app.api.deps import REQUIRES_LOGIN, CurrentUser, SessionDep
from app.errors import ErrorResponse
from app.models import Mode
from app.schemas.profiles import BusinessProfile, Contact, PrivateProfile
from app.services import contacts

router = APIRouter(prefix="/me/contacts", tags=["contacts"], responses=REQUIRES_LOGIN)


@router.get("/private")
async def list_private_contacts(
    user: CurrentUser, session: SessionDep
) -> list[Contact[PrivateProfile]]:
    """Saved private contacts with their current profiles, oldest first."""
    rows = await contacts.list_contacts(session, user.id, Mode.PRIVATE)
    return [
        Contact[PrivateProfile](
            user_id=profile.user_id, saved_at=saved_at, profile=PrivateProfile.from_model(profile)
        )
        for profile, saved_at in rows
    ]


@router.get("/business")
async def list_business_contacts(
    user: CurrentUser, session: SessionDep
) -> list[Contact[BusinessProfile]]:
    """Saved business contacts with their current profiles, oldest first."""
    rows = await contacts.list_contacts(session, user.id, Mode.BUSINESS)
    return [
        Contact[BusinessProfile](
            user_id=profile.user_id, saved_at=saved_at, profile=BusinessProfile.from_model(profile)
        )
        for profile, saved_at in rows
    ]


@router.put(
    "/{mode}/{contact_id}",
    status_code=status.HTTP_204_NO_CONTENT,
    responses={
        404: {"model": ErrorResponse, "description": "`user_not_found`"},
        422: {"model": ErrorResponse, "description": "`cannot_save_self`"},
    },
)
async def save_contact(
    mode: Mode, contact_id: uuid.UUID, user: CurrentUser, session: SessionDep
) -> None:
    """Saves a user to the contacts. Saving an already saved contact is not an error."""
    await contacts.save_contact(session, user.id, mode, contact_id)


@router.delete("/{mode}/{contact_id}", status_code=status.HTTP_204_NO_CONTENT)
async def remove_contact(
    mode: Mode, contact_id: uuid.UUID, user: CurrentUser, session: SessionDep
) -> None:
    """Removes a user from the contacts. Removing a contact that isn't saved is not an error."""
    await contacts.remove_contact(session, user.id, mode, contact_id)
