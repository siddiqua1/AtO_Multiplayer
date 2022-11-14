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


class AtOManager_Multiplayer 
{
    //private readonly Harmony harmony = new Harmony(modInfo.modGUID);

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(CreateTeam));
        modInfo.harmony.PatchAll(typeof(CreateRoom));
        modInfo.harmony.PatchAll(typeof(ShareTeam));
        modInfo.harmony.PatchAll(typeof(InitCombatStats));
        modInfo.harmony.PatchAll(typeof(GetHero));
        modInfo.harmony.PatchAll(typeof(FinishCardRewards));
        modInfo.harmony.PatchAll(typeof(BeginAdventure));
        modInfo.harmony.PatchAll(typeof(SetPlayerPerks));
    }

    [HarmonyPatch(typeof(AtOManager), "SetPlayerPerks")]
    class SetPlayerPerks {
        [HarmonyPrefix]
        static bool setpatch(AtOManager __instance, Dictionary<string, List<string>> _playerHeroPerks, string[] teamString) {
            __instance.heroPerks = new Dictionary<string, List<string>>();
            if (!GameManager.Instance.IsMultiplayer())
            {
                for (int i = 0; i < teamString.Length; i++)
                {
                    string text = teamString[i].ToLower();
                    List<string> list = PlayerManager.Instance.GetHeroPerks(text, true);
                    __instance.heroPerks.Add(text, list);
                }
                return false;
            }
            for (int j = 0; j < teamString.Length; j++)
            {
                string text2 = teamString[j].ToLower();
                string text3 = (NetworkManager.Instance.PlayerHeroPositionOwner[j] + "_" + text2).ToLower();
                if (_playerHeroPerks.ContainsKey(text3))
                {
                    __instance.heroPerks.Add(text2, _playerHeroPerks[text3]);
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(AtOManager), "BeginAdventure")]
    class BeginAdventure {
        [HarmonyPrefix]
        static bool setpatch(AtOManager __instance) {
            Debug.Log("AtO Begin Adventure");
            if (__instance.teamAtO == null || __instance.teamAtO.Length == 0)
            {
                __instance.CreateTeam();
            }
            if (__instance.teamAtO[0] == null)
            {
                __instance.SetTeamSingle(GameManager.Instance.GameHeroes["mercenary"], 0);
            }
            if (__instance.teamAtO[1] == null)
            {
                __instance.SetTeamSingle(GameManager.Instance.GameHeroes["ranger"], 1);
            }
            if (__instance.teamAtO[2] == null)
            {
                __instance.SetTeamSingle(GameManager.Instance.GameHeroes["elementalist"], 2);
            }
            if (__instance.teamAtO[3] == null)
            {
                __instance.SetTeamSingle(GameManager.Instance.GameHeroes["cleric"], 3);
            }
            if (GameManager.Instance.IsObeliskChallenge())
            {
                __instance.ngPlus = 0;
                __instance.madnessCorruptors = "";
            }
            else
            {
                __instance.obeliskMadness = 0;
            }
            for (int i = 0; i < __instance.teamAtO.Length; i++)
            {
                if (__instance.teamAtO[i] != null && __instance.teamAtO[i].HeroData != null && __instance.teamAtO[i].HeroData.HeroSubClass != null)
                {
                    for (int j = 0; j < __instance.teamAtO[i].HeroData.HeroSubClass.Traits.Count; j++)
                    {
                        System.Console.WriteLine($"[BeginAdventure] HeroName: {__instance.teamAtO[i].SubclassName}");
                        System.Console.WriteLine($"[BeginAdventure] Assign Trait: {__instance.teamAtO[i].HeroData.HeroSubClass.Traits[j].Id}");
                        if (__instance.teamAtO[i].HeroData.HeroSubClass.Traits[j] != null)
                        {
                            __instance.teamAtO[i].AssignTrait(__instance.teamAtO[i].HeroData.HeroSubClass.Traits[j].Id);
                        }
                    }
                    if (!GameManager.Instance.IsObeliskChallenge())
                    {
                        __instance.teamAtO[i].ReassignInitialItem();
                    }
                }
            }
            if (!GameManager.Instance.IsObeliskChallenge())
            {
                if (!GameManager.Instance.IsMultiplayer() || NetworkManager.Instance.IsMaster())
                {
                    __instance.AssignGlobalEventRequirements();
                    if (__instance.ngPlus >= 7)
                    {
                        __instance.SetGameId("");
                    }
                }
                if (GameManager.Instance.IsMultiplayer() && NetworkManager.Instance.IsMaster())
                {
                    __instance.ShareNGPlus();
                }
            }
            else
            {
                if (!GameManager.Instance.IsWeeklyChallenge())
                {
                    if (__instance.obeliskMadness >= 9)
                    {
                        __instance.SetGameId("");
                    }
                    if (GameManager.Instance.IsMultiplayer() && NetworkManager.Instance.IsMaster())
                    {
                        __instance.ShareObeliskMadness();
                    }
                }
                __instance.SetObeliskNodes();
            }
            if (!__instance.GameLoaded)
            {
                for (int k = 0; k < __instance.teamAtO.Length; k++)
                {
                    if (__instance.teamAtO[k] != null && __instance.teamAtO[k].HeroData != null && __instance.teamAtO[k].HeroData.HeroSubClass != null)
                    {
                        __instance.teamAtO[k].AssignTrait(__instance.teamAtO[k].HeroData.HeroSubClass.Trait0.Id);
                    }
                }
                Debug.Log("[ATO BeginAdventure] end assign traits to heroes");
                if (GameManager.Instance.IsMultiplayer())
                {
                    __instance.InitGameMP();
                }
                else
                {
                    __instance.InitGame();
                }
                Debug.Log("[ATO BeginAdventure] end InitGame");
                __instance.SetTownTier(0);
                Debug.Log("[ATO BeginAdventure] end SetTownTier");
                if (GameManager.Instance.IsObeliskChallenge())
                {
                    __instance.SetCurrentNode(__instance.obeliskLow + "_0", "", "");
                }
                else if (!GameManager.Instance.IsMultiplayer() && __instance.IsFirstGame())
                {
                    __instance.SetCurrentNode("tutorial_0", "", "");
                }
                else
                {
                    __instance.SetCurrentNode("sen_0", "", "");
                }
                if (GameManager.Instance.IsMultiplayer() && NetworkManager.Instance.IsMaster())
                {
                    if (GameManager.Instance.IsObeliskChallenge())
                    {
                        __instance.StartCoroutine(__instance.ShareTeam("ChallengeSelection", true, true));
                    }
                    else
                    {
                        __instance.StartCoroutine(__instance.ShareTeam("IntroNewGame", true, true));
                    }
                }
                else if (!GameManager.Instance.IsMultiplayer())
                {
                    if (GameManager.Instance.IsObeliskChallenge())
                    {
                        SceneStatic.LoadByName("ChallengeSelection", true);
                    }
                    else
                    {
                        SceneStatic.LoadByName("IntroNewGame", true);
                    }
                }
            }
            Debug.Log("[ATO BeginAdventure] END");

            return false;
        }
    }

    [HarmonyPatch(typeof(AtOManager), "FinishCardRewards")]
    class FinishCardRewards {
        [HarmonyPrefix]
        static bool setpatch(AtOManager __instance, string[] arrRewards)
        {
            for (int i = 0; i < 8; i++)
            {
                if (__instance.teamAtO[i] != null && !(__instance.teamAtO[i].HeroData == null))
                {
                    if (arrRewards[i] != "" && arrRewards[i] != "dust")
                    {
                        __instance.AddCardToHero(i, arrRewards[i]);
                    }
                    else if (arrRewards[i] == "dust")
                    {
                        int num = Globals.Instance.GetTierRewardData(__instance.currentRewardTier).Dust;
                        if (GameManager.Instance.IsObeliskChallenge() && Globals.Instance.ZoneDataSource[__instance.GetTownZoneId().ToLower()].ObeliskLow)
                        {
                            num *= 2;
                        }
                        if (MadnessManager.Instance.IsMadnessTraitActive("poverty") || __instance.IsChallengeTraitActive("poverty"))
                        {
                            if (!GameManager.Instance.IsObeliskChallenge())
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.5f);
                            }
                            else
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.3f);
                            }
                        }
                        if (__instance.IsChallengeTraitActive("prosperity"))
                        {
                            num += Functions.FuncRoundToInt((float)num * 0.5f);
                        }
                        __instance.GivePlayer(1, num, __instance.teamAtO[i].Owner, "", true, false);
                    }
                }
            }
            if (GameManager.Instance.IsObeliskChallenge() && __instance.mapNodeObeliskBoss.Contains(__instance.currentMapNode))
            {
                NodeData nodeData = Globals.Instance.GetNodeData(__instance.currentMapNode);
                if (nodeData.NodeCombatTier == Enums.CombatTier.T8)
                {
                    __instance.DoLoot("challenge_boss_low");
                    return false;
                }
                if (nodeData.NodeCombatTier == Enums.CombatTier.T9)
                {
                    __instance.DoLoot("challenge_boss_high");
                    return false;
                }
                if (__instance.townZoneId.ToLower() == __instance.obeliskLow)
                {
                    __instance.DoLoot("challenge_chest_low");
                    return false;
                }
                if (__instance.townZoneId.ToLower() == __instance.obeliskHigh)
                {
                    __instance.DoLoot("challenge_chest_high");
                    return false;
                }
                __instance.DoLoot("challenge_chest_final");
                return false;
            }
            else if (GameManager.Instance.IsMultiplayer())
            {
                if (__instance.townDivinationTier != null)
                {
                    __instance.StartCoroutine(__instance.ShareTeam("Town", true, false));
                    return false;
                }
                __instance.StartCoroutine(__instance.ShareTeam("Map", true, false));
                return false;
            }
            else
            {
                if (__instance.townDivinationTier != null)
                {
                    SceneStatic.LoadByName("Town", true);
                    return false;
                }
                SceneStatic.LoadByName("Map", true);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(AtOManager), "GetHero")]
    class GetHero {
        [HarmonyPrefix]
        static bool setpatch(ref Hero __result, ref AtOManager __instance, ref int index) {
            if (index >= 0 && index < 8 && __instance.teamAtO != null && __instance.teamAtO.Length != 0)
            {
                __result = __instance.teamAtO[index];
                return false;
            }
            __result = null;

            return false;
        }
    }

    [HarmonyPatch(typeof(AtOManager), "InitCombatStats")]
    class InitCombatStats
    {
        [HarmonyPrefix]
        static bool setpatch(AtOManager __instance)
        {
            System.Console.WriteLine("[CombatStats] Update for 8 players");
            __instance.combatStats = new int[8, 12];
            return false;
        }
    }

    [HarmonyPatch(typeof(AtOManager), "ShareTeam")]
    class ShareTeam
    {
        [HarmonyPrefix]
        static bool setpatch() {
            return false;
        }
        [HarmonyPostfix]
        static IEnumerator Postfix(IEnumerator result, AtOManager __instance, string sceneToLoad = "", bool showMask = true, bool setOwners = false)
        {
            System.Console.WriteLine("[ShareTeam] Postfix applied");
            if (NetworkManager.Instance.IsMaster())
            {
                for (int i = 0; i < __instance.teamAtO.Length; i++)
                {
                    if (__instance.teamAtO[i] != null)
                    {
                        __instance.teamAtO[i].AssignOwner(NetworkManager.Instance.PlayerHeroPositionOwner[i]);
                    }
                    if (__instance.teamAtO[i].HpCurrent <= 0)
                    {
                        __instance.teamAtO[i].HpCurrent = 1;
                    }
                    if (__instance.heroPerks != null && __instance.heroPerks.ContainsKey(__instance.teamAtO[i].SubclassName))
                    {
                        __instance.teamAtO[i].PerkList = __instance.heroPerks[__instance.teamAtO[i].SubclassName];
                    }
                }
                string text = JsonHelper.ToJson<Hero>(__instance.teamAtO);
                string text2 = JsonHelper.ToJson<string>(__instance.teamNPCAtO);
                Functions.DebugLogGD("shareTeam MP", "net");
                __instance.RedoSkins();
                NetworkManager.Instance.ClearAllPlayerManualReady();
                Functions.DebugLogGD("NET_SetTeam CALL", "net");
                __instance.photonView.RPC("NET_SetTeam", RpcTarget.Others, new object[]
                {
                __instance.gameId,
                __instance.currentMapNode,
                Functions.CompressString(text),
                Functions.CompressString(text2)
                });
                while (!NetworkManager.Instance.AllPlayersReady("shareteam"))
                {
                    yield return Globals.Instance.WaitForSeconds(0.01f);
                }
                if (sceneToLoad != "")
                {
                    NetworkManager.Instance.LoadScene(sceneToLoad, true);
                }
                else if (MapManager.Instance != null)
                {
                    MapManager.Instance.sideCharacters.Refresh();
                }
            }
            yield break;
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

}