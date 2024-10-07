using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewModdingAPI.Events;
using StardewValley.Objects;
using StardewValley;

using SObject = StardewValley.Object;
using StardewModdingAPI;

namespace StardewAquarium.src.Framework;
internal static class CrabPotHandler
{
    private static IMonitor Monitor = null!;
    internal static void Init(IGameLoopEvents events, IMonitor monitor)
    {
        Monitor = monitor;
        events.DayStarted += OnDayStart;
    }

    /// <summary>
    /// Updates crab pots
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private static void OnDayStart(object sender, DayStartedEventArgs e)
    {
        GameLocation loc = Game1.getLocationFromName(ModEntry.Data.ExteriorMapName);
        if (loc is null)
            return;

        // the original method actually flat out re-implemented the code for crab pots. We're...not going to do that anymore.
        // it was meant to mimic crabpot behavior on the beach, which is now in the data.

        // HOWEVER that wasn't actually what it did. Instead, it just...caught something. Even if not baited.
        // We will mimic this by baiting the crabpots ourselves.

        foreach (SObject obj in loc.objects.Values)
        {
            if (obj is not CrabPot pot || (pot.heldObject.Value is not null && pot.heldObject.Value.Category != SObject.junkCategory))
            {
                continue;
            }

            try
            {
                pot.bait.Value ??= new SObject("685", 1); // normal bait.
                pot.DayUpdate();
            }
            catch (Exception ex)
            {
                Monitor.Log(ex.ToString());
            }
        }
    }
}