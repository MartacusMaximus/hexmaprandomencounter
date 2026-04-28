using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KnightsAndGM.Api.Realtime;

[Authorize]
public sealed class CampaignHub : Hub
{
    public Task JoinCampaignGroup(Guid campaignId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, campaignId.ToString());
    }
}
