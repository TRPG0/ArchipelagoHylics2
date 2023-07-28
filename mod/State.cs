using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Helpers;
using UnityEngine;
using ORKFramework;
using UnityEngine.SceneManagement;
using System.Linq;
using ORKFramework.Behaviours;

namespace ArchipelagoHylics2
{
    public static class APState
    {

        public static int[] AP_VERSION = new int[] { 0, 4, 1 };
        public static APData ServerData = new();
        public static DeathLinkService DeathLinkService = null;
        public static bool DeathLinkKilling = false; // indicates player is currently being deathlinked
        public static List<string> message_log = new List<string> { "Hylics 2 | Archipelago | " + APH2Plugin.PluginVersion,
            "Available commands: <color=#00EEEEFF>/connect, /disconnect, /popups, /airship, /respawn, /checked, /deathlink, /help</color>" };
        public static bool Authenticated;

        public static ArchipelagoSession Session;

        public static bool Connect()
        {
            if (Authenticated)
            {
                return true;
            }
            var url = ServerData.host_name;
            int port = 38281;
            
            if (url.Contains(":"))
            {
                var splits = url.Split(new char[] { ':' });
                url = splits[0];
                if (!int.TryParse(splits[1], out port)) port = 38281;
            }

            Session = ArchipelagoSessionFactory.CreateSession(url, port);
            Session.Socket.PacketReceived += Session_PacketRecieved;
            Session.Socket.ErrorReceived += Session_ErrorRecieved;
            Session.Socket.SocketClosed += Session_SocketClosed;
            Session.Items.ItemReceived += Session_ItemRecieved;

            LoginResult loginResult = Session.TryConnectAndLogin(
                "Hylics 2",
                ServerData.slot_name,
                ItemsHandlingFlags.AllItems,                
                new Version(AP_VERSION[0], AP_VERSION[1], AP_VERSION[2]),
                null,
                null,
                ServerData.password == "" ? null : ServerData.password);

            if (loginResult is LoginSuccessful loginSuccess)
            {
                Authenticated = true;
                Debug.Log("Successfully connected to server!");

                if (loginSuccess.SlotData["party_shuffle"].ToString() == "1")
                {
                    ServerData.party_shuffle = true;
                    if (APH2Plugin.currentScene.name == "Town_VaultOnly" || APH2Plugin.currentScene.name == "BanditFort_Scene" || APH2Plugin.currentScene.name == "SomsnosaHouse_Scene")
                    {
                        APH2Plugin.ReloadEvents();
                    }
                }
                if (loginSuccess.SlotData["medallion_shuffle"].ToString() == "1")
                {
                    ServerData.medallion_shuffle = true;
                    if (APH2Plugin.currentScene.name == "Town_Scene_WithAdditions" || APH2Plugin.currentScene.name == "Town_VaultOnly" || APH2Plugin.currentScene.name == "BanditFort_Scene" ||
                        APH2Plugin.currentScene.name == "LD44 Scene" || APH2Plugin.currentScene.name == "LD44_ChibiScene2_TheCarpetScene" || APH2Plugin.currentScene.name == "Foglast_Exterior_Dry" ||
                        APH2Plugin.currentScene.name == "BigAirship_Scene" || APH2Plugin.currentScene.name == "FlyingPalaceDungeon_Scene")
                    {
                        APH2Plugin.ReloadEvents();
                    }
                }
                if (loginSuccess.SlotData["random_start"].ToString() == "1")
                {
                    ServerData.random_start = true;
                    ServerData.start_location = loginSuccess.SlotData["start_location"].ToString();

                    if ((ServerData.checked_waynehouse + ServerData.checked_afterlife + ServerData.checked_new_muldul + ServerData.checked_new_muldul_vault + 
                        ServerData.checked_pongorma + ServerData.checked_blerol1 + ServerData.checked_blerol2 + ServerData.checked_viewaxs_edifice + 
                        ServerData.checked_arcade1 + ServerData.checked_airship + ServerData.checked_arcade_island + ServerData.checked_arcade2 + 
                        ServerData.checked_tv_island + ServerData.checked_juice_ranch + ServerData.checked_worm_pod + ServerData.checked_foglast + 
                        ServerData.checked_drill_castle + ServerData.checked_sage_labyrinth + ServerData.checked_sage_airship + ServerData.checked_hylemxylem) == 0 
                        && ServerData.start_location != "Waynehouse")
                    {
                        // remove mini crystal from inventory if random start is enabled and start location is not Waynehouse
                        if (ORK.Game.ActiveGroup.Leader.Inventory.Has(new ItemShortcut(15, 1)))
                        {
                            ORK.Game.ActiveGroup.Leader.Inventory.Remove(new ItemShortcut(15, 1), false, false);
                        }

                        GameObject gameObject = new("Changer");
                        SceneChanger changer = gameObject.AddComponent<SceneChanger>();
                        SceneTarget target = new();
                        target.spawnID = 9;

                        if (ServerData.start_location == "Viewax's Edifice")
                        {
                            ORK.Game.Variables.Set("Warp3_Fort", true);
                            target.sceneName = "BanditFort_Scene";
                            changer.target = new[] { target };
                            changer.StartEvent(gameObject);
                        }
                        else if (ServerData.start_location == "TV Island")
                        {
                            ORK.Game.Variables.Set("Warp60_BigTV", true);
                            target.sceneName = "BigTV_Island_Scene";
                            changer.target = new[] { target };
                            changer.StartEvent(gameObject);
                        }
                        else if (ServerData.start_location == "Shield Facility")
                        {
                            ORK.Game.Variables.Set("Warp100_WormHouse", true);
                            target.sceneName = "WormRoom_Scene";
                            changer.target = new[] { target };
                            changer.StartEvent(gameObject);
                        }
                        ServerData.visited_waynehouse = false;
                    }
                }
                if (loginSuccess.SlotData["death_link"].ToString() == "1") ServerData.death_link = true;
                set_deathlink();

                // send any location checks that may have been completed while disconnected
                if (ServerData.@checked != null)
                {
                    Session.Locations.CompleteLocationChecks(ServerData.@checked.ToArray());
                }
            }
            else if (loginResult is LoginFailure loginFailure)
            {
                Authenticated = false;
                Debug.LogError("Connection Error: " + String.Join("\n", loginFailure.Errors));
                message_log.Add("Connection Error: " + String.Join("\n", loginFailure.Errors));
                Session.Socket.Disconnect();
                Session = null;
            }
            return loginResult.Successful;
        }

        public static void Session_ItemRecieved(ReceivedItemsHelper helper)
        {
            //Debug.Log(helper.PeekItemName());
            //Debug.Log(helper.PeekItem().Player);

            if (helper.Index !> ServerData.index)
            {
                string type = IdentifyItemGetType(helper.PeekItemName());
                string name = helper.PeekItemName();
                string player = Session.Players.GetPlayerName(helper.PeekItem().Player);
                bool self = false;

                if (player == ServerData.slot_name) self = true;

                if (APH2Plugin.currentScene.name != "Battle Scene")
                {
                    if (type == "THING")
                    {
                        APH2Plugin.APRecieveItem(IdentifyItemGetID(name), player, self);
                        if (name == "PNEUMATOPHORE") ORK.Game.Variables.Set("AirDashBool", true);
                        if (name == "DOCK KEY") ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(37, 1), false, false, false);
                    }
                    else if (type == "GLOVE") APH2Plugin.APRecieveEquip(IdentifyItemGetID(name), "GLOVE", player, self);
                    else if (type == "ACCESSORY") APH2Plugin.APRecieveEquip(IdentifyItemGetID(name), "ACCESSORY", player, self);
                    else if (type == "GESTURE") APH2Plugin.APRecieveAbility(IdentifyItemGetID(name), player, self);
                    else if (type == "BONES") APH2Plugin.APRecieveMoney(IdentifyItemGetID(name), player, self);
                    else if (type == "PARTY") APH2Plugin.APRecieveParty(name, player, self);
                }
                else
                {
                    APH2Plugin.queueItemType.Add(type);
                    APH2Plugin.queueItemPlayer.Add(player);
                    if (type == "PARTY") APH2Plugin.queueItemNameOrId.Add(name);
                    else APH2Plugin.queueItemNameOrId.Add(IdentifyItemGetID(name).ToString());
                }

                ServerData.index++;
            }
            helper.DequeueItem();
        }

        public static void Session_SocketClosed(string reason)
        {
            message_log.Add("Lost connection to Archipelago server. " + reason);
            APH2Plugin.queueMessage.Add("Lost connection to Archipelago server.");
            Debug.LogError("Lost connection to Archipelago server. " + reason);
            Disconnect();
        }

        public static void Session_ErrorRecieved(Exception e, string message)
        {
            Debug.LogError(message);
            if (e != null) Debug.LogError(e.ToString());
            Disconnect();
        }

        public static void Disconnect()
        {
            if (Session != null && Session.Socket != null)
            {
                Session.Socket.Disconnect();
            }
            Session = null;
            Authenticated = false;
        }

        public static void DeathLinkReceieved(DeathLink deathLink)
        {
            DeathLinkKilling = true;
            if (!APH2Plugin.cutscenes.Contains(APH2Plugin.currentScene.name) && APH2Plugin.currentScene.name != "Battle Scene")
            {
                SceneManager.LoadScene("DeathScene");
            }
            if (deathLink.Cause != "")
            {
                message_log.Add("<color=#FA8072FF>" + deathLink.Cause + "</color>");
                APH2Plugin.queueMessage.Add(deathLink.Cause);
            }
            else
            {
                message_log.Add("<color=#FA8072FF>" + deathLink.Source + " has perished, and so have you.</color>");
                APH2Plugin.queueMessage.Add(deathLink.Source + " has perished, and so have you.");
            }
        }

        public static void Session_PacketRecieved(ArchipelagoPacketBase packet)
        {
            //Debug.Log("Incoming Packet: " + packet.PacketType.ToString());
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.PrintJSON:
                    {
                        var p = packet as PrintJsonPacket;
                        string text = "";
                        string color = "<color=#FFFFFFFF>";

                        // setup in-game message if a location has an item for a different player
                        if (p.Data[0].Type == JsonMessagePartType.PlayerId && Session.Players.GetPlayerName(int.Parse(p.Data[0].Text)) == ServerData.slot_name && p.Data[1].Text == " sent " && APH2Plugin.showPopups)
                        {
                            APH2Plugin.queueMessage.Add("Found " + Session.Items.GetItemName(long.Parse(p.Data[2].Text)) + " for " + Session.Players.GetPlayerAlias(int.Parse(p.Data[4].Text)) + ".");
                        }

                        foreach (var messagePart in p.Data)
                        {
                            switch (messagePart.Type)
                            {
                                case JsonMessagePartType.PlayerId:
                                    if (Session.Players.GetPlayerName(int.Parse(messagePart.Text)) == ServerData.slot_name) color = "<color=#EE00EEFF>";
                                    else color = "<color=#FAFAD2FF>";
                                    text += int.TryParse(messagePart.Text, out var playerSlot)
                                        ? color + Session.Players.GetPlayerAlias(playerSlot) + "</color>" ?? $"{color}Slot: {playerSlot}</color>"
                                        : $"{color}{messagePart.Text}</color>";
                                    break;
                                case JsonMessagePartType.ItemId:
                                    color = ItemFlagsToRGBA(messagePart.Flags);
                                    text += int.TryParse(messagePart.Text, out var itemId)
                                        ? color + Session.Items.GetItemName(itemId) + "</color>" ?? $"{color}Item: {itemId}</color>"
                                        : $"{color}{messagePart.Text}</color>";
                                    break;
                                case JsonMessagePartType.LocationId:
                                    color = "<color=#00FF7FFF>";
                                    text += int.TryParse(messagePart.Text, out var locationId)
                                        ? color + Session.Locations.GetLocationNameFromId(locationId) + "</color>" ?? $"{color}Location: {locationId}</color>"
                                        : $"{color}{messagePart.Text}</color>";
                                    break;
                                default:
                                    text += messagePart.Text;
                                    break;
                            }
                        }
                        message_log.Add(text);
                        break;
                    }
            }
        }

        public static string ItemFlagsToRGBA(ItemFlags? flags)
        {
            switch (flags)
            {
                case ItemFlags.Advancement:
                    return "<color=#AF99EFFF>";
                case ItemFlags.NeverExclude: // useful
                    return "<color=#6D8BE8FF>";
                case ItemFlags.Trap:
                    return "<color=#FA8072FF>";
                default:
                    return "<color=#00EEEEFF>";
            }
        }

        public static void set_deathlink()
        {
            if (DeathLinkService == null)
            {
                DeathLinkService = Session.CreateDeathLinkService();
                DeathLinkService.OnDeathLinkReceived += DeathLinkReceieved;
            }
            if (ServerData.death_link)
            {
                DeathLinkService.EnableDeathLink();
            }
            else
            {
                DeathLinkService.DisableDeathLink();
            }
        }

        public static void send_completion()
        {
            var statusUpdatePacket = new StatusUpdatePacket();
            statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
            Session.Socket.SendPacket(statusUpdatePacket);
        }

        // get IDs for server locations based on the activated ItemCollector's scene and ID
        public static long? IdentifyItemCheck(string scene, int id)
        {
            switch (scene)
            {
                case "StartHouse_Room1": // Waynehouse
                    switch (id)
                    {
                        case 13:
                            return 200622;
                        case 7:
                            return 200623;
                        case 3:
                            return 200624;
                        case 5:
                            return 200625;
                        case 0:
                            return 200626;
                        default:
                            return null;
                    }
                case "Afterlife_Island": // Afterlife
                    switch (id)
                    {
                        case 1:
                            return 200628;
                        case 2:
                            return 200629;
                        case 0:
                            return 200630;
                        default:
                            return null;
                    }
                case "Town_Scene_WithAdditions": // New Muldul
                    switch (id)
                    {
                        case 372:
                            return 200632;
                        case 158:
                            return 200633;
                        case 416:
                            return 200634;
                        case 18:
                            return 200635;
                        case 131:
                            return 200636;
                        case 114:
                            return 200637;
                        case 27:
                            return 200639;
                        case 417:
                            return 200640;
                        case 84:
                            return 200641;
                        case 54:
                            return 200785;
                        case 48:
                            if (ServerData.medallion_shuffle) return 200755;
                            else return null;
                        default:
                            return null;
                    }
                case "Town_VaultOnly": // New Muldul Vault
                    switch (id)
                    {
                        case 112:
                            return 200647;
                        case 12:
                            return 200648;
                        case 414:
                            return 200649;
                        case 370:
                            if (ServerData.medallion_shuffle) return 200756;
                            else return null;
                        case 227:
                            if (ServerData.medallion_shuffle) return 200757;
                            else return null;
                        case 134:
                            if (ServerData.medallion_shuffle) return 200758;
                            else return null;
                        case 182:
                            if (ServerData.medallion_shuffle) return 200759;
                            else return null;
                        case 222:
                            if (ServerData.medallion_shuffle) return 200760;
                            else return null;
                        default:
                            return null;
                    }
                case "BanditFort_Scene": // Viewax's Edifice
                    switch (id)
                    {
                        case 2:
                            return 200650;
                        case 126:
                            return 200651;
                        case 79:
                            return 200652;
                        case 106:
                            return 200655;
                        case 81:
                            return 200656;
                        case 124:
                            return 200657;
                        case 77:
                            return 200658;
                        case 34:
                            return 200659;
                        case 9945:
                            return 200660;
                        case 5:
                            return 200661;
                        case 120:
                            return 200664;
                        case 3:
                            if (ServerData.medallion_shuffle) return 200761;
                            else return null;
                        case 53:
                            if (ServerData.medallion_shuffle) return 200762;
                            else return null;
                        case 0:
                            if (ServerData.medallion_shuffle) return 200763;
                            else return null;
                        default:
                            return null;
                    }
                case "LD44 Scene": // Arcade 1
                    switch (id)
                    {
                        case 154:
                            return 200667;
                        case 95:
                            return 200668;
                        case 81:
                            return 200669;
                        case 145:
                            return 200671;
                        case 118:
                            return 200670;
                        case 22:
                            return 200672;
                        case 153:
                            return 200673;
                        case 117:
                            return 200674;
                        case 88:
                            if (ServerData.medallion_shuffle) return 200764;
                            else return null;
                        case 155:
                            if (ServerData.medallion_shuffle) return 200765;
                            else return null;
                        case 144:
                            if (ServerData.medallion_shuffle) return 200766;
                            else return null;
                        default:
                            return null;
                    }
                case "SecondArcade_Scene": // Arcade Island
                    if (id == 0)
                    {
                        return 200676;
                    }
                    else
                    {
                        return null;
                    }
                case "LD44_ChibiScene2_TheCarpetScene": // Arcade 2
                    switch (id)
                    {
                        case 1001:
                            return 200677;
                        case 999:
                            return 200678;
                        case 1000:
                            return 200679;
                        case 174:
                            return 200680;
                        case 22:
                            return 200681;
                        case 1002:
                            return 200682;
                        case 165:
                            if (ServerData.medallion_shuffle) return 200767;
                            else return null;
                        case 101:
                            if (ServerData.medallion_shuffle) return 200768;
                            else return null;
                        case 140:
                            if (ServerData.medallion_shuffle) return 200769;
                            else return null;
                        case 44:
                            if (ServerData.medallion_shuffle) return 200770;
                            else return null;
                        case 76:
                            if (ServerData.medallion_shuffle) return 200771;
                            else return null;
                        default:
                            return null;
                    }
                case "SomsnosaHouse_Scene": // Juice Ranch
                    switch (id)
                    {
                        case 80:
                            return 200684;
                        case 82:
                            return 200685;
                        case 79:
                            return 200686;
                        case 76:
                            return 200690;
                        default:
                            return null;
                    }
                case "MazeScene1": // Worm Pod
                    if (id == 140)
                    {
                        return 200692;
                    }
                    else
                    {
                        return null;
                    }
                case "Foglast_Exterior_Dry": // Foglast
                    switch (id)
                    {
                        case 5:
                            return 200693;
                        case 2:
                            return 200694;
                        case 30:
                            return 200695;
                        case 182:
                            return 200698;
                        case 200:
                            return 200699;
                        case 4:
                            return 200700;
                        case 6:
                            return 200701;
                        case 3:
                            return 200702;
                        case 0:
                            return 200703;
                        case 98:
                            if (ServerData.medallion_shuffle) return 200772;
                            else return null;
                        case 194:
                            if (ServerData.medallion_shuffle) return 200773;
                            else return null;
                        case 153:
                            if (ServerData.medallion_shuffle) return 200774;
                            else return null;
                        default:
                            return null;
                    }
                case "Foglast_SageRoom_Scene": // Foglast
                    if (id == 1)
                    {
                        return 200704;
                    }
                    else
                    {
                        return null;
                    }
                case "DrillCastle": // Drill Castle
                    switch (id)
                    {
                        case 94:
                            return 200707;
                        case 92:
                            return 200708;
                        case 95:
                            return 200709;
                        case 89:
                            return 200710;
                        case 91:
                            return 200711;
                        default:
                            return null;
                    }
                case "Dungeon_Labyrinth_Scene_Final": // Sage Labyrinth
                    switch (id)
                    {
                        case 8:
                            return 200713;
                        case 0:
                            return 200714;
                        case 7:
                            return 200715;
                        case 6:
                            return 200716;
                        case 10:
                            return 200717;
                        case 3:
                            return 200718;
                        case 1:
                            return 200719;
                        case 5:
                            return 200720;
                        case 1220:
                            return 200721;
                        case 9:
                            return 200722;
                        case 2:
                            return 200723;
                        case 1212:
                            return 200724;
                        case 1213:
                            return 200754;
                        case 4:
                            return 200725;
                        default:
                            return null;
                    }
                case "ThirdSageBeach_Scene": // Sage Labyrinth
                    switch (id)
                    {
                        case 3:
                            return 200728;
                        case 0:
                            return 200729;
                        case 1:
                            return 200730;
                        case 2:
                            return 200731;
                        default:
                            return null;
                    }
                case "BigAirship_Scene": // Sage Airship
                    switch (id)
                    {
                        case 30:
                            return 200732;
                        case 31:
                            return 200733;
                        case 29:
                            return 200734;
                        case 24:
                            if (ServerData.medallion_shuffle) return 200775;
                            else return null;
                        case 28:
                            if (ServerData.medallion_shuffle) return 200776;
                            else return null;
                        case 25:
                            if (ServerData.medallion_shuffle) return 200777;
                            else return null;
                        case 17:
                            if (ServerData.medallion_shuffle) return 200778;
                            else return null;
                        case 18:
                            if (ServerData.medallion_shuffle) return 200779;
                            else return null;
                        case 19:
                            if (ServerData.medallion_shuffle) return 200780;
                            else return null;
                        default:
                            return null;
                    }
                case "FlyingPalaceDungeon_Scene": // Hylemxylem
                    switch (id)
                    {
                        case 149:
                            return 200736;
                        case 115:
                            return 200737;
                        case 229:
                            return 200738;
                        case 261:
                            return 200739;
                        case 110:
                            return 200740;
                        case 230:
                            return 200741;
                        case 34:
                            return 200742;
                        case 131:
                            return 200743;
                        case 85:
                            return 200744;
                        case 221:
                            return 200745;
                        case 157:
                            return 200746;
                        case 120:
                            return 200747;
                        case 151:
                            return 200748;
                        case 256:
                            return 200749;
                        case 71:
                            return 200750;
                        case 20:
                            return 200751;
                        case 195:
                            return 200752;
                        case 212:
                            return 200753;
                        case 138:
                            if (ServerData.medallion_shuffle) return 200781;
                            else return null;
                        case 132:
                            if (ServerData.medallion_shuffle) return 200782;
                            else return null;
                        case 140:
                            if (ServerData.medallion_shuffle) return 200783;
                            else return null;
                        case 270:
                            if (ServerData.medallion_shuffle) return 200784;
                            else return null;
                        default:
                            return null;
                    }
                default:
                    return null;
            }

        }

        // get in-game IDs for items, equipment, gestures from names recieved from server
        public static int IdentifyItemGetID(string name)
        {
            switch (name)
            {
                case "DUBIOUS BERRY":
                    return 0;
                case "BURRITO":
                    return 2;
                case "COFFEE":
                    return 3;
                case "SOUL SPONGE":
                    return 4;
                case "MUSCLE APPLIQUE":
                    return 5;
                case "POOLWINE":
                    return 8;
                case "CUPCAKE":
                    return 9;
                case "COOKIE":
                    return 10;
                case "HOUSE KEY":
                    return 11;
                case "MEAT":
                    return 12;
                case "PNEUMATOPHORE":
                    return 14;
                case "CAVE KEY":
                    return 18;
                case "JUICE":
                    return 22;
                case "DOCK KEY":
                    return 23;
                case "BANANA":
                    return 24;
                case "PAPER CUP":
                    return 25;
                case "JAIL KEY":
                    return 26;
                case "PADDLE":
                    return 27;
                case "WORM ROOM KEY":
                    return 28;
                case "BRIDGE KEY":
                    return 29;
                case "STEM CELL":
                    return 31;
                case "UPPER CHAMBER KEY":
                    return 32;
                case "VESSEL ROOM KEY":
                    return 33;
                case "CLOUD GERM":
                    return 34;
                case "SKULL BOMB":
                    return 35;
                case "TOWER KEY":
                    return 36;
                case "DEEP KEY":
                    return 38;
                case "MULTI-COFFEE":
                    return 39;
                case "MULTI-JUICE":
                    return 40;
                case "MULTI STEM CELL":
                    return 42;
                case "MULTI SOUL SPONGE":
                    return 43;
                case "UPPER HOUSE KEY":
                    return 45;
                case "BOTTOMLESS JUICE":
                    return 46;
                case "SAGE TOKEN":
                    return 47;
                case "CLICKER":
                    return 48;

                case "CURSED GLOVES":
                    return 0;
                case "LONG GLOVES":
                    return 1;
                case "BRAIN DIGITS":
                    return 2;
                case "MATERIEL MITTS":
                    return 3;
                case "PLEATHER GAGE":
                    return 4;
                case "PEPTIDE BODKINS":
                    return 5;
                case "TELESCOPIC SLEEVE":
                    return 6;
                case "TENDRIL HAND":
                    return 7;
                case "PSYCHIC KNUCKLE":
                    return 8;
                case "SINGLE GLOVE":
                    return 9;

                case "FADED PONCHO":
                    return 0;
                case "JUMPSUIT":
                    return 1;
                case "BOOTS":
                    return 2;
                case "CONVERTER WORM":
                    return 3;
                case "COFFEE CHIP":
                    return 4;
                case "RANCHER PONCHO":
                    return 6;
                case "ORGAN FORT":
                    return 7;
                case "LOOPED DOME":
                    return 8;
                case "DUCTILE HABIT":
                    return 9;
                case "TARP":
                    return 10;

                case "POROMER BLEB":
                    return 4;
                case "SOUL CRISPER":
                    return 14;
                case "TIME SIGIL":
                    return 18;
                case "CHARGE UP":
                    return 68;
                case "FATE SANDBOX":
                    return 29;
                case "TELEDENUDATE":
                    return 37;
                case "LINK MOLLUSC":
                    return 43;
                case "BOMBO - GENESIS":
                    return 72;
                case "NEMATODE INTERFACE":
                    return 31;

                case "100 Bones":
                    return 100;
                case "50 Bones":
                    return 50;
                case "10 Bones":
                    return 10;

                default: return 0;
            }
        }

        private static List<string> items = new() { "DUBIOUS BERRY", "BURRITO", "COFFEE", "SOUL SPONGE", "MUSCLE APPLIQUE", "POOLWINE", "CUPCAKE", "COOKIE",
            "HOUSE KEY", "MEAT", "PNEUMATOPHORE", "CAVE KEY", "JUICE", "DOCK KEY", "BANANA", "PAPER CUP", "JAIL KEY", "PADDLE", "WORM ROOM KEY", "BRIDGE KEY",
            "STEM CELL", "UPPER CHAMBER KEY", "VESSEL ROOM KEY", "CLOUD GERM", "SKULL BOMB", "TOWER KEY", "DEEP KEY", "MULTI-COFFEE", "MULTI-JUICE", "MULTI STEM CELL",
            "MULTI SOUL SPONGE", "UPPER HOUSE KEY", "BOTTOMLESS JUICE", "SAGE TOKEN", "CLICKER" };
        private static List<string> gloves = new() { "CURSED GLOVES", "LONG GLOVES", "BRAIN DIGITS", "MATERIEL MITTS", "PLEATHER GAGE", "PEPTIDE BODKINS",
            "TELESCOPIC SLEEVE", "TENDRIL HAND", "PSYCHIC KNUCKLE", "SINGLE GLOVE" };
        private static List<string> accessories = new() { "FADED PONCHO", "JUMPSUIT", "BOOTS", "CONVERTER WORM", "COFFEE CHIP", "RANCHER PONCHO", "ORGAN FORT", "LOOPED DOME", "DUCTILE HABIT", "TARP" };
        private static List<string> gestures = new() { "POROMER BLEB", "SOUL CRISPER", "TIME SIGIL", "CHARGE UP", "FATE SANDBOX", "TELEDENUDATE", "LINK MOLLUSC", "BOMBO - GENESIS", "NEMATODE INTERFACE" };
        private static List<string> party = new() { "Pongorma", "Dedusmuln", "Somsnosa" };

        // get type of item based on name from server
        public static string IdentifyItemGetType(string name)
        {
            if (items.Contains(name))
            {
                return "THING";
            }
            else if (gloves.Contains(name))
            {
                return "GLOVE";
            }
            else if (accessories.Contains(name))
            {
                return "ACCESSORY";
            }
            else if (gestures.Contains(name))
            {
                return "GESTURE";
            }
            else if (party.Contains(name))
            {
                return "PARTY";
            }
            else
            {
                return "BONES";
            }
        }

    }
}