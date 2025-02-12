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

namespace BetterJetPack
{
    [BepInPlugin("aedenthorn.BetterJetPack", "Better JetPack", "0.3.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> speedMult;
        public static ConfigEntry<float> highCutoffMult;
        public static ConfigEntry<float> highStartValue;
        public static ConfigEntry<float> highTargetValue;
        public static ConfigEntry<float> lowStartValue;
        public static ConfigEntry<float> lowTargetValue;

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
            speedMult = Config.Bind<float>("Options", "SpeedMult", 1f, "Jetpack speed multiplier");
            highStartValue = Config.Bind<float>("Options", "HighStartValue", 0f, "High start value");
            highTargetValue = Config.Bind<float>("Options", "HighTargetValue", 20f, "High target value");
            lowStartValue = Config.Bind<float>("Options", "LowStartValue", -5f, "Low start value");
            lowTargetValue = Config.Bind<float>("Options", "LowTargetValue", 0f, "Low target value");
            highCutoffMult = Config.Bind<float>("Options", "HighCutoffMult", 1f, "High cutoff multiplier");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(PlayerMovable), nameof(PlayerMovable.UpdatePlayerMovement))]
        static class PlayerMovable_UpdatePlayerMovement_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                if(!modEnabled.Value)
                    return codes.AsEnumerable();

                bool found1 = false;
                bool found2 = false;
                Dbgl("Transpiling PlayerMovable.UpdatePlayerMovement");
                for (int i = 0; i < codes.Count; i++)
                {
                    if (!found1 && codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == typeof(PlayerGroundRelation).GetMethod(nameof(PlayerGroundRelation.GetGroundDistance)))
                    {
                        Dbgl("adjusting ground distance");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, typeof(BepInExPlugin).GetMethod(nameof(BepInExPlugin.JetpackHighMult))));
                        found1 = true;
                    }
                    if (!found2 && i > 8 && i < codes.Count - 5 && codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == typeof(Mathf).GetMethod(nameof(Mathf.Lerp)) && codes[i - 8].opcode == OpCodes.Ldc_R4 && codes[i - 7].opcode == OpCodes.Ldc_R4 && codes[i + 3].opcode == OpCodes.Ldc_R4 && codes[i + 4].opcode == OpCodes.Ldc_R4)
                    {
                        Dbgl("Adjusting jetpack falloff");
                        codes.Insert(i + 5, new CodeInstruction(OpCodes.Call, typeof(BepInExPlugin).GetMethod(nameof(BepInExPlugin.JetpackLowMax))));
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Call, typeof(BepInExPlugin).GetMethod(nameof(BepInExPlugin.JetpackLowMin))));
                        codes.Insert(i - 6, new CodeInstruction(OpCodes.Call, typeof(BepInExPlugin).GetMethod(nameof(BepInExPlugin.JetpackHighMax))));
                        codes.Insert(i - 7, new CodeInstruction(OpCodes.Call, typeof(BepInExPlugin).GetMethod(nameof(BepInExPlugin.JetpackHighMin))));

                        found2 = true;
                    }
                    if (found1 && found2)
                        break;
                }
                return codes.AsEnumerable();
            }
            static void Prefix(PlayerMovable __instance, ref float ___jetpackFactor, ref float __state)
            {
                if (!modEnabled.Value)
                    return;
                __state = ___jetpackFactor;
                ___jetpackFactor *= speedMult.Value;
            }
            static void Postfix(PlayerMovable __instance, ref float ___jetpackFactor, ref float __state)
            {
                if (!modEnabled.Value)
                    return;
                ___jetpackFactor = __state;
            }
        }

        public static float JetpackHighMult(float value)
        {
            if (!modEnabled.Value)
                return value;
            return value / highCutoffMult.Value;
        }

        public static float JetpackHighMin(float value)
        {
            if (!modEnabled.Value)
                return value;
            return highStartValue.Value;
        }
        public static float JetpackHighMax(float value)
        {
            if (!modEnabled.Value)
                return value;
            return highTargetValue.Value;
        }
        public static float JetpackLowMin(float value)
        {
            if (!modEnabled.Value)
                return value;
            return lowStartValue.Value;
        }
        public static float JetpackLowMax(float value)
        {
            if (!modEnabled.Value)
                return value;
            return lowTargetValue.Value;
        }
    }
}
