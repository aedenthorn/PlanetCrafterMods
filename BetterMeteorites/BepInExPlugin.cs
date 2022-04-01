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
        public static ConfigEntry<float> asteroidResourceMult;

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
            addSpecialAsteroids = Config.Bind<bool>("Options", "AddSpecialMeteorite", false, "Add iridium and uranium meteorite to event list");
            asteroidResourceMult = Config.Bind<float>("Options", "MeteoriteResourceMult", 1f, "Resource per meteorite multiplier");
            uraniumChance = Config.Bind<float>("Options", "UraniumChance", 0.05f, "Chance of adding uranium to meteorite (1.0 = 100% chance)");
            iridiumChance = Config.Bind<float>("Options", "IridiumChance", 0.05f, "Chance of adding iridium to meteorite (1.0 = 100% chance)");


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

                __instance.eventChanceOnOneHundred = 100;

                for (int i = __instance.meteoEvents.Count - 1; i >= 0; i--)
                {
                    if (__instance.meteoEvents[i].asteroidEventData == null)
                        __instance.meteoEvents.RemoveAt(i);
                }
                return;
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
        [HarmonyPatch(typeof(AsteroidsImpactHandler), nameof(AsteroidsImpactHandler.CreateImpact))]
        static class Asteroid_CreateImpact_Patch
        {
            static void Prefix(AsteroidsImpactHandler __instance, ref Asteroid _asteroid)
            {
                if (!modEnabled.Value || __instance.name.Contains("Uranium") || __instance.name.Contains("Iridium"))
                    return;
                double r = Random.value;
                if (r < uraniumChance.Value)
                {
                    MeteoSendInSpace s = FindObjectOfType<MeteoSendInSpace>();
                    if (s != null)
                    {
                        var e = s.meteoEvents.Find(d => d.name.Contains("Uranium"));
                        if (e != null)
                        {
                            var a = e.asteroidEventData.asteroidGameObject.GetComponent<Asteroid>();
                            var groups = AccessTools.FieldRefAccess<Asteroid, List<GroupItem>>(_asteroid, "associatedGroups");
                            foreach (GroupDataItem groupDataItem in a.groupsSelected)
                            {
                                groups.Add((GroupItem)GroupsHandler.GetGroupViaId(groupDataItem.id));
                            }
                            //Dbgl($"Got uranium asteroid");
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
                            var a = e.asteroidEventData.asteroidGameObject.GetComponent<Asteroid>();
                            var groups = AccessTools.FieldRefAccess<Asteroid, List<GroupItem>>(_asteroid, "associatedGroups");
                            foreach (GroupDataItem groupDataItem in a.groupsSelected)
                            {
                                groups.Add((GroupItem)GroupsHandler.GetGroupViaId(groupDataItem.id));
                            }
                            //Dbgl($"Got iridium asteroid");
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Asteroid), nameof(Asteroid.GetNumberOfResourceInDebris))]
        static class Asteroid_GetNumberOfResourceInDebris_Patch
        {
            static void Postfix(Asteroid __instance, ref float __result)
            {
                if (!modEnabled.Value)
                    return;
                __result *= asteroidResourceMult.Value;
            }
        }
    }
}
