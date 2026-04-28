using System.Text.Json;
using System.Text.Json.Serialization;
using KnightsAndGM.Api.Seed;
using KnightsAndGM.Shared;
using Npgsql;

namespace KnightsAndGM.Api.Infrastructure;

public sealed class PostgresGameRepository : IGameRepository, IAsyncDisposable
{
    private readonly NpgsqlDataSource dataSource;
    private readonly SeedCampaignFactory seedFactory;
    private readonly JsonSerializerOptions jsonOptions;
    private bool schemaEnsured;

    public PostgresGameRepository(string connectionString, SeedCampaignFactory seedFactory)
    {
        dataSource = NpgsqlDataSource.Create(connectionString);
        this.seedFactory = seedFactory;
        jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        const string existsSql = "select count(1) from campaign_states where campaign_id = @campaign_id;";
        await using var existsCommand = dataSource.CreateCommand(existsSql);
        existsCommand.Parameters.AddWithValue("@campaign_id", SeedCampaignFactory.DefaultCampaignId);
        var existing = (long)(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0L);
        if (existing > 0)
        {
            return;
        }

        var campaign = seedFactory.CreateDefaultCampaign();
        await SaveCampaignStateAsync(campaign, cancellationToken);
    }

    public async Task<AuthUserRecord?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        const string sql = """
            select user_id, username, display_name, role
            from users
            where lower(username) = lower(@username)
            limit 1;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("@username", username);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuthUserRecord
        {
            Id = reader.GetGuid(0),
            Username = reader.GetString(1),
            DisplayName = reader.GetString(2),
            Role = Enum.Parse<CampaignRole>(reader.GetString(3), true)
        };
    }

    public async Task<AuthUserRecord?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        const string sql = """
            select user_id, username, display_name, role
            from users
            where user_id = @user_id
            limit 1;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("@user_id", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuthUserRecord
        {
            Id = reader.GetGuid(0),
            Username = reader.GetString(1),
            DisplayName = reader.GetString(2),
            Role = Enum.Parse<CampaignRole>(reader.GetString(3), true)
        };
    }

    public async Task<AuthUserRecord> CreateUserAsync(string username, string displayName, CampaignRole role, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        var record = new AuthUserRecord
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = displayName,
            Role = role
        };

        const string sql = """
            insert into users (user_id, username, display_name, role, created_at_utc)
            values (@user_id, @username, @display_name, @role, @created_at_utc);
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("@user_id", record.Id);
        command.Parameters.AddWithValue("@username", record.Username);
        command.Parameters.AddWithValue("@display_name", record.DisplayName);
        command.Parameters.AddWithValue("@role", record.Role.ToString());
        command.Parameters.AddWithValue("@created_at_utc", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return record;
    }

    public async Task<AuthUserRecord> UpdateUserRoleAsync(Guid userId, CampaignRole role, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        const string sql = """
            update users
            set role = @role
            where user_id = @user_id;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@role", role.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);

        return await GetUserByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found after updating role.");
    }

    public async Task EnsureMembershipAsync(Guid campaignId, string campaignName, Guid userId, CampaignRole role, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        const string sql = """
            insert into campaign_memberships (campaign_id, user_id, campaign_name, role)
            values (@campaign_id, @user_id, @campaign_name, @role)
            on conflict (campaign_id, user_id) do nothing;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("@campaign_id", campaignId);
        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@campaign_name", campaignName);
        command.Parameters.AddWithValue("@role", role.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CampaignMembershipRecord>> GetMembershipsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        const string sql = """
            select campaign_id, campaign_name, role
            from campaign_memberships
            where user_id = @user_id;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("@user_id", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<CampaignMembershipRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new CampaignMembershipRecord
            {
                CampaignId = reader.GetGuid(0),
                CampaignName = reader.GetString(1),
                Role = Enum.Parse<CampaignRole>(reader.GetString(2), true)
            });
        }

        return results;
    }

    public async Task<CampaignStateModel?> GetCampaignStateAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        const string sql = """
            select state_json
            from campaign_states
            where campaign_id = @campaign_id
            limit 1;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("@campaign_id", campaignId);
        var json = (string?)await command.ExecuteScalarAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<CampaignStateModel>(json, jsonOptions);
    }

    public async Task SaveCampaignStateAsync(CampaignStateModel state, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);

        const string sql = """
            insert into campaign_states (campaign_id, campaign_name, seed, radius, state_json, updated_at_utc)
            values (@campaign_id, @campaign_name, @seed, @radius, cast(@state_json as jsonb), @updated_at_utc)
            on conflict (campaign_id)
            do update set
                campaign_name = excluded.campaign_name,
                seed = excluded.seed,
                radius = excluded.radius,
                state_json = excluded.state_json,
                updated_at_utc = excluded.updated_at_utc;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("@campaign_id", state.CampaignId);
        command.Parameters.AddWithValue("@campaign_name", state.Name);
        command.Parameters.AddWithValue("@seed", state.Seed);
        command.Parameters.AddWithValue("@radius", state.Radius);
        command.Parameters.AddWithValue("@state_json", JsonSerializer.Serialize(state, jsonOptions));
        command.Parameters.AddWithValue("@updated_at_utc", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (schemaEnsured)
        {
            return;
        }

        const string sql = """
            create table if not exists users (
                user_id uuid primary key,
                username text not null unique,
                display_name text not null,
                role text not null,
                created_at_utc timestamptz not null
            );

            create table if not exists campaign_states (
                campaign_id uuid primary key,
                campaign_name text not null,
                seed text not null,
                radius integer not null,
                state_json jsonb not null,
                updated_at_utc timestamptz not null
            );

            create table if not exists campaign_memberships (
                campaign_id uuid not null,
                user_id uuid not null,
                campaign_name text not null,
                role text not null,
                primary key (campaign_id, user_id)
            );
            """;

        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
        schemaEnsured = true;
    }

    public async ValueTask DisposeAsync()
    {
        await dataSource.DisposeAsync();
    }
}
