using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Extensions;



namespace NoSaveOnDeath;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.takenmake.nosaveondeath";
    public override string Name { get; init; } = "NoSaveOnDeath";
    public override string Author { get; init; } = "takenmake";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    
    
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "MIT";
}

[Injectable()]
public class OverrideMethod(
    ISptLogger<LocationLifecycleService> logger,
    RewardHelper rewardHelper,
    ConfigServer configServer,
    TimeUtil timeUtil,
    DatabaseService databaseService,
    ProfileHelper profileHelper,
    BackupService backupService,
    ProfileActivityService profileActivityService,
    BotNameService botNameService,
    ICloner cloner,
    RaidTimeAdjustmentService raidTimeAdjustmentService,
    LocationLootGenerator locationLootGenerator,
    ServerLocalisationService serverLocalisationService,
    BotLootCacheService botLootCacheService,
    LootGenerator lootGenerator,
    MailSendService mailSendService,
    TraderHelper traderHelper,
    RandomUtil randomUtil,
    InRaidHelper inRaidHelper,
    PlayerScavGenerator playerScavGenerator,
    SaveServer saveServer,
    HealthHelper healthHelper,
    PmcChatResponseService pmcChatResponseService,
    PmcWaveGenerator pmcWaveGenerator,
    QuestHelper questHelper,
    InsuranceService insuranceService,
    MatchBotDetailsCacheService matchBotDetailsCacheService,
    BtrDeliveryService btrDeliveryService)
    : LocationLifecycleService(logger, rewardHelper, configServer, timeUtil, databaseService, profileHelper,
        backupService, profileActivityService, botNameService, cloner, raidTimeAdjustmentService,
        locationLootGenerator, serverLocalisationService, botLootCacheService, lootGenerator, mailSendService,
        traderHelper, randomUtil, inRaidHelper, playerScavGenerator, saveServer, healthHelper, pmcChatResponseService,
        pmcWaveGenerator, questHelper, insuranceService, matchBotDetailsCacheService, btrDeliveryService)
{

    public override void EndLocalRaid(MongoId sessionId, EndLocalRaidRequestData request)
    {
        logger.Success("[NoSaveOnDeath] Overriding EndLocalRaid...");
        
        if (request.Results != null && request.Results.IsPlayerDead()) 
        {
            botLootCacheService.ClearCache();

            if (logger.IsLogEnabled(LogLevel.Debug))
            {
                logger.Debug($"Raid: {request.ServerId} outcome: {request.Results.Result}");
            }

            RagfairConfig.RunIntervalSeconds = RagfairConfig.RunIntervalValues.OutOfRaid;
            HideoutConfig.RunIntervalSeconds = HideoutConfig.RunIntervalValues.OutOfRaid;

            logger.Success("[NoSaveOnDeath] Player died, skipping save");

            return;
        }

        logger.Success("[NoSaveOnDeath] Player survived, saving normally");
    
        base.EndLocalRaid(sessionId, request);
    }
}
