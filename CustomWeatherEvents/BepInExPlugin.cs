using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomWeatherEvents
{
    [BepInPlugin("aedenthorn.CustomWeatherEvents", "Custom Weather Events", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> dumpData;
        public static ConfigEntry<float> launchCheckInterval;
        public static ConfigEntry<float> launchChancePerCheck;
        public static ConfigEntry<int> spawnedResourcesDestroyMultiplier;

        private static WeatherDataDict meteoEventDict;
        private static bool firstTry;

        const string modFeatMultiplayerGuid = "akarnokd.theplanetcraftermods.featmultiplayer";
        private static MethodInfo GetMultiplayerMode;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            dumpData = Config.Bind<bool>("General", "DumpData", true, "Dump items and terraform stages to files");
            launchCheckInterval = Config.Bind<float>("Options", "LaunchCheckInterval", 20f, "Launch check interval (seconds)");
            launchChancePerCheck = Config.Bind<float>("Options", "LaunchChancePerCheck", 2f, "Launch chance per check (%)");
            spawnedResourcesDestroyMultiplier = Config.Bind<int>("Options", "SpawnedResourcesDestroyMultiplier", 8, "Asteroid resources disappear this many times slower than ordinary asteroid chunks (as defined in the event data field 'debrisDestroyTime').");

            if (Chainloader.PluginInfos.TryGetValue(modFeatMultiplayerGuid, out var pi))
            {
                GetMultiplayerMode = AccessTools.Method(pi.Instance.GetType(), "GetMultiplayerMode");
                Dbgl("Found " + modFeatMultiplayerGuid + ", disabling launch overrides in client mode.");
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(AsteroidsImpactHandler), "Start")]
        static class AsteroidsImpactHandler_Start_Patch
        {

            static void Postfix(AsteroidsImpactHandler __instance, ref int ___spawnedResourcesDestroyMultiplier)
            {
                Dbgl($"Setting spawnedResourcesDestroyMultiplier to {spawnedResourcesDestroyMultiplier.Value}");

                ___spawnedResourcesDestroyMultiplier = spawnedResourcesDestroyMultiplier.Value;
            }
        }

        [HarmonyPatch(typeof(MeteoHandler), "TryToLaunchAnEventLogic")]
        static class MeteoHandler_TryToLaunchAnEventLogic_Patch
        {

            static bool Prefix(MeteoHandler __instance, List<MeteoEventData> ___meteoEventQueue, MeteoEventData ___selectedDataMeteoEvent, List<MeteoEventData> ___meteoEvents)
            {
                if (___meteoEventQueue.Count > 0 || ___selectedDataMeteoEvent != null)
                    return true;

                if (GetMultiplayerMode != null && ((string)GetMultiplayerMode.Invoke(null, new object[0])) == "CoopClient")
                    return false;

                if (firstTry)
                {
                    Dbgl("Skipping first check");
                    firstTry = false;
                    return false;
                }

                if (launchChancePerCheck.Value < Random.Range(0f, 100f))
                    return false;

                Dbgl("Launching random meteo event");
                Dictionary<int, MeteoEventData> weights = new Dictionary<int, MeteoEventData>();
                int totalWeight = 0;
                foreach(var m in ___meteoEvents)
                {
                    if(meteoEventDict.events.TryGetValue(m.name, out var d))
                    {
                        if (!d.random)
                        {
                            continue;
                        }
                        totalWeight += d.weight;
                    }
                    else
                    {
                        totalWeight += 100;
                    }
                    //Dbgl($"Total weight: {totalWeight}");
                    if (!weights.ContainsKey(totalWeight))
                        weights[totalWeight] = m;
                }
                var worldUnitsHandler = Managers.GetManager<WorldUnitsHandler>();
                for (int i = 0; i < 100; i++)
                {
                    int randomWeight = Random.Range(0, totalWeight);
                    //Dbgl($"Random weight: {randomWeight}");
                    foreach (var kvp in weights)
                    {
                        //Dbgl($"Checking weight: {kvp.Key}");
                        if (randomWeight < kvp.Key)
                        {
                            //Dbgl($"Found weight: {kvp.Value.name}");
                            if (worldUnitsHandler.IsWorldValuesAreBetweenStages(kvp.Value.GetMeteoStartTerraformStage(), kvp.Value.GetMeteoStopTerraformStage()))
                            {
                                Dbgl($"Launching: {kvp.Value.name}");
                                __instance.LaunchSpecificMeteoEvent(kvp.Value);
                                return false;
                            }
                            break;
                        }
                    }
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(MeteoHandler), nameof(MeteoHandler.InitMeteoHandler))]
        static class MeteoHandler_InitMeteoHandler_Patch
        {
            static void Prefix(MeteoHandler __instance)
            {
                Dbgl($"Default launch chance per interval {__instance.eventChanceOnOneHundred}");
                Dbgl($"Changing tryToLaunchEventEvery from {__instance.tryToLaunchEventEvery} to {launchCheckInterval.Value}");

                firstTry = true;

                __instance.tryToLaunchEventEvery = launchCheckInterval.Value;
            }
            static void Postfix(List<MeteoEventData> ___meteoEvents)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl($"Start meteo handler");

                if (dumpData.Value)
                {
                    dumpData.Value = false;
                    var list = GroupsHandler.GetAllGroups().Where(g => g is GroupItem).Select(g => $"{Readable.GetGroupName(g)}: {g.GetId()}").ToList();
                    list.Sort();
                    File.WriteAllLines(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "groups.txt"), list);
                    list = Managers.GetManager<TerraformStagesHandler>().GetAllTerraGlobalStages().Select(t => t.GetTerraId()).ToList();
                    File.WriteAllLines(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "terraform_stages.txt"), list);
                }

                bool added = false;

                string filePath = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "event_types.json");
                meteoEventDict =  File.Exists(filePath) ? JsonSerializer.Deserialize<WeatherDataDict>(File.ReadAllText(filePath)) : new WeatherDataDict();
                List<GroupData> groupsData = (List<GroupData>)AccessTools.Field(typeof(StaticDataHandler), "groupsData").GetValue(AccessTools.Field(typeof(StaticDataHandler), "Instance").GetValue(null));
                MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                for (int i = 0; i < ___meteoEvents.Count; i++)
                {

                    WeatherData d;
                    Dbgl($"meteoEvent: {___meteoEvents[i].name}");
                    if (!meteoEventDict.events.TryGetValue(___meteoEvents[i].name, out d))
                    {
                        d = new WeatherData(___meteoEvents[i], true);
                        meteoEventDict.events[___meteoEvents[i].name] = d;
                        added = true;
                    }
                    else if (d.custom)
                    {
                        ___meteoEvents[i] = SetMeteoData(___meteoEvents[i], d, groupsData);
                    }
                }
                for (int i = 0; i < s.meteoEvents.Count; i++)
                {
                    WeatherData d;
                    Dbgl($"meteoEvent: {s.meteoEvents[i].name}");
                    if (!meteoEventDict.events.TryGetValue(s.meteoEvents[i].name, out d))
                    {
                        d = new WeatherData(s.meteoEvents[i], false);
                        meteoEventDict.events[s.meteoEvents[i].name] = d;
                        added = true;
                    }
                    else
                    {
                        if(d.custom)
                            s.meteoEvents[i] = SetMeteoData(s.meteoEvents[i], d, groupsData);
                        if(d.random && !___meteoEvents.Exists(e => e.name == s.meteoEvents[i].name))
                        {
                            Dbgl($"adding meteoEvent {s.meteoEvents[i].name} to random meteo events");
                            ___meteoEvents.Add(s.meteoEvents[i]);
                        }
                    }
                }
                if (added)
                {
                    File.WriteAllText(filePath, JsonSerializer.Serialize(meteoEventDict, new JsonSerializerOptions() { WriteIndented = true }));
                }
            }
        }

        private static MeteoEventData SetMeteoData(MeteoEventData m, WeatherData d, List<GroupData> groupsData)
        {
            Dbgl($"Setting custom event data for {m.name}");

            m.duration = d.duration;
            m.rainEmission = d.rainEmission;
            m.wetness = d.wetness;
            m.startTerraformStage = d.startTerraformStage is null ? null : Managers.GetManager<TerraformStagesHandler>().GetAllTerraGlobalStages().Find(t => t.GetTerraId() == d.startTerraformStage);
            m.stopTerraformStage = d.stopTerraformStage is null ? null : Managers.GetManager<TerraformStagesHandler>().GetAllTerraGlobalStages().Find(t => t.GetTerraId() == d.stopTerraformStage);

            if (d.asteroidEventData is null)
            {
                m.asteroidEventData = null;
                return m;
            }

            if (m.asteroidEventData is null)
                return m;

            m.asteroidEventData.spawnOneEvery = d.asteroidEventData.spawnOneEvery;
            m.asteroidEventData.maxAsteroidsSimultaneous = d.asteroidEventData.maxAsteroidsSimultaneous;
            m.asteroidEventData.maxAsteroidsTotal = d.asteroidEventData.maxAsteroidsTotal;
            m.asteroidEventData.distanceFromPlayer = d.asteroidEventData.distanceFromPlayer;

            var a = m.asteroidEventData.asteroidGameObject?.GetComponent<Asteroid>();

            if (a is null)
                return m;

            a.shakeOnImpact = d.asteroidEventData.asteroid.shakeOnImpact;

            a.doDamage = d.asteroidEventData.asteroid.doDamage;

            a.destroyTime = d.asteroidEventData.asteroid.destroyTime;

            a.numberOfResourceInDebris = d.asteroidEventData.asteroid.numberOfResourceInDebris;

            a.groupsSelected.Clear();
            foreach (var id in d.asteroidEventData.asteroid.groupsSelected)
            {
                var gd = (GroupDataItem)groupsData.Find(g => g.id == id && g is GroupDataItem);
                if(gd != null)
                {
                    if (!gd.associatedGameObject.GetComponent<WorldObjectFromScene>())
                    {
                        var wo = gd.associatedGameObject.AddComponent<WorldObjectFromScene>();
                        AccessTools.FieldRefAccess<WorldObjectFromScene, GroupData>(wo, "groupData") = gd;
                    }
                    a.groupsSelected.Add(gd);

                }
            }

            a.debrisNumber = d.asteroidEventData.asteroid.debrisNumber;

            a.placeAsteroidBody = d.asteroidEventData.asteroid.placeAsteroidBody;

            a.debrisDestroyTime = d.asteroidEventData.asteroid.debrisDestroyTime;

            a.debrisSize = d.asteroidEventData.asteroid.debrisSize;

            AccessTools.Field(typeof(Asteroid), "maxLiveTime").SetValue(a, d.asteroidEventData.asteroid.maxLiveTime);

            AccessTools.Field(typeof(Asteroid), "initialSpeed").SetValue(a, d.asteroidEventData.asteroid.initialSpeed);

            AccessTools.Field(typeof(Asteroid), "maxAcceleration").SetValue(a, d.asteroidEventData.asteroid.maxAcceleration);

            AccessTools.Field(typeof(Asteroid), "maxScale").SetValue(a, d.asteroidEventData.asteroid.maxScale);

            return m;
        }
    }
}
