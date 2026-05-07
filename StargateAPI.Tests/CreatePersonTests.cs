using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;
using StargateAPI.Controllers;

namespace StargateAPI.Tests;

public class CreatePersonTests : IClassFixture<StargateApiFactory>
{
    private readonly StargateApiFactory _factory;

    public CreatePersonTests(StargateApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePerson_WithNewName_ReturnsCreatedPersonIdAndPersistsPerson()
    {
        var client = _factory.CreateClient();
        var name = $"Test Person {Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/Person", name);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreatePersonResult>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Id > 0);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StargateContext>();
        var person = await context.People.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);

        Assert.NotNull(person);
        Assert.Equal(result.Id, person.Id);
    }

    [Fact]
    public async Task CreatePerson_WithLeadingAndTrailingWhitespace_TrimsName()
    {
        var client = _factory.CreateClient();
        var name = $"Trimmed Person {Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/Person", $"  {name}  ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StargateContext>();
        var trimmedPerson = await context.People.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);
        var untrimmedPerson = await context.People.AsNoTracking().SingleOrDefaultAsync(x => x.Name == $"  {name}  ");

        Assert.NotNull(trimmedPerson);
        Assert.Null(untrimmedPerson);
    }

    [Fact]
    public async Task CreatePerson_WithAllowedPunctuation_ReturnsCreatedPerson()
    {
        var client = _factory.CreateClient();
        var name = $"Anne O'Neil-Smith {Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/Person", name);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StargateContext>();
        var person = await context.People.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);

        Assert.NotNull(person);
    }

    [Fact]
    public async Task CreatePerson_WithUnsupportedSpecialCharacter_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/Person", "Invalid Person!");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
    }

    [Fact]
    public async Task CreatePerson_WithExistingName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/Person", "John Doe");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
    }

    [Fact]
    public async Task CreatePerson_WithExistingNameDifferentCasing_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/Person", "john doe");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePerson_WithBlankName_ReturnsBadRequest(string name)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/Person", name);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
    }

    [Fact]
    public async Task GetPeople_ReturnsAllSeededPeople()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Person");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetPeopleResult>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains(result.People, person => person.Name == "John Doe");
        Assert.Contains(result.People, person => person.Name == "Jane Doe");
    }

    [Fact]
    public async Task GetPeople_AfterCreatePerson_IncludesNewPerson()
    {
        var client = _factory.CreateClient();
        var name = $"Created Person {Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/Person", name);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var getResponse = await client.GetAsync("/Person");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<GetPeopleResult>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains(result.People, person => person.Name == name);
    }

    [Fact]
    public async Task GetPersonByName_WithExistingName_ReturnsPerson()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Person/John%20Doe");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetPersonByNameResult>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Person);
        Assert.Equal("John Doe", result.Person.Name);
        Assert.Equal(1, result.Person.PersonId);
    }

    [Fact]
    public async Task GetPersonByName_WithDifferentCasing_ReturnsPerson()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Person/john%20doe");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetPersonByNameResult>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Person);
        Assert.Equal("John Doe", result.Person.Name);
        Assert.Equal(1, result.Person.PersonId);
    }

    [Fact]
    public async Task GetPersonByName_WithMissingName_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Person/Missing%20Person");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetPersonByNameResult>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Person not found", result.Message);
        Assert.Equal((int)HttpStatusCode.NotFound, result.ResponseCode);
        Assert.Null(result.Person);
    }

    [Fact]
    public async Task UpdatePersonName_WithExistingPerson_ChangesName()
    {
        var client = _factory.CreateClient();
        var oldName = $"Old Name {Guid.NewGuid():N}";
        var newName = $"New Name {Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/Person", oldName);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync(
            $"/Person/{Uri.EscapeDataString(oldName)}",
            new UpdatePersonNameRequest { NewName = newName });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updateResult = await updateResponse.Content.ReadFromJsonAsync<UpdatePersonNameResult>();

        Assert.NotNull(updateResult);
        Assert.True(updateResult.Success);
        Assert.Equal(newName, updateResult.Name);

        var oldNameResponse = await client.GetAsync($"/Person/{Uri.EscapeDataString(oldName)}");
        var newNameResponse = await client.GetAsync($"/Person/{Uri.EscapeDataString(newName)}");

        Assert.Equal(HttpStatusCode.NotFound, oldNameResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newNameResponse.StatusCode);
    }

    [Fact]
    public async Task UpdatePersonName_WithLeadingAndTrailingWhitespace_TrimsNewName()
    {
        var client = _factory.CreateClient();
        var oldName = $"Old Name {Guid.NewGuid():N}";
        var newName = $"Trimmed New Name {Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/Person", oldName);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync(
            $"/Person/{Uri.EscapeDataString($"  {oldName}  ")}",
            new UpdatePersonNameRequest { NewName = $"  {newName}  " });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StargateContext>();
        var trimmedPerson = await context.People.AsNoTracking().SingleOrDefaultAsync(x => x.Name == newName);
        var untrimmedPerson = await context.People.AsNoTracking().SingleOrDefaultAsync(x => x.Name == $"  {newName}  ");

        Assert.NotNull(trimmedPerson);
        Assert.Null(untrimmedPerson);
    }

    [Fact]
    public async Task UpdatePersonName_WithMissingPerson_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/Person/Missing%20Person",
            new UpdatePersonNameRequest { NewName = "Renamed Person" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetPersonByNameResult>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Person not found", result.Message);
        Assert.Equal((int)HttpStatusCode.NotFound, result.ResponseCode);
    }

    [Fact]
    public async Task UpdatePersonName_WithExistingNewName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var oldName = $"Old Name {Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/Person", oldName);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var response = await client.PutAsJsonAsync(
            $"/Person/{Uri.EscapeDataString(oldName)}",
            new UpdatePersonNameRequest { NewName = "Jane Doe" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
    }

    [Fact]
    public async Task UpdatePersonName_WithUnsupportedSpecialCharacter_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/Person/John%20Doe",
            new UpdatePersonNameRequest { NewName = "Invalid Person!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
    }

    [Fact]
    public async Task UpdatePersonName_WithBlankNewName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/Person/John%20Doe",
            new UpdatePersonNameRequest { NewName = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
    }
}

public class StargateApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"stargate-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<StargateContext>>();
            services.AddDbContext<StargateContext>(options =>
                options.UseSqlite($"Data Source={_databasePath}"));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StargateContext>();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        SqliteConnection.ClearAllPools();
        TryDelete(_databasePath);
        TryDelete($"{_databasePath}-shm");
        TryDelete($"{_databasePath}-wal");
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
