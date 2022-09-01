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
            if (APState.Authenticated)
            {
                long? location = APState.IdentifyItemCheck(__instance.GameObject.scene.name.ToString(), __instance.sceneID);
                if (location.HasValue)
                {
                    Debug.Log(location.Value);
                    APState.ServerData.@checked.Add(location.Value);
                    APState.Session.Locations.CompleteLocationChecks(location.Value);

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
                        case "LD44 Scene":
                            APState.ServerData.checked_viewaxs_edifice++;
                            break;
                        case "SecondArcade_Scene":
                        case "LD44_ChibiScene2_TheCarpetScene":
                            APState.ServerData.checked_arcade_island++;
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
            }
        }
    }

    // patch EventInteraction to send location checks after completing certain events and with certain outcomes
    [HarmonyPatch(typeof(EventInteraction), "EventEnded")]
    class EventEnded_Patch
    {
        public static void Postfix(EventInteraction __instance)
        {
            Debug.Log("EventEnded: " + __instance.eventAsset.name);
            if (APState.Authenticated)
            {
                switch (__instance.eventAsset.name)
                {
                    case "Learn_PoromericBleb_Event": // Waynehouse TV
                        APState.Session.Locations.CompleteLocationChecks(200627);
                        APState.ServerData.checked_waynehouse++;
                        break;

                    case "CaveMinerJuiceSpeechEvent": // Give Juice to Cave Miner
                        if (ORK.Game.Variables.Check("MinerJuiceGiven_Variable", true))
                        {
                            APState.Session.Locations.CompleteLocationChecks(200638);
                            APState.ServerData.checked_new_muldul++;
                        }
                        break;

                    case "LearnSmallFire": // New Muldul TV
                        APState.Session.Locations.CompleteLocationChecks(200642);
                        APState.ServerData.checked_new_muldul++;
                        break;

                    case "Learn_Nematode_Event": // Drill Castle TV
                        APState.Session.Locations.CompleteLocationChecks(200712);
                        APState.ServerData.checked_drill_castle++;
                        break;

                    case "Dedusmuln_Join_Event": // Talk to Dedusmuln in Viewax's Edifice
                        APState.Session.Locations.CompleteLocationChecks(200653);
                        APState.ServerData.checked_viewaxs_edifice++;
                        if (APState.ServerData.party_shuffle)
                        {
                            APState.Session.Locations.CompleteLocationChecks(200654);
                            APState.ServerData.checked_viewaxs_edifice++;
                        }
                        break;

                    case "FirstSage_Event": // Talk to Sage in Viewax's Edifice
                        APState.Session.Locations.CompleteLocationChecks(200662, 200663);
                        APState.ServerData.checked_viewaxs_edifice += 2;
                        break;

                    case "BanditFort_Boss_DialogueEvent": // Defeat Viewax
                        ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(5, 1), false, false, false);
                        ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(10, 2), false, false, false);
                        ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(12, 5), false, false, false);
                        ORK.Game.ActiveGroup.Leader.Inventory.AddMoney(0, 25, false, false);
                        APState.Session.Locations.CompleteLocationChecks(200665);
                        APState.ServerData.checked_viewaxs_edifice++;
                        break;

                    case "LearnTimeSigil": // Viewax's Edifice TV
                        APState.Session.Locations.CompleteLocationChecks(200666);
                        APState.ServerData.checked_viewaxs_edifice++;
                        break;

                    case "KingDialogue_Revamp_Event":
                        if (ORK.Game.Variables.GetFloat("SpokeXTimesAfterBandits") == 1)
                        {
                            APState.Session.Locations.CompleteLocationChecks(200645);
                            APState.ServerData.checked_new_muldul_vault++;
                        }
                        else if (ORK.Game.Variables.GetFloat("SpokeXTimesAfterBandits") == 4)
                        {
                            APState.Session.Locations.CompleteLocationChecks(200646);
                            APState.ServerData.checked_new_muldul_vault++;
                        }
                        break;

                    case "LearnCharge_Event": // TV Island TV
                        APState.Session.Locations.CompleteLocationChecks(200683);
                        APState.ServerData.checked_tv_island++;
                        break;

                    case "Farmer_Gift_Event": // Talk to Farmer in Juice Ranch
                        APState.Session.Locations.CompleteLocationChecks(200687);
                        APState.ServerData.checked_juice_ranch++;
                        break;

                    case "SomsnosaHouse_PostBattle_AutoRunEvent": // Finish battle with Somsnosa
                        APState.Session.Locations.CompleteLocationChecks(200688);
                        APState.ServerData.checked_juice_ranch++;
                        if (APState.ServerData.party_shuffle)
                        {
                            APState.Session.Locations.CompleteLocationChecks(200689);
                            APState.ServerData.checked_juice_ranch++;
                        }
                        break;

                    case "AirshipDialogueEvent_Somsnosa": // Talk to Somsnosa in Airship after Worm Room
                        if (ORK.Game.Variables.Check("WormDefeated", true))
                        {
                            APState.Session.Locations.CompleteLocationChecks(200675);
                            APState.ServerData.checked_airship++;
                        }
                        break;

                    case "LearnFateSandbox": // Juice Ranch TV
                        APState.Session.Locations.CompleteLocationChecks(200691);
                        APState.ServerData.checked_juice_ranch++;
                        break;

                    case "LearnTeledenudate": // Afterlife TV
                        APState.Session.Locations.CompleteLocationChecks(200631);
                        APState.ServerData.checked_afterlife++;
                        break;

                    case "ClickerSellerEvent": // Buy Clicker for Foglast TV
                        if (ORK.Game.ActiveGroup.Leader.Inventory.Has(new ItemShortcut(55, 1)))
                        {
                            APState.Session.Locations.CompleteLocationChecks(200696);
                            APState.ServerData.checked_foglast++;
                        }
                        break;

                    case "LearnDialMollusc": // Foglast TV
                        APState.Session.Locations.CompleteLocationChecks(200697);
                        APState.ServerData.checked_foglast++;
                        break;

                    case "FoglastSageEvent": // Talk to Sage in Foglast
                        APState.Session.Locations.CompleteLocationChecks(200705, 200706);
                        APState.ServerData.checked_foglast += 2;
                        break;

                    case "ThirdSage_Event": // Talk to Sage in Sage Labyrinth
                        APState.Session.Locations.CompleteLocationChecks(200726, 200727);
                        APState.ServerData.checked_sage_labyrinth += 2;
                        break;

                    case "LearnSageSpell_Event": // Sage Airship TV
                        APState.Session.Locations.CompleteLocationChecks(200735);
                        APState.ServerData.checked_sage_airship++;
                        break;

                    default:
                        break;
                }
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
                    APState.ServerData.checked_new_muldul_vault++;
                    if (APState.ServerData.party_shuffle)
                    {
                        APState.Session.Locations.CompleteLocationChecks(200644);
                        APState.ServerData.checked_new_muldul_vault++;
                    }
                }
            }
        }
    }

    
    [HarmonyPatch(typeof(SaveGameHandler), "SaveFile")]
    class SaveFile_Patch
    {
        public static void Postfix(int index)
        {
            //Debug.Log("Saved to file " + index);
            //Debug.Log(APState.Session.Items.Index);
            if (APState.Authenticated)
            {
                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(APState.ServerData));
                var path = Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\save\\archipelago" + index + ".json";
                File.WriteAllBytes(path, bytes);
            }
        }
    }


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

            /*
            string save = File.ReadAllText(path);
            string[] keys = save.Split('|');
            long ind = long.Parse(keys[0]);
            Debug.Log("Index of file " + index + " is " + ind);
            APState.ServerData.index = ind;
            */
        }
    }
}