using KnightsAndGM.Shared;

namespace KnightsAndGM.Api.Infrastructure;

public sealed class AuthUserRecord
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public CampaignRole Role { get; set; }
}

public sealed class CampaignMembershipRecord
{
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public CampaignRole Role { get; set; }
}

public interface IGameRepository
{
    Task EnsureSeedDataAsync(CancellationToken cancellationToken);
    Task<AuthUserRecord?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<AuthUserRecord?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<AuthUserRecord> CreateUserAsync(string username, string displayName, CampaignRole role, CancellationToken cancellationToken);
    Task<AuthUserRecord> UpdateUserRoleAsync(Guid userId, CampaignRole role, CancellationToken cancellationToken);
    Task EnsureMembershipAsync(Guid campaignId, string campaignName, Guid userId, CampaignRole role, CancellationToken cancellationToken);
    Task<IReadOnlyList<CampaignMembershipRecord>> GetMembershipsAsync(Guid userId, CancellationToken cancellationToken);
    Task<CampaignStateModel?> GetCampaignStateAsync(Guid campaignId, CancellationToken cancellationToken);
    Task SaveCampaignStateAsync(CampaignStateModel state, CancellationToken cancellationToken);
}
