﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Delete
{
    [BepInPlugin("aedenthorn.Delete", "Delete", "0.5.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> isDebug;
        private static ConfigEntry<string> delKey;

        private static InputAction actionDel;
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

        [HarmonyPatch(typeof(PlayerInputDispatcher), "Update")]
        static class PlayerInputDispatcher_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                if (actionDel.WasPressedThisFrame() && Managers.GetManager<PlayersManager>())
                {
                    Dbgl("Pressed delete key");
                    var aimC = Managers.GetManager<PlayersManager>().GetActivePlayerController().GetAimController();
                    RaycastHit raycastHit;
                    if (Physics.Raycast(aimC.GetAimRay(), out raycastHit, AccessTools.FieldRefAccess<PlayerAimController, float>(aimC, "_distanceHitLimit"), AccessTools.FieldRefAccess<PlayerAimController, int>(aimC, "_layerMask")))
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
                                Dbgl($"Destroying world object {t.name}");
                                WorldObjectsHandler.Instance.DestroyWorldObject(t.GetComponent<WorldObjectAssociated>().GetWorldObject());
                                Destroy(t.gameObject);
                                Managers.GetManager<DisplayersHandler>().GetItemWorldDisplayer().Hide();
                                return;
                            }
                            t = t.parent;
                        }
                        Dbgl($"Destroying {raycastHit.transform.name}");
                        Destroy(raycastHit.transform.gameObject);
                        Managers.GetManager<DisplayersHandler>().GetItemWorldDisplayer().Hide();
                    }
                }
            }
        }
    }
}
