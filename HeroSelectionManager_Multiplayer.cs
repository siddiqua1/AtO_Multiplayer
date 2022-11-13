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

class HeroSelectionManager_Multiplayer : MonoBehaviour
{
    //private readonly Harmony harmony = new Harmony(modInfo.modGUID);

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(BeginAdventure));
        modInfo.harmony.PatchAll(typeof(DrawBoxSelectionNames));
        modInfo.harmony.PatchAll(typeof(AllBoxWithHeroes));
        modInfo.harmony.PatchAll(typeof(AddBoxes));
    }


    [HarmonyPatch(typeof(HeroSelectionManager), "Awake")]
    class AddBoxes
    {
        [HarmonyPostfix]
        static void setpatch(ref GameObject[] ___boxGO)
        {
            float spacer = -2.4f;
            Transform boxChar = GameObject.Find("/BoxCharacters").transform;

            Array.Resize<GameObject>(ref ___boxGO, 8);

            System.Console.WriteLine($"Length of boxGO is {___boxGO.Length}");

            for (int i = 0; i < 4; i++)
            {
                //TODO: before duplicating, update the boxPlayer array under boxSelection, should be moved down .25 
                GameObject original = boxChar.GetChild(4 + i).gameObject;
                BoxSelection select = original.GetComponent<BoxSelection>();
                Array.Resize<BoxPlayer>(ref select.boxPlayer, 8);
                for (int j = 0; j < 4; j++)
                {
                    GameObject selectTmp = Instantiate(original.transform.GetChild(6).transform.GetChild(5).gameObject);
                    selectTmp.transform.SetParent(original.transform.GetChild(6).transform, true);
                    selectTmp.name = $"Box Player ({4 + j})";
                    selectTmp.transform.localScale = original.transform.GetChild(6).transform.GetChild(5).transform.localScale;
                    selectTmp.transform.position = original.transform.GetChild(6).transform.GetChild(5).transform.position + new Vector3(0, -0.25f * (j + 1), 0);
                    select.boxPlayer[4 + j] = selectTmp.GetComponent<BoxPlayer>();
                    selectTmp.gameObject.SetActive(false);
                }


                System.Console.WriteLine($"Box {i} has up to {select.boxPlayer.Length} selections possible");
                GameObject tmpBox = Instantiate(original);
                tmpBox.name = $"Box_{4 + i}";
                tmpBox.transform.SetParent(boxChar.transform, true);
                tmpBox.transform.position = boxChar.GetChild(4 + i).position + new Vector3(0, spacer, 0);
                tmpBox.gameObject.SetActive(false);
                tmpBox.gameObject.SetActive(true);
                ___boxGO[4 + i] = tmpBox;
            }
        }
    }

    [HarmonyPatch(typeof(HeroSelectionManager), "AllBoxWithHeroes")]
    class AllBoxWithHeroes
    {
        [HarmonyPrefix]
        static bool setpatch(ref bool __result, Dictionary<GameObject, bool> ___boxFilled)
        {
            //gives back number of portraits
            int num = 0;
            if (___boxFilled.Count > 0)
            {
                foreach (GameObject gameObject in ___boxFilled.Keys)
                {
                    if (___boxFilled[gameObject])
                    {
                        num++;
                    }
                }
                //return num == 4;
            }
            //four childs are static, so if childs - 4 = 2 * portraits we have them all filled
            int childCnt = GameObject.Find("/BoxCharacters").transform.childCount;
            __result = (2 * num == childCnt - 4);
            return false;
        }
    }

    [HarmonyPatch(typeof(HeroSelectionManager), "DrawBoxSelectionNames")]
    class DrawBoxSelectionNames
    {
        [HarmonyPrefix]
        static bool setpatch(ref BoxSelection[] ___boxSelection)
        {
            int num = 0;
            System.Console.WriteLine($"Accessing boxSelection of length {___boxSelection.Length}");
            foreach (Player player in NetworkManager.Instance.PlayerList)
            {
                System.Console.WriteLine($"Adding player {player.NickName} to all boxSelections");
                for (int j = 0; j < ___boxSelection.Length; j++)
                {
                    ___boxSelection[j].ShowPlayer(num);
                    ___boxSelection[j].SetPlayerPosition(num, player.NickName);
                }
                num++;
            }
            for (int k = num; k < (int)PhotonNetwork.CurrentRoom.MaxPlayers; k++)
            {
                for (int l = 0; l < ___boxSelection.Length; l++)
                {
                    ___boxSelection[l].SetPlayerPosition(k, "");
                }
            }
            foreach (Player player2 in NetworkManager.Instance.PlayerList)
            {
                string playerNickReal = NetworkManager.Instance.GetPlayerNickReal(player2.NickName);
                if (playerNickReal == NetworkManager.Instance.Owner0)
                {
                    HeroSelectionManager.Instance.AssignPlayerToBox(player2.NickName, 0);
                }
                if (playerNickReal == NetworkManager.Instance.Owner1)
                {
                    HeroSelectionManager.Instance.AssignPlayerToBox(player2.NickName, 1);
                }
                if (playerNickReal == NetworkManager.Instance.Owner2)
                {
                    HeroSelectionManager.Instance.AssignPlayerToBox(player2.NickName, 2);
                }
                if (playerNickReal == NetworkManager.Instance.Owner3)
                {
                    HeroSelectionManager.Instance.AssignPlayerToBox(player2.NickName, 3);
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(HeroSelectionManager), "BeginAdventure")]
    class BeginAdventure
    {
        [HarmonyPrefix]
        static bool setpatch(
            ref BotonGeneric ___botonBegin,
            ref Dictionary<GameObject, HeroSelection> ___boxHero,
            ref GameObject[] ___boxGO,
            ref Dictionary<string, List<string>> ___playerHeroPerksDict,
            ref int ___ngValue,
            ref string ___ngCorruptors,
            ref int ___obeliskMadnessValue
            )
        {
            ___botonBegin.gameObject.SetActive(false);
            if (!GameManager.Instance.IsMultiplayer() || (GameManager.Instance.IsMultiplayer() && NetworkManager.Instance.IsMaster()))
            {
                if (GameManager.Instance.GameStatus == Enums.GameStatus.LoadGame)
                {
                    AtOManager.Instance.DoLoadGameFromMP();
                    return false;
                }
                string[] array = new string[8];
                for (int i = 0; i < ___boxHero.Count; i++)
                {
                    array[i] = ___boxHero[___boxGO[i]].GetSubclassName();
                }
                if (!GameManager.Instance.IsMultiplayer() && !GameManager.Instance.IsWeeklyChallenge())
                {
                    PlayerManager.Instance.LastUsedTeam = new string[4];
                    for (int j = 0; j < 4; j++)
                    {
                        PlayerManager.Instance.LastUsedTeam[j] = array[j].ToLower();
                    }
                    //SaveManager.SavePlayerData(false);
                }
                if (!GameManager.Instance.IsObeliskChallenge())
                {
                    AtOManager.Instance.SetPlayerPerks(___playerHeroPerksDict, array);
                    AtOManager.Instance.SetNgPlus(___ngValue);
                    AtOManager.Instance.SetMadnessCorruptors(___ngCorruptors);
                }
                else if (!GameManager.Instance.IsWeeklyChallenge())
                {
                    AtOManager.Instance.SetObeliskMadness(___obeliskMadnessValue);
                }
                AtOManager.Instance.SetTeamFromArray(array);
                AtOManager.Instance.BeginAdventure();
            }
            return false;
        }
    }

}