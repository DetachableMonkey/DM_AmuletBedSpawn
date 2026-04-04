using System.Collections.Generic;

namespace DM_AmuletBedSpawn.Configuration
{
    public class AmuletBedSpawnConfig
    {
        public bool AllowHayBeds { get; set; } = true;
        public bool AllowRustyGearAmulets { get; set; } = true;
        public bool AmuletsCanBreakAfterRespawn { get; set; } = false;
        public bool BreakingBedRemovesSpawnPoint { get; set; } = true;
        public int RustyGearAmuletBreakChancePct { get; set; } = 30;
        public int TemporalGearAmuletBreakChancePct { get; set; } = 5;
    }
}
