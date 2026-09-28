from functools import lru_cache

from pydantic import Field, SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict
from sqlalchemy import URL


class Settings(BaseSettings):
    """Application settings, read from environment variables (or a local `.env` file)."""

    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    log_level: str = "INFO"

    postgres_host: str = "localhost"
    postgres_port: int = 5432
    postgres_db: str = "birdtouch"
    postgres_user: str = "postgres"
    postgres_password: SecretStr

    jwt_secret_key: SecretStr = Field(min_length=32)
    jwt_issuer: str = "birdtouch"
    jwt_audience: str = "birdtouch"
    jwt_lifetime_days: int = Field(default=30, gt=0)

    max_picture_bytes: int = Field(default=5 * 1024 * 1024, gt=0)

    remove_inactive_users_run_every_minutes: float = Field(default=5, gt=0)
    remove_inactive_users_older_than_hours: float = Field(default=24, gt=0)

    @property
    def database_url(self) -> URL:
        return URL.create(
            drivername="postgresql+asyncpg",
            username=self.postgres_user,
            password=self.postgres_password.get_secret_value(),
            host=self.postgres_host,
            port=self.postgres_port,
            database=self.postgres_db,
        )


@lru_cache
def get_settings() -> Settings:
    return Settings()  # type: ignore[call-arg]  # required values come from the environment
