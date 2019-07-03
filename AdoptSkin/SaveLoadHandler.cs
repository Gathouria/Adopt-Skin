using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;

namespace AdoptSkin.Framework
{
    class SaveLoadHandler
    {
        /// <summary>The file extensions recognised by the mod.</summary>
        private static readonly HashSet<string> ValidExtensions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ".png",
            ".xnb"
        };
        private static readonly Random Randomizer = new Random();

        private static readonly IModHelper SHelper = ModEntry.SHelper;
        private static readonly IMonitor SMonitor = ModEntry.SMonitor;

        private static ModEntry Entry;



        internal SaveLoadHandler(ModEntry entry)
        {
            Entry = entry;
        }






        /**************************
        ** Setup + Load/Save Logic
        ***************************/

        /// <summary>Sets up initial values needed for Adopt & Skin.</summary>
        internal static void Setup(object sender, SaveLoadedEventArgs e)
        {
            ModApi.RegisterDefaultTypes();
            IntegrateMods();

            LoadAssets();

            // Remove the Setup from the loop, so that it isn't done twice when the player returns to the title screen and loads again
            SHelper.Events.GameLoop.SaveLoaded -= Setup;
        }


        /// <summary>Loads handlers for integration of other mods</summary>
        private static void IntegrateMods()
        {
            if (SHelper.ModRegistry.IsLoaded("Paritee.BetterFarmAnimalVariety"))
            {
                ISemanticVersion bfavVersion = SHelper.ModRegistry.Get("Paritee.BetterFarmAnimalVariety").Manifest.Version;

                if (bfavVersion.IsNewerThan("2.2.6"))
                    ModEntry.BFAV300Worker = new BFAV300Integrator();
                else
                    ModEntry.BFAV226Worker = new BFAV226Integrator();
            }
        }


        /// <summary>Starts processes that Adopt & Skin checks at every update tick</summary>
        internal static void StartUpdateChecks()
        {
            SHelper.Events.GameLoop.UpdateTicked += Entry.OnUpdateTicked;
            SHelper.Events.Input.ButtonPressed += ModEntry.Creator.WildHorseInteractionCheck;
            SHelper.Events.Input.ButtonPressed += ModEntry.Creator.StrayInteractionCheck;
            SHelper.Events.Input.ButtonReleased += ModEntry.HotKeyCheck;

            SHelper.Events.World.NpcListChanged += ModEntry.SaveTheHorse;
            SHelper.Events.World.NpcListChanged += Entry.CheckForFirstPet;
            SHelper.Events.World.NpcListChanged += Entry.CheckForFirstHorse;
        }


        /// <summary>Stops Adopt & Skin from updating at each tick</summary>
        internal static void StopUpdateChecks(object s, EventArgs e)
        {
            SHelper.Events.GameLoop.UpdateTicked -= Entry.OnUpdateTicked;
            SHelper.Events.Input.ButtonPressed -= ModEntry.Creator.WildHorseInteractionCheck;
            SHelper.Events.Input.ButtonPressed -= ModEntry.Creator.StrayInteractionCheck;
            SHelper.Events.Input.ButtonReleased -= ModEntry.HotKeyCheck;

            SHelper.Events.World.NpcListChanged -= ModEntry.SaveTheHorse;

            // Ensure variables from other saves aren't carried over on return to title screen
            ModEntry.SkinMap = new Dictionary<long, int>();
            ModEntry.IDToCategory = new Dictionary<long, ModEntry.CreatureCategory>();
            ModEntry.AnimalLongToShortIDs = new Dictionary<long, int>();
            ModEntry.AnimalShortToLongIDs = new Dictionary<int, long>();
            ModEntry.Creator.FirstPetReceived = false;
            ModEntry.Creator.FirstHorseReceived = false;
        }


        internal static void LoadData(object s, EventArgs e)
        {
            // Only allow the host player to load Adopt & Skin data
            if (!Context.IsMainPlayer)
            {
                SMonitor.Log("Multiplayer Farmhand detected. Adopt & Skin has been disabled.", LogLevel.Debug);
                return;
            }

            // Load skin and category maps
            ModEntry.SkinMap = SHelper.Data.ReadSaveData<Dictionary<long, int>>("skin-map") ?? new Dictionary<long, int>();
            ModEntry.IDToCategory = SHelper.Data.ReadSaveData<Dictionary<long, ModEntry.CreatureCategory>>("id-to-category") ?? new Dictionary<long, ModEntry.CreatureCategory>();
            
            // Load Short ID maps
            ModEntry.AnimalLongToShortIDs = SHelper.Data.ReadSaveData<Dictionary<long, int>>("animal-long-to-short-ids") ?? new Dictionary<long, int>();
            ModEntry.AnimalShortToLongIDs = SHelper.Data.ReadSaveData<Dictionary<int, long>>("animal-short-to-long-ids") ?? new Dictionary<int, long>();

            // Set up maps if save data is from an older A&S
            if (ModEntry.SkinMap.Count == 0)
                LoadSkinsOldVersion();

            // Load received first pet/horse status
            ModEntry.Creator.FirstHorseReceived = bool.Parse(SHelper.Data.ReadSaveData<string>("first-horse-received") ?? "false");
            ModEntry.Creator.FirstPetReceived = bool.Parse(SHelper.Data.ReadSaveData<string>("first-pet-received") ?? "false");
            Entry.CheckForFirstPet(null, null);
            Entry.CheckForFirstHorse(null, null);

            // Refresh skins via skinmap
            LoadCreatureSkins();

            ModEntry.SMonitor.Log($"{string.Join("\n", ModEntry.IDToCategory)}", LogLevel.Info);

            // Make sure Marnie's cows put some clothes on
            foreach (GameLocation loc in Game1.locations)
            {
                if (loc is Forest forest)
                    foreach (FarmAnimal animal in forest.marniesLivestock)
                    {
                        if (ModEntry.AnimalAssets.Keys.Count != 0 && ModEntry.AnimalAssets[ModEntry.Sanitize(animal.type.Value)].Count != 0)
                            animal.Sprite = new AnimatedSprite(ModEntry.GetSkinFromID(ModEntry.Sanitize(animal.type.Value),
                                Randomizer.Next(1, ModEntry.AnimalAssets[ModEntry.Sanitize(animal.type.Value)].Count)).AssetKey, 0, 32, 32);
                    }
            }

            // Set configuration for walk-through pets
            foreach (Pet pet in ModEntry.GetPets())
                if (!Stray.IsStray(pet))
                {
                    if (ModEntry.Config.WalkThroughPets)
                        pet.farmerPassesThrough = true;
                    else
                        pet.farmerPassesThrough = false;
                }

            // Set last known animal count
            ModEntry.AnimalCount = Game1.getFarm().getAllFarmAnimals().Count;

            // Add Adopt & Skin to the update loop
            StartUpdateChecks();
        }


        /// <summary>Refreshes creature information based on how much information the save file contains</summary>
        internal static void LoadCreatureSkins()
        {
            foreach (FarmAnimal animal in ModEntry.GetAnimals())
                if (ModEntry.IsInDatabase(animal))
                    Entry.UpdateSkin(animal);

            foreach (Pet pet in ModEntry.GetPets())
                // Remove extra Strays left on the map
                if (Stray.IsStray(pet))
                    Game1.removeThisCharacterFromAllLocations(pet);
                else if (ModEntry.IsInDatabase(pet))
                    Entry.UpdateSkin(pet);

            foreach (Horse horse in ModEntry.GetHorses())
                // Remove extra WildHorses left on the map
                if (WildHorse.IsWildHorse(horse))
                        Game1.removeThisCharacterFromAllLocations(horse);
                else if (ModEntry.IsInDatabase(horse))
                    Entry.UpdateSkin(horse);
        }


        internal static void LoadSkinsOldVersion()
        {
            // Load pet information stored from older version formats
            Dictionary<long, int> petSkinMap = SHelper.Data.ReadSaveData<Dictionary<long, int>>("pet-skin-map") ?? new Dictionary<long, int>();
            foreach (Pet pet in ModEntry.GetPets())
            {
                if (!Stray.IsStray(pet) && petSkinMap.ContainsKey(pet.Manners))
                    Entry.AddCreature(pet, petSkinMap[pet.Manners]);
                else if (!Stray.IsStray(pet))
                    Entry.AddCreature(pet);
            }


            // Load horse information stored from older version formats
            Dictionary<long, int> horseSkinMap = SHelper.Data.ReadSaveData<Dictionary<long, int>>("horse-skin-map") ?? new Dictionary<long, int>();
            foreach (Horse horse in ModEntry.GetHorses())
            {
                if (!WildHorse.IsWildHorse(horse) && ModEntry.IsNotATractor(horse) && horseSkinMap.ContainsKey(horse.Manners))
                    Entry.AddCreature(horse, horseSkinMap[horse.Manners]);
                else if (!WildHorse.IsWildHorse(horse) && ModEntry.IsNotATractor(horse))
                    Entry.AddCreature(horse);
            }


            // Load animal information stored from older version formats
            Dictionary<long, int> animalSkinMap = SHelper.Data.ReadSaveData<Dictionary<long, int>>("animal-skin-map") ?? new Dictionary<long, int>();
            ModEntry.SMonitor.Log($"animalSkinMap: {string.Join("\n", animalSkinMap)}", LogLevel.Warn);
            foreach (FarmAnimal animal in ModEntry.GetAnimals())
            {
                if (animalSkinMap.ContainsKey(animal.myID.Value))
                    Entry.AddCreature(animal, animalSkinMap[animal.myID.Value]);
                else
                    Entry.AddCreature(animal);
            }
        }


        internal static void SaveData(object s, EventArgs e)
        {
            // Only allow the host player to save Adopt & Skin data
            if (!Context.IsMainPlayer)
                return;

            // Remove Adopt & Skin from update loop
            StopUpdateChecks(null, null);

            // Save skin and category maps
            SHelper.Data.WriteSaveData("skin-map", ModEntry.SkinMap);
            SHelper.Data.WriteSaveData("id-to-category", ModEntry.IDToCategory);

            // Save Short ID maps
            SHelper.Data.WriteSaveData("animal-long-to-short-ids", ModEntry.AnimalLongToShortIDs);
            SHelper.Data.WriteSaveData("animal-short-to-long-ids", ModEntry.AnimalShortToLongIDs);

            // Save Stray and WildHorse spawn potential
            SHelper.Data.WriteSaveData("first-pet-received", ModEntry.Creator.FirstPetReceived.ToString());
            SHelper.Data.WriteSaveData("first-horse-received", ModEntry.Creator.FirstHorseReceived.ToString());

            // Save data version. May be used for reverse-compatibility for files.
            SHelper.Data.WriteSaveData("data-version", "4");
        }


        /// <summary>Load skin assets from the /assets/skins directory into the A&S database</summary>
        internal static void LoadAssets()
        {
            // Gather handled types
            string validTypes = string.Join(", ", ModApi.GetHandledAllTypes());

            foreach (FileInfo file in new DirectoryInfo(Path.Combine(SHelper.DirectoryPath, "assets", "skins")).EnumerateFiles())
            {
                // Check extension of file is handled by Adopt & Skin
                string extension = Path.GetExtension(file.Name);
                if (!ValidExtensions.Contains(extension))
                {
                    ModEntry.SMonitor.Log($"Ignored skin `assets/skins/{file.Name}` with invalid extension (extension must be one of type {string.Join(", ", ValidExtensions)})", LogLevel.Warn);
                    continue;
                }

                // Parse file name
                string[] nameParts = Path.GetFileNameWithoutExtension(file.Name).Split(new[] { '_' }, 2);
                string type = ModEntry.Sanitize(nameParts[0]);
                // Ensure creature type is handled by Adopt & Skin
                if (!ModEntry.PetAssets.ContainsKey(type) && !ModEntry.HorseAssets.ContainsKey(type) && !ModEntry.AnimalAssets.ContainsKey(type))
                {
                    ModEntry.SMonitor.Log($"Ignored skin `assets/skins/{file.Name}` with invalid naming convention (can't parse {nameParts[0]} as an animal, pet, or horse. Expected one of type: {validTypes})", LogLevel.Warn);
                    continue;
                }
                // Ensure both a type and skin ID can be found in the file name
                if (nameParts.Length != 2)
                {
                    ModEntry.SMonitor.Log($"Ignored skin `assets/skins/{file.Name} with invalid naming convention (no skin ID found)", LogLevel.Warn);
                    continue;
                }
                // Ensure the skin ID is a number
                int skinID = 0;
                if (nameParts.Length == 2 && !int.TryParse(nameParts[1], out skinID))
                {
                    ModEntry.SMonitor.Log($"Ignored skin `assets/skins/{file.Name}` with invalid skin ID (can't parse {nameParts[1]} as a number)", LogLevel.Warn);
                    continue;
                }
                // Ensure the skin ID is not 0 or negative
                if (skinID <= 0)
                {
                    ModEntry.SMonitor.Log($"Ignored skin `assets/skins/{file.Name}` with skin ID of less than or equal to 0. Skins must have an ID of at least 1.");
                    continue;
                }

                // File naming is valid, add asset into system
                string assetKey = SHelper.Content.GetActualAssetKey(Path.Combine("assets", "skins", extension.Equals("xnb") ? Path.Combine(Path.GetDirectoryName(file.Name), Path.GetFileNameWithoutExtension(file.Name)) : file.Name));
                if (ModEntry.AnimalAssets.ContainsKey(type))
                    ModEntry.AnimalAssets[type].Add(new AnimalSkin(type, skinID, assetKey));
                else if (ModEntry.HorseAssets.ContainsKey(type))
                    ModEntry.HorseAssets[type].Add(new AnimalSkin(type, skinID, assetKey));
                else
                    ModEntry.PetAssets[type].Add(new AnimalSkin(type, skinID, assetKey));
            }


            // Sort each list by ID
            AnimalSkin.Comparer comp = new AnimalSkin.Comparer();
            foreach (string type in ModEntry.AnimalAssets.Keys)
                ModEntry.AnimalAssets[type].Sort((p1, p2) => comp.Compare(p1, p2));
            foreach (string type in ModEntry.PetAssets.Keys)
                ModEntry.PetAssets[type].Sort((p1, p2) => comp.Compare(p1, p2));
            foreach (string type in ModEntry.HorseAssets.Keys)
                ModEntry.HorseAssets[type].Sort((p1, p2) => comp.Compare(p1, p2));


            // Print loaded assets to console
            StringBuilder summary = new StringBuilder();
            summary.AppendLine(
                "Statistics:\n"
                + "\n  Registered types: " + validTypes
                + "\n  Animal Skins:"
            );
            foreach (KeyValuePair<string, List<AnimalSkin>> pair in ModEntry.AnimalAssets)
            {
                if (pair.Value.Count > 0)
                    summary.AppendLine($"    {pair.Key}: {pair.Value.Count} skins ({string.Join(", ", pair.Value.Select(p => Path.GetFileName(p.AssetKey)).OrderBy(p => p))})");
            }
            summary.AppendLine("  Pet Skins:");
            foreach (KeyValuePair<string, List<AnimalSkin>> pair in ModEntry.PetAssets)
            {
                if (pair.Value.Count > 0)
                    summary.AppendLine($"    {pair.Key}: {pair.Value.Count} skins ({string.Join(", ", pair.Value.Select(p => Path.GetFileName(p.AssetKey)).OrderBy(p => p))})");
            }
            summary.AppendLine("  Horse Skins:");
            foreach (KeyValuePair<string, List<AnimalSkin>> pair in ModEntry.HorseAssets)
            {
                if (pair.Value.Count > 0)
                    summary.AppendLine($"    {pair.Key}: {pair.Value.Count} skins ({string.Join(", ", pair.Value.Select(p => Path.GetFileName(p.AssetKey)).OrderBy(p => p))})");
            }


            ModEntry.SMonitor.Log(summary.ToString(), LogLevel.Trace);
            ModEntry.AssetsLoaded = true;
        }
    }
}
