﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using ORKFramework;
using ORKFramework.Behaviours;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using System;

namespace ArchipelagoHylics2
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class APH2Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.trpg.ArchipelagoHylics2";
        public const string PluginName = "ArchipelagoHylics2";
        public const string PluginVersion = "1.0.0";

        public static Harmony harmony = new("mod.APH2");

        private ConfigEntry<string> configOpenConsole;
        private ConfigEntry<string> configPauseControl;
        private ConfigEntry<string> configShowPopups;
        public static bool pauseControl;
        public static bool showPopups;
        private bool isConsoleOpen = false;
        public string consoleHistory;
        public string consoleCommand = "/";
        GUIStyle consoleStyle = new();
        RectOffset consolePadding = new();
        Texture2D consoleBG = new(1, 1);

        private bool checkedBattle;
        public static List<string> cutscenes = new() { "StartScene", "FirstCutscene", "DeathScene", "SarcophagousDig_Cutscene", "ShieldDown_Cutscene", "SarcophagousCutscene",
            "Hylemxylem_Cutscene", "Cutscene_Drill_SkullBomb", "SpaceshipRising_Cutscene", "Hylemxylem_Explode_Cutscene", "SomsnosaGolfScene" };

        private static GUIBox box;
        public static bool boxOpen = false;
        public static List<string> queueMessage = new();
        public static List<string> queuePartyMember = new();
        public static List<string> queuePartyPlayer = new();

        public static Scene currentScene;

        void Awake()
        {
            Logger.LogInfo($"Plugin {PluginGUID} is loaded!");

            // set config
            configOpenConsole = Config.Bind("General",    // category
                                           "OpenConsole", // name
                                           "/",          // default value
                                           "The key used to open the console to send commands to an Archipelago server."); // description
            configPauseControl = Config.Bind("General",
                                             "PauseControl",
                                             "false",
                                             "Decide whether or not to pause the game's control while the console is open.");
            configShowPopups = Config.Bind("General",
                                           "ShowPopups",
                                           "true",
                                           "Decide whether or not to show in-game messages when an item is found or recieved.");

            if (bool.TryParse(configPauseControl.Value, out bool cfg1))
            {
                pauseControl = cfg1;
            }
            else
            {
                Logger.LogError("Couldn't parse config setting \"PauseControl\". Default value of \"false\" will be used instead.");
                pauseControl = false;
            }

            if (bool.TryParse(configShowPopups.Value, out bool cfg2))
            {
                showPopups = cfg2;
            }
            else
            {
                Logger.LogError("Couldn't parse config setting \"ShowPopups\". Default value of \"true\" will be used instead.");
                showPopups = true;
            }

            harmony.PatchAll();
        }

        // display message(s) from AP server
        public IEnumerator APShow()
        {
            box = ORK.GUIBoxes.Create(31);
            box.Content = new DialogueContent(queueMessage[0], "", null, null);
            box.Settings.showBox = true;
            box.Settings.showNameBox = false;
            box.Settings.namePadding = new Vector4(12f, 12f, 12f, 12f);
            box.bounds = new Rect(75f, 75f, 1125f, 100f);
            box.InitIn();
            queueMessage.RemoveAt(0);
            boxOpen = true;
            yield return new WaitForSecondsRealtime(2.5f);
            box.InitOut();
            if (queueMessage.Count > 0)
            {
                StartCoroutine(APShow());
            }
            else
            {
                boxOpen = false;
            }
        }

        // add item to inventory and set messages
        public static void APRecieveItem(int itemID, string player, bool self)
        {
            ItemShortcut item = new(itemID, 1);
            string message;
            ORK.Game.ActiveGroup.Leader.Inventory.Add(item, false, false, false);
            if (!self) message = "Got " + item.GetName() + " from " + player + ".";
            else message = "Found " + item.GetName() + ".";

            if (showPopups)
            {
                queueMessage.Add(message);
            }
        }

        // add equipment to inventory and set messages
        public static void APRecieveEquip(int equipID, string type, string player, bool self)
        {
            EquipSet equipSet = new EquipSet();
            if (type == "GLOVE") equipSet = EquipSet.Weapon;
            else if (type == "ACCESSORY") equipSet = EquipSet.Armor;

            EquipShortcut equip = new(equipSet, equipID, 1, 1);
            string message;
            if (!self) message = "Got " + equip.GetName() + " from " + player + ".";
            else message = "Found " + equip.GetName() + ".";

            ORK.Game.ActiveGroup.Leader.Inventory.AddEquipment(equip, false, false, false);
            if (showPopups)
            {
                queueMessage.Add(message);
            }
        }

        // add ability to character(s) and set messages
        public static void APRecieveAbility(int abilityID, string player, bool self)
        {
            AbilityShortcut ability = new(abilityID, 1, AbilityActionType.Ability);
            string message;
            if (!self) message = "Learned " + ability.GetName() + " from " + player + ".";
            else message = "Learned " + ability.GetName() + ".";

            ORK.Game.ActiveGroup.Abilities.Learn(ability, false, false);
            if (showPopups)
            {
                queueMessage.Add(message);
            }
        }

        // add money to inventory and set messages
        public static void APRecieveMoney(int amount, string player, bool self)
        {
            string message;
            if (!self) message = "Got " + amount.ToString() + " Bones from " + player + ".";
            else message = "Found " + amount.ToString() + " Bones.";

            ORK.Game.ActiveGroup.Leader.Inventory.AddMoney(0, amount, false, false);
            if (showPopups)
            {
                queueMessage.Add(message);
            }
        }

        // add member to party, reset positions, and set messages
        public static void APRecieveParty(string party, string player, bool self)
        {
            // add members to party
            if (party == "Pongorma")
            {
                Combatant combatant = ORK.Combatants.Create(37, ORK.Game.ActiveGroup);
                combatant.Init(1, 1, 37, true, true, true, false);
                ORK.Game.ActiveGroup.JoinBattle(combatant);
            }
            if (party == "Dedusmuln")
            {
                Combatant combatant = ORK.Combatants.Create(4, ORK.Game.ActiveGroup);
                combatant.Init(1, 1, 4, true, true, true, false);
                ORK.Game.ActiveGroup.JoinBattle(combatant);

            }
            if (party == "Somsnosa")
            {
                Combatant combatant = ORK.Combatants.Create(5, ORK.Game.ActiveGroup);
                combatant.Init(1, 1, 5, true, true, true, false);
                ORK.Game.ActiveGroup.JoinBattle(combatant);

            }

            // respawn new members
            if (currentScene.name != "Battle Scene" && currentScene.name != "Dungeon_Labyrinth_Scene_Final" && currentScene.name != "MazeScene1") ORK.Game.ActiveGroup.SpawnGroup(true, false);

            string message;
            if (!self) message = "Got " + party + " from " + player + ".";
            else message = "Found " + party + ".";

            if (showPopups)
            {
                queueMessage.Add(message);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                queueMessage.Add("This is a test message with lots of text to see how the game reacts to very long messages.");
            }
            // open console
            if (Input.GetKeyDown(configOpenConsole.Value))
            {
                if (!isConsoleOpen && currentScene.name != "StartScene")
                {
                    isConsoleOpen = true;
                    consoleCommand = "/";
                    if (pauseControl) ORK.Control.EnablePlayerControls(false);
                }
            }
            // close console
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isConsoleOpen = false;
                if (pauseControl) ORK.Control.EnablePlayerControls(true);
            }
            // send command
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (consoleCommand.StartsWith("/connect") && isConsoleOpen)
                {
                    string[] key = consoleCommand.Split(' ');
                    if (key.Length == 3)
                    {
                        APState.ServerData.host_name = key[1];
                        APState.ServerData.slot_name = key[2];
                        APState.Connect();
                        consoleCommand = "";
                    }
                    else if (key.Length < 3)
                    {
                        APState.message_log.Add("Not enough arguments. Command should follow the form of /connect [address:port] [name] [password]");
                        consoleCommand = "";
                    }
                    else if (key.Length > 4)
                    {
                        APState.message_log.Add("Too many arguments. Command should follow the form of /connect [address:port] [name] [password]");
                        consoleCommand = "";
                    }
                    else
                    {
                        APState.ServerData.host_name = key[1];
                        APState.ServerData.slot_name = key[2];
                        APState.ServerData.password = key[3];
                        APState.Connect();
                        consoleCommand = "";
                    }
                }
                else if (consoleCommand.StartsWith("/disconnect"))
                {
                    if (consoleCommand.Contains(" "))
                    {
                        APState.message_log.Add("The command /disconnect does not accept any arguments.");
                        consoleCommand = "";
                    }
                    else
                    {
                        if (!APState.Authenticated)
                        {
                            APState.message_log.Add("You aren't connected to an Archipelago server.");
                            consoleCommand = "";
                        }
                        else
                        {
                            APState.Disconnect();
                            APState.message_log.Add("Disconnected from Archipelago server.");
                            consoleCommand = "";
                        }
                    }
                }
                else if (consoleCommand.StartsWith("/popups"))
                {
                    if (consoleCommand.Contains(" "))
                    {
                        APState.message_log.Add("The command /popups does not accept any arguments.");
                        consoleCommand = "";
                    }
                    else
                    {
                        if (showPopups)
                        {
                            showPopups = false;
                            APState.message_log.Add("Popups have been disabled.");
                            queueMessage.Clear();
                            consoleCommand = "";
                        }
                        else if (!showPopups)
                        {
                            showPopups = true;
                            APState.message_log.Add("Popups have been enabled.");
                            consoleCommand = "";
                        }
                    }
                }
                else if (consoleCommand.StartsWith("/airship"))
                {
                    if (consoleCommand.Contains(" "))
                    {
                        APState.message_log.Add("The command /airship does not accept any arguments.");
                        consoleCommand = "";
                    }
                    else
                    {
                        if (currentScene.name != "World_Map_Scene")
                        {
                            APState.message_log.Add("Denied. Can't summon airship here.");
                            consoleCommand = "";
                        }
                        else if (!ORK.Game.ActiveGroup.Leader.Inventory.Has(new ItemShortcut(23, 1)))
                        {
                            APState.message_log.Add("Denied. You don't have DOCK KEY.");
                            consoleCommand = "";
                        }
                        else
                        {
                            GameObject[] objectList = FindObjectsOfType<GameObject>();
                            foreach (GameObject obj in objectList)
                            {
                                if (obj.name == "AirshipModel_Prefab" || obj.name == "AirshipModel_Prefab(Clone)")
                                {
                                    obj.transform.SetPositionAndRotation(new Vector3(-23.71f, 16.225f, -57.12f), obj.transform.rotation);
                                    ORK.Game.ActiveGroup.Leader.GameObject.transform.SetPositionAndRotation(new Vector3(-24.3298f, 16f, -57.7844f), ORK.Game.ActiveGroup.Leader.GameObject.transform.rotation);
                                }
                            }
                            APState.message_log.Add("Success. Airship position has been reset. Teleported to airship.");
                            consoleCommand = "";
                        }
                    }
                }
                else if (consoleCommand.StartsWith("!"))
                {
					if (!APState.Authenticated)
					{
						APState.message_log.Add("You aren't connected to an Archipelago server.");
						consoleCommand = "";
					}
					else						{
						string text = consoleCommand;
						var packet = new SayPacket();
						packet.Text = text;
						APState.Session.Socket.SendPacket(packet);
                        consoleCommand = "";
                    }
                }
                else if (consoleCommand.StartsWith("/deathlink"))
                {
                    if (consoleCommand.Contains(" "))
                    {
                        APState.message_log.Add("The command /deathlink does not accept any arguments.");
                        consoleCommand = "";
                    }
                    else
                    {
                        APState.ServerData.death_link = !APState.ServerData.death_link;
                        APState.set_deathlink();

                        if (APState.ServerData.death_link) APState.message_log.Add("DeathLink is now enabled.");
                        else APState.message_log.Add("DeathLink is now disabled.");
                        consoleCommand = "";
                    }
                }
                else if (consoleCommand.StartsWith("/checked"))
                {
                    string region;
                    if (consoleCommand.Contains(" "))
                    {
                        var array = consoleCommand.Split(new[] { ' ' }, 2);
                        region = array[1];
                    }
                    else region = currentScene.name;

                    switch (region.ToLower())
                    {
                        case "starthouse_room1":
                        case "waynehouse":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_waynehouse.ToString() + "/6 locations in the Waynehouse.");
                            break;
                        case "afterlife_island":
                        case "afterlife":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_afterlife.ToString() + "/4 locations in the Afterlife.");
                            break;
                        case "town_scene_withadditions":
                        case "town_vaultonly":
                        case "new muldul":
                        case "new muldul vault":
                            if (APState.ServerData.party_shuffle) APState.message_log.Add("You have checked " + APState.ServerData.checked_new_muldul.ToString() + "/11 locations in New Muldul and " + APState.ServerData.checked_new_muldul_vault.ToString() + "/7 locations in the New Muldul Vault.");
                            else APState.message_log.Add("You have checked " + APState.ServerData.checked_new_muldul.ToString() + "/11 locations in New Muldul and " + APState.ServerData.checked_new_muldul_vault.ToString() + "/6 locations in the New Muldul Vault.");
                            break;
                        case "banditfort_scene":
                        case "ld44 scene":
                        case "viewax's edifice":
                        case "viewaxs edifice":
                        case "viewax":
                        case "arcade 1":
                            if (APState.ServerData.party_shuffle) APState.message_log.Add("You have checked " + APState.ServerData.checked_viewaxs_edifice.ToString() + "/24 locations in Viewax's Edifice.");
                            else APState.message_log.Add("You have checked " + APState.ServerData.checked_viewaxs_edifice.ToString() + "/23 locations in Viewax's Edifice.");
                            break;
                        case "airship_scene":
                        case "airship":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_airship.ToString() + "/1 locations in the Airship.");
                            break;
                        case "secondarcade_scene":
                        case "ld44_chibiscene2_thecarpetscene":
                        case "arcade island":
                        case "arcade 2":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_arcade_island.ToString() + "/7 locations in Arcade Island.");
                            break;
                        case "bigtv_island_scene":
                        case "tv island":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_tv_island.ToString() + "/1 locations in TV Island.");
                            break;
                        case "somsnosahouse_scene":
                        case "juice ranch":
                            if (APState.ServerData.party_shuffle) APState.message_log.Add("You have checked " + APState.ServerData.checked_juice_ranch.ToString() + "/7 locations in the Juice Ranch.");
                            else APState.message_log.Add("You have checked " + APState.ServerData.checked_juice_ranch.ToString() + "/6 locations in the Juice Ranch.");
                            break;
                        case "mazescene1":
                        case "worm pod":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_worm_pod.ToString() + "/1 locations in the Worm Pod.");
                            break;
                        case "foglast_exterior_dry":
                        case "foglast":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_foglast.ToString() + "/14 locations in Foglast.");
                            break;
                        case "drillcastle":
                        case "drill castle":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_drill_castle.ToString() + "/6 locations in the Drill Castle.");
                            break;
                        case "dungeon_labyrinth_scene_final":
                        case "sage labyrinth":
                        case "sage maze":
                        case "skull bomb maze":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_sage_labyrinth.ToString() + "/20 locations in the Sage Labyrinth.");
                            break;
                        case "bigairship_scene":
                        case "sage airship":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_sage_airship.ToString() + "/4 locations in the Sage Airship.");
                            break;
                        case "flyingpalacedungeon_scene":
                        case "hylemxylem":
                            APState.message_log.Add("You have checked " + APState.ServerData.checked_hylemxylem.ToString() + "/18 locations in the Hylemxylem.");
                            break;
                        case "world_map_scene":
                        case "world":
                            APState.message_log.Add("There aren't any locations to check in World.");
                            break;
                        case "wormroom_scene":
                        case "shield facility":
                            APState.message_log.Add("There aren't any locations to check in the Shield Facility.");
                            break;
                        case "arcade":
                            APState.message_log.Add("Unknown region. Did you mean \"Viewax's Edifice\" or \"Arcade Island\"?");
                            break;
                        case "maze":
                        case "labyrinth":
                            APState.message_log.Add("Unknown region. Did you mean \"Worm Pod\" or \"Sage Labyrinth\"?");
                            break;
                        default:
                            APState.message_log.Add("Unknown region.");
                            break;
                    }
                    consoleCommand = "";
                }
                else if (consoleCommand.StartsWith("/help"))
                {
                    if (consoleCommand.Contains(" "))
                    {
                        APState.message_log.Add("The command /help does not accept any arguments.");
                        consoleCommand = "";
                    }
                    else
                    {
                        APState.message_log.Add("/connect [address:port] [name] [password]");
                        APState.message_log.Add("   Connect to an Archipelago server. Port and password are optional. If no port is given, then the default of 38281 is used.");
                        APState.message_log.Add("/disconnect");
                        APState.message_log.Add("   Disconnect from an Archipelago server.");
                        APState.message_log.Add("/popups");
                        APState.message_log.Add("   Enables or disables in-game messages when an item is found or recieved.");
                        APState.message_log.Add("/airship");
                        APState.message_log.Add("   Resets the airship and Wayne's positions in case you get stuck. Cannot be used if you don't have DOCK KEY.");
                        APState.message_log.Add("/checked [region]");
                        APState.message_log.Add("   States how many locations have been checked in a given region. If no region is given, then the player's current location will be used.");
                        APState.message_log.Add("/deathlink");
                        APState.message_log.Add("   Enables or disables DeathLink.");
                        consoleCommand = "";
                    }
                }
                else if (consoleCommand != "" && isConsoleOpen)
                {
                    consoleCommand = "";
                    APState.message_log.Add("Unknown command.");
                }
            }

            // display queued messages if not currently in a cutscene
            if (!boxOpen && queueMessage.Count > 0 && !cutscenes.Contains(currentScene.name))
            {
                StartCoroutine(APShow());
            }

            // if current battle is Viewax boss fight, remove his loot
            if (currentScene.name == "Battle Scene" && !checkedBattle)
            {
                GameObject[] objectList = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in objectList)
                {
                    CombatantComponent enemy = obj.GetComponent(typeof(CombatantComponent)) as CombatantComponent;
                    if (enemy != null && obj.name == "Viewax")
                    {
                        enemy.combatant.Setting.lootID = new int[0];
                        checkedBattle = true;
                    }
                    else
                    {
                        checkedBattle = true;
                    }
                }
            }

            // kill the party members if deathlink is enabled
            if (currentScene.name == "Battle Scene" && APState.DeathLinkKilling) 
            {
                foreach (Combatant combatant in ORK.Game.ActiveGroup.GetBattle())
                {
                    combatant.Death();
                }
            }

            // set DeathLinkKilling to false if the player is not currently dying or in the afterlife
            if ((currentScene.name != "Afterlife_Island" || currentScene.name != "DeathScene") && APState.DeathLinkKilling) APState.DeathLinkKilling = false;

            // recieve queued party members after exiting a battle
            if (queuePartyMember.Count != 0 && queuePartyPlayer.Count != 0 && currentScene.name != "Battle Scene" && !cutscenes.Contains(currentScene.name))
            {
                APRecieveParty(queuePartyMember[0], queuePartyPlayer[0], (queuePartyPlayer[0] == APState.ServerData.slot_name));
                queuePartyMember.RemoveAt(0);
                queuePartyPlayer.RemoveAt(0);
            }
        }

        void Start()
        {
            consoleStyle.alignment = TextAnchor.LowerLeft;
            consoleStyle.clipping = TextClipping.Clip;
            consolePadding.left = 5;
            consolePadding.right = 5;
            consolePadding.top = 5;
            consolePadding.bottom = 5;
            consoleStyle.padding = consolePadding;
            consoleStyle.wordWrap = true;
            consoleStyle.normal.textColor = Color.white;
            consoleBG.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
            consoleBG.Apply();
            consoleStyle.normal.background = consoleBG;
        }

        void OnGUI()
        {
            // show the console on screen
            if (isConsoleOpen == true)
            {
                var message_array = APState.message_log.ToArray();
                consoleHistory = string.Join("\n", message_array);
                GUI.Box(new Rect(0, 855, 1920, 200), consoleHistory, consoleStyle);
                consoleCommand = GUI.TextField(new Rect(0, 1055, 1920, 25), consoleCommand, consoleStyle);
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //Logger.LogInfo("OnSceneLoaded: " + scene.name);
            currentScene = scene;

            if (scene.name != "Battle Scene") checkedBattle = false;

            // show a reminder that the player is not currently connected to a server every time a new area is entered
            if (!cutscenes.Contains(scene.name) && !APState.Authenticated)
            {
                queueMessage.Add("Not currently connected to an Archipelago server.");
            }

            // load DeathScene if player is not currently in battle and DeathLink is recieved
            if (!cutscenes.Contains(scene.name) && scene.name != "Battle Scene" && APState.Authenticated && APState.DeathLinkKilling)
            {
                //SceneManager.LoadScene("DeathScene", LoadSceneMode.Single);
                foreach (Combatant combatant in ORK.Game.ActiveGroup.GetBattle())
                {
                    combatant.Death();
                }
            }

            // send DeathLink to server when DeathScene is loaded if DeathLink is enabled
            if (scene.name == "DeathScene" && APState.Authenticated && !APState.DeathLinkKilling && APState.ServerData.death_link)
            {
                APState.DeathLinkService.SendDeathLink(new DeathLink(APState.ServerData.slot_name, "has perished."));
                APState.message_log.Add(APState.ServerData.slot_name + " has perished.");
            }

            // send victory to server after defeating Gibby
            if (scene.name == "HylemxylemExplode_Cutscene" && APState.Authenticated)
            {
                long win = APState.Session.Locations.GetLocationIdFromName("Hylics 2", "Defeat Gibby");
                //Logger.LogInfo("win location: " + win);
                APState.Session.Locations.CompleteLocationChecks(win);
                APState.send_completion();
            }

            // find and modify all necessary events
            GameObject[] objectList = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objectList)
            {
                //Debug.Log("Object: " + obj.name + " | Component: " + comp.GetType());
                ItemCollector compIC = obj.GetComponent(typeof(ItemCollector)) as ItemCollector;
                if (compIC != null)
                {
                    compIC.showDialogue = false;
                    if (compIC.item[0].type != ItemDropType.Currency) compIC.item[0].id = 7;
                    //Debug.Log("Found ItemCollector with name " + obj.name + " and modified item");
                }

                EventInteraction ei = obj.GetComponent(typeof(EventInteraction)) as EventInteraction;
                if (ei != null)
                {
                    string xml = ei.eventAsset.GetData().GetXML();
                    switch (ei.eventAsset.name)
                    {
                        case "Learn_PoromericBleb_Event":
                            xml = xml.Replace("1 next=\"6\"", "1 next=\"2\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "CaveMinerJuiceSpeechEvent":
                            xml = xml.Replace("12 next=\"11\"", "12 next=\"10\"");
                            xml = xml.Replace("10 origin=\"1\" next=\"6\"", "10 origin=\"1\" next=\"3\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnSmallFire":
                            xml = xml.Replace("startIndex=\"0\"", "startIndex=\"2\"");
                            xml = xml.Replace("6 next=\"1\"", "6 next=\"7\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "PongormaJoinEvent":
                            if (APState.ServerData.party_shuffle)
                            {
                                xml = xml.Replace("17 next=\"14\"", "17 next=\"2\"");
                                xml = xml.Replace("3 next=\"6\"", "3 next=\"30\"");
                                xml = xml.Replace("11 next=\"1\"", "11 next=\"-1\"");
                                ei.eventAsset.GetData().SetXML(xml);
                            }
                            else
                            {
                                xml = xml.Replace("17 next=\"14\"", "17 next=\"0\"");
                                ei.eventAsset.GetData().SetXML(xml);
                            }
                            break;

                        case "Learn_Nematode_Event":
                            xml = xml.Replace("6 origin=\"1\" next=\"0\"", "6 origin=\"1\" next=\"2\"");
                            xml = xml.Replace("3 next=\"1\"", "3 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "Dedusmuln_Join_Event":
                            if (APState.ServerData.party_shuffle)
                            {
                                xml = xml.Replace("6 next=\"24\"", "6 next=\"0\"");
                                xml = xml.Replace("0 origin=\"1\" next=\"32\"", "0 origin=\"1\" next=\"26\"");
                                ei.eventAsset.GetData().SetXML(xml);
                            }
                            else
                            {
                                xml = xml.Replace("6 next=\"24\"", "6 next=\"0\"");
                                ei.eventAsset.GetData().SetXML(xml);
                            }
                            break;

                        case "FirstSage_Event":
                            xml = xml.Replace("2 next=\"1\"", "2 next=\"6\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnTimeSigil":
                            xml = xml.Replace("startIndex=\"0\"", "startIndex=\"2\"");
                            xml = xml.Replace("4 next=\"1\"", "4 next=\"3\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "KingDialogue_Revamp_Event":
                            xml = xml.Replace("3 next=\"0\"", "3 next=\"22\"");
                            xml = xml.Replace("7 next=\"11\"", "7 next=\"15\"");
                            xml = xml.Replace("22 next=\"21\"", "22 next=\"18\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnCharge_Event":
                            xml = xml.Replace("startIndex=\"0\"", "startIndex=\"2\"");
                            xml = xml.Replace("3 next=\"1\"", "3 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "Farmer_Gift_Event":
                            xml = xml.Replace("0 next=\"3\"", "0 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "SomsnosaHouse_JoinBattle_Event":
                            if (APState.ServerData.party_shuffle)
                            {
                                xml = xml.Replace("0 next=\"7\"", "0 next=\"15\"");
                                ei.eventAsset.GetData().SetXML(xml);
                            }
                            break;

                        case "SomsnosaHouse_PostBattle_AutoRunEvent":
                            xml = xml.Replace("10 next=\"5\"", "10 next=\"9\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "AirshipDialogueEvent_Somsnosa":
                            xml = xml.Replace("7 next=\"13\"", "7 next=\"30\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnFateSandbox":
                            xml = xml.Replace("6 origin=\"1\" next=\"0\"", "6 origin=\"1\" next=\"2\"");
                            xml = xml.Replace("3 next=\"1\"", "3 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnTeledenudate":
                            xml = xml.Replace("startIndex=\"0\"", "startIndex=\"2\"");
                            xml = xml.Replace("3 next=\"1\"", "3 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "ClickerSellerEvent":
                            xml = xml.Remove(xml.IndexOf("<1 nextFail"), xml.IndexOf("</1>", xml.IndexOf("<1 nextFail")) + 4 - xml.IndexOf("<1 nextFail"));
                            xml = xml.Remove(xml.IndexOf("<3"), xml.IndexOf("</4>") + 4 - xml.IndexOf("<3"));
                            string insert = File.ReadAllText(Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\events\\Clicker1.txt");
                            xml = xml.Insert(xml.IndexOf("<2") - 1, insert);
                            insert = File.ReadAllText(Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\events\\Clicker2.txt");
                            xml = xml.Insert(xml.IndexOf("<5") - 1, insert);
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnDialMollusc":
                            xml = xml.Replace("6 nextFail=\"7\" next=\"0\"", "6 nextFail=\"7\" next=\"2\"");
                            xml = xml.Replace("3 next=\"1\"", "3 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "FoglastSageEvent":
                            xml = xml.Replace("2 next=\"1\"", "2 next=\"6\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "ThirdSage_Event":
                            xml = xml.Replace("2 next=\"1\"", "2 next=\"10\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnSageSpell_Event":
                            xml = xml.Replace("11 next=\"0\"", "11 next=\"2\"");
                            xml = xml.Replace("8 next=\"1\"", "8 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        default:
                            break;
                    }
                }

                BattleComponent battle = obj.GetComponent(typeof(BattleComponent)) as BattleComponent;
                if (battle != null && obj.name == "ThroneRoomNPCs")
                {
                    string xml = battle.victoryEventAsset.GetData().GetXML();
                    string insert = File.ReadAllText(Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\events\\Viewax.txt");
                    xml = xml.Insert(xml.IndexOf("</9>") + 4, insert);
                    xml = xml.Replace("2 next=\"4\"", "2 next=\"10\"");
                    battle.victoryEventAsset.GetData().SetXML(xml);
                }
            }

            // recieve missing items on loading a save file (if there are any)
            if (APState.Authenticated && (APState.ServerData.index < APState.Session.Items.Index)) StartCoroutine(LoadItems());
        }

        public static IEnumerator LoadItems()
        {
            yield return new WaitForSecondsRealtime(2f);
            while (APState.ServerData.index < APState.Session.Items.Index)
            {
                var item = APState.Session.Items.AllItemsReceived[Convert.ToInt32(APState.ServerData.index)];
                string name = APState.Session.Items.GetItemName(item.Item);
                string type = APState.IdentifyItemGetType(name);
                string player = APState.Session.Players.GetPlayerName(item.Player);
                bool self = false;

                if (APState.ServerData.slot_name == player) self = true;

                if (type == "THING")
                {
                    APRecieveItem(APState.IdentifyItemGetID(name), player, self);
                    if (name == "PNEUMATOPHORE") ORK.Game.Variables.Set("AirDashBool", true);
                    if (name == "DOCK KEY") ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(37, 1), false, false, false);
                }
                else if (type == "GLOVE") APRecieveEquip(APState.IdentifyItemGetID(name), "GLOVE", player, self);
                else if (type == "ACCESSORY") APRecieveEquip(APState.IdentifyItemGetID(name), "ACCESSORY", player, self);
                else if (type == "GESTURE") APRecieveAbility(APState.IdentifyItemGetID(name), player, self);
                else if (type == "BONES") APRecieveMoney(APState.IdentifyItemGetID(name), player, self);
                else if (type == "PARTY") APRecieveParty(name, player, self);


                APState.ServerData.index++;
            }
        }

        // reload events related to party members joining if necessary
        public static void ReloadEvents()
        {
            GameObject[] objectList = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objectList)
            {
                EventInteraction ei = obj.GetComponent(typeof(EventInteraction)) as EventInteraction;
                if (ei != null)
                {
                    string xml = ei.eventAsset.GetData().GetXML();
                    switch (ei.eventAsset.name)
                    {
                        case "PongormaJoinEvent":
                            xml = xml.Replace("17 next=\"14\"", "17 next=\"3\"");
                            xml = xml.Replace("3 next=\"6\"", "3 next=\"30\"");
                            xml = xml.Replace("11 next=\"1\"", "11 next=\"-1\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;
                        case "Dedusmuln_Join_Event":
                            xml = xml.Replace("6 next=\"24\"", "6 next=\"26\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;
                        case "SomsnosaHouse_JoinBattle_Event":
                            xml = xml.Replace("0 next=\"7\"", "0 next=\"15\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
