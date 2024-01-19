using HarmonyLib;
using HMLLibrary;
using RaftModLoader;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


namespace RemoveAnimals
{
    public class Main : Mod
    {
        public static HashSet<string> disallowed = new HashSet<string>();
        Harmony harmony;
        public static bool PreventBeeDamage = false;
        public static bool PreventPufferExplode = false;
        public void Start()
        {
            (harmony = new Harmony("com.aidanamite.RemoveAnimals")).PatchAll();
            Log("Mod has been loaded!");
        }

        public void OnModUnload()
        {
            harmony?.UnpatchAll(harmony.Id);
            Log("Mod has been unloaded!");
        }

        public void UpdateComboboxContents()
        {
            var a = new string[disallowed.Count + 1];
            a[0] = "None";
            var j = 1;
            foreach (var i in disallowed)
                a[j++] = i;
            Array.Sort(a, 1, disallowed.Count);
            ExtraSettingsAPI_SetComboboxContent("removeCombo", a);
            a = Enum.GetNames(typeof(AI_NetworkBehaviourType));
            var a2 = new List<string>();
            a2.Add("None");
            foreach (var i in a)
                if (i != "None" && !disallowed.Contains(i) && !i.StartsWith("NPC_"))
                    a2.Add(i);
            a = a2.ToArray();
            Array.Sort(a, 1, a.Length - 1);
            ExtraSettingsAPI_SetComboboxContent("addCombo", a);
            ExtraSettingsAPI_SetDataValues("disallowed", disallowed.ToDictionary(x => x, x => ""));
        }

        public void ExtraSettingsAPI_Load()
        {
            ExtraSettingsAPI_SetButtons("switch", new[] { "\\/", "/\\" });
            disallowed.Clear();
            foreach (var i in ExtraSettingsAPI_GetDataNames("disallowed"))
                disallowed.Add(i);
            UpdateComboboxContents();
            ExtraSettingsAPI_SettingsClose();
        }

        public void ExtraSettingsAPI_SettingsClose()
        {
            PreventBeeDamage = ExtraSettingsAPI_GetCheckboxState("beeDamage");
            PreventPufferExplode = ExtraSettingsAPI_GetCheckboxState("pufferExplode");
        }

        public void ExtraSettingsAPI_ButtonPress(string SettingName, int Index)
        {
            if (SettingName == "switch")
            {
                var i = ExtraSettingsAPI_GetComboboxSelectedItem(Index == 0 ? "addCombo" : "removeCombo");
                if (i == "None")
                    return;
                if (Index == 0)
                    disallowed.Add(i);
                else
                    disallowed.Remove(i);
                UpdateComboboxContents();
                ExtraSettingsAPI_SetComboboxSelectedIndex(Index == 0 ? "addCombo" : "removeCombo",0);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool ExtraSettingsAPI_GetCheckboxState(string SettingName) => false;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ExtraSettingsAPI_SetComboboxSelectedIndex(string SettingName, int value) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ExtraSettingsAPI_GetComboboxSelectedItem(string SettingName) => "";

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string[] ExtraSettingsAPI_GetDataNames(string SettingName) => new string[0];

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ExtraSettingsAPI_SetComboboxContent(string SettingName, string[] value) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ExtraSettingsAPI_SetDataValues(string SettingName, Dictionary<string, string> values) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ExtraSettingsAPI_SetButtons(string SettingName, string[] buttons) { }
    }

    [HarmonyPatch(typeof(AI_NetworkBehaviour), "Update")]
    static class Patch_NetworkBehaviourUpdate
    {
        static ConditionalWeakTable<AI_NetworkBehaviour, empty> req = new ConditionalWeakTable<AI_NetworkBehaviour, empty>();
        static void Prefix(AI_NetworkBehaviour __instance)
        {
            if (__instance is AI_NetworkBehaviour_Animal && Main.disallowed.Contains( __instance.behaviourType.ToString()) && !req.TryGetValue(__instance,out _))
            {
                req.GetOrCreateValue(__instance);
                NetworkIDManager.SendIDBehaviourDead(__instance.ObjectIndex, typeof(AI_NetworkBehaviour), true);
            }
        }
        class empty { }
    }

    [HarmonyPatch(typeof(AI_NetworkBehaviour_BugSwarm), "DamageClosestPlayer")]
    static class Patch_BugSwarmAttack
    {
        static bool Prefix() => !Main.PreventBeeDamage;
    }

    [HarmonyPatch(typeof(AI_State_PufferFish_CirculateWater), "PlayerIsWithinRange")]
    static class Patch_PufferfishCanSeePlayer
    {
        static void Postfix(ref bool __result)
        {
            if (Main.PreventPufferExplode)
                __result = false;
        }
    }
}