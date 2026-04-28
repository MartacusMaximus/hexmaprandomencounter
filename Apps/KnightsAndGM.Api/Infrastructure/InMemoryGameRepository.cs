using System.Collections.Concurrent;
using KnightsAndGM.Api.Seed;
using KnightsAndGM.Shared;

namespace KnightsAndGM.Api.Infrastructure;

public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly SeedCampaignFactory seedFactory;
    private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
    private readonly ConcurrentDictionary<Guid, AuthUserRecord> users = new ConcurrentDictionary<Guid, AuthUserRecord>();
    private readonly ConcurrentDictionary<Guid, CampaignStateModel> campaigns = new ConcurrentDictionary<Guid, CampaignStateModel>();
    private readonly ConcurrentDictionary<Guid, List<CampaignMembershipRecord>> memberships = new ConcurrentDictionary<Guid, List<CampaignMembershipRecord>>();

    public InMemoryGameRepository(SeedCampaignFactory seedFactory)
    {
        this.seedFactory = seedFactory;
    }

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!campaigns.ContainsKey(SeedCampaignFactory.DefaultCampaignId))
            {
                var campaign = seedFactory.CreateDefaultCampaign();
                campaigns[campaign.CampaignId] = campaign;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public Task<AuthUserRecord?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var match = users.Values.FirstOrDefault(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<AuthUserRecord?>(match);
    }

    public Task<AuthUserRecord?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        users.TryGetValue(userId, out var user);
        return Task.FromResult<AuthUserRecord?>(user);
    }

    public async Task<AuthUserRecord> CreateUserAsync(string username, string displayName, CampaignRole role, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var record = new AuthUserRecord
            {
                Id = Guid.NewGuid(),
                Username = username,
                DisplayName = displayName,
                Role = role
            };

            users[record.Id] = record;
            return record;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<AuthUserRecord> UpdateUserRoleAsync(Guid userId, CampaignRole role, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!users.TryGetValue(userId, out var existingUser))
            {
                throw new InvalidOperationException("User not found.");
            }

            existingUser.Role = role;
            users[userId] = existingUser;
            return existingUser;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task EnsureMembershipAsync(Guid campaignId, string campaignName, Guid userId, CampaignRole role, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var list = memberships.GetOrAdd(userId, _ => new List<CampaignMembershipRecord>());
            if (list.Any(entry => entry.CampaignId == campaignId))
            {
                return;
            }

            list.Add(new CampaignMembershipRecord
            {
                CampaignId = campaignId,
                CampaignName = campaignName,
                Role = role
            });
        }
        finally
        {
            gate.Release();
        }
    }

    public Task<IReadOnlyList<CampaignMembershipRecord>> GetMembershipsAsync(Guid userId, CancellationToken cancellationToken)
    {
        memberships.TryGetValue(userId, out var list);
        return Task.FromResult<IReadOnlyList<CampaignMembershipRecord>>(list ?? new List<CampaignMembershipRecord>());
    }

    public Task<CampaignStateModel?> GetCampaignStateAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        campaigns.TryGetValue(campaignId, out var state);
        return Task.FromResult<CampaignStateModel?>(state == null ? null : Clone(state));
    }

    public async Task SaveCampaignStateAsync(CampaignStateModel state, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            campaigns[state.CampaignId] = Clone(state);
        }
        finally
        {
            gate.Release();
        }
    }

    private static CampaignStateModel Clone(CampaignStateModel state)
    {
        return System.Text.Json.JsonSerializer.Deserialize<CampaignStateModel>(
            System.Text.Json.JsonSerializer.Serialize(state)) ?? new CampaignStateModel();
    }
}
