"""API errors.

Every error response has the same JSON shape: `{"code": "...", "message": "..."}`, plus `details`
for validation errors. `code` is stable and meant for clients; `message` is for humans.
"""

from http import HTTPStatus

from fastapi import FastAPI, Request
from fastapi.encoders import jsonable_encoder
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from starlette.exceptions import HTTPException


class ErrorResponse(BaseModel):
    code: str
    message: str
    details: list[dict] | None = None


class ApiError(Exception):
    status_code: int = 400
    code: str = "bad_request"
    message: str = "Bad request"

    def __init__(self, message: str | None = None) -> None:
        super().__init__(message or self.message)
        self.message = message or self.message


class NotAuthenticatedError(ApiError):
    status_code, code, message = 401, "not_authenticated", "Missing, invalid or expired token"


class InvalidCredentialsError(ApiError):
    status_code, code, message = 401, "invalid_credentials", "Wrong username or password"


class UsernameTakenError(ApiError):
    status_code, code, message = 409, "username_taken", "Username is already taken"


class UserNotFoundError(ApiError):
    status_code, code, message = 404, "user_not_found", "User not found"


class PictureNotFoundError(ApiError):
    status_code, code, message = 404, "picture_not_found", "The user has no picture"


class NotVisibleError(ApiError):
    status_code, code, message = 409, "not_visible", "You must be visible in this mode to search"


class CannotSaveSelfError(ApiError):
    status_code, code, message = 422, "cannot_save_self", "You cannot save yourself as a contact"


class PictureTooLargeError(ApiError):
    status_code, code, message = 413, "picture_too_large", "The picture is too large"


class UnsupportedPictureTypeError(ApiError):
    status_code = 415
    code = "unsupported_picture_type"
    message = "Pictures must be JPEG, PNG or WebP"


async def _api_error(request: Request, error: Exception) -> JSONResponse:
    assert isinstance(error, ApiError)
    headers = {"WWW-Authenticate": "Bearer"} if error.status_code == 401 else None
    return JSONResponse(
        ErrorResponse(code=error.code, message=error.message).model_dump(exclude_none=True),
        status_code=error.status_code,
        headers=headers,
    )


async def _http_error(request: Request, error: Exception) -> JSONResponse:
    # Errors raised by the framework itself, e.g. unknown path (404) or wrong method (405).
    assert isinstance(error, HTTPException)
    status = HTTPStatus(error.status_code)
    return JSONResponse(
        ErrorResponse(code=status.name.lower(), message=status.phrase).model_dump(
            exclude_none=True
        ),
        status_code=error.status_code,
        headers=error.headers,
    )


async def _validation_error(request: Request, error: Exception) -> JSONResponse:
    assert isinstance(error, RequestValidationError)
    details = [
        {"location": list(e["loc"]), "message": e["msg"], "type": e["type"]}
        for e in jsonable_encoder(error.errors())
    ]
    return JSONResponse(
        ErrorResponse(
            code="validation_error", message="The request is invalid", details=details
        ).model_dump(exclude_none=True),
        status_code=422,
    )


def register_error_handlers(app: FastAPI) -> None:
    app.add_exception_handler(ApiError, _api_error)
    app.add_exception_handler(HTTPException, _http_error)
    app.add_exception_handler(RequestValidationError, _validation_error)
