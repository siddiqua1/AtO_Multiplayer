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

public class modInfo {
    public const string modGUID = "Kazoo.Multiplayer";
    public const string modName = "AtO_Multiplayer";
    public const string ModVersion = "0.0.0.1";
    public static readonly Harmony harmony = new Harmony(modGUID);
}

[BepInPlugin(modInfo.modGUID, modInfo.modName, modInfo.ModVersion)]
public class Plugin : BaseUnityPlugin
{
 
    private void Awake()
    {
        LobbyManager_Multiplayer.patch();
        MatchManager_Multiplayer.patch();
        AtOManager_Multiplayer.patch();
        NetworkManager_Multiplayer.patch();
        HeroSelectionManager_Multiplayer.patch();
        SteamManager_Multiplayer.patch();
    }
}