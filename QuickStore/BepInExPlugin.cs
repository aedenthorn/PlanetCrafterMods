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

namespace QuickStore
{
    [BepInPlugin("aedenthorn.QuickStore", "Quick Store", "0.1.3")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<string> storeKey;
        private static ConfigEntry<float> range;

        private InputAction action;

        private static bool skip;

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
            storeKey = Config.Bind<string>("Options", "StoreKey", "<Keyboard>/l", "Key to store items");
            range = Config.Bind<float>("Options", "Range", 20f, "Store range (m)");

            if (!storeKey.Value.Contains("<"))
                storeKey.Value = "<Keyboard>/" + storeKey.Value;

            action = new InputAction(binding: storeKey.Value);
            action.Enable();

            Dbgl("Plugin awake");

        }
        private void Update()
        {
            if (modEnabled.Value && Managers.GetManager<WindowsHandler>()?.GetHasUiOpen() == false && action.WasPressedThisFrame())
            {
                Dbgl("Hotkey Pressed");

                StoreItems();
            }
        }
        private void StoreItems()
        {
            InventoryAssociated[] ial = FindObjectsOfType<InventoryAssociated>();
            Vector2 pos = Managers.GetManager<PlayersManager>().GetActivePlayerController().transform.position;

            Dbgl($"got {ial.Length} inventories");

            var objects = Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory().GetInsideWorldObjects();
            InformationsDisplayer informationsDisplayer = Managers.GetManager<DisplayersHandler>().GetInformationsDisplayer();

            for (int i = 0; i < ial.Length; i++)
            {
                var dist = Vector2.Distance(ial[i].transform.position, pos);
                try
                {
                    ial[i].GetInventory();
                }
                catch
                {
                    continue;
                }
                if (ial[i].GetInventory() == Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory() || dist > range.Value || ial[i].GetInventory().IsFull())
                    continue;
                Dbgl($"checking close inventory {ial[i].name}: {ial[i].transform.position}, {pos}: {dist}m");

                for(int j = objects.Count - 1; j >= 0; j--)
                {
                    if (!ial[i].GetInventory().IsFull() && ial[i].GetInventory().GetInsideWorldObjects().Exists(o => o.GetGroup() == objects[j].GetGroup()))
                    {
                        Dbgl($"Storing {objects[j].GetGroup()} in {ial[i].name}");
                        ial[i].GetInventory().AddItem(objects[j]);
                        informationsDisplayer.AddInformation(2f, Readable.GetGroupName(objects[j].GetGroup()), DataConfig.UiInformationsType.OutInventory, objects[j].GetGroup().GetImage());
                        Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory().RemoveItem(objects[j]);
                        if (ial[i].GetInventory().IsFull())
                            break;
                    }
                }

                if (objects.Count == 0)
                {
                    Dbgl($"stored all items");
                    return;
                }
            }
        }
    }
}
