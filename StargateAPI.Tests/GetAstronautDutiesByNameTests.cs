using System.Net;
using System.Net.Http.Json;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using StargateAPI.Controllers;

namespace StargateAPI.Tests;

public class GetAstronautDutiesByNameTests : IClassFixture<StargateApiFactory>
{
    private static int _dutyStartDateOffset;
    private readonly StargateApiFactory _factory;

    public GetAstronautDutiesByNameTests(StargateApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAstronautDutiesByName_WithAstronaut_ReturnsPersonAndDuties()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/AstronautDuty/John%20Doe");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetAstronautDutiesByNameResult>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Person);
        Assert.Equal("John Doe", result.Person.Name);
        Assert.Equal("1LT", result.Person.CurrentRank);
        Assert.Equal("Commander", result.Person.CurrentDutyTitle);

        var duty = Assert.Single(result.AstronautDuties);
        Assert.Equal(1, duty.PersonId);
        Assert.Equal("1LT", duty.Rank);
        Assert.Equal("Commander", duty.DutyTitle);
        Assert.Equal(new DateTime(2024, 1, 1), duty.DutyStartDate);
        Assert.Null(duty.DutyEndDate);
    }

    [Fact]
    public async Task GetAstronautDutiesByName_WithDifferentNameCasing_ReturnsPersonAndDuties()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/AstronautDuty/john%20doe");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetAstronautDutiesByNameResult>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Person);
        Assert.Equal("John Doe", result.Person.Name);
        Assert.Single(result.AstronautDuties);
    }

    [Fact]
    public async Task GetAstronautDutiesByName_WithPersonWhoHasNoDuties_ReturnsEmptyDuties()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/AstronautDuty/Jane%20Doe");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetAstronautDutiesByNameResult>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Person);
        Assert.Equal("Jane Doe", result.Person.Name);
        Assert.Empty(result.AstronautDuties);
    }

    [Fact]
    public async Task GetAstronautDutiesByName_WithMissingPerson_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/AstronautDuty/Missing%20Person");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetAstronautDutiesByNameResult>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Person not found", result.Message);
        Assert.Equal((int)HttpStatusCode.NotFound, result.ResponseCode);
        Assert.Null(result.Person);
        Assert.Empty(result.AstronautDuties);
    }

    [Fact]
    public async Task CreateAstronautDuty_ForPersonWithNoDuties_CreatesCurrentDuty()
    {
        var client = _factory.CreateClient();
        var name = $"Duty Person {Guid.NewGuid():N}";
        var dutyStartDate = NextDutyStartDate();

        var createPersonResponse = await client.PostAsJsonAsync("/Person", name);

        Assert.Equal(HttpStatusCode.OK, createPersonResponse.StatusCode);

        var createDutyResponse = await client.PostAsJsonAsync("/AstronautDuty", new CreateAstronautDuty
        {
            Name = name,
            Rank = "CAPT",
            DutyTitle = "Pilot",
            DutyStartDate = dutyStartDate
        });

        Assert.Equal(HttpStatusCode.OK, createDutyResponse.StatusCode);

        var createDutyResult = await createDutyResponse.Content.ReadFromJsonAsync<CreateAstronautDutyResult>();

        Assert.NotNull(createDutyResult);
        Assert.True(createDutyResult.Success);
        Assert.True(createDutyResult.Id > 0);

        var getDutyResponse = await client.GetAsync($"/AstronautDuty/{Uri.EscapeDataString(name)}");
        var getDutyResult = await getDutyResponse.Content.ReadFromJsonAsync<GetAstronautDutiesByNameResult>();

        Assert.NotNull(getDutyResult);
        Assert.True(getDutyResult.Success);
        Assert.NotNull(getDutyResult.Person);
        Assert.Equal("CAPT", getDutyResult.Person.CurrentRank);
        Assert.Equal("Pilot", getDutyResult.Person.CurrentDutyTitle);

        var duty = Assert.Single(getDutyResult.AstronautDuties);
        Assert.Equal("CAPT", duty.Rank);
        Assert.Equal("Pilot", duty.DutyTitle);
        Assert.Equal(dutyStartDate, duty.DutyStartDate);
        Assert.Null(duty.DutyEndDate);
    }

    [Fact]
    public async Task CreateAstronautDuty_WithDifferentNameCasing_CreatesDutyForExistingPerson()
    {
        var client = _factory.CreateClient();
        var name = $"Duty Person {Guid.NewGuid():N}";
        var dutyStartDate = NextDutyStartDate();

        var createPersonResponse = await client.PostAsJsonAsync("/Person", name);

        Assert.Equal(HttpStatusCode.OK, createPersonResponse.StatusCode);

        var createDutyResponse = await client.PostAsJsonAsync("/AstronautDuty", new CreateAstronautDuty
        {
            Name = name.ToLowerInvariant(),
            Rank = "CAPT",
            DutyTitle = "Pilot",
            DutyStartDate = dutyStartDate
        });

        Assert.Equal(HttpStatusCode.OK, createDutyResponse.StatusCode);

        var getDutyResponse = await client.GetAsync($"/AstronautDuty/{Uri.EscapeDataString(name)}");
        var getDutyResult = await getDutyResponse.Content.ReadFromJsonAsync<GetAstronautDutiesByNameResult>();

        Assert.NotNull(getDutyResult);
        Assert.True(getDutyResult.Success);
        Assert.NotNull(getDutyResult.Person);
        Assert.Equal(name, getDutyResult.Person.Name);
        Assert.Single(getDutyResult.AstronautDuties);
    }

    [Fact]
    public async Task CreateAstronautDuty_WithLeadingAndTrailingWhitespace_TrimsFields()
    {
        var client = _factory.CreateClient();
        var name = $"Duty Person {Guid.NewGuid():N}";
        var dutyStartDate = NextDutyStartDate();

        var createPersonResponse = await client.PostAsJsonAsync("/Person", name);

        Assert.Equal(HttpStatusCode.OK, createPersonResponse.StatusCode);

        var createDutyResponse = await client.PostAsJsonAsync("/AstronautDuty", new CreateAstronautDuty
        {
            Name = $"  {name}  ",
            Rank = "  CAPT  ",
            DutyTitle = "  Pilot  ",
            DutyStartDate = dutyStartDate
        });

        Assert.Equal(HttpStatusCode.OK, createDutyResponse.StatusCode);

        var getDutyResponse = await client.GetAsync($"/AstronautDuty/{Uri.EscapeDataString(name)}");
        var getDutyResult = await getDutyResponse.Content.ReadFromJsonAsync<GetAstronautDutiesByNameResult>();

        Assert.NotNull(getDutyResult);
        Assert.True(getDutyResult.Success);
        Assert.NotNull(getDutyResult.Person);
        Assert.Equal("CAPT", getDutyResult.Person.CurrentRank);
        Assert.Equal("Pilot", getDutyResult.Person.CurrentDutyTitle);

        var duty = Assert.Single(getDutyResult.AstronautDuties);
        Assert.Equal("CAPT", duty.Rank);
        Assert.Equal("Pilot", duty.DutyTitle);
    }

    [Fact]
    public async Task CreateAstronautDuty_ForPersonWithExistingDuty_ClosesPreviousDutyAndCreatesNewCurrentDuty()
    {
        var client = _factory.CreateClient();
        var name = $"Duty Person {Guid.NewGuid():N}";
        var firstDutyStartDate = NextDutyStartDate();
        var secondDutyStartDate = firstDutyStartDate.AddDays(30);

        var createPersonResponse = await client.PostAsJsonAsync("/Person", name);
        var firstDutyResponse = await client.PostAsJsonAsync("/AstronautDuty", new CreateAstronautDuty
        {
            Name = name,
            Rank = "CAPT",
            DutyTitle = "Pilot",
            DutyStartDate = firstDutyStartDate
        });
        var secondDutyResponse = await client.PostAsJsonAsync("/AstronautDuty", new CreateAstronautDuty
        {
            Name = name,
            Rank = "MAJ",
            DutyTitle = "Commander",
            DutyStartDate = secondDutyStartDate
        });

        Assert.Equal(HttpStatusCode.OK, createPersonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, firstDutyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondDutyResponse.StatusCode);

        var getDutyResponse = await client.GetAsync($"/AstronautDuty/{Uri.EscapeDataString(name)}");
        var getDutyResult = await getDutyResponse.Content.ReadFromJsonAsync<GetAstronautDutiesByNameResult>();

        Assert.NotNull(getDutyResult);
        Assert.True(getDutyResult.Success);
        Assert.NotNull(getDutyResult.Person);
        Assert.Equal("MAJ", getDutyResult.Person.CurrentRank);
        Assert.Equal("Commander", getDutyResult.Person.CurrentDutyTitle);
        Assert.Equal(2, getDutyResult.AstronautDuties.Count);

        var currentDuty = getDutyResult.AstronautDuties.Single(x => x.DutyTitle == "Commander");
        var previousDuty = getDutyResult.AstronautDuties.Single(x => x.DutyTitle == "Pilot");

        Assert.Equal(secondDutyStartDate, currentDuty.DutyStartDate);
        Assert.Null(currentDuty.DutyEndDate);
        Assert.Equal(firstDutyStartDate, previousDuty.DutyStartDate);
        Assert.Equal(secondDutyStartDate.AddDays(-1), previousDuty.DutyEndDate);
    }

    [Fact]
    public async Task CreateAstronautDuty_WithRetiredDuty_SetsCareerEndDate()
    {
        var client = _factory.CreateClient();
        var name = $"Retired Duty Person {Guid.NewGuid():N}";
        var activeDutyStartDate = NextDutyStartDate();
        var retiredDutyStartDate = activeDutyStartDate.AddDays(30);

        var createPersonResponse = await client.PostAsJsonAsync("/Person", name);
        var activeDutyResponse = await client.PostAsJsonAsync("/AstronautDuty", new CreateAstronautDuty
        {
            Name = name,
            Rank = "CAPT",
            DutyTitle = "Pilot",
            DutyStartDate = activeDutyStartDate
        });
        var retiredDutyResponse = await client.PostAsJsonAsync("/AstronautDuty", new CreateAstronautDuty
        {
            Name = name,
            Rank = "CAPT",
            DutyTitle = "RETIRED",
            DutyStartDate = retiredDutyStartDate
        });

        Assert.Equal(HttpStatusCode.OK, createPersonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, activeDutyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retiredDutyResponse.StatusCode);

        var getDutyResponse = await client.GetAsync($"/AstronautDuty/{Uri.EscapeDataString(name)}");
        var getDutyResult = await getDutyResponse.Content.ReadFromJsonAsync<GetAstronautDutiesByNameResult>();

        Assert.NotNull(getDutyResult);
        Assert.True(getDutyResult.Success);
        Assert.NotNull(getDutyResult.Person);
        Assert.Equal("Retired", getDutyResult.Person.CurrentDutyTitle);
        Assert.Equal(retiredDutyStartDate.AddDays(-1), getDutyResult.Person.CareerEndDate);
    }

    [Fact]
    public async Task CreateAstronautDuty_WithMissingPerson_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/AstronautDuty", new CreateAstronautDuty
        {
            Name = $"Missing Person {Guid.NewGuid():N}",
            Rank = "CAPT",
            DutyTitle = "Pilot",
            DutyStartDate = NextDutyStartDate()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
    }

    [Fact]
    public async Task CreateAstronautDuty_WithDuplicateDuty_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var name = $"Duplicate Duty Person {Guid.NewGuid():N}";
        var request = new CreateAstronautDuty
        {
            Name = name,
            Rank = "CAPT",
            DutyTitle = "Pilot",
            DutyStartDate = NextDutyStartDate()
        };

        var createPersonResponse = await client.PostAsJsonAsync("/Person", name);
        var firstDutyResponse = await client.PostAsJsonAsync("/AstronautDuty", request);
        var duplicateDutyResponse = await client.PostAsJsonAsync("/AstronautDuty", request);

        Assert.Equal(HttpStatusCode.OK, createPersonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, firstDutyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateDutyResponse.StatusCode);

        var result = await duplicateDutyResponse.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
    }

    public static IEnumerable<object[]> InvalidCreateAstronautDutyRequests()
    {
        yield return new object[]
        {
            new CreateAstronautDuty
            {
                Name = "",
                Rank = "CAPT",
                DutyTitle = "Pilot",
                DutyStartDate = new DateTime(2026, 1, 15)
            }
        };
        yield return new object[]
        {
            new CreateAstronautDuty
            {
                Name = "John Doe",
                Rank = "",
                DutyTitle = "Pilot",
                DutyStartDate = new DateTime(2026, 1, 15)
            }
        };
        yield return new object[]
        {
            new CreateAstronautDuty
            {
                Name = "John Doe",
                Rank = "CAPT",
                DutyTitle = "",
                DutyStartDate = new DateTime(2026, 1, 15)
            }
        };
        yield return new object[]
        {
            new CreateAstronautDuty
            {
                Name = "John Doe",
                Rank = "CAPT",
                DutyTitle = "Pilot",
                DutyStartDate = default
            }
        };
    }

    [Theory]
    [MemberData(nameof(InvalidCreateAstronautDutyRequests))]
    public async Task CreateAstronautDuty_WithInvalidRequest_ReturnsBadRequest(CreateAstronautDuty request)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/AstronautDuty", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
    }

    private static DateTime NextDutyStartDate()
    {
        var offset = Interlocked.Increment(ref _dutyStartDateOffset);

        return new DateTime(2026, 1, 1).AddDays(offset * 10);
    }
}
