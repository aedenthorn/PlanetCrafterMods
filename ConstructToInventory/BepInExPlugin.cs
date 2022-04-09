using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace ConstructToInventory
{
    [BepInPlugin("aedenthorn.ConstructToInventory", "Construct To Inventory", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;



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


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(UiWindowConstruction), "Construct")]
        private static class UiWindowConstruction_Construct_Patch
        {
            static bool Prefix(GroupConstructible groupConstructible)
            {
                if (!modEnabled.Value || !Keyboard.current.leftShiftKey.isPressed)
                    return true;
                Dbgl($"Trying to build into inventory");

                PlayerMainController activePlayerController = Managers.GetManager<PlayersManager>().GetActivePlayerController();
                if(activePlayerController.GetPlayerBackpack().GetInventory().IsFull())
                    return true;
                List<Group> ingredientsGroupInRecipe = groupConstructible.GetRecipe().GetIngredientsGroupInRecipe();
                if (Managers.GetManager<PlayModeHandler>().GetFreeCraft() || activePlayerController.GetPlayerBackpack().GetInventory().ContainsItems(ingredientsGroupInRecipe))
                {
                    Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory().AddItem(WorldObjectsHandler.CreateNewWorldObject(groupConstructible));
                }
                return true;
            }
        }
    }
}
