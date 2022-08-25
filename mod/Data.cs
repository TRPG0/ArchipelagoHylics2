using System.Collections.Generic;

namespace ArchipelagoHylics2
{
    public class APData
    {
        public long index;
        public string host_name;
        public string slot_name;
        public string password;
        public HashSet<long> @checked = new HashSet<long>();
        public bool death_link = false;

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
                        default:
                            return null;
                    }
                default:
                    return null;
            }

        }

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
                case "TELESCOPIC SLEEVES":
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

                default: return 0;
            }
        }

        private static List<string> items = new() { "DUBIOUS BERRY", "BURRITO", "COFFEE", "SOUL SPONGE", "MUSCLE APPLIQUE", "POOLWINE", "CUPCAKE", "COOKIE",
            "HOUSE KEY", "MEAT", "PNEUMATOPHORE", "CAVE KEY", "JUICE", "DOCK KEY", "BANANA", "PAPER CUP", "JAIL KEY", "PADDLE", "WORM ROOM KEY", "BRIDGE KEY",
            "STEM CELL", "UPPER CHAMBER KEY", "VESSEL ROOM KEY", "CLOUD GERM", "SKULL BOMB", "TOWER KEY", "DEEP KEY", "MULTI-COFFEE", "MULTI-JUICE", "MULTI STEM CELL",
            "MULTI SOUL SPONGE", "UPPER HOUSE KEY", "BOTTOMLESS JUICE", "SAGE TOKEN", "CLICKER" };
        private static List<string> gloves = new() { "CURSED GLOVES", "LONG GLOVES", "BRAIN DIGITS", "MATERIEL MITTS", "PLEATHER GAGE", "PEPTIDE BODKINS",
            "TELESCOPIC SLEEVES", "TENDRIL HAND", "PSYCHIC KNUCKLE", "SINGLE GLOVE" };
        private static List<string> accessories = new() { "FADED PONCHO", "JUMPSUIT", "BOOTS", "CONVERTER WORM", "COFFEE CHIP", "RANCHER PONCHO", "ORGAN FORT", "LOOPED DOME", "DUCTILE HABIT", "TARP" };
        private static List<string> gestures = new() { "POROMER BLEB", "SOUL CRISPER", "TIME SIGIL", "CHARGE UP", "FATE SANDBOX", "TELEDENUDATE", "LINK MOLLUSC", "BOMBO - GENESIS" };
        private static List<string> party = new() { "Pongorma", "Dedusmuln", "Somsnosa" };

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