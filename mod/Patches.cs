using HarmonyLib;
using ORKFramework.Behaviours;
using ORKFramework;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Text;
using System.Linq;

namespace ArchipelagoHylics2
{
    // patch ItemCollector to send location checks depending on scene name and ID
    [HarmonyPatch(typeof(ItemCollector), "CollectionFinished")]
    class CollectionFinished_Patch
    {
        public static void Postfix(ItemCollector __instance)
        {
            //Debug.Log("CollectionFinished: " + __instance.name + " in " + SceneManager.GetActiveScene().name + " with Scene ID " + __instance.sceneID);
            //Debug.Log("Scene: " + __instance.GameObject.scene.name.ToString() + " | ID: " + __instance.sceneID);
            long? location = APState.IdentifyItemCheck(__instance.GameObject.scene.name.ToString(), __instance.sceneID);
            if (location.HasValue)
            {
                APState.ServerData.@checked.Add(location.Value);
                switch (__instance.GameObject.scene.name)
                {
                    case "StartHouse_Room1":
                        APState.ServerData.checked_waynehouse++;
                        break;
                    case "Afterlife_Island":
                        APState.ServerData.checked_afterlife++;
                        break;
                    case "Town_Scene_WithAdditions":
                        APState.ServerData.checked_new_muldul++;
                        break;
                    case "Town_VaultOnly":
                        APState.ServerData.checked_new_muldul_vault++;
                        break;
                    case "BanditFort_Scene":
                        APState.ServerData.checked_viewaxs_edifice++;
                        break;
                    case "LD44 Scene":
                        APState.ServerData.checked_arcade1++;
                        break;
                    case "SecondArcade_Scene":
                        APState.ServerData.checked_arcade_island++;
                        break;
                    case "LD44_ChibiScene2_TheCarpetScene":
                        APState.ServerData.checked_arcade2++;
                        break;
                    case "SomsnosaHouse_Scene":
                        APState.ServerData.checked_juice_ranch++;
                        break;
                    case "MazeScene1":
                        APState.ServerData.checked_worm_pod++;
                        break;
                    case "Foglast_Exterior_Dry":
                    case "Foglast_SageRoom_Scene":
                        APState.ServerData.checked_foglast++;
                        break;
                    case "DrillCastle":
                        APState.ServerData.checked_drill_castle++;
                        break;
                    case "Dungeon_Labyrinth_Scene_Final":
                    case "ThirdSageBeach_Scene":
                        APState.ServerData.checked_sage_labyrinth++;
                        break;
                    case "BigAirship_Scene":
                        APState.ServerData.checked_sage_airship++;
                        break;
                    case "FlyingPalaceDungeon_Scene":
                        APState.ServerData.checked_hylemxylem++;
                        break;
                    default: break;
                }
            }

            if (APState.Authenticated)
            {
                if (location.HasValue)
                {
                    Debug.Log("Sending location check: " + location.Value);
                    APState.Session.Locations.CompleteLocationChecks(location.Value);
                }
            }
        }
    }

    // patch EventInteraction to send location checks after completing certain events and with certain outcomes
    [HarmonyPatch(typeof(EventInteraction), "EventEnded")]
    class EventEnded_Patch
    {
        public static void Postfix(EventInteraction __instance)
        {
            switch (__instance.eventAsset.name)
            {
                case "Learn_PoromericBleb_Event": // Waynehouse TV
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200627);
                    APState.ServerData.@checked.Add(200627);
                    APState.ServerData.checked_waynehouse++;
                    Debug.Log("Sending location check: 200627");
                    break;

                case "CaveMinerJuiceSpeechEvent": // Give Juice to Cave Miner
                    if (ORK.Game.Variables.Check("MinerJuiceGiven_Variable", true))
                    {
                        if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200638);
                        APState.ServerData.@checked.Add(200638);
                        APState.ServerData.checked_new_muldul++;
                        Debug.Log("Sending location check: 200638");
                    }
                    break;

                case "LearnSmallFire": // New Muldul TV
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200642);
                    APState.ServerData.@checked.Add(200642);
                    APState.ServerData.checked_new_muldul++;
                    Debug.Log("Sending location check: 200642");
                    break;

                case "Learn_Nematode_Event": // Drill Castle TV
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200712);
                    APState.ServerData.@checked.Add(200712);
                    APState.ServerData.checked_drill_castle++;
                    Debug.Log("Sending location check: 200712");
                    break;

                case "Dedusmuln_Join_Event": // Talk to Dedusmuln in Viewax's Edifice
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200653);
                    APState.ServerData.@checked.Add(200653);
                    APState.ServerData.checked_viewaxs_edifice++;
                    Debug.Log("Sending location check: 200653");
                    if (APState.ServerData.party_shuffle)
                    {
                        if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200654);
                        APState.ServerData.@checked.Add(200654);
                        APState.ServerData.checked_viewaxs_edifice++;
                        Debug.Log("Sending location check: 200654");
                    }
                    break;

                case "FirstSage_Event": // Talk to Sage in Viewax's Edifice
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200662, 200663);
                    APState.ServerData.@checked.Add(200662);
                    APState.ServerData.@checked.Add(200663);
                    APState.ServerData.checked_viewaxs_edifice += 2;
                    Debug.Log("Sending location check: 200662, 200663");
                    break;

                case "BanditFort_Boss_DialogueEvent": // Defeat Viewax
                    ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(5, 1), false, false, false);
                    ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(10, 2), false, false, false);
                    ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(12, 5), false, false, false);
                    ORK.Game.ActiveGroup.Leader.Inventory.AddMoney(0, 25, false, false);
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200665);
                    APState.ServerData.@checked.Add(200665);
                    APState.ServerData.checked_viewaxs_edifice++;
                    Debug.Log("Sending location check: 200665");
                    break;

                case "LearnTimeSigil": // Viewax's Edifice TV
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200666);
                    APState.ServerData.@checked.Add(200666);
                    APState.ServerData.checked_viewaxs_edifice++;
                    Debug.Log("Sending location check: 200666");
                    break;

                case "KingDialogue_Revamp_Event": // Speak to Blerol after rescuing him from Viewax's jail
                    if (ORK.Game.Variables.GetFloat("SpokeXTimesAfterBandits") == 1)
                    {
                        if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200645);
                        APState.ServerData.@checked.Add(200645);
                        APState.ServerData.checked_blerol1 = 1;
                        Debug.Log("Sending location check: 200645");
                    }
                    else if (ORK.Game.Variables.GetFloat("SpokeXTimesAfterBandits") == 4)
                    {
                        if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200646);
                        APState.ServerData.@checked.Add(200646);
                        APState.ServerData.checked_blerol2 = 1;
                        Debug.Log("Sending location check: 200646");
                    }
                    break;

                case "TransformedKingDialogueEvent": // Speak to Blerol after Hylemxylem has been built
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200645, 200646);
                    if (!APState.ServerData.@checked.Contains(200645)) APState.ServerData.@checked.Add(200645);
                    if (!APState.ServerData.@checked.Contains(200646)) APState.ServerData.@checked.Add(200646);
                    Debug.Log("Sending location check: 200645, 200646");
                    break;

                case "LearnCharge_Event": // TV Island TV
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200683);
                    APState.ServerData.@checked.Add(200683);
                    APState.ServerData.checked_tv_island++;
                    Debug.Log("Sending location check: 200683");
                    break;

                case "Farmer_Gift_Event": // Talk to Farmer in Juice Ranch
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200687);
                    APState.ServerData.@checked.Add(200687);
                    APState.ServerData.checked_juice_ranch++;
                    Debug.Log("Sending location check: 200687");
                    break;

                case "SomsnosaHouse_PostBattle_AutoRunEvent": // Finish battle with Somsnosa
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200688);
                    APState.ServerData.@checked.Add(200688);
                    APState.ServerData.checked_juice_ranch++;
                    Debug.Log("Sending location check: 200688");
                    if (APState.ServerData.party_shuffle)
                    {
                        if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200689);
                        APState.ServerData.@checked.Add(200689);
                        APState.ServerData.checked_juice_ranch++;
                        Debug.Log("Sending location check: 200689");
                    }
                    break;

                case "AirshipDialogueEvent_Somsnosa": // Talk to Somsnosa in Airship after Worm Room
                    if (ORK.Game.Variables.Check("WormDefeated", true))
                    {
                        if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200675);
                        APState.ServerData.@checked.Add(200675);
                        APState.ServerData.checked_airship = 1;
                        Debug.Log("Sending location check: 200675");
                    }
                    break;

                case "LearnFateSandbox": // Juice Ranch TV
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200691);
                    APState.ServerData.@checked.Add(200691);
                    APState.ServerData.checked_juice_ranch++;
                    Debug.Log("Sending location check: 200691");
                    break;

                case "LearnTeledenudate": // Afterlife TV
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200631);
                    APState.ServerData.@checked.Add(200631);
                    APState.ServerData.checked_afterlife++;
                    Debug.Log("Sending location check: 200631");
                    break;

                case "ClickerSellerEvent": // Buy Clicker for Foglast TV
                    if (ORK.Game.ActiveGroup.Leader.Inventory.Has(new ItemShortcut(55, 1)))
                    {
                        if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200696);
                        APState.ServerData.@checked.Add(200696);
                        APState.ServerData.checked_foglast++;
                    Debug.Log("Sending location check: 200696");
                    }
                    break;

                case "LearnDialMollusc": // Foglast TV
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200697);
                    APState.ServerData.@checked.Add(200697);
                    APState.ServerData.checked_foglast++;
                    Debug.Log("Sending location check: 200697");
                    break;

                case "FoglastSageEvent": // Talk to Sage in Foglast
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200705, 200706);
                    APState.ServerData.@checked.Add(200705);
                    APState.ServerData.@checked.Add(200706);
                    APState.ServerData.checked_foglast += 2;
                    Debug.Log("Sending location check: 200705, 200706");
                    break;

                case "ThirdSage_Event": // Talk to Sage in Sage Labyrinth
                    if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200726, 200727);
                    APState.ServerData.@checked.Add(200726);
                    APState.ServerData.@checked.Add(200727);
                    APState.ServerData.checked_sage_labyrinth += 2;
                    Debug.Log("Sending location check: 200726, 200727");
                    break;

                case "LearnSageSpell_Event": // Sage Airship TV
                    if (ORK.Game.Variables.Check("SageTokenCounter", 3))
                    {
                        if (APState.Authenticated) APState.Session.Locations.CompleteLocationChecks(200735);
                        APState.ServerData.@checked.Add(200735);
                        APState.ServerData.checked_sage_airship++;
                        Debug.Log("Sending location check: 200735");
                    }
                    break;

                default:
                    break;
            }
        }
    }

    // edge case patch for talking to Pongorma in New Muldul for the first time since for some reason the EventEnded patch doesn't work
    [HarmonyPatch(typeof(EventInteraction), "OnDestroy")]
    class OnDestroy_Patch
    {
        public static void Postfix(EventInteraction __instance)
        {
            if (APState.Authenticated)
            {
                if (__instance.eventAsset.name == "PongormaJoinEvent" && ORK.Game.Variables.Check("Pongorma_Joined", true))
                {
                    APState.Session.Locations.CompleteLocationChecks(200643);
                    APState.ServerData.checked_pongorma = 1;
                    Debug.Log("Sending location check: 200643");
                    if (APState.ServerData.party_shuffle)
                    {
                        APState.Session.Locations.CompleteLocationChecks(200644);
                        APState.ServerData.checked_pongorma = 2;
                        Debug.Log("Sending location check: 200644");
                    }
                }
            }
        }
    }

    // save files go from 0 to 2
    // save -2 is autosave 1, save -3 is autosave 2

    // save relevant info to a json file
    [HarmonyPatch(typeof(SaveGameHandler), "Save")]
    class SaveFile_Patch
    {
        public static void Prefix(int index)
        {
            Debug.Log("Saved to file " + index);
            if (APState.Authenticated)
            {
                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(APState.ServerData));
                var path = Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\save\\archipelago" + index + ".json";
                File.WriteAllBytes(path, bytes);
            }
        }
    }

    // load relevant info from a json file
    [HarmonyPatch(typeof(SaveGameHandler), "Load")]
    class Load_Patch
    {
        public static void Postfix(int index)
        {
            Debug.Log("Loaded file " + index);

            var path = Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\save\\archipelago" + index + ".json";

            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    APState.ServerData = JsonConvert.DeserializeObject<APData>(reader.ReadToEnd());
                }

                if (APState.Authenticated && APState.ServerData.@checked != null)
                {
                    APState.Session.Locations.CompleteLocationChecks(APState.ServerData.@checked.ToArray());
                }
            }
        }
    }
}