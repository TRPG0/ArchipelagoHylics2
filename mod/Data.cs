using System.Collections.Generic;

namespace ArchipelagoHylics2
{
    public class APData
    {
        public long index;
        public string host_name;
        public string slot_name;
        public string password;
        public bool party_shuffle = false;
        public bool medallion_shuffle = false;
        public string start_location = "?";
        public bool visited_waynehouse = false;
        public bool death_link = false;
        public HashSet<long> @checked = new HashSet<long>();

        public bool has_pongorma = false;
        public bool has_dedusmuln = false;
        public bool has_somsnosa = false;

        public int checked_waynehouse = 0;
        public int checked_afterlife = 0;
        public int checked_new_muldul = 0;
        public int checked_new_muldul_vault = 0;
        public int checked_pongorma = 0;
        public int checked_blerol1 = 0;
        public int checked_blerol2 = 0;
        public int checked_viewaxs_edifice = 0;
        public int checked_arcade1 = 0;
        public int checked_airship = 0;
        public int checked_arcade_island = 0;
        public int checked_arcade2 = 0;
        public int checked_tv_island = 0;
        public int checked_juice_ranch = 0;
        public int checked_worm_pod = 0;
        public int checked_foglast = 0;
        public int checked_drill_castle = 0;
        public int checked_sage_labyrinth = 0;
        public int checked_sage_airship = 0;
        public int checked_hylemxylem = 0;
    }
}