using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Collections;
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
    [BepInPlugin("aedenthorn.SpawnObject", "Spawn Object", "0.7.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<bool> dumpItems;
        private static ConfigEntry<string> toggleKey;
        private static ConfigEntry<string> spawnKey;
        private static ConfigEntry<string> itemText;
        private static ConfigEntry<string> amountText;

        private static InputAction action;
        //private static InputAction action2;
        private static Texture2D hexTexture;
        private static GameObject inputObject;
        private static GameObject suggestionBox;
        private static GameObject numberFieldObject;
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
            isDebug = Config.Bind<bool>("General", "IsDebug", false, "Enable debug logs");
            toggleKey = Config.Bind<string>("Options", "ToggleKey", "<Keyboard>/end", "Key to open / close GUI");
            //spawnKey = Config.Bind<string>("Options", "SpawnKey", "<Keyboard>/enter", "Key to spawn item");
            itemText = Config.Bind<string>("Options", "ItemText", "Enter Item...", "Item text placeholder");
            amountText = Config.Bind<string>("Options", "AmountText", "Enter Amount...", "Amount text placeholder");

            if (!toggleKey.Value.Contains("<"))
                toggleKey.Value = "<Keyboard>/" + toggleKey.Value;

            //if (!spawnKey.Value.Contains("<"))
            //    spawnKey.Value = "<Keyboard>/" + spawnKey.Value;

            hexTexture = new Texture2D(46, 46);
            Color[] colors = new Color[46 * 46];
            for(int y = 0; y < 46; y++)
            {
                for (int x = 0; x < 46; x++)
                {
                    if(y < 23)
                    {
                        if (x == 21 + y || x == 20 + y || x == 19 + y || x == 18 + y)
                            colors[y * 46 + x] = Color.white;
                        else if (x == 23 + y || x == 22 + y || x == 17 + y || x == 16 + y)
                            colors[y * 46 + x] = Color.white * 0.5f;
                        else
                            colors[y * 46 + x] = Color.clear;
                    }
                    else
                    {
                        if (x == 43 - y + 23 || x == 42 - y + 23 || x == 41 - y + 23 || x == 40 - y + 23)
                            colors[y * 46 + x] = Color.white;
                        else if (x == 45 - y + 23 || x == 44 - y + 23  || x == 39 - y + 23 || x == 38 - y + 23)
                            colors[y * 46 + x] = Color.white * 0.5f;
                        else
                            colors[y * 46 + x] = Color.clear;
                    }
                }
            }
            hexTexture.SetPixels(colors);
            hexTexture.Apply();

            action = new InputAction(binding: toggleKey.Value);
            action.Enable();

            //action2 = new InputAction(binding: spawnKey.Value);
            //action2.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(PlayerInputDispatcher), "Update")]
        public static class PlayerInputDispatcher_Update_Patch
        {
            public static void Postfix(PlayerInputDispatcher __instance)
            {
                if (!modEnabled.Value || Managers.GetManager<WindowsHandler>() == null)
                    return;

                var uiOpen = Managers.GetManager<WindowsHandler>().GetHasUiOpen();

                if (!uiOpen && inputObject != null)
                {
                    Dbgl($"ui open, closing");
                    UiWindowTextInput templateWindow = (UiWindowTextInput)Managers.GetManager<WindowsHandler>().OpenAndReturnUi(DataConfig.UiType.TextInput);
                    inputObject.GetComponent<UiWindowTextInput>().OnClose();
                    Destroy(inputObject);
                    inputObject = null;
                    Managers.GetManager<WindowsHandler>().CloseAllWindows();
                    return;
                }
                /*
                if (modEnabled.Value && uiOpen && inputObject != null && action2.WasPressedThisFrame())
                {
                    Dbgl("pressed spawn key");
                    inputObject.GetComponentInChildren<Button>(true)?.onClick.Invoke();
                }
                */
                if (modEnabled.Value && action.WasPressedThisFrame())
                {
                    Dbgl("pressed action");

                    if (uiOpen)
                    {
                        if (inputObject != null)
                        {
                            Managers.GetManager<WindowsHandler>().OpenAndReturnUi(DataConfig.UiType.TextInput);

                            inputObject.GetComponent<UiWindowTextInput>().OnClose();
                            Destroy(inputObject);
                            inputObject = null;
                            Managers.GetManager<WindowsHandler>().CloseAllWindows();
                        }
                        return;
                    }
                    if ((bool)AccessTools.Method(typeof(PlayerInputDispatcher), "IsTyping").Invoke(__instance, new object[0]))
                        return;
                    if (objectNames is null)
                    {

                        objectNames = GroupsHandler.GetAllGroups().Select(g => g.GetId());
                        Dbgl($"Got {objectNames.Count()} objects");

                        if (dumpItems.Value)
                        {
                            Dbgl($"Dumping items");

                            dumpItems.Value = false;
                            List<string> list = new List<string>();
                            foreach (var group in GroupsHandler.GetAllGroups())
                            {
                                list.Add($"{Readable.GetGroupName(group)}: {group.GetId()}");
                            }
                            list.Sort();
                            File.WriteAllLines(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "items.txt"), list);
                        }
                    }

                    Dbgl("Creating input object");
                    UiWindowTextInput templateWindow = (UiWindowTextInput)Managers.GetManager<WindowsHandler>().OpenAndReturnUi(DataConfig.UiType.TextInput);
                    if (templateWindow == null)
                    {
                        Dbgl("missing template window");
                        return;
                    }
                    inputObject = Instantiate(templateWindow.gameObject, templateWindow.transform.parent);
                    inputObject.SetActive(true);
                    templateWindow.gameObject.SetActive(false);
                    inputObject.name = "Spawn Item Window";
                    inputObject.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 300);
                    Destroy(inputObject.GetComponentInChildren<TextMeshProUGUI>().gameObject);

                    Dbgl("Creating input field");

                    UiWindowTextInput windowViaUiId = inputObject.GetComponent<UiWindowTextInput>();
                    windowViaUiId.inputField = inputObject.GetComponentInChildren<TMP_InputField>(true);
                    var inputField = windowViaUiId.inputField;
                    inputField.characterLimit = 0;
                    foreach (var tmp in inputField.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        if (tmp.text.Length > 0)
                        {
                            tmp.text = itemText.Value;
                        }
                    }


                    Dbgl("Creating button");
                    Button goButton = inputObject.GetComponentInChildren<Button>(true);
                    if (goButton != null)
                    {
                        goButton.gameObject.SetActive(true);
                        goButton.interactable = true;
                        goButton.enabled = true;
                        foreach (Transform t in goButton.transform)
                        {
                            if (t.GetComponent<Image>() != null)
                            {
                                t.GetComponent<Image>().sprite = Sprite.Create(hexTexture, new Rect(0, 0, 46, 46), Vector2.zero);
                            }
                        }
                        goButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(270, 367);

                        goButton.onClick = new Button.ButtonClickedEvent();
                        goButton.onClick.AddListener(delegate ()
                        {
                            Dbgl($"Pressed button");

                            var text = inputField.text;
                            var amount = numberFieldObject.GetComponent<TMP_InputField>().text;
                            var controller = Managers.GetManager<PlayersManager>().GetActivePlayerController();
                            var aimray = controller.GetAimController().GetAimRay();
                            var backpack = controller.GetPlayerBackpack();
                            if (text.Length > 0 && amount.Length > 0 && objectNames.Contains(text))
                            {
                                Dbgl($"Spawning {amount} {text}");
                                int i = 0;
                                var group = GroupsHandler.GetAllGroups().FirstOrDefault(g => g.GetId() == text);
                                if (group != null)
                                {
                                    while (i++ < int.Parse(amount))
                                    {
                                        if (Keyboard.current.leftShiftKey.isPressed && !backpack.GetInventory().IsFull())
                                        {
                                            backpack.GetInventory().AddItem(WorldObjectsHandler.Instance.CreateNewWorldObject(group));
                                        }
                                        else if (group is GroupConstructible)
                                        {
                                            Dbgl($"{context is null}");
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
                            else
                            {
                                Dbgl($"missing amount or item");
                            }
                            Managers.GetManager<WindowsHandler>().OpenAndReturnUi(DataConfig.UiType.TextInput);
                            inputObject.GetComponent<UiWindowTextInput>().OnClose();
                            Destroy(inputObject);
                            inputObject = null;
                            Managers.GetManager<WindowsHandler>().CloseAllWindows();
                        });
                    }
                    else
                    {
                        Dbgl("Button is null");
                    }

                    Dbgl("Creating number field");


                    Destroy(inputField.transform.parent.GetComponent<RectMask2D>());

                    numberFieldObject = Instantiate(inputField.gameObject, inputField.transform.parent);
                    numberFieldObject.name = "Amount";
                    numberFieldObject.GetComponent<RectTransform>().anchoredPosition = inputField.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, inputField.GetComponent<RectTransform>().rect.height * inputField.GetComponent<RectTransform>().localScale.x);
                    var numberField = numberFieldObject.GetComponent<TMP_InputField>();
                    foreach (var tmp in numberFieldObject.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        if (tmp.text.Length > 0)
                        {
                            tmp.enabled = true;
                            tmp.text = amountText.Value;
                        }
                    }

                    numberField.onValueChanged = new TMP_InputField.OnChangeEvent();
                    numberField.text = "";
                    numberField.contentType = TMP_InputField.ContentType.IntegerNumber;

                    Dbgl("Creating suggestion box");

                    suggestionBox = new GameObject("Suggestion Box");
                    suggestionBox.transform.SetParent(inputField.transform.parent, false);
                    suggestionBox.AddComponent<RectTransform>().anchoredPosition = inputField.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, inputField.GetComponent<RectTransform>().rect.height * inputField.GetComponent<RectTransform>().localScale.x * 2);
                    suggestionBox.GetComponent<RectTransform>().sizeDelta = new Vector2(inputField.GetComponent<RectTransform>().rect.size.x * 0.9f * inputField.GetComponent<RectTransform>().localScale.x, 1000);
                    var wot = inputObject.AddComponent<WorldObjectText>();
                    wot.textContainer = inputObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (wot.textContainer == null)
                    {
                        Dbgl("text container is null, creating");
                        wot.textContainer = inputObject.AddComponent<TextMeshProUGUI>();
                    }
                    var proxy = inputObject.GetComponentInChildren<TextProxy>();
                    if(proxy == null)
                    {
                        Dbgl("text proxy is null, creating");
                        proxy = inputObject.AddComponent<TextProxy>();
                    }
                    AccessTools.FieldRefAccess<WorldObjectText, TextProxy>(wot, "_proxy") = proxy;
                    inputField.onValueChanged = new TMP_InputField.OnChangeEvent();
                    inputField.text = "";
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
                        for (int i = 0; i < possibles.Count(); i++)
                        {
                            GameObject go = new GameObject($"Suggestion {i + 1}");
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
                            b.interactable = true;
                        }
                    });
                    Dbgl("activating window");
                    windowViaUiId.gameObject.SetActive(true);
                    windowViaUiId.OnOpen();
                    windowViaUiId.SetTextWorldObject(wot);
                    Dbgl("Window created successfully");

                }
            }
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
