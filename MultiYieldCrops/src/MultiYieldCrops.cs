using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using MultiYieldCrops;

namespace MultiYieldCrop
{
    class MultiYieldCrops : Mod
    {
        public static MultiYieldCrops instance;
        private Dictionary<string, IDictionary<int, string>> ObjectInfoSource { get; set; }

        private Dictionary<string, List<Rule>> allHarvestRules;

        public override void Entry(IModHelper helper)
        {
            instance = this;

            //harmony stuff
            HarvestPatches.Initialize(this.Monitor);
            Harmony harmony = new(this.ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Crop), nameof(Crop.harvest)),
                prefix: new HarmonyMethod(typeof(HarvestPatches), nameof(HarvestPatches.CropHarvest_prefix)),
                postfix: new HarmonyMethod(typeof(HarvestPatches), nameof(HarvestPatches.CropHarvest_postfix))
                );

            /* patch for handling tea leaves
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.TerrainFeatures.Bush), nameof(StardewValley.TerrainFeatures.Bush.performUseAction)),
                postfix: new HarmonyMethod(typeof(HarvestPatches), nameof(HarvestPatches.BushPerformUseAction_postfix))
                );
                */

            helper.Events.GameLoop.SaveLoaded += this.UpdateObjectInfoSource;

            this.InitializeHarvestRules();
        }

        public void SpawnHarvest(Vector2 tileLocation, string cropName, int fertilizer, JunimoHarvester junimo = null)
        {

            if (!this.allHarvestRules.ContainsKey(cropName))
                return;

            Vector2 location = new((tileLocation.X * 64 + 32), (tileLocation.Y * 64 + 32));

            foreach (Rule data in this.allHarvestRules[cropName])
            {
                foreach (Item item in this.SpawnItems(data,fertilizer))
                {
                    if (item == null)
                        continue;
                    if (junimo == null)
                    {
                        Game1.createItemDebris(item, location, -1);
                    }
                    else
                    {
                        junimo.tryToAddItemToHut(item);
                    }
                }
            }

        }

        private IEnumerable<Item> SpawnItems(Rule data, int fertilizer)
        {
            int quality = fertilizer;
            int itemID = this.GetIndexByName(data.ItemName, data.ExtraYieldItemType);
            int xTile = Game1.player.getTileX();
            int yTile = Game1.player.getTileY(); ;

            //stole this code from the game to calculate crop quality
            Random random = new Random(xTile * 7 + yTile * 11 + (int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame);
            double highQualityChance = 0.2 * (Game1.player.FarmingLevel / 10.0) + 0.2 * fertilizer * ((Game1.player.FarmingLevel + 2.0) / 12.0) + 0.01;
            double lowerQualityChance = Math.Min(0.75, highQualityChance * 2.0);

            //stole this code from the game to calculate # of crops
            int increaseMaxHarvest = 0;
            if (data.maxHarvestIncreasePerFarmingLevel > 0)
                increaseMaxHarvest = (int)(Game1.player.FarmingLevel * data.maxHarvestIncreasePerFarmingLevel);
            int quantity = random.Next(data.minHarvest, Math.Max(data.minHarvest, data.maxHarvest + increaseMaxHarvest + 1));

            if (quantity < 0)
                quantity = 0;

            if (itemID < 0)
            {
                this.Monitor.Log($"No idea what {data.ExtraYieldItemType} {data.ItemName} is", LogLevel.Warn);
                yield return null;
            }

            for (int i = 0; i < quantity; i++)
            {
                if (random.NextDouble() < highQualityChance)
                    quality = 2;
                else if (random.NextDouble() < lowerQualityChance)
                    quality = 1;
                yield return this.CreateItem(itemID, data.ExtraYieldItemType, quality);
            }

        }

        private Item CreateItem(int itemID,string ItemType,int quality)
        {
            return ItemType switch
            {
                "Object" => new StardewValley.Object(itemID, 1, false, quality: quality),
                "BigCraftable" => new StardewValley.Object(Vector2.Zero, itemID),
                "Clothing" => new Clothing(itemID),
                "Ring" => new Ring(itemID),
                "Hat" => new Hat(itemID),
                "Boot" => new Boots(itemID),
                "Furniture" => new Furniture(itemID, Vector2.Zero),
                "Weapon" => new MeleeWeapon(itemID),
                _ => null,
            };
        }

        public int GetIndexByName(string name,string itemType)
        {
            //there's multiple stone items and 390 is the one that works
            if (itemType == "Object" && name == "Stone")
                return 390;

            foreach (KeyValuePair<int, string> kvp in this.ObjectInfoSource[itemType])
            {
                if (kvp.Value.Split('/')[0] == name)
                {
                    return kvp.Key;
                }
            }
            return -1;
        }
        private void UpdateObjectInfoSource(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            //load up all the object information into a static dictionary
            this.ObjectInfoSource = new Dictionary<string, IDictionary<int, string>>
            {
                { "Object", Game1.objectInformation },
                { "BigCraftable", Game1.bigCraftablesInformation },
                { "Clothing", Game1.clothingInformation },
                { "Ring", Game1.objectInformation },
                {
                    "Hat",
                    this.Helper.GameContent.Load<Dictionary<int, string>>(@"Data/hats")
                },
                {
                    "Boot",
                    this.Helper.GameContent.Load<Dictionary<int, string>>(@"Data/Boots")
                },
                {
                    "Furniture",
                    this.Helper.GameContent.Load<Dictionary<int, string>>(@"Data/Furniture")
                },
                {
                    "Weapon",
                    this.Helper.GameContent.Load<Dictionary<int, string>>(@"Data/weapons")
                }
            };

        }

        private void InitializeHarvestRules()
        {
            this.allHarvestRules = new Dictionary<string, List<Rule>>();
            try
            {
                ContentModel data = this.Helper.ReadConfig<ContentModel>();
                if (data.Harvests != null)
                {
                    this.LoadContentPack(data);
                }

            }
            catch(Exception ex)
            {
                this.Monitor.Log(ex.Message + ex.StackTrace,LogLevel.Error);
            }

            foreach (var pack in this.Helper.ContentPacks.GetOwned())
            {
                if (!pack.HasFile("HarvestRules.json"))
                {
                    this.Monitor.Log($"{pack.Manifest.UniqueID} does not have a HarvestRules.json", LogLevel.Error);
                    continue;
                }
                
                this.LoadContentPack(pack.ReadJsonFile<ContentModel>("HarvestRules.json"));
                
            }
        }
        private void LoadContentPack(ContentModel data)
        {
            if (data == null)
                return;

            foreach (var harvests in data.Harvests)
            {
                this.LoadCropHarvestRulesFor(harvests.CropName,harvests.HarvestRules);
            }
        }

        private void LoadCropHarvestRulesFor(string cropName, List<Rule> harvestRules)
        {
            foreach(Rule rule in harvestRules)
            {
                if (rule.disableWithMods != null)
                {
                    bool skipRule = false;
                    foreach (string mod in rule.disableWithMods)
                    {
                        if (this.Helper.ModRegistry.IsLoaded(mod))
                        {
                            this.Monitor.Log($"A rule was skipped for {cropName} because {mod} was found", LogLevel.Trace);
                            skipRule = true;
                            break;
                        }
                    }

                    if (skipRule)
                        continue;
                }


                if (this.allHarvestRules.ContainsKey(cropName)){
                    this.allHarvestRules[cropName].Add(rule);
                }else
                {
                    this.allHarvestRules[cropName] = new List<Rule>{rule};
                }
                
            }
        }

    } //end class

    class ContentModel
    {
        public List<CropHarvestRules> Harvests { get; set; }
    }

    class CropHarvestRules
    {
        public string CropName;
        public List<Rule> HarvestRules;
    }

    class Rule
    {
        public string ExtraYieldItemType = "Object";
        public string ItemName;
        public int minHarvest = 1;
        public int maxHarvest = 1;
        public float maxHarvestIncreasePerFarmingLevel = 0;
        public string[] disableWithMods = null;
    }
}