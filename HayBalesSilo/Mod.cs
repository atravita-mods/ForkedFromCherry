﻿using StardewModdingAPI;
using StardewValley;
using HarmonyLib;
using StardewModdingAPI.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using StardewValley.Menus;
using StardewValley.Locations;
using System.Linq;
using StardewValley.Buildings;
using GenericModConfigMenu;

namespace HayBalesSilo
{
    public class ModEntry : Mod
    {
        internal static IMonitor monitor;
        internal static ModConfig Config;
        public override void Entry(IModHelper helper)
        {
            monitor = Monitor;
            Config = helper.ReadConfig<ModConfig>();

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Utility), nameof(Utility.numSilos)),
                postfix: new HarmonyMethod(typeof(PatchNumSilos), nameof(PatchNumSilos.Postfix))
                );

            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            helper.Events.Display.MenuChanged += Display_MenuChanged;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        // Add GMCM compatibility
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var gmcm = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm is null) return;

            gmcm.Register(
                mod: this.ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config)
            );

            gmcm.AddBoolOption(
                mod: this.ModManifest,
                name: () => "RequiresConstructedSilo",
                tooltip: () => "Whether you need to construct at least one Silo for hay bales to work",
                getValue: () => Config.RequiresConstructedSilo,
                setValue: value => Config.RequiresConstructedSilo = value
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                name: () => "HayBaleEquivalentToHowManySilos",
                tooltip: () => "How many silos is each hay bale equivalent to in terms of adding hay capacity",
                getValue: () => Config.HayBaleEquivalentToHowManySilos,
                setValue: value => Config.HayBaleEquivalentToHowManySilos = value
                );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                name: () => "HaybalePrice",
                getValue: () => Config.HaybalePrice,
                setValue: value => Config.HaybalePrice = value
            );
        }

        private void Display_MenuChanged(object sender, MenuChangedEventArgs e)
        {
            //we don't care if it's not a shop menu
            if (!(e.NewMenu is ShopMenu))
                return;

            var shop = (ShopMenu)e.NewMenu;

            //we don't care if it's not Marnie's store
            if (shop.portraitPerson == null || !(shop.portraitPerson.Name == "Marnie"))
                return;

            //create the farm renderer object and add it to robin's stock
            var itemStock = shop.itemPriceAndStock;

            foreach (var item in itemStock)
            {
                if (item.Key.Name == "Ornamental Hay Bale")
                {
                    item.Value[0] = Config.HaybalePrice; //change the price
                    break;
                }
            }

            shop.setItemPriceAndStock(itemStock);
        }

        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            //ignore input if the player isnt free to move aka world not loaded,
            //they're in an event, a menu is up, etc
            if (!Context.CanPlayerMove)
                return;

            if (!GetAllAffectedMaps().Contains(Game1.currentLocation))
                return;

            //action button works for right click on mouse and action button for controllers
            if (!e.Button.IsActionButton() && !e.Button.IsUseToolButton())
                return;
            //check if the clicked tile contains a Farm Renderer
            Vector2 tile = Helper.Input.GetCursorPosition().GrabTile;
            Game1.currentLocation.Objects.TryGetValue(tile, out StardewValley.Object obj);
            if (obj != null && obj.bigCraftable.Value)
            {
                if (obj.Name == "Ornamental Hay Bale")
                {
                    if (Utility.numSilos() == 0)
                    {
                        Game1.showRedMessage(Game1.content.LoadString("Strings\\Buildings:NeedSilo"));
                        return;
                    }

                    if (e.Button.IsActionButton())
                    {

                        Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:PiecesOfHay",
                            Game1.getFarm().piecesOfHay.Value,
                            (Utility.numSilos() * 240)));
                    }
                    else if (e.Button.IsUseToolButton())
                    {
                        //if holding hay, try to add it
                        if (Game1.player.ActiveObject != null && Game1.player.ActiveObject.Name == "Hay")
                        {
                            int stack = Game1.player.ActiveObject.Stack;
                            int tryToAddHay = Game1.getFarm().tryToAddHay(Game1.player.ActiveObject.Stack);
                            Game1.player.ActiveObject.Stack = tryToAddHay;

                            if (Game1.player.ActiveObject.Stack < stack)
                            {
                                Game1.playSound("Ship");
                                DelayedAction.playSoundAfterDelay("grassyStep", 100, (GameLocation)null, -1);
                                Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:AddedHay", (object)(stack - Game1.player.ActiveObject.Stack)));
                            }
                            if (Game1.player.ActiveObject.Stack <= 0)
                                Game1.player.removeItemFromInventory((Item)Game1.player.ActiveObject);
                        }
                    }
                }

            }
        }

        internal static IEnumerable<GameLocation> GetAllAffectedMaps()
        {
            yield return Game1.getFarm();
            foreach (Building building in Game1.getFarm().buildings.Where(building => building.indoors.Value != null))
            {
                    yield return building.indoors.Value;
            }
        }
        internal static int NumHayBales()
        {
            int numHayBales = 0;

            foreach (var loc in GetAllAffectedMaps())
            {
                foreach (var temp in loc.Objects)
                {
                    foreach (var obj in temp.Values)
                    {
                        if (obj.Name == "Ornamental Hay Bale")
                        {
                            numHayBales++;
                        }
                    }
                }
            }

            return numHayBales;
        }

        public void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {

            if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftablesInformation"))
            {
                e.Edit(asset =>
                {
                    IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;
                    string[] fields = data[45].Split('/');

                    fields[4] = Helper.Translation.Get("Description",
                        new { capacity = 240 * Config.HayBaleEquivalentToHowManySilos }); //description
                    fields[8] = Helper.Translation.Get("DisplayName"); //display name

                    data[45] = string.Join("/", fields);
                });
            }
        }
        }
    }

    public class ModConfig
    {
        public bool RequiresConstructedSilo { get; set; } = true;
        public int HayBaleEquivalentToHowManySilos { get; set; } = 1;
        public int HaybalePrice { get; set; } = 5000;
    }


    public class PatchNumSilos
    {
        internal static void Postfix(ref int __result)
        {

            if (__result > 0 || !ModEntry.Config.RequiresConstructedSilo)
            {
                __result = __result + (ModEntry.NumHayBales()*ModEntry.Config.HayBaleEquivalentToHowManySilos);
            }
        }
    }
}
