using HarmonyLib;
using Newtonsoft.Json.Linq;
using SDG.Unturned;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Steamworks;

namespace UnturnedDataSerializer {
    [HarmonyPatch]
    [HarmonyPatchCategory("Workshop")]
    internal class WorkshopPatch {
        private static UGCQueryHandle_t queryHandle;
        private static CallResult<SteamUGCQueryCompleted_t> queryCompleted =
            CallResult<SteamUGCQueryCompleted_t>.Create(onQueryCompleted);
        
        private static HashSet<PublishedFileId_t> workshopItems = new HashSet<PublishedFileId_t>();

        private static SortedDictionary<string, Item> items = new SortedDictionary<string, Item>();
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DedicatedUGC), "enqueueItemToDownload")]
        static void enqueueItemToDownload_Prefix(PublishedFileId_t item) {
            Logger.Log($"Queued to download {item.m_PublishedFileId}");
            workshopItems.Add(item);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DedicatedUGC), "installNextItem")]
        static void installNextItem_Prefix() {
            Logger.Log($"{workshopItems.Count} items to download");
            queryHandle = SteamGameServerUGC.CreateQueryUGCDetailsRequest(workshopItems.ToArray(), (uint)workshopItems.Count);
            SteamGameServerUGC.SetReturnKeyValueTags(queryHandle, true);
            SteamGameServerUGC.SetReturnChildren(queryHandle, true);
            SteamGameServerUGC.SetAllowCachedResponse(queryHandle, uint.MaxValue);
            SteamAPICall_t hAPICall = SteamGameServerUGC.SendQueryUGCRequest(queryHandle);
            queryCompleted.Set(hAPICall);
        }

        static void onQueryCompleted(SteamUGCQueryCompleted_t callback, bool ioFailure) {
            items.Add("Unturned", new Item { version = Provider.APP_VERSION_PACKED, dependencies = new SortedSet<string>()});
            
            uint resultsCount = callback.m_unNumResultsReturned;
            Logger.Log($"{resultsCount} items in query result");
            HashSet<PublishedFileId_t> queryResultWorkshopItems = new HashSet<PublishedFileId_t>();
            for (uint index = 0; index < resultsCount; index++) {
                SteamUGCDetails_t details;
                SteamGameServerUGC.GetQueryUGCResult(queryHandle, index, out details);
                ulong workshopID = details.m_nPublishedFileId.m_PublishedFileId;
                uint lastUpdated = details.m_rtimeUpdated;
                uint childrenCount = details.m_unNumChildren;
                Logger.Log($"{workshopID}, last update: {lastUpdated}");
                
                items.Add(workshopID.ToString(), new Item { version = lastUpdated, dependencies = new SortedSet<string>()});
                queryResultWorkshopItems.Add(details.m_nPublishedFileId);
                
                PublishedFileId_t[] children = new PublishedFileId_t[(int)childrenCount];
                SteamGameServerUGC.GetQueryUGCChildren(queryHandle, index, children,
                    childrenCount);

                foreach (var child in children) {
                    Logger.Log($"\t{child.m_PublishedFileId}");
                    if (workshopItems.Contains(child))
                        items[workshopID.ToString()].dependencies.Add(child.m_PublishedFileId.ToString());
                    else
                        Logger.Log($"Item {child} is child of {workshopID}, but is not downloaded");
                }
            }
            
            if (!workshopItems.Equals(queryResultWorkshopItems))
                Logger.LogError("Workshop items query result differs from downloaded workshop items!");

            using (var streamWriter = File.CreateText("/app/output/versions.json"))
            using (var jsonWriter = new JsonTextWriter(streamWriter)) {
                jsonWriter.Formatting = Formatting.Indented;
                new JsonSerializer().Serialize(jsonWriter, items);
            }
            
            Main.Shutdown();
        }
    }
}