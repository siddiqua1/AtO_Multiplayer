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

class SteamManager_Multiplayer 
{
    //private readonly Harmony harmony = new Harmony(modInfo.modGUID);

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(InviteSteam));
    }

    [HarmonyPatch(typeof(SteamManager), "InviteSteam")]
    class InviteSteam
    {
        [HarmonyPrefix]
        static bool setpatch()
        {
            SteamMatchmaking.CreateLobbyAsync(8);
            return false;
        }
    }
}