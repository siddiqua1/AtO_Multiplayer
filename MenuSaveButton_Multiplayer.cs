using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
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


class MenuSaveButton_Multiplayer : MonoBehaviour
{

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(Awake));
		modInfo.harmony.PatchAll(typeof(SetGameData));
	}

    [HarmonyPatch(typeof(MenuSaveButton), "Awake")]
    class Awake {
        [HarmonyPrefix]
        static void setpatch(MenuSaveButton __instance)
        {
            if (__instance.imgHero.Length == 4) {
                Array.Resize<Image>(ref __instance.imgHero, 8);
				for (int i = 4; i < 8; i++) {
					Image tmp = Instantiate(__instance.imgHero[i - 4]);
					tmp.gameObject.transform.SetParent(__instance.imgHero[i - 4].gameObject.transform.parent, true);
					__instance.imgHero[i] = tmp;
				}
            }
        }
    }

    [HarmonyPatch(typeof(MenuSaveButton), "SetGameData")]
    class SetGameData :MonoBehaviour {

        [HarmonyPrefix]
        static bool setpatch(MenuSaveButton __instance ,ref GameData _gameData) {
			__instance.gameData = _gameData;
			if (__instance.gameData.Version == null)
			{
				__instance.gameData.Version = "0.6.82";
			}
			if (__instance.CheckIfSavegameIsCompatible(__instance.gameData.Version) != "")
			{
				__instance.GetComponent<Button>().interactable = false;
				__instance.incompatibleT.gameObject.SetActive(true);
			}
			else
			{
				__instance.GetComponent<Button>().interactable = true;
				__instance.incompatibleT.gameObject.SetActive(false);
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("v.");
			stringBuilder.Append(__instance.gameData.Version);
			__instance.versionText.text = stringBuilder.ToString();
			stringBuilder.Clear();
			NodeData nodeData = Globals.Instance.GetNodeData(_gameData.CurrentMapNode);
			if (_gameData.GameType == Enums.GameType.Adventure)
			{
				if (nodeData != null && nodeData.NodeZone != null)
				{
					stringBuilder.Append(nodeData.NodeName);
					stringBuilder.Append(" <voffset=3><color=#666>|</color></voffset> <color=#AAA>");
					if (Globals.Instance.ZoneDataSource.ContainsKey(nodeData.NodeZone.ZoneId.ToLower()))
					{
						stringBuilder.Append(Texts.Instance.GetText(Globals.Instance.ZoneDataSource[nodeData.NodeZone.ZoneId.ToLower()].ZoneName, ""));
					}
					else
					{
						stringBuilder.Append(Texts.Instance.GetText(nodeData.NodeZone.ZoneId.ToLower(), ""));
					}
					int num = _gameData.TownTier + 1;
					if (num > 4)
					{
						num = 4;
					}
					string text = string.Format(Texts.Instance.GetText("actNumber", ""), num);
					stringBuilder.Append(" <size=-2>(");
					stringBuilder.Append(text);
					stringBuilder.Append(")</size>");
					stringBuilder.Append("</color>");
				}
			}
			else if (_gameData.GameType == Enums.GameType.WeeklyChallenge)
			{
				stringBuilder.Append(string.Format(Texts.Instance.GetText("weekNumber", ""), _gameData.Weekly));
				stringBuilder.Append(" <voffset=3><color=#666>|</color></voffset> <color=#AAA>");
				if (nodeData != null && nodeData.NodeZone != null)
				{
					if (nodeData.NodeZone.ObeliskLow)
					{
						stringBuilder.Append(Texts.Instance.GetText("lowerObelisk", ""));
					}
					else if (nodeData.NodeZone.ObeliskHigh)
					{
						stringBuilder.Append(Texts.Instance.GetText("upperObelisk", ""));
					}
					else
					{
						stringBuilder.Append(Texts.Instance.GetText("finalObelisk", ""));
					}
				}
				stringBuilder.Append("</color>");
			}
			else if (nodeData != null && nodeData.NodeZone != null)
			{
				if (nodeData.NodeZone.ObeliskLow)
				{
					stringBuilder.Append(Texts.Instance.GetText("lowerObelisk", ""));
				}
				else if (nodeData.NodeZone.ObeliskHigh)
				{
					stringBuilder.Append(Texts.Instance.GetText("upperObelisk", ""));
				}
				else
				{
					stringBuilder.Append(Texts.Instance.GetText("finalObelisk", ""));
				}
			}
			string[] array = _gameData.GameDate.Split(' ', (char)StringSplitOptions.None);
			stringBuilder.Append("   <nobr><size=-2><color=#ffffff>");
			stringBuilder.Append(array[0]);
			stringBuilder.Append("</color> <color=#aaaaaa>");
			stringBuilder.Append(array[1]);
			stringBuilder.Append("</color></nobr>");
			__instance.descriptionText.text = stringBuilder.ToString();
			int num2 = _gameData.NgPlus;
			string madnessCorruptors = _gameData.MadnessCorruptors;
			int num3 = MadnessManager.Instance.CalculateMadnessTotal(num2, madnessCorruptors);
			if (num3 == 0)
			{
				int obeliskMadness = _gameData.ObeliskMadness;
				num3 = _gameData.ObeliskMadness;
			}
			if (num3 > 0)
			{
				__instance.ShowNGPlus(true);
				if (madnessCorruptors == null)
				{
				}
				__instance.madnessText.text = "M" + num3.ToString();
			}
			if (__instance.gameData.GameMode == Enums.GameMode.Multiplayer)
			{
				stringBuilder.Clear();
				if (_gameData.Owner0 != null && _gameData.Owner0 != "")
				{
					int num4 = 0;
					string text2 = _gameData.Owner0;
					foreach (KeyValuePair<string, string> keyValuePair in _gameData.PlayerNickRealDict)
					{
						if (keyValuePair.Value == text2)
						{
							break;
						}
						num4++;
					}
					stringBuilder.Append("<color=");
					stringBuilder.Append(NetworkManager.Instance.ColorFromPosition(num4));
					stringBuilder.Append(">");
					stringBuilder.Append(_gameData.Owner0);
					stringBuilder.Append("</color>");
					__instance.playerText[0].text = stringBuilder.ToString();
					text2 = _gameData.Owner1;
					num4 = 0;
					foreach (KeyValuePair<string, string> keyValuePair2 in _gameData.PlayerNickRealDict)
					{
						if (keyValuePair2.Value == text2)
						{
							break;
						}
						num4++;
					}
					stringBuilder.Clear();
					stringBuilder.Append("<color=");
					stringBuilder.Append(NetworkManager.Instance.ColorFromPosition(num4));
					stringBuilder.Append(">");
					stringBuilder.Append(_gameData.Owner1);
					stringBuilder.Append("</color>");
					__instance.playerText[1].text = stringBuilder.ToString();
					text2 = _gameData.Owner2;
					num4 = 0;
					foreach (KeyValuePair<string, string> keyValuePair3 in _gameData.PlayerNickRealDict)
					{
						if (keyValuePair3.Value == text2)
						{
							break;
						}
						num4++;
					}
					stringBuilder.Clear();
					stringBuilder.Append("<color=");
					stringBuilder.Append(NetworkManager.Instance.ColorFromPosition(num4));
					stringBuilder.Append(">");
					stringBuilder.Append(_gameData.Owner2);
					stringBuilder.Append("</color>");
					__instance.playerText[2].text = stringBuilder.ToString();
					text2 = _gameData.Owner3;
					num4 = 0;
					foreach (KeyValuePair<string, string> keyValuePair4 in _gameData.PlayerNickRealDict)
					{
						if (keyValuePair4.Value == text2)
						{
							break;
						}
						num4++;
					}
					stringBuilder.Clear();
					stringBuilder.Append("<color=");
					stringBuilder.Append(NetworkManager.Instance.ColorFromPosition(num4));
					stringBuilder.Append(">");
					stringBuilder.Append(_gameData.Owner3);
					stringBuilder.Append("</color>");
					__instance.playerText[3].text = stringBuilder.ToString();
					for (int i = 0; i < 4; i++)
					{
						__instance.playerText[i].gameObject.SetActive(true);
					}
					goto IL_830;
				}
				List<string> list = new List<string>();
				if (_gameData.PlayerNickRealDict == null)
				{
					goto IL_830;
				}
				int num5 = 0;
				for (int j = 0; j < 4; j++)
				{
					__instance.playerText[j].gameObject.SetActive(false);
				}
				using (Dictionary<string, string>.Enumerator enumerator = _gameData.PlayerNickRealDict.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<string, string> keyValuePair5 = enumerator.Current;
						if (!list.Contains(keyValuePair5.Value))
						{
							stringBuilder.Append("<color=");
							stringBuilder.Append(NetworkManager.Instance.ColorFromPosition(num5));
							stringBuilder.Append(">");
							stringBuilder.Append(keyValuePair5.Value);
							stringBuilder.Append("</color>, ");
							num5++;
							list.Add(keyValuePair5.Value);
						}
					}
					goto IL_830;
				}
			}
			for (int k = 0; k < 4; k++)
			{
				__instance.playerText[k].gameObject.SetActive(false);
			}
			IL_830:
			Hero[] array2 = JsonHelper.FromJson<Hero>(_gameData.TeamAtO);
			for (int l = 0; l < array2.Length; l++)
			{
				if (array2[l].SubclassName != null && Globals.Instance.GetSubClassData(array2[l].SubclassName) != null)
				{
					
					SkinData skinData;
					if (array2[l].SkinUsed == null || array2[l].SkinUsed == "")
					{
						skinData = Globals.Instance.GetSkinData(Globals.Instance.GetSkinBaseIdBySubclass(array2[l].SubclassName));
					}
					else
					{
						skinData = Globals.Instance.GetSkinData(array2[l].SkinUsed);
					}
					
					if (skinData != null)
					{
						__instance.imgHero[l].sprite = skinData.SpritePortrait;
					}
					else
					{
						__instance.imgHero[l].sprite = Globals.Instance.GetSubClassData(array2[l].SubclassName).SpriteSpeed;
					}
				}
			}
			return false;
		}
    }
}
