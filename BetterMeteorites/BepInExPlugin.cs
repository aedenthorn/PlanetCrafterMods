using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BetterMeteorites
{
    [BepInPlugin("aedenthorn.BetterMeteorites", "Better Meteorites", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> addSpecialAsteroids;
        public static ConfigEntry<float> uraniumChance;
        public static ConfigEntry<float> iridiumChance;

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
            addSpecialAsteroids = Config.Bind<bool>("General", "AddSpecialAsteroids", true, "Add iridium and uranium asteroids to event list");

            uraniumChance = Config.Bind<float>("Options", "UraniumChance", 0.05f, "Chance of replacing asteroid resource with uranium");
            iridiumChance = Config.Bind<float>("Options", "IridiumChance", 0.05f, "Chance of replacing asteroid resource with iridium");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(MeteoHandler), "Start")]
        static class MeteoHandler_Start_Patch
        {
            static void Postfix(MeteoHandler __instance)
            {
                if (!modEnabled.Value || !addSpecialAsteroids.Value)
                    return;
                Dbgl($"Start meteo handler");
                MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                if (s != null)
                {
                    var e = s.meteoEvents.Find(d => d.name.Contains("Uranium"));
                    if (e != null)
                    {
                        __instance.meteoEvents.Add(e);
                        Dbgl($"added uranium event");
                    }
                    e = s.meteoEvents.Find(d => d.name.Contains("Iridium"));
                    if (e != null)
                    {
                        __instance.meteoEvents.Add(e);
                        Dbgl($"added iridium event");
                    }
                }
            }
        }
        //[HarmonyPatch(typeof(MeteoHandler), nameof(MeteoHandler.LaunchSpecificMeteoEvent))]
        static class MeteoHandler_LaunchSpecificMeteoEvent_Patch
        {
            static void Prefix(MeteoHandler __instance, ref MeteoEventData _meteoEvent)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl($"Getting asteroid meteo event");
                double r = Random.value;
                if (r < uraniumChance.Value)
                {
                    MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                    if (s != null)
                    {
                        var e = s.meteoEvents.Find(d => d.name.Contains("Uranium"));
                        if (e != null)
                        {
                            _meteoEvent = e;
                            Dbgl($"Got uranium asteroid");
                            return;
                        }
                    }
                }
                else if (r < uraniumChance.Value + iridiumChance.Value)
                {
                    MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                    if (s != null)
                    {
                        var e = s.meteoEvents.Find(d => d.name.Contains("Iridium"));
                        if (e != null)
                        {
                            _meteoEvent = e;
                            Dbgl($"Got iridium asteroid");
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Asteroid), nameof(Asteroid.GetAssociatedGroups))]
        static class Asteroid_GetAssociatedGroups_Patch
        {
            static bool Prefix(Asteroid __instance, ref List<GroupItem> __result)
            {
                if (!modEnabled.Value || __instance.name.Contains("Uranium") || __instance.name.Contains("Iridium"))
                    return true;
                Dbgl($"Getting better asteroid");
                double r = Random.value;
                if (r < uraniumChance.Value)
                {
                    MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                    if (s != null)
                    {
                        var e = s.meteoEvents.Find(d => d.name.Contains("Uranium"));
                        if (e != null)
                        {
                            __result = AccessTools.FieldRefAccess<Asteroid, List<GroupItem>>(e.asteroidEventData.asteroidGameObject.GetComponent<Asteroid>(), "associatedGroups");
                            Dbgl($"Got uranium asteroid");
                            return false;
                        }
                    }
                }
                else if (r < uraniumChance.Value + iridiumChance.Value)
                {
                    MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                    if (s != null)
                    {
                        var e = s.meteoEvents.Find(d => d.name.Contains("Iridium"));
                        if (e != null)
                        {
                            __result = AccessTools.FieldRefAccess<Asteroid, List<GroupItem>>(e.asteroidEventData.asteroidGameObject.GetComponent<Asteroid>(), "associatedGroups");
                            Dbgl($"Got iridium asteroid");
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}
