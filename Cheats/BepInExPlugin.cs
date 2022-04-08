using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CreativeMenu
{
    [BepInPlugin("aedenthorn.CreativeMenu", "Creative Menu", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> toggleKey;

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
            toggleKey = Config.Bind<string>("General", "ToggleKey", "u", "Key to toggle menu");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }


        private void Update()
        {
            if (!modEnabled.Value)
                return;
            if (AedenthornUtils.CheckKeyDown("f1"))
            {
                Dbgl("Pressed toggle key");
                Managers.GetManager<WindowsHandler>().ToggleUi(DataConfig.UiType.Balancing);
            }
        }
    }
}
