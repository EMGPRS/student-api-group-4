using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StudentRegistration.Api.Controllers;
using StudentRegistration.Api.Models;
using StudentRegistration.Api.Services;

namespace StudentRegistration.Api.Tests;

public sealed class AuthorizationTests : IClassFixture<StudentApiFactory>
{
    private readonly StudentApiFactory _factory;

    public AuthorizationTests(StudentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStudents_WithoutLogin_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/student");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CanReadStudents()
    {
        using var client = CreateClient();
        await LoginAsync(client, "viewer", "viewer123");

        var response = await client.GetAsync("/api/student");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CannotDeleteStudent()
    {
        using var client = CreateClient();
        await LoginAsync(client, "viewer", "viewer123");
        _factory.StudentService.Invocations.Clear();

        var response = await client.DeleteAsync("/api/student/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _factory.StudentService.Verify(
            service => service.DeleteStudentAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Admin_CanDeleteStudent()
    {
        using var client = CreateClient();
        await LoginAsync(client, "admin", "admin123");
        _factory.StudentService.Invocations.Clear();

        var response = await client.DeleteAsync("/api/student/1");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        _factory.StudentService.Verify(
            service => service.DeleteStudentAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task InvalidCredentials_ReturnUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "viewer", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_AfterLogin_ReturnsAuthenticatedUserAndRole()
    {
        using var client = CreateClient();
        await LoginAsync(client, "admin", "admin123");

        var response = await client.GetAsync("/api/auth/me");
        var user = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(user);
        Assert.Equal("admin", user.Username);
        Assert.Equal("Admin", user.Role);
    }

    [Fact]
    public async Task Logout_RemovesAuthenticationCookie()
    {
        using var client = CreateClient();
        await LoginAsync(client, "viewer", "viewer123");

        var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        var protectedResponse = await client.GetAsync("/api/student");

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static async Task LoginAsync(
        HttpClient client,
        string username,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password });

        response.EnsureSuccessStatusCode();
    }
}

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

    private static Mock<IStudentService> CreateStudentService()
    {
        var service = new Mock<IStudentService>();
        service.Setup(value => value.GetStudentsAsync())
            .ReturnsAsync(new List<Student>());
        service.Setup(value => value.DeleteStudentAsync(1))
            .ReturnsAsync(true);
        return service;
    }
}
