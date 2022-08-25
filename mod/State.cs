using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Helpers;
using WebSocketSharp;
using UnityEngine;
using ORKFramework;
using UnityEngine.SceneManagement;

namespace ArchipelagoHylics2
{
    public static class APState
    {
        public enum State
        {
            Menu,
            InGame
        }

        public static int[] AP_VERSION = new int[] { 0, 3, 4 };
        public static APData ServerData = new();
        public static DeathLinkService DeathLinkService = null;
        // to add: locations
        public static bool DeathLinkKilling = false; // indicates player is currently being deathlinked
        public static Dictionary<string, int> archipelago_indexes = new Dictionary<string, int>();
        public static List<string> message_log = new List<string> { "Hylics 2 | Archipelago | " + APH2Plugin.PluginVersion, 
            "Available commands: /connect, /disconnect, /popups, /airship, /deathlink, /help" };
        public static State state = State.Menu;
        public static bool party_shuffle = false;
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
                "",
                ServerData.password == "" ? null : ServerData.password);

            if (loginResult is LoginSuccessful loginSuccess)
            {
                Authenticated = true;
                state = State.InGame;
                Debug.Log("Successfully connected to server!");

                if (loginSuccess.SlotData["party_shuffle"].ToString() == "1")
                {
                    party_shuffle = true;
                    APH2Plugin.ReloadEvents();
                }
                if (loginSuccess.SlotData["death_link"].ToString() == "1") ServerData.death_link = true;
                set_deathlink();
            }
            else if (loginResult is LoginFailure loginFailure)
            {
                Authenticated = false;
                Debug.LogError("Connection Error: " + String.Join("\n", loginFailure.Errors));
                Session.Socket.Disconnect();
                Session = null;
            }
            return loginResult.Successful;
        }

        public static void Session_ItemRecieved(ReceivedItemsHelper helper)
        {
            Debug.Log(helper.PeekItemName());
            Debug.Log(helper.PeekItem().Player);
            string type = APData.IdentifyItemGetType(helper.PeekItemName());
            string player = Session.Players.GetPlayerName(helper.PeekItem().Player);
            bool self = false;

            if (player == ServerData.slot_name) self = true;

            if (type == "THING")
            {
                APH2Plugin.APRecieveItem(APData.IdentifyItemGetID(helper.PeekItemName()), player, self);
                if (helper.PeekItemName() == "PNEUMATOPHORE") ORK.Game.Variables.Set("AirDashBool", true);
                if (helper.PeekItemName() == "DOCK KEY") ORK.Game.ActiveGroup.Leader.Inventory.Add(new ItemShortcut(37, 1), false, false, false);
            }
            else if (type == "GLOVE")
            {
                APH2Plugin.APRecieveEquip(APData.IdentifyItemGetID(helper.PeekItemName()), "GLOVE", player, self);
            }
            else if (type == "ACCESSORY")
            {
                APH2Plugin.APRecieveEquip(APData.IdentifyItemGetID(helper.PeekItemName()), "ACCESSORY", player, self);

            }
            else if (type == "GESTURE")
            {
                APH2Plugin.APRecieveAbility(APData.IdentifyItemGetID(helper.PeekItemName()), player, self);
            }
            else if (type == "BONES")
            {
                APH2Plugin.APRecieveMoney(APData.IdentifyItemGetID(helper.PeekItemName()), player, self);
            }
            else if (type == "PARTY")
            {
                APH2Plugin.APRecieveParty(helper.PeekItemName(), player, self);
            }

            helper.DequeueItem();
        }

        public static void Session_SocketClosed(string reason)
        {
            message_log.Add("Connection to Archipelago server was lost: " + reason);
            Debug.LogError("Connection to Archipelago server was lost: " + reason);
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
            state = State.Menu;
        }

        public static void DeathLinkReceieved(DeathLink deathLink)
        {
            DeathLinkKilling = true;
            if (!APH2Plugin.cutscenes.Contains(APH2Plugin.currentScene.name) && APH2Plugin.currentScene.name != "Battle Scene")
            {
                SceneManager.LoadScene("DeathScene", LoadSceneMode.Single);
            }
            message_log.Add(deathLink.Cause);
        }

        public static void Session_PacketRecieved(ArchipelagoPacketBase packet)
        {
            //Debug.Log("Incoming Packet: " + packet.PacketType.ToString());
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.Print:
                    {
                        var p = packet as PrintPacket;
                        message_log.Add(p.Text);
                        break;
                    }

                case ArchipelagoPacketType.PrintJSON:
                    {
						var p = packet as PrintJsonPacket;
						string text = "";
						foreach (var messagePart in p.Data)
						{
							switch (messagePart.Type)
							{
								case JsonMessagePartType.PlayerId:
									text += int.TryParse(messagePart.Text, out var playerSlot)
										? Session.Players.GetPlayerAlias(playerSlot) ?? $"Slot: {playerSlot}"
										: messagePart.Text;
									break;
								case JsonMessagePartType.ItemId:
									text += int.TryParse(messagePart.Text, out var itemId)
										? Session.Items.GetItemName(itemId) ?? $"Item: {itemId}"
										: messagePart.Text;
									break;
								case JsonMessagePartType.LocationId:
									text += int.TryParse(messagePart.Text, out var locationId)
										? Session.Locations.GetLocationNameFromId(locationId) ?? $"Location: {locationId}"
										: messagePart.Text;
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

        
    }
}