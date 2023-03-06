using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System.Reflection;
using UnityEngine;

namespace CustomFlashlight
{
    [BepInPlugin("aedenthorn.CustomFlashlight", "Custom Flashlight", "0.3.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<Color> color;
        public static ConfigEntry<bool> useColorTemp;
        public static ConfigEntry<int> colorTemp;
        public static ConfigEntry<float> spotlightAngle;
        public static ConfigEntry<float> spotlightInnerAngle;
        public static ConfigEntry<float> intensity;
        public static ConfigEntry<float> range;


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
            useColorTemp = Config.Bind<bool>("Options", "UseColorTemp", false, "Use color temperature.");
            color = Config.Bind<Color>("Options", "Color", new Color(1, 0.9734f, 0.9009f, 1), "Flashlight color.");
            colorTemp = Config.Bind<int>("Options", "ColorTemp", 6570, "Color temperature.");
            spotlightAngle = Config.Bind<float>("Options", "FlashlightAngle", 55.8698f, "Flashlight angle.");
            intensity = Config.Bind<float>("Options", "FlashlightIntensity", 40, "Flashlight intensity.");
            range = Config.Bind<float>("Options", "FlashlightRange", 40, "Flashlight range.");
            spotlightInnerAngle = Config.Bind<float>("Options", "FlashlightInnerAngle", 36.6912f, "Flashlight inner angle.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MetadataHelper.GetMetadata(this).GUID);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(MultiToolLight), "Start")]
        static class MultiToolLight_Start_Patch
        {
            static void Postfix(MultiToolLight __instance)
            {
                if (!modEnabled.Value)
                    return;
                Light light = __instance.toolLightT1.GetComponent<Light>();
                light.innerSpotAngle = spotlightInnerAngle.Value;
                light.spotAngle = spotlightAngle.Value;
                light.color = color.Value;
                light.useColorTemperature = useColorTemp.Value;
                light.colorTemperature = colorTemp.Value;
                light.intensity = intensity.Value;
                light.range = range.Value;
                light = __instance.toolLightT2.GetComponent<Light>();
                light.innerSpotAngle = spotlightInnerAngle.Value;
                light.spotAngle = spotlightAngle.Value;
                light.color = color.Value;
                light.useColorTemperature = useColorTemp.Value;
                light.colorTemperature = colorTemp.Value;
                light.intensity = intensity.Value;
                light.range = range.Value;
            }
        }
    }
}
