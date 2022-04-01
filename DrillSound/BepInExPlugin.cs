using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DrillSound
{
    [BepInPlugin("aedenthorn.DrillSound", "Drill Sound", "0.1.0")]
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

            muteSound = Config.Bind<bool>("Options", "MuteSound", false, "Mute sound entirely");
            fixedPitch = Config.Bind<bool>("Options", "FixedPitch", true, "Use fixed pitch");
            pitch = Config.Bind<float>("Options", "Pitch", 0.5f, "Pitch if using fixed pitch.");
            volume = Config.Bind<float>("Options", "Volume", 0.5f, "Volume of drill sound.");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(PlayerAudio), "PlayRecolt")]
        static class PlayerAudio_PlayRecolt_Patch
        {
            static bool Prefix(PlayerAudio __instance, ref bool _play, float _changeSpeedPercentage)
            {
                if (!modEnabled.Value || !_play)
                    return true;
                Debug.Log($"play recolt speed %: {_changeSpeedPercentage}");
                if (muteSound.Value)
                {
                    _play = false;
                    return true;
                }
                __instance.soundContainerRecolt.volume = volume.Value;
                if (fixedPitch.Value)
                {
                    __instance.soundContainerRecolt.pitch = pitch.Value;
                    __instance.recoltMixer.SetFloat("RecoltPitch", 1f / (1f + _changeSpeedPercentage / 100f));
                    __instance.soundContainerRecolt.Play();
                    return false;
                }
                return true;
            }
        }
    }
}
