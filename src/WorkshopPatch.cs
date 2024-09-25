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
            CommandWindow.Log($"UnturnedDataSerializer: queued to download {item.m_PublishedFileId}");
            workshopItems.Add(item);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DedicatedUGC), "installNextItem")]
        static void installNextItem_Prefix() {
            CommandWindow.Log($"UnturnedDataSerializer: {workshopItems.Count} items to download");
            queryHandle = SteamGameServerUGC.CreateQueryUGCDetailsRequest(workshopItems.ToArray(), (uint)workshopItems.Count);
            SteamGameServerUGC.SetReturnKeyValueTags(queryHandle, true);
            SteamGameServerUGC.SetReturnChildren(queryHandle, true);
            SteamAPICall_t hAPICall = SteamGameServerUGC.SendQueryUGCRequest(queryHandle);
            queryCompleted.Set(hAPICall);
        }

        static void onQueryCompleted(SteamUGCQueryCompleted_t callback, bool ioFailure) {
            items.Add("Unturned", new Item { version = Provider.APP_VERSION_PACKED, dependencies = new SortedSet<string>()});
            
            uint resultsCount = callback.m_unNumResultsReturned;
            CommandWindow.Log($"UnturnedDataSerializer: {resultsCount} items in query result");
            for (uint index = 0; index < resultsCount; index++) {
                SteamUGCDetails_t details;
                SteamGameServerUGC.GetQueryUGCResult(queryHandle, index, out details);
                ulong workshopID = details.m_nPublishedFileId.m_PublishedFileId;
                uint lastUpdated = details.m_rtimeUpdated;
                uint childrenCount = details.m_unNumChildren;
                CommandWindow.Log($"UnturnedDataSerializer: {workshopID}, last update: {lastUpdated}");
                
                items.Add(workshopID.ToString(), new Item { version = lastUpdated, dependencies = new SortedSet<string>()});
                
                PublishedFileId_t[] children = new PublishedFileId_t[(int)childrenCount];
                SteamGameServerUGC.GetQueryUGCChildren(queryHandle, index, children,
                    childrenCount);

                foreach (var child in children) {
                    CommandWindow.Log($"UnturnedDataSerializer: \t{child.m_PublishedFileId}");
                    items[workshopID.ToString()].dependencies.Add(child.m_PublishedFileId.ToString());
                }
            }

            using (var streamWriter = File.CreateText("/app/output/versions.json"))
            using (var jsonWriter = new JsonTextWriter(streamWriter)) {
                jsonWriter.Formatting = Formatting.Indented;
                new JsonSerializer().Serialize(jsonWriter, items);
            }
            
            Main.Shutdown();
        }
    }
}