using Dashboard.Infrastructure.Quotes;

namespace Dashboard.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class QuoteRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly QuoteRepository _repository;

    public QuoteRepositoryIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _repository = new QuoteRepository(fixture.Factory);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await using var db = _fixture.Factory.CreateDbContext();
        db.Quotes.AddRange(
            new Quote { Text = "First", Author = "A" },
            new Quote { Text = "Second", Author = "B" },
            new Quote { Text = "Third" });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [IntegrationFact]
    public async Task The_ordinal_is_a_position_in_id_order()
    {
        Assert.Equal(3, await _repository.GetCountAsync());

        var second = await _repository.GetByOrdinalAsync(1);

        Assert.NotNull(second);
        Assert.Equal("Second", second.Text);
    }

    [IntegrationFact]
    public async Task Out_of_range_and_negative_ordinals_return_null()
    {
        Assert.Null(await _repository.GetByOrdinalAsync(3));
        Assert.Null(await _repository.GetByOrdinalAsync(-1));
    }
}
