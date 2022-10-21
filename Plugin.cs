using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using Steamworks.Data;
using Image = UnityEngine.UI.Image;

namespace AtO_Multiplayer;

[BepInPlugin(modGUID, modName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string modGUID = "Kazoo.Multiplayer";
    private const string modName = "AtO_Multiplayer";
    private const string ModVersion = "0.0.0.1";
    private readonly Harmony harmony = new Harmony(modGUID);

    private void Awake()
    {
        harmony.PatchAll(typeof(Dropdown));
        harmony.PatchAll(typeof(LobbyNames));
        harmony.PatchAll(typeof(SteamLobby));
        harmony.PatchAll(typeof(AddBoxes));
    }

    //AllBoxWithHeroes

    [HarmonyPatch(typeof(HeroSelectionManager), "Awake")]
    class AddBoxes
    {
        [HarmonyPostfix]
        static void setpatch()
        {
            float spacer = -2.4f;
            Transform boxChar = GameObject.Find("/BoxCharacters").transform;
            for (int i = 0; i < 4; i++)
            {
                GameObject tmpBox = Instantiate(boxChar.GetChild(4 + i).gameObject);
                tmpBox.name = $"Box_{4 + i}";
                tmpBox.transform.parent = boxChar;
                tmpBox.transform.position = boxChar.GetChild(4 + i).position + new Vector3(0, spacer, 0);
                tmpBox.gameObject.SetActive(false);
                tmpBox.gameObject.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(SteamManager), "InviteSteam")]
    class SteamLobby
    {
        [HarmonyPrefix]
        static bool setpatch()
        {
            SteamMatchmaking.CreateLobbyAsync(8);
            return false;
        }
    }

    [HarmonyPatch(typeof(LobbyManager), "ShowCreate")]
    class Dropdown 
    {
        [HarmonyPrefix]
        static void setpatch(ref TMP_Dropdown ___UICreatePlayers) 
        {
            ___UICreatePlayers.options.Clear();
            //Add options 2, 3, 4, 5, 6, 7, 8

            for (int i = 2; i < 9; i++) {
                ___UICreatePlayers.options.Add(new TMP_Dropdown.OptionData() { text = i.ToString() });
            }
        }
    }
    [HarmonyPatch(typeof(LobbyManager), "DrawLobbyNames")]
    class LobbyNames
    {
        [HarmonyPrefix]
        static void setpatch(ref TMP_Text[] ___roomSlots, ref Image[] ___roomSlotsImage, ref Transform[]___roomSlotsKick, ref Transform ___buttonLaunch, ref Transform ___buttonSteam)
        {
            float spacer = -0.8f;
            for (int i = 0; i < ___roomSlots.Length; i++) { 
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