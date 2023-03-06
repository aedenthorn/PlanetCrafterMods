using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace StorageAnywhere
{
    [BepInPlugin("aedenthorn.StorageAnywhere", "Storage Anywhere", "0.3.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> ignoreSingleCell;
        public static ConfigEntry<bool> ignoreUnnamed;
        private static ConfigEntry<string> ignoreTypes;
        private static ConfigEntry<string> toggleKey;
        private static ConfigEntry<string> leftKey;
        private static ConfigEntry<string> rightKey;
        private static ConfigEntry<float> range;

        private static InputAction action;
        private static InputAction actionLeft;
        private static InputAction actionRight;
        private static List<InventoryAssociated> inventoryList = new List<InventoryAssociated>();
        private static int currentIndex = 0;
        private static TMP_Dropdown dropDown;

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
            ignoreSingleCell = Config.Bind<bool>("Options", "IgnoreSingleCell", true, "Ignore single cell inventories");
            ignoreUnnamed = Config.Bind<bool>("Options", "IgnoreUnnamed", false, "Ignore unnamed inventories");
            ignoreTypes = Config.Bind<string>("Options", "IgnoreTypes", "Golden", "Ignore inventories with type names that contain strings in this list (comma-separated)");
            toggleKey = Config.Bind<string>("Options", "ToggleKey", "<Keyboard>/i", "Key to toggle inventory UI");
            leftKey = Config.Bind<string>("Options", "LeftKey", "<Keyboard>/leftArrow", "Key to switch to previous inventory");
            rightKey = Config.Bind<string>("Options", "RightKey", "<Keyboard>/rightArrow", "Key to switch to next inventory");
            range = Config.Bind<float>("Options", "Range", 20f, "Range (m)");

            action = new InputAction(binding: toggleKey.Value);
            actionLeft = new InputAction(binding: leftKey.Value);
            actionRight = new InputAction(binding: rightKey.Value);
            action.Enable();
            actionLeft.Enable();
            actionRight.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(PlayerInputDispatcher), "Update")]
        static class PlayerInputDispatcher_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value || Managers.GetManager<PlayersManager>() == null)
                    return;
                if (action.WasPressedThisFrame())
                {
                    if (Managers.GetManager<WindowsHandler>().GetHasUiOpen())
                    {
                        if (Managers.GetManager<WindowsHandler>().GetOpenedUi() == DataConfig.UiType.Container)
                        {
                            Managers.GetManager<WindowsHandler>().CloseAllWindows();
                        }
                        return;
                    }
                    currentIndex = 0;
                    inventoryList = GetNearbyInventories();
                    SetInventories();
                }
                else if (!Managers.GetManager<WindowsHandler>().GetHasUiOpen())
                {
                    currentIndex = 0;
                    inventoryList.Clear();
                }
                else if (actionRight.WasPressedThisFrame() && Managers.GetManager<WindowsHandler>().GetOpenedUi() == DataConfig.UiType.Container)
                {
                    inventoryList = GetNearbyInventories();
                    currentIndex++;
                    currentIndex %= inventoryList.Count;
                    SetInventories();
                }
                else if (actionLeft.WasPressedThisFrame() && Managers.GetManager<WindowsHandler>().GetOpenedUi() == DataConfig.UiType.Container)
                {
                    inventoryList = GetNearbyInventories();
                    currentIndex--;
                    if (currentIndex < 0)
                        currentIndex = inventoryList.Count - 1;
                    SetInventories();
                }
            }
        }

        private static void SetInventories()
        {
            Managers.GetManager<WindowsHandler>().CloseAllWindows();

            UiWindowContainer uiWindowContainer = (UiWindowContainer)Managers.GetManager<WindowsHandler>().OpenAndReturnUi(DataConfig.UiType.Container);
            if (uiWindowContainer != null && inventoryList.Count > 0)
            {
                dropDown = uiWindowContainer.transform.Find("Container").gameObject.GetComponent<TMP_Dropdown>();
                if (dropDown is null)
                {
                    Vector2 containerSize = new Vector2(500, 500);
                    Vector2 cellSize = new Vector2(500, 30);
                    var text = uiWindowContainer.transform.Find("Container").GetComponent<TextMeshProUGUI>();

                    dropDown = uiWindowContainer.transform.Find("Container").gameObject.AddComponent<TMP_Dropdown>();
                    dropDown.captionText = text;
                    dropDown.onValueChanged.AddListener(ChangeInventory);

                    GameObject template = new GameObject("Template");
                    template.transform.SetParent(dropDown.transform, false);
                    template.gameObject.SetActive(false);
                    dropDown.template = template.AddComponent<RectTransform>();
                    dropDown.template.anchoredPosition = new Vector2(0, -200);
                    dropDown.template.sizeDelta = containerSize;

                    GameObject scrollObject = new GameObject() { name = "ScrollView" };
                    scrollObject.transform.SetParent(template.transform, false);
                    RectTransform rts = scrollObject.AddComponent<RectTransform>();
                    rts.sizeDelta = containerSize;

                    GameObject mask = new GameObject { name = "Mask" };
                    mask.transform.SetParent(scrollObject.transform);
                    RectTransform rtm = mask.AddComponent<RectTransform>();
                    rtm.anchoredPosition = Vector2.zero;
                    rtm.sizeDelta = containerSize;

                    Texture2D tex = new Texture2D((int)Mathf.Ceil(rtm.rect.width), (int)Mathf.Ceil(rtm.rect.height));
                    Image image = mask.AddComponent<Image>();
                    image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
                    Mask m = mask.AddComponent<Mask>();
                    m.showMaskGraphic = false;

                    GameObject content = new GameObject { name = "Content" };
                    content.transform.SetParent(mask.transform);
                    RectTransform rtc = content.AddComponent<RectTransform>();
                    rtc.anchoredPosition = Vector2.zero;
                    rtc.sizeDelta = cellSize;

                    GameObject item = new GameObject("Item");
                    item.transform.SetParent(content.transform, false);
                    var rti = item.AddComponent<RectTransform>();
                    rti.sizeDelta = cellSize + new Vector2(0, 4);

                    Texture2D itemTex = new Texture2D(500, 25);
                    Texture2D itemSelTex = new Texture2D(500, 25);
                    Color[] colors = new Color[itemTex.width * itemTex.height];
                    Color[] colorsS = new Color[itemTex.width * itemTex.height];
                    for (int i = 0; i < colors.Length; i++)
                    {
                        colors[i] = new Color(0, 0, 0, 0.9f);
                        colorsS[i] = new Color(1, 1, 1, 0.9f);
                    }
                    itemTex.SetPixels(colors);
                    itemSelTex.SetPixels(colorsS);
                    itemTex.Apply();
                    itemSelTex.Apply();

                    Image itemBack = Instantiate(new GameObject("Background"), item.transform).AddComponent<Image>();
                    itemBack.sprite = Sprite.Create(itemTex, new Rect(0, 0, itemTex.width, itemTex.height), Vector2.zero);
                    itemBack.GetComponent<RectTransform>().sizeDelta = cellSize;

                    Image itemSel = Instantiate(new GameObject("Selected"), item.transform).AddComponent<Image>();
                    itemSel.sprite = Sprite.Create(itemSelTex, new Rect(0, 0, itemTex.width, itemTex.height), Vector2.zero);
                    itemSel.GetComponent<RectTransform>().sizeDelta = cellSize;
                    itemSel.gameObject.SetActive(false);

                    Toggle t = item.AddComponent<Toggle>();

                    TextMeshProUGUI itemText = Instantiate(new GameObject("Text"), item.transform).AddComponent<TextMeshProUGUI>();
                    itemText.font = text.font;
                    itemText.fontSize = 16;
                    itemText.GetComponent<RectTransform>().sizeDelta = cellSize;
                    dropDown.itemText = itemText;

                    ScrollRect sr = scrollObject.AddComponent<ScrollRect>();
                    sr.movementType = ScrollRect.MovementType.Clamped;
                    sr.horizontal = false;
                    sr.viewport = mask.GetComponent<RectTransform>();
                    sr.content = content.transform.GetComponent<RectTransform>();
                    sr.verticalNormalizedPosition = 1;
                    sr.scrollSensitivity = 10;
                }

                dropDown.onValueChanged = new TMP_Dropdown.DropdownEvent();

                dropDown.ClearOptions();
                for (int i = 0; i < inventoryList.Count; i++)
                {
                    dropDown.options.Add(new TMP_Dropdown.OptionData() { text = GetObjectName(inventoryList[i].gameObject) });
                    if (i == currentIndex)
                        dropDown.value = i;
                }
                dropDown.RefreshShownValue();
                dropDown.onValueChanged.AddListener(ChangeInventory);

                uiWindowContainer.SetInventories(Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory(), inventoryList[currentIndex].GetInventory(), false);
            }
        }

        private static void OnItemHover(EventTriggerCallbackData obj)
        {
            Managers.GetManager<GlobalAudioHandler>().PlayUiHover();
        }

        private static void ChangeInventory(int arg0)
        {
            currentIndex = arg0;
            SetInventories();
        }

        private static string GetObjectName(GameObject go, bool typeOnly = false, bool nameOnly = false)
        {
            Transform t = go.transform;
            if (!typeOnly && t.GetComponent<WorldObjectText>() != null)
            {
                return t.GetComponent<WorldObjectText>().GetText(); 
            }
            if (!nameOnly && t.GetComponent<WorldObjectAssociated>() != null)
            {
                return Readable.GetGroupName(t.GetComponent<WorldObjectAssociated>().GetWorldObject().GetGroup());
            }
            while (t.parent != null && t.parent.parent != null && t.parent.name != "WorldObjectsContainer")
            {
                t = t.parent;
                if (!typeOnly && t.GetComponent<WorldObjectText>() != null)
                {
                    return t.GetComponent<WorldObjectText>().GetText();
                }
                if (!nameOnly && t.GetComponent<WorldObjectAssociated>() != null)
                {
                    return Readable.GetGroupName(t.GetComponent<WorldObjectAssociated>().GetWorldObject().GetGroup());
                }
            }
            return typeOnly || nameOnly ? "" : t.name.Replace("(Clone)", "");
        }

        private static List<InventoryAssociated> GetNearbyInventories()
        {
            List<InventoryAssociated> result = new List<InventoryAssociated>();
            InventoryAssociated[] ial = FindObjectsOfType<InventoryAssociated>();
            Vector3 pos = Managers.GetManager<PlayersManager>().GetActivePlayerController().transform.position;

            Dbgl($"got {ial.Length} inventories");
            var ignores = ignoreTypes.Value.Split(',');
            for (int i = 0; i < ial.Length; i++)
            {
                try
                {
                    ial[i].GetInventory();
                }
                catch
                {
                    continue;
                }
                var dist = Vector3.Distance(ial[i].transform.position, pos);
                if (dist > range.Value)
                    continue;

                if (ignoreUnnamed.Value && GetObjectName(ial[i].gameObject, false, true).Length == 0)
                {
                    continue;
                }

                if (ignoreSingleCell.Value && ial[i].GetInventory().GetSize() == 1)
                {
                    continue;
                }

                bool ignore = false;
                string type = GetObjectName(ial[i].gameObject, true, false);
                foreach (string ign in ignores)
                {
                    if(type.Contains(ign))
                    {
                        ignore = true;
                        break;
                    }
                }

                if (!ignore && ial[i].GetInventory() != Managers.GetManager<PlayersManager>().GetActivePlayerController().GetPlayerBackpack().GetInventory())
                    result.Add(ial[i]);
            }
            result.Sort(delegate (InventoryAssociated a, InventoryAssociated b) { return GetObjectName(a.gameObject).CompareTo(GetObjectName(b.gameObject)); });
            return result;
        }
    }
}
