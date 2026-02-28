using Vintagestory.API.Common;

namespace DM_AmuletBedSpawn.Configuration
{
    public static class ModConfig
    {
        public static AmuletBedSpawnConfig ReadConfig(ICoreAPI api)
        {
            try
            {
                var config = LoadConfig(api);

                if (config == null)
                {
                    SaveOrCreateConfig(api);
                    config = LoadConfig(api);
                }

                // Re-save the config to add any missing properties to the json file
                SaveOrCreateConfig(api, config);

                return config;
            }
            catch
            {
                SaveOrCreateConfig(api);

                return LoadConfig(api);
            }
        }

        private static AmuletBedSpawnConfig LoadConfig(ICoreAPI api)
        {
            return api.LoadModConfig<AmuletBedSpawnConfig>(ModConstants.ConfigFileName);
        }

        private static void SaveOrCreateConfig(ICoreAPI api, AmuletBedSpawnConfig config = default)
        {
            api.StoreModConfig(config ?? new(), ModConstants.ConfigFileName);
        }
    }
}
