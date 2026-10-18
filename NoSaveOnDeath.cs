using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services.Bot;
using SPTarkov.Server.Core.Services.InRaid;

namespace NoSaveOnDeath;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.takenmake.nosaveondeath";
    public string Name { get; init; } = "NoSaveOnDeath";
    public string Author { get; init; } = "takenmake";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("3.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");


    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

// SPT 4.1 made LocationLifecycleService's methods non-virtual, so the raid end is intercepted with a Harmony patch instead
[Injectable(TypePriority = OnLoadOrder.Preload)]
public class NoSaveOnDeathLoader(
    ISptLogger<LocationLifecycleService> logger,
    BotLootCacheService botLootCacheService,
    RagfairConfig ragfairConfig,
    HideoutConfig hideoutConfig) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        EndLocalRaidPatch.Logger = logger;
        EndLocalRaidPatch.BotLootCacheService = botLootCacheService;
        EndLocalRaidPatch.RagfairConfig = ragfairConfig;
        EndLocalRaidPatch.HideoutConfig = hideoutConfig;

        new EndLocalRaidPatch().Enable();

        logger.Success("[NoSaveOnDeath] EndLocalRaid patch loaded");

        return Task.CompletedTask;
    }
}

public class EndLocalRaidPatch : AbstractPatch
{
    internal static ISptLogger<LocationLifecycleService> Logger = null!;
    internal static BotLootCacheService BotLootCacheService = null!;
    internal static RagfairConfig RagfairConfig = null!;
    internal static HideoutConfig HideoutConfig = null!;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(LocationLifecycleService).GetMethod(nameof(LocationLifecycleService.EndLocalRaidAsync))!;
    }

    [PatchPrefix]
    public static bool Prefix(MongoId sessionId, EndLocalRaidRequestData request, ref Task __result)
    {
        Logger.Success("[NoSaveOnDeath] Overriding EndLocalRaid...");

        if (request.Results != null && request.Results.IsPlayerDead())
        {
            // Same out-of-raid cleanup the original method does before it saves the profile
            BotLootCacheService.ClearCache();

            if (Logger.IsLogEnabled(LogLevel.Debug))
            {
                Logger.Debug($"Raid: {request.ServerId} outcome: {request.Results.Result}");
            }

            RagfairConfig.RunIntervalSeconds = RagfairConfig.RunIntervalValues.OutOfRaid;
            HideoutConfig.RunIntervalSeconds = HideoutConfig.RunIntervalValues.OutOfRaid;

            Logger.Success("[NoSaveOnDeath] Player died, skipping save");

            // Skipping an async method still has to hand the caller a Task to await
            __result = Task.CompletedTask;
            return false;
        }

        Logger.Success("[NoSaveOnDeath] Player survived, saving normally");

        return true;
    }
}
