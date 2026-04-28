using System.Net.Http.Headers;
using System.Net.Http.Json;
using KnightsAndGM.Shared;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KnightsAndGM.Api.Tests;

public sealed class CampaignApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public CampaignApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task PlayerMapOnlyReturnsExploredTilesInitially()
    {
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client, CampaignRole.Player, "player-one");
        var campaignId = auth.Campaigns[0].CampaignId;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var map = await client.GetFromJsonAsync<CampaignMapResponse>($"/campaigns/{campaignId}/map");

        Assert.NotNull(map);
        Assert.All(map!.Tiles, tile => Assert.True(tile.IsExploredPublic));
        Assert.NotEmpty(map.Tiles);
    }

    [Fact]
    public async Task MoveAndNotesFlowProducesTravelAndEncounterData()
    {
        using var gmClient = factory.CreateClient();
        var gmAuth = await RegisterAsync(gmClient, CampaignRole.GameMaster, "gm-one");
        var campaignId = gmAuth.Campaigns[0].CampaignId;
        gmClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gmAuth.Token);

        var map = await gmClient.GetFromJsonAsync<CampaignMapResponse>($"/campaigns/{campaignId}/map");
        var party = map!.Parties.Single();
        var target = map.Tiles.First(tile => HexDistance(tile.Coordinate, party.CurrentHex) == 1);

        var moveResponse = await gmClient.PostAsJsonAsync($"/campaigns/{campaignId}/parties/{party.Id}/move", new MovePartyRequest
        {
            TargetQ = target.Coordinate.Q,
            TargetR = target.Coordinate.R
        });

        moveResponse.EnsureSuccessStatusCode();

        var noteResponse = await gmClient.PostAsJsonAsync($"/campaigns/{campaignId}/hexes/{target.Id}/notes", new CreateNoteRequest
        {
            Body = "The road is open, but watch the ridgeline."
        });
        noteResponse.EnsureSuccessStatusCode();

        var encounters = await gmClient.GetFromJsonAsync<List<EncounterLedgerEntryModel>>($"/campaigns/{campaignId}/gm/encounters");
        Assert.NotNull(encounters);
        Assert.NotEmpty(encounters!);
        Assert.NotNull(encounters[0].Prompt);
    }

    private static int HexDistance(HexCoordinate a, HexCoordinate b)
    {
        return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs((-a.Q - a.R) - (-b.Q - b.R))) / 2;
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, CampaignRole role, string username)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Username = username,
            Role = role
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}
