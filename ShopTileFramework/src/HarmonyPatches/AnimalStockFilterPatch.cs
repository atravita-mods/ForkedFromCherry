using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using StardewValley.Menus;

namespace ShopTileFramework.src.HarmonyPatches;
internal static class AnimalStockFilterPatch
{
    public static void Apply(Harmony harmony)
    {

    }

    private static void Prefix(ref Action<PurchaseAnimalsMenu> onMenuOpened)
    {
        if (onMenuOpened is null)
        {
            onMenuOpened = FilterAnimals;
        }
        else
        {
            onMenuOpened += FilterAnimals;
        }
    }

    // TODO
    private static void FilterAnimals(PurchaseAnimalsMenu menu)
    {
        menu.RepositionAnimalButtons();
    }
}
