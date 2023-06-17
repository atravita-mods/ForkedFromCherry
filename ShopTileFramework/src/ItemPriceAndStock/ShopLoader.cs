using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using ShopTileFramework.Data;
using ShopTileFramework.ItemPriceAndStock;

using StardewModdingAPI.Events;

using StardewValley.GameData.Shops;

namespace ShopTileFramework.src.ItemPriceAndStock;
internal static class ShopLoader
{
    private static readonly ConcurrentDictionary<string, ShopData> ShopsToInject = new();

    internal static void Load(ContentPack content)
    {

    }

    internal static void ApplyEdits(AssetRequestedEventArgs e)
    {

    }

    private static bool TryParseToShopData(ItemStockModel model, [NotNullWhen(true)] out ShopItemData? shopItemData)
    {
        ShopItemData item = new()
        {

        };
        shopItemData = null;
        return false;
    }
}
