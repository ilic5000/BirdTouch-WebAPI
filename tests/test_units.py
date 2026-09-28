import uuid

import pytest
from pydantic import SecretStr, ValidationError

from app.core.config import get_settings
from app.core.security import (
    InvalidTokenError,
    create_access_token,
    hash_password,
    read_access_token,
    verify_password,
)
from app.errors import UnsupportedPictureTypeError
from app.schemas.profiles import PrivateProfileUpdate
from app.services.pictures import detect_content_type


def test_password_hashing() -> None:
    hashed = hash_password("secret123")
    assert hashed.startswith("$argon2id$")
    assert verify_password("secret123", hashed) == (True, None)
    assert verify_password("secret124", hashed)[0] is False


def test_token_roundtrip() -> None:
    user_id = uuid.uuid4()
    token, _ = create_access_token(get_settings(), user_id)
    assert read_access_token(get_settings(), token) == user_id


def test_token_signed_with_another_key_is_rejected() -> None:
    other = get_settings().model_copy(update={"jwt_secret_key": SecretStr("x" * 40)})
    token, _ = create_access_token(other, uuid.uuid4())
    with pytest.raises(InvalidTokenError):
        read_access_token(get_settings(), token)


def test_profile_update_tracks_sent_fields_and_cleans_text() -> None:
    update = PrivateProfileUpdate.model_validate(
        {"firstName": "  Ana ", "lastName": "   ", "dateOfBirth": "1990-05-17"}
    )
    assert update.model_fields_set == {"first_name", "last_name", "date_of_birth"}
    assert (update.first_name, update.last_name) == ("Ana", None)
    assert update.date_of_birth.isoformat() == "1990-05-17"


@pytest.mark.parametrize(
    "body", [{"FirstName": "Ana"}, {"first_name": "Ana"}, {"email": "not-an-email"}]
)
def test_profile_update_rejects_unknown_keys_and_invalid_values(body: dict) -> None:
    with pytest.raises(ValidationError):
        PrivateProfileUpdate.model_validate(body)


def test_picture_type_is_detected_from_content() -> None:
    assert detect_content_type(b"\xff\xd8\xff\xe0rest") == "image/jpeg"
    assert detect_content_type(b"\x89PNG\r\n\x1a\nrest") == "image/png"
    assert detect_content_type(b"RIFF\x00\x00\x00\x00WEBPrest") == "image/webp"
    with pytest.raises(UnsupportedPictureTypeError):
        detect_content_type(b"GIF89a")
