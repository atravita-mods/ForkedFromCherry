using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopTileFramework.src.Utility;
internal static class LegacyUtils
{
    private static readonly Dictionary<string, string> STFToVanillaShopNames = new();

    /// <summary>
    /// Parses the EPU conditions to the closest GameStateQuery.
    /// </summary>
    /// <param name="conditions">EPU query.</param>
    /// <param name="result">The GameStateQuery if true, a player-readable error message if false.</param>
    /// <returns>True if parse-able, false otherwise.</returns>
    internal static bool TryParseEPUConditionsToGSQ(string[]? conditions, out string? result)
    {
        // no conditions.
        if (conditions?.Length is 0 or null)
        {
            result = null;
            return true;
        }

        List<string> gsq = new(conditions.Length);
        foreach (var condition in conditions)
        {
            string[] parts = condition.Split('/');
            foreach (var part in parts)
            {
            }
        }

        result = null;
        return false;
    }
}
