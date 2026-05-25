using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SistemaEvaluacionAcademica.API.Middleware;
using SistemaEvaluacionAcademica.Domain.Exceptions;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

// ── Middleware unit tests — no Docker needed ──────────────────────────────────

/// <summary>
/// Directly invokes ExceptionHandlingMiddleware to verify exception-to-status-code mapping
/// after removing the dead NotFoundException branch.
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int statusCode, string body)> InvokeAsync(Exception thrown)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw thrown,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task DomainException_Returns400()
    {
        var (status, _) = await InvokeAsync(new DomainException("domain rule broken"));
        status.Should().Be(400);
    }

    [Fact]
    public async Task DomainException_ResponseBodyContainsMessage()
    {
        var (_, body) = await InvokeAsync(new DomainException("specific domain error"));
        body.Should().Contain("specific domain error");
    }

    [Fact]
    public async Task ConcurrencyException_Returns409()
    {
        var (status, _) = await InvokeAsync(new ConcurrencyException("race condition"));
        status.Should().Be(409);
    }

    [Fact]
    public async Task ConcurrencyException_ResponseBodyContainsMessage()
    {
        var (_, body) = await InvokeAsync(new ConcurrencyException("concurrent write detected"));
        body.Should().Contain("concurrent write detected");
    }

    [Fact]
    public async Task UnexpectedException_Returns500()
    {
        var (status, _) = await InvokeAsync(new InvalidOperationException("unexpected boom"));
        status.Should().Be(500);
    }

    [Fact]
    public async Task UnexpectedException_ResponseBodyContainsGenericMessage()
    {
        var (_, body) = await InvokeAsync(new Exception("internal detail"));
        body.Should().Contain("error interno");
        body.Should().NotContain("internal detail");
    }

    [Fact]
    public async Task NoException_DoesNotInterfereWithResponse()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns403NotUnauthorized()
    {
        var (status, _) = await InvokeAsync(new UnauthorizedAccessException("access denied"));
        status.Should().Be(403);
    }

    [Fact]
    public async Task UnauthorizedAccessException_ResponseBodyContainsDeniedMessage()
    {
        var (_, body) = await InvokeAsync(new UnauthorizedAccessException("access denied"));
        body.Should().Contain("denegado");
    }
}

// ── Integration tests — 404 via Result<T>.NotFound (requires Docker) ─────────

/// <summary>
/// Verifies that 404 responses still flow correctly from controllers via Result&lt;T&gt;.NotFound
/// after NotFoundException was removed.  Controllers produce 404 directly — the
/// ExceptionHandlingMiddleware is not involved.
/// </summary>
[Trait("Category", "Integration")]
public class NotFoundViaResultTests : IntegrationTestBase
{
    public NotFoundViaResultTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetNonExistentProfessor_Returns404()
    {
        var client = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/professors/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetNonExistentSubject_Returns404()
    {
        var client = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/subjects/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetNonExistentSection_Returns404()
    {
        var client = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/sections/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetNonExistentStudent_Returns404()
    {
        var client = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/students/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
