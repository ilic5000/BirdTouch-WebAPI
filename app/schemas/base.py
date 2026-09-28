from typing import Annotated

from pydantic import AfterValidator, BaseModel, ConfigDict, StringConstraints
from pydantic.alias_generators import to_camel


class ApiModel(BaseModel):
    """Base class for response bodies. JSON keys are camelCase."""

    model_config = ConfigDict(
        alias_generator=to_camel,
        validate_by_name=True,
        serialize_by_alias=True,
        from_attributes=True,
    )


class RequestModel(ApiModel):
    """Base class for request bodies. Only camelCase keys; unknown keys are rejected."""

    model_config = ConfigDict(validate_by_name=False, extra="forbid")


def _blank_to_none(value: str) -> str | None:
    return value or None


# Optional free-text fields: surrounding whitespace is removed, and blank text is stored as null.
ShortText = Annotated[
    str, StringConstraints(strip_whitespace=True, max_length=200), AfterValidator(_blank_to_none)
]
LongText = Annotated[
    str, StringConstraints(strip_whitespace=True, max_length=2000), AfterValidator(_blank_to_none)
]
