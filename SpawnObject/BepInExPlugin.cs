using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MijuTools;
using SpaceCraft;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SpawnObject
{
    [BepInPlugin("aedenthorn.SpawnObject", "Spawn Object", "0.2.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<string> toggleKey;
        private static ConfigEntry<string> itemText;
        private static ConfigEntry<string> amountText;

        private InputAction action;

        private static GameObject inputObject;
        private static GameObject suggestionBox;
        private static GameObject numberFieldObject;
        private static IEnumerable<string> objectNames;
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
            toggleKey = Config.Bind<string>("Options", "ToggleKey", "<Keyboard>/end", "Key to open / close GUI");
            itemText = Config.Bind<string>("Options", "ItemText", "Enter Item...", "Item text placeholder");
            amountText = Config.Bind<string>("Options", "AmountText", "Enter Amount...", "Amount text placeholder");

            if (!toggleKey.Value.Contains("<"))
                toggleKey.Value = "<Keyboard>/" + toggleKey.Value;

            action = new InputAction(binding: toggleKey.Value);
            action.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        private void Update()
        {
            if (Managers.GetManager<WindowsHandler>()?.GetHasUiOpen() != true && inputObject != null)
            {
                Destroy(inputObject);
                inputObject = null;
                return;
            }
            if (modEnabled.Value && action.WasPressedThisFrame())
            {
                if(Managers.GetManager<WindowsHandler>()?.GetHasUiOpen() == true)
                {
                    if(inputObject != null)
                    {
                        Destroy(inputObject);
                        inputObject = null;
                        Managers.GetManager<WindowsHandler>().CloseAllWindows();
                    }
                    return;
                }

                if (objectNames is null)
                {
                    objectNames = GroupsHandler.GetAllGroups().Select(g => g.GetId());
                    Dbgl($"Got {objectNames.Count()} objects");
                    /*
                    List<string> list = new List<string>();
                    foreach(var group in GroupsHandler.GetAllGroups())
                    {
                        list.Add($"{Readable.GetGroupName(group)}: {group.GetId()}");
                    }
                    File.WriteAllLines(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "items.txt"), list);
                    */
                }
                Dbgl("Creating input object");
                UiWindowTextInput templateWindow = (UiWindowTextInput)Managers.GetManager<WindowsHandler>().GetWindowViaUiId(DataConfig.UiType.TextInput);
                inputObject = Instantiate(templateWindow.gameObject, templateWindow.transform.parent);
                inputObject.name = "Spawn Item Window";
                inputObject.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 300);
                Destroy(inputObject.GetComponentInChildren<TextMeshProUGUI>().gameObject);
                foreach(Transform t in inputObject.GetComponentInChildren<Button>().transform)
                {
                    t.gameObject.SetActive(false);
                }

                UiWindowTextInput windowViaUiId = inputObject.GetComponent<UiWindowTextInput>();
                var inputField = windowViaUiId.inputField;

                foreach (var tmp in inputField.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    if (tmp.text.Length > 0)
                    {
                        tmp.text = itemText.Value;
                    }
                }

                inputObject.GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
                inputObject.GetComponentInChildren<Button>().onClick.AddListener(delegate () {
                    var text = inputField.text;
                    var amount = numberFieldObject.GetComponent<TMP_InputField>().text;
                    if (text.Length > 0 &&  amount.Length > 0 &&  objectNames.Contains(text))
                    {
                        Dbgl($"Spawning {amount} {text}");
                        int i = 0;
                        var group = GroupsHandler.GetAllGroups().FirstOrDefault(g => g.GetId() == text);
                        if (group != null)
                        {
                            while (i++ < int.Parse(amount))
                            {
                                if (Keyboard.current.leftShiftKey.isPressed && !Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory().IsFull())
                                {
                                    Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory().AddItem(WorldObjectsHandler.CreateNewWorldObject(group));
                                }
                                else
                                {
                                    WorldObjectsHandler.CreateAndDropOnFloor(group, Managers.GetManager<PlayersManager>().GetActivePlayerController().GetAimController().GetAimRay().GetPoint(0.7f));
                                }
                            }
                        }
                        if (Managers.GetManager<PopupsHandler>() != null)
                            AccessTools.FieldRefAccess<PopupsHandler, List<PopupData>>(Managers.GetManager<PopupsHandler>(), "popupsToPop").Add(new PopupData(group.GetImage(), $"Spawned {amount} {Readable.GetGroupName(group)}", 2));
                    }
                    Managers.GetManager<WindowsHandler>().CloseAllWindows();
                    Destroy(inputObject);
                    inputObject = null;
                });


                Destroy(inputField.transform.parent.GetComponent<RectMask2D>());

                numberFieldObject = Instantiate(inputField.gameObject, inputField.transform.parent);
                numberFieldObject.name = "Amount";
                numberFieldObject.GetComponent<RectTransform>().anchoredPosition = inputField.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, inputField.GetComponent<RectTransform>().rect.height * inputField.GetComponent<RectTransform>().localScale.x);

                foreach(var tmp in numberFieldObject.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    if(tmp.text.Length > 0)
                    {
                        tmp.text = amountText.Value;
                    }
                }

                numberFieldObject.GetComponent<TMP_InputField>().text = "";
                numberFieldObject.GetComponent<TMP_InputField>().contentType = TMP_InputField.ContentType.IntegerNumber;
                numberFieldObject.GetComponent<TMP_InputField>().onValueChanged = new TMP_InputField.OnChangeEvent();

                suggestionBox = new GameObject("Suggestion Box");
                suggestionBox.transform.SetParent(inputField.transform.parent, false);
                suggestionBox.AddComponent<RectTransform>().anchoredPosition = inputField.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, inputField.GetComponent<RectTransform>().rect.height * inputField.GetComponent<RectTransform>().localScale.x * 2);
                suggestionBox.GetComponent<RectTransform>().sizeDelta = new Vector2(inputField.GetComponent<RectTransform>().rect.size.x * 0.9f * inputField.GetComponent<RectTransform>().localScale.x, 1000);
                windowViaUiId.SetTextWorldObject(new WorldObjectText());
                inputField.text = "";
                inputField.onValueChanged = new TMP_InputField.OnChangeEvent();
                inputField.onValueChanged.AddListener(delegate (string value)
                {
                    foreach (Transform child in suggestionBox.transform)
                    {
                        Destroy(child.gameObject);
                    }
                    if (value.Length == 0)
                        return;
                    IEnumerable<string> possibles = objectNames.Where(s => s.ToLower().StartsWith(value.ToLower()));
                    Dbgl($"Got {possibles.Count()}/{objectNames.Count()} objects");
                    for(int i = 0; i < possibles.Count(); i++)
                    {
                        GameObject go = new GameObject($"Suggestion {i+1}");
                        go.transform.SetParent(suggestionBox.transform, false);
                        go.AddComponent<RectTransform>().anchoredPosition = new Vector2(0, i * -inputField.GetComponent<RectTransform>().rect.height * inputField.GetComponent<RectTransform>().localScale.x / 2);
                        go.GetComponent<RectTransform>().sizeDelta = windowViaUiId.inputField.GetComponent<RectTransform>().rect.size * 0.9f * inputField.GetComponent<RectTransform>().localScale.x;
                        var tmp = go.AddComponent<TextMeshProUGUI>();
                        tmp.color = Color.black;
                        tmp.text = possibles.ElementAt(i);

                        GameObject child = Instantiate(go, go.transform.parent);
                        child.GetComponent<TextMeshProUGUI>().color = Color.white;
                        child.GetComponent<RectTransform>().anchoredPosition += new Vector2(1, -1);

                        //tmp.fontSize = 24;
                        var b = child.AddComponent<Button>();
                        b.onClick.AddListener(delegate ()
                        {
                            windowViaUiId.inputField.text = tmp.text;
                        });
                    }
                });
                windowViaUiId.gameObject.SetActive(true);
                windowViaUiId.OnOpen();
                AccessTools.FieldRefAccess<WindowsHandler, DataConfig.UiType>(Managers.GetManager<WindowsHandler>(), "openedUi") = DataConfig.UiType.TextInput;
            }
        }

        //[HarmonyPatch(typeof(PlayerEquipment), "UpdateAfterEquipmentChange")]
        static class PlayerEquipment_UpdateAfterEquipmentChange_Patch
        {
            static void Prefix(PlayerEquipment __instance, WorldObject _worldObject, bool _hasBeenAdded, bool _isFirstInit)
            {
                if (!modEnabled.Value)
                    return;
            }
        }
    }
}
