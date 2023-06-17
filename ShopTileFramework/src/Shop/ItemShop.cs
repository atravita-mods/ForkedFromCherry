using Microsoft.Xna.Framework.Graphics;
using ShopTileFramework.Data;
using ShopTileFramework.ItemPriceAndStock;
using ShopTileFramework.Utility;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using ShopTileFramework.API;

namespace ShopTileFramework.Shop
{
    /// <summary>
    /// This class holds all the information for each custom item shop
    /// </summary>
    class ItemShop : ItemShopModel
    {
        private Texture2D _portrait;
        public ItemPriceAndStockManager StockManager { get; set; }

        public IContentPack ContentPack { set; get; }

        /// <summary>
        /// This is used to make sure that JA only adds items to this shop the first time it is opened each day
        /// or else items will be added every time the shop is opened
        /// </summary>
        private bool _shopOpenedToday;

        /// <summary>
        /// Initializes the stock manager, done at game loaded so that content packs have finished loading in
        /// </summary>
        public void Initialize()
        {
            this.StockManager = new ItemPriceAndStockManager(this);
        }

        /// <summary>
        /// Loads the portrait, if it exists, and use the seasonal version if one is found for the current season
        /// </summary>
        public void UpdatePortrait()
        {
            if (this.PortraitPath == null)
                return;

            //construct seasonal path to the portrait
            string seasonalPath = this.PortraitPath.Insert(this.PortraitPath.IndexOf('.'), "_" + Game1.currentSeason);
            try
            {
                //if the seasonal version exists, load it
                if (this.ContentPack.HasFile(seasonalPath)) 
                {
                    this._portrait = this.ContentPack.LoadAsset<Texture2D>(seasonalPath);
                }
                //if the seasonal version doesn't exist, try to load the default
                else if (this.ContentPack.HasFile(this.PortraitPath))
                {
                    this._portrait = this.ContentPack.LoadAsset<Texture2D>(this.PortraitPath);
                }
            }
            catch (Exception ex) //couldn't load the image
            {
                ModEntry.monitor.Log(ex.Message+ex.StackTrace, LogLevel.Error);
            }
        }
        /// <summary>
        /// Refreshes the contents of all stores
        /// and sets the flag for if the store has been opened yet today to false
        /// </summary>
        public void UpdateItemPriceAndStock()
        {
            this._shopOpenedToday = false;
            ModEntry.monitor.Log($"Generating stock for {this.ShopName}", LogLevel.Debug);
            this.StockManager.Update();
        }

        /// <summary>
        /// Opens the shop if conditions are met. If not, display the closed message
        /// </summary>
        public void DisplayShop(bool debug = false)
        {
            ModEntry.monitor.Log($"Attempting to open the shop \"{this.ShopName}\"", LogLevel.Trace);

            //if conditions aren't met, display closed message if there is one
            //skips condition checking if debug mode
            if (!debug && !APIs.Conditions.CheckConditions(this.When))
            {
                if (this.ClosedMessage != null)
                {
                    Game1.activeClickableMenu = new DialogueBox(this.ClosedMessage);
                }

                return;
            }

            int currency = 0;
            switch (this.StoreCurrency)
            {
                case "festivalScore":
                    currency = 1;
                    break;
                case "clubCoins":
                    currency = 2;
                    break;
            }

            var shopMenu = new ShopMenu(this.StockManager.ItemPriceAndStock, currency: currency);

            if (this.CategoriesToSellHere != null)
                shopMenu.categoriesToSellHere = this.CategoriesToSellHere;

            if (this._portrait != null)
            {
                shopMenu.portraitPerson = new NPC();
                //only add a shop name the first time store is open each day so that items added from JA's side are only added once
                if (!this._shopOpenedToday)
                    shopMenu.portraitPerson.Name = "STF." + this.ShopName;

                shopMenu.portraitPerson.Portrait = this._portrait;
            }

            if (this.Quote != null)
            {
                shopMenu.potraitPersonDialogue = Game1.parseText(this.Quote, Game1.dialogueFont, 304);
            }

            Game1.activeClickableMenu = shopMenu;
            this._shopOpenedToday = true;
        }

        /// <summary>
        /// Translate what needs to be translated on game saved, in case of the language being changed
        /// </summary>
        internal void UpdateTranslations()
        {
            this.Quote = Translations.Localize(this.Quote, this.LocalizedQuote);
            this.ClosedMessage = Translations.Localize(this.ClosedMessage, this.LocalizedClosedMessage);
        }
    }
}
