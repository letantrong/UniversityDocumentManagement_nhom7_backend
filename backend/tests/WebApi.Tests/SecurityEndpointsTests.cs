using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Approvals;
using Application.Features.AuditLogs;
using Application.Features.Notifications;
using Application.Features.Permissions;
using Application.Features.Reports;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebApi.Controllers;

namespace WebApi.Tests;

public class SecurityEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "TestScheme";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var hasRoleHeader = Request.Headers.TryGetValue("X-Test-Role", out var roleValues) &&
                                !string.IsNullOrWhiteSpace(roleValues.ToString());
            var hasUserHeader = Request.Headers.TryGetValue("X-Test-UserId", out var userValues) &&
                                !string.IsNullOrWhiteSpace(userValues.ToString());
            var hasAuthHeader = Request.Headers.ContainsKey("Authorization");

            if (!hasRoleHeader && !hasUserHeader && !hasAuthHeader)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var role = hasRoleHeader ? roleValues.ToString() : "User";
            var userId = hasUserHeader ? userValues.ToString() : Guid.NewGuid().ToString();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "test-user"),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private WebApplicationFactory<Program> CreateTestFactory(
        Action<Mock<ISender>>? configureSender = null,
        Action<IServiceCollection>? extraServices = null)
    {
        var senderMock = new Mock<ISender>();
        configureSender?.Invoke(senderMock);

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => new Mock<IApplicationDbContext>().Object);
                services.AddScoped(_ => new Mock<ICurrentUserService>().Object);
                services.AddScoped(_ => new Mock<IDateTimeService>().Object);
                services.AddScoped(_ => new Mock<IPasswordHasher>().Object);
                services.AddScoped(_ => new Mock<IJwtTokenService>().Object);
                services.AddScoped(_ => new Mock<IFileStorageService>().Object);
                services.AddScoped(_ => senderMock.Object);

                extraServices?.Invoke(services);

                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    private static HttpClient CreateAuthenticatedClient(
        WebApplicationFactory<Program> factory,
        string role = "Admin",
        string? userId = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        }

        return client;
    }

    [Theory]
    [InlineData("api/approvals/pending")]
    [InlineData("api/auditlogs")]
    [InlineData("api/notifications")]
    [InlineData("api/notifications/unread-count")]
    [InlineData("api/permissions")]
    [InlineData("api/reports/dashboard")]
    public async Task ProtectedEndpoints_WithoutToken_ShouldReturn401(string route)
    {
        var client = CreateTestFactory().CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("api/auditlogs", "Student")]
    [InlineData("api/auditlogs", "Staff")]
    [InlineData("api/auditlogs", "Manager")]
    [InlineData("api/permissions", "Student")]
    [InlineData("api/permissions", "Staff")]
    [InlineData("api/reports/dashboard", "Student")]
    [InlineData("api/reports/dashboard", "Staff")]
    [InlineData("api/approvals/pending", "Student")]
    [InlineData("api/approvals/pending", "Staff")]
    public async Task RestrictedEndpoints_WithUnauthorizedRole_ShouldReturn403(string route, string role)
    {
        var factory = CreateTestFactory();
        var client = CreateAuthenticatedClient(factory, role: role);

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUser_WithValidTestScheme_ShouldPassAuthentication()
    {
        var factory = CreateTestFactory(sender =>
        {
            sender
                .Setup(s => s.Send(It.IsAny<GetMyNotificationsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<NotificationDto>>.Success(new List<NotificationDto>()));
        });

        var client = CreateAuthenticatedClient(factory, role: "Student");
        var response = await client.GetAsync("api/notifications/unread-count");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("api/approvals/pending")]
    [InlineData("api/auditlogs")]
    [InlineData("api/notifications")]
    [InlineData("api/notifications/unread-count")]
    [InlineData("api/permissions")]
    [InlineData("api/reports/dashboard")]
    public async Task FiveControllers_GetEndpoints_WhenSenderSucceeds_ShouldReturn200(string route)
    {
        var factory = CreateTestFactory(sender =>
        {
            sender
                .Setup(s => s.Send(It.IsAny<GetPendingApprovalsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<ApprovalDto>>.Success(new List<ApprovalDto>()));

            sender
                .Setup(s => s.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PaginatedList<AuditLogDto>>.Success(
                    new PaginatedList<AuditLogDto>(new List<AuditLogDto>(), 0, 1, 10)));

            sender
                .Setup(s => s.Send(It.IsAny<GetMyNotificationsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<NotificationDto>>.Success(new List<NotificationDto>()));

            sender
                .Setup(s => s.Send(It.IsAny<GetPermissionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<PermissionDto>>.Success(new List<PermissionDto>()));

            sender
                .Setup(s => s.Send(It.IsAny<GetDashboardStatisticsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DashboardStatisticsDto>.Success(
                    new DashboardStatisticsDto(0, 0, 0, 0, 0, 0, 0, 0)));
        });

        var client = CreateAuthenticatedClient(factory, role: "Admin");
        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Notifications_WhenSenderFails_ShouldReturn400()
    {
        var factory = CreateTestFactory(sender =>
        {
            sender
                .Setup(s => s.Send(It.IsAny<GetMyNotificationsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<NotificationDto>>.Failure("Failed to load notifications"));
        });

        var client = CreateAuthenticatedClient(factory, role: "Student");
        var response = await client.GetAsync("api/notifications");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Approvals_Approve_WhenAdminOrManager_ShouldReturn200()
    {
        var approvalId = Guid.NewGuid();
        var factory = CreateTestFactory(sender =>
        {
            sender
                .Setup(s => s.Send(
                    It.Is<SubmitApprovalDecisionCommand>(c =>
                        c.ApprovalId == approvalId && c.IsApproved == true),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
        });

        var client = CreateAuthenticatedClient(factory, role: "Manager");
        var response = await client.PostAsJsonAsync(
            $"api/approvals/{approvalId}/approve",
            new ApprovalCommentRequest("Approved"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Approvals_Reject_WithReason_WhenCommandFails_ShouldReturn400()
    {
        var approvalId = Guid.NewGuid();
        const string reason = "Missing required signature";
        var factory = CreateTestFactory(sender =>
        {
            sender
                .Setup(s => s.Send(
                    It.Is<SubmitApprovalDecisionCommand>(c =>
                        c.ApprovalId == approvalId &&
                        c.IsApproved == false &&
                        c.Comments == reason),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(reason));
        });

        var client = CreateAuthenticatedClient(factory, role: "Admin");
        var response = await client.PostAsJsonAsync(
            $"api/approvals/{approvalId}/reject",
            new ApprovalCommentRequest(reason));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Notifications_UnreadCount_ShouldReturnAccurateCount()
    {
        var unreadList = new List<NotificationDto> { default!, default!, default! };
        var factory = CreateTestFactory(sender =>
        {
            sender
                .Setup(s => s.Send(
                    It.Is<GetMyNotificationsQuery>(q => q.OnlyUnread == true),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<NotificationDto>>.Success(unreadList));
        });

        var client = CreateAuthenticatedClient(factory, role: "Student");
        var response = await client.GetAsync("api/notifications/unread-count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        Assert.Equal(3, json.GetProperty("data").GetInt32());
    }

    [Fact]
    public async Task Notifications_MarkAsRead_ShouldReturn200()
    {
        var notificationId = Guid.NewGuid();
        var factory = CreateTestFactory(sender =>
        {
            sender
                .Setup(s => s.Send(
                    It.Is<MarkNotificationAsReadCommand>(c => c.NotificationId == notificationId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());
        });

        var client = CreateAuthenticatedClient(factory, role: "Student");
        var response = await client.PatchAsync($"api/notifications/{notificationId}/read", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}