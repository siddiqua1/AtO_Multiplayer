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

class LobbyManager_Multiplayer : MonoBehaviour{
    //private readonly Harmony harmony = new Harmony(modInfo.modGUID);

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(ShowCreate));
        modInfo.harmony.PatchAll(typeof(DrawLobbyNames));
    }

    [HarmonyPatch(typeof(LobbyManager), "ShowCreate")]
    class ShowCreate
    {
        [HarmonyPrefix]
        static void setpatch(ref TMP_Dropdown ___UICreatePlayers)
        {
            ___UICreatePlayers.options.Clear();
            //Add options 2, 3, 4, 5, 6, 7, 8

            for (int i = 2; i < 9; i++)
            {
                ___UICreatePlayers.options.Add(new TMP_Dropdown.OptionData() { text = i.ToString() });
            }
        }
    }

    [HarmonyPatch(typeof(LobbyManager), "DrawLobbyNames")]
    class DrawLobbyNames
    {

        [HarmonyPostfix]
        static void forceRoom(ref Transform ___buttonLaunch)
        {
            ___buttonLaunch.gameObject.SetActive(true);
        }

        [HarmonyPrefix]
        static void setpatch(ref TMP_Text[] ___roomSlots, ref Image[] ___roomSlotsImage, ref Transform[] ___roomSlotsKick, ref Transform ___buttonLaunch, ref Transform ___buttonSteam)
        {
            float spacer = -0.8f;
            for (int i = 0; i < ___roomSlots.Length; i++)
            {
                System.Console.WriteLine($"Object {i} has position <{___roomSlots[i].gameObject.transform.position.x}, {___roomSlots[i].gameObject.transform.position.y}, {___roomSlots[i].gameObject.transform.position.z}>");
            }
            System.Console.WriteLine($"Button Launch has position <{___buttonLaunch.gameObject.transform.position.x}, {___buttonLaunch.gameObject.transform.position.y}, {___buttonLaunch.gameObject.transform.position.z}>");
            System.Console.WriteLine($"Button Steam has position <{___buttonSteam.gameObject.transform.position.x}, {___buttonSteam.gameObject.transform.position.y}, {___buttonSteam.gameObject.transform.position.z}>");
            if (___roomSlots.Length == 4)
            {
                Array.Resize<TMP_Text>(ref ___roomSlots, 8);
                Array.Resize<Image>(ref ___roomSlotsImage, 8);
                Array.Resize<Transform>(ref ___roomSlotsKick, 8);

                ___buttonLaunch.transform.position += new Vector3(0, spacer * 4, 0);
                ___buttonSteam.transform.position += new Vector3(0, spacer * 4, 0);

                for (int i = 0; i < 4; i++)
                {
                    GameObject tmp = Instantiate(___roomSlots[3].gameObject.transform.parent.gameObject);
                    tmp.gameObject.name = $"player{5 + i}";
                    tmp.gameObject.transform.parent = ___roomSlots[3].gameObject.transform.parent.transform.parent;
                    tmp.transform.position = ___roomSlots[3].gameObject.transform.position;
                    tmp.transform.position += new Vector3(0, spacer * (i + 1), 0);
                    tmp.transform.localScale = new Vector3(1, 1, 1);

                    TMP_Text tmpT = tmp.transform.GetChild(0).GetComponent<TMP_Text>();
                    tmpT.gameObject.name = $"player{5 + i}T";
                    tmpT.transform.position = ___roomSlots[3].gameObject.transform.position + new Vector3(0, spacer * (i + 1), 0);

                    Transform tmpKick = tmpT.transform.GetChild(0).GetComponent<Transform>();
                    tmpKick.gameObject.name = $"Kick{5 + i}";
                    tmpKick.transform.position = ___roomSlotsKick[3].gameObject.transform.position + new Vector3(0, spacer * (i + 1), 0);

                    Image tmpImg = tmpT.transform.GetChild(2).GetComponent<Image>();
                    tmpImg.gameObject.name = $"Image ({5 + i})";
                    tmpImg.transform.position = ___roomSlotsImage[3].gameObject.transform.position + new Vector3(0, spacer * (i + 1), 0);

                    ___roomSlots[4 + i] = tmpT;
                    ___roomSlotsKick[4 + i] = tmpKick;
                    ___roomSlotsImage[4 + i] = tmpImg;
                }
            }
        }
    }
}
  