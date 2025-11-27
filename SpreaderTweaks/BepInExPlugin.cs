using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Reflection;
using UnityEngine;

namespace SpreaderTweaks
{
    [BepInPlugin("aedenthorn.SpreaderTweaks", "Spreader Tweaks", "0.4.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> treeRadiusMult;
        public static ConfigEntry<float> treeIntervalMult;
        public static ConfigEntry<float> treeAmountMult;
        public static ConfigEntry<float> flowerRadiusMult;
        public static ConfigEntry<float> flowerIntervalMult;
        public static ConfigEntry<float> flowerAmountMult;
        public static ConfigEntry<float> grassRadiusMult;
        public static ConfigEntry<float> grassIntervalMult;
        public static ConfigEntry<float> grassAmountMult;

        public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }
        public void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            treeRadiusMult = Config.Bind<float>("Options", "TreeRadiusMult", 1f, "Tree radius multiplier.");
            treeIntervalMult = Config.Bind<float>("Options", "TreeIntervalMult", 1f, "Multiplier for interval to trigger planting.");
            treeAmountMult = Config.Bind<float>("Options", "TreeAmountMult", 1f, "Multiplier for amount to plant per interval.");
            grassRadiusMult = Config.Bind<float>("Options", "GrassRadiusMult", 1f, "Grass radius multiplier.");
            grassIntervalMult = Config.Bind<float>("Options", "GrassIntervalMult", 1f, "Multiplier for interval to trigger planting.");
            grassAmountMult = Config.Bind<float>("Options", "GrassAmountMult", 1f, "Multiplier for amount to plant per interval.");
            flowerRadiusMult = Config.Bind<float>("Options", "FlowerRadiusMult", 1f, "Flower radius multiplier.");
            flowerIntervalMult = Config.Bind<float>("Options", "FlowerIntervalMult", 1f, "Multiplier for interval to trigger planting.");
            flowerAmountMult = Config.Bind<float>("Options", "FlowerAmountMult", 1f, "Multiplier for amount to plant per interval.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(MachineGrowerVegetationStatic), "UpdateGrowing")]
        public static class MachineGrowerVegetationStatic_UpdateGrowing_Patch
        {
            public static void Prefix(MachineGrowerVegetationStatic __instance, ref float timeRepeat)
            {
                if (!modEnabled.Value)
                    return;
                WorldObject worldObject = __instance.GetComponentInChildren<WorldObjectAssociated>(true).GetWorldObject();
                if (worldObject == null)
                    return;
                string id = worldObject.GetGroup().id;
                if (id.StartsWith("TreeSpreader"))
                {
                    Dbgl($"Starting tree spreader {timeRepeat * treeIntervalMult.Value}");
                    timeRepeat *= treeIntervalMult.Value;
                }
                if (id.StartsWith("GrassSpreader"))
                {
                    Dbgl($"Starting grass spreader {timeRepeat * grassIntervalMult.Value}");
                    timeRepeat *= grassIntervalMult.Value;
                }
                if (id.StartsWith("SeedSpreader"))
                {
                    Dbgl($"Starting flower spreader {timeRepeat * flowerIntervalMult.Value}");
                    timeRepeat *= flowerIntervalMult.Value;
                }
                return;
            }
        }

        [HarmonyPatch(typeof(MachineGrowerVegetationStatic), "InstantiateAtRandomPosition")]
        public static class MachineOutsideGrower_InstantiateAtRandomPosition_Patch
        {
            public static void Prefix(MachineGrowerVegetationStatic __instance, ref float __state)
            {
                if (!modEnabled.Value)
                    return;
                __state = __instance.radius;
                WorldObject worldObject = __instance.GetComponentInChildren<WorldObjectAssociated>(true).GetWorldObject();
                if (worldObject == null)
                    return;
                string id = worldObject.GetGroup().id;
                if (id.StartsWith("TreeSpreader"))
                {
                    __instance.radius *= treeRadiusMult.Value;
                }
                if (id.StartsWith("GrassSpreader"))
                {
                    __instance.radius *= grassRadiusMult.Value;
                }
                if (id.StartsWith("SeedSpreader"))
                {
                    __instance.radius *= flowerRadiusMult.Value;
                }
            }
            public static void Postfix(MachineGrowerVegetationStatic __instance, float __state)
            {
                if (!modEnabled.Value)
                    return;
                __instance.radius = __state;
            }
        }

        [HarmonyPatch(typeof(MachineGrowerVegetationStatic), nameof(MachineGrowerVegetationStatic.SpawnRandom), new Type[0])]
        public static class MachineGrowerVegetationStatic_SpawnRandom_Patch
        {
            public static void Prefix(MachineGrowerVegetationStatic __instance, ref int __state)
            {
                if (!modEnabled.Value)
                    return;
                __state = __instance.spawNumber;
                WorldObject worldObject = __instance.GetComponentInChildren<WorldObjectAssociated>(true).GetWorldObject();
                if (worldObject == null)
                    return;
                string id = worldObject.GetGroup().id;
                if (id.StartsWith("TreeSpreader"))
                {
                    __instance.spawNumber = Mathf.RoundToInt(__instance.spawNumber * treeAmountMult.Value);
                }
                if (id.StartsWith("GrassSpreader"))
                {
                    __instance.spawNumber = Mathf.RoundToInt(__instance.spawNumber * grassAmountMult.Value);
                }
                if (id.StartsWith("SeedSpreader"))
                {
                    __instance.spawNumber = Mathf.RoundToInt(__instance.spawNumber * flowerAmountMult.Value);
                }
            }
            public static void Postfix(MachineGrowerVegetationStatic __instance, int __state)
            {
                if (!modEnabled.Value)
                    return;
                __instance.spawNumber = __state;
            }
        }
    }
}
