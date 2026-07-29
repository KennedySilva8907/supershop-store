using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SuperShop.Api.Middleware;
using SuperShop.Domain.Exceptions;

namespace SuperShop.UnitTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    public static TheoryData<Exception, int> Cases => new()
    {
        { new ValidationException([new ValidationFailure("Email", "O email não é válido.")]), 400 },
        { new UnauthorizedException("Token inválido."), 401 },
        { new ForbiddenException("Sem permissão."), 403 },
        { NotFoundException.For("Produto", "axis-none"), 404 },
        { new ConflictException("Transição inválida."), 409 },
        { new InsufficientStockException("SS-SAP-001-41", 5, 2), 422 },
        { new InvalidOperationException("boom"), 500 }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Maps_exception_to_status_and_problem_json(Exception exception, int expectedStatus)
    {
        var (context, body) = await InvokeAsync(exception);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        var problem = JsonDocument.Parse(body).RootElement;
        Assert.Equal(expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Validation_failure_exposes_errors_by_field()
    {
        var exception = new ValidationException([
            new ValidationFailure("Email", "O email não é válido."),
            new ValidationFailure("Password", "Mínimo de 8 caracteres.")
        ]);

        var (_, body) = await InvokeAsync(exception);

        var errors = JsonDocument.Parse(body).RootElement.GetProperty("errors");

        Assert.Equal("O email não é válido.", errors.GetProperty("email")[0].GetString());
        Assert.Equal("Mínimo de 8 caracteres.", errors.GetProperty("password")[0].GetString());
    }

    [Fact]
    public async Task Unexpected_failure_hides_the_exception_in_production()
    {
        var (_, body) = await InvokeAsync(new InvalidOperationException("connection string leaked"), "Production");

        var problem = JsonDocument.Parse(body).RootElement;

        Assert.False(problem.TryGetProperty("exception", out _));
        Assert.DoesNotContain("connection string leaked", body);
    }

    [Fact]
    public async Task Unexpected_failure_includes_the_exception_in_development()
    {
        var (_, body) = await InvokeAsync(new InvalidOperationException("boom"));

        Assert.Contains("boom", JsonDocument.Parse(body).RootElement.GetProperty("exception").GetString());
    }

    private static async Task<(HttpContext Context, string Body)> InvokeAsync(
        Exception exception,
        string environmentName = "Development")
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception,
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            new StubEnvironment(environmentName));

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        return (context, body);
    }

    private sealed class StubEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "SuperShop.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
