using HarmonyLib;
using ORKFramework.Behaviours;
using ORKFramework;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArchipelagoHylics2
{
    [HarmonyPatch(typeof(ItemCollector), "CollectionFinished")]
    class CollectionFinished_Patch
    {
        public static void Postfix(ItemCollector __instance)
        {
            //Debug.Log("CollectionFinished: " + __instance.name + " in " + SceneManager.GetActiveScene().name + " with Scene ID " + __instance.sceneID);
            //Debug.Log("Scene: " + __instance.GameObject.scene.name.ToString() + " | ID: " + __instance.sceneID);
            if (APState.Authenticated)
            {
                long? location = APData.IdentifyItemCheck(__instance.GameObject.scene.name.ToString(), __instance.sceneID);
                if (location.HasValue)
                {
                    Debug.Log(location.Value);
                    APState.Session.Locations.CompleteLocationChecks(location.Value);
                }
            }
        }
    }

    [HarmonyPatch(typeof(EventInteraction), "OnDestroy")]
    class OnDestroy_Patch
    {
        public static void Postfix(EventInteraction __instance)
        {
            if (APState.Authenticated)
            {
                if (__instance.eventAsset.name == "PongormaJoinEvent")
                {
                    APState.Session.Locations.CompleteLocationChecks(200643);
                    if (APState.party_shuffle) APState.Session.Locations.CompleteLocationChecks(200644);
                }
            }
        }
    }

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
                        break;

                    case "CaveMinerJuiceSpeechEvent": // Give Juice to Cave Miner
                        if (ORK.Game.Variables.Check("MinerJuiceGiven_Variable", true))
                        {
                            APState.Session.Locations.CompleteLocationChecks(200638);
                        }
                        break;

                    case "LearnSmallFire": // New Muldul TV
                        APState.Session.Locations.CompleteLocationChecks(200642);
                        break;

                    case "Learn_Nematode_Event": // Drill Castle TV
                        APState.Session.Locations.CompleteLocationChecks(200712);
                        break;

                    case "Dedusmuln_Join_Event": // Talk to Dedusmuln in Viewax's Edifice
                        APState.Session.Locations.CompleteLocationChecks(200653);
                        if (APState.party_shuffle) APState.Session.Locations.CompleteLocationChecks(200654);
                        break;

                    case "FirstSage_Event": // Talk to Sage in Viewax's Edifice
                        APState.Session.Locations.CompleteLocationChecks(200662, 200663);
                        break;

                    case "BanditFort_Boss_DialogueEvent": // Defeat Viewax
                        ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(5, 1), false, false, false);
                        ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(10, 2), false, false, false);
                        ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(12, 5), false, false, false);
                        ORK.Game.ActiveGroup.Leader.Inventory.AddMoney(0, 25, false, false);
                        APState.Session.Locations.CompleteLocationChecks(200665);
                        break;

                    case "LearnTimeSigil": // Viewax's Edifice TV
                        APState.Session.Locations.CompleteLocationChecks(200666);
                        break;

                    case "KingDialogue_Revamp_Event":
                        if (ORK.Game.Variables.GetFloat("SpokeXTimesAfterBandits") == 1)
                        {
                            APState.Session.Locations.CompleteLocationChecks(200645);
                        }
                        else if (ORK.Game.Variables.GetFloat("SpokeXTimesAfterBandits") == 4)
                        {
                            APState.Session.Locations.CompleteLocationChecks(200646);
                        }
                        break;

                    case "LearnCharge_Event": // TV Island TV
                        APState.Session.Locations.CompleteLocationChecks(200683);
                        break;

                    case "Farmer_Gift_Event": // Talk to Farmer in Juice Ranch
                        APState.Session.Locations.CompleteLocationChecks(200687);
                        break;

                    case "SomsnosaHouse_PostBattle_AutoRunEvent": // Finish battle with Somsnosa
                        APState.Session.Locations.CompleteLocationChecks(200688);
                        break;

                    case "AirshipDialogueEvent_Somsnosa": // Talk to Somsnosa in Airship after Worm Room
                        if (ORK.Game.Variables.Check("WormDefeated", true))
                        {
                            APState.Session.Locations.CompleteLocationChecks(200675);
                        }
                        break;

                    case "LearnFateSandbox": // Juice Ranch TV
                        APState.Session.Locations.CompleteLocationChecks(200691);
                        break;

                    case "LearnTeledenudate": // Afterlife TV
                        APState.Session.Locations.CompleteLocationChecks(200631);
                        break;

                    case "ClickerSellerEvent": // Buy Clicker for Foglast TV
                        if (ORK.Game.ActiveGroup.Leader.Inventory.Has(new ItemShortcut(55, 1)))
                        {
                            APState.Session.Locations.CompleteLocationChecks(200696);
                        }
                        break;

                    case "LearnDialMollusc": // Foglast TV
                        APState.Session.Locations.CompleteLocationChecks(200697);
                        break;

                    case "FoglastSageEvent": // Talk to Sage in Foglast
                        APState.Session.Locations.CompleteLocationChecks(200705, 200706);
                        break;

                    case "ThirdSage_Event": // Talk to Sage in Sage Labyrinth
                        APState.Session.Locations.CompleteLocationChecks(200726, 200727);
                        break;

                    case "LearnSageSpell_Event": // Sage Airship TV
                        APState.Session.Locations.CompleteLocationChecks(200735);
                        break;

                    default:
                        break;
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
                string save = APState.Session.Items.Index.ToString() + "|" + APState.party_shuffle.ToString();
                File.WriteAllText(Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\save\\savegame" + index + ".txt", save);
            }
        }
    }

    [HarmonyPatch(typeof(SaveGameHandler), "Load")]
    class Load_Patch
    {
        public static void Postfix(int index)
        {
            //Debug.Log("Loaded file " + index);
        }
    }
}