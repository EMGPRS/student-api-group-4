# Student Registration

ASP.NET Core solution for managing student records. The API uses cookie authentication and role-based authorization with `Viewer` and `Admin` demonstration accounts.

For a step-by-step classroom tutorial, see [Student Guide: Cookie Authentication, Roles, Tests, and CI](./COOKIE_AUTHENTICATION_AND_WORKFLOW_README.md).

## Projects

- `StudentRegistration.Api`: controller-based REST API backed by SQL Server and Entity Framework Core.
- `StudentRegistration.Api.Tests`: xUnit controller and authorization tests.
- `StudentRegistration.Web`: web client.

## Prerequisites

- .NET 10 SDK
- SQL Server or SQL Server Express
- A trusted ASP.NET Core development HTTPS certificate for local HTTPS

## Configure and run

1. Update `StudentRegistration.Api/appsettings.json` if your SQL Server instance is not `Local\SQLEXPRESS`.
2. From the solution directory, restore and build:

   ```powershell
   dotnet restore .\StudentRegistration.Api.slnx
   dotnet build .\StudentRegistration.Api.slnx --no-restore
   ```

3. Run the API:

   ```powershell
   dotnet run --project .\StudentRegistration.Api\StudentRegistration.Api.csproj
   ```

The development profiles expose Swagger at `http://localhost:5022/swagger` or `https://localhost:7147/swagger`. Database migrations are applied when the API starts outside the `Testing` environment.

## Authentication

Call `POST /api/auth/login` with one of the demonstration accounts. A successful login returns an HTTP-only `StudentRegistration.Auth` session cookie. Clients must send that cookie on later requests.

| Username | Password | Role | Permissions |
|----------|----------|------|-------------|
| `viewer` | `viewer123` | Viewer | Read and search students |
| `admin` | `admin123` | Admin | Read, search, create, update, and delete students |

Example:

```http
POST http://localhost:5022/api/auth/login
Content-Type: application/json

{
  "username": "viewer",
  "password": "viewer123"
}
```

Authentication endpoints:

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/login` | Validate demonstration credentials and issue the session cookie |
| GET | `/api/auth/me` | Return the current username and role |
| POST | `/api/auth/logout` | Delete the session cookie |

API clients receive `401 Unauthorized` when no valid cookie is present and `403 Forbidden` when the authenticated role lacks permission. The API does not redirect clients to an HTML login page.

> The hard-coded accounts are for classroom demonstration only. A production system should store password hashes and users in a database or external identity provider, use HTTPS-only cookies, configure cookie lifetime, and add anti-forgery protection to cookie-authenticated write requests.

## Role-based student access

| Operation | Route | Viewer | Admin |
|-----------|-------|--------|-------|
| List students | `GET /api/student` | Allowed | Allowed |
| Get student | `GET /api/student/{id}` | Allowed | Allowed |
| Search students | `GET /api/student/search?search=...` | Allowed | Allowed |
| Add student | `POST /api/student` | Forbidden | Allowed |
| Update student | `PUT /api/student/{id}` | Forbidden | Allowed |
| Delete student | `DELETE /api/student/{id}` | Forbidden | Allowed |

See `StudentRegistration.Api/StudentRegistration.Api.http` for runnable login, student, and logout requests. VS Code's HTTP client preserves the cookie between requests.

## Tests

Run all unit and integration tests:

```powershell
dotnet test .\StudentRegistration.Api.Tests\StudentRegistration.Api.Tests.csproj
```

The authorization tests host the API in the `Testing` environment, replace the student service with a mock, preserve cookies, and verify:

- anonymous requests receive `401`;
- Viewer reads succeed but writes receive `403`;
- Admin writes succeed;
- invalid credentials receive `401`;
- `/api/auth/me` reports the authenticated identity and role;
- logout removes access to protected endpoints.

GitHub Actions runs restore, build, and tests for pushes and pull requests using `.github/workflows/unit-tests.yml`.

## Changing roles

1. Add or change constants in `StudentRegistration.Api/Authorization/AppRoles.cs`.
2. Update the matching role claim in `AuthController`.
3. Update `[Authorize(Roles = "...")]` on the affected controller or actions.
4. Add tests for anonymous, allowed, and forbidden requests.
