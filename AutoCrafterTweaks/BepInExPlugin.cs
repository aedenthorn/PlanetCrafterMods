using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Reflection;

namespace AutoCrafterTweaks
{
    [BepInPlugin("aedenthorn.AutoCrafterTweaks", "Auto Crafter Tweaks", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<float> range;
        public static ConfigEntry<float> interval;

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
            range = Config.Bind<float>("Options", "Range", 30f, "Range (m)");
            interval = Config.Bind<float>("Options", "Interval", 1f, "Craft interval in seconds");

            range.SettingChanged += SettingChanged;
            interval.SettingChanged += SettingChanged;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
            InvokeRepeating("CheckAutoHarvest", 0, interval.Value);

            ResetValues();
        }


        private void SettingChanged(object sender, EventArgs e)
        {
            ResetValues();
        }

        private void ResetValues()
        {
            if (!modEnabled.Value)
                return;
            foreach(var m in FindObjectsByType(typeof(MachineAutoCrafter), UnityEngine.FindObjectsSortMode.None))
            {
                SetValues((MachineAutoCrafter)m);
            }
        }

        [HarmonyPatch(typeof(MachineAutoCrafter), "Awake")]
        private static class MachineAutoCrafter_Start_Patch
        {
            static void Prefix(MachineAutoCrafter __instance)
            {
                if (!modEnabled.Value)
                    return;

                SetValues(__instance);
            }

        }

        private static void SetValues(MachineAutoCrafter machine)
        {

            machine.range = range.Value;
            machine.craftEveryXSec = interval.Value;
        }

    }
}
