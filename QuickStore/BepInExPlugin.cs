using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using MijuTools;
using SpaceCraft;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuickStore
{
    [BepInPlugin("aedenthorn.QuickStore", "Quick Store", "0.3.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<bool> allowStoreInChests;
        private static ConfigEntry<string> storeKey;
        private static ConfigEntry<string> allowList;
        private static ConfigEntry<string> disallowList;
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
            allowStoreInChests = Config.Bind<bool>("Options", "AllowStoreInChests", false, "Allow storing in chests.");
            storeKey = Config.Bind<string>("Options", "StoreKey", "<Keyboard>/l", "Key to store items");
            allowList = Config.Bind<string>("Options", "AllowList", "", "Comma-separated list of item IDs to allow storing (overrides DisallowList).");
            disallowList = Config.Bind<string>("Options", "DisallowList", "", "Comma-separated list of item IDs to disallow storing (if AllowList is empty)");
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
            List<string> allow = allowList.Value.Split(',').ToList();
            List<string> disallow = disallowList.Value.Split(',').ToList();
            InventoryAssociated[] ial = FindObjectsOfType<InventoryAssociated>();
            Vector2 pos = Managers.GetManager<PlayersManager>().GetActivePlayerController().transform.position;

            Dbgl($"got {ial.Length} inventories");

            List<WorldObject> objects = Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory().GetInsideWorldObjects();

            InformationsDisplayer informationsDisplayer = Managers.GetManager<DisplayersHandler>().GetInformationsDisplayer();
            for (int i = 0; i < ial.Length; i++)
            {
                var dist = Vector2.Distance(ial[i].transform.position, pos);
                if (dist > range.Value || (!ial[i].name.StartsWith("Container1") && !ial[i].name.StartsWith("Container2")) || (ial[i].name.StartsWith("Container1") && !allowStoreInChests.Value))
                    continue;
                try
                {
                    ial[i].GetInventory();
                }
                catch
                {
                    continue;
                }
                if (ial[i].GetInventory() == Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory() || ial[i].GetInventory().IsFull())
                    continue;
                Dbgl($"checking close inventory {ial[i].name}: {ial[i].transform.position}, {pos}: {dist}m");

                for (int j = objects.Count - 1; j >= 0; j--)
                {
                    if (allowList.Value.Length > 0)
                    {
                        if (!allow.Contains(objects[j].GetGroup().GetId()))
                            continue;
                    }
                    else if (disallowList.Value.Length > 0)
                    {
                        if (disallow.Contains(objects[j].GetGroup().GetId()))
                            continue;
                    }
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
