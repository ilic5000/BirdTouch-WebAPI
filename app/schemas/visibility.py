from datetime import datetime

from pydantic import Field

from app.models import Mode
from app.schemas.base import ApiModel, RequestModel


class Location(RequestModel):
    latitude: float = Field(ge=-90, le=90)
    longitude: float = Field(ge=-180, le=180)


class VisibilityStatus(ApiModel):
    mode: Mode
    latitude: float
    longitude: float
    updated_at: datetime
