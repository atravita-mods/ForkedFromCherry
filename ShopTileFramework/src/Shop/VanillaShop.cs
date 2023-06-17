using ShopTileFramework.Data;
using ShopTileFramework.ItemPriceAndStock;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace ShopTileFramework.Shop
{
    class VanillaShop : VanillaShopModel
    {
        public List<ItemPriceAndStockManager> StockManagers { get; set; }
        public Dictionary<ISalable, int[]> ItemPriceAndStock { get; set; }
        public void Initialize()
        {
            this.StockManagers = new List<ItemPriceAndStockManager>();
        }

        public void UpdateItemPriceAndStock()
        {
            this.ItemPriceAndStock = new Dictionary<ISalable, int[]>();
            ModEntry.monitor.Log($"Generating stock for {this.ShopName}", LogLevel.Debug);
            foreach(ItemPriceAndStockManager manager in this.StockManagers)
            {
                manager.Update();
                this.ItemPriceAndStock = this.ItemPriceAndStock.Concat(manager.ItemPriceAndStock).ToDictionary(x => x.Key, x => x.Value);
            }
        }
    }
}
