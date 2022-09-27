using BepInEx;
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

        public static Harmony harmony = new("mod.ArchipelagoHylics2");

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

        public static List<string> cutscenes = new() { "StartScene", "FirstCutscene", "DeathScene", "SarcophagousDig_Cutscene", "ShieldDown_Cutscene", "SarcophagousCutscene",
            "Hylemxylem_Cutscene", "Cutscene_Drill_SkullBomb", "SpaceshipRising_Cutscene", "Hylemxylem_Explode_Cutscene", "SomsnosaGolfScene" };
        public static List<string> deathMessage = new() { " has perished.", "'s flesh has melted away.", " didn't have enough meat to survive.", " has entered the afterlife.", " was overpowered by Gibby's minions." };

        private static GUIBox box;
        public static bool boxOpen = false;
        public static List<string> queueMessage = new();
        public static List<string> queueItemNameOrId = new();
        public static List<string> queueItemType = new();
        public static List<string> queueItemPlayer = new();

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
            pauseControl = false;

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

        // add item to inventory and set message
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

        // add equipment to inventory and set message
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

        // add ability to character(s) and set message
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

        // add money to inventory and set message
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

        // add member to party, respawn group, and set message
        public static void APRecieveParty(string party, string player, bool self)
        {
            // add members to party
            if (party == "Pongorma" && !APState.ServerData.has_pongorma)
            {
                Combatant combatant = ORK.Combatants.Create(37, ORK.Game.ActiveGroup);
                combatant.Init(1, 1, 0, true, true, true, false);
                combatant.SetName("Pongorma");
                ORK.Game.ActiveGroup.Join(combatant, false, false);
                APState.ServerData.has_pongorma = true;
            }
            if (party == "Dedusmuln" && !APState.ServerData.has_dedusmuln)
            {
                Combatant combatant = ORK.Combatants.Create(4, ORK.Game.ActiveGroup);
                combatant.Init(1, 1, 0, true, true, true, false);
                combatant.SetName("Dedusmuln");
                ORK.Game.ActiveGroup.Join(combatant, false, false);
                APState.ServerData.has_dedusmuln = true;
            }
            if (party == "Somsnosa" && !APState.ServerData.has_somsnosa)
            {
                Combatant combatant = ORK.Combatants.Create(5, ORK.Game.ActiveGroup);
                combatant.Init(1, 1, 0, true, true, true, false);
                combatant.SetName("Somsnosa");
                ORK.Game.ActiveGroup.Join(combatant, false, false);
                APState.ServerData.has_somsnosa = true;
            }

            // set position variables
            if (APState.ServerData.has_pongorma && !APState.ServerData.has_dedusmuln && !APState.ServerData.has_somsnosa)
            {
                ORK.Game.Variables.Set("PongormaPartyPosition", 1);
            }
            else if (!APState.ServerData.has_pongorma && APState.ServerData.has_dedusmuln && !APState.ServerData.has_somsnosa)
            {
                ORK.Game.Variables.Set("DedusmulnPartyPosition", 1);
            }
            else if (!APState.ServerData.has_pongorma && !APState.ServerData.has_dedusmuln && APState.ServerData.has_somsnosa)
            {
                ORK.Game.Variables.Set("SomsnosaPartyPosition", 1);
            }
            else if (APState.ServerData.has_pongorma && APState.ServerData.has_dedusmuln && !APState.ServerData.has_somsnosa)
            {
                ORK.Game.Variables.Set("PongormaPartyPosition", 2);
                ORK.Game.Variables.Set("DedusmulnPartyPosition", 1);
            }
            else if (!APState.ServerData.has_pongorma && APState.ServerData.has_dedusmuln && APState.ServerData.has_somsnosa)
            {
                ORK.Game.Variables.Set("DedusmulnPartyPosition", 2);
                ORK.Game.Variables.Set("SomsnosaPartyPosition", 1);
            }
            else if (APState.ServerData.has_pongorma && !APState.ServerData.has_dedusmuln && APState.ServerData.has_somsnosa)
            {
                ORK.Game.Variables.Set("PongormaPartyPosition", 2);
                ORK.Game.Variables.Set("SomsnosaPartyPosition", 1);
            }
            else if (APState.ServerData.has_pongorma && APState.ServerData.has_dedusmuln && APState.ServerData.has_somsnosa)
            {
                ORK.Game.Variables.Set("PongormaPartyPosition", 3);
                ORK.Game.Variables.Set("DedusmulnPartyPosition", 2);
                ORK.Game.Variables.Set("SomsnosaPartyPosition", 1);
            }

            // set message
            string message;
            if (!self) message = "Got " + party + " from " + player + ".";
            else message = "Found " + party + ".";

            if (showPopups)
            {
                queueMessage.Add(message);
            }

            // respawn new members
            if (currentScene.name != "Battle Scene" && currentScene.name != "Dungeon_Labyrinth_Scene_Final" && currentScene.name != "Airship_Scene" &&
                currentScene.name != "MazeScene1" && currentScene.name != "LD44 Scene" && currentScene.name != "LD44_ChibiScene2_TheCarpetScene")
            {
                ORK.Game.ActiveGroup.SpawnGroup(true, false);
            }

            // set new leaders to follow (not required for Somsnosa since she is always in front)
            GameObject[] objectList = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objectList)
            {
                CaterpillarFollowerScript follow = obj.GetComponent(typeof(CaterpillarFollowerScript)) as CaterpillarFollowerScript;
                if (follow != null && obj.name == "Dedusmuln")
                {
                    GameObject.Find("Dedusmuln").GetComponent<CaterpillarFollowerScript>().Invoke("CheckPartyPositionFunction", 0.25f);
                }
                if (follow != null && obj.name == "Pongorma")
                {
                    GameObject.Find("Pongorma").GetComponent<CaterpillarFollowerScript>().Invoke("CheckPartyPositionFunction", 0.25f);
                }
            }
        }

        void Update()
        {
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

            // display queued messages if not currently in battle or a cutscene
            if (!boxOpen && queueMessage.Count > 0 && currentScene.name != "Battle Scene" && !cutscenes.Contains(currentScene.name))
            {
                StartCoroutine(APShow());
            }

            // kill the party members if deathlink is enabled
            if (currentScene.name == "Battle Scene" && APState.DeathLinkKilling) 
            {
                foreach (Combatant combatant in ORK.Game.ActiveGroup.GetBattle())
                {
                    combatant.Death();
                }
            }

            // recieve queued items after exiting a battle
            if (queueItemType.Count != 0 && queueItemPlayer.Count != 0 && queueItemNameOrId.Count != 0 && currentScene.name != "Battle Scene" && !cutscenes.Contains(currentScene.name))
            {
                string type = queueItemType[0];
                bool self = false;

                if (queueItemPlayer[0] == APState.ServerData.slot_name) self = true;

                if (type == "THING")
                {
                    APRecieveItem(int.Parse(queueItemNameOrId[0]), queueItemPlayer[0], self);
                    if (name == "PNEUMATOPHORE") ORK.Game.Variables.Set("AirDashBool", true);
                    if (name == "DOCK KEY") ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(37, 1), false, false, false);
                }
                else if (type == "GLOVE") APRecieveEquip(int.Parse(queueItemNameOrId[0]), "GLOVE", queueItemPlayer[0], self);
                else if (type == "ACCESSORY") APRecieveEquip(int.Parse(queueItemNameOrId[0]), "ACCESSORY", queueItemPlayer[0], self);
                else if (type == "GESTURE") APRecieveAbility(int.Parse(queueItemNameOrId[0]), queueItemPlayer[0], self);
                else if (type == "BONES") APRecieveMoney(int.Parse(queueItemNameOrId[0]), queueItemPlayer[0], self);
                else if (type == "PARTY") APRecieveParty(queueItemNameOrId[0], queueItemPlayer[0], self);

                queueItemType.RemoveAt(0);
                queueItemPlayer.RemoveAt(0);
                queueItemNameOrId.RemoveAt(0);
            }
        }

        void Start()
        {
            // set styles for console
            consoleStyle.alignment = TextAnchor.LowerLeft;
            consoleStyle.clipping = TextClipping.Clip;
            consolePadding.left = 5;
            consolePadding.right = 5;
            consolePadding.top = 5;
            consolePadding.bottom = 5;
            consoleStyle.padding = consolePadding;
            consoleStyle.wordWrap = true;
            consoleStyle.normal.textColor = Color.white;
            consoleStyle.richText = true;
            consoleBG.SetPixel(0, 0, new Color(0, 0, 0, 0.65f));
            consoleBG.Apply();
            consoleStyle.normal.background = consoleBG;
        }

        void OnGUI()
        {
            // show the console on screen
            if (isConsoleOpen == true)
            {
                if (APState.message_log.Count >= 14) APState.message_log.RemoveAt(0);
                var message_array = APState.message_log.ToArray();
                consoleHistory = string.Join("\n", message_array);
                GUI.Box(new Rect(0, 855, 1920, 200), consoleHistory, consoleStyle);
                GUI.SetNextControlName("ConsoleInput");
                consoleCommand = GUI.TextField(new Rect(0, 1055, 1920, 25), consoleCommand, consoleStyle);
            }
            // close console
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape && GUI.GetNameOfFocusedControl() == "ConsoleInput")
            {
                isConsoleOpen = false;
                if (pauseControl) ORK.Control.EnablePlayerControls(true);
            }
            // send command
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "ConsoleInput")
            {
                // connect to server
                if (consoleCommand.StartsWith("/connect"))
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
                        APState.message_log.Add("Not enough arguments. Command should follow the form of <color=#00EEEEFF>/connect [address:</color><color=#FAFAD2FF>port</color><color=#00EEEEFF>] [name]</color> <color=#FAFAD2FF>[password]</color>");
                        consoleCommand = "";
                    }
                    else if (key.Length > 4)
                    {
                        APState.message_log.Add("Too many arguments. Command should follow the form of <color=#00EEEEFF>/connect [address:</color><color=#FAFAD2FF>port</color><color=#00EEEEFF>] [name]</color> <color=#FAFAD2FF>[password]</color>");
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
                // disconnect from server
                else if (consoleCommand.StartsWith("/disconnect"))
                {
                    if (consoleCommand.Contains(" "))
                    {
                        APState.message_log.Add("The command <color=#00EEEEFF>/disconnect</color> does not accept any arguments.");
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
                // enable/disable in-game messages
                else if (consoleCommand.StartsWith("/popups"))
                {
                    if (consoleCommand.Contains(" "))
                    {
                        APState.message_log.Add("The command <color=#00EEEEFF>/popups</color> does not accept any arguments.");
                        consoleCommand = "";
                    }
                    else
                    {
                        if (showPopups)
                        {
                            showPopups = false;
                            APState.message_log.Add("Popups have been <color=#FA8072FF>disabled.</color>");
                            queueMessage.Clear();
                            consoleCommand = "";
                        }
                        else if (!showPopups)
                        {
                            showPopups = true;
                            APState.message_log.Add("Popups have been <color=#00FF7FFF>enabled.</color>");
                            consoleCommand = "";
                        }
                    }
                }
                // respawn airship and teleport to it in case player gets stuck
                else if (consoleCommand.StartsWith("/airship"))
                {
                    if (consoleCommand.Contains(" "))
                    {
                        APState.message_log.Add("The command <color=#00EEEEFF>/airship</color> does not accept any arguments.");
                        consoleCommand = "";
                    }
                    else
                    {
                        if (currentScene.name != "World_Map_Scene")
                        {
                            APState.message_log.Add("<color=#FA8072FF>Denied.</color> Can't summon airship here.");
                            consoleCommand = "";
                        }
                        else if (!ORK.Game.ActiveGroup.Leader.Inventory.Has(new ItemShortcut(23, 1)))
                        {
                            APState.message_log.Add("<color=#FA8072FF>Denied.</color> You don't have DOCK KEY.");
                            consoleCommand = "";
                        }
                        else
                        {
                            GameObject[] objectList = FindObjectsOfType<GameObject>();
                            foreach (GameObject obj in objectList)
                            {
                                if (obj.name == "AirshipModel_Prefab" || obj.name == "AirshipModel_Prefab(Clone)")
                                {
                                    obj.transform.SetPositionAndRotation(new Vector3(-23.71f, 16.225f, -57.12f), new Quaternion(0, 0.3827f, 0, 0.9239f));
                                    ORK.Game.ActiveGroup.Leader.GameObject.transform.SetPositionAndRotation(new Vector3(-24.3298f, 16f, -57.7844f), ORK.Game.ActiveGroup.Leader.GameObject.transform.rotation);
                                }
                            }
                            APState.message_log.Add("<color=#00FF7FFF>Success.</color> Airship position has been reset. Teleported to airship.");
                            consoleCommand = "";
                        }
                    }
                }
                // toggle deathlink on/off
                else if (consoleCommand.StartsWith("/deathlink"))
                {
                    if (consoleCommand.Contains(" "))
                    {
                        APState.message_log.Add("The command <color=#00EEEEFF>/deathlink</color> does not accept any arguments.");
                        consoleCommand = "";
                    }
                    else
                    {
                        APState.ServerData.death_link = !APState.ServerData.death_link;
                        APState.set_deathlink();

                        if (APState.ServerData.death_link) APState.message_log.Add("DeathLink is now <color=#00FF7FFF>enabled.</color>");
                        else APState.message_log.Add("DeathLink is now <color=#FA8072FF>disabled.</color>");
                        consoleCommand = "";
                    }
                }
                // see how many locations checked in a region
                else if (consoleCommand.StartsWith("/checked"))
                {
                    string region;
                    int total;
                    int total2;
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
                        case "wayne house":
                        case "spawn":
                        case "start":
                            total = 6;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_waynehouse.ToString() + 
                                "</color>/" + total.ToString() + " locations in the <color=#00FF7FFF>Waynehouse.</color>");
                            break;
                        case "afterlife_island":
                        case "afterlife":
                            total = 4;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_afterlife.ToString() + 
                                "</color>/" + total.ToString() + " locations in the <color=#00FF7FFF>Afterlife.</color>");
                            break;
                        case "town_scene_withadditions":
                        case "town_vaultonly":
                        case "new muldul":
                        case "new muldul vault":
                            if (APState.ServerData.party_shuffle && APState.ServerData.medallion_shuffle)
                            {
                                total = 12;
                                total2 = 12;
                            }
                            else if (APState.ServerData.medallion_shuffle)
                            {
                                total = 12;
                                total2 = 11;
                            }
                            else if (APState.ServerData.party_shuffle)
                            {
                                total = 11;
                                total2 = 17;
                            }
                            else
                            {
                                total = 11;
                                total2 = 6;
                            }
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_new_muldul.ToString() +
                                "</color>/" + total.ToString() + " locations in <color=#00FF7FFF>New Muldul</color> and <color=#00FF7FFF>" +
                                (APState.ServerData.checked_new_muldul_vault + APState.ServerData.checked_blerol1 + APState.ServerData.checked_blerol2 + APState.ServerData.checked_pongorma).ToString() +
                                "</color>/" + total2.ToString() + " locations in the <color=#00FF7FFF>New Muldul Vault.</color>");
                            break;
                        case "banditfort_scene":
                        case "ld44 scene":
                        case "viewax's edifice":
                        case "viewaxs edifice":
                        case "viewax":
                        case "arcade 1":
                            if (APState.ServerData.party_shuffle && APState.ServerData.medallion_shuffle)
                            {
                                total = 20;
                                total2 = 11;
                            }
                            else if (APState.ServerData.medallion_shuffle)
                            {
                                total = 19;
                                total2 = 11;
                            }
                            else if (APState.ServerData.party_shuffle)
                            {
                                total = 17;
                                total2 = 8;
                            }
                            else
                            {
                                total = 16;
                                total2 = 8;
                            }
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_viewaxs_edifice.ToString() + 
                                "</color>/" + total.ToString() + " locations in <color=#00FF7FFF>Viewax's Edifice</color> and <color=#00FF7FFF>" + APState.ServerData.checked_arcade1.ToString() + 
                                "</color>/" + total2.ToString() + " locations in <color=#00FF7FFF>Arcade 1.</color>");
                            break;
                        case "airship_scene":
                        case "airship":
                            total = 1;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_airship.ToString() + "</color>/" + total.ToString() + 
                                " locations in the <color=#00FF7FFF>Airship.</color>");
                            break;
                        case "secondarcade_scene":
                        case "ld44_chibiscene2_thecarpetscene":
                        case "arcade island":
                        case "arcade 2":
                            total = 1;
                            if (APState.ServerData.medallion_shuffle) total2 = 11;
                            else total2 = 6;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_arcade_island.ToString() + 
                                "</color>/" + total.ToString() + " locations in <color=#00FF7FFF>Arcade Island</color> and <color=#00FF7FFF>" + APState.ServerData.checked_arcade2.ToString() + 
                                "</color>/" + total2.ToString() + " locations in <color=#00FF7FFF>Arcade 2.</color>");
                            break;
                        case "bigtv_island_scene":
                        case "tv island":
                            total = 1;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_tv_island.ToString() + "</color>/" + total.ToString() + 
                                " locations in <color=#00FF7FFF>TV Island.</color>");
                            break;
                        case "somsnosahouse_scene":
                        case "juice ranch":
                        case "somsnosa's house":
                        case "somsnosas house":
                            if (APState.ServerData.party_shuffle) total = 8;
                            else total = 7;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_juice_ranch.ToString() + "</color>/" + total.ToString() + 
                                " locations in the <color=#00FF7FFF>Juice Ranch.</color>");
                            break;
                        case "mazescene1":
                        case "worm pod":
                            total = 1;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_worm_pod.ToString() + "</color>/" + total.ToString() + 
                                " locations in the <color=#00FF7FFF>Worm Pod.</color>");
                            break;
                        case "foglast_exterior_dry":
                        case "foglast":
                            if (APState.ServerData.medallion_shuffle) total = 17;
                            else total = 14;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_foglast.ToString() + "</color>/" + total.ToString() + 
                                " locations in <color=#00FF7FFF>Foglast.</color>");
                            break;
                        case "drillcastle":
                        case "drill castle":
                        case "dig site":
                            total = 6;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_drill_castle.ToString() + "</color>/" + total.ToString() + 
                                " locations in the <color=#00FF7FFF>Drill Castle.</color>");
                            break;
                        case "dungeon_labyrinth_scene_final":
                        case "sage labyrinth":
                        case "sage maze":
                        case "skull bomb maze":
                        case "skull bomb labyrinth":
                            total = 20;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_sage_labyrinth.ToString() + "</color>/" + total.ToString() + 
                                " locations in the <color=#00FF7FFF>Sage Labyrinth.</color>");
                            break;
                        case "bigairship_scene":
                        case "sage airship":
                            if (APState.ServerData.medallion_shuffle) total = 10;
                            else total = 4;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_sage_airship.ToString() + "</color>/" + total.ToString() + 
                                " locations in the <color=#00FF7FFF>Sage Airship.</color>");
                            break;
                        case "flyingpalacedungeon_scene":
                        case "hylemxylem":
                            if (APState.ServerData.medallion_shuffle) total = 22;
                            else total = 18;
                            APState.message_log.Add("You have checked <color=#00FF7FFF>" + APState.ServerData.checked_hylemxylem.ToString() + "</color>/" + total.ToString() + 
                                " locations in the <color=#00FF7FFF>Hylemxylem.</color>");
                            break;
                        case "world_map_scene":
                        case "world":
                            APState.message_log.Add("There aren't any locations to check in <color=#00FF7FFF>World.</color>");
                            break;
                        case "wormroom_scene":
                        case "shield facility":
                            APState.message_log.Add("There aren't any locations to check in the <color=#00FF7FFF>Shield Facility.</color>");
                            break;
                        case "arcade":
                            APState.message_log.Add("Unknown region. Did you mean <color=#00FF7FFF>\"Arcade 1\"</color> in Viewax's Edifice or <color=#00FF7FFF>\"Arcade 2\"</color> in Arcade Island?");
                            break;
                        case "maze":
                        case "labyrinth":
                            APState.message_log.Add("Unknown region. Did you mean <color=#00FF7FFF>\"Worm Pod\"</color> or <color=#00FF7FFF>\"Sage Labyrinth\"</color>?");
                            break;
                        default:
                            APState.message_log.Add("Unknown region.");
                            break;
                    }
                    consoleCommand = "";
                }
                // list all commands
                else if (consoleCommand.StartsWith("/help"))
                {
                    string command = null;
                    if (consoleCommand.Contains(" "))
                    {
                        var array = consoleCommand.Split(new[] { ' ' }, 2);
                        command = array[1];
                    }

                    if (command != null)
                    {
                        switch (command)
                        {
                            case "connect":
                                APState.message_log.Add("<color=#00EEEEFF>/connect [address:</color><color=#FAFAD2FF>port</color><color=#00EEEEFF>] [name]</color> <color=#FAFAD2FF>[password]</color>");
                                APState.message_log.Add("   Connect to an Archipelago server. <color=#FAFAD2FF>Port</color> and <color=#FAFAD2FF>password</color> are optional. If no port is given, then the default of 38281 is used.");
                                break;
                            case "disconnect":
                                APState.message_log.Add("<color=#00EEEEFF>/disconnect</color>");
                                APState.message_log.Add("   Disconnect from an Archipelago server.");
                                break;
                            case "popups":
                                APState.message_log.Add("<color=#00EEEEFF>/popups</color>");
                                APState.message_log.Add("   Enables or disables in-game messages when an item is found or recieved.");
                                break;
                            case "airship":
                                APState.message_log.Add("<color=#00EEEEFF>/airship</color>");
                                APState.message_log.Add("   Resets the airship's and Wayne's positions in case you get stuck. Cannot be used if you don't have DOCK KEY.");
                                break;
                            case "checked":
                                APState.message_log.Add("<color=#00EEEEFF>/checked</color> <color=#FAFAD2FF>[region]</color>");
                                APState.message_log.Add("   States how many locations have been checked in a given <color=#FAFAD2FF>region.</color> If no region is given, then the player's <color=#FAFAD2FF>current location</color> will be used.");
                                break;
                            case "deathlink":
                                APState.message_log.Add("<color=#00EEEEFF>/deathlink</color>");
                                APState.message_log.Add("   Enables or disables DeathLink.");
                                break;
                            default:
                                APState.message_log.Add("Unknown command. Available commands: <color=#00EEEEFF>/connect, /disconnect, /popups, /airship, /checked, /deathlink</color>");
                                break;
                        }
                    }
                    else
                    {
                        APState.message_log.Add("<color=#00EEEEFF>/connect [address:</color><color=#FAFAD2FF>port</color><color=#00EEEEFF>] [name]</color> <color=#FAFAD2FF>[password]</color>");
                        APState.message_log.Add("   Connect to an Archipelago server. <color=#FAFAD2FF>Port</color> and <color=#FAFAD2FF>password</color> are optional. If no port is given, then the default of 38281 is used.");
                        APState.message_log.Add("<color=#00EEEEFF>/disconnect</color>");
                        APState.message_log.Add("   Disconnect from an Archipelago server.");
                        APState.message_log.Add("<color=#00EEEEFF>/popups</color>");
                        APState.message_log.Add("   Enables or disables in-game messages when an item is found or recieved.");
                        APState.message_log.Add("<color=#00EEEEFF>/airship</color>");
                        APState.message_log.Add("   Resets the airship's and Wayne's positions in case you get stuck. Cannot be used if you don't have DOCK KEY.");
                        APState.message_log.Add("<color=#00EEEEFF>/checked</color> <color=#FAFAD2FF>[region]</color>");
                        APState.message_log.Add("   States how many locations have been checked in a given <color=#FAFAD2FF>region.</color> If no region is given, then the player's <color=#FAFAD2FF>current location</color> will be used.");
                        APState.message_log.Add("<color=#00EEEEFF>/deathlink</color>");
                        APState.message_log.Add("   Enables or disables DeathLink.");
                        consoleCommand = "";
                    }
                }
                // send message or command to server
                else if (consoleCommand != "")
                {
                    if (!APState.Authenticated)
                    {
                        APState.message_log.Add("You aren't connected to an Archipelago server.");
                        consoleCommand = "";
                    }
                    else
                    {
                        string text = consoleCommand;
                        var packet = new SayPacket();
                        packet.Text = text;
                        APState.Session.Socket.SendPacket(packet);
                        consoleCommand = "";
                    }
                }
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

            if (scene.name == "Battle Scene") Invoke("RemoveViewaxLoot", 1f);

            if (scene.name == "StartHouse_Room1") APState.ServerData.visited_waynehouse = true;

            // show a reminder that the player is not currently connected to a server every time a new area is loaded
            if (!cutscenes.Contains(scene.name) && !APState.Authenticated)
            {
                queueMessage.Add("Not currently connected to an Archipelago server.");
            }

            // load DeathScene if DeathLink was recieved during a cutscene
            if (!cutscenes.Contains(scene.name) && scene.name != "Battle Scene" && scene.name != "Afterlife_Island" && APState.Authenticated && APState.DeathLinkKilling)
            {
                SceneManager.LoadScene("DeathScene");
            }

            // send DeathLink to server when DeathScene is loaded if DeathLink is enabled
            if (scene.name == "DeathScene" && APState.Authenticated && !APState.DeathLinkKilling && APState.ServerData.death_link)
            {
                int random = new System.Random().Next(0, 4);
                APState.DeathLinkService.SendDeathLink(new DeathLink(APState.ServerData.slot_name, APState.ServerData.slot_name + deathMessage[random]));
                APState.message_log.Add("<color=#FA8072FF>" + APState.ServerData.slot_name + deathMessage[random] + "</color>");
            }

            // set DeathLinkKilling to false if the player is not currently dying
            if ((currentScene.name == "Afterlife_Island") && APState.DeathLinkKilling) APState.DeathLinkKilling = false;

            // send victory to server after defeating Gibby
            if (scene.name == "HylemxylemExplode_Cutscene" && APState.Authenticated)
            {
                long win = APState.Session.Locations.GetLocationIdFromName("Hylics 2", "Defeat Gibby");
                //Logger.LogInfo("win location: " + win);
                APState.Session.Locations.CompleteLocationChecks(win);
                APState.send_completion();
            }

            // find and modify all necessary GameObjects
            GameObject[] objectList = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objectList)
            {
                // move airship if random start is enabled
                if ((obj.name == "AirshipModel_Prefab" || obj.name == "AirshipModel_Prefab(Clone)") && APState.ServerData.random_start && APState.ServerData.start_location != "Waynehouse"
                    && !ORK.Game.Variables.GetBool("AirshipEnteredNormallyAtLeastOnce"))
                {
                    if (APState.ServerData.start_location == "Viewax's Edifice")
                    {
                        obj.transform.SetPositionAndRotation(new Vector3(66.5159f, 6.2685f, -12.0272f), new Quaternion(0, 0.8808f, 0, 0.4734f));
                    }
                    else if (APState.ServerData.start_location == "TV Island")
                    {
                        obj.transform.SetPositionAndRotation(new Vector3(125.4f, 5.3839f, 121.033f), new Quaternion(0, 0.9849f, 0, -0.1729f));

                    }
                    else if (APState.ServerData.start_location == "Shield Facility")
                    {
                        obj.transform.SetPositionAndRotation(new Vector3(-116.3876f, 6.2785f, 45.9867f), new Quaternion(0, 0.9931f, 0, -0.1176f));
                    }
                }

                // replace all items with nothing. recieved items are all remote
                ItemCollector compIC = obj.GetComponent(typeof(ItemCollector)) as ItemCollector;
                if (compIC != null)
                {
                    compIC.showDialogue = false;

                    if (compIC.item[0].type != ItemDropType.Currency)
                    {
                        if (compIC.item[0].type == ItemDropType.Weapon || compIC.item[0].type == ItemDropType.Armor) compIC.item[0].type = ItemDropType.Item;
                        compIC.item[0].quantity = 0;
                    }
                    else if (compIC.item[0].type == ItemDropType.Currency && compIC.item[0].quantity >= 50)
                    { 
                        compIC.item[0].quantity = 0;
                    }
                    else if (compIC.item[0].type == ItemDropType.Currency && compIC.item[0].quantity == 10 && APState.ServerData.medallion_shuffle)
                    {
                        compIC.item[0].quantity = 0;
                    }
                }

                // modify events
                EventInteraction ei = obj.GetComponent(typeof(EventInteraction)) as EventInteraction;
                if (ei != null)
                {
                    string xml = ei.eventAsset.GetData().GetXML();
                    switch (ei.eventAsset.name)
                    {
                        case "Learn_PoromericBleb_Event": // Waynehouse TV
                            xml = xml.Replace("1 next=\"6\"", "1 next=\"2\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "CaveMinerJuiceSpeechEvent": // Trade with miner in underground New Muldul
                            xml = xml.Replace("12 next=\"11\"", "12 next=\"10\"");
                            xml = xml.Replace("10 origin=\"1\" next=\"6\"", "10 origin=\"1\" next=\"3\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnSmallFire": // New Muldul TV
                            xml = xml.Replace("startIndex=\"0\"", "startIndex=\"2\"");
                            xml = xml.Replace("6 next=\"1\"", "6 next=\"7\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "PongormaJoinEvent": // Talk to Pongorma in New Muldul Vault
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

                        case "Learn_Nematode_Event": // Drill Castle TV
                            xml = xml.Replace("6 origin=\"1\" next=\"0\"", "6 origin=\"1\" next=\"2\"");
                            xml = xml.Replace("3 next=\"1\"", "3 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "Dedusmuln_Join_Event": // Talk to Dedusmuln in Viewax's Edifice
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

                        case "CanoeEvent_Beach": // Use canoe in Viewax's Edifice
                            if (xml.Contains("IsBattleMemberStep"))
                            {
                                xml = xml.Remove(xml.IndexOf("<0 id"), xml.IndexOf("</0>", xml.IndexOf("<0 id")) + 4 - xml.IndexOf("<0 id"));
                                string insert1 = File.ReadAllText(Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\events\\Canoe.txt");
                                xml = xml.Insert(xml.IndexOf("<1", xml.IndexOf("<step>")) - 1, insert1);
                                ei.eventAsset.GetData().SetXML(xml);
                            }
                            break;

                        case "FirstSage_Event": // Talk to Sage in Viewax's Edifice
                            xml = xml.Replace("2 next=\"1\"", "2 next=\"6\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnTimeSigil": // Viewax's Edifice TV
                            xml = xml.Replace("startIndex=\"0\"", "startIndex=\"2\"");
                            xml = xml.Replace("4 next=\"1\"", "4 next=\"3\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "KingDialogue_Revamp_Event": // Talk to Blerol after rescuing him
                            xml = xml.Replace("3 next=\"0\"", "3 next=\"22\"");
                            xml = xml.Replace("7 next=\"11\"", "7 next=\"15\"");
                            xml = xml.Replace("22 next=\"21\"", "22 next=\"18\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnCharge_Event": // TV Island TV
                            xml = xml.Replace("startIndex=\"0\"", "startIndex=\"2\"");
                            xml = xml.Replace("3 next=\"1\"", "3 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "Farmer_Gift_Event": // Talk to rancher on ledge in Juice Ranch
                            xml = xml.Replace("0 next=\"3\"", "0 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "SomsnosaHouse_JoinBattle_Event": // Talk to Somsnosa in Juice Ranch
                            if (APState.ServerData.party_shuffle)
                            {
                                xml = xml.Replace("0 next=\"7\"", "0 next=\"15\"");
                                ei.eventAsset.GetData().SetXML(xml);
                            }
                            break;

                        case "SomsnosaHouse_PostBattle_AutoRunEvent": // Win battle in Juice Ranch
                            xml = xml.Replace("10 next=\"5\"", "10 next=\"9\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "AirshipDialogueEvent_Somsnosa": // Talk to Somsnosa in Airship after completing Worm Pod
                            xml = xml.Replace("7 next=\"13\"", "7 next=\"30\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnFateSandbox": // Juice Ranch TV
                            xml = xml.Replace("6 origin=\"1\" next=\"0\"", "6 origin=\"1\" next=\"2\"");
                            xml = xml.Replace("3 next=\"1\"", "3 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnTeledenudate": // Afterlife TV
                            xml = xml.Replace("startIndex=\"0\"", "startIndex=\"2\"");
                            xml = xml.Replace("3 next=\"1\"", "3 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "ClickerSellerEvent": // Buy clicker from guy in Foglast
                            xml = xml.Remove(xml.IndexOf("<1 nextFail"), xml.IndexOf("</1>", xml.IndexOf("<1 nextFail")) + 4 - xml.IndexOf("<1 nextFail"));
                            xml = xml.Remove(xml.IndexOf("<3"), xml.IndexOf("</4>") + 4 - xml.IndexOf("<3"));
                            string insert2 = File.ReadAllText(Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\events\\Clicker1.txt");
                            xml = xml.Insert(xml.IndexOf("<2") - 1, insert2);
                            insert2 = File.ReadAllText(Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\events\\Clicker2.txt");
                            xml = xml.Insert(xml.IndexOf("<5") - 1, insert2);
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnDialMollusc": // Foglast TV
                            xml = xml.Replace("6 nextFail=\"7\" next=\"0\"", "6 nextFail=\"7\" next=\"2\"");
                            xml = xml.Replace("3 next=\"1\"", "3 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "FoglastSageEvent": // Talk to Sage in Foglast
                            xml = xml.Replace("2 next=\"1\"", "2 next=\"6\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "ThirdSage_Event": // Talk to Sage in Sage Labyrinth
                            xml = xml.Replace("2 next=\"1\"", "2 next=\"10\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "LearnSageSpell_Event": // Sage Airship TV
                            xml = xml.Replace("11 next=\"0\"", "11 next=\"2\"");
                            xml = xml.Replace("8 next=\"1\"", "8 next=\"5\"");
                            ei.eventAsset.GetData().SetXML(xml);
                            break;

                        case "AfterlifeWarpChoice_Event": // Talk to guy in Afterlife next to pool
                            if (APState.ServerData.random_start && APState.ServerData.start_location != "Waynehouse" && !APState.ServerData.visited_waynehouse)
                            {
                                if (xml.Contains("Waynehouse"))
                                {
                                    xml = xml.Remove(xml.IndexOf("<1 next=\"3\""), xml.IndexOf("</1>", xml.IndexOf("<1 next=\"3\"")) + 4 - xml.IndexOf("<1 next=\"3\""));
                                    ei.eventAsset.GetData().SetXML(xml);
                                }
                            }
                            break;

                        case "Afterlife_WarpPool_SceneChangerEvent": // Jump in pool in Afterlife
                            if (APState.ServerData.random_start && APState.ServerData.start_location != "Waynehouse" && !APState.ServerData.visited_waynehouse)
                            {
                                if (xml.Contains("StartHouse_Room1"))
                                {
                                    xml = xml.Replace("StartHouse_Room1", "Afterlife_Island");
                                    ei.eventAsset.GetData().SetXML(xml);
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }

                // modify battle related events
                BattleComponent battle = obj.GetComponent(typeof(BattleComponent)) as BattleComponent;
                if (battle != null)
                {
                    if (obj.name == "ThroneRoomNPCs") // add rewards back in to end of Viewax boss fight
                    {
                        string xml = battle.victoryEventAsset.GetData().GetXML();
                        string insert = File.ReadAllText(Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\ArchipelagoHylics2\\events\\Viewax.txt");
                        xml = xml.Insert(xml.IndexOf("</9>") + 4, insert);
                        xml = xml.Replace("2 next=\"4\"", "2 next=\"10\"");
                        battle.victoryEventAsset.GetData().SetXML(xml);
                    }
                    // prevent Somsnosa from leaving the party when losing the fight in her house if party_shuffle is enabled
                    else if (obj.name == "JoinBattle_Soms" && APState.ServerData.party_shuffle) 
                    {
                        string xml = battle.defeatEventAsset.GetData().GetXML();
                        xml = xml.Replace("9 next=\"8\"", "9 next=\"0\"");
                        battle.defeatEventAsset.GetData().SetXML(xml);
                    }
                }
            }

            // recieve missing items on loading a save file (if there are any)
            if (APState.Authenticated && (APState.ServerData.index < APState.Session.Items.Index)) StartCoroutine(LoadItems());
        }

        // remove Viewax's drops from battle
        public void RemoveViewaxLoot()
        {
            GameObject[] objectList = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objectList)
            {
                CombatantComponent enemy = obj.GetComponent(typeof(CombatantComponent)) as CombatantComponent;
                if (enemy != null && obj.name == "Viewax")
                {
                    enemy.combatant.Setting.lootID = new int[0];
                }
            }
        }

        // recieve missing items when loading a previous save
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
                if (APState.ServerData.party_shuffle)
                {
                    EventInteraction ei = obj.GetComponent(typeof(EventInteraction)) as EventInteraction;
                    if (ei != null)
                    {
                        string xml = ei.eventAsset.GetData().GetXML();
                        switch (ei.eventAsset.name)
                        {
                            case "PongormaJoinEvent":
                                xml = xml.Replace("17 next=\"14\"", "17 next=\"2\"");
                                xml = xml.Replace("3 next=\"6\"", "3 next=\"30\"");
                                xml = xml.Replace("11 next=\"1\"", "11 next=\"-1\"");
                                ei.eventAsset.GetData().SetXML(xml);
                                break;
                            case "Dedusmuln_Join_Event":
                                xml = xml.Replace("6 next=\"24\"", "6 next=\"0\"");
                                xml = xml.Replace("0 origin=\"1\" next=\"32\"", "0 origin=\"1\" next=\"26\"");
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
                    BattleComponent battle = obj.GetComponent(typeof(BattleComponent)) as BattleComponent;
                    if (battle != null)
                    {
                        if (obj.name == "JoinBattle_Soms" && APState.ServerData.party_shuffle)
                        {
                            string xml = battle.defeatEventAsset.GetData().GetXML();
                            xml = xml.Replace("9 next=\"8\"", "9 next=\"0\"");
                            battle.defeatEventAsset.GetData().SetXML(xml);
                        }
                    }
                }
                if (APState.ServerData.medallion_shuffle)
                {
                    ItemCollector compIC = obj.GetComponent(typeof(ItemCollector)) as ItemCollector;
                    if (compIC != null)
                    {
                        if (compIC.item[0].type == ItemDropType.Currency && compIC.item[0].quantity == 10)
                        {
                            compIC.item[0].quantity = 0;
                        }
                    }
                }
            }
        }
    }
}
