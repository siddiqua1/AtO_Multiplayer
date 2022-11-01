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
using Photon.Realtime;
using Photon.Pun;
using Image = UnityEngine.UI.Image;

namespace AtO_Multiplayer;

public class potato {
    public int player = 0;
}

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
        harmony.PatchAll(typeof(BoxesWithHeroes));
        harmony.PatchAll(typeof(DrawBoxSelectionNames));
        harmony.PatchAll(typeof(BeginAdventure));
        harmony.PatchAll(typeof(AssignHeroPlayerPositionOwner));
        harmony.PatchAll(typeof(CreateRoom));
        harmony.PatchAll(typeof(CreateTeam));
        //harmony.PatchAll(typeof(CreateTeamNPC));
    }

    [HarmonyPatch(typeof(BoxSelection), "Awake")]
    class AdditionalNames
    {
        static void setpatch() 
        { 
            //TODO:
        }
    }

    [HarmonyPatch(typeof(MatchManager), "GenerateHeroes")]
    class GenerateHeroes
    {
        [HarmonyPrefix]
        static bool setpatch(
                MatchManager __instance,
                ref Hero[] ___TeamHero,
                ref bool ___tutorialCombat,
                ref List<string> ___teamHeroItemsFromTurnSave,
                ref int[] ___heroLifeArr,
                ref Dictionary<int, List<string>> ___heroBeginItems,
                ref int ___currentRound,
                ref Dictionary<int, Dictionary<string, string>> ___heroDestroyedItemsInThisTurn,
                ref GameObject ___heroPrefab,
                ref Dictionary<string, Transform> ___targetTransformDict,
                ref string ___currentGameCode,
                ref List<string>[] ___HeroHand,
                ref List<string>[] ___HeroDeckDiscard,
                ref List<string>[] ___HeroDeckVanish
            ) 
        {
            int num = 0;
            System.Console.WriteLine($"[ATO GenerateHeroes] Length of teamHero: {___TeamHero.Length}");
            Hero[] array = new Hero[___TeamHero.Length];
            for (int i = 0; i < ___TeamHero.Length; i++)
            {
                System.Console.WriteLine($"[ATO GenerateHeroes] Iteration {i}");
                if (___TeamHero[i] != null && (!___tutorialCombat || (i != 1 && i != 2)))
                {
                    Hero hero = ___TeamHero[i];
                    if (hero.HpCurrent <= 0)
                    {
                        hero.HpCurrent = 1;
                    }
                    if (AtOManager.Instance.combatGameCode == "" || ___teamHeroItemsFromTurnSave != null)
                    {
                        ___heroLifeArr[i] = hero.HpCurrent;
                        List<string> list = new List<string>();
                        list.Add(hero.Weapon);
                        list.Add(hero.Armor);
                        list.Add(hero.Jewelry);
                        list.Add(hero.Accesory);
                        list.Add(hero.Pet);
                        if (!___heroBeginItems.ContainsKey(i))
                        {
                            ___heroBeginItems.Add(i, list);
                        }
                        else
                        {
                            ___heroBeginItems[i] = list;
                        }
                    }
                    if (AtOManager.Instance.combatGameCode != "")
                    {
                        if (___teamHeroItemsFromTurnSave != null)
                        {
                            hero.Weapon = ___teamHeroItemsFromTurnSave[i * 5];
                            hero.Armor = ___teamHeroItemsFromTurnSave[i * 5 + 1];
                            hero.Jewelry = ___teamHeroItemsFromTurnSave[i * 5 + 2];
                            hero.Accesory = ___teamHeroItemsFromTurnSave[i * 5 + 3];
                            hero.Pet = ___teamHeroItemsFromTurnSave[i * 5 + 4];
                        }
                        else if (___currentRound == 0 && ___heroBeginItems != null && ___heroBeginItems.ContainsKey(i) && ___heroBeginItems[i] != null)
                        {
                            List<string> list2 = ___heroBeginItems[i];
                            hero.Weapon = list2[0];
                            hero.Armor = list2[1];
                            hero.Jewelry = list2[2];
                            hero.Accesory = list2[3];
                            hero.Pet = list2[4];
                        }
                        else if (___currentRound > 0 && ___heroDestroyedItemsInThisTurn.ContainsKey(i))
                        {
                            if (___heroDestroyedItemsInThisTurn[i].ContainsKey("weapon"))
                            {
                                hero.Weapon = ___heroDestroyedItemsInThisTurn[i]["weapon"];
                            }
                            if (___heroDestroyedItemsInThisTurn[i].ContainsKey("armor"))
                            {
                                hero.Armor = ___heroDestroyedItemsInThisTurn[i]["armor"];
                            }
                            if (___heroDestroyedItemsInThisTurn[i].ContainsKey("jewelry"))
                            {
                                hero.Jewelry = ___heroDestroyedItemsInThisTurn[i]["jewelry"];
                            }
                            if (___heroDestroyedItemsInThisTurn[i].ContainsKey("accesory"))
                            {
                                hero.Accesory = ___heroDestroyedItemsInThisTurn[i]["accesory"];
                            }
                            if (___heroDestroyedItemsInThisTurn[i].ContainsKey("pet"))
                            {
                                hero.Pet = ___heroDestroyedItemsInThisTurn[i]["pet"];
                            }
                        }
                    } 
                    hero.Alive = true;
                    hero.InternalId = MatchManager.Instance.GetRandomString("default");
                    hero.Id = hero.HeroData.HeroSubClass.Id + "_" + hero.InternalId;
                    hero.Position = num;
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(___heroPrefab, Vector3.zero, Quaternion.identity, __instance.GO_Heroes.transform);
                    gameObject.name = hero.Id;
                    ___targetTransformDict.Add(hero.Id, gameObject.transform);
                    hero.ResetDataForNewCombat(___currentGameCode == "");
                    hero.SetHeroIndex(i);
                    hero.HeroItem = gameObject.GetComponent<HeroItem>();
                    hero.HeroItem.HeroData = hero.HeroData;
                    hero.HeroItem.Init(hero);
                    hero.HeroItem.SetPosition(true, -10);
                    if (AtOManager.Instance.CharacterHavePerk(hero.SubclassName, "mainperkmark1a") && !hero.AuracurseImmune.Contains("mark"))
                    {
                        hero.AuracurseImmune.Add("mark");
                    }
                    if (AtOManager.Instance.CharacterHavePerk(hero.SubclassName, "mainperkinspire0c") && !hero.AuracurseImmune.Contains("stress"))
                    {
                        hero.AuracurseImmune.Add("stress");
                    }
                    ___HeroHand[i] = new List<string>();
                    ___HeroDeckDiscard[i] = new List<string>();
                    ___HeroDeckVanish[i] = new List<string>();
                    array[i] = hero;
                    num++;
                    CardData pet = hero.GetPet();
                    if (pet != null)
                    {
                        MatchManager.Instance.CreatePet(pet, gameObject, hero);
                    }
                }
            }
            ___TeamHero = new Hero[array.Length];
            for (int j = 0; j < array.Length; j++)
            {
                ___TeamHero[j] = array[j];
            }
            ___teamHeroItemsFromTurnSave = null;
            return false;
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

    [HarmonyPatch(typeof(AtOManager), "CreateTeamNPC")]
    class CreateTeamNPC
    {
        [HarmonyPrefix]
        static bool setpatch(ref string[] ___teamNPCAtO)
        {
            ___teamNPCAtO = new string[8];
            return false;
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

    [HarmonyPatch(typeof(HeroSelectionManager), "AllBoxWithHeroes")]
    class BoxesWithHeroes
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

        [HarmonyPostfix]
        static void forceRoom(ref Transform ___buttonLaunch) {
            ___buttonLaunch.gameObject.SetActive(true);
        }

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