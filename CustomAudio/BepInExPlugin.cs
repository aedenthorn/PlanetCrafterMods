using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace CustomAudio
{
    [BepInPlugin("aedenthorn.CustomAudio", "CustomAudio", "0.2.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<bool> allowReload;
        private static ConfigEntry<string> reloadKey;

        private static Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();
        private InputAction action;

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
            allowReload = Config.Bind<bool>("Options", "AllowReload", true, "Allow use of the reload key");
            reloadKey = Config.Bind<string>("Options", "ReloadKey", "<Keyboard>/l", "Key to reload audio clips on-demand");

            action = new InputAction(binding: reloadKey.Value);
            action.Enable();

            var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
            LoadAudioClips();

            foreach (var type in typeof(PopupsHandler).Assembly.GetTypes())
            {
                if (type.FullName.Contains("TryToDisplayPopup"))
                {
                    Dbgl($"Found {type}");
                    harmony.Patch(
                       original: AccessTools.Method(type, "MoveNext"),
                       transpiler: new HarmonyMethod(typeof(BepInExPlugin), nameof(BepInExPlugin.TryToDisplayPopup_Transpiler))
                    );
                    break;
                }
            }
        }


        private void Update()
        {
            if (!modEnabled.Value)
                return;
            if (Managers.GetManager<WindowsHandler>()?.GetHasUiOpen() == true)
                return;

            if (action.WasPressedThisFrame())
            {
                var audioResourcesHandler = Managers.GetManager<AudioResourcesHandler>();
                if (audioResourcesHandler is null)
                    return;
                loadedClips.Clear();
                LoadAudioClips();
                SetClips(audioResourcesHandler);
            }
        }

        private void LoadAudioClips()
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomAudio");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }
            foreach(var file in Directory.GetFiles(path))
            {
                LoadClip(file);
            }
        }

        private void LoadClip(string path)
        {
            if (path.EndsWith(".txt") || !path.Contains("."))
                return;

            path = "file:///" + path.Replace("\\", "/");
            try
            {
                var www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN);
                www.SendWebRequest();
                while (!www.isDone)
                {

                }

                //Dbgl($"checking downloaded {filename}");
                if (www != null)
                {
                    //Dbgl("www not null. errors: " + www.error);
                    DownloadHandlerAudioClip dac = ((DownloadHandlerAudioClip)www.downloadHandler);
                    if (dac != null)
                    {
                        AudioClip ac = dac.audioClip;
                        if (ac != null)
                        {
                            string name = Path.GetFileNameWithoutExtension(path);
                            ac.name = name;
                            if (!loadedClips.ContainsKey(name))
                            {
                                loadedClips[name] = ac;
                                Dbgl($"Added audio clip {name} to dict");
                            }
                        }
                        else
                        {
                            Dbgl("audio clip is null. data: " + dac.text);
                        }
                    }
                    else
                    {
                        Dbgl("DownloadHandler is null. bytes downloaded: " + www.downloadedBytes);
                    }
                }
                else
                {
                    Dbgl("www is null " + www.url);
                }
            }
            catch { }
        }
        private static void SetClips(AudioResourcesHandler audioResourcesHandler)
        {
            AudioClip clip;
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CustomAudio");

            foreach (var field in typeof(AudioResourcesHandler).GetFields())
            {
                if (field.FieldType == typeof(AudioClip))
                {
                    Dbgl($"Checking field {field.Name}");
                    if (loadedClips.TryGetValue(field.Name, out clip))
                    {
                        Dbgl($"Setting custom audio for {field.Name}");
                        field.SetValue(audioResourcesHandler, clip);
                    }
                    else
                    {
                        Dbgl($"saving {field.Name}.wav");

                        var c = (AudioClip)field.GetValue(audioResourcesHandler);
                        if (c != null)
                        {
                            SavWav.Save(Path.Combine(path, field.Name + ".wav"), c);
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(PopupsHandler), "Start")]
        static class PopupsHandler_Start_Patch
        {
            static void Postfix(AudioResourcesHandler ___audioResourcesHandler)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl($"Started popups handler");
                SetClips(___audioResourcesHandler);
            }
        }
        [HarmonyPatch(typeof(PopupDisplayer), "Pop")]
        static class PopupDisplayer_Pop_Patch
        {
            static void Postfix(PopupData _popupData)
            {
                if (!modEnabled.Value)
                    return;
                if(_popupData.GetType().GetField("audio") != null)
                {
                    string audio = _popupData.GetType().GetField("audio").GetValue(_popupData) as string;
                    if(audio != null && loadedClips.TryGetValue(audio, out AudioClip clip))
                    {
                        FindObjectOfType<PopupsHandler>().soundContainerAlert.PlayOneShot(clip);
                        return;
                    }
                }
                
                FindObjectOfType<PopupsHandler>().soundContainerAlert.PlayOneShot(Managers.GetManager<AudioResourcesHandler>().alertUnlockableUnlocked);
            }
        }
        public static IEnumerable<CodeInstruction> TryToDisplayPopup_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Dbgl($"Transpiling TryToDisplayPopup");

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (i < codes.Count - 6 && codes[i].opcode == OpCodes.Ldloc_1  && codes[i + 2].opcode == OpCodes.Ldloc_1 && codes[i + 5].opcode == OpCodes.Callvirt && (MethodInfo)codes[i + 5].operand == AccessTools.Method(typeof(AudioSource), nameof(AudioSource.PlayOneShot), new Type[] { typeof(AudioClip) }))
                {
                    Dbgl($"Removing audio call");

                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;
                    codes[i + 1].opcode = OpCodes.Nop;
                    codes[i + 1].operand = null;
                    codes[i + 2].opcode = OpCodes.Nop;
                    codes[i + 2].operand = null;
                    codes[i + 3].opcode = OpCodes.Nop;
                    codes[i + 3].operand = null;
                    codes[i + 4].opcode = OpCodes.Nop;
                    codes[i + 4].operand = null;
                    codes[i + 5].opcode = OpCodes.Nop;
                    codes[i + 5].operand = null;
                    break;
                }
            }

            return codes.AsEnumerable();
        }
    }
}
