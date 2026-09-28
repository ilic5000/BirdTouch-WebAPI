from fastapi import APIRouter

from app.api.routes import auth, contacts, me, pictures, profiles, visibility
from app.errors import ErrorResponse

api_router = APIRouter(
    prefix="/api/v1",
    responses={
        422: {"model": ErrorResponse, "description": "Invalid request"},
    },
)
for module in (auth, me, profiles, pictures, visibility, contacts):
    api_router.include_router(module.router)
