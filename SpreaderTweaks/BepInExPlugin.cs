using BepInEx;
using BepInEx.Configuration;
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
    [BepInPlugin("aedenthorn.SpreaderTweaks", "Spreader Tweaks", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> radiusMult;
        public static ConfigEntry<float> plantIntervalMult;
        public static ConfigEntry<float> plantAmountMult;


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
            radiusMult = Config.Bind<float>("Options", "RadiusMult", 1f, "Radius multiplier.");
            plantIntervalMult = Config.Bind<float>("Options", "PlantIntervalMult", 1f, "Multiplier for interval to trigger planting.");
            plantAmountMult = Config.Bind<float>("Options", "PlantAmountMult", 1f, "Multiplier for amount to plant per interval.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

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
                        codes[i + 1].opcode = OpCodes.Call;
                        codes[i + 1].operand = typeof(BepInExPlugin).GetMethod(nameof(BepInExPlugin.GetInterval));
                        break;
                    }
                }
                return codes.AsEnumerable();
            }
        }

        public static float GetInterval()
        {
            return 3f * (modEnabled.Value ? plantIntervalMult.Value : 1);
        }

        [HarmonyPatch(typeof(MachineOutsideGrower), "InstantiateAtRandomPosition")]
        static class MachineOutsideGrower_InstantiateAtRandomPosition_Patch
        {
            static void Prefix(MachineOutsideGrower __instance, ref float __state)
            {
                if (!modEnabled.Value)
                    return;
                __state = __instance.radius;
                __instance.radius *= radiusMult.Value;
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
                __instance.updateInterval *= plantIntervalMult.Value;
                __instance.spawNumber = Mathf.RoundToInt(__instance.spawNumber * plantAmountMult.Value);
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
