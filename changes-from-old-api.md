# Changes from the old API

This document was written for rewriting the BirdTouch client against the new server API. It lists
everything that changed compared to the old .NET API, which the old Xamarin client was built against.
The client has since been rewritten in Flutter (Android and iOS,
[BirdTouch-Client](https://github.com/ilic5000/BirdTouch-Client)) and uses this API as described here.

Nothing of the old API remains. Every endpoint, field name and error format changed, so
treat this as a new API.

**Source of truth:** the running server's OpenAPI spec at `/openapi.json` (interactive at `/docs`).
Generate client models from it if possible. This document explains the changes and the behaviour
the spec can't express.

## Contents

1. [Endpoint mapping](#1-endpoint-mapping)
2. [General conventions](#2-general-conventions)
3. [Authentication](#3-authentication)
4. [Errors](#4-errors)
5. [Endpoints](#5-endpoints)
6. [Field renames](#6-field-renames)
7. [Behaviour changes the client must handle](#7-behaviour-changes-the-client-must-handle)

## 1. Endpoint mapping

| Old                                                            | New                                                                  |
| -------------------------------------------------------------- | -------------------------------------------------------------------- |
| `GET api/welcome`                                              | `GET /health`                                                        |
| `POST api/users` (register)                                    | `POST /api/v1/auth/register`                                         |
| `POST api/login`                                               | `POST /api/v1/auth/login`                                            |
| `GET api/users/doesusernameexist?username=`                    | `GET /api/v1/auth/username-availability?username=` (**inverted:** `available`) |
| `DELETE api/users`                                             | `DELETE /api/v1/me`                                                  |
| (none)                                                         | `GET /api/v1/me`                                                     |
| `GET api/privateinfo`                                          | `GET /api/v1/me/private-profile`                                     |
| `PATCH api/privateinfo`                                        | `PATCH /api/v1/me/private-profile` (**partial update now**)          |
| `GET api/businessinfo`                                         | `GET /api/v1/me/business-profile`                                    |
| `PATCH api/businessinfo`                                       | `PATCH /api/v1/me/business-profile` (**partial update now**)         |
| `ProfilePictureData` (base64) inside profile JSON              | `PUT` / `DELETE /api/v1/me/pictures/{mode}` and `GET /api/v1/users/{userId}/pictures/{mode}` |
| `POST api/activeusers` (`ActiveMode` in body)                  | `PUT /api/v1/me/visibility/{mode}`                                   |
| `DELETE api/activeusers` (`ActiveMode` in body)                | `DELETE /api/v1/me/visibility/{mode}`                                |
| (none)                                                         | `GET /api/v1/me/visibility`                                          |
| `GET api/activeusers/getusersnearme?activeMode=1&radiusOfSearch=` | `GET /api/v1/nearby/private?radiusKm=&limit=`                     |
| `GET api/activeusers/getusersnearme?activeMode=2&radiusOfSearch=` | `GET /api/v1/nearby/business?radiusKm=&limit=`                    |
| `POST api/savedprivate` (replace the whole list)               | `GET /api/v1/me/contacts/private`, `PUT` / `DELETE /api/v1/me/contacts/private/{userId}` |
| `POST api/savedbusiness` (replace the whole list)              | `GET /api/v1/me/contacts/business`, `PUT` / `DELETE /api/v1/me/contacts/business/{userId}` |

`{mode}` is `private` or `business` everywhere.

## 2. General conventions

- **Base path:** `/api/v1`. The health check is `/health`, outside of it.
- **Paths** are case-sensitive and lowercase, without a trailing slash. The old API accepted any
  casing.
- **JSON keys** are camelCase in both directions. Keys are case-sensitive: `FirstName` is not
  `firstName`. The old client sent PascalCase (Newtonsoft default), which is now rejected.
- **Unknown keys in request bodies are rejected** with `422 validation_error`. The old API ignored them.
- **Modes** are the strings `"private"` and `"business"`, used as path segments. They used to be
  `1` and `2`, often sent as the strings `"1"`/`"2"`. Mode 3 ("celebrity") no longer exists.
- **Ids** are UUID strings, e.g. `"01a0e941-1eda-766e-ad74-94d872b2f84c"`.
- **Timestamps** are ISO 8601 in UTC, e.g. `"2026-09-28T18:22:34.446920Z"`.
- **Dates** (`dateOfBirth`) are ISO dates, `"1990-05-17"`. They used to be free text.
- **Status codes** follow HTTP semantics:
  - `200` with a body, `201` for registration, `204` with no body for deletes and uploads.
  - Errors: see [Errors](#4-errors).
- **Text fields** are trimmed, and blank strings are stored as `null`. Most are limited to 200
  characters; `description` allows 2000.
- **Pictures** are binary uploads and downloads, no longer base64 in JSON. See [Pictures](#pictures).

## 3. Authentication

- Register or log in to get an `accessToken`, then send `Authorization: Bearer <accessToken>` on
  every other request.
- The token is a JWT valid for 30 days (server setting `JWT_LIFETIME_DAYS`). `expiresAt` in the
  login/registration response says when it expires.
- There is **no refresh token**. When a request returns `401 not_authenticated` (token expired,
  invalid, or the account was deleted), send the user to the login screen.
- Tokens issued by the old server don't work.
- **Lockout:** after 5 failed logins in a row, the account is locked for 5 minutes. During that
  time, login returns the same `401 invalid_credentials` as a wrong password.

## 4. Errors

Every error has the same JSON body. Old errors were plain text, empty bodies or ASP.NET problem
details.

```json
{ "code": "username_taken", "message": "Username is already taken" }
```

Validation errors add `details`:

```json
{
  "code": "validation_error",
  "message": "The request is invalid",
  "details": [
    { "location": ["body", "password"], "message": "String should have at least 8 characters", "type": "string_too_short" }
  ]
}
```

Branch on `code` (stable), not on `message` (for humans, may change). `details[].location` says
which input is wrong: `["body", "<field>"]`, `["query", "<param>"]` or `["path", "<param>"]`.

| Status | `code`                     | When                                                                 |
| ------ | -------------------------- | -------------------------------------------------------------------- |
| 401    | `not_authenticated`        | Missing/invalid/expired token, or the account no longer exists       |
| 401    | `invalid_credentials`      | Login: wrong username or password, or account locked out             |
| 404    | `user_not_found`           | Saving a contact that doesn't exist                                  |
| 404    | `picture_not_found`        | Downloading a picture that doesn't exist (anymore)                   |
| 404    | `not_found`                | Unknown path                                                         |
| 405    | `method_not_allowed`       | Wrong HTTP method for the path                                       |
| 409    | `username_taken`           | Registration: username exists (case-insensitive)                     |
| 409    | `not_visible`              | Nearby search while not visible in that mode                         |
| 413    | `picture_too_large`        | Picture over 5 MB                                                    |
| 415    | `unsupported_picture_type` | Picture is not JPEG, PNG or WebP                                     |
| 422    | `validation_error`         | Invalid body, query or path parameter (see `details`)                |
| 422    | `cannot_save_self`         | Saving yourself as a contact                                         |

The old `user-creation-first-error` response header no longer exists. Registration problems are
now `422 validation_error`, with the field in `details`, or `409 username_taken`. Show your own
messages for them.

## 5. Endpoints

### Health

`GET /health` → `200 {"status": "ok"}`. No authentication. Use it to test a server address
(the old client used `api/welcome`).

### Register

`POST /api/v1/auth/register`

```json
{ "username": "ana.anic", "password": "secret123", "firstName": "Ana", "lastName": "Anić" }
```

- `username`: 3-64 characters, only letters, digits and `. _ @ + -`. It is unique regardless of
  case, and kept as typed.
- `password`: 8-128 characters, no composition rules. Old rules: at least 5 characters, one
  lowercase letter.
- `firstName`, `lastName`: optional, stored in the private profile.
- `description` can no longer be sent at registration; set it with `PATCH /api/v1/me/private-profile`.

→ `201`, the same body as login. `409 username_taken`, `422 validation_error`.

### Log in

`POST /api/v1/auth/login` with `{"username": "...", "password": "..."}`. The username is matched
case-insensitively.

→ `200`

```json
{
  "accessToken": "eyJhbGciOi...",
  "tokenType": "bearer",
  "expiresAt": "2026-10-28T18:22:34Z",
  "user": { "id": "01a0e941-...", "username": "ana.anic", "createdAt": "2026-09-28T18:22:34.4Z" }
}
```

`401 invalid_credentials`. The old API answered wrong credentials with a `302` redirect.

The old response also contained `firstname`, `lastname` and `profilePictureData`. Get them with
`GET /api/v1/me/private-profile` after login.

### Username availability

`GET /api/v1/auth/username-availability?username=ana.anic` → `200 {"username": "ana.anic", "available": false}`

This is the **opposite** of the old `userExists`. A username that breaks the username rules gets
`422`.

### Current user

- `GET /api/v1/me` → `200 {"id", "username", "createdAt"}`
- `DELETE /api/v1/me` → `204`. Permanently deletes the account and everything about it:
  profiles, pictures, visibility, the user's contacts, and the user from other users' contacts.

### Profiles

Every user has exactly one private and one business profile. Both are created empty at
registration.

`GET /api/v1/me/private-profile` → `200`

```json
{
  "firstName": "Ana",
  "lastName": "Anić",
  "email": "ana@example.com",
  "phoneNumber": "+381 60 123 4567",
  "dateOfBirth": "1990-05-17",
  "address": "Knez Mihailova 1, Beograd",
  "description": "Hi!",
  "facebookUrl": "https://facebook.com/ana",
  "twitterUrl": "https://x.com/ana",
  "linkedinUrl": null,
  "pictureUrl": "/api/v1/users/01a0e941-.../pictures/private?v=1790619754260",
  "updatedAt": "2026-09-28T18:22:34.4Z"
}
```

`GET /api/v1/me/business-profile` → `200`

```json
{
  "companyName": "Ana d.o.o.",
  "email": "office@ana.rs",
  "phoneNumber": "011 123 456",
  "website": "https://ana.rs",
  "address": "Bulevar 1, Beograd",
  "description": "We make things.",
  "pictureUrl": null,
  "updatedAt": "2026-09-28T18:22:34.4Z"
}
```

`PATCH /api/v1/me/private-profile` or `PATCH /api/v1/me/business-profile` with any subset of the
fields above, except `pictureUrl` and `updatedAt` → `200` with the updated profile.

- **Only the fields you send change.** Send `null` to clear a field. The old API was a full
  replace: omitted fields were cleared, and the picture was kept when null.
- `email` must be a valid email address. `dateOfBirth` must be `YYYY-MM-DD`.
- The profile itself has no ids. The owner is the logged in user, or the `userId` next to the
  profile in nearby and contact lists.

### Pictures

Each profile (private and business) has its own optional picture.

- **Upload / replace:** `PUT /api/v1/me/pictures/{mode}` with the raw image bytes as the request
  body (not JSON, not base64, not multipart). Set `Content-Type` to the image type, e.g.
  `image/jpeg`. The server detects the real type from the content. Accepted: JPEG, PNG, WebP.
  Max 5 MB (server setting `MAX_PICTURE_BYTES`). → `204`, or `413 picture_too_large` /
  `415 unsupported_picture_type`.
  Scale and compress on the device before uploading. A few hundred KB is plenty.
- **Delete:** `DELETE /api/v1/me/pictures/{mode}` → `204`.
- **Download:** use the `pictureUrl` of a profile. It is relative, so prefix the server's base
  URL. The download needs the `Authorization` header too. → `200` with the image bytes and
  `Content-Type`, or `404 picture_not_found`.
  `pictureUrl` is `null` when there is no picture. It changes every time the picture changes (the
  `v` parameter), and the response is sent with `Cache-Control: immutable`. Image libraries can
  therefore cache by URL forever (e.g. Coil/Glide with an auth header interceptor).

### Visibility (was "active users")

A user can be visible in private mode, business mode, both, or neither. Visibility is stored with
the user's last reported location.

- `PUT /api/v1/me/visibility/{mode}` with `{"latitude": 44.8125, "longitude": 20.4612}` → `200`

  ```json
  { "mode": "private", "latitude": 44.8125, "longitude": 20.4612, "updatedAt": "2026-09-28T18:22:34.4Z" }
  ```

  This makes the user visible in the mode or updates their location. Latitude must be between
  -90 and 90, longitude between -180 and 180.
- `DELETE /api/v1/me/visibility/{mode}` → `204`. Makes the user invisible in the mode. It isn't an
  error if they weren't visible.
- `GET /api/v1/me/visibility` → `200`: a list of the objects above, one per mode the user is
  visible in. Empty when invisible everywhere. Use it on app start to restore the visibility
  toggles. The old API had no way to ask.

### Nearby users

`GET /api/v1/nearby/private?radiusKm=5&limit=50` and `GET /api/v1/nearby/business?radiusKm=5&limit=50`

- `radiusKm`: greater than 0 and at most 500. It is a decimal number with a `.` separator. The
  old client formatted it with the phone's locale, which could produce `5,5`. Omit it to search
  without a distance limit (the client offers this in its developer options, for testing).
- `limit`: optional, 1-200, default 50.
- The user must be visible in that mode themselves, otherwise `409 not_visible`. The search is
  centered on the location from their last `PUT /api/v1/me/visibility/{mode}`.

→ `200`, nearest first, never including the user themselves:

```json
[
  {
    "userId": "01a0e941-...",
    "distanceKm": 0.11,
    "latitude": 44.8135,
    "longitude": 20.4612,
    "profile": { "firstName": "Bob", "...": "the full private profile, as above" }
  }
]
```

`/nearby/business` items have the same shape with a business profile. `latitude`/`longitude` are
the user's last location (as sent with `PUT /api/v1/me/visibility/{mode}`), so the client can show
people on a map. The old API returned no location.

Only profiles with enough information are returned:
- private: a first or last name, plus at least one of email, phone number, Facebook, Twitter or LinkedIn;
- business: a company name and an email.

In the old API, a description alone counted as a way to reach someone; it no longer does.

### Contacts (was "saved private/business users")

The server now stores the contacts. The old client kept them in local storage (SharedPreferences)
and only pushed the list of ids to the server. **The server is now the source of truth.**

- `GET /api/v1/me/contacts/private` / `GET /api/v1/me/contacts/business` → `200`, oldest first:

  ```json
  [ { "userId": "01a0e941-...", "savedAt": "2026-09-28T18:22:34.4Z", "profile": { "...": "current private/business profile" } } ]
  ```

  The profiles are always current, so there is no need to store profile copies on the device.
  When a contact deletes their account, they disappear from the list.
- `PUT /api/v1/me/contacts/{mode}/{userId}` → `204`. Saves a contact; saving one that's already
  saved is fine. `404 user_not_found`, `422 cannot_save_self`.
- `DELETE /api/v1/me/contacts/{mode}/{userId}` → `204`. Removes a contact; removing one that isn't
  saved is fine.

Private and business contacts are separate lists. The `userId` to save comes from `userId` in the
nearby results.

## 6. Field renames

Private profile (old `UserInfoModel`):

| Old key                     | New key                    | Notes                                                    |
| --------------------------- | -------------------------- | -------------------------------------------------------- |
| `Id`                        | (removed)                  | Use `userId` from nearby/contact items, or `GET /api/v1/me` for yourself |
| `FkUserId`                  | `userId`                   | On the nearby/contact item that wraps the profile        |
| `Username`                  | (removed from profiles)    | Only in `GET /api/v1/me` and the login response. Other users' usernames are no longer exposed |
| `FirstName` / `Firstname`   | `firstName`                |                                                          |
| `LastName` / `Lastname`     | `lastName`                 |                                                          |
| `Email`                     | `email`                    | Validated                                                |
| `PhoneNumber`               | `phoneNumber`              |                                                          |
| `DateOfBirth`               | `dateOfBirth`              | ISO date `YYYY-MM-DD` instead of free text               |
| `Adress`                    | `address`                  | Spelling fixed                                           |
| `Description`               | `description`              | Up to 2000 characters                                    |
| `FbLink`                    | `facebookUrl`              | The old client URL-encoded links before sending. Don't; send them as typed |
| `TwitterLink`               | `twitterUrl`               | The old server ignored it; now it is stored              |
| `LinkedInLink`              | `linkedinUrl`              |                                                          |
| `GPlusLink`                 | (removed)                  | Google+ was shut down in 2019                            |
| `ProfilePictureData`        | `pictureUrl`               | Read-only URL; upload via the pictures endpoints         |
| (none)                      | `updatedAt`                | When the profile was last changed                        |

Business profile (old `BusinessInfoModel`):

| Old key              | New key        | Notes                                             |
| -------------------- | -------------- | ------------------------------------------------- |
| `Id`, `FkUserId`     | `userId`       | On the nearby/contact item that wraps the profile |
| `CompanyName`        | `companyName`  |                                                   |
| `Email`              | `email`        | Validated                                         |
| `PhoneNumber`        | `phoneNumber`  |                                                   |
| `Website`            | `website`      | Don't URL-encode it                               |
| `Adress`             | `address`      | Spelling fixed                                    |
| `Description`        | `description`  |                                                   |
| `ProfilePictureData` | `pictureUrl`   | See pictures                                      |
| (none)               | `updatedAt`    |                                                   |

Location update (old `UserLocationUpdate`):

| Old key              | New                                     |
| -------------------- | --------------------------------------- |
| `ActiveMode` (`"1"`) | `{mode}` path segment (`private`)       |
| `LocationLatitude`   | `latitude`                              |
| `LocationLongitude`  | `longitude`                             |

Auth (old `LoginCredentials`, `LoginResponse`, `UserExistResponse`):

| Old                                    | New                                                    |
| -------------------------------------- | ------------------------------------------------------ |
| `Username`, `Password`                 | `username`, `password`                                 |
| `Firstname`, `Lastname` (register)     | `firstName`, `lastName`                                |
| `Description` (register)               | (removed, set it in the private profile)              |
| `JwtToken`                             | `accessToken` (plus `tokenType`, `expiresAt`)          |
| `User` (id, username, names, picture)  | `user` (`id`, `username`, `createdAt`)                 |
| `UserExists`                           | `available` (inverted)                                 |

## 7. Behaviour changes the client must handle

1. **Keep refreshing the location while visible.** Users whose location isn't updated for 24 hours
   (server setting) are hidden automatically. The old server had this feature but it was switched
   off. Call `PUT /api/v1/me/visibility/{mode}` periodically while the user stays visible, e.g. on
   location changes or every few minutes. Use `GET /api/v1/me/visibility` to find out whether the
   user is still visible.
2. **Contacts live on the server.** Load them with `GET /api/v1/me/contacts/{mode}`, and add or
   remove them one at a time. Drop the local dictionary and the "replace the whole list" sync.
3. **Profile updates are partial.** Send only what changed. To clear a field, send `null`.
4. **Pictures are separate from profiles.** Upload them with `PUT /api/v1/me/pictures/{mode}`, and
   load them from `pictureUrl` with the auth header. Don't embed base64 in JSON.
5. **Nearby search needs visibility.** Make the user visible in the mode first, otherwise
   `409 not_visible`. Results include `distanceKm` and are sorted by distance.
6. **Handle `401 not_authenticated` globally** by going to the login screen. There are no refresh
   tokens, and tokens stop working when the account is deleted.
7. **Validate forms like the server does** to avoid `422`:
   - username: 3-64 characters from `A-Z a-z 0-9 . _ @ + -`;
   - password: 8-128 characters;
   - email: a valid address;
   - date of birth: a date;
   - text: max 200 characters, description max 2000.
8. **Other users' usernames aren't visible.** Show names and company names from profiles instead.
