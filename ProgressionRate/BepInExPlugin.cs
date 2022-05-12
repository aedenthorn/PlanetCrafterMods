using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProgressionRate
{
    [BepInPlugin("aedenthorn.ProgressionRate", "ProgressionRate", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<float> requirementMult;

        private InputAction actionDel;
        public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug logs");
            requirementMult = Config.Bind<float>("Options", "RequirementMult", 1f, "Multiply resource unlock requirements by this amount");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(UnlockingInfos), new Type[] { typeof(DataConfig.WorldUnitType), typeof(float) })]
        [HarmonyPatch(MethodType.Constructor)]
        private static class UnlockingInfos_Patch
        {
            static void Prefix(UnlockingInfos __instance, ref float _unlockingValue)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl($"{__instance.GetWorldUnit()} unlocking value original: {_unlockingValue}, new value {_unlockingValue * requirementMult.Value}");
                _unlockingValue *= requirementMult.Value;
            }
        }
        [HarmonyPatch(typeof(TerraformStage), nameof(TerraformStage.GetStageStartValue))]
        private static class TerraformStage_GetStageStartValue_Patch
        {
            static void Postfix(ref float __result)
            {
                if (!modEnabled.Value)
                    return;
                __result *= requirementMult.Value;
            }
        }
    }
}
