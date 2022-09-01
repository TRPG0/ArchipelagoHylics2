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
        public bool death_link = false;
        public HashSet<long> @checked = new HashSet<long>();

        public int checked_waynehouse = 0;        // total = 5
        public int checked_afterlife = 0;         // total = 3
        public int checked_new_muldul = 0;        // total = 10
        public int checked_new_muldul_vault = 0;  // total = 5  (6 with party_shuffle)
        public int checked_viewaxs_edifice = 0;   // total = 22 (23 with party_shuffle)
        public int checked_airship = 0;           // total = 1
        public int checked_arcade_island = 0;     // total = 7
        public int checked_tv_island = 0;         // total = 1
        public int checked_juice_ranch = 0;       // total = 5  (6 with party_shuffle)
        public int checked_worm_pod = 0;          // total = 1
        public int checked_foglast = 0;           // total = 13
        public int checked_drill_castle = 0;      // total = 5
        public int checked_sage_labyrinth = 0;    // total = 20
        public int checked_sage_airship = 0;      // total = 3
        public int checked_hylemxylem = 0;        // total = 18
    }
}