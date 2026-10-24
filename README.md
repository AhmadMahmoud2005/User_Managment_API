# User Management API

A small ASP.NET Core minimal API for managing users in memory. It demonstrates JWT authentication, CRUD endpoints, OpenAPI/Swagger documentation, request metrics, rate limiting, structured logging, and centralized error handling.

## Features

- In-memory user storage with sample data loaded at startup
- JWT login and bearer-token authentication
- User creation, listing, lookup, update, and deletion
- Pagination for the user list with `skip` and `take`
- OpenAPI and Swagger UI in the Development environment
- Fixed-window rate limiting for selected endpoints
- Per-route request counters exposed through a response header and endpoint
- Request duration and status logging through Serilog
- JSON error responses for unhandled exceptions
- Data annotation validation for names, email addresses, and ages

## Requirements

- .NET SDK 10.0 or later

## Run locally

```bash
dotnet restore
dotnet run --launch-profile http
```

The HTTP profile listens on `http://localhost:5047`. The HTTPS profile listens on `https://localhost:7169` and `http://localhost:5047`:

```bash
dotnet run --launch-profile https
```

When running in Development, Swagger UI is available at:

```text
http://localhost:5047/swagger
```

The OpenAPI document is available at:

```text
http://localhost:5047/openapi/v1.json
```

## Authentication

Login uses the seeded credentials below:

| Username | Password |
| --- | --- |
| `Ahmad` | `1234` |
| `Sara` | `abcd` |
| `Omar` | `pass123` |

Request a token with query-string parameters:

```bash
curl -X POST "http://localhost:5047/login?username=Ahmad&password=1234"
```

Use the returned token for protected requests:

```bash
curl -X POST "http://localhost:5047/users" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"name":"Nour","email":"nour@example.com","age":26}'
```

The JWT issuer, audience, and signing key are configured in `appsettings.json`. Replace the development signing key and use a secure configuration provider or environment variables before deploying.

## API endpoints

| Method | Route | Description | Authentication |
| --- | --- | --- | --- |
| `GET` | `/` | Returns a root health-style response | None |
| `GET` | `/users?skip=0&take=10` | Returns a paginated list of users | None |
| `GET` | `/users/{id}` | Returns one user by ID | None |
| `POST` | `/login?username=...&password=...` | Returns a JWT for valid seeded credentials | None |
| `POST` | `/users` | Creates a user and assigns an ID | Required |
| `PUT` | `/users/{id}` | Updates an existing user | Required outside Development |
| `DELETE` | `/users/{id}` | Deletes an existing user | Required outside Development |
| `GET` | `/users/counts` | Returns request counts by path | None |
| `GET` | `/test-error` | Triggers the error-handling middleware | None |

### List users

```bash
curl "http://localhost:5047/users?skip=0&take=3"
```

### Get a user

```bash
curl "http://localhost:5047/users/1"
```

### Update a user

```bash
curl -X PUT "http://localhost:5047/users/1" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"name":"Ahmad Mahmoud","email":"ahmad.mahmoud@example.com","age":26}'
```

### Delete a user

```bash
curl -X DELETE "http://localhost:5047/users/1" \
  -H "Authorization: Bearer <token>"
```

## Validation

User payloads must satisfy these rules:

- `Name` is required
- `Email` must be a valid email address
- `Age` must be between 18 and 120

Invalid create or update payloads return `400 Bad Request`.

## Rate limiting and request metrics

`GET /users` and `GET /users/counts` use a fixed-window limiter allowing two requests per five seconds, with a queue limit of two. Rejected requests return `429 Too Many Requests`.

Every request receives an `X-Request-Count` response header containing the number of requests observed for that path. The `/users/counts` endpoint returns the counters as JSON.

## Project structure

```text
.
├── Extensions/       Middleware registration extensions
├── Middlewares/      Error handling, request logging, and request counting
├── Models/           User model and validation helper
├── Services/         Request counter service
├── Program.cs        Application configuration and endpoint definitions
└── appsettings.json  Logging and JWT configuration
```

## Notes

- Data is stored only in memory and is reset whenever the application restarts.
- The API does not currently use a database or persistent user store.
- Swagger and the OpenAPI endpoint are enabled only in Development.
- Update and delete routes are anonymous in Development for easier local testing, but require JWT authorization in other environments.

