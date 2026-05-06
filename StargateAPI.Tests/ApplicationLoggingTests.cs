using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests;

public class ApplicationLoggingTests : IClassFixture<StargateApiFactory>
{
    private readonly StargateApiFactory _factory;

    public ApplicationLoggingTests(StargateApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SuccessfulRequest_WritesInformationLog()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Person/John%20Doe");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var log = await GetLatestLogAsync("GetPersonByName", "Information");

        Assert.NotNull(log);
        Assert.Equal("GetPersonByName completed successfully.", log.Message);
        Assert.Null(log.ExceptionMessage);
        Assert.Null(log.ExceptionStackTrace);
    }

    [Fact]
    public async Task UnsuccessfulResponse_WritesWarningLog()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Person/Missing%20Person");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var log = await GetLatestLogAsync("GetPersonByName", "Warning");

        Assert.NotNull(log);
        Assert.Equal("GetPersonByName completed with response 404: Person not found", log.Message);
        Assert.Null(log.ExceptionMessage);
        Assert.Null(log.ExceptionStackTrace);
    }

    [Fact]
    public async Task ExceptionRequest_WritesErrorLog()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/AstronautDuty", new CreateAstronautDuty
        {
            Name = $"Missing Person {Guid.NewGuid():N}",
            Rank = "CAPT",
            DutyTitle = "Pilot",
            DutyStartDate = new DateTime(2026, 1, 15)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var log = await GetLatestLogAsync("CreateAstronautDuty", "Error");

        Assert.NotNull(log);
        Assert.Equal("CreateAstronautDuty failed with an unhandled exception.", log.Message);
        Assert.Equal("Bad Request", log.ExceptionMessage);
        Assert.NotNull(log.ExceptionStackTrace);
    }

    private async Task<ApplicationLog?> GetLatestLogAsync(string operation, string logLevel)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StargateContext>();

        return await context.ApplicationLogs
            .AsNoTracking()
            .Where(log => log.Operation == operation && log.LogLevel == logLevel)
            .OrderByDescending(log => log.Id)
            .FirstOrDefaultAsync();
    }
}
