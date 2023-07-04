using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SpreaderTweaks
{
    [BepInPlugin("aedenthorn.SpreaderTweaks", "Spreader Tweaks", "0.3.0")]
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
        
        public static AccessTools.FieldRef<object, WorldObject> fieldRef;

        public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }
        private void Awake()
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
            fieldRef = AccessTools.FieldRefAccess<WorldObject>(typeof(MachineOutsideGrower), "worldObjectGrower");

        }

        [HarmonyPatch(typeof(MachineOutsideGrower), nameof(MachineOutsideGrower.SetWorldObjectForGrower))]
        static class MachineOutsideGrower_SetWorldObjectForGrower_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                if (!modEnabled.Value)
                    return codes.AsEnumerable();

                Dbgl("Transpiling MachineOutsideGrower.SetWorldObjectForGrower");
                for (int i = 0; i < codes.Count; i++)
                {
                    if (i < codes.Count - 1 && codes[i].opcode == OpCodes.Ldstr && (string)codes[i].operand == "LaunchGrowingProcess" && codes[i + 1].opcode == OpCodes.Ldc_R4 && (float)codes[i + 1].operand == 3)
                    {
                        Dbgl("Switching interval to method for multiplier");
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetInterval))));
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldarg_0));
                        break;
                    }
                }
                return codes.AsEnumerable();
            }
        }

        public static float GetInterval(float interval, MachineOutsideGrower grower)
        {
            if (!modEnabled.Value)
                return interval;
            string id = fieldRef.Invoke(grower).GetGroup().id;
            if (id.StartsWith("TreeSpreader"))
            {
                Dbgl($"Starting tree spreader {interval * treeIntervalMult.Value}");
                return interval * treeIntervalMult.Value;
            }
            if (id.StartsWith("GrassSpreader"))
            {
                Dbgl($"Starting grass spreader {interval * grassIntervalMult.Value}");
                return interval * grassIntervalMult.Value;
            }
            if (id.StartsWith("SeedSpreader"))
            {
                Dbgl($"Starting flower spreader {interval * flowerIntervalMult.Value}");
                return interval * flowerIntervalMult.Value;
            }
            return interval;
        }

        [HarmonyPatch(typeof(MachineOutsideGrower), "InstantiateAtRandomPosition")]
        static class MachineOutsideGrower_InstantiateAtRandomPosition_Patch
        {
            static void Prefix(MachineOutsideGrower __instance, ref float __state)
            {
                if (!modEnabled.Value)
                    return;
                __state = __instance.radius;
                string id = fieldRef.Invoke(__instance).GetGroup().id;
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
            static void Postfix(MachineOutsideGrower __instance, float __state)
            {
                if (!modEnabled.Value)
                    return;
                __instance.radius = __state;
            }
        }
        [HarmonyPatch(typeof(MachineOutsideGrower), "LaunchGrowingProcess")]
        static class MachineOutsideGrower_LaunchGrowingProcess_Patch
        {
            static void Prefix(MachineOutsideGrower __instance, ref float[] __state)
            {
                if (!modEnabled.Value)
                    return;
                __state = new float[] { __instance.updateInterval, __instance.spawNumber };

                string id = fieldRef.Invoke(__instance).GetGroup().id;
                if (id.StartsWith("TreeSpreader"))
                {
                    __instance.updateInterval *= treeIntervalMult.Value;
                    __instance.spawNumber = Mathf.RoundToInt(__instance.spawNumber * treeAmountMult.Value);
                }
                if (id.StartsWith("GrassSpreader"))
                {
                    __instance.updateInterval *= grassIntervalMult.Value;
                    __instance.spawNumber = Mathf.RoundToInt(__instance.spawNumber * grassAmountMult.Value);
                }
                if (id.StartsWith("SeedSpreader"))
                {
                    __instance.updateInterval *= flowerIntervalMult.Value;
                    __instance.spawNumber = Mathf.RoundToInt(__instance.spawNumber * flowerAmountMult.Value);
                }
            }
            static void Postfix(MachineOutsideGrower __instance, float[] __state)
            {
                if (!modEnabled.Value)
                    return;
                __instance.updateInterval = __state[0];
                __instance.spawNumber = (int)__state[1];
            }
        }
    }
}
