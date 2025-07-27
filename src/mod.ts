import { DependencyContainer } from "tsyringe";
import { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import { LocationLifecycleService } from "@spt/services/LocationLifecycleService";
import { IEndLocalRaidRequestData } from "@spt/models/eft/match/IEndLocalRaidRequestData";
import { ContextVariableType } from "@spt/context/ContextVariableType";

class NoSaveOnDeath implements IPreSptLoadMod
{
    private static container: DependencyContainer;
    
    public preSptLoad(container: DependencyContainer): void 
    {
        NoSaveOnDeath.container = container;

        container.afterResolution("LocationLifecycleService", (_t, result: LocationLifecycleService) => 
        {
            result.endLocalRaid = (
                sessionId: string, 
                request: IEndLocalRaidRequestData) => 
            {
                // replace method with the one defined below
                return this.endLocalRaidIfLived(sessionId, request);
            }
        }, {frequency: "Always"});
    }

    public endLocalRaidIfLived(sessionId: string, request: IEndLocalRaidRequestData): void {
        const locationLifeCycleService = NoSaveOnDeath.container.resolve<LocationLifecycleService>("LocationLifecycleService");

        // ## ORIGINAL METHOD BEGINS ##
        // Clear bot loot cache
        locationLifeCycleService.botLootCacheService.clearCache();

        const fullProfile = locationLifeCycleService.profileHelper.getFullProfile(sessionId);
        const pmcProfile = fullProfile.characters.pmc;
        const scavProfile = fullProfile.characters.scav;

        // TODO:
        // Quest status?
        // stats/eft/aggressor - weird values (EFT.IProfileDataContainer.Nickname)

        locationLifeCycleService.logger.debug(`Raid: ${request.serverId} outcome: ${request.results.result}`);

        // Reset flea interval time to out-of-raid value
        locationLifeCycleService.ragfairConfig.runIntervalSeconds = locationLifeCycleService.ragfairConfig.runIntervalValues.outOfRaid;
        locationLifeCycleService.hideoutConfig.runIntervalSeconds = locationLifeCycleService.hideoutConfig.runIntervalValues.outOfRaid;

        // ServerId has various info stored in it, delimited by a period
        const serverDetails = request.serverId.split(".");

        const locationName = serverDetails[0].toLowerCase();
        const isPmc = serverDetails[1].toLowerCase() === "pmc";
        const mapBase = locationLifeCycleService.databaseService.getLocation(locationName).base;
        const isDead = locationLifeCycleService.isPlayerDead(request.results);
        const isTransfer = locationLifeCycleService.isMapToMapTransfer(request.results);
        const isSurvived = locationLifeCycleService.isPlayerSurvived(request.results);

        // break out before saving if the player dies
        if (isDead && isPmc) return;

        // Handle items transferred via BTR or transit to player mailbox
        locationLifeCycleService.handleItemTransferEvent(sessionId, request);

        // Player is moving between maps
        if (isTransfer && request.locationTransit) {
            // Manually store the map player just left
            request.locationTransit.sptLastVisitedLocation = locationName;
            // TODO - Persist each players last visited location history over multiple transits, e.g using InMemoryCacheService, need to take care to not let data get stored forever
            // Store transfer data for later use in `startLocalRaid()` when next raid starts
            request.locationTransit.sptExitName = request.results.exitName;
            locationLifeCycleService.applicationContext.addValue(ContextVariableType.TRANSIT_INFO, request.locationTransit);
        }

        if (!isPmc) {
            locationLifeCycleService.handlePostRaidPlayerScav(sessionId, pmcProfile, scavProfile, isDead, isTransfer, request);

            return;
        }

        locationLifeCycleService.handlePostRaidPmc(
            sessionId,
            fullProfile,
            scavProfile,
            isDead,
            isSurvived,
            isTransfer,
            request,
            locationName,
        );

        // Handle car extracts
        if (locationLifeCycleService.extractWasViaCar(request.results.exitName)) {
            locationLifeCycleService.handleCarExtract(request.results.exitName, pmcProfile, sessionId);
        }

        // Handle coop exit
        if (
            request.results.exitName &&
            locationLifeCycleService.extractTakenWasCoop(request.results.exitName) &&
            locationLifeCycleService.traderConfig.fence.coopExtractGift.sendGift
        ) {
            locationLifeCycleService.handleCoopExtract(sessionId, pmcProfile, request.results.exitName);
            locationLifeCycleService.sendCoopTakenFenceMessage(sessionId);
        }
    }
}

export const mod = new NoSaveOnDeath();