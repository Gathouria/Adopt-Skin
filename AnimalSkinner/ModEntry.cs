using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using AnimalSkinner.Framework;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace AnimalSkinner
{

    /// <summary>The mod entry point.</summary>
    class ModEntry : Mod
    {
        /************************
        ** Fields
        *************************/

        /// <summary>The file extensions recognised by the mod.</summary>
        private readonly HashSet<string> ValidExtensions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ".png",
            ".xnb"
        };

        /// <summary>The RNG used for selecting randomized items</summary>
        private readonly Random Randomizer = new Random();

        /// <summary>Allowable custom creature group denotations for use in commands</summary>
        internal static readonly List<string> CreatureGroups = new List<string>() { "all", "animal", "coop", "barn", "chicken", "cow", "pet", "horse" };

        /// <summary>Recognized major creature categories</summary>
        internal enum CreatureCategory { Horse, Pet, Animal };






        /************************
       ** Accessors
       *************************/

        // SMAPI Modding helpers
        internal static IModHelper SHelper;
        internal static IMonitor SMonitor;

        // Pet and Horse creation handler
        internal CreationHandler Creator;

        // SMAPI console command handler
        internal CommandHandler Commander;

        // Mod integration
        internal static BFAV226Integrator BFAV226Worker;
        internal static BFAV300Integrator BFAV300Worker;

        // Skin assets
        internal static Dictionary<string, List<AnimalSkin>> AnimalAssets = new Dictionary<string, List<AnimalSkin>>();
        internal static Dictionary<string, List<AnimalSkin>> PetAssets = new Dictionary<string, List<AnimalSkin>>();
        internal static Dictionary<string, List<AnimalSkin>> HorseAssets = new Dictionary<string, List<AnimalSkin>>();

        // Skin mappings
        internal static Dictionary<long, int> AnimalSkinMap = new Dictionary<long, int>();
        internal static Dictionary<long, int> PetSkinMap = new Dictionary<long, int>();
        internal static Dictionary<long, int> HorseSkinMap = new Dictionary<long, int>();

        // Pet and Horse string to type mappings
        internal static Dictionary<string, Type> PetTypeMap = new Dictionary<string, Type>();
        internal static Dictionary<string, Type> HorseTypeMap = new Dictionary<string, Type>();

        // Short ID mappings. Short IDs are small, user-friendly numbers for referencing specific creatures.
        internal static Dictionary<long, int> AnimalLongToShortIDs = new Dictionary<long, int>();
        internal static Dictionary<int, long> AnimalShortToLongIDs = new Dictionary<int, long>();

        // Tracks last known animal count. Used to check whether the animal mappings should be updated.
        internal int AnimalCount = 0;

        // Track whether assets have been loaded
        internal bool AssetsReady = false;

        // Horse holder, to make sure multi-horses don't disappear on dismount
        internal List<Horse> BeingRidden = new List<Horse>();

        // Tracks whether information has been loaded from save that needs to be accounted for in the new day check
        internal bool NewDayFromSave = true;

        // Tracks the text to display on hover over a pet or horse, if any
        internal string HoverText;






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

            // Pet and Horse creation handler
            Creator = new CreationHandler(this);

            // SMAPI console command handler
            Commander = new CommandHandler(this);

            // Event Listeners
            helper.Events.GameLoop.SaveLoaded += this.Setup;
            helper.Events.GameLoop.SaveLoaded += this.LoadData;
            helper.Events.GameLoop.Saving += this.SaveData;
            helper.Events.GameLoop.Saved += this.LoadData;

            helper.Events.World.NpcListChanged += this.SaveTheHorse;
            helper.Events.GameLoop.DayStarted += Creator.ProcessNewDay;
            helper.Events.Input.ButtonPressed += Creator.HorseCheck;

            helper.Events.Display.RenderingHud += this.RenderHoverTooltip;
            
            // ** TODO: Implement handling added and removed animals


            // SMAPI Commands (CommandHandler? Internal?)
            helper.ConsoleCommands.Add("list_creatures", $"Lists the creature IDs and skin IDs of the given type.\n(Options: '{string.Join("', '", CreatureGroups)}', or a specific animal type (such as bluechicken))", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("randomize_all_skins", "Randomizes the skins for every farm animal, pet, and horse on the farm.", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("randomize_skin", "Randomizes the skin for the given creature. Call `randomize_skin <animal/pet/horse> <creature ID>`. To find a creature's ID, call list_creatures.", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("set_skin", "Sets the skin of the given creature to the given skin ID. Call `set_skin <skin ID> <animal/pet/horse> <creature ID>`. To find a creature's ID, call list_creatures.", Commander.OnCommandReceived);

            // DEBUG
            helper.ConsoleCommands.Add("debug_skinmaps", "DEBUG: Prints all info in current skin maps", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("debug_idmaps", "DEBUG: Prints AnimalLongToShortIDs", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("debug_manners", "DEBUG: Print all manners", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("debug_horses", "DEBUG: Print all horses that exist on the map, not just player-owned ones", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("find_horse", "DEBUG: Find where the heck dat horse at. Format: debug_find_horse <horse ID>.", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("adopt_pet", "DEBUG: Add pet. Warp to farm.", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("summon_horse", "DEBUG: Summons a wild horse. Somewhere.", Commander.OnCommandReceived);
            helper.ConsoleCommands.Add("corral_horses", "DEBUG: Warp all horses to the farm's stable, giving you the honor of being a clown car chauffeur.", Commander.OnCommandReceived);

        }


        /// <summary>Standardize internal types and file names to have no spaces and to be entirely lowercase. </summary>
        public static string Sanitize(string input)
        {
            input = input.ToLower().Replace(" ", "");
            return string.IsInterned(input) ?? input;
        }


        /// <summary>Returns an enumerable list containing all horses owned.</summary>
        public static IEnumerable<Horse> GetHorses()
        {
            foreach (NPC npc in Utility.getAllCharacters())
                if (npc is Horse horse)
                    yield return horse;
        }


        /// <summary>Returns an enumerable list containing all pets owned.</summary>
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






        /************************
        ** Protected / Internal
        *************************/

        /// <summary>Returns the creature of the given category and ID</summary>
        internal static Character GetCreature(CreatureCategory creatureCategory, long id)
        {
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
                    foreach (FarmAnimal animal in GetAnimals())
                        if (id == animal.myID.Value)
                            return animal;
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

            // Creature type not handled by Animal Skinner. No skin to return.
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
                    if (horse.Manners == WildHorse.WildID)
                    {
                        this.Monitor.Log($"Wild ID: {Creator.HorseInfo.SkinID}", LogLevel.Info);
                        return GetSkinFromID(horse.GetType().Name, Creator.HorseInfo.SkinID);
                    }
                    // Horse is not in system
                    else if (horse.Manners == 0 || !HorseSkinMap.ContainsKey(horse.Manners))
                    {
                        this.Monitor.Log($"Horse not in system: {horse.Name}", LogLevel.Error);
                        return null;
                    }

                    // Ensure skin ID given is a valid number for the given horse type
                    int horseSkinID = HorseSkinMap[horse.Manners];
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
                    else if (PetAssets[Sanitize(pet.GetType().Name)].Count == 0)
                        return null;

                    // Pet is not in system
                    if (pet.Manners == 0 || !PetSkinMap.ContainsKey(pet.Manners))
                    {
                        this.Monitor.Log($"Pet not in system: {pet.Name}", LogLevel.Error);
                        return null;
                    }

                    // Ensure skin ID given is a current valid number for the given pet type
                    int petSkinID = PetSkinMap[pet.Manners];
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
                    else if (AnimalAssets[Sanitize(animal.type.Value)].Count == 0)
                        return null;

                    // Set sub-type if applicable
                    if (ModApi.HasBabySprite(animalType) && animal.age.Value < animal.ageWhenMature.Value)
                        animalType = "baby" + animalType;
                    else if (ModApi.HasShearedSprite(animalType) && animal.currentProduce.Value <= 0)
                        animalType = "sheared" + animalType;

                    // Animal is not in system
                    if (!AnimalSkinMap.ContainsKey(animal.myID.Value))
                        return null;

                    // Ensure skin ID given is a current valid number for the given animal type
                    int animalSkinID = AnimalSkinMap[animal.myID.Value];
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
                    if (HorseAssets[Sanitize(horse.GetType().Name)].Count == 0)
                        return 0;
                    HorseSkinMap[horse.Manners] = skinID;
                    break;

                case Pet pet:
                    if (PetAssets[Sanitize(pet.GetType().Name)].Count == 0)
                        return 0;
                    PetSkinMap[pet.Manners] = skinID;
                    break;

                case FarmAnimal animal:
                    if (AnimalAssets[Sanitize(animal.type.Value)].Count == 0)
                        return 0;
                    AnimalSkinMap[animal.myID.Value] = skinID;
                    break;

                default:
                    break;
            }

            UpdateSkin(creature);
            return skinID;
        }


        /// <summary>Returns an unused Short ID value for the given creature type to use.</summary>
        private int GetUnusedShortID(CreatureCategory creatureCategory)
        {
            int newShortID = 1;

            switch (creatureCategory)
            {
                case CreatureCategory.Horse:
                    List<int> usedHorseIDs = new List<int>();
                    foreach (Horse horse in GetHorses())
                        usedHorseIDs.Add(horse.Manners);

                    while (usedHorseIDs.Contains(newShortID))
                        newShortID++;

                    break;

                case CreatureCategory.Pet:
                    List<int> usedPetIDs = new List<int>();
                    foreach (Pet pet in GetPets())
                        usedPetIDs.Add(pet.Manners);

                    while (usedPetIDs.Contains(newShortID))
                        newShortID++;

                    break;

                case CreatureCategory.Animal:
                    while (AnimalShortToLongIDs.ContainsKey(newShortID))
                        newShortID++;
                    break;

                default:
                    break;
            }

            return newShortID;
        }






        /************************
        ** Save/Load/Update logic
        *************************/

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // Check if a horse being ridden has been dismounted. If so, re-add it to the map.
            List<Horse> dismounted = new List<Horse>();
            foreach (Horse horse in BeingRidden)
            {
                if (BeingRidden.Contains(horse) && horse.rider == null)
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
                {
                    this.Monitor.Log($"{horse.Name} has been dismounted", LogLevel.Info);
                    BeingRidden.Remove(horse);
                }

            // Check that animal list is up to date. If not, add/remove animal in system.
            if (Game1.getFarm() != null && AnimalCount != Game1.getFarm().getAllFarmAnimals().Count)
            {
                List<long> existingAnimals = new List<long>();
                List<FarmAnimal> newAnimals = new List<FarmAnimal>();

                // Check for new animals and populate lists containing existing and new animals
                foreach (FarmAnimal animal in GetAnimals())
                {
                    // New animal was found
                    if (!AnimalSkinMap.ContainsKey(animal.myID.Value))
                        // Remember new animals
                        newAnimals.Add(animal);

                    // Remember pre-existing animals
                    else
                        existingAnimals.Add(animal.myID.Value);
                }

                // Check for removed animals, comparing with the list of existing IDs found above
                List<long> animalsToRemove = new List<long>();
                foreach (long id in AnimalSkinMap.Keys)
                    // Removed animal has been found
                    if (!existingAnimals.Contains(id))
                        animalsToRemove.Add(id);

                // Remove animals found to be gone
                foreach (long id in animalsToRemove)
                    RemoveCreature(CreatureCategory.Animal, id);

                // Add new animals
                foreach (FarmAnimal animal in newAnimals)
                    AddCreature(animal);

                // Update last known animal count
                AnimalCount = Game1.getFarm().getAllFarmAnimals().Count;
            }


            // Display name tooltips when hovering over a pet or horse
            bool isHovering = false;
            Vector2 mousePos = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;

            // Show pet tooltip
            foreach (Pet pet in GetPets())
            {
                if (IsWithinSpriteBox(mousePos, pet))
                {
                    isHovering = true;
                    HoverText = pet.displayName;
                }
            }
            // Show horse tooltip
            foreach (Horse horse in GetHorses())
            {
                if (IsWithinSpriteBox(mousePos, horse))
                {
                    isHovering = true;
                    HoverText = horse.displayName;
                }
            }

            // Clear hover text when not hovering over a pet or horse
            if (!isHovering)
            {
                HoverText = null;
            }
        }


        /// <summary>Handler to remember the current horses being ridden, such that they can be manually re-added, preventing the disappearence of dismounted multihorses.</summary>
        private void SaveTheHorse(object sender, NpcListChangedEventArgs e)
        {
            foreach (NPC npc in e.Removed)
                if (npc is Horse horse && horse.rider != null)
                    BeingRidden.Add(horse);
        }


        /// <summary>Renders the name hover tooltip if a pet or horse is being hovered over</summary>
        private void RenderHoverTooltip(object sender, RenderingHudEventArgs e)
        {
            if (Context.IsPlayerFree && HoverText != null)
                this.DrawSimpleTooltip(Game1.spriteBatch, HoverText, Game1.smallFont);
        }


        /// <summary>Returns true if the given mouse cursor location is over the given pet or horse's location</summary>
        private bool IsWithinSpriteBox(Vector2 mousePos, Character creature)
        {
            if ((int)mousePos.X >= creature.getLeftMostTileX().X && (int)mousePos.X <= creature.getRightMostTileX().X &&
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


        /// <summary>Adds a creature into the Animal Skinner system.</summary>
        /// <param name="creature">The StardewValley.Character type creature (animal, pet, or horse) to add to the system</param>
        /// <param name="skin">Optional parameter. Given when a creature is being created with a predetermined skin.</param>
        internal void AddCreature(Character creature, int skin = 0)
        {
            switch (creature)
            {
                case Horse horse:
                    if (horse.Manners == WildHorse.WildID)
                        this.Monitor.Log("WE GOT A WILD ONE", LogLevel.Info);
                    // Horse is already in system
                    if (HorseSkinMap.ContainsKey(horse.Manners))
                        break;
                    if (horse.Manners == WildHorse.WildID)
                        this.Monitor.Log("Wild didn't break", LogLevel.Info);

                    // Assign a ShortID to the horse
                    horse.Manners = GetUnusedShortID(CreatureCategory.Horse);


                    // Set horse's skin
                    if (skin == 0)
                        HorseSkinMap[horse.Manners] = SetRandomSkin(horse);
                    else
                        HorseSkinMap[horse.Manners] = skin;
                    break;

                case Pet pet:
                    // Pet is already in the system
                    if (PetSkinMap.ContainsKey(pet.Manners))
                        break;

                    // Assign a ShortID to the pet
                    pet.Manners = GetUnusedShortID(CreatureCategory.Pet);

                    // Set pet's skin
                    if (skin == 0)
                        PetSkinMap[pet.Manners] = SetRandomSkin(pet);
                    else
                        PetSkinMap[pet.Manners] = skin;
                    break;

                case FarmAnimal animal:
                    // Animal is already in system
                    if (AnimalSkinMap.ContainsKey(animal.myID.Value))
                        break;

                    // Assign a ShortID to the animal
                    int shortID = GetUnusedShortID(CreatureCategory.Animal);
                    AnimalLongToShortIDs[animal.myID.Value] = shortID;
                    AnimalShortToLongIDs[shortID] = animal.myID.Value;

                    // Set animal's skin
                    if (skin == 0)
                        AnimalSkinMap[animal.myID.Value] = SetRandomSkin(animal);
                    else
                        AnimalSkinMap[animal.myID.Value] = skin;
                    break;

                default:
                    break;
            }
        }


        /// <summary>Removes a creature from the Animal Skinner system.</summary>
        private void RemoveCreature(CreatureCategory category, long id)
        {
            switch (category)
            {
                case CreatureCategory.Horse:
                    // Horse isn't in the system
                    if (!HorseSkinMap.ContainsKey(id))
                        return;

                    HorseSkinMap.Remove(id);
                    break;

                case CreatureCategory.Pet:
                    // Pet isn't in the system
                    if (!PetSkinMap.ContainsKey(id))
                        return;

                    PetSkinMap.Remove(id);
                    break;

                case CreatureCategory.Animal:
                    // Animal isn't in the system
                    if (!AnimalSkinMap.ContainsKey(id))
                        return;

                    // Remove from ShortID lists
                    int shortID = AnimalLongToShortIDs[id];
                    AnimalLongToShortIDs.Remove(id);
                    AnimalShortToLongIDs.Remove(shortID);

                    AnimalSkinMap.Remove(id);
                    break;

                default:
                    break;
            }
        }


        private void LoadData(object s, EventArgs e)
        {
            // Only allow the host player to load Animal Skinner data
            if (!Context.IsMainPlayer)
            {
                this.Monitor.Log("Multiplayer Farmhand detected. Animal Skinner has been disabled.", LogLevel.Debug);
                return;
            }

            // Add Animal Skinner to the update loop
            this.Helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;


            // Load skin maps
            AnimalSkinMap = this.Helper.Data.ReadSaveData<Dictionary<long, int>>("animal-skin-map") ?? new Dictionary<long, int>();
            PetSkinMap = this.Helper.Data.ReadSaveData<Dictionary<long, int>>("pet-skin-map") ?? new Dictionary<long, int>();
            HorseSkinMap = this.Helper.Data.ReadSaveData<Dictionary<long, int>>("horse-skin-map") ?? new Dictionary<long, int>();

            // Load Short ID maps
            AnimalLongToShortIDs = this.Helper.Data.ReadSaveData<Dictionary<long, int>>("animal-long-to-short-ids") ?? new Dictionary<long, int>();
            AnimalShortToLongIDs = this.Helper.Data.ReadSaveData<Dictionary<int, long>>("animal-short-to-long-ids") ?? new Dictionary<int, long>();

            // Load potential pet info
            List<string> potentialPet = this.Helper.Data.ReadSaveData<List<string>>("potential-pet") ?? new List<string>();
            if (potentialPet.Count != 0)
                Creator.LoadPotentialPet(potentialPet);
            else
                NewDayFromSave = false;

            // Load wild horse info
            List<string> wildHorse = this.Helper.Data.ReadSaveData<List<string>>("wild-horse") ?? new List<string>();
            if (wildHorse.Count != 0)
                Creator.LoadWildHorse(wildHorse);
            else
                NewDayFromSave = false;


            // Ensure Pets and Horses are using the new Pet/Horse.Manners = Short ID system, as implemented in Animal Skinner 2.0.0
            foreach (Pet pet in GetPets())
            {
                if (pet.Manners == 0)
                {
                    this.Monitor.Log($"File new to Animal Skinner or older Animal Skinner save detected. The data has been updated and {pet.Name}'s skin has been randomized.", LogLevel.Alert);
                    AddCreature(pet);
                }
            }
            foreach (Horse horse in GetHorses())
                if (horse.Manners == 0)
                {
                    this.Monitor.Log($"File new to Animal Skinner or older Animal Skinner save detected. The data has been updated and {horse.Name}'s skin has been randomized.", LogLevel.Alert);
                    AddCreature(horse);
                }


            // Set creature skins
            foreach (Horse horse in GetHorses())
                UpdateSkin(horse);
            foreach (Pet pet in GetPets())
                UpdateSkin(pet);
            foreach (FarmAnimal animal in GetAnimals())
                UpdateSkin(animal);


            // Set last known animal count
            AnimalCount = Game1.getFarm().getAllFarmAnimals().Count;
        }


        /// <summary>Refreshes the given animal, pet, or horse's skin texture with the one Animal Skinner has saved for it.</summary>
        internal void UpdateSkin(Character creature)
        {
            switch (creature)
            {
                case Horse horse:
                    if ( Creator.HorseInfo != null && horse.Manners == WildHorse.WildID)
                    {
                        horse.Sprite = new AnimatedSprite(GetSkinFromID(horse.GetType().Name, Creator.HorseInfo.SkinID).AssetKey, 0, 32, 32);
                        break;
                    }
                    AnimalSkin horseSkin = GetSkin(horse);
                    if (horseSkin != null && horse.Sprite.textureName.Value != horseSkin.AssetKey)
                        horse.Sprite = new AnimatedSprite(horseSkin.AssetKey, 7, 32, 32);
                    break;

                case Pet pet:
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


        private void SaveData(object s, EventArgs e)
        {
            // Only allow the host player to save Animal Skinner data
            if (!Context.IsMainPlayer)
                return;

            // Remove Animal Skinner from update loop
            this.Helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked;

            // Save skin maps
            this.Helper.Data.WriteSaveData("animal-skin-map", AnimalSkinMap);
            this.Helper.Data.WriteSaveData("pet-skin-map", PetSkinMap);
            this.Helper.Data.WriteSaveData("horse-skin-map", HorseSkinMap);

            // Save Short ID maps
            this.Helper.Data.WriteSaveData("animal-long-to-short-ids", AnimalLongToShortIDs);
            this.Helper.Data.WriteSaveData("animal-short-to-long-ids", AnimalShortToLongIDs);

            // Save potential pet info
            this.Helper.Data.WriteSaveData("potential-pet", Creator.GetPetSaveInfo());

            // Save wild horse info
            this.Helper.Data.WriteSaveData("wild-horse", Creator.GetHorseSaveInfo());

            // Save data version
            this.Helper.Data.WriteSaveData("data-version", "2");
        }


        /// <summary>Sets up initial values needed for Animal Skinner.</summary>
        private void Setup(object sender, SaveLoadedEventArgs e)
        {
            ModApi.RegisterDefaultTypes();
            IntegrateMods();

            LoadAssets();

            // Remove the Setup from the loop, so that it isn't done twice when the player returns to the title screen and loads again
            this.Helper.Events.GameLoop.SaveLoaded -= this.Setup;
        }


        private void IntegrateMods()
        {
            ISemanticVersion bfavVersion;
            if (this.Helper.ModRegistry.IsLoaded("Paritee.BetterFarmAnimalVariety"))
            {
                bfavVersion = this.Helper.ModRegistry.Get("Paritee.BetterFarmAnimalVariety").Manifest.Version;

                if (bfavVersion.IsNewerThan("2.2.6"))
                    BFAV300Worker = new BFAV300Integrator();
                else
                    BFAV226Worker = new BFAV226Integrator();
            }
        }


        /// <summary>Load skin assets into AnimalSkinner</summary>
        private void LoadAssets()
        {
            // Gather handled types
            string validTypes = string.Join(", ", ModApi.GetHandledAllTypes());

            foreach (FileInfo file in new DirectoryInfo(Path.Combine(this.Helper.DirectoryPath, "assets", "skins")).EnumerateFiles())
            {
                // Check extension of file is handled by AnimalSkinner
                string extension = Path.GetExtension(file.Name);
                if (!this.ValidExtensions.Contains(extension))
                {
                    this.Monitor.Log($"Ignored skin `assets/skins/{file.Name}` with invalid extension (extension must be one of type {string.Join(", ", this.ValidExtensions)})", LogLevel.Warn);
                    continue;
                }

                // Parse file name
                string[] nameParts = Path.GetFileNameWithoutExtension(file.Name).Split(new[] { '_' }, 2);
                string type = Sanitize(nameParts[0]);
                // Ensure creature type is handled by AnimalSkinner
                if (!PetAssets.ContainsKey(type) && !HorseAssets.ContainsKey(type) && !AnimalAssets.ContainsKey(type))
                {
                    this.Monitor.Log($"Ignored skin `assets/skins/{file.Name}` with invalid naming convention (can't parse {nameParts[0]} as an animal, pet, or horse. Expected one of type: {validTypes})", LogLevel.Warn);
                    continue;
                }
                // Ensure both a type and skin ID can be found in the file name
                if (nameParts.Length != 2)
                {
                    this.Monitor.Log($"Ignored skin `assets/skins/{file.Name} with invalid naming convention (no skin ID found)", LogLevel.Warn);
                    continue;
                }
                // Ensure the skin ID is a number
                int skinID = 0;
                if (nameParts.Length == 2 && !int.TryParse(nameParts[1], out skinID))
                {
                    this.Monitor.Log($"Ignored skin `assets/skins/{file.Name}` with invalid skin ID (can't parse {nameParts[1]} as a number)", LogLevel.Warn);
                    continue;
                }
                // Ensure the skin ID is not 0 or negative
                if (skinID <= 0)
                {
                    this.Monitor.Log($"Ignored skin `assets/skins/{file.Name}` with skin ID of less than or equal to 0. Skins must have an ID of at least 1.");
                }

                // File naming is valid, add asset into system
                string assetKey = this.Helper.Content.GetActualAssetKey(Path.Combine("assets", "skins", extension.Equals("xnb") ? Path.Combine(Path.GetDirectoryName(file.Name), Path.GetFileNameWithoutExtension(file.Name)) : file.Name));
                if (AnimalAssets.ContainsKey(type))
                    AnimalAssets[type].Add(new AnimalSkin(type, skinID, assetKey));
                else if (HorseAssets.ContainsKey(type))
                    HorseAssets[type].Add(new AnimalSkin(type, skinID, assetKey));
                else
                    PetAssets[type].Add(new AnimalSkin(type, skinID, assetKey));
            }


            // Sort each list
            AnimalSkin.Comparer comp = new AnimalSkin.Comparer();
            foreach (string type in AnimalAssets.Keys)
                AnimalAssets[type].Sort((p1, p2) => comp.Compare(p1, p2));
            foreach (string type in PetAssets.Keys)
                PetAssets[type].Sort((p1, p2) => comp.Compare(p1, p2));
            foreach (string type in HorseAssets.Keys)
                HorseAssets[type].Sort((p1, p2) => comp.Compare(p1, p2));


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
            this.Monitor.Log(summary.ToString(), LogLevel.Trace);
        }
        
    }
}
