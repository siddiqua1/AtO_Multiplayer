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

class NetworkManager_Multiplayer 
{
    //private readonly Harmony harmony = new Harmony(modInfo.modGUID);

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(CreateRoom));
        modInfo.harmony.PatchAll(typeof(AssignHeroPlayerPositionOwner));
    }


    [HarmonyPatch(typeof(NetworkManager), "AssignHeroPlayerPositionOwner")]
    class AssignHeroPlayerPositionOwner
    {
        [HarmonyPrefix]
        static bool setpatch(ref int id, ref string nickName, ref string[] ___PlayerHeroPositionOwner)
        {
            if (___PlayerHeroPositionOwner == null)
            {
                ___PlayerHeroPositionOwner = new string[8];
            }
            ___PlayerHeroPositionOwner[id] = nickName;
            return false;
        }
    }


    [HarmonyPatch(typeof(NetworkManager), "CreateRoom")]
    class CreateRoom
    {
        [HarmonyPostfix]
        static void setpatch(ref string[] ___PlayerHeroPositionOwner)
        {
            ___PlayerHeroPositionOwner = new string[8];
        }
    }

}