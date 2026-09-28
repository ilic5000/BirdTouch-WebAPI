"""Initial schema.

Revision ID: 0001
Revises:
Create Date: 2026-09-28 20:20:27.352758
"""

from collections.abc import Sequence

import sqlalchemy as sa
from alembic import op

revision: str = "0001"
down_revision: str | None = None
branch_labels: str | Sequence[str] | None = None
depends_on: str | Sequence[str] | None = None


def upgrade() -> None:
    op.create_table(
        "users",
        sa.Column("id", sa.Uuid(), nullable=False),
        sa.Column("username", sa.String(length=64), nullable=False),
        sa.Column("normalized_username", sa.String(length=64), nullable=False),
        sa.Column("password_hash", sa.Text(), nullable=False),
        sa.Column("failed_login_count", sa.Integer(), nullable=False),
        sa.Column("lockout_end", sa.DateTime(timezone=True), nullable=True),
        sa.Column(
            "created_at",
            sa.DateTime(timezone=True),
            server_default=sa.text("now()"),
            nullable=False,
        ),
        sa.PrimaryKeyConstraint("id", name=op.f("pk_users")),
        sa.UniqueConstraint("normalized_username", name=op.f("uq_users_normalized_username")),
    )
    op.create_table(
        "business_profiles",
        sa.Column("user_id", sa.Uuid(), nullable=False),
        sa.Column("company_name", sa.Text(), nullable=True),
        sa.Column("email", sa.Text(), nullable=True),
        sa.Column("phone_number", sa.Text(), nullable=True),
        sa.Column("website", sa.Text(), nullable=True),
        sa.Column("address", sa.Text(), nullable=True),
        sa.Column("description", sa.Text(), nullable=True),
        sa.Column(
            "updated_at",
            sa.DateTime(timezone=True),
            server_default=sa.text("now()"),
            nullable=False,
        ),
        sa.ForeignKeyConstraint(
            ["user_id"],
            ["users.id"],
            name=op.f("fk_business_profiles_user_id_users"),
            ondelete="CASCADE",
        ),
        sa.PrimaryKeyConstraint("user_id", name=op.f("pk_business_profiles")),
    )
    op.create_table(
        "private_profiles",
        sa.Column("user_id", sa.Uuid(), nullable=False),
        sa.Column("first_name", sa.Text(), nullable=True),
        sa.Column("last_name", sa.Text(), nullable=True),
        sa.Column("email", sa.Text(), nullable=True),
        sa.Column("phone_number", sa.Text(), nullable=True),
        sa.Column("date_of_birth", sa.Date(), nullable=True),
        sa.Column("address", sa.Text(), nullable=True),
        sa.Column("description", sa.Text(), nullable=True),
        sa.Column("facebook_url", sa.Text(), nullable=True),
        sa.Column("twitter_url", sa.Text(), nullable=True),
        sa.Column("linkedin_url", sa.Text(), nullable=True),
        sa.Column(
            "updated_at",
            sa.DateTime(timezone=True),
            server_default=sa.text("now()"),
            nullable=False,
        ),
        sa.ForeignKeyConstraint(
            ["user_id"],
            ["users.id"],
            name=op.f("fk_private_profiles_user_id_users"),
            ondelete="CASCADE",
        ),
        sa.PrimaryKeyConstraint("user_id", name=op.f("pk_private_profiles")),
    )
    op.create_table(
        "profile_pictures",
        sa.Column("user_id", sa.Uuid(), nullable=False),
        sa.Column(
            "mode",
            sa.Enum("private", "business", name="mode", native_enum=False, length=16),
            nullable=False,
        ),
        sa.Column("content_type", sa.String(length=32), nullable=False),
        sa.Column("data", sa.LargeBinary(), nullable=False),
        sa.Column(
            "updated_at",
            sa.DateTime(timezone=True),
            server_default=sa.text("now()"),
            nullable=False,
        ),
        sa.ForeignKeyConstraint(
            ["user_id"],
            ["users.id"],
            name=op.f("fk_profile_pictures_user_id_users"),
            ondelete="CASCADE",
        ),
        sa.PrimaryKeyConstraint("user_id", "mode", name=op.f("pk_profile_pictures")),
    )
    op.create_table(
        "saved_contacts",
        sa.Column("user_id", sa.Uuid(), nullable=False),
        sa.Column(
            "mode",
            sa.Enum("private", "business", name="mode", native_enum=False, length=16),
            nullable=False,
        ),
        sa.Column("contact_id", sa.Uuid(), nullable=False),
        sa.Column(
            "created_at",
            sa.DateTime(timezone=True),
            server_default=sa.text("now()"),
            nullable=False,
        ),
        sa.ForeignKeyConstraint(
            ["contact_id"],
            ["users.id"],
            name=op.f("fk_saved_contacts_contact_id_users"),
            ondelete="CASCADE",
        ),
        sa.ForeignKeyConstraint(
            ["user_id"],
            ["users.id"],
            name=op.f("fk_saved_contacts_user_id_users"),
            ondelete="CASCADE",
        ),
        sa.PrimaryKeyConstraint("user_id", "mode", "contact_id", name=op.f("pk_saved_contacts")),
    )
    op.create_index(
        op.f("ix_saved_contacts_contact_id"), "saved_contacts", ["contact_id"], unique=False
    )
    op.create_table(
        "visibilities",
        sa.Column("user_id", sa.Uuid(), nullable=False),
        sa.Column(
            "mode",
            sa.Enum("private", "business", name="mode", native_enum=False, length=16),
            nullable=False,
        ),
        sa.Column("latitude", sa.Double(), nullable=False),
        sa.Column("longitude", sa.Double(), nullable=False),
        sa.Column("updated_at", sa.DateTime(timezone=True), nullable=False),
        sa.ForeignKeyConstraint(
            ["user_id"],
            ["users.id"],
            name=op.f("fk_visibilities_user_id_users"),
            ondelete="CASCADE",
        ),
        sa.PrimaryKeyConstraint("user_id", "mode", name=op.f("pk_visibilities")),
    )
    op.create_index(
        op.f("ix_visibilities_updated_at"), "visibilities", ["updated_at"], unique=False
    )


def downgrade() -> None:
    op.drop_index(op.f("ix_visibilities_updated_at"), table_name="visibilities")
    op.drop_table("visibilities")
    op.drop_index(op.f("ix_saved_contacts_contact_id"), table_name="saved_contacts")
    op.drop_table("saved_contacts")
    op.drop_table("profile_pictures")
    op.drop_table("private_profiles")
    op.drop_table("business_profiles")
    op.drop_table("users")
