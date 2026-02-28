using System.Collections.Generic;

namespace DM_AmuletBedSpawn.Configuration
{
    public class AmuletBedSpawnConfig
    {
        public bool AmuletsCanBreakAfterRespawn { get; set; } = false;
        public int RustyGearAmuletBreakChancePct { get; set; } = 30;
        public int TemporalGearAmuletBreakChancePct { get; set; } = 5;
    }
}
