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


class Template_Multiplayer : MonoBehaviour
{

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(Awake));
    }

    [HarmonyPatch(typeof(modInfo), "Awake")]
    class Awake
    {
        [HarmonyPrefix]
        static void setpatch(modInfo __instance)
        {
            //do prefix stuff
        }
    }
}
