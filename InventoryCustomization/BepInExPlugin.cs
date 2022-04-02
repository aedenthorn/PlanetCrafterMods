using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace InventoryCustomization
{
    [BepInPlugin("aedenthorn.InventoryCustomization", "Inventory Customization", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> muteSound;
        public static ConfigEntry<bool> fixedPitch;
        public static ConfigEntry<float> pitch;
        public static ConfigEntry<float> volume;


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
            muteSound = Config.Bind<bool>("Options", "MuteSound", false, "Mute drill sound entirely.");
            pitch = Config.Bind<float>("Options", "Pitch", 0.5f, "Drill sound pitch.");
            volume = Config.Bind<float>("Options", "Volume", 0.25f, "Drill sound volume.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetSize))]
        static class Inventory_GetSize_Patch
        {
            static bool Prefix(PlayerAudio __instance, ref bool _play, float _changeSpeedPercentage)
            {
                if (!modEnabled.Value || !_play)
                    return true;
                if (muteSound.Value)
                {
                    _play = false;
                    return true;
                }
                __instance.soundContainerRecolt.volume = volume.Value;
                __instance.soundContainerRecolt.pitch = 1f + _changeSpeedPercentage / 100f;
                __instance.recoltMixer.SetFloat("RecoltPitch", 1 / (1f + _changeSpeedPercentage / 100f) * pitch.Value);
                __instance.soundContainerRecolt.Play();
                return false;
            }
        }
    }
}
