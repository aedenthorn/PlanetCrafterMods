using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ChatCommands
{
    [BepInPlugin("aedenthorn.ChatCommands", "Chat Commands", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<bool> dumpItems;
        private static ConfigEntry<string> itemText;
        private static ConfigEntry<string> amountText;
        private static List<string> history = new List<string>();

        private static IEnumerable<string> objectNames;
        public static void Dbgl(string str = "", LogLevel logLevel = LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(logLevel, str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            dumpItems = Config.Bind<bool>("Options", "DumpItems", true, "Set to true to perform a one-time item id dump to BepInEx/plugins/SpawnObject/items.txt the first time you open the GUI.");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            itemText = Config.Bind<string>("Options", "ItemText", "Enter Item...", "Item text placeholder");
            amountText = Config.Bind<string>("Options", "AmountText", "Enter Amount...", "Amount text placeholder");



            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(UiWindowChat), "Awake")]
        public static class UiWindowChat_Awake_Patch
        {
            public static void Postfix(TMP_InputField ____inputField, GameObject ____textContent, GameObject ____textField)
            {
                ____inputField.onValueChanged.AddListener(delegate (string text)
                {
                    ResetChildren(____textContent);
                    if (text.Length == 0 || !text.StartsWith("/spawn "))
                    {
                        ____textContent.SetActive(true);
                        return;
                    }
                    foreach (Transform child in ____textContent.transform)
                    {
                        child.gameObject.SetActive(false);
                    }
                    var value = text.Split(' ')[1];
                    IEnumerable<string> possibles = objectNames.Where(s => s.ToLower().StartsWith(value.ToLower()));
                    Dbgl($"Got {possibles.Count()}/{objectNames.Count()} objects");
                    for (int i = 0; i < possibles.Count(); i++)
                    {
                        GameObject go = Instantiate(____textField, ____textContent.transform);
                        go.name = "SpawnSuggest" + i;
                        var tmp = go.GetComponent<TMP_Text>();
                        tmp.text = possibles.ElementAt(i);

                        //tmp.fontSize = 24;
                        var b = go.AddComponent<Button>();
                        b.onClick.AddListener(delegate ()
                        {
                            if(____inputField.text.StartsWith("/spawn "))
                            {
                                var parts = ____inputField.text.Split(' ');
                                parts[1] = tmp.text;
                                ____inputField.text = string.Join(" ", parts);
                                ____inputField.onFocusSelectAll = false;
                                ____inputField.caretPosition = ("/spawn " + tmp.text).Length;
                                ____inputField.ActivateInputField();
                            }
                        });
                    }
                });
            }

            private static void ResetChildren(GameObject textContent)
            {
                foreach (Transform child in textContent.transform)
                {
                    if (child.name.StartsWith("SpawnSuggest"))
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }

        
        [HarmonyPatch(typeof(ChatHandler), nameof(ChatHandler.SendChatMessage))]
        public static class ChatHandler_SendChatMessage_Patch
        {
            public static void Postfix(string message)
            {
                history.Add(message);
            }
        }


        [HarmonyPatch(typeof(GroupsHandler), nameof(GroupsHandler.SetAllGroups))]
        public static class GroupsHandler_SetAllGroups_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                var groups = GroupsHandler.GetAllGroups();
                if (groups == null)
                    return;
                objectNames = groups.Select(g => g.GetId());
                Dbgl($"Got {objectNames.Count()} objects");

                if (dumpItems.Value)
                {
                    dumpItems.Value = false;
                    List<string> list = new List<string>();
                    foreach (var g in groups)
                    {
                        list.Add($"{Readable.GetGroupName(g)}: {g.GetId()}");
                    }
                    list.Sort();
                    File.WriteAllLines(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "items.txt"), list);
                }
            }
        }

        [HarmonyPatch(typeof(ChatHandler), nameof(ChatHandler.OnNetworkSpawn))]
        public static class ChatHandler_OnNetworkSpawn_Patch
        {
            public static void Postfix(Dictionary<string, Action<string>> ____commands)
            {
                ____commands.Add("/spawn", new Action<string>(CommandSpawn));
                ____commands.Add("/goto", new Action<string>(CommandTeleport));
                ____commands.Add("/pos", new Action<string>(CommandPos));

            }
        }
        public static void CommandPos(string obj)
        {
            var builder = AccessTools.FieldRefAccess<ChatHandler, StringBuilder>(ChatHandler.Instance, "_builder");
            builder.Clear();
            var pos = Managers.GetManager<PlayersManager>().GetActivePlayerController().transform.position;
            builder.Append($"{pos.ToString()}\n");
            ChatHandler.Instance.SendServiceMessage(builder.ToString());
        }

        public static void CommandTeleport(string text)
        {
            var builder = AccessTools.FieldRefAccess<ChatHandler, StringBuilder>(ChatHandler.Instance, "_builder");
            builder.Clear();
            string[] parts = text.Split(' ');
            int x = 0;
            int y = -1;
            int z = 0;
            bool valid = true;
            if (parts.Length == 2)
            {
                if (!int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out z))
                {
                    builder.Append($"Syntax: /goto <x> <y>\n");
                    valid = false;
                }
            }
            else if (parts.Length == 3)
            {
                if (!int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y) || !int.TryParse(parts[2], out z))
                {
                    builder.Append($"Syntax: /goto <x> <y> <z>\n");
                    valid = false;
                }
            }
            else
            {
                builder.Append($"Syntax: /goto <x> <y>\n");
                builder.Append($"Syntax: /goto <x> <y> <z>\n");
                valid = false;
            }
            if (valid)
            {
                Vector3 dest = Vector3.negativeInfinity;
                if(y < 0)
                {
                    RaycastHit[] racastHits = new RaycastHit[1024];
                    Vector3 vector = new Vector3(x, 2000, z);
                    int num = Physics.RaycastNonAlloc(new Ray(vector, Vector3.down), racastHits, 5000f);
                    int i = 0;
                    while (i < num)
                    {
                        RaycastHit raycastHit = racastHits[i];
                        if (raycastHit.collider.gameObject.GetComponent<Terrain>() != null)
                        {
                            dest = raycastHit.point;
                            break;
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
                else
                {
                    dest = new Vector3(x, y, z);
                }
                if(dest != Vector3.negativeInfinity)
                {
                    builder.Append($"Teleporting to {dest}");
                    Teleport(dest);
                }
            }
            ChatHandler.Instance.SendServiceMessage(builder.ToString());

        }
        public static void Teleport(Vector3 dest)
        {
            Dbgl($"Teleporting to {dest}");
            var player = Managers.GetManager<PlayersManager>().GetActivePlayerController();
            player.SetPlayerPlacement(dest, player.transform.rotation, true);
            Managers.GetManager<MeshOccluderHandler>().SpeedUpProcess(5);

        }
        public static void CommandSpawn(string text)
        {
            var builder = AccessTools.FieldRefAccess<ChatHandler, StringBuilder>(ChatHandler.Instance, "_builder");
            builder.Clear();
            string[] parts = text.Split(' ');
            if (parts.Length != 2)
            {
                builder.Append($"Syntax: /spawn <id> <amount>\n");
            }
            else if (!objectNames.Contains(parts[0]))
            {
                builder.Append($"Can't find item '{parts[0]}'\n");
            }
            else if (!int.TryParse(parts[1], out var amount))
            {
                builder.Append($"Invalid amount '{parts[1]}'\n");
            }
            else
            {
                Dbgl($"Spawning {amount} {parts[0]}");
                builder.Append($"Spawning {amount} {parts[0]}\n");
                var controller = Managers.GetManager<PlayersManager>().GetActivePlayerController();
                var aimray = controller.GetAimController().GetAimRay();
                var backpack = controller.GetPlayerBackpack();
                int i = 0;
                var group = GroupsHandler.GetAllGroups().FirstOrDefault(g => g.GetId() == parts[0]);
                if (group != null)
                {
                    while (i++ < amount)
                    {
                        if (Keyboard.current.leftShiftKey.isPressed && !backpack.GetInventory().IsFull())
                        {
                            backpack.GetInventory().AddItem(WorldObjectsHandler.Instance.CreateNewWorldObject(group));
                        }
                        else if (group is GroupConstructible)
                        {
                            controller.StartCoroutine(BuildObject(group));
                            break;
                        }
                        else
                        {
                            WorldObjectsHandler.Instance.CreateAndDropOnFloor(group, aimray.GetPoint(0.7f));
                        }
                    }
                }
                if (Managers.GetManager<PopupsHandler>() != null)
                    AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>(Managers.GetManager<PopupsHandler>(), "popupsToPop").Add(new PopupData(group.GetImage(), $"Spawned {amount} {Readable.GetGroupName(group)}", 2));
            }
            ChatHandler.Instance.SendServiceMessage(builder.ToString());

        }

        public static IEnumerator BuildObject(Group group)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(group.GetAssociatedGameObject());
            var ghost = gameObject.AddComponent<ConstructibleGhost>();
            ghost.InitGhost((GroupConstructible)group, Managers.GetManager<PlayersManager>().GetActivePlayerController().GetAimController());
            yield return new WaitForEndOfFrame();
            AccessTools.Method(typeof(GhostPlacementChecker), "CheckPlacement").Invoke(ghost.gameObject.GetComponent<GhostPlacementChecker>(), new object[0]);
            if (!ghost.Place())
            {
                ghost.DestroyGhost();
            }
        }
    }
}
