using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using AdoptSkin.Framework;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Locations;
using StardewValley.Buildings;

namespace AdoptSkin
{

    /// <summary>The mod entry point.</summary>
    class ModEntry : Mod, IAssetEditor
    {
        /************************
        ** Fields
        *************************/

        private readonly Random Randomizer = new Random();

        internal enum CreatureCategory { Horse, Pet, Animal, Null };
        
        internal static ModConfig Config;






        /************************
       ** Accessors
       *************************/

        // SMAPI Modding helpers
        internal static IModHelper SHelper;
        internal static IMonitor SMonitor;

        // Internal helpers
        internal static CommandHandler Commander;
        internal static CreationHandler Creator;
        internal static SaveLoadHandler SaverLoader;

        // Mod integration
        internal static BFAV226Integrator BFAV226Worker;
        internal static BFAV300Integrator BFAV300Worker;

        // Skin assets
        internal static Dictionary<string, List<AnimalSkin>> AnimalAssets = new Dictionary<string, List<AnimalSkin>>();
        internal static Dictionary<string, List<AnimalSkin>> PetAssets = new Dictionary<string, List<AnimalSkin>>();
        internal static Dictionary<string, List<AnimalSkin>> HorseAssets = new Dictionary<string, List<AnimalSkin>>();

        internal static Dictionary<string, List<AnimalSkin>> SkinAssets = new Dictionary<string, List<AnimalSkin>>();

        // Skin mappings
        //internal static Dictionary<long, int> AnimalSkinMap = new Dictionary<long, int>();
        //internal static Dictionary<long, int> PetSkinMap = new Dictionary<long, int>();
        //internal static Dictionary<long, int> HorseSkinMap = new Dictionary<long, int>();

        internal static Dictionary<long, int> SkinMap = new Dictionary<long, int>();
        // ID >> Category mappings
        internal static Dictionary<long, CreatureCategory> IDToCategory = new Dictionary<long, CreatureCategory>();

        // Short ID mappings for animals. Short IDs are small, user-friendly numbers for referencing specific creatures.
        internal static Dictionary<long, int> AnimalLongToShortIDs = new Dictionary<long, int>();
        internal static Dictionary<int, long> AnimalShortToLongIDs = new Dictionary<int, long>();

        // Pet and Horse string to type mappings
        internal static Dictionary<string, Type> PetTypeMap = new Dictionary<string, Type>();
        internal static Dictionary<string, Type> HorseTypeMap = new Dictionary<string, Type>();

        // Ridden horse holder
        internal static List<Horse> BeingRidden = new List<Horse>();
        // Last known FarmAnimal count
        internal static int AnimalCount = 0;
        // Test to display on tooltip for Pet or Horse, if any
        internal static string HoverText;

        internal static bool AssetsLoaded = false;




        /************************
        ** Public methods
        *************************/

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // SMAPI helpers
            ModEntry.SHelper = helper;
            ModEntry.SMonitor = this.Monitor;

            // Config settings
            Config = this.Helper.ReadConfig<ModConfig>();

            // Internal helpers
            Commander = new CommandHandler(this);
            Creator = new CreationHandler(this);
            SaverLoader = new SaveLoadHandler(this);

            // Event Listeners
            helper.Events.GameLoop.SaveLoaded += SaveLoadHandler.Setup;
            helper.Events.GameLoop.SaveLoaded += SaveLoadHandler.LoadData;
            helper.Events.GameLoop.Saving += SaveLoadHandler.SaveData;
            helper.Events.GameLoop.Saved += SaveLoadHandler.LoadData;
            helper.Events.GameLoop.ReturnedToTitle += SaveLoadHandler.StopUpdateChecks;

            helper.Events.GameLoop.DayStarted += Creator.ProcessNewDay;
            helper.Events.GameLoop.DayEnding += Creator.ProcessEndDay;
            helper.Events.Display.RenderingHud += RenderHoverTooltip;


            // SMAPI Commands
            helper.ConsoleCommands.Add("list_creatures", $"Lists the creature IDs and skin IDs of the given type.\n(Options: '{string.Join("', '", CommandHandler.CreatureGroups)}', or a specific animal type (such as bluechicken))", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("randomize_all_skins", "Randomizes the skins for every farm animal, pet, and horse on the farm.", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("randomize_skin", "Randomizes the skin for the given creature. Call `randomize_skin <animal/pet/horse> <creature ID>`. To find a creature's ID, call `list_creatures`.", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("set_skin", "Sets the skin of the given creature to the given skin ID. Call `set_skin <skin ID> <animal/pet/horse> <creature ID>`. To find a creature's ID, call `list_creatures`.", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("corral_horses", "Warp all horses to the farm's stable, giving you the honor of being a clown car chauffeur.", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("horse_whistle", "Summons one of the player's horses to them. Can be called with a horse's ID to call a specific horse. To find a horse's ID, call `list_creatures horse`.", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("sell", "Used to give away one of your pets or horses. Call `sell <pet/horse> <creature ID>`. To find a creature's ID, call `list_creatures`.", Commander.OnCommandReceived);

            // DEBUG
            if (Config.DebuggingMode)
            {
                // ** TODO: Make debug commands line up with the lack of a need to call categories anymore
                helper.ConsoleCommands.Add("debug_reset", "DEBUG: ** WARNING ** Resets all skins and creature IDs, but ensures that all creatures are properly in the Adopt & Skin system.", Commander.OnCommandReceived);
                helper.ConsoleCommands.Add("debug_skinmaps", "DEBUG: Prints all info in current skin maps", Commander.OnCommandReceived);
                helper.ConsoleCommands.Add("debug_idmaps", "DEBUG: Prints AnimalLongToShortIDs", Commander.OnCommandReceived);
                helper.ConsoleCommands.Add("debug_pets", "DEBUG: Print the information for every Pet instance on the map", Commander.OnCommandReceived);
                helper.ConsoleCommands.Add("debug_horses", "DEBUG: Print the information for every Horse instance on the map", Commander.OnCommandReceived);
                helper.ConsoleCommands.Add("debug_find", "DEBUG: Locate the creature with the given ID. Call `debug_find <horse/pet/animal> <creature ID>`.", Commander.OnCommandReceived);
                helper.ConsoleCommands.Add("summon_stray", "DEBUG: Summons a new stray at Marnie's.", Commander.OnCommandReceived);
                helper.ConsoleCommands.Add("summon_horse", "DEBUG: Summons a wild horse. Somewhere.", Commander.OnCommandReceived);
                helper.ConsoleCommands.Add("debug_clearunowned", "DEBUG: Removes any wild horses or strays that exist, to clear out glitched extras", Commander.OnCommandReceived);
            }

        }


        /// <summary>Get whether this instance can edit the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            if (asset.AssetNameEquals("Data/mail"))
                return true;
            return false;
        }


        /// <summary>Edit a matched asset.</summary>
        /// <param name="asset">A helper which encapsulates metadata about an asset and enables changes to it.</param>
        public void Edit<T>(IAssetData asset)
        {
            // Add the letter Marnie sends regarding the stray animals
            if (asset.AssetNameEquals("Data/mail"))
            {
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                data.Add("MarnieStrays", "Dear @,   ^   Since I came over with the stray that I found, I've been running across a lot more of them! Poor things, I think they're escapees from nearby Weatherwoods, that town that burned down a few weeks back.   ^   Anyway, I'm adopting out the ones that I happen across! Just stop by during my normal hours if you'd like to bring home a friend for your newest companion.   ^   -Marnie");
            }
        }


        /// <summary>Standardize internal types and file names to have no spaces and to be entirely lowercase. </summary>
        public static string Sanitize(string input)
        {
            input = input.ToLower().Replace(" ", "");
            return string.IsInterned(input) ?? input;
        }


        /// <summary>Returns an enumerable list containing all horses.</summary>
        public static IEnumerable<Horse> GetHorses()
        {
            foreach (NPC npc in Utility.getAllCharacters())
                if (npc is Horse horse)
                    yield return horse;
        }


        /// <summary>Returns an enumerable list containing all pets.</summary>
        public static IEnumerable<Pet> GetPets()
        {
            foreach (NPC npc in Utility.getAllCharacters())
                if (npc is Pet pet)
                    yield return pet;
        }


        /// <summary>Returns an enumerable list copy of the list containing all animals on the farm.</summary>
        public static IEnumerable<FarmAnimal> GetAnimals()
        {
            Farm farm = Game1.getFarm();

            if (farm == null)
                yield break;

            foreach (FarmAnimal animal in farm.getAllFarmAnimals())
                yield return animal;
        }


        /// <summary>Returns true if the given Character is of the given CreatureCategory type (Animal, Pet, or Horse)</summary>
        public static bool IsCreatureType(Character character, CreatureCategory category)
        {
            if (character is FarmAnimal && category == CreatureCategory.Animal)
                return true;
            else if (character is Pet && category == CreatureCategory.Pet)
                return true;
            else if (character is Horse && category == CreatureCategory.Horse)
                return true;
            return false;
        }


        public static bool IsInDatabase(Character character)
        {
            if (character == null)
                return false;
            else if (character is FarmAnimal animal && IDToCategory.ContainsKey(animal.myID.Value) && IDToCategory[animal.myID.Value] == CreatureCategory.Animal)
                return true;
            else if (character is Pet pet && IDToCategory.ContainsKey(pet.Manners) && IDToCategory[pet.Manners] == CreatureCategory.Pet)
                return true;
            else if (character is Horse horse && IDToCategory.ContainsKey(horse.Manners) && IDToCategory[horse.Manners] == CreatureCategory.Horse)
                return true;
            return false;
        }

        
        /// <summary>Returns the CreatureCategory type of the creature associated with the given ID</summary>
        public static CreatureCategory GetCreatureCategory(long id)
        {
            if (IDToCategory.ContainsKey(id))
                return IDToCategory[id];
            // Creature not in system
            return CreatureCategory.Null;
        }


        public static bool IsNotATractor(Horse horse)
        {
            if (!horse.Name.StartsWith("tractor/"))
                return true;
            return false;
        }






        /************************
        ** Protected / Internal
        *************************/

        /// <summary>Calls a horse that the player owns to the player's location</summary>
        internal static void CallHorse()
        {
            // Make sure that the player is calling the horse while outside
            if (!Game1.player.currentLocation.IsOutdoors)
            {
                ModEntry.SMonitor.Log("You cannot call for a horse while indoors.", LogLevel.Alert);
                Game1.chatBox.addInfoMessage("You hear your Grandfather's voice echo in your head.. \"Now is not the time to use that.\"");
                return;
            }

            // Teleport the first horse you find that the player actually owns
            foreach (Horse taxi in GetHorses())
            {
                if (IsInDatabase(taxi) && IsCreatureType(taxi, CreatureCategory.Horse) && !WildHorse.IsWildHorse(taxi))
                {
                    Game1.warpCharacter(taxi, Game1.player.currentLocation, Game1.player.getTileLocation());
                    return;
                }
            }

            // Player doesn't own a horse yet
            ModEntry.SMonitor.Log("You do not own any horse that you can call.", LogLevel.Alert);
            Game1.chatBox.addInfoMessage("Your Grandfather's voice echoes in your head.. \"You aren't yet ready for this gift.\"");
        }


        /// <summary>Calls all horses owned by the player to return to the player's stable</summary>
        internal static void CorralHorses()
        {
            // Find the farm's stable
            Stable horsehut = null;
            foreach (Building building in Game1.getFarm().buildings)
                if (building is Stable)
                    horsehut = building as Stable;

            // No stable was found on the farm
            if (horsehut == null)
            {
                ModEntry.SMonitor.Log("NOTICE: You don't have a stable to warp to!", LogLevel.Error);
                return;
            }

            // WARP THEM. WARP THEM ALL.
            int stableX = int.Parse(horsehut.tileX.ToString()) + 1;
            int stableY = int.Parse(horsehut.tileY.ToString()) + 1;
            Vector2 stableWarp = new Vector2(stableX, stableY);
            foreach (Horse horse in ModEntry.GetHorses())
            {
                if (IsInDatabase(horse) && IsCreatureType(horse, CreatureCategory.Horse) && !WildHorse.IsWildHorse(horse))
                    Game1.warpCharacter(horse, "farm", stableWarp);
            }

            ModEntry.SMonitor.Log("All horses have been warped to the stable.", LogLevel.Alert);
        }


        /// <summary>Returns the creature of the given Long IDs.</summary>
        internal static Character GetCreature(long id)
        {
            
            if (!IDToCategory.ContainsKey(id) && !AnimalShortToLongIDs.ContainsKey((int)id))
                return null;

            CreatureCategory creatureCategory = CreatureCategory.Null;
            if (IDToCategory.ContainsKey(id))
                creatureCategory = IDToCategory[id];
            else
                creatureCategory = CreatureCategory.Animal;

            switch (creatureCategory)
            {
                case CreatureCategory.Horse:
                    foreach (Horse horse in GetHorses())
                        if (id == horse.Manners)
                            return horse;
                    break;

                case CreatureCategory.Pet:
                    foreach (Pet pet in GetPets())
                        if (id == pet.Manners)
                            return pet;
                    break;

                case CreatureCategory.Animal:
                    ModEntry.SMonitor.Log("GetCreature: Enter animal", LogLevel.Info);
                    foreach (FarmAnimal animal in GetAnimals())
                    {
                        ModEntry.SMonitor.Log($"Animal: {animal.myID.Value}, In Long>Short? {AnimalLongToShortIDs.ContainsKey(animal.myID.Value)}", LogLevel.Info);
                        if (id == animal.myID.Value || (AnimalLongToShortIDs.ContainsKey(animal.myID.Value) && AnimalLongToShortIDs[animal.myID.Value] == id))
                            return animal;
                    }

                    break;

                default:
                    return null;
            }

            return null;
        }


        /// <summary>Returns the animal skin associated with the skin ID. Assumes the ID is in range.</summary>
        internal static AnimalSkin GetSkinFromID(string type, int ID)
        {
            // Creature does not have a skin set for it
            if (ID == 0)
                return null;

            type = Sanitize(type);

            if (PetAssets.ContainsKey(type))
                return PetAssets[type][ID - 1];
            else if (HorseAssets.ContainsKey(type))
                return HorseAssets[type][ID - 1];
            else if (AnimalAssets.ContainsKey(type))
                return AnimalAssets[type][ID - 1];

            // Creature type not handled by Adopt & Skin. No skin to return.
            return null;
        }


        /// <summary>
        /// Given the ID for an animal, pet, or horse, and that creature's type, return the AnimalSkin mapped to that creature.
        /// Return null if the creature type isn't handled.
        /// </summary>
        internal AnimalSkin GetSkin(Character creature)
        {
            switch (creature)
            {
                case Horse horse:
                    // No horse skins are loaded
                    if (HorseAssets[Sanitize(horse.GetType().Name)].Count == 0)
                        return null;

                    // A wild horse is being checked
                    if (WildHorse.IsWildHorse(horse))
                        return GetSkinFromID(horse.GetType().Name, Creator.HorseInfo.SkinID);
                    // Horse is not in system
                    else if (!IsInDatabase(horse))
                    {
                        this.Monitor.Log($"Horse not in system: {horse.Name}", LogLevel.Error);
                        return null;
                    }

                    // Ensure skin ID given is a valid number for the given horse type
                    int horseSkinID = SkinMap[horse.Manners];
                    if (horseSkinID < 1 || horseSkinID > HorseAssets[Sanitize(horse.GetType().Name)].Count)
                    {
                        this.Monitor.Log($"{horse.Name}'s skin ID no longer exists in `/assets/skins`. Skin will be randomized.", LogLevel.Alert);
                        horseSkinID = SetRandomSkin(horse);
                    }

                    // Horse has a skin. Return it.
                    return GetSkinFromID(horse.GetType().Name, horseSkinID);

                case Pet pet:
                    string petType = Sanitize(pet.GetType().Name);
                    // Break out of unhandled types
                    if (!ModApi.GetHandledPetTypes().Contains(petType))
                        break;

                    // No pet skins are loaded for this pet type
                    else if (PetAssets[Sanitize(pet.GetType().Name)].Count == 0)
                        return null;

                    // A stray pet is being checked
                    if (Stray.IsStray(pet))
                        return GetSkinFromID(pet.GetType().Name, Creator.StrayInfo.SkinID);
                    // Pet is not in system
                    else if (!IsInDatabase(pet))
                    {
                        this.Monitor.Log($"Pet not in system: {pet.Name}", LogLevel.Error);
                        return null;
                    }

                    // Ensure skin ID given is a current valid number for the given pet type
                    int petSkinID = SkinMap[pet.Manners];
                    if (petSkinID < 1 || petSkinID > PetAssets[petType].Count)
                    {
                        this.Monitor.Log($"{pet.Name}'s skin ID no longer exists in `/assets/skins`. Skin will be randomized.", LogLevel.Alert);
                        petSkinID = SetRandomSkin(pet);
                    }
                    return GetSkinFromID(petType, petSkinID);

                case FarmAnimal animal:
                    string animalType = Sanitize(animal.type.Value);
                    // Break out of unhandled types
                    if (!ModApi.GetHandledAnimalTypes().Contains(animalType))
                        break;

                    // No farm animal skins are loaded for this animal type
                    else if (AnimalAssets[Sanitize(animal.type.Value)].Count == 0)
                        return null;

                    // Set sub-type if applicable
                    if (ModApi.HasBabySprite(animalType) && animal.age.Value < animal.ageWhenMature.Value)
                        animalType = "baby" + animalType;
                    else if (ModApi.HasShearedSprite(animalType) && animal.currentProduce.Value <= 0)
                        animalType = "sheared" + animalType;

                    // Animal is not in system
                    if (!IsInDatabase(animal))
                        return null;

                    // Ensure skin ID given is a current valid number for the given animal type
                    int animalSkinID = SkinMap[animal.myID.Value];
                    if (animalSkinID < 1 || animalSkinID > AnimalAssets[animalType].Count)
                    {
                        this.Monitor.Log($"{animal.Name}'s skin ID is no longer exists in `/assets/skins`. Skin will be randomized.", LogLevel.Alert);
                        animalSkinID = SetRandomSkin(animal);
                    }
                    return GetSkinFromID(animalType, animalSkinID);

                default:
                    break;
            }
            return null;
        }


        /// <summary>Assigns a new random skin to the given creature.</summary>
        /// <param name="creature">The animal, pet, or horse to assign a new random skin.</param>
        internal int SetRandomSkin(Character creature)
        {
            switch (creature)
            {
                case Horse horse:
                    return SetSkin(horse, Randomizer.Next(1, HorseAssets[Sanitize(horse.GetType().Name)].Count + 1));

                case Pet pet:
                    return SetSkin(pet, Randomizer.Next(1, PetAssets[Sanitize(pet.GetType().Name)].Count + 1));

                case FarmAnimal animal:
                    return SetSkin(animal, Randomizer.Next(1, AnimalAssets[Sanitize(animal.type.Value)].Count + 1));

                default:
                    return 0;
            }
        }


        /// <summary>Sets the skin of the given creature with the given skin ID</summary>
        internal int SetSkin(Character creature, int skinID)
        {
            switch (creature)
            {
                case Horse horse:
                    if (HorseAssets[Sanitize(horse.GetType().Name)].Count == 0 || !IsInDatabase(horse))
                        return 0;
                    SkinMap[horse.Manners] = skinID;
                    break;

                case Pet pet:
                    if (PetAssets[Sanitize(pet.GetType().Name)].Count == 0 || !IsInDatabase(pet))
                        return 0;
                    SkinMap[pet.Manners] = skinID;
                    break;

                case FarmAnimal animal:
                    if (AnimalAssets[Sanitize(animal.type.Value)].Count == 0 || !IsInDatabase(animal))
                    {
                        Monitor.Log($"Animal not in database or has no skins", LogLevel.Error);
                        return 0;
                    }
                    SkinMap[animal.myID.Value] = skinID;
                    break;

                default:
                    break;
            }

            UpdateSkin(creature);
            return skinID;
        }


        /// <summary>Returns an unused Short ID for a new creature to use.</summary>
        private int GetUnusedShortID()
        {
            int newShortID = 1;

            // Gather all current ShortIDs
            List<int> usedIDs = new List<int>();
            foreach (Horse horse in GetHorses())
                usedIDs.Add(horse.Manners);
            foreach (Pet pet in GetPets())
                usedIDs.Add(pet.Manners);
            foreach (int shortID in AnimalShortToLongIDs.Keys)
                usedIDs.Add(shortID);

            // Find an unused ShortID and return it
            while (usedIDs.Contains(newShortID))
                newShortID++;
            return newShortID;
        }






        /************************
        ** Save/Load/Update logic
        *************************/


        internal void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // Check if a horse being ridden has been dismounted. If so, re-add it to the map.
            HorseRidingCheck();

            // Check that animal list is up to date. If not, add/remove animal in system.
            AnimalListCheck();


            // -- Display name tooltips when hovering over a pet or horse
            bool isHovering = false;
            Vector2 mousePos = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;

            // Show pet tooltip
            foreach (Pet pet in GetPets())
                if (IsWithinSpriteBox(mousePos, pet))
                {
                    isHovering = true;
                    HoverText = pet.displayName;
                }
            // Show horse tooltip
            foreach (Horse horse in GetHorses())
                if (IsWithinSpriteBox(mousePos, horse))
                {
                    isHovering = true;
                    HoverText = horse.displayName;
                }

            // Clear hover text when not hovering over a pet or horse
            if (!isHovering)
            {
                HoverText = null;
            }
        }


        /// <summary>Handler to remember the current horses being ridden, such that they can be manually re-added, preventing the disappearence of dismounted multihorses.</summary>
        internal static void SaveTheHorse(object sender, NpcListChangedEventArgs e)
        {
            foreach (NPC npc in e.Removed)
                if (npc is Horse horse && horse.rider != null && horse.Manners != 0)
                {
                    BeingRidden.Add(horse);
                    if (Config.OneTileHorse)
                        horse.squeezeForGate();
                }
        }


        /// <summary>Checks horses known to be being ridden and re-adds them to the map if they've been dismounted.</summary>
        internal static void HorseRidingCheck()
        {
            List<Horse> dismounted = new List<Horse>();
            foreach (Horse horse in BeingRidden)
            {
                if (horse.rider == null)
                {
                    GameLocation loc = horse.currentLocation;
                    Game1.removeThisCharacterFromAllLocations(horse);
                    loc.addCharacter(horse);
                    dismounted.Add(horse);
                }
            }
            // Remove any dismounted horses from the list of horses currently being ridden
            if (dismounted.Count > 0)
                foreach (Horse horse in dismounted)
                    BeingRidden.Remove(horse);
        }


        internal void AnimalListCheck()
        {
            if (Game1.getFarm() != null && AnimalCount != Game1.getFarm().getAllFarmAnimals().Count)
            {
                List<long> existingAnimals = new List<long>();
                List<FarmAnimal> newAnimals = new List<FarmAnimal>();

                // Check for new animals and populate lists containing existing and new animals
                foreach (FarmAnimal animal in GetAnimals())
                {
                    if (!IsInDatabase(animal))
                        newAnimals.Add(animal);
                    else
                        existingAnimals.Add(animal.myID.Value);
                }

                // Check for removed animals
                List<long> animalsToRemove = new List<long>();
                foreach (long id in SkinMap.Keys)
                    if (IDToCategory[id] == CreatureCategory.Animal && !existingAnimals.Contains(id))
                        animalsToRemove.Add(id);
                // Remove animals no longer on farm
                foreach (long id in animalsToRemove)
                {
                    Monitor.Log($"Removing animal, id: {id}", LogLevel.Warn);
                    RemoveCreature(id);
                }

                // Add new animals
                foreach (FarmAnimal animal in newAnimals)
                {
                    Monitor.Log($"Adding new animal: {animal.type.Value}, {animal.Name}", LogLevel.Warn);
                    AddCreature(animal);
                }

                // Update last known animal count
                AnimalCount = Game1.getFarm().getAllFarmAnimals().Count;
            }
        }


        /// <summary>Checks for the arrival of the player's first pet and adds it to the system.</summary>
        internal void CheckForFirstPet(object sender, NpcListChangedEventArgs e)
        {
            // First check after load from save, first pet known to exist
            if (Creator.FirstPetReceived)
            {
                this.Helper.Events.World.NpcListChanged -= this.CheckForFirstPet;
                Creator.PlaceBetBed();
                return;
            }

            if (e != null)
            {
                // Check for the arrival of the vanilla first pet
                foreach (NPC npc in e.Added)
                    if (npc is Pet pet)
                    {
                        AddCreature(pet);
                        this.Helper.Events.World.NpcListChanged -= this.CheckForFirstPet;
                        this.Helper.Events.GameLoop.DayStarted += PlaceBedTomorrow;
                        Game1.addMailForTomorrow("MarnieStrays");
                        return;
                    }
            }

            // A pet already exists in the system, wasn't in the save loaded variables due to older A&S version
            foreach (CreatureCategory category in IDToCategory.Values)
                if (category == CreatureCategory.Pet)
                {
                    Creator.FirstPetReceived = true;
                    this.Helper.Events.World.NpcListChanged -= this.CheckForFirstPet;
                    Creator.PlaceBetBed();
                    return;
                }
        }


        /// <summary>Checks for the arrival of the player's first horse and adds it to the system.</summary>
        internal void CheckForFirstHorse(object sender, NpcListChangedEventArgs e)
        {
            // First check after load from save, first horse known to exist
            if (Creator.FirstHorseReceived)
            {
                this.Helper.Events.World.NpcListChanged -= this.CheckForFirstHorse;
                return;
            }

            if (e.Added != null)
            {
                // Check for the arrival of the vanilla horse
                foreach (NPC npc in e.Added)
                    if (npc is Horse horse)
                    {
                        // A tractor is not your first horse
                        if (horse.Name.StartsWith("tractor/"))
                            break;

                        AddCreature(horse);
                        Creator.FirstHorseReceived = true;
                        this.Helper.Events.World.NpcListChanged -= this.CheckForFirstHorse;
                        return;
                    }
            }

            // A horse already exists in the system, wasn't in the save loaded variables due to older A&S version
            foreach (CreatureCategory category in IDToCategory.Values)
                if (category == CreatureCategory.Horse)
                {
                    Creator.FirstHorseReceived = true;
                    this.Helper.Events.World.NpcListChanged -= this.CheckForFirstHorse;
                    return;
                }
        }


        /// <summary>Helper to place the pet bed in Marnie's on the day after the first pet is received</summary>
        internal static void PlaceBedTomorrow(object sender, DayStartedEventArgs e)
        {
            Creator.PlaceBetBed();
            // Remove self from day update after bed has been placed
            SHelper.Events.GameLoop.DayStarted -= PlaceBedTomorrow;
        }


        /// <summary>Check for the Horse Whistle or Corral hotkey to be pressed, and execute the function if necessary</summary>
        internal static void HotKeyCheck(object sender, ButtonReleasedEventArgs e)
        {
            // Only check for hotkeys if the player is not in a menu
            if (!Context.IsPlayerFree)
                return;

            if (e.Button.ToString().ToLower() == Config.HorseWhistleKey.ToLower())
            {
                CallHorse();
            }
            if (e.Button.ToString().ToLower() == Config.CorralKey.ToLower())
            {
                CorralHorses();
            }
        }


        /// <summary>Refreshes the given animal, pet, or horse's skin texture with the one Adopt & Skin has saved for it.</summary>
        internal void UpdateSkin(Character creature)
        {
            switch (creature)
            {
                case Horse horse:
                    if (WildHorse.IsWildHorse(horse))
                    {
                        horse.Sprite = new AnimatedSprite(GetSkinFromID(horse.GetType().Name, Creator.HorseInfo.SkinID).AssetKey, 0, 32, 32);
                        break;
                    }
                    AnimalSkin horseSkin = GetSkin(horse);
                    if (horseSkin != null && horse.Sprite.textureName.Value != horseSkin.AssetKey)
                        horse.Sprite = new AnimatedSprite(horseSkin.AssetKey, 7, 32, 32);
                    break;

                case Pet pet:
                    if (Stray.IsStray(pet))
                    {
                        pet.Sprite = new AnimatedSprite(GetSkinFromID(pet.GetType().Name, Creator.StrayInfo.SkinID).AssetKey, 28, 32, 32);
                        break;
                    }
                    AnimalSkin petSkin = GetSkin(pet);
                    if (petSkin != null && pet.Sprite.textureName.Value != petSkin.AssetKey)
                        pet.Sprite = new AnimatedSprite(petSkin.AssetKey, 28, 32, 32);
                    break;

                case FarmAnimal animal:
                    AnimalSkin animalSkin = GetSkin(animal);
                    if (animalSkin != null && animal.Sprite.textureName.Value != animalSkin.AssetKey)
                        animal.Sprite = new AnimatedSprite(animalSkin.AssetKey, 0, animal.frontBackSourceRect.Width, animal.frontBackSourceRect.Height);
                    break;

                default:
                    break;
            }
        }


        /// <summary>Adds a creature into the Adopt & Skin system.</summary>
        /// <param name="creature">The StardewValley.Character type creature (animal, pet, or horse) to add to the system</param>
        /// <param name="skin">Optional parameter. Given when a creature is being created with a predetermined skin.</param>
        internal void AddCreature(Character creature, int skin = 0)
        {
            // Creature is already in the system or is invalid
            if (IsInDatabase(creature) || creature == null)
                return;

            CreateSystemIDs(creature);

            // Give a skin
            if (skin == 0)
                SkinMap[GetLongID(creature)] = SetRandomSkin(creature);
            else
                SetSkin(creature, skin);

            if (creature is FarmAnimal animal)
            {
                Monitor.Log($"End of AddCreature. {animal.type.Value} {animal.Name}:: Short {AnimalLongToShortIDs[animal.myID.Value]}   Skin {GetSkin(animal).ID}", LogLevel.Alert);
            }
        }


        /// <summary>Removes a creature from the Adopt & Skin system.</summary>
        internal void RemoveCreature(long id)
        {
            Character creature = GetCreature(id);
            if (!IsInDatabase(creature))
                return;

            // Remove ShortID from animal ID lists
            if (IsCreatureType(creature, CreatureCategory.Animal))
            {
                // Remove from ShortID lists
                int shortID = AnimalLongToShortIDs[id];
                AnimalLongToShortIDs.Remove(id);
                AnimalShortToLongIDs.Remove(shortID);
            }

            // Remove from general skin map
            SkinMap.Remove(id);

            // Remove from ID >> Category list
            IDToCategory.Remove(id);
        }


        /// <summary>Creates and stores the long and ShortIDs for the given creature being added into the system</summary>
        private void CreateSystemIDs(Character creature)
        {
            int newShortID = GetUnusedShortID();

            switch (creature)
            {
                case Horse horse:
                    horse.Manners = newShortID;
                    IDToCategory[horse.Manners] = CreatureCategory.Horse;
                    break;

                case Pet pet:
                    pet.Manners = newShortID;
                    IDToCategory[pet.Manners] = CreatureCategory.Pet;
                    break;

                case FarmAnimal animal:
                    AnimalLongToShortIDs[animal.myID.Value] = newShortID;
                    AnimalShortToLongIDs[newShortID] = animal.myID.Value;
                    IDToCategory[animal.myID.Value] = CreatureCategory.Animal;
                    break;

                default:
                    break;
            }
        }


        internal static long GetLongID(Character creature)
        {
            if (creature is Horse horse)
                return horse.Manners;
            else if (creature is Pet pet)
                return pet.Manners;
            else if (creature is FarmAnimal animal)
                return animal.myID.Value;
            return 0;
        }


        internal static int GetShortID(Character creature)
        {
            if (creature is Horse || creature is Pet)
                return (int)GetLongID(creature);
            else if (creature is FarmAnimal animal)
                return AnimalLongToShortIDs[animal.myID.Value];
            return 0;
        }






        /*****************
        ** Name Tooltip
        ******************/

        /// <summary>Renders the name hover tooltip if a pet or horse is being hovered over</summary>
        private void RenderHoverTooltip(object sender, RenderingHudEventArgs e)
        {
            if (Context.IsPlayerFree && HoverText != null)
                this.DrawSimpleTooltip(Game1.spriteBatch, HoverText, Game1.smallFont);
        }


        /// <summary>Returns true if the given mouse cursor location is over the given pet or horse's location</summary>
        private bool IsWithinSpriteBox(Vector2 mousePos, Character creature)
        {
            // ** MAY NEED TO CHANGE FOR MULTIPLAYER **
            if (Game1.player.currentLocation == creature.currentLocation &&
                (int)mousePos.X >= creature.getLeftMostTileX().X && (int)mousePos.X <= creature.getRightMostTileX().X &&
                    (int)mousePos.Y <= creature.getTileY() && (int)mousePos.Y >= creature.getTileY() - 1)
                return true;

            return false;
        }


        /// <summary>Draw tooltip at the cursor position with the given message.</summary>
        /// <param name="b">The sprite batch to update.</param>
        /// <param name="hoverText">The tooltip text to display.</param>
        /// <param name="font">The tooltip font.</param>
        private void DrawSimpleTooltip(SpriteBatch b, string hoverText, SpriteFont font)
        {
            Vector2 textSize = font.MeasureString(hoverText);
            int width = (int)textSize.X + Game1.tileSize / 2;
            int height = Math.Max(60, (int)textSize.Y + Game1.tileSize / 2);
            int x = Game1.getOldMouseX() + Game1.tileSize / 2;
            int y = Game1.getOldMouseY() - Game1.tileSize / 2;
            if (x + width > Game1.viewport.Width)
            {
                x = Game1.viewport.Width - width;
                y += Game1.tileSize / 4;
            }
            if (y + height < 0)
            {
                x += Game1.tileSize / 4;
                y = Game1.viewport.Height + height;
            }
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White);
            if (hoverText.Length > 1)
            {
                Vector2 tPosVector = new Vector2(x + (Game1.tileSize / 4), y + (Game1.tileSize / 4 + 4));
                b.DrawString(font, hoverText, tPosVector + new Vector2(2f, 2f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector + new Vector2(0f, 2f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector + new Vector2(2f, 0f), Game1.textShadowColor, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                b.DrawString(font, hoverText, tPosVector, Game1.textColor * 0.9f, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
        }

    }
}
