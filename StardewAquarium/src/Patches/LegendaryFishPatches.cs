using HarmonyLib;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;
using Object = StardewValley.Object;

namespace StardewAquarium.Patches
{
    static class LegendaryFishPatches
    {
        private static string? PufferChickID => ModEntry.JsonAssets?.GetObjectId(ModEntry.PufferChickName);
        private static string? LegendaryBaitId => ModEntry.JsonAssets?.GetObjectId(ModEntry.LegendaryBaitName);

        private const string AnglerId = "160";
        private const string LegendId = "163";
        private const string MutantCarpId = "682";

        public static void Initialize()
        {
            Harmony harmony = ModEntry.Harmony;

            //patch handles making the pufferchick catchable
            harmony.Patch(
                AccessTools.Method(typeof(GameLocation), nameof(GameLocation.getFish)),
                new HarmonyMethod(typeof(LegendaryFishPatches), nameof(GameLocation_getFish_Prefix))
            );
        }


        public static bool Sewer_getFish_prefix(Farmer who, ref Item __result)
        {
            if (
                Game1.player?.CurrentTool is not FishingRod rod
                || rod.GetBait()?.ItemId != LegendaryBaitId
                || !who.fishCaught.ContainsKey(MutantCarpId)
            )
                return true;

            __result = new Object(MutantCarpId, 1);
            return false;

        }
        public static bool Mountain_getFish_prefix(int waterDepth, Farmer who, ref Item __result)
        {
            if (
                Game1.player?.CurrentTool is not FishingRod rod
                || rod.GetBait()?.ItemId != LegendaryBaitId
                || !Game1.isRaining
                || who.FishingLevel < 10
                || waterDepth < 4
                || !who.fishCaught.ContainsKey(LegendId)
                || Game1.season != Season.Spring
            )
                return true;

            __result = new Object(LegendId, 1);
            return false;
        }

        public static bool Town_getFish_prefix(Farmer who, ref Item __result)
        {
            if (
                Game1.player?.CurrentTool is not FishingRod rod
                || rod.GetBait()?.ItemId != LegendaryBaitId
                || !(who.Tile.Y < 15f)
                || who.FishingLevel < 3
                || !who.fishCaught.ContainsKey(AnglerId)
                || Game1.season != Season.Fall
            )
                return true;

            __result = new Object(AnglerId, 1);
            return false;
        }

        public static bool GameLocation_getFish_Prefix(GameLocation __instance, Farmer who, int waterDepth, ref Item __result)
        {
            //checks if player should get pufferchick
            switch (__instance)
            {
                case Town:
                    return Town_getFish_prefix(who, ref __result);

                case Mountain:
                    return Mountain_getFish_prefix(waterDepth, who, ref __result);

                case Sewer:
                    return Sewer_getFish_prefix(who, ref __result);

                default:
                    {
                        Object pufferchick = GetFishPufferchick(__instance, who);
                        if (pufferchick is null)
                            return true;

                        __result = pufferchick;
                        return false;
                    }
            }
        }

        private static Object GetFishPufferchick(GameLocation loc, Farmer who)
        {
            if (loc.Name != ModEntry.Data.ExteriorMapName) //only happens on the exterior museum map
                return null;

            if (!who.fishCaught.ContainsKey("128")) //has caught a pufferfish before
                return null;

            if (who.stats.ChickenEggsLayed == 0) //has had a chicken lay at least one egg
                return null;

            if (who.CurrentTool is FishingRod rod && rod.GetBait()?.ItemId == LegendaryBaitId)
            {
                return new Object(PufferChickID, 1);
            }
            if (who.fishCaught.ContainsKey(PufferChickID)) return null;

            //base of 1% and an additional 0.5% per fish donated
            double pufferChance = 0.01 + 0.005 * Utils.GetNumDonatedFish();

            if (Game1.random.NextDouble() > pufferChance)
                return null;

            Object pufferchick = new Object(PufferChickID, 1);
            pufferchick.SetTempData("IsBossFish", true); //Make pufferchick boss fish in 1.6+
            return pufferchick;
        }
    }
}
