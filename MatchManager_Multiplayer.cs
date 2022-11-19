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
using Object = UnityEngine.Object;

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
        modInfo.harmony.PatchAll(typeof(CardNamesForSyncCode));
        modInfo.harmony.PatchAll(typeof(FixCodeSyncFromMaster));
        modInfo.harmony.PatchAll(typeof(FixTOTALCo));
        modInfo.harmony.PatchAll(typeof(GetHero));
        modInfo.harmony.PatchAll(typeof(GetHeroFromId));
        modInfo.harmony.PatchAll(typeof(GetHeroItemsForTurnSave));
        modInfo.harmony.PatchAll(typeof(NET_ShareDecks));
        modInfo.harmony.PatchAll(typeof(NextTurnContinue));
        modInfo.harmony.PatchAll(typeof(PositionIsMiddle));
        modInfo.harmony.PatchAll(typeof(ReloadCombatFullAction));
        modInfo.harmony.PatchAll(typeof(SetCharactersPing));
        modInfo.harmony.PatchAll(typeof(NET_SaveCardDeck));
        //NET_SaveCardDeck (need more args)

    }

    [HarmonyPatch(typeof(MatchManager), "NET_SaveCardDeck")]
    class NET_SaveCardDeck {
        [HarmonyPrefix]
        static bool skip() {
            return false;
        }

        [HarmonyPostfix]
        static void setpatch(MatchManager __instance, ref string _type, ref string[] _arr0) {
            List<string> list = new List<string>();
            if (_arr0[0] != "")
            {
                list.AddRange(JsonHelper.FromJson<string>(Functions.DecompressString(_arr0[0])));
            }
            List<string> list2 = new List<string>();
            if (_arr0[1] != "")
            {
                list2.AddRange(JsonHelper.FromJson<string>(Functions.DecompressString(_arr0[1])));
            }
            List<string> list3 = new List<string>();
            if (_arr0[2] != "")
            {
                list3.AddRange(JsonHelper.FromJson<string>(Functions.DecompressString(_arr0[2])));
            }
            List<string> list4 = new List<string>();
            if (_arr0[3] != "")
            {
                list4.AddRange(JsonHelper.FromJson<string>(Functions.DecompressString(_arr0[3])));
            }
            List<string> list5 = new List<string>();
            if (_arr0[4] != "")
            {
                list5.AddRange(JsonHelper.FromJson<string>(Functions.DecompressString(_arr0[4])));
            }
            List<string> list6 = new List<string>();
            if (_arr0[5] != "")
            {
                list6.AddRange(JsonHelper.FromJson<string>(Functions.DecompressString(_arr0[5])));
            }
            List<string> list7 = new List<string>();
            if (_arr0[6] != "")
            {
                list7.AddRange(JsonHelper.FromJson<string>(Functions.DecompressString(_arr0[6])));
            }
            List<string> list8 = new List<string>();
            if (_arr0[7] != "")
            {
                list8.AddRange(JsonHelper.FromJson<string>(Functions.DecompressString(_arr0[7])));
            }
            if (_type == "Hero")
            {
                __instance.HeroDeck[0] = list;
                __instance.HeroDeck[1] = list2;
                __instance.HeroDeck[2] = list3;
                __instance.HeroDeck[3] = list4;
                __instance.HeroDeck[4] = list5;
                __instance.HeroDeck[5] = list6;
                __instance.HeroDeck[6] = list7;
                __instance.HeroDeck[7] = list8;
                __instance.gotHeroDeck = true;
            }
            else
            {
                __instance.NPCDeck[0] = list;
                __instance.NPCDeck[1] = list2;
                __instance.NPCDeck[2] = list3;
                __instance.NPCDeck[3] = list4;
                __instance.gotNPCDeck = true;
            }
            __instance.NetworkGotCards = true;
        }
    }

    [HarmonyPatch(typeof(MatchManager), "FixCodeSyncFromMaster")]
    class FixCodeSyncFromMaster {
        [HarmonyPrefix]
        static bool setpatch(MatchManager __instance,ref int _randomIndex,ref string _codeFromMaster, bool _sendStatusReady, bool _sendFinishCastReady) {
            Functions.DebugLogGD("FixCodeSyncFromMaster", "synccode");
            Functions.DebugLogGD(_codeFromMaster, "synccode");
            __instance.SetRandomIndex(_randomIndex);
            if (_codeFromMaster != "")
            {
                bool flag = false;
                try
                {
                    _codeFromMaster = Functions.DecompressString(_codeFromMaster);
                }
                catch
                {
                }
                Functions.DebugLogGD(_codeFromMaster, "synccode");
                string[] array = _codeFromMaster.Split('$', (char)StringSplitOptions.None);
                int teamNum = __instance.TeamHero.Length;
                for (int i = 0; i < teamNum + 4; i++)
                {
                    string[] array2 = array[i].Split('|', (char)StringSplitOptions.None);
                    Character character;
                    if (i < teamNum)
                    {
                        character = __instance.TeamHero[i];
                    }
                    else
                    {
                        character = __instance.TeamNPC[i - teamNum];
                    }
                    if (character != null && character.Alive)
                    {
                        string[] array3 = array2[1].Split('_', (char)StringSplitOptions.None);
                        character.HpCurrent = int.Parse(array3[0]);
                        if (array3.Length > 1 && array3[1] != null)
                        {
                            character.Hp = int.Parse(array3[1]);
                        }
                        else
                        {
                            character.Hp = character.HpCurrent;
                        }
                        character.AuraList = new List<Aura>();
                        if (array2.Length >= 4)
                        {
                            string[] array4 = array2[3].Split(':', (char)StringSplitOptions.None);
                            for (int j = 0; j < array4.Length; j++)
                            {
                                Aura aura = new Aura();
                                string[] array5 = array4[j].Split('_', (char)StringSplitOptions.None);
                                if (array5.Length == 2)
                                {
                                    aura.SetAura(AtOManager.Instance.GlobalAuraCurseModificationByTraitsAndItems("set", Globals.Instance.GetAuraCurseFromIndex(int.Parse(array5[0])).ToLower(), null, character), int.Parse(array5[1]));
                                    character.AuraList.Add(aura);
                                }
                            }
                            character.UpdateAuraCurseFunctions(null, 0, -1);
                        }
                        if (i < teamNum)
                        {
                            character.EnergyCurrent = int.Parse(array2[4]);
                            character.HeroItem.DrawEnergy();
                            if (flag || array2[5] != "")
                            {
                                string[] array6 = array2[5].Split('%', (char)StringSplitOptions.None);
                                __instance.HeroDeckDiscard[i] = new List<string>();
                                for (int k = 0; k < array6.Length; k++)
                                {
                                    if (array6[k] != "")
                                    {
                                        __instance.HeroDeckDiscard[i].Add(array6[k]);
                                    }
                                }
                                string[] array7 = array2[6].Split('%', (char)StringSplitOptions.None);
                                __instance.HeroDeck[i] = new List<string>();
                                for (int l = 0; l < array7.Length; l++)
                                {
                                    if (array7[l] != "")
                                    {
                                        __instance.HeroDeck[i].Add(array7[l]);
                                    }
                                }
                                string[] array8 = array2[7].Split('%', (char)StringSplitOptions.None);
                                __instance.HeroDeckVanish[i] = new List<string>();
                                for (int m = 0; m < array8.Length; m++)
                                {
                                    if (array8[m] != "")
                                    {
                                        __instance.HeroDeckVanish[i].Add(array8[m]);
                                    }
                                }
                            }
                            string[] array9 = array2[10].Split(':', (char)StringSplitOptions.None);
                            bool flag2 = false;
                            if (array9 != null)
                            {
                                if (array9[0] != null && array9[0] != "")
                                {
                                    character.AssignEnchantmentManual(array9[0], 0);
                                }
                                if (array9[1] != null && array9[1] != "")
                                {
                                    character.AssignEnchantmentManual(array9[1], 1);
                                }
                                if (array9[2] != null && array9[2] != "")
                                {
                                    character.AssignEnchantmentManual(array9[2], 2);
                                }
                                flag2 = true;
                            }
                            if (flag2)
                            {
                                character.HeroItem.ShowEnchantments();
                            }
                        }
                        character.RoundMoved = int.Parse(array2[8]);
                    }
                }
                __instance.currentRound = int.Parse(array[8]);
                string[] array10 = array[9].Split('&', (char)StringSplitOptions.None);
                __instance.enchantmentExecutedTotal.Clear();
                for (int n = 0; n < array10.Length; n++)
                {
                    string[] array11 = array10[n].Split('%', (char)StringSplitOptions.None);
                    if (array11 != null && array11.Length == 2 && array11[0] != null && array11[1] != null && int.Parse(array11[1]) > 0)
                    {
                        __instance.enchantmentExecutedTotal.Add(array11[0], int.Parse(array11[1]));
                    }
                }
            }
            Functions.DebugLogGD("FixCodeSyncFromMaster ends", "synccode");
            if (GameManager.Instance.IsMultiplayer() && _sendStatusReady)
            {
                NetworkManager.Instance.SetStatusReady("FixingSyncCode");
            }
            if (GameManager.Instance.IsMultiplayer() && _sendFinishCastReady)
            {
                NetworkManager.Instance.SetStatusReady("finishcast");
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MatchManager), "FixTOTALCo")]
    class FixTOTALCo {
        [HarmonyPrefix]
        static bool setpatch() {
            return false;
        }

        [HarmonyPostfix]
        static IEnumerator patch(IEnumerator result, MatchManager __instance, int _randomIndex, string codeFromMaster)
        {
            __instance.eventList.Add("FixingTotalCo");
            string[] codeArr = codeFromMaster.Split('$', (char)StringSplitOptions.None);
            int teamNum = __instance.TeamHero.Length;
            int npcNum = __instance.TeamNPC.Length;
            __instance.currentRound = int.Parse(codeArr[teamNum + npcNum]);
            for (int j = 0; j < teamNum + npcNum; j++)
            {
                string[] array = codeArr[j].Split('|', (char)StringSplitOptions.None);
                if (array.Length >= teamNum + npcNum + 1 && int.Parse(array[teamNum + npcNum]) == __instance.currentRound)
                {
                    __instance.roundBegan = true;
                }
            }
            string[] array2 = codeArr[teamNum + npcNum + 1].Split('&', (char)StringSplitOptions.None);
            __instance.enchantmentExecutedTotal.Clear();
            for (int k = 0; k < array2.Length; k++)
            {
                string[] array3 = array2[k].Split('%', (char)StringSplitOptions.None);
                if (array3 != null && array3.Length == 2 && array3[0] != null && array3[1] != null && int.Parse(array3[1]) > 0)
                {
                    __instance.enchantmentExecutedTotal.Add(array3[0], int.Parse(array3[1]));
                }
            }
            int num6;
            for (int i = 0; i < teamNum + npcNum; i = num6 + 1)
            {
                string[] aux = codeArr[i].Split('|', (char)StringSplitOptions.None);
                Character theChar = null;
                if (i < teamNum)
                {
                    theChar = __instance.TeamHero[i];
                }
                else
                {
                    theChar = __instance.TeamNPC[i - teamNum];
                }
                if (theChar != null || (aux.Length >= 7 && aux[8] != ""))
                {
                    if (aux.Length == 1)
                    {
                        theChar.Alive = false;
                        theChar.HpCurrent = 0;
                        if (i < 4)
                        {
                            Object.Destroy(__instance.GO_Heroes.transform.GetChild(i).gameObject);
                        }
                    }
                    else
                    {
                        if (i >= teamNum)
                        {
                            __instance.CreateNPC(Globals.Instance.GetNPC(aux[9]), "", i - teamNum, true, aux[10], null);
                            yield return Globals.Instance.WaitForSeconds(0.2f);
                            theChar = __instance.TeamNPC[i - teamNum];
                            theChar.Corruption = aux[11];
                        }
                        string[] array4 = aux[1].Split('_', (char)StringSplitOptions.None);
                        theChar.HpCurrent = int.Parse(array4[0]);
                        if (array4.Length > 1 && array4[1] != null)
                        {
                            theChar.Hp = int.Parse(array4[1]);
                        }
                        else
                        {
                            theChar.Hp = theChar.HpCurrent;
                        }
                        theChar.AuraList = new List<Aura>();
                        if (aux.Length >= 4)
                        {
                            string[] array5 = aux[3].Split(':', (char)StringSplitOptions.None);
                            for (int l = 0; l < array5.Length; l++)
                            {
                                Aura aura = new Aura();
                                string[] array6 = array5[l].Split('_', (char)StringSplitOptions.None);
                                if (array6.Length == 2)
                                {
                                    aura.SetAura(AtOManager.Instance.GlobalAuraCurseModificationByTraitsAndItems("set", Globals.Instance.GetAuraCurseFromIndex(int.Parse(array6[0])).ToLower(), null, theChar), int.Parse(array6[1]));
                                    theChar.AuraList.Add(aura);
                                }
                            }
                            theChar.UpdateAuraCurseFunctions(null, 0, -1);
                        }
                        if (i < teamNum)
                        {
                            theChar.EnergyCurrent = int.Parse(aux[4]);
                            theChar.HeroItem.DrawEnergy();
                            string[] array7 = aux[5].Split('%', (char)StringSplitOptions.None);
                            __instance.HeroDeckDiscard[i] = new List<string>();
                            for (int m = 0; m < array7.Length; m++)
                            {
                                if (array7[m] != "")
                                {
                                    __instance.HeroDeckDiscard[i].Add(array7[m]);
                                }
                            }
                            string[] array8 = aux[6].Split('%', (char)StringSplitOptions.None);
                            __instance.HeroDeck[i] = new List<string>();
                            for (int n = 0; n < array8.Length; n++)
                            {
                                if (array8[n] != "")
                                {
                                    __instance.HeroDeck[i].Add(array8[n]);
                                }
                            }
                            string[] array9 = aux[7].Split('%', (char)StringSplitOptions.None);
                            __instance.HeroDeckVanish[i] = new List<string>();
                            for (int num = 0; num < array9.Length; num++)
                            {
                                if (array9[num] != "")
                                {
                                    __instance.HeroDeckVanish[i].Add(array9[num]);
                                }
                            }
                            string[] array10 = aux[10].Split(':', (char)StringSplitOptions.None);
                            bool flag = false;
                            if (array10 != null)
                            {
                                if (array10[0] != null && array10[0] != "")
                                {
                                    theChar.AssignEnchantmentManual(array10[0], 0);
                                }
                                if (array10[1] != null && array10[1] != "")
                                {
                                    theChar.AssignEnchantmentManual(array10[1], 1);
                                }
                                if (array10[2] != null && array10[2] != "")
                                {
                                    theChar.AssignEnchantmentManual(array10[2], 2);
                                }
                                flag = true;
                            }
                            if (flag)
                            {
                                theChar.HeroItem.ShowEnchantments();
                            }
                        }
                        else
                        {
                            string[] array11 = aux[4].Split('%', (char)StringSplitOptions.None);
                            if (__instance.NPCDeck[i - teamNum] != null)
                            {
                                __instance.NPCDeck[i - teamNum].Clear();
                                for (int num2 = 0; num2 < array11.Length; num2++)
                                {
                                    if (array11[num2] != "")
                                    {
                                        __instance.NPCDeck[i - teamNum].Add(array11[num2]);
                                    }
                                }
                            }
                            string[] array12 = aux[5].Split('%', (char)StringSplitOptions.None);
                            if (__instance.NPCDeckDiscard[i - teamNum] != null)
                            {
                                __instance.NPCDeckDiscard[i - teamNum].Clear();
                                for (int num3 = 0; num3 < array12.Length; num3++)
                                {
                                    if (array12[num3] != "")
                                    {
                                        __instance.NPCDeckDiscard[i - teamNum].Add(array12[num3]);
                                    }
                                }
                            }
                            string[] array13 = aux[6].Split('%', (char)StringSplitOptions.None);
                            if (__instance.NPCHand != null && __instance.NPCHand[i - teamNum] == null)
                            {
                                __instance.NPCHand[i - teamNum] = new List<string>();
                            }
                            if (__instance.NPCHand != null && __instance.NPCHand[i - teamNum] != null)
                            {
                                __instance.NPCHand[i - teamNum].Clear();
                                for (int num4 = 0; num4 < array13.Length; num4++)
                                {
                                    if (array13[num4] != "")
                                    {
                                        __instance.NPCHand[i - teamNum].Add(array13[num4]);
                                        Functions.DebugLogGD(array13[num4] + " added to the hand of " + aux[8], "trace");
                                    }
                                }
                                if (__instance.roundBegan && __instance.NPCHand[i - teamNum].Count > 0)
                                {
                                    yield return Globals.Instance.WaitForSeconds(0.1f);
                                    theChar.CreateOverDeck();
                                }
                            }
                            string[] array14 = aux[7].Split('%', (char)StringSplitOptions.None);
                            if (__instance.npcCardsCasted == null)
                            {
                                __instance.npcCardsCasted = new Dictionary<string, List<string>>();
                            }
                            if (!__instance.npcCardsCasted.ContainsKey(aux[10]))
                            {
                                __instance.npcCardsCasted.Add(aux[10], new List<string>());
                            }
                            for (int num5 = 0; num5 < array14.Length; num5++)
                            {
                                if (array14[num5] != "")
                                {
                                    __instance.npcCardsCasted[aux[10]].Add(array14[num5]);
                                }
                            }
                            string[] array15 = aux[12].Split(':', (char)StringSplitOptions.None);
                            bool flag2 = false;
                            if (array15 != null)
                            {
                                if (array15[0] != null && array15[0] != "")
                                {
                                    theChar.AssignEnchantmentManual(array15[0], 0);
                                }
                                if (array15[1] != null && array15[1] != "")
                                {
                                    theChar.AssignEnchantmentManual(array15[1], 1);
                                }
                                if (array15[2] != null && array15[2] != "")
                                {
                                    theChar.AssignEnchantmentManual(array15[2], 2);
                                }
                                flag2 = true;
                            }
                            if (flag2)
                            {
                                theChar.NPCItem.ShowEnchantments();
                            }
                        }
                        theChar.RoundMoved = int.Parse(aux[8]);
                    }
                }
                aux = null;
                theChar = null;
                num6 = i;
            }
            __instance.SetRandomIndex(int.Parse(codeArr[11]));
            __instance.eventList.Remove("FixingTotalCo");
            __instance.gotHeroDeck = true;
            __instance.gotNPCDeck = true;
            __instance.gotDictionary = true;
            __instance.RestoreCombatStats();
            __instance.backingDictionary = true;
            __instance.RestoreCardDictionary();
            while (__instance.backingDictionary)
            {
                yield return Globals.Instance.WaitForSeconds(0.01f);
            }
            if (!GameManager.Instance.IsMultiplayer())
            {
                __instance.StartCoroutine(__instance.BeginMatch());
            }
            else if (NetworkManager.Instance.IsMaster())
            {
                __instance.NET_ShareDecks(true);
            }
            yield break;
        }
    }
    
    [HarmonyPatch(typeof(MatchManager), "GetHero")]
    class GetHero
    {
        [HarmonyPrefix]
        static bool setpatch()
        {
            return false;
        }
        [HarmonyPostfix]
        static void postpatch(MatchManager __instance, int _order, ref Hero __result) {
            if (_order > -1 && _order < 8 && __instance.TeamHero[_order] != null)
            {
                __result = __instance.TeamHero[_order];
            }
            __result = null;
        }
    }
    
    [HarmonyPatch(typeof(MatchManager), "GetHeroFromId")]
    class GetHeroFromId
    {
        [HarmonyPrefix]
        static bool setpatch()
        {
            return false;
        }

        [HarmonyPostfix]
        static void postpatch(MatchManager __instance, string _id, ref int __result) {
            for (int i = 0; i < 8; i++)
            {
                if (__instance.TeamHero[i] != null && __instance.TeamHero[i].Id == _id)
                {
                    __result = i;
                }
            }
            __result = - 1;
        }
    }
    [HarmonyPatch(typeof(MatchManager), "GetHeroItemsForTurnSave")]
    class GetHeroItemsForTurnSave
    {
        [HarmonyPrefix]
        static bool setpatch()
        {
            return false;
        }

        [HarmonyPostfix]
        static void postpatch(MatchManager __instance, ref string __result)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                list.Add(__instance.TeamHero[i].Weapon);
                list.Add(__instance.TeamHero[i].Armor);
                list.Add(__instance.TeamHero[i].Jewelry);
                list.Add(__instance.TeamHero[i].Accesory);
                list.Add(__instance.TeamHero[i].Pet);
            }
            __result = Functions.CompressString(JsonHelper.ToJson<string>(list.ToArray()));
        }
    }
    [HarmonyPatch(typeof(MatchManager), "NET_ShareDecks")]
    class NET_ShareDecks
    {
        [HarmonyPrefix]
        static bool setpatch()
        {
            return false;
        }

        [HarmonyPostfix]
        static void postpatch(MatchManager __instance, bool ContinueNewGame = false)
        {
            Functions.DebugLogGD("[BEGIN] NET_ShareDecks", "net");
            if (GameManager.Instance.IsMultiplayer())
            {
                if (__instance.currentRound == 0)
                {
                    string[] array = new string[__instance.cardDictionary.Count];
                    __instance.cardDictionary.Keys.CopyTo(array, 0);
                    string text = JsonHelper.ToJson<string>(array);
                    MatchManager.CardDataForShare[] array2 = new MatchManager.CardDataForShare[__instance.cardDictionary.Count];
                    int num = 0;
                    foreach (KeyValuePair<string, CardData> keyValuePair in __instance.cardDictionary)
                    {
                        array2[num] = new MatchManager.CardDataForShare
                        {
                            vanish = keyValuePair.Value.Vanish,
                            energyReductionPermanent = keyValuePair.Value.EnergyReductionPermanent,
                            energyReductionTemporal = keyValuePair.Value.EnergyReductionTemporal,
                            energyReductionToZeroPermanent = keyValuePair.Value.EnergyReductionToZeroPermanent,
                            energyReductionToZeroTemporal = keyValuePair.Value.EnergyReductionToZeroTemporal
                        };
                        num++;
                    }
                    string text2 = JsonHelper.ToJson<MatchManager.CardDataForShare>(array2);
                    __instance.photonView.RPC("NET_SaveCardDictionary", RpcTarget.Others, new object[]
                    {
                    Functions.CompressString(text),
                    Functions.CompressString(text2)
                    });
                }
                else
                {
                    __instance.photonView.RPC("NET_SaveCardDictionary", RpcTarget.Others, new object[] { "", "" });
                }
                __instance.gotDictionary = true;
                string[] array3 = new string[8];
                for (int i = 0; i < 8; i++)
                {
                    array3[i] = "";
                    if (__instance.HeroDeck[i] != null)
                    {
                        string text3 = JsonHelper.ToJson<string>(__instance.HeroDeck[i].ToArray());
                        array3[i] = Functions.CompressString(text3);
                    }
                }
                __instance.photonView.RPC("NET_SaveCardDeck", RpcTarget.Others, new object[]
                {
                "Hero",
                array3
                });
                array3 = new string[4];
                for (int j = 0; j < 4; j++)
                {
                    array3[j] = "";
                    if (__instance.NPCDeck[j] != null)
                    {
                        string text4 = JsonHelper.ToJson<string>(__instance.NPCDeck[j].ToArray());
                        array3[j] = Functions.CompressString(text4);
                    }
                }
                __instance.photonView.RPC("NET_SaveCardDeck", RpcTarget.Others, new object[]
                {
                "NPC",
                array3
                });
                if (ContinueNewGame)
                {
                    __instance.gotHeroDeck = true;
                    __instance.gotNPCDeck = true;
                    __instance.photonView.RPC("NET_BeginMatch", RpcTarget.All, new object[] { __instance.randomIndex });
                    return;
                }
            }
            else if (ContinueNewGame)
            {
                __instance.StartCoroutine(__instance.BeginMatch());
            }
        }
    }
    [HarmonyPatch(typeof(MatchManager), "NextTurnContinue")]
    class NextTurnContinue : MonoBehaviour
    {
        [HarmonyPrefix]
        static bool setpatch()
        {
            return false;
        }

        [HarmonyPostfix]
        static IEnumerator postpatch(IEnumerator result, MatchManager __instance)
        {
            AtOManager.Instance.SaveGameTurn();
            __instance.backingDictionary = true;
            __instance.BackupCardDictionary();
            while (__instance.backingDictionary)
            {
                yield return Globals.Instance.WaitForSeconds(0.01f);
            }
            int eventExaust = 0;
            __instance.CombatTextIterations = 0;
            __instance.generatedCardTimes = 0;
            __instance.failCount = 0;
            if (__instance.theHero != null && __instance.theHero.Alive)
            {
                __instance.CleanPrePostDamageDictionary(__instance.theHero.Id);
                if (__instance.theHero.HeroItem != null)
                {
                    __instance.theHero.HeroItem.CalculateDamagePrePostForThisCharacter();
                }
            }
            if (__instance.theNPC != null && __instance.theNPC.Alive)
            {
                __instance.CleanPrePostDamageDictionary(__instance.theNPC.Id);
                if (__instance.theNPC.NPCItem != null)
                {
                    __instance.theNPC.NPCItem.CalculateDamagePrePostForThisCharacter();
                }
            }
            __instance.theHero = null;
            __instance.theNPC = null;
            bool flag = true;
            for (int j = 0; j < __instance.CharOrder.Count; j++)
            {
                if (__instance.CharOrder[j].hero != null)
                {
                    if (__instance.CharOrder[j].hero.RoundMoved < __instance.currentRound && __instance.CharOrder[j].hero.Alive)
                    {
                        flag = false;
                        break;
                    }
                }
                else if (__instance.CharOrder[j].npc.RoundMoved < __instance.currentRound && __instance.CharOrder[j].npc.Alive)
                {
                    flag = false;
                    break;
                }
            }
            if (flag)
            {
                Functions.DebugLogGD("CURRENT ROUND -> " + __instance.currentRound.ToString(), "trace");
                int num;
                if (__instance.currentRound > 0)
                {
                    for (int i = 0; i < __instance.TeamHero.Length; i = num + 1)
                    {
                        if (__instance.TeamHero[i] != null && __instance.TeamHero[i].Alive)
                        {
                            __instance.waitExecution = true;
                            __instance.TeamHero[i].EndRound();
                            eventExaust = 0;
                            while (__instance.waitExecution)
                            {
                                yield return Globals.Instance.WaitForSeconds(0.01f);
                                num = eventExaust;
                                eventExaust = num + 1;
                                if (eventExaust > 400)
                                {
                                    Functions.DebugLogGD("[ENDTURNCO] Waitexecution EXAUSTED!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", "trace");
                                    __instance.waitExecution = false;
                                }
                            }
                        }
                        num = i;
                    }
                    for (int i = 0; i < __instance.TeamNPC.Length; i = num + 1)
                    {
                        if (__instance.TeamNPC[i] != null && __instance.TeamNPC[i].Alive)
                        {
                            __instance.waitExecution = true;
                            __instance.TeamNPC[i].EndRound();
                            eventExaust = 0;
                            while (__instance.waitExecution)
                            {
                                yield return Globals.Instance.WaitForSeconds(0.01f);
                                num = eventExaust;
                                eventExaust = num + 1;
                                if (eventExaust > 400)
                                {
                                    Functions.DebugLogGD("[ENDTURNCO] Waitexecution EXAUSTED!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", "trace");
                                    __instance.waitExecution = false;
                                }
                            }
                        }
                        num = i;
                    }
                }
                if (__instance.MatchIsOver)
                {
                    yield break;
                }
                if (__instance.currentRound == 0 && __instance.corruptionItem != null && __instance.corruptionItem.Activation == Enums.EventActivation.CorruptionCombatStart)
                {
                    CardData cardData = __instance.GetCardData(__instance.corruptionCardId);
                    cardData.EnergyCost = 0;
                    cardData.Vanish = true;
                    __instance.GenerateNewCard(1, __instance.corruptionCardId, false, Enums.CardPlace.Vanish, null, null, -1, true, 0);
                    for (int k = 0; k < 4; k++)
                    {
                        if (__instance.TeamNPC[k] != null && __instance.TeamNPC[k].Alive)
                        {
                            __instance.TeamNPC[k].DoItem(Enums.EventActivation.CorruptionCombatStart, cardData, "", null, 0, "", 0, null);
                            break;
                        }
                    }
                    yield return Globals.Instance.WaitForSeconds(1.5f);
                    __instance.SetGameBusy(false);
                }
                __instance.currentRound++;
                __instance.activatedTraitsRound.Clear();
                if (__instance.combatData != null && (MadnessManager.Instance.IsMadnessTraitActive("impedingdoom") || AtOManager.Instance.IsChallengeTraitActive("impedingdoom")))
                {
                    ThermometerTierData thermometerTierData = __instance.combatData.ThermometerTierData;
                    if (thermometerTierData != null)
                    {
                        for (int l = 0; l < thermometerTierData.RoundThermometer.Length; l++)
                        {
                            if (thermometerTierData.RoundThermometer[l] != null && __instance.currentRound >= thermometerTierData.RoundThermometer[l].Round)
                            {
                                if (thermometerTierData.RoundThermometer[l].Round == __instance.currentRound)
                                {
                                    ThermometerData thermometerData = thermometerTierData.RoundThermometer[l].ThermometerData;
                                    Functions.DebugLogGD(thermometerData.ThermometerId + "<------------", "");
                                    if (thermometerData != null && thermometerData.ThermometerId.ToLower() == "underwhelming")
                                    {
                                        for (int m = 0; m < __instance.TeamHero.Length; m++)
                                        {
                                            __instance.TeamHero[m].SetAura(null, Globals.Instance.GetAuraCurseData("doom"), 2, false, Enums.CardClass.None);
                                        }
                                        break;
                                    }
                                    break;
                                }
                                else if (thermometerTierData.RoundThermometer[l].Round > __instance.currentRound)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                Functions.DebugLogGD("[CORRUPTIONBEGINROUND]", "");
                if (__instance.corruptionItem != null && __instance.corruptionItem.Activation == Enums.EventActivation.CorruptionBeginRound)
                {
                    if (GameManager.Instance.IsMultiplayer())
                    {
                        Functions.DebugLogGD("**************************", "net");
                        Functions.DebugLogGD("WaitingSyncro CorruptionBeginRoundPre", "net");
                        if (NetworkManager.Instance.IsMaster())
                        {
                            if (__instance.coroutineSyncBeginRound != null)
                            {
                                __instance.StopCoroutine(__instance.coroutineSyncBeginRound);
                            }
                            __instance.coroutineSyncBeginRound = __instance.StartCoroutine(__instance.ReloadCombatCo("CorruptionBeginRoundPre"));
                            while (!NetworkManager.Instance.AllPlayersReady("CorruptionBeginRoundPre"))
                            {
                                yield return Globals.Instance.WaitForSeconds(0.01f);
                            }
                            __instance.StopCoroutine(__instance.coroutineSyncBeginRound);
                            Functions.DebugLogGD("Game ready, Everybody checked CorruptionBeginRoundPre", "net");
                            __instance.SetRandomIndex(__instance.randomIndex);
                            NetworkManager.Instance.PlayersNetworkContinue("CorruptionBeginRoundPre", __instance.randomIndex.ToString());
                        }
                        else
                        {
                            NetworkManager.Instance.SetWaitingSyncro("CorruptionBeginRoundPre", true);
                            NetworkManager.Instance.SetStatusReady("CorruptionBeginRoundPre");
                            while (NetworkManager.Instance.WaitingSyncro["CorruptionBeginRoundPre"])
                            {
                                yield return Globals.Instance.WaitForSeconds(0.01f);
                            }
                            if (NetworkManager.Instance.netAuxValue != "")
                            {
                                __instance.SetRandomIndex(int.Parse(NetworkManager.Instance.netAuxValue));
                            }
                            Functions.DebugLogGD("CorruptionBeginRoundPre, we can continue!", "net");
                        }
                    }
                    if (__instance.corruptionItem.ItemTarget == Enums.ItemTarget.AllHero || __instance.corruptionItem.ItemTarget == Enums.ItemTarget.RandomHero || __instance.corruptionItem.ItemTarget == Enums.ItemTarget.Self)
                    {
                        if (__instance.corruptionItem.ItemTarget == Enums.ItemTarget.AllHero || __instance.corruptionItem.ItemTarget == Enums.ItemTarget.Self)
                        {
                            Functions.DebugLogGD("corr0", "");
                            for (int n = 0; n < __instance.TeamHero.Length; n++)
                            {
                                if (__instance.TeamHero[n] != null && __instance.TeamHero[n].Alive)
                                {
                                    __instance.TeamHero[n].SetEvent(Enums.EventActivation.CorruptionBeginRound, null, 0, "");
                                }
                            }
                        }
                        else
                        {
                            Functions.DebugLogGD("corr1", "");
                            bool flag2 = false;
                            while (!flag2)
                            {
                                int randomIntRange = __instance.GetRandomIntRange(0, 4, "default", "");
                                if (__instance.TeamHero[randomIntRange] != null && __instance.TeamHero[randomIntRange].Alive)
                                {
                                    __instance.TeamHero[randomIntRange].SetEvent(Enums.EventActivation.CorruptionBeginRound, null, 0, "");
                                    flag2 = true;
                                }
                            }
                        }
                    }
                    else if (__instance.corruptionItem.ItemTarget == Enums.ItemTarget.AllEnemy || __instance.corruptionItem.ItemTarget == Enums.ItemTarget.RandomEnemy || __instance.corruptionItem.ItemTarget == Enums.ItemTarget.SelfEnemy)
                    {
                        if (__instance.corruptionItem.ItemTarget == Enums.ItemTarget.AllEnemy || __instance.corruptionItem.ItemTarget == Enums.ItemTarget.SelfEnemy)
                        {
                            Functions.DebugLogGD("corr2", "");
                            for (int num2 = 0; num2 < 4; num2++)
                            {
                                if (__instance.TeamNPC[num2] != null && __instance.TeamNPC[num2].Alive)
                                {
                                    __instance.TeamNPC[num2].SetEvent(Enums.EventActivation.CorruptionBeginRound, null, 0, "");
                                }
                            }
                        }
                        else
                        {
                            Functions.DebugLogGD("corr3", "");
                            bool flag3 = false;
                            while (!flag3)
                            {
                                int randomIntRange2 = __instance.GetRandomIntRange(0, 4, "default", "");
                                if (__instance.TeamNPC[randomIntRange2] != null && __instance.TeamNPC[randomIntRange2].Alive)
                                {
                                    __instance.TeamNPC[randomIntRange2].SetEvent(Enums.EventActivation.CorruptionBeginRound, null, 0, "");
                                    flag3 = true;
                                }
                            }
                        }
                    }
                    yield return Globals.Instance.WaitForSeconds(0.2f);
                    int i = 0;
                    while (__instance.generatedCardTimes > 0 && i < 200)
                    {
                        yield return Globals.Instance.WaitForSeconds(0.01f);
                        num = i;
                        i = num + 1;
                    }
                    yield return Globals.Instance.WaitForSeconds(0.1f);
                    yield return Globals.Instance.WaitForSeconds(0.1f);
                    int eventExaustCorrupt = 0;
                    while (__instance.eventList.Count > 0)
                    {
                        if (GameManager.Instance.GetDeveloperMode() && eventExaustCorrupt % 50 == 0)
                        {
                            __instance.eventListDebug = "";
                            for (int num3 = 0; num3 < __instance.eventList.Count; num3++)
                            {
                                __instance.eventListDebug = __instance.eventListDebug + __instance.eventList[num3] + " || ";
                            }
                            Functions.DebugLogGD("[CorruptionWAIT] Waiting For Eventlist to clean", "");
                            Functions.DebugLogGD(__instance.eventListDebug, "");
                        }
                        num = eventExaustCorrupt;
                        eventExaustCorrupt = num + 1;
                        if (eventExaustCorrupt > 300)
                        {
                            Functions.DebugLogGD("[CorruptionWAIT] EXAUSTED!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", "");
                            __instance.eventList.Clear();
                            break;
                        }
                        yield return Globals.Instance.WaitForSeconds(0.01f);
                    }
                    if (GameManager.Instance.IsMultiplayer())
                    {
                        Functions.DebugLogGD("**************************", "net");
                        Functions.DebugLogGD("WaitingSyncro CorruptionBeginRound", "net");
                        if (NetworkManager.Instance.IsMaster())
                        {
                            if (__instance.coroutineSyncBeginRound != null)
                            {
                                __instance.StopCoroutine(__instance.coroutineSyncBeginRound);
                            }
                            __instance.coroutineSyncBeginRound = __instance.StartCoroutine(__instance.ReloadCombatCo("CorruptionBeginRound"));
                            while (!NetworkManager.Instance.AllPlayersReady("CorruptionBeginRound"))
                            {
                                yield return Globals.Instance.WaitForSeconds(0.01f);
                            }
                            __instance.StopCoroutine(__instance.coroutineSyncBeginRound);
                            Functions.DebugLogGD("Game ready, Everybody checked CorruptionBeginRound", "net");
                            __instance.SetRandomIndex(__instance.randomIndex);
                            NetworkManager.Instance.PlayersNetworkContinue("CorruptionBeginRound", __instance.randomIndex.ToString());
                        }
                        else
                        {
                            NetworkManager.Instance.SetWaitingSyncro("CorruptionBeginRound", true);
                            NetworkManager.Instance.SetStatusReady("CorruptionBeginRound");
                            while (NetworkManager.Instance.WaitingSyncro["CorruptionBeginRound"])
                            {
                                yield return Globals.Instance.WaitForSeconds(0.01f);
                            }
                            if (NetworkManager.Instance.netAuxValue != "")
                            {
                                __instance.SetRandomIndex(int.Parse(NetworkManager.Instance.netAuxValue));
                            }
                            Functions.DebugLogGD("CorruptionBeginRound, we can continue!", "net");
                        }
                        yield return Globals.Instance.WaitForSeconds(0.1f);
                    }
                }
                Functions.DebugLogGD("[CORRUPTIONBEGINROUND] END", "trace");
                __instance.ClearItemExecuteForThisTurn();
                for (int eventExaustCorrupt = 0; eventExaustCorrupt < __instance.TeamHero.Length; eventExaustCorrupt = num + 1)
                {
                    if (__instance.TeamHero[eventExaustCorrupt] != null && __instance.TeamHero[eventExaustCorrupt].Alive)
                    {
                        __instance.waitExecution = true;
                        __instance.TeamHero[eventExaustCorrupt].BeginRound();
                        eventExaust = 0;
                        while (__instance.waitExecution)
                        {
                            yield return Globals.Instance.WaitForSeconds(0.01f);
                            num = eventExaust;
                            eventExaust = num + 1;
                            if (eventExaust > 400)
                            {
                                Functions.DebugLogGD("[BeginRound] Waitexecution EXAUSTED!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", "trace");
                                __instance.waitExecution = false;
                            }
                        }
                        if (__instance.TeamHero[eventExaustCorrupt].Alive)
                        {
                            __instance.TeamHero[eventExaustCorrupt].SetEvent(Enums.EventActivation.BeginRound, null, 0, "");
                        }
                    }
                    num = eventExaustCorrupt;
                }
                for (int eventExaustCorrupt = 0; eventExaustCorrupt < __instance.TeamNPC.Length; eventExaustCorrupt = num + 1)
                {
                    if (__instance.TeamNPC[eventExaustCorrupt] != null && __instance.TeamNPC[eventExaustCorrupt].Alive)
                    {
                        __instance.waitExecution = true;
                        __instance.TeamNPC[eventExaustCorrupt].BeginRound();
                        eventExaust = 0;
                        while (__instance.waitExecution)
                        {
                            yield return Globals.Instance.WaitForSeconds(0.01f);
                            num = eventExaust;
                            eventExaust = num + 1;
                            if (eventExaust > 400)
                            {
                                Functions.DebugLogGD("[BeginRound] Waitexecution EXAUSTED!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", "trace");
                                __instance.waitExecution = false;
                            }
                        }
                        if (__instance.TeamNPC[eventExaustCorrupt].Alive)
                        {
                            __instance.TeamNPC[eventExaustCorrupt].SetEvent(Enums.EventActivation.BeginRound, null, 0, "");
                        }
                    }
                    num = eventExaustCorrupt;
                }
                yield return Globals.Instance.WaitForSeconds(0.1f);
                if (GameManager.Instance.IsMultiplayer())
                {
                    Functions.DebugLogGD("**************************", "net");
                    Functions.DebugLogGD("WaitingSyncro beginround", "net");
                    if (NetworkManager.Instance.IsMaster())
                    {
                        if (__instance.coroutineSyncBeginRound != null)
                        {
                            __instance.StopCoroutine(__instance.coroutineSyncBeginRound);
                        }
                        __instance.coroutineSyncBeginRound = __instance.StartCoroutine(__instance.ReloadCombatCo("beginround"));
                        while (!NetworkManager.Instance.AllPlayersReady("beginround"))
                        {
                            yield return Globals.Instance.WaitForSeconds(0.01f);
                        }
                        __instance.StopCoroutine(__instance.coroutineSyncBeginRound);
                        Functions.DebugLogGD("Game ready, Everybody checked beginround", "net");
                        __instance.SetRandomIndex(__instance.randomIndex);
                        NetworkManager.Instance.PlayersNetworkContinue("beginround", "");
                    }
                    else
                    {
                        NetworkManager.Instance.SetWaitingSyncro("beginround", true);
                        NetworkManager.Instance.SetStatusReady("beginround");
                        while (NetworkManager.Instance.WaitingSyncro["beginround"])
                        {
                            yield return Globals.Instance.WaitForSeconds(0.01f);
                        }
                        Functions.DebugLogGD("beginround, we can continue!", "net");
                    }
                }
            }
            eventExaust = 0;
            while (__instance.eventList.Count > 0)
            {
                if (GameManager.Instance.GetDeveloperMode() && eventExaust % 50 == 0)
                {
                    __instance.eventListDebug = "";
                    for (int num4 = 0; num4 < __instance.eventList.Count; num4++)
                    {
                        __instance.eventListDebug = __instance.eventListDebug + __instance.eventList[num4] + " || ";
                    }
                    Functions.DebugLogGD("[BeginTurn] Waiting For Eventlist to clean", "");
                    Functions.DebugLogGD(__instance.eventListDebug, "");
                }
                int num = eventExaust;
                eventExaust = num + 1;
                if (eventExaust > 300)
                {
                    Functions.DebugLogGD("[BeginTurn] EXAUSTED!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", "");
                    __instance.eventList.Clear();
                    break;
                }
                yield return Globals.Instance.WaitForSeconds(0.01f);
            }
            __instance.NextTurnContinue2();
            yield break;
        }
    }
    [HarmonyPatch(typeof(MatchManager), "PositionIsMiddle")]
    class PositionIsMiddle
    {
        [HarmonyPrefix]
        static bool setpatch()
        {
            return false;
        }

        [HarmonyPostfix]
        static void postpatch(MatchManager __instance, ref bool __result, Character character)
        {
            if (character == null || !character.Alive)
            {
                __result = false;
            }
            bool flag;
            bool flag2;
            if (!character.IsHero)
            {
                flag = __instance.PositionIsFront(false, character.Position);
                flag2 = __instance.PositionIsBack(character);
                __result = __instance.NumNPCsAlive() <= 2 || (!flag && !flag2);
            }
            flag = __instance.PositionIsFront(true, character.Position);
            flag2 = __instance.PositionIsBack(character);
            if (__instance.NumHeroesAlive() > 2 && (flag || flag2))
            {
                bool flag3 = true;
                for (int i = 0; i < 4; i++)
                {
                    if (character.Id != __instance.TeamHero[i].Id && __instance.TeamHero[i] != null && __instance.TeamHero[i].Alive && !__instance.PositionIsFront(true, __instance.TeamHero[i].Position) && !__instance.PositionIsBack(__instance.TeamHero[i]) && !__instance.TeamHero[i].HasEffect("Stealth"))
                    {
                        flag3 = false;
                        break;
                    }
                }
                __result = flag3;
            }
            __result = true;
        }
    }
    [HarmonyPatch(typeof(MatchManager), "ReloadCombatFullAction")]
    class ReloadCombatFullAction
    {
        [HarmonyPrefix]
        static bool setpatch()
        {
            return false;
        }

        [HarmonyPostfix]
        static void postpatch(MatchManager __instance)
        {
            for (int i = 0; i < __instance.TeamHero.Length; i++)
            {
                if (__instance.TeamHero[i] != null)
                {
                    __instance.TeamHero[i].HpCurrent = __instance.heroLifeArr[i];
                    __instance.TeamHero[i].Alive = true;
                    if (i < __instance.heroBeginItems.Count && __instance.heroBeginItems[i] != null)
                    {
                        List<string> list = __instance.heroBeginItems[i];
                        __instance.TeamHero[i].Weapon = list[0];
                        __instance.TeamHero[i].Armor = list[1];
                        __instance.TeamHero[i].Jewelry = list[2];
                        __instance.TeamHero[i].Accesory = list[3];
                        __instance.TeamHero[i].Pet = list[4];
                    }
                }
            }
            AtOManager.Instance.combatCardDictionary = null;
            AtOManager.Instance.combatGameCode = "";
            __instance.currentGameCode = "";
            __instance.heroDestroyedItemsInThisTurn.Clear();
            __instance.heroBeginItems.Clear();
            __instance.heroLifeArr = null;
            if (GameManager.Instance.IsMultiplayer())
            {
                AtOManager.Instance.DoLoadGameFromMP();
                return;
            }
            AtOManager.Instance.LoadGame(-1);
        }
    }
    [HarmonyPatch(typeof(MatchManager), "SetCharactersPing")]
    class SetCharactersPing
    {
        [HarmonyPrefix]
        static bool setpatch()
        {
            return false;
        }

        [HarmonyPostfix]
        static void postpatch(MatchManager __instance, int _action)
        {
            if (__instance.waitingDeathScreen || __instance.WaitingForActionScreen())
            {
                return;
            }
            if (!__instance.emoteManager.IsBlocked() && __instance.emoteManager.gameObject.activeSelf)
            {
                __instance.emoteManager.HideEmotes();
                if (__instance.emoteManager.EmoteNeedsTarget(_action))
                {
                    __instance.ShowCharactersPing(_action);
                    return;
                }
                if (__instance.emoteManager.heroActive > -1 && __instance.emoteManager.heroActive < 8 && __instance.TeamHero[__instance.emoteManager.heroActive] != null)
                {
                    __instance.EmoteTarget(__instance.TeamHero[__instance.emoteManager.heroActive].Id, _action, -1, false);
                }
            }
        }
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
