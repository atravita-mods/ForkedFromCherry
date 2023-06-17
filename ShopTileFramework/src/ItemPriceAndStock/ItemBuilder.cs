﻿using Microsoft.Xna.Framework;
using ShopTileFramework.Utility;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System.Collections.Generic;

namespace ShopTileFramework.ItemPriceAndStock
{
    /// <summary>
    /// This class stores the global data for each itemstock, in order to generate and add items by ID or name
    /// to the stock
    /// </summary>
    class ItemBuilder
    {
        private Dictionary<ISalable, int[]> _itemPriceAndStock;
        private readonly ItemStock _itemStock;

        public ItemBuilder(ItemStock itemStock)
        {
            this._itemStock = itemStock;
        }

        /// <param name="itemPriceAndStock">the ItemPriceAndStock this builder will add items to</param>
        public void SetItemPriceAndStock(Dictionary<ISalable, int[]> itemPriceAndStock)
        {
            this._itemPriceAndStock = itemPriceAndStock;
        }

        /// <summary>
        /// Takes an item name, and adds that item to the stock
        /// </summary>
        /// <param name="itemName">name of the item</param>
        /// <param name="priceMultiplier"></param>
        /// <returns></returns>
        public bool AddItemToStock(string itemName, double priceMultiplier = 1)
        {
            int id = ItemsUtil.GetIndexByName(itemName, this._itemStock.ItemType);
            if (id < 0)
            {
                ModEntry.monitor.Log($"{this._itemStock.ItemType} named \"{itemName}\" could not be added to the Shop {this._itemStock.ShopName}", LogLevel.Trace);
                return false;
            }

            return this.AddItemToStock(id, priceMultiplier);
        }

        /// <summary>
        /// Takes an item id, and adds that item to the stock
        /// </summary>
        /// <param name="itemId">the id of the item</param>
        /// <param name="priceMultiplier"></param>
        /// <returns></returns>
        public bool AddItemToStock(int itemId, double priceMultiplier = 1)
        {

            if (ModEntry.VerboseLogging)
                ModEntry.monitor.Log($"Adding item ID {itemId} to {this._itemStock.ShopName}", LogLevel.Debug);

            if (itemId < 0)
            {
                ModEntry.monitor.Log($"{this._itemStock.ItemType} of ID {itemId} could not be added to the Shop {this._itemStock.ShopName}", LogLevel.Trace);
                return false;
            }

            if (this._itemStock.ItemType == "Seed" && this._itemStock.FilterSeedsBySeason)
            {
                if (!ItemsUtil.IsInSeasonCrop(itemId)) return false;
            }

            var item = this.CreateItem(itemId);
            if (item == null)
            {
                return false;
            }

            if (this._itemStock.IsRecipe)
            {
                if (!ItemsUtil.RecipesList.Contains(item.Name))
                {
                    ModEntry.monitor.Log($"{item.Name} is not a valid recipe and won't be added.", LogLevel.Trace);
                    return false;
                }
            }

            var priceStockCurrency = this.GetPriceStockAndCurrency(item, priceMultiplier);
            this._itemPriceAndStock.Add(item, priceStockCurrency);

            return true;       
        }

        /// <summary>
        /// Given an itemID, return an instance of that item with the parameters saved in this builder
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private ISalable CreateItem(int itemId)
        {
            switch (this._itemStock.ItemType)
            {
                case "Object":
                case "Seed":
                    return new Object(itemId, this._itemStock.Stock, this._itemStock.IsRecipe, quality: this._itemStock.Quality);
                case "BigCraftable":
                    return new Object(Vector2.Zero, itemId) { Stack = this._itemStock.Stock, IsRecipe = this._itemStock.IsRecipe };
                case "Clothing":
                    return new Clothing(itemId);
                case "Ring":
                    return new Ring(itemId);
                case "Hat":
                    return new Hat(itemId);
                case "Boot":
                    return new Boots(itemId);
                case "Furniture":
                    return new Furniture(itemId, Vector2.Zero);
                case "Weapon":
                    return new MeleeWeapon(itemId);
                case "Wallpaper":
                    return new Wallpaper(itemId);
                case "Floors":
                    return new Wallpaper(itemId, true);
                default: return null;
            }
        }

        /// <summary>
        /// Creates the second parameter in ItemStockAndPrice, an array that holds info on the price, stock,
        /// and if it exists, the item currency it takes
        /// </summary>
        /// <param name="item">An instance of the item</param>
        /// <param name="priceMultiplier"></param>
        /// <returns>The array that's the second parameter in ItemPriceAndStock</returns>
        private int[] GetPriceStockAndCurrency(ISalable item, double priceMultiplier)
        {
            int[] priceStockCurrency;
            //if no price is provided, use the item's sale price multiplied by defaultSellPriceMultiplier
            var price = (this._itemStock.StockPrice == -1) ? (int)(item.salePrice()* this._itemStock.DefaultSellPriceMultiplier) : this._itemStock.StockPrice;
            price = (int)(price*priceMultiplier);

            if (this._itemStock.CurrencyObjectId == -1) // no currency item
            {
                priceStockCurrency = new[] { price, this._itemStock.Stock };
            }
            else if (this._itemStock.StockCurrencyStack == -1) //no stack provided for currency item so defaults to 1
            {
                priceStockCurrency = new[] { price, this._itemStock.Stock, this._itemStock.CurrencyObjectId };
            }
            else //both currency item and stack provided
            {
                priceStockCurrency = new[] { price, this._itemStock.Stock, this._itemStock.CurrencyObjectId, this._itemStock.StockCurrencyStack };
            }

            return priceStockCurrency;
        }
    }
}
