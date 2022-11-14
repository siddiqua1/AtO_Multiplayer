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


class CharacterWindowUI_Multiplayer : MonoBehaviour
{

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(Resize));
    }

    [HarmonyPatch(typeof(CharacterWindowUI), "Resize")]
    class Resize
    {
        [HarmonyPrefix]
        static bool setpatch(CharacterWindowUI __instance)
        {
            __instance.GetComponent<RectTransform>().sizeDelta = new Vector2(Globals.Instance.sizeW, Globals.Instance.sizeH);
            __instance.exitButton.transform.localPosition = new Vector3(Globals.Instance.sizeW * 0.37f + 1f * Globals.Instance.multiplierX, -Globals.Instance.sizeH * 0.5f + 3.9f * Globals.Instance.multiplierY, __instance.exitButton.transform.localPosition.z);

            return false;
        }
    }
}
