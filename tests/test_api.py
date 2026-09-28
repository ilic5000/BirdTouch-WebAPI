"""End-to-end tests of the HTTP API."""

import uuid
from datetime import timedelta

import httpx
import pytest

from app.core.database import get_session_factory
from app.services.visibility import remove_inactive

JPEG = b"\xff\xd8\xff\xe0" + b"\x00" * 100
PNG = b"\x89PNG\r\n\x1a\n" + b"\x00" * 100

# The tests share one database, so each test places its users at its own spot on the map.
_latitudes = iter(range(-80, 80))


class User:
    def __init__(self, id: str, username: str, token: str) -> None:
        self.id, self.username = id, username
        self.headers = {"Authorization": f"Bearer {token}"}


async def register(client: httpx.AsyncClient, password: str = "secret123", **extra) -> User:
    username = f"user-{uuid.uuid4().hex[:12]}"
    response = await client.post(
        "/api/v1/auth/register", json={"username": username, "password": password, **extra}
    )
    assert response.status_code == 201, response.text
    body = response.json()
    return User(body["user"]["id"], username, body["accessToken"])


async def set_private_profile(client: httpx.AsyncClient, user: User, **fields) -> None:
    response = await client.patch("/api/v1/me/private-profile", headers=user.headers, json=fields)
    assert response.status_code == 200, response.text


async def make_visible(
    client: httpx.AsyncClient, user: User, mode: str, latitude: float, longitude: float = 0
) -> None:
    response = await client.put(
        f"/api/v1/me/visibility/{mode}",
        headers=user.headers,
        json={"latitude": latitude, "longitude": longitude},
    )
    assert response.status_code == 200, response.text


async def nearby(client: httpx.AsyncClient, user: User, mode: str, radius: object):
    return await client.get(
        f"/api/v1/nearby/{mode}", headers=user.headers, params={"radiusKm": radius}
    )


async def test_health(client: httpx.AsyncClient) -> None:
    assert (await client.get("/health")).json() == {"status": "ok"}


async def test_register_and_login(client: httpx.AsyncClient) -> None:
    response = await client.post(
        "/api/v1/auth/register",
        json={"username": "Ana.Anic", "password": "secret123", "firstName": "Ana"},
    )
    assert response.status_code == 201
    body = response.json()
    assert body["tokenType"] == "bearer"
    assert set(body) == {"accessToken", "tokenType", "expiresAt", "user"}
    assert set(body["user"]) == {"id", "username", "createdAt"}
    assert body["user"]["username"] == "Ana.Anic"

    response = await client.post(
        "/api/v1/auth/login", json={"username": "ana.anic", "password": "secret123"}
    )
    assert response.status_code == 200
    assert response.json()["user"]["id"] == body["user"]["id"]
    headers = {"Authorization": f"Bearer {response.json()['accessToken']}"}

    me = (await client.get("/api/v1/me", headers=headers)).json()
    assert (me["id"], me["username"]) == (body["user"]["id"], "Ana.Anic")
    profile = (await client.get("/api/v1/me/private-profile", headers=headers)).json()
    assert profile["firstName"] == "Ana"


@pytest.mark.parametrize(
    "body",
    [
        {"username": "ok-name", "password": "short"},
        {"username": "bad name", "password": "secret123"},
        {"username": "ab", "password": "secret123"},
        {"Username": "ok-name", "Password": "secret123"},
    ],
)
async def test_register_validation(client: httpx.AsyncClient, body: dict) -> None:
    response = await client.post("/api/v1/auth/register", json=body)
    assert response.status_code == 422
    assert response.json()["code"] == "validation_error"
    assert response.json()["details"]


async def test_usernames_are_unique_regardless_of_case(client: httpx.AsyncClient) -> None:
    user = await register(client)
    response = await client.post(
        "/api/v1/auth/register", json={"username": user.username.upper(), "password": "secret123"}
    )
    assert response.status_code == 409
    assert response.json() == {"code": "username_taken", "message": "Username is already taken"}


async def test_username_availability(client: httpx.AsyncClient) -> None:
    url = "/api/v1/auth/username-availability"
    response = await client.get(url, params={"username": "free-name-123"})
    assert response.json() == {"username": "free-name-123", "available": True}
    user = await register(client)
    response = await client.get(url, params={"username": user.username.upper()})
    assert response.json()["available"] is False


async def test_account_is_locked_out_after_five_failed_logins(client: httpx.AsyncClient) -> None:
    user = await register(client)
    for _ in range(5):
        response = await client.post(
            "/api/v1/auth/login", json={"username": user.username, "password": "wrong-password"}
        )
        assert response.status_code == 401
        assert response.json()["code"] == "invalid_credentials"
    response = await client.post(
        "/api/v1/auth/login", json={"username": user.username, "password": "secret123"}
    )
    assert response.status_code == 401


async def test_requests_without_valid_token_are_rejected(client: httpx.AsyncClient) -> None:
    for headers in ({}, {"Authorization": "Bearer garbage"}):
        response = await client.get("/api/v1/me", headers=headers)
        assert response.status_code == 401
        assert response.json()["code"] == "not_authenticated"
        assert response.headers["www-authenticate"] == "Bearer"


async def test_unknown_path(client: httpx.AsyncClient) -> None:
    response = await client.get("/api/v1/nothing-here")
    assert response.status_code == 404
    assert response.json() == {"code": "not_found", "message": "Not Found"}


async def test_private_profile_patch_changes_only_sent_fields(client: httpx.AsyncClient) -> None:
    user = await register(client, firstName="Ana")
    response = await client.patch(
        "/api/v1/me/private-profile",
        headers=user.headers,
        json={"email": "ana@example.com", "dateOfBirth": "1990-05-17", "twitterUrl": "x.com/ana"},
    )
    assert response.status_code == 200
    profile = response.json()
    assert (profile["firstName"], profile["email"]) == ("Ana", "ana@example.com")
    assert (profile["dateOfBirth"], profile["twitterUrl"]) == ("1990-05-17", "x.com/ana")

    await set_private_profile(client, user, firstName=None)
    profile = (await client.get("/api/v1/me/private-profile", headers=user.headers)).json()
    assert (profile["firstName"], profile["email"]) == (None, "ana@example.com")


async def test_business_profile(client: httpx.AsyncClient) -> None:
    user = await register(client)
    profile = (await client.get("/api/v1/me/business-profile", headers=user.headers)).json()
    assert profile["companyName"] is None
    response = await client.patch(
        "/api/v1/me/business-profile",
        headers=user.headers,
        json={"companyName": "Bob Inc", "website": "bob.example"},
    )
    assert (response.json()["companyName"], response.json()["website"]) == (
        "Bob Inc",
        "bob.example",
    )


async def test_pictures(client: httpx.AsyncClient) -> None:
    user = await register(client)
    other = await register(client)
    profile = (await client.get("/api/v1/me/private-profile", headers=user.headers)).json()
    assert profile["pictureUrl"] is None

    response = await client.put(
        "/api/v1/me/pictures/private",
        headers={**user.headers, "Content-Type": "image/jpeg"},
        content=JPEG,
    )
    assert response.status_code == 204
    url = (await client.get("/api/v1/me/private-profile", headers=user.headers)).json()[
        "pictureUrl"
    ]
    assert url.startswith(f"/api/v1/users/{user.id}/pictures/private?v=")

    # Any logged in user can download it.
    response = await client.get(url, headers=other.headers)
    assert response.status_code == 200
    assert (response.content, response.headers["content-type"]) == (JPEG, "image/jpeg")

    # A new picture gets a new URL.
    await client.put("/api/v1/me/pictures/private", headers=user.headers, content=PNG)
    new_url = (await client.get("/api/v1/me/private-profile", headers=user.headers)).json()[
        "pictureUrl"
    ]
    assert new_url != url

    # The business profile has its own picture.
    business = (await client.get("/api/v1/me/business-profile", headers=user.headers)).json()
    assert business["pictureUrl"] is None

    response = await client.delete("/api/v1/me/pictures/private", headers=user.headers)
    assert response.status_code == 204
    response = await client.get(url, headers=other.headers)
    assert (response.status_code, response.json()["code"]) == (404, "picture_not_found")


async def test_picture_validation(client: httpx.AsyncClient, monkeypatch) -> None:
    user = await register(client)
    response = await client.put("/api/v1/me/pictures/private", headers=user.headers, content=b"GIF")
    assert (response.status_code, response.json()["code"]) == (415, "unsupported_picture_type")

    too_large = JPEG + b"\x00" * (5 * 1024 * 1024)
    response = await client.put(
        "/api/v1/me/pictures/private", headers=user.headers, content=too_large
    )
    assert (response.status_code, response.json()["code"]) == (413, "picture_too_large")


async def test_visibility(client: httpx.AsyncClient) -> None:
    user = await register(client)
    assert (await client.get("/api/v1/me/visibility", headers=user.headers)).json() == []

    await make_visible(client, user, "private", 44.8, 20.4)
    await make_visible(client, user, "private", 44.9, 20.5)  # Update.
    await make_visible(client, user, "business", 44.9, 20.5)
    visibility = (await client.get("/api/v1/me/visibility", headers=user.headers)).json()
    assert [(v["mode"], v["latitude"]) for v in visibility] == [
        ("business", 44.9),
        ("private", 44.9),
    ]

    response = await client.delete("/api/v1/me/visibility/private", headers=user.headers)
    assert response.status_code == 204
    visibility = (await client.get("/api/v1/me/visibility", headers=user.headers)).json()
    assert [v["mode"] for v in visibility] == ["business"]


async def test_nearby_private_users(client: httpx.AsyncClient) -> None:
    latitude = next(_latitudes)
    searcher, near, far, without_contact = [await register(client) for _ in range(4)]
    for user in (near, far):
        await set_private_profile(client, user, firstName="X", phoneNumber="123")
    await set_private_profile(client, without_contact, firstName="Silent")

    await make_visible(client, searcher, "private", latitude)
    await make_visible(client, near, "private", latitude + 0.001)
    await make_visible(client, without_contact, "private", latitude + 0.001)
    await make_visible(client, far, "private", latitude + 0.5)  # About 55 km away.
    await make_visible(client, far, "business", latitude)  # Other modes don't matter.

    response = await nearby(client, searcher, "private", 1)
    assert response.status_code == 200
    [found] = response.json()
    assert (found["userId"], found["profile"]["firstName"]) == (near.id, "X")
    assert found["distanceKm"] == pytest.approx(0.11, abs=0.01)

    response = await nearby(client, searcher, "private", 100)
    assert [user["userId"] for user in response.json()] == [near.id, far.id]  # Nearest first.


async def test_nearby_business_users(client: httpx.AsyncClient) -> None:
    latitude = next(_latitudes)
    searcher, business, incomplete = [await register(client) for _ in range(3)]
    await client.patch(
        "/api/v1/me/business-profile",
        headers=business.headers,
        json={"companyName": "B", "email": "b@example.com"},
    )
    for user in (searcher, business, incomplete):
        await make_visible(client, user, "business", latitude)

    [found] = (await nearby(client, searcher, "business", 1)).json()
    assert (found["userId"], found["profile"]["companyName"]) == (business.id, "B")


async def test_nearby_requires_being_visible(client: httpx.AsyncClient) -> None:
    user = await register(client)
    response = await nearby(client, user, "private", 5)
    assert (response.status_code, response.json()["code"]) == (409, "not_visible")
    await make_visible(client, user, "private", next(_latitudes))
    assert (await nearby(client, user, "private", 5)).status_code == 200
    for radius in (0, 501, "abc"):
        assert (await nearby(client, user, "private", radius)).status_code == 422


async def test_contacts(client: httpx.AsyncClient) -> None:
    user, contact_a, contact_b = [await register(client, firstName=n) for n in "UAB"]
    url = "/api/v1/me/contacts"
    for contact in (contact_a, contact_b, contact_a):  # Saving twice is fine.
        response = await client.put(f"{url}/private/{contact.id}", headers=user.headers)
        assert response.status_code == 204
    await client.put(f"{url}/business/{contact_b.id}", headers=user.headers)

    saved = (await client.get(f"{url}/private", headers=user.headers)).json()
    assert [(c["userId"], c["profile"]["firstName"]) for c in saved] == [
        (contact_a.id, "A"),
        (contact_b.id, "B"),
    ]
    saved = (await client.get(f"{url}/business", headers=user.headers)).json()
    assert [c["userId"] for c in saved] == [contact_b.id]

    response = await client.delete(f"{url}/private/{contact_a.id}", headers=user.headers)
    assert response.status_code == 204
    saved = (await client.get(f"{url}/private", headers=user.headers)).json()
    assert [c["userId"] for c in saved] == [contact_b.id]

    response = await client.put(f"{url}/private/{uuid.uuid4()}", headers=user.headers)
    assert (response.status_code, response.json()["code"]) == (404, "user_not_found")
    response = await client.put(f"{url}/private/{user.id}", headers=user.headers)
    assert (response.status_code, response.json()["code"]) == (422, "cannot_save_self")


async def test_delete_account(client: httpx.AsyncClient) -> None:
    user, other = await register(client), await register(client)
    await client.put(f"/api/v1/me/contacts/private/{user.id}", headers=other.headers)
    await client.put("/api/v1/me/pictures/private", headers=user.headers, content=JPEG)
    await make_visible(client, user, "private", next(_latitudes))

    assert (await client.delete("/api/v1/me", headers=user.headers)).status_code == 204
    assert (await client.get("/api/v1/me", headers=user.headers)).status_code == 401
    assert (await client.get("/api/v1/me/contacts/private", headers=other.headers)).json() == []
    response = await client.post(
        "/api/v1/auth/login", json={"username": user.username, "password": "secret123"}
    )
    assert response.status_code == 401


async def test_inactive_users_are_hidden(client: httpx.AsyncClient) -> None:
    user = await register(client)
    await make_visible(client, user, "private", next(_latitudes))

    async with get_session_factory()() as session:
        assert await remove_inactive(session, timedelta(hours=1)) == 0
        assert await remove_inactive(session, timedelta(0)) >= 1

    assert (await client.get("/api/v1/me/visibility", headers=user.headers)).json() == []
