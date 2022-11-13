using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using Steamworks.Data;
using Photon.Realtime;
using Photon.Pun;
using Image = UnityEngine.UI.Image;

namespace AtO_Multiplayer;


class AtOManager_Multiplayer 
{
    //private readonly Harmony harmony = new Harmony(modInfo.modGUID);

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(CreateTeam));
        modInfo.harmony.PatchAll(typeof(CreateRoom));
        modInfo.harmony.PatchAll(typeof(ShareTeam));
        modInfo.harmony.PatchAll(typeof(InitCombatStats));
        modInfo.harmony.PatchAll(typeof(GetHero));
    }

    [HarmonyPatch(typeof(AtOManager), "GetHero")]
    class GetHero {
        [HarmonyPrefix]
        static bool setpatch(ref Hero __result, ref AtOManager __instance, ref int index) {
            if (index >= 0 && index < 8 && __instance.teamAtO != null && __instance.teamAtO.Length != 0)
            {
                __result = __instance.teamAtO[index];
                return false;
            }
            __result = null;

            return false;
        }
    }

    [HarmonyPatch(typeof(AtOManager), "InitCombatStats")]
    class InitCombatStats
    {
        [HarmonyPrefix]
        static bool setpatch(AtOManager __instance)
        {
            System.Console.WriteLine("[CombatStats] Update for 8 players");
            __instance.combatStats = new int[8, 12];
            return false;
        }
    }

    [HarmonyPatch(typeof(AtOManager), "ShareTeam")]
    class ShareTeam
    {
        [HarmonyPostfix]
        static void Postfix(AtOManager __instance, string sceneToLoad = "", bool showMask = true, bool setOwners = false)
        {
            System.Console.WriteLine("[ShareTeam] Postfix applied");
            if (NetworkManager.Instance.IsMaster())
            {
                for (int i = 0; i < 8; i++)
                {
                    if (__instance.teamAtO[i] != null)
                    {
                        __instance.teamAtO[i].AssignOwner(NetworkManager.Instance.PlayerHeroPositionOwner[i]);
                    }
                    if (__instance.teamAtO[i].HpCurrent <= 0)
                    {
                        __instance.teamAtO[i].HpCurrent = 1;
                    }
                    if (__instance.heroPerks != null && __instance.heroPerks.ContainsKey(__instance.teamAtO[i].SubclassName))
                    {
                        __instance.teamAtO[i].PerkList = __instance.heroPerks[__instance.teamAtO[i].SubclassName];
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(NetworkManager), "CreateRoom")]
    class CreateRoom
    {
        [HarmonyPostfix]
        static void setpatch(ref string[] ___PlayerHeroPositionOwner) {
            ___PlayerHeroPositionOwner = new string[8];
        }
    }

    [HarmonyPatch(typeof(AtOManager), "CreateTeam")]
    class CreateTeam
    {
        [HarmonyPrefix]
        static bool setpatch(ref Hero[] ___teamAtO)
        {
            ___teamAtO = new Hero[8];
            return false;
        }
    }

}