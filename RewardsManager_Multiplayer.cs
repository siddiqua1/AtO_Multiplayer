using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection.Emit;
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
using Object = UnityEngine.Object;

namespace AtO_Multiplayer;

class RewardsManager_Multiplayer : MonoBehaviour
{
    //private readonly Harmony harmony = new Harmony(modInfo.modGUID);

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(ShowRewardsCo));
        modInfo.harmony.PatchAll(typeof(Awake));
        modInfo.harmony.PatchAll(typeof(Start));
        modInfo.harmony.PatchAll(typeof(SetCardReward));
        //modInfo.harmony.PatchAll(typeof(CheckAllAssigned));
    }

    //[HarmonyPatch(typeof(RewardsManager), "CheckAllAssigned")]
    //class CheckAllAssigned : MonoBehaviour {
    //    [HarmonyPrefix]
    //    bool setpatch(RewardsManager __instance) {
    //        for (int i = 0; i < __instance.numRewards; i++)
    //        {
    //            if (__instance.cardSelectedArr[i] == null)
    //            {
    //                return false;
    //            }
    //        }
    //        if (!GameManager.Instance.IsMultiplayer() || (GameManager.Instance.IsMultiplayer() && NetworkManager.Instance.IsMaster()))
    //        {
    //            __instance.finishReward = true;
    //            __instance.buttonRestart.gameObject.SetActive(false);
    //            base.StartCoroutine(__instance.CloseWindow());
    //        }
    //        SaveManager.SavePlayerData(false);
    //        return false;
    //    }
    //}

    [HarmonyPatch(typeof(RewardsManager), "SetCardReward")]
    class SetCardReward {
        [HarmonyPrefix]
        static bool setpatch(RewardsManager __instance, ref string playerNick,ref string internalId) {
            for (int i = 0; i < __instance.characterRewardArray.Length; i++)
            {
                __instance.characterRewardArray[i].GetComponent<CharacterReward>().CardSelected(playerNick, internalId);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(RewardsManager), "Start")]
    class Start
    {
        [HarmonyPostfix]
        static void setpatch(RewardsManager __instance) {
            __instance.cardSelectedArr = new string[8];
        }
    }

    [HarmonyPatch(typeof(RewardsManager), "Awake")]
    class Awake {

        [HarmonyPrefix]
        static void setpatch(RewardsManager __instance) {
            if (__instance.characterRewardArray.Length == 4) {
                System.Console.WriteLine($"[RewardsManager Awake] Increasing the number of rewards avaliable per player");
                //update to 8
                Array.Resize<Transform>(ref __instance.characterRewardArray, 8);
                Vector3 offsetRight = new Vector3(4.5f, 0, 0);
                Vector3 offsetLeft = new Vector3(-4f, 0, 0);
                Vector3 newScale = new Vector3(0.7f, 0.7f, 0.7f);
                for (int i = 4; i < 8; i++) {
                    Transform tmp = Instantiate(__instance.characterRewardArray[i - 4]);
                    tmp.SetParent(__instance.characterRewardArray[i - 4].parent, true);
                    tmp.position += offsetRight;
                    tmp.localScale = newScale;
                    __instance.characterRewardArray[i - 4].position += offsetLeft;
                    __instance.characterRewardArray[i - 4].localScale = newScale;
                    tmp.name = $"CharacterReward ({i})";
                    tmp.gameObject.SetActive(false);
                    __instance.characterRewardArray[i] = tmp;
                }
            }
        }
    }


    [HarmonyPatch(typeof(RewardsManager), "ShowRewardsCo")]
    class ShowRewardsCo {
        [HarmonyPostfix]
        static void setpatch(RewardsManager __instance)
        {
            System.Console.WriteLine($"[ShowRewardsCo] Postfix starting");
            for (int i = 4; i < 8; i++)
            {
                if (__instance.theTeam[i] != null && !(__instance.theTeam[i].HeroData == null))
                {
                    __instance.characterRewardArray[i].gameObject.SetActive(true);
                    __instance.characterRewardArray[i].GetComponent<CharacterReward>().Init(i);
                    __instance.numRewards++;
                }
            }
        }
    }
}