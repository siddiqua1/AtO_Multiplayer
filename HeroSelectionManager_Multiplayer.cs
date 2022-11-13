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

class HeroSelectionManager_Multiplayer : MonoBehaviour
{
    //private readonly Harmony harmony = new Harmony(modInfo.modGUID);

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(BeginAdventure));
        modInfo.harmony.PatchAll(typeof(DrawBoxSelectionNames));
        modInfo.harmony.PatchAll(typeof(AllBoxWithHeroes));
        modInfo.harmony.PatchAll(typeof(AddBoxes));
        modInfo.harmony.PatchAll(typeof(StartCo));
    }

    [HarmonyPatch(typeof(HeroSelectionManager), "StartCo")]
    class StartCo {
        [HarmonyPrefix]
        static bool setpatch() {
            //Don't run original at all
            return false;
        }

        [HarmonyPostfix]
        static IEnumerator replace(IEnumerator result, HeroSelectionManager __instance) {
			System.Console.WriteLine($"[HeroSelectionManager StartCo] Replacing function completely");
			__instance.ngValueMaster = (__instance.ngValue = 0);
			__instance.ngCorruptors = "";
			__instance.obeliskMadnessValue = (__instance.obeliskMadnessValueMaster = 0);
			__instance.madnessLevel.text = string.Format(Texts.Instance.GetText("madnessNumber", ""), 0);
			if (GameManager.Instance.IsMultiplayer())
			{
				Debug.Log("WaitingSyncro heroSelection");
				if (NetworkManager.Instance.IsMaster())
				{
					while (!NetworkManager.Instance.AllPlayersReady("heroSelection"))
					{
						yield return Globals.Instance.WaitForSeconds(0.01f);
					}
					Functions.DebugLogGD("Game ready, Everybody checked heroSelection", "");
					if (GameManager.Instance.IsLoadingGame())
					{
						__instance.photonView.RPC("NET_SetLoadingGame", RpcTarget.Others, Array.Empty<object>());
					}
					NetworkManager.Instance.PlayersNetworkContinue("heroSelection", AtOManager.Instance.GetWeekly().ToString());
					yield return Globals.Instance.WaitForSeconds(0.3f);
				}
				else
				{
					GameManager.Instance.SetGameStatus(Enums.GameStatus.NewGame);
					NetworkManager.Instance.SetWaitingSyncro("heroSelection", true);
					NetworkManager.Instance.SetStatusReady("heroSelection");
					while (NetworkManager.Instance.WaitingSyncro["heroSelection"])
					{
						yield return Globals.Instance.WaitForSeconds(0.01f);
					}
					if (NetworkManager.Instance.netAuxValue != "")
					{
						AtOManager.Instance.SetWeekly(int.Parse(NetworkManager.Instance.netAuxValue));
					}
					Functions.DebugLogGD("heroSelection, we can continue!", "");
				}
			}
			if (GameManager.Instance.IsMultiplayer() && !NetworkManager.Instance.IsMaster())
			{
				string roomName = NetworkManager.Instance.GetRoomName();
				if (roomName != "")
				{
					SaveManager.SaveIntoPrefsString("coopRoomId", roomName);
					SaveManager.SavePrefs();
				}
			}
			MadnessManager.Instance.ShowMadness();
			MadnessManager.Instance.RefreshValues("");
			MadnessManager.Instance.ShowMadness();
			__instance.playerHeroSkinsDict = new Dictionary<string, string>();
			__instance.playerHeroCardbackDict = new Dictionary<string, string>();
			__instance.boxSelection = new BoxSelection[__instance.boxGO.Length];
			for (int i = 0; i < __instance.boxGO.Length; i++)
			{
				__instance.boxHero[__instance.boxGO[i]] = null;
				__instance.boxFilled[__instance.boxGO[i]] = false;
				__instance.boxSelection[i] = __instance.boxGO[i].GetComponent<BoxSelection>();
			}
			__instance.ShowDrag(false, Vector3.zero);
			foreach (KeyValuePair<string, SubClassData> keyValuePair in Globals.Instance.SubClass)
			{
				string text = Enum.GetName(typeof(Enums.HeroClass), Globals.Instance.SubClass[keyValuePair.Key].HeroClass).ToLower().Replace(" ", "");
				if (!__instance.subclassDictionary.ContainsKey(text))
				{
					__instance.subclassDictionary.Add(text, new SubClassData[4]);
				}
				__instance.subclassDictionary[text][Globals.Instance.SubClass[keyValuePair.Key].OrderInList] = Globals.Instance.SubClass[keyValuePair.Key];
			}
			__instance._ClassWarriors.color = Functions.HexToColor(Globals.Instance.ClassColor["warrior"]);
			__instance._ClassHealers.color = Functions.HexToColor(Globals.Instance.ClassColor["healer"]);
			__instance._ClassMages.color = Functions.HexToColor(Globals.Instance.ClassColor["mage"]);
			__instance._ClassScouts.color = Functions.HexToColor(Globals.Instance.ClassColor["scout"]);
			__instance._ClassMagicKnights.color = Functions.HexToColor(Globals.Instance.ClassColor["magicknight"]);
			for (int j = 0; j < 4; j++)
			{
				for (int k = 0; k < 4; k++)
				{
					SubClassData subClassData = null;
					GameObject gameObject = null;
					if (j == 0)
					{
						subClassData = __instance.subclassDictionary["warrior"][k];
						gameObject = __instance.warriorsGO;
					}
					else if (j == 1)
					{
						subClassData = __instance.subclassDictionary["scout"][k];
						gameObject = __instance.scoutsGO;
					}
					else if (j == 2)
					{
						subClassData = __instance.subclassDictionary["mage"][k];
						gameObject = __instance.magesGO;
					}
					else if (j == 3)
					{
						subClassData = __instance.subclassDictionary["healer"][k];
						gameObject = __instance.healersGO;
					}
					GameObject gameObject2 = Object.Instantiate<GameObject>(__instance.heroSelectionPrefab, Vector3.zero, Quaternion.identity, gameObject.transform);
					gameObject2.transform.localPosition = new Vector3(0.55f + 1.85f * (float)k, -0.7f, 0f);
					gameObject2.name = subClassData.SubClassName.ToLower();
					HeroSelection component = gameObject2.transform.Find("Portrait").transform.GetComponent<HeroSelection>();
					__instance.heroSelectionDictionary.Add(gameObject2.name, component);
					component.blocked = !PlayerManager.Instance.IsHeroUnlocked(subClassData.Id);
					if (component.blocked && GameManager.Instance.IsObeliskChallenge() && !GameManager.Instance.IsWeeklyChallenge())
					{
						component.blocked = false;
					}
					if (subClassData.Id == "mercenary" || subClassData.Id == "ranger" || subClassData.Id == "elementalist" || subClassData.Id == "cleric")
					{
						component.blocked = false;
					}
					component.SetSubclass(subClassData);
					component.SetSprite(subClassData.SpriteSpeed, subClassData.SpriteBorderSmall, subClassData.SpriteBorderLocked);
					string activeSkin = PlayerManager.Instance.GetActiveSkin(subClassData.Id);
					if (activeSkin != "")
					{
						SkinData skinData = Globals.Instance.GetSkinData(activeSkin);
						__instance.AddToPlayerHeroSkin(subClassData.Id, activeSkin);
						component.SetSprite(skinData.SpritePortrait, skinData.SpriteSilueta, subClassData.SpriteBorderLocked);
					}
					component.SetName(subClassData.CharacterName);
					component.Init();
					if (subClassData.SpriteBorderLocked != null && subClassData.SpriteBorderLocked.name == "regularBorderSmall")
					{
						component.ShowComingSoon();
					}
					__instance.SubclassByName.Add(subClassData.Id, subClassData.SubClassName);
					if (GameManager.Instance.IsWeeklyChallenge())
					{
						component.blocked = true;
					}
				}
			}
			if (!GameManager.Instance.IsObeliskChallenge() && AtOManager.Instance.IsFirstGame() && !GameManager.Instance.IsMultiplayer())
			{
				AtOManager.Instance.SetGameId("ZXCVBNM");
				__instance.heroSelectionDictionary["mercenary"].AssignHeroToBox(__instance.boxGO[0]);
				__instance.heroSelectionDictionary["ranger"].AssignHeroToBox(__instance.boxGO[1]);
				__instance.heroSelectionDictionary["elementalist"].AssignHeroToBox(__instance.boxGO[2]);
				__instance.heroSelectionDictionary["cleric"].AssignHeroToBox(__instance.boxGO[3]);
				yield return Globals.Instance.WaitForSeconds(1f);
				__instance.BeginAdventure();
				yield break;
			}
			__instance.charPopupGO = Object.Instantiate<GameObject>(__instance.charPopupPrefab, new Vector3(0f, 0f, -1f), Quaternion.identity);
			__instance.charPopup = __instance.charPopupGO.GetComponent<CharPopup>();
			__instance.charPopup.HideNow();
			__instance.magicknightsGO.SetActive(false);
			__instance.separator.gameObject.SetActive(true);
			if (!GameManager.Instance.IsWeeklyChallenge())
			{
				__instance.titleGroupDefault.gameObject.SetActive(true);
				__instance.titleWeeklyDefault.gameObject.SetActive(false);
				__instance.weeklyModifiersButton.gameObject.SetActive(false);
				__instance.weeklyT.gameObject.SetActive(false);
			}
			else
			{
				__instance.titleGroupDefault.gameObject.SetActive(false);
				__instance.titleWeeklyDefault.gameObject.SetActive(true);
				__instance.weeklyModifiersButton.gameObject.SetActive(true);
				__instance.weeklyT.gameObject.SetActive(true);
				__instance.setWeekly = true;
				if (!GameManager.Instance.IsLoadingGame())
				{
					AtOManager.Instance.SetWeekly(Functions.GetCurrentWeeklyWeek());
				}
				__instance.weeklyNumber.text = string.Format(Texts.Instance.GetText("weekNumber", ""), AtOManager.Instance.GetWeekly());
			}
			if (!GameManager.Instance.IsObeliskChallenge())
			{
				__instance.madnessButton.gameObject.SetActive(true);
				if (GameManager.Instance.IsMultiplayer())
				{
					if (NetworkManager.Instance.IsMaster())
					{
						if (GameManager.Instance.IsLoadingGame())
						{
							__instance.ngValueMaster = (__instance.ngValue = AtOManager.Instance.GetNgPlus());
							__instance.ngCorruptors = AtOManager.Instance.GetMadnessCorruptors();
							__instance.SetMadnessLevel();
						}
						else if (SaveManager.PrefsHasKey("madnessLevelCoop") && SaveManager.PrefsHasKey("madnessCorruptorsCoop"))
						{
							int num = SaveManager.LoadPrefsInt("madnessLevelCoop");
							string text2 = SaveManager.LoadPrefsString("madnessCorruptorsCoop");
							__instance.ngValueMaster = (__instance.ngValue = num);
							if (text2 != "")
							{
								__instance.ngCorruptors = text2;
							}
							__instance.SetMadnessLevel();
						}
					}
				}
				else if (SaveManager.PrefsHasKey("madnessLevel") && SaveManager.PrefsHasKey("madnessCorruptors"))
				{
					int num2 = SaveManager.LoadPrefsInt("madnessLevel");
					string text3 = SaveManager.LoadPrefsString("madnessCorruptors");
					__instance.ngValueMaster = (__instance.ngValue = num2);
					if (text3 != "")
					{
						__instance.ngCorruptors = text3;
					}
					__instance.SetMadnessLevel();
				}
			}
			else if (!GameManager.Instance.IsWeeklyChallenge())
			{
				__instance.madnessButton.gameObject.SetActive(true);
				if (GameManager.Instance.IsMultiplayer())
				{
					if (NetworkManager.Instance.IsMaster())
					{
						if (GameManager.Instance.IsLoadingGame())
						{
							__instance.obeliskMadnessValue = (__instance.obeliskMadnessValueMaster = AtOManager.Instance.GetObeliskMadness());
							__instance.SetObeliskMadnessLevel();
						}
						else if (SaveManager.PrefsHasKey("obeliskMadnessCoop"))
						{
							int num3 = SaveManager.LoadPrefsInt("obeliskMadnessCoop");
							__instance.obeliskMadnessValue = (__instance.obeliskMadnessValueMaster = num3);
							__instance.SetObeliskMadnessLevel();
						}
					}
				}
				else if (SaveManager.PrefsHasKey("obeliskMadness"))
				{
					int num4 = SaveManager.LoadPrefsInt("obeliskMadness");
					__instance.obeliskMadnessValue = (__instance.obeliskMadnessValueMaster = num4);
					__instance.SetObeliskMadnessLevel();
				}
			}
			else
			{
				__instance.madnessButton.gameObject.SetActive(false);
			}
			__instance.Resize();
			if (GameManager.Instance.IsWeeklyChallenge() && !GameManager.Instance.IsLoadingGame())
			{
				__instance.gameSeedModify.gameObject.SetActive(false);
				ChallengeData weeklyData = Globals.Instance.GetWeeklyData(Functions.GetCurrentWeeklyWeek());
				if (weeklyData != null)
				{
					__instance.heroSelectionDictionary[weeklyData.Hero1.Id].AssignHeroToBox(__instance.boxGO[0]);
					__instance.heroSelectionDictionary[weeklyData.Hero1.Id].blocked = false;
					__instance.heroSelectionDictionary[weeklyData.Hero2.Id].AssignHeroToBox(__instance.boxGO[1]);
					__instance.heroSelectionDictionary[weeklyData.Hero2.Id].blocked = false;
					__instance.heroSelectionDictionary[weeklyData.Hero3.Id].AssignHeroToBox(__instance.boxGO[2]);
					__instance.heroSelectionDictionary[weeklyData.Hero3.Id].blocked = false;
					__instance.heroSelectionDictionary[weeklyData.Hero4.Id].AssignHeroToBox(__instance.boxGO[3]);
					__instance.heroSelectionDictionary[weeklyData.Hero4.Id].blocked = false;
				}
				if (!GameManager.Instance.IsMultiplayer() || NetworkManager.Instance.IsMaster())
				{
					if (weeklyData != null)
					{
						AtOManager.Instance.SetGameId(weeklyData.Seed);
					}
					else
					{
						AtOManager.Instance.SetGameId("");
					}
				}
				GameManager.Instance.SceneLoaded();
			}
			else if (GameManager.Instance.IsLoadingGame() || (AtOManager.Instance.IsFirstGame() && !GameManager.Instance.IsMultiplayer() && !GameManager.Instance.IsObeliskChallenge()))
			{
				__instance.gameSeedModify.gameObject.SetActive(false);
				if (AtOManager.Instance.IsFirstGame())
				{
					AtOManager.Instance.SetGameId("ZXCVBNM");
				}
			}
			else
			{
				if (!GameManager.Instance.IsMultiplayer() || NetworkManager.Instance.IsMaster())
				{
					AtOManager.Instance.SetGameId("");
				}
				__instance.gameSeed.gameObject.SetActive(true);
			}
			if (!GameManager.Instance.IsMultiplayer() || NetworkManager.Instance.IsMaster())
			{
				__instance.SetSeed(AtOManager.Instance.GetGameId(), false);
			}
			if (GameManager.Instance.IsWeeklyChallenge() || (GameManager.Instance.IsObeliskChallenge() && __instance.obeliskMadnessValue > 8))
			{
				__instance.gameSeed.gameObject.SetActive(false);
			}
			__instance.playerHeroPerksDict = new Dictionary<string, List<string>>();
			if (GameManager.Instance.IsMultiplayer())
			{
				__instance.masterDescription.gameObject.SetActive(true);
				if (NetworkManager.Instance.IsMaster())
				{
					__instance.DrawBoxSelectionNames();
					__instance.botonBegin.gameObject.SetActive(true);
					__instance.botonBegin.Disable();
					__instance.botonFollow.transform.parent.gameObject.SetActive(false);
				}
				else
				{
					__instance.gameSeedModify.gameObject.SetActive(false);
					__instance.botonBegin.gameObject.SetActive(false);
					__instance.botonFollow.transform.parent.gameObject.SetActive(true);
					__instance.ShowFollowStatus();
				}
				if (NetworkManager.Instance.IsMaster() && GameManager.Instance.IsLoadingGame())
				{
					for (int l = 0; l < 8; l++)
					{
						System.Console.WriteLine($"[StartCo] get hero {l}");
						Hero hero = AtOManager.Instance.GetHero(l);
						string subclassName = hero.SubclassName;
						int perkRank = hero.PerkRank;
						string skinUsed = hero.SkinUsed;
						string cardbackUsed = hero.CardbackUsed;
						System.Console.WriteLine($"[StartCo] Does my life hurt {l}");
						__instance.AddToPlayerHeroSkin(subclassName, skinUsed);
						System.Console.WriteLine("[StartCo] not yet");
						__instance.AddToPlayerHeroCardback(subclassName, cardbackUsed);
						System.Console.WriteLine($"[StartCo] assign to box {l}");
						__instance.heroSelectionDictionary[subclassName].AssignHeroToBox(__instance.boxGO[l]);
						System.Console.WriteLine($"[StartCo] set rank box {l}");
						__instance.heroSelectionDictionary[subclassName].SetRankBox(perkRank);
						__instance.heroSelectionDictionary[subclassName].SetSkin(skinUsed);
						__instance.photonView.RPC("NET_AssignHeroToBox", RpcTarget.Others, new object[]
						{
						hero.SubclassName.ToLower(),
						l,
						perkRank,
						skinUsed,
						cardbackUsed
						});
					}
				}
			}
			else
			{
				__instance.masterDescription.gameObject.SetActive(false);
				__instance.botonFollow.transform.parent.gameObject.SetActive(false);
				__instance.botonBegin.gameObject.SetActive(true);
				__instance.botonBegin.Disable();
				if (!GameManager.Instance.IsWeeklyChallenge())
				{
					__instance.PreAssign();
				}
			}
			yield return Globals.Instance.WaitForSeconds(0.1f);
			__instance.readyButtonText.gameObject.SetActive(false);
			__instance.readyButton.gameObject.SetActive(false);
			if (GameManager.Instance.IsMultiplayer())
			{
				if (NetworkManager.Instance.IsMaster())
				{
					NetworkManager.Instance.ClearAllPlayerManualReady();
					NetworkManager.Instance.SetManualReady(true);
				}
				else
				{
					__instance.readyButtonText.gameObject.SetActive(true);
					__instance.readyButton.gameObject.SetActive(true);
				}
			}
			GameManager.Instance.SceneLoaded();
			if (!false && !GameManager.Instance.TutorialWatched("characterPerks"))
			{
				foreach (KeyValuePair<string, HeroSelection> keyValuePair2 in __instance.heroSelectionDictionary)
				{
					if (keyValuePair2.Value.perkPointsT.gameObject.activeSelf)
					{
						GameManager.Instance.ShowTutorialPopup("characterPerks", keyValuePair2.Value.perkPointsT.gameObject.transform.position, Vector3.zero);
						break;
					}
				}
			}
			if (GameManager.Instance.IsMultiplayer() && GameManager.Instance.IsLoadingGame() && NetworkManager.Instance.IsMaster())
			{
				bool flag = true;
				List<string> list = new List<string>();
				List<string> list2 = new List<string>();
				for (int m = 0; m < 8; m++)
				{
					Hero hero2 = AtOManager.Instance.GetHero(m);
					if (hero2.OwnerOriginal == null)
					{
						break;
					}
					string text4 = hero2.OwnerOriginal.ToLower();
					if (!list.Contains(text4))
					{
						list.Add(text4);
					}
				}
				foreach (Player player in NetworkManager.Instance.PlayerList)
				{
					string text5 = NetworkManager.Instance.GetPlayerNickReal(player.NickName).ToLower();
					if (!list2.Contains(text5))
					{
						list2.Add(text5);
					}
				}
				if (list.Count != list2.Count)
				{
					flag = false;
				}
				else
				{
					for (int num5 = 0; num5 < list2.Count; num5++)
					{
						if (!list.Contains(list2[num5]))
						{
							flag = false;
							break;
						}
					}
				}
				if (!flag)
				{
					__instance.photonView.RPC("NET_SetNotOriginal", RpcTarget.All, Array.Empty<object>());
				}
			}
			yield break;
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