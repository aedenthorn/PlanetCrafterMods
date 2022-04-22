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
    [BepInPlugin("aedenthorn.Delete", "Delete", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<string> delKey;

        private InputAction actionDel;
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
            delKey = Config.Bind<string>("Options", "DeleteKey", "<Keyboard>/delete", "Key to delete spawned items");


            actionDel = new InputAction(binding: delKey.Value);
            actionDel.Enable();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        private void Update()
        {
            if(modEnabled.Value && actionDel.WasPressedThisFrame() && Managers.GetManager<PlayersManager>())
            {
                var aimC = Managers.GetManager<PlayersManager>().GetActivePlayerController().GetAimController();
                RaycastHit raycastHit;
                if (Physics.Raycast(aimC.GetAimRay(), out raycastHit, AccessTools.FieldRefAccess<PlayerAimController, float>(aimC, "distanceHitLimit"), AccessTools.FieldRefAccess<PlayerAimController, int>(aimC, "layerMask")))
                {
                    Dbgl($"raycast hit {raycastHit.transform.name}");

                    var t = raycastHit.transform;
                    while (t)
                    {
                        if (t.GetComponentInChildren<ActionDeconstructible>())
                        {
                            var state = Managers.GetManager<PlayersManager>().GetActivePlayerController().GetMultitool().GetState();
                            Managers.GetManager<PlayersManager>().GetActivePlayerController().GetMultitool().SetState(DataConfig.MultiToolState.Deconstruct);
                            Dbgl($"Deconstructing {raycastHit.transform.name}");
                            t.GetComponentInChildren<ActionDeconstructible>().OnAction();
                            Managers.GetManager<PlayersManager>().GetActivePlayerController().GetMultitool().SetState(state);
                            return;
                        }
                        if (t.GetComponent<WorldObjectAssociated>())
                        {
                            Dbgl($"Destroying parent {t.name}");
                            Destroy(t.gameObject);
                            return;
                        }
                        t = t.parent;
                    }
                    Dbgl($"Destroying {raycastHit.transform.name}");
                    Destroy(raycastHit.transform.gameObject);
                    Managers.GetManager<DisplayersHandler>().GetItemWorldDislpayer().Hide();
                }
            }

        }
    }
}
