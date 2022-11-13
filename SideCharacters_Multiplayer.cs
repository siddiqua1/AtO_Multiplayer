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


class SideCharacters_Multiplayer : MonoBehaviour
{
    //private readonly Harmony harmony = new Harmony(modInfo.modGUID);

    public static void patch()
    {
        modInfo.harmony.PatchAll(typeof(Awake));
		modInfo.harmony.PatchAll(typeof(Show));
		modInfo.harmony.PatchAll(typeof(InCharacterScreen));
		modInfo.harmony.PatchAll(typeof(Hide));
		modInfo.harmony.PatchAll(typeof(Resize));
		modInfo.harmony.PatchAll(typeof(Refresh));
		modInfo.harmony.PatchAll(typeof(RefreshCards));
		modInfo.harmony.PatchAll(typeof(ShowChallengeButtons));
		modInfo.harmony.PatchAll(typeof(ShowUpgrade));
		modInfo.harmony.PatchAll(typeof(ResetCharacters));
		modInfo.harmony.PatchAll(typeof(EnableOwnedCharacters));
		modInfo.harmony.PatchAll(typeof(ShowLevelUpCharacters));
		modInfo.harmony.PatchAll(typeof(SetActive));
		modInfo.harmony.PatchAll(typeof(GetFirstEnabledCharacter));
		modInfo.harmony.PatchAll(typeof(EnableAll));
	}

    [HarmonyPatch(typeof(SideCharacters), "Awake")]
    class Awake {
        [HarmonyPrefix]
        static bool setpatch(SideCharacters __instance) {
            Array.Resize<OverCharacter>(ref __instance.charArray, 8);
			Array.Resize<Transform>(ref __instance.charTransforms, 8);

			for (int i = 4; i < 8; i++) {
				System.Console.WriteLine($"[SideCharacters Awake] Expanding charArray {i}");
				OverCharacter tmp = Instantiate(__instance.charArray[i - 4]);
				tmp.gameObject.transform.SetParent(__instance.charArray[i - 4].gameObject.transform.parent, true);
				__instance.charArray[i] = tmp;
			}

            for (int i = 0; i < 8; i++)
            {
				System.Console.WriteLine($"[SideCharacters Awake] Setting transforms {i}");
				__instance.charTransforms[i] = __instance.charArray[i].transform;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(SideCharacters), "Show")]
    class Show
    {
        [HarmonyPrefix]
        static bool setpatch(SideCharacters __instance)
        {
			__instance.Resize();
			int i = 0;
			while (i < 8)
			{
				if (!(AtOManager.Instance.currentMapNode == "tutorial_0") && !(AtOManager.Instance.currentMapNode == "tutorial_1"))
				{
					goto IL_76;
				}
				if (i != 1 && i != 2)
				{
					if (i == 3)
					{
						__instance.charArray[i].transform.localPosition = new Vector3(0f, -1.24f * Globals.Instance.multiplierY, 0f);
						goto IL_76;
					}
					goto IL_76;
				}
				IL_A4:
				i++;
				continue;
				IL_76:
				__instance.charArray[i].gameObject.SetActive(true);
				__instance.charArray[i].Init(i);
				__instance.charArray[i].Enable();
				goto IL_A4;
			}
			if (TownManager.Instance || ChallengeSelectionManager.Instance || EventManager.Instance)
			{
				__instance.InCharacterScreen(true);
			}
			return false;
		}
    }

	[HarmonyPatch(typeof(SideCharacters), "InCharacterScreen")]
	class InCharacterScreen {

		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance, ref bool state) {
			for (int i = 0; i < 8; i++)
			{
				__instance.charArray[i].InCharacterScreen(state);
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "Hide")]
	class Hide
	{

		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance)
		{
			if (MatchManager.Instance)
			{
				for (int i = 0; i < 8; i++)
				{
					__instance.charArray[i].gameObject.SetActive(false);
				}
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "Resize")]
	class Resize : MonoBehaviour
	{

		[HarmonyPostfix]
		static void setpatch(SideCharacters __instance)
		{
			for (int i = 0; i < 8; i++)
			{
				__instance.charTransforms[i].localPosition = new Vector3(0f, (float)i * -1.24f * Globals.Instance.multiplierY, 0f);
			}
			;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "Refresh")]
	class Refresh : MonoBehaviour
	{

		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance)
		{
			if (AtOManager.Instance.currentMapNode == "tutorial_0" || AtOManager.Instance.currentMapNode == "tutorial_1" || AtOManager.Instance.currentMapNode == "tutorial_2")
			{
				__instance.Show();
			}
			else
			{
				for (int i = 0; i < 8; i++)
				{
					__instance.charArray[i].Init(i);
				}
			}
			if (__instance.heroActive > -1)
			{
				__instance.charArray[__instance.heroActive].SetActive(true);
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "RefreshCards")]
	class RefreshCards : MonoBehaviour
	{

		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance, int hero = -1)
		{
			for (int i = 0; i < 8; i++)
			{
				if (hero == -1 || hero == i)
				{
					__instance.charArray[i].DoCards();
				}
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "ShowChallengeButtons")]
	class ShowChallengeButtons : MonoBehaviour
	{
		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance, int hero = -1, bool state = true)
		{
			for (int i = 0; i < 8; i++)
			{
				if (hero == -1 || hero == i)
				{
					__instance.charArray[i].ShowChallengeButtonReady(state);
				}
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "ShowUpgrade")]
	class ShowUpgrade : MonoBehaviour
	{
		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance, int hero = -1)
		{
			for (int i = 0; i < 8; i++)
			{
				if (hero == -1 || hero == i)
				{
					__instance.charArray[i].ShowUpgrade();
				}
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "ResetCharacters")]
	class ResetCharacters : MonoBehaviour
	{
		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance)
		{
			for (int i = 0; i < 8; i++)
			{
				__instance.charArray[i].Enable();
				__instance.charArray[i].SetActive(false);
				__instance.charArray[i].SetClickable(true);
				__instance.charArray[i].SetClickable(true);
			}
			__instance.ShowLevelUpCharacters();
			__instance.heroActive = -1;
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "EnableOwnedCharacters")]
	class EnableOwnedCharacters : MonoBehaviour
	{
		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance, bool clickable = true)
		{
			Hero[] team = AtOManager.Instance.GetTeam();
			string playerNick = NetworkManager.Instance.GetPlayerNick();
			for (int i = 0; i < 8; i++)
			{
				if (team[i].Owner == null || team[i].Owner == "" || team[i].Owner == playerNick)
				{
					__instance.charArray[i].Enable();
					__instance.charArray[i].SetClickable(clickable);
					if (clickable)
					{
						__instance.charArray[i].EnableCards(false);
					}
					else
					{
						__instance.charArray[i].EnableCards(true);
					}
				}
				else
				{
					__instance.charArray[i].Disable();
					__instance.charArray[i].EnableCards(false);
				}
				__instance.charArray[i].Enable();
				__instance.charArray[i].SetClickable(true);
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "ShowLevelUpCharacters")]
	class ShowLevelUpCharacters : MonoBehaviour
	{
		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance)
		{
			for (int i = 0; i < 8; i++)
			{
				__instance.charArray[i].ShowLevelUp();
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "SetActive")]
	class SetActive : MonoBehaviour
	{
		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance, int characterIndex)
		{
			for (int i = 0; i < 8; i++)
			{
				if (i != characterIndex)
				{
					__instance.charArray[i].SetActive(false);
				}
				else
				{
					__instance.charArray[i].SetActive(true);
				}
			}
			__instance.heroActive = characterIndex;
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "GetFirstEnabledCharacter")]
	class GetFirstEnabledCharacter : MonoBehaviour
	{
		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance, ref int __result)
		{
			string playerNick = NetworkManager.Instance.GetPlayerNick();
			Hero[] team = AtOManager.Instance.GetTeam();
			for (int i = 0; i < 8; i++)
			{
				if (team[i].Owner == null || team[i].Owner == "" || team[i].Owner == playerNick)
				{
					__result = i;
					return false;
				}
			}
			__result = 0;
			return false;
		}
	}

	[HarmonyPatch(typeof(SideCharacters), "EnableAll")]
	class EnableAll : MonoBehaviour
	{
		[HarmonyPrefix]
		static bool setpatch(SideCharacters __instance)
		{
			for (int i = 0; i < 4; i++)
			{
				__instance.charArray[i].Enable();
			}
			return false;
		}
	}
}