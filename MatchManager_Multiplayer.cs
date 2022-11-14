using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Text;
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


class MatchManager_Multiplayer{
    //private readonly Harmony harmony = new Harmony(modInfo.modGUID);

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(InitializeVars));
        modInfo.harmony.PatchAll(typeof(GenerateDecks));
        modInfo.harmony.PatchAll(typeof(GenerateHeroes));
        //seperate file for CastCards and CardCardAction

        //TODO: below are methods that have < 4 and seem relevant to change
        //modInfo.harmony.PatchAll(typeof(CardNamesForSyncCode));
        //FixCodeSyncFromMaster
        //FixTOTALCo
        //GetHero
        //GetHeroFromId
        //GetHeroItemsForTurnSave
        //NET_ShareDecks
        //NextTurnContinue
        //PositionIsMiddle
        //ReloadCombatFullAction
        //SetCharactersPing

    }

    [HarmonyPatch(typeof(MatchManager), "CardNamesForSyncCode")]
    class CardNamesForSyncCode {
        [HarmonyPrefix]
        static bool setpatch(MatchManager __instance,ref string __result)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(__instance.CardDictionaryKeys());
            for (int i = 0; i < __instance.TeamHero.Length; i++)
            {
                if (__instance.HeroDeck[i] != null)
                {
                    for (int j = 0; j < __instance.HeroDeck[i].Count; j++)
                    {
                        stringBuilder.Append(__instance.HeroDeck[i][j]);
                    }
                }
                if (__instance.HeroDeckDiscard[i] != null)
                {
                    for (int k = 0; k < __instance.HeroDeckDiscard[i].Count; k++)
                    {
                        stringBuilder.Append(__instance.HeroDeckDiscard[i][k]);
                    }
                }
                if (__instance.HeroDeckVanish[i] != null)
                {
                    for (int l = 0; l < __instance.HeroDeckVanish[i].Count; l++)
                    {
                        stringBuilder.Append(__instance.HeroDeckVanish[i][l]);
                    }
                }
            }
            __result = stringBuilder.ToString();
            return false;
        }
    }

    [HarmonyPatch(typeof(MatchManager), "InitializeVars")]
    class InitializeVars
    {
        [HarmonyPrefix]
        static bool setup(MatchManager __instance)
        {
            Debug.Log("Initialize Variables");
            int betterNum = 8;
            __instance.reloadingGame = false;
            __instance.heroIndexWaitingForAddDiscard = -1;
            __instance.HeroDeck = new List<string>[betterNum];
            __instance.HeroDeckDiscard = new List<string>[betterNum];
            __instance.HeroDeckVanish = new List<string>[betterNum];
            __instance.HeroHand = new List<string>[betterNum];
            __instance.NPCDeck = new List<string>[betterNum];
            __instance.NPCDeckDiscard = new List<string>[betterNum];
            __instance.NPCHand = new List<string>[betterNum];
            __instance.cardDictionary = new Dictionary<string, CardData>();
            __instance.castedCards = new List<string>();
            __instance.castedCards.Add("");
            __instance.CICardDiscard = new List<CardItem>();
            __instance.CICardAddcard = new List<CardItem>();
            __instance.npcCardsCasted = new Dictionary<string, List<string>>();
            __instance.canInstaCastDict = new Dictionary<string, bool>();
            if (!__instance.turnLoadedBySave)
            {
                AtOManager.Instance.InitCombatStatsCurrent();
            }
            for (int i = 0; i < betterNum; i++)
            {
                __instance.HeroDeck[i] = new List<string>();
            }
            for (int j = 0; j < betterNum; j++)
            {
                __instance.NPCDeck[j] = new List<string>();
            }
            __instance.itemTimeout = new float[10];
            for (int k = 0; k < __instance.itemTimeout.Length; k++)
            {
                __instance.itemTimeout[k] = 0f;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MatchManager), "GenerateDecks")]
    class GenerateDecks
    {
        [HarmonyPrefix]
        static bool setpatch(MatchManager __instance)
        {
            List<string>[] array = new List<string>[8];
            for (int i = 0; i < __instance.TeamHero.Length; i++)
            {
                System.Console.WriteLine($"[ATO GenerateDecks] Iteration {i + 1}");
                if (__instance.TeamHero[i] != null)
                {
                    array[i] = new List<string>();
                    List<string> list = __instance.TeamHero[i].Cards;
                    if (__instance.tutorialCombat)
                    {
                        if (i == 0)
                        {
                            list = new List<string>();
                            list.Add("fastStrike");
                            list.Add("defend");
                            list.Add("rend");
                            list.Add("intercept");
                            list.Add("intercept");
                        }
                        else if (i == 3)
                        {
                            list = new List<string>();
                            list.Add("heal");
                            list.Add("heal");
                            list.Add("heal");
                            list.Add("flash");
                            list.Add("foresight");
                        }
                    }
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (!(Globals.Instance.GetCardData(list[j], false) == null))
                        {
                            string text = __instance.CreateCardInDictionary(list[j], "", false);
                            array[i].Add(text);
                        }
                    }
                }
            }
            System.Console.WriteLine($"[ATO GenerateDecks] Vibe check");
            for (int k = 0; k < __instance.TeamHero.Length; k++)
            {
                System.Console.WriteLine($"[ATO GenerateDecks] Iteration {k + 1}");
                if (__instance.TeamHero[k] != null)
                {
                    List<string> list2 = array[k].ShuffleList<string>();
                    __instance.HeroDeck[k] = list2;
                    if (__instance.currentRound == 0)
                    {
                        List<string> list3 = new List<string>();
                        List<string> list4 = new List<string>();
                        for (int l = __instance.HeroDeck[k].Count - 1; l >= 0; l--)
                        {
                            CardData cardData = __instance.GetCardData(__instance.HeroDeck[k][l]);
                            if (cardData.Innate)
                            {
                                list3.Add(__instance.HeroDeck[k][l]);
                                __instance.HeroDeck[k].RemoveAt(l);
                            }
                            else if (cardData.Lazy)
                            {
                                list4.Add(__instance.HeroDeck[k][l]);
                                __instance.HeroDeck[k].RemoveAt(l);
                            }
                        }
                        if (list3.Count > 0)
                        {
                            list3 = list3.ShuffleList<string>();
                            list3.AddRange(__instance.HeroDeck[k]);
                            __instance.HeroDeck[k] = new List<string>();
                            __instance.HeroDeck[k].Clear();
                            for (int m = 0; m < list3.Count; m++)
                            {
                                __instance.HeroDeck[k].Add(list3[m]);
                            }
                        }
                        if (list4.Count > 0)
                        {
                            list4 = list4.ShuffleList<string>();
                            for (int n = 0; n < list4.Count; n++)
                            {
                                __instance.HeroDeck[k].Add(list4[n]);
                            }
                        }
                    }
                }
            }
            return false;
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
            System.Console.WriteLine("[GenerateHeroes] Updating each Hero's owner");
            for (int i = 0; i < 8; i++)
            {
                if (___TeamHero[i] != null)
                {
                    ___TeamHero[i].AssignOwner(NetworkManager.Instance.PlayerHeroPositionOwner[i]);
                }
                if (___TeamHero[i].HpCurrent <= 0)
                {
                    ___TeamHero[i].HpCurrent = 1;
                }
                if (AtOManager.Instance.heroPerks != null && AtOManager.Instance.heroPerks.ContainsKey(___TeamHero[i].SubclassName))
                {
                    ___TeamHero[i].PerkList = AtOManager.Instance.heroPerks[___TeamHero[i].SubclassName];
                }
            }

            Array.Resize<int>(ref ___heroLifeArr, 8);
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
                    //System.Console.WriteLine($"[ATO GenerateHeroes] FLAG 1");
                    if (AtOManager.Instance.combatGameCode == "" || ___teamHeroItemsFromTurnSave != null)
                    {
                        ___heroLifeArr[i] = hero.HpCurrent;
                        List<string> list = new List<string>();
                        list.Add(hero.Weapon);
                        list.Add(hero.Armor);
                        list.Add(hero.Jewelry);
                        list.Add(hero.Accesory);
                        list.Add(hero.Pet);
                        //System.Console.WriteLine($"[ATO GenerateHeroes] VIBE CHECK");
                        if (!___heroBeginItems.ContainsKey(i))
                        {
                            //System.Console.WriteLine($"[ATO GenerateHeroes] ADD CHECK");
                            ___heroBeginItems.Add(i, list);
                            //System.Console.WriteLine($"[ATO GenerateHeroes] ADD POG");
                        }
                        else
                        {
                            //System.Console.WriteLine($"[ATO GenerateHeroes] ACCESS CHECK");
                            ___heroBeginItems[i] = list;
                            //System.Console.WriteLine($"[ATO GenerateHeroes] ACCESS POG");
                        }
                    }
                    //System.Console.WriteLine($"[ATO GenerateHeroes] FLAG 2");
                    if (AtOManager.Instance.combatGameCode != "")
                    {
                        if (___teamHeroItemsFromTurnSave != null)
                        {
                            //System.Console.WriteLine($"[ATO GenerateHeroes] FLAG 3");
                            hero.Weapon = ___teamHeroItemsFromTurnSave[i * 5];
                            hero.Armor = ___teamHeroItemsFromTurnSave[i * 5 + 1];
                            hero.Jewelry = ___teamHeroItemsFromTurnSave[i * 5 + 2];
                            hero.Accesory = ___teamHeroItemsFromTurnSave[i * 5 + 3];
                            hero.Pet = ___teamHeroItemsFromTurnSave[i * 5 + 4];
                        }
                        else if (___currentRound == 0 && ___heroBeginItems != null && ___heroBeginItems.ContainsKey(i) && ___heroBeginItems[i] != null)
                        {
                            //System.Console.WriteLine($"[ATO GenerateHeroes] FLAG 4");
                            List<string> list2 = ___heroBeginItems[i];
                            hero.Weapon = list2[0];
                            hero.Armor = list2[1];
                            hero.Jewelry = list2[2];
                            hero.Accesory = list2[3];
                            hero.Pet = list2[4];
                        }
                        else if (___currentRound > 0 && ___heroDestroyedItemsInThisTurn.ContainsKey(i))
                        {
                            //System.Console.WriteLine($"[ATO GenerateHeroes] FLAG 5");
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
                    //System.Console.WriteLine($"[ATO GenerateHeroes] FLAG 6");
                    hero.Alive = true;
                    hero.InternalId = MatchManager.Instance.GetRandomString("default");
                    hero.Id = hero.HeroData.HeroSubClass.Id + "_" + hero.InternalId;
                    hero.Position = num;
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(___heroPrefab, Vector3.zero, Quaternion.identity, __instance.GO_Heroes.transform);
                    gameObject.name = hero.Id;
                    ___targetTransformDict.Add(hero.Id, gameObject.transform);
                    hero.ResetDataForNewCombat(___currentGameCode == "");
                    //System.Console.WriteLine($"[ATO GenerateHeroes] FLAG 7");
                    hero.SetHeroIndex(i);
                    //System.Console.WriteLine($"[ATO GenerateHeroes] vibes");
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
                    //System.Console.WriteLine($"[ATO GenerateHeroes] FLAG 8");
                    array[i] = hero;
                    num++;
                    CardData pet = hero.GetPet();
                    if (pet != null)
                    {
                        MatchManager.Instance.CreatePet(pet, gameObject, hero);
                    }
                }
            }
            System.Console.WriteLine($"[ATO GenerateHeroes] Length of teamHero: {___TeamHero.Length}");
            ___TeamHero = new Hero[array.Length];
            for (int j = 0; j < array.Length; j++)
            {
                ___TeamHero[j] = array[j];
            }
            ___teamHeroItemsFromTurnSave = null;
            return false;
        }
    }

}
