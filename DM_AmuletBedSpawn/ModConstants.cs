using System;
using System.Collections.Generic;
using System.Text;

namespace DM_AmuletBedSpawn
{
    internal static class ModConstants
    {
        public const string BedIsMissing = "AmuletBedSpawn:BedMissing";
        public const string ConfigFileName = "AmuletBedSpawn.json";
        public const string SpawnSetByAmuletBedSpawnMod = "AmuletBedSpawn:SpawnSetByAmuletBedSpawnMod";
        public const string RustyAmuletUsed = "AmuletBedSpawn:RustyAmuletUsed";
        public const string TemporalAmuletUsed = "AmuletBedSpawn:TemporalAmuletUsed";
    }

    public enum AmuletType
    {
        None,
        TemporalGearAmulet,
        RustyGearAmulet
    }

    internal static class AmuletTypeExtensions
    {
        public static string ToItemName(this AmuletType amuletType) => amuletType switch
        {
            AmuletType.TemporalGearAmulet => "Temporal gear amulet",
            AmuletType.RustyGearAmulet => "Rusty gear amulet",
            _ => string.Empty
        };
    }
}
