# Student Guide: Cookie Authentication, Roles, Tests, and CI

This guide shows how to add cookie-based authentication and role-based authorization to an ASP.NET Core controller API. It uses this Student Registration API as the working example and finishes by adding integration tests and a GitHub Actions workflow.

## What you will build

The completed API has:

- a login endpoint that creates an HTTP-only authentication cookie;
- a logout endpoint that removes the cookie;
- a current-user endpoint that reads identity and role claims;
- `Viewer` and `Admin` roles;
- read endpoints available to both roles;
- write endpoints available only to administrators;
- integration tests that send and retain real cookies;
- a GitHub Actions workflow that restores, builds, and tests the solution.

## 1. Define role names

Create `Authorization/AppRoles.cs`:

```csharp
namespace StudentRegistration.Api.Authorization;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Viewer = "Viewer";
}
```

Constants prevent spelling differences between the login claims, controller attributes, and tests. Role values are case-sensitive, so `"Admin"` and `"admin"` should not be mixed.

## 2. Register cookie authentication

In `Program.cs`, import the cookie authentication namespace:

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
```

Register authentication before building the application:

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "StudentRegistration.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
```

The redirect event handlers are important for APIs. The default cookie handler redirects unauthenticated users to an HTML login page. An API should return:

- `401 Unauthorized` when the request has no valid identity;
- `403 Forbidden` when the user is authenticated but lacks the required role.

Add the middleware in this order:

```csharp
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

Authentication must run before authorization so that ASP.NET Core can create `HttpContext.User` before checking access rules.

## 3. Create the authentication controller

Create `Controllers/AuthController.cs`. A successful login:

1. validates the supplied credentials;
2. selects the user's role;
3. creates name and role claims;
4. creates a `ClaimsPrincipal`;
5. calls `SignInAsync` to issue the encrypted cookie.

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistration.Api.Authorization;

namespace StudentRegistration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var role = (request.Username, request.Password) switch
        {
            ("viewer", "viewer123") => AppRoles.Viewer,
            ("admin", "admin123") => AppRoles.Admin,
            _ => null
        };

        if (role is null)
        {
            return Unauthorized();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, role)
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        return Ok(new LoginResponse(request.Username, role));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<LoginResponse> Me()
    {
        var username = User.Identity?.Name;
        var role = User.FindFirstValue(ClaimTypes.Role);

        return username is null || role is null
            ? Unauthorized()
            : Ok(new LoginResponse(username, role));
    }
}

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Username, string Role);
```

`[AllowAnonymous]` is required on login when a controller or global policy requires authentication. Logout and `/me` require an authenticated cookie.

The sample credentials are suitable only for a classroom demonstration. Production applications must use a user store or identity provider and compare password hashes rather than hard-coding plain-text passwords.

## 4. Protect controllers by role

Require either role at controller level:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Viewer}")]
public class StudentController : ControllerBase
{
}
```

Comma-separated roles mean "Admin or Viewer." Add an Admin-only requirement to write actions:

```csharp
[HttpPost]
[Authorize(Roles = AppRoles.Admin)]
public async Task<ActionResult<Student>> AddStudent(Student student)
{
    // Add the student.
}
```

Apply the same Admin-only attribute to update and delete actions. The effective access rules are:

| Request | Anonymous | Viewer | Admin |
|---------|-----------|--------|-------|
| GET students | 401 | Allowed | Allowed |
| POST student | 401 | 403 | Allowed |
| PUT student | 401 | 403 | Allowed |
| DELETE student | 401 | 403 | Allowed |

## 5. Test cookie authentication

Add the ASP.NET Core test-host package to the test project:

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.12" />
```

Expose the generated `Program` type to `WebApplicationFactory` by adding this after `app.Run()`:

```csharp
public partial class Program;
```

Database migration should not run in integration tests:

```csharp
if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<StudentDBContext>();
    await context.Database.MigrateAsync();
}
```

Create a factory that selects the `Testing` environment and replaces database-backed services with mocks:

```csharp
public sealed class StudentApiFactory : WebApplicationFactory<Program>
{
    public Mock<IStudentService> StudentService { get; } = CreateStudentService();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IStudentService>();
            services.AddSingleton(StudentService.Object);
        });
    }
}
```

Configure the client to preserve cookies and disable automatic redirects:

```csharp
private HttpClient CreateClient() =>
    _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });
```

`HandleCookies = true` makes the test client send the login cookie on later requests. `AllowAutoRedirect = false` ensures tests observe the real `401` or `403` response instead of following a redirect.

Test at least these cases:

- anonymous users receive `401`;
- Viewer login succeeds;
- Viewer reads succeed;
- Viewer writes receive `403`;
- Admin writes succeed;
- invalid credentials receive `401`;
- `/api/auth/me` contains the expected name and role;
- logout removes access to protected endpoints.

Run the tests locally:

```powershell
dotnet test .\StudentRegistration.Api.Tests\StudentRegistration.Api.Tests.csproj
```

## 6. Add a GitHub Actions unit-test workflow

Create `.github/workflows/unit-tests.yml`:

```yaml
name: Unit tests

on:
  push:
  pull_request:

permissions:
  contents: read

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
      - name: Check out repository
        uses: actions/checkout@v4

      - name: Set up .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Restore
        run: dotnet restore StudentRegistration.Api.slnx

      - name: Build
        run: dotnet build StudentRegistration.Api.slnx --configuration Release --no-restore

      - name: Test
        run: dotnet test StudentRegistration.Api.Tests/StudentRegistration.Api.Tests.csproj --configuration Release --no-build --verbosity normal
```

The workflow runs for every push and pull request. It uses separate restore, build, and test steps so failures clearly show which stage failed. `--no-restore` and `--no-build` avoid repeating work.

Ensure the test project is included in the solution:

```powershell
dotnet sln .\StudentRegistration.Api.slnx add .\StudentRegistration.Api.Tests\StudentRegistration.Api.Tests.csproj
```

## 7. Try the API manually

Use `StudentRegistration.Api/StudentRegistration.Api.http` or another HTTP client that retains cookies:

```http
POST http://localhost:5022/api/auth/login
Content-Type: application/json

{
  "username": "viewer",
  "password": "viewer123"
}

###

GET http://localhost:5022/api/student

###

POST http://localhost:5022/api/auth/logout
```

The cookie is returned in the login response's `Set-Cookie` header and sent in later requests through the `Cookie` request header.

## Production checklist

Before using cookie authentication in a production application:

- store users and password hashes securely;
- require HTTPS and configure the cookie's `SecurePolicy`;
- choose an appropriate cookie lifetime;
- add account lockout and login rate limiting;
- add anti-forgery protection to state-changing cookie-authenticated requests;
- validate all request models;
- do not log passwords or cookie values;
- keep authorization tests for every protected operation;
- use a secrets provider rather than committing credentials.
