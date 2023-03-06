using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Reflection;

namespace CameraShakeTweaks
{
    [BepInPlugin("aedenthorn.CameraShakeTweaks", "CameraShakeTweaks", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<bool> disableShake;
        public static ConfigEntry<float> shakeMult;
        public static ConfigEntry<float> decreaseMult;

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
            disableShake = Config.Bind<bool>("Options", "DisableShake", false, "Completely disable camera shake.");
            shakeMult = Config.Bind<float>("Options", "ShakeMult", 0.5f, "Multiply shake effect by this amount.");
            decreaseMult = Config.Bind<float>("Options", "DecreaseMult", 1f, "Multiply shake decrease over time by this amount.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(PlayerCameraShake), nameof(PlayerCameraShake.SetShaking))]
        private static class PlayerCameraShake_SetShaking_Patch
        {
            static void Prefix(PlayerCameraShake __instance, ref bool _isShaking, ref float _shakeValue, ref float _decreaseValue)
            {
                if (!modEnabled.Value)
                    return;
                if (disableShake.Value)
                    _isShaking = false;
                _shakeValue *= shakeMult.Value;
                _decreaseValue *= decreaseMult.Value;

            }
        }
    }
}
