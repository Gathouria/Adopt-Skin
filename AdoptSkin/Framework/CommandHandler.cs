using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Buildings;
using StardewValley.Menus;

namespace AdoptSkin.Framework
{
    /// <summary>The SMAPI console command handler for Adopt & Skin</summary>
    class CommandHandler
    {
        /// <summary>Allowable custom creature group denotations for use in commands</summary>
        internal static readonly List<string> CreatureGroups = new List<string>() { "all", "animal", "coop", "barn", "chicken", "cow", "pet", "horse" };

        internal ModEntry Earth;

        internal CommandHandler(ModEntry modEntry)
        {
            Earth = modEntry;
        }


        /*****************************
        ** Console Command Handlers
        ******************************/

        /// <summary>Handles commands for the SMAPI console</summary>
        internal void OnCommandReceived(string command, string[] args)
        {
            switch (command)
            {
                case "debug_reset":
                    if (!EnforceArgCount(args, 0))
                        break;

                    ModEntry.SkinMap = new Dictionary<long, int>();

                    ModEntry.AnimalLongToShortIDs = new Dictionary<long, int>();
                    ModEntry.AnimalShortToLongIDs = new Dictionary<int, long>();

                    ModEntry.IDToCategory = new Dictionary<long, ModEntry.CreatureCategory>();

                    foreach (Pet pet in ModEntry.GetPets())
                        if (!Stray.IsStray(pet))
                            pet.Manners = 0;
                    foreach (Horse horse in ModEntry.GetHorses())
                        if (!WildHorse.IsWildHorse(horse))
                            horse.Manners = 0;

                    foreach (Pet pet in ModEntry.GetPets())
                        if (!Stray.IsStray(pet))
                            Earth.AddCreature(pet);
                    foreach (Horse horse in ModEntry.GetHorses())
                        if (!WildHorse.IsWildHorse(horse))
                            Earth.AddCreature(horse);
                    foreach (FarmAnimal animal in ModEntry.GetAnimals())
                        Earth.AddCreature(animal);

                    break;


                case "debug_idmaps":
                    if (!EnforceArgCount(args, 0))
                        break;

                    ModEntry.SMonitor.Log($"Animals Long to Short:\n{string.Join("\n", ModEntry.AnimalLongToShortIDs)}", LogLevel.Info);
                    ModEntry.SMonitor.Log($"Animals Short to Long equal length: {ModEntry.AnimalLongToShortIDs.Count == ModEntry.AnimalShortToLongIDs.Count}", LogLevel.Alert);
                    break;


                case "debug_skinmaps":
                    if (!EnforceArgCount(args, 0))
                        break;

                    ModEntry.SMonitor.Log($"{string.Join("\n", ModEntry.SkinMap)}", LogLevel.Info);
                    break;


                case "debug_pets":
                    if (!EnforceArgCount(args, 0))
                        break;

                    foreach (NPC npc in Utility.getAllCharacters())
                        if (npc is Pet pet)
                        {
                            int petSkin = 0;
                            if (Stray.IsStray(pet))
                                petSkin = ModEntry.Creator.StrayInfo.SkinID;
                            else if (ModEntry.IsInDatabase(pet))
                                petSkin = ModEntry.SkinMap[pet.Manners];

                            ModEntry.SMonitor.Log($"[{pet.Name}] | Manners: {pet.Manners} | Skin: {petSkin} | Stray: {pet.Manners == Stray.StrayID}", LogLevel.Info);
                        }
                    break;


                case "debug_horses":
                    if (!EnforceArgCount(args, 0))
                        break;

                    foreach (NPC npc in Utility.getAllCharacters())
                        if (npc is Horse horse)
                        {
                            int horseSkin = 0;
                            if (WildHorse.IsWildHorse(horse))
                                horseSkin = ModEntry.Creator.HorseInfo.SkinID;
                            else if (ModEntry.IsInDatabase(horse))
                                horseSkin = ModEntry.SkinMap[horse.Manners];

                            ModEntry.SMonitor.Log($"[{horse.Name}] | Manners: {horse.Manners} | Skin: {horseSkin} | Wild: {horse.Manners == WildHorse.WildID}", LogLevel.Info);
                        }
                    break;


                case "summon_stray":
                    if (!EnforceArgCount(args, 0))
                        break;

                    // Get rid of any previous stray still on the map
                    if (ModEntry.Creator.StrayInfo != null)
                        ModEntry.Creator.StrayInfo.RemoveFromWorld();
                    // Create a gosh darn new stray
                    ModEntry.Creator.StrayInfo = new Stray();
                    break;


                case "summon_horse":
                    if (!EnforceArgCount(args, 0))
                        break;

                    // Get rid of any previous horse still on the map
                    if (ModEntry.Creator.HorseInfo != null)
                        ModEntry.Creator.HorseInfo.RemoveFromWorld();
                    // Create a gosh darn new horse
                    ModEntry.Creator.HorseInfo = new WildHorse();
                    break;


                case "debug_clearunowned":
                    if (!EnforceArgCount(args, 0))
                        break;

                    foreach (NPC npc in Utility.getAllCharacters())
                    {
                        if (npc is Horse horse && WildHorse.IsWildHorse(horse))
                        {
                            Game1.removeThisCharacterFromAllLocations(horse);
                        }
                        if (npc is Pet pet && Stray.IsStray(pet))
                        {
                            Game1.removeThisCharacterFromAllLocations(pet);
                        }
                    }
                    break;


                // Expected arguments: <horse ID>
                case "debug_find":
                    // Enforce argument constraints
                    if (!EnforceArgCount(args, 2) ||
                        !EnforceArgTypeCategory(args[0], 1) ||
                        !EnforceArgTypeInt(args[1], 2))
                        break;

                    string cat = ModEntry.Sanitize(args[0]);
                    int id = int.Parse(args[1]);

                    switch (cat)
                    {
                        case "horse":
                            foreach (Horse horse in ModEntry.GetHorses())
                                if (horse.Manners == id)
                                    ModEntry.SMonitor.Log($"{horse.Name} located at: {horse.currentLocation} | {horse.getTileX()}, {horse.getTileY()}", LogLevel.Alert);
                            break;

                        case "pet":
                            foreach (Pet pet in ModEntry.GetPets())
                                if (pet.Manners == id)
                                    ModEntry.SMonitor.Log($"{pet.Name} located at: {pet.currentLocation} | {pet.getTileX()}, {pet.getTileY()}", LogLevel.Alert);
                            break;

                        case "animal":
                            foreach (FarmAnimal animal in ModEntry.GetAnimals())
                                if (ModEntry.AnimalLongToShortIDs[animal.myID.Value] == id)
                                    ModEntry.SMonitor.Log($"{animal.Name} located at: {animal.currentLocation} | {animal.getTileX()}, {animal.getTileY()}", LogLevel.Alert);
                            break;

                        default:
                            break;
                    }

                    break;


                case "horse_whistle":
                    if (args.ToList().Count == 1)
                    {
                        // Enforce argument constraints
                        if (!EnforceArgTypeInt(args[0], 1))
                            break;

                        int callID = int.Parse(args[0]);
                        foreach (Horse horse in ModEntry.GetHorses())
                        {
                            if (horse.Manners == callID)
                            {
                                Game1.warpCharacter(horse, Game1.player.currentLocation, Game1.player.getTileLocation());
                                return;
                            }
                        }
                        // Horse with given ID wasn't found
                        ModEntry.SMonitor.Log($"No horse exists with the given ID: {callID}", LogLevel.Error);
                    }
                    else
                    {
                        // Enforce argument constraints
                        if (!EnforceArgCount(args, 0))
                            break;

                        // Call the first horse found
                        ModEntry.CallHorse();
                    }
                    break;


                case "corral_horses":
                    ModEntry.CorralHorses();
                    break;


                case "sell":
                    // Enforce argument constraints
                    if (!EnforceArgCount(args, 1))
                        break;
                    if (!EnforceArgTypeInt(args[0], 1))
                        break;


                    int sellID = int.Parse(args[1]);
                    ModEntry.CreatureCategory sellCategory = ModEntry.GetCreatureCategory(sellID);
                    if (sellCategory == ModEntry.CreatureCategory.Pet || sellCategory == ModEntry.CreatureCategory.Horse)
                    {
                        Character sellCreature = ModEntry.GetCreature(sellID);
                        Game1.activeClickableMenu = new ConfirmationDialog($"Are you sure you want to sell your {ModEntry.Sanitize(sellCreature.GetType().Name)}, {sellCreature.Name}?", (who) =>
                        {
                            if (Game1.activeClickableMenu is StardewValley.Menus.ConfirmationDialog cd)
                                cd.cancel();

                            Earth.RemoveCreature(sellID);
                            Game1.removeThisCharacterFromAllLocations((NPC)sellCreature);
                        });
                    }
                    else
                    {
                        ModEntry.SMonitor.Log("You may only sell pets and horses via the A&S console. The ID given is for a farm animal or unknown type.", LogLevel.Error);
                        return;
                    }

                    break;


                // Expected arguments: <creature type/category/group>
                case "list_creatures":
                    // Enforce argument constraints
                    if (!EnforceArgCount(args, 1) ||
                        !EnforceArgTypeGroup(args[0]))
                        break;

                    PrintRequestedCreatures(ModEntry.Sanitize(args[0]));
                    break;


                // Expected arguments: None.
                case "randomize_all_skins":
                    // Enforce argument constraints
                    if (!EnforceArgCount(args, 0))
                        break;

                    // Randomize all skins
                    foreach (Horse horse in ModEntry.GetHorses())
                        Earth.SetRandomSkin(horse);
                    foreach (Pet pet in ModEntry.GetPets())
                        Earth.SetRandomSkin(pet);
                    foreach (FarmAnimal animal in ModEntry.GetAnimals())
                        Earth.SetRandomSkin(animal);

                    ModEntry.SMonitor.Log("All animal, pet, and horse skins have been randomized.", LogLevel.Alert);
                    break;


                // Expected arguments: <creature category>, <creature ID>
                case "randomize_skin":
                    // Enforce argument constraints
                    if (!EnforceArgCount(args, 1))
                        break;
                    if (!EnforceArgTypeInt(args[1], 1))
                        break;

                    // Find associated creature instance
                    int creatureID = int.Parse(args[0]);
                    Character creature = ModEntry.GetCreature(creatureID);

                    // A creature was able to be located with the given category and ID
                    if (creature != null)
                    {
                        int newSkin = Earth.SetRandomSkin(creature);
                        if (newSkin == 0)
                            ModEntry.SMonitor.Log($"No skins are located in `/assets/skins` for {creature.Name}'s type: {ModEntry.Sanitize(creature.GetType().Name)}", LogLevel.Error);
                        else
                            ModEntry.SMonitor.Log($"{creature.Name}'s skin has been randomized.", LogLevel.Alert);
                    }
                    else
                    {
                        ModEntry.SMonitor.Log($"Creature with given ID does not exist: {creatureID}", LogLevel.Error);
                    }


                    break;


                // Expected arguments: <skin ID>, <creature category>, <creature ID>
                case "set_skin":
                    // Enforce argument constraints
                    if (!EnforceArgCount(args, 2))
                        break;
                    if (!EnforceArgTypeInt(args[0], 1) || !EnforceArgTypeInt(args[1], 2))
                        break;

                    int skinID = int.Parse(args[0]);
                    int shortID = int.Parse(args[1]);

                    Character creatureToSkin = ModEntry.GetCreature(shortID);

                    // DEBUG: Did we find it
                    if (creatureToSkin == null)
                        ModEntry.SMonitor.Log($"SET SKIN:: Creature is NULL (ID {shortID})", LogLevel.Error);
                    else
                        ModEntry.SMonitor.Log($"SET SKIN:: Creature found {creatureToSkin.GetType().Name}, {creatureToSkin.Name}", LogLevel.Info);


                    if (!ModEntry.IsInDatabase(creatureToSkin))
                    {
                        ModEntry.SMonitor.Log($"No creature is registered with the given ID: {shortID}", LogLevel.Error);
                        return;
                    }


                    // Enforce argument range to the range of the available skins for this creature's type
                    ModEntry.CreatureCategory category = ModEntry.GetCreatureCategory(shortID);
                    switch (category)
                    {
                        case ModEntry.CreatureCategory.Pet:
                            if (!EnforceArgRange(skinID, ModEntry.PetAssets[(creatureToSkin as Pet).GetType().Name].Count, 2))
                                return;
                            break;

                        case ModEntry.CreatureCategory.Horse:
                            if (!EnforceArgRange(skinID, ModEntry.HorseAssets[(creatureToSkin as Horse).GetType().Name].Count, 2))
                                return;
                            break;

                        case ModEntry.CreatureCategory.Animal:
                            if (!EnforceArgRange(skinID, ModEntry.AnimalAssets[(creatureToSkin as FarmAnimal).type.Value].Count, 2))
                                return;
                            break;

                        default:
                            return;
                    }


                    // Successfully found given creature to set skin for. Run a skin update.
                    if (creatureToSkin != null)
                    {
                        ModEntry.SkinMap[shortID] = skinID;
                        Earth.UpdateSkin(creatureToSkin);
                        ModEntry.SMonitor.Log($"{creatureToSkin.Name}'s skin has been set to skin {skinID}", LogLevel.Alert);
                    }
                    else
                        ModEntry.SMonitor.Log($"Skin setting error. Creature with ID {shortID} could not be given skin {skinID}", LogLevel.Error);

                    break;


                default:
                    break;
            }
        }






        /// <summary>Checks that the correct number of arguments are given for a console command.</summary>
        /// <param name="args">Arguments given to the command</param>
        /// <param name="number">Correct number of arguments to give to the command</param>
        /// <returns>Returns true if the correct number of arguments was given. Otherwise gives a console error report and returns false.</returns>
        internal static bool EnforceArgCount(string[] args, int number)
        {
            if (args.Length == number)
            {
                return true;
            }
            ModEntry.SMonitor.Log($"Incorrect number of arguments given. The command requires {number} arguments, {args.Length} were given.", LogLevel.Error);
            return false;
        }


        /// <summary>Checks that the given int argument is less than or equal to the given maxValue, and at least of value 1</summary>
        /// <param name="arg">The argument being checked</param>
        /// <param name="argNumber">The numbered order of the argument for the command (e.g. the first argument would be argNumber = 1)</param>
        /// <param name="maxValue">The maximum value that the given argument is allowed to be</param>
        /// <returns>Returns true if the given argument is within the range of 1 to maxValue. Otherwise gives a console error report and returns false.</returns>
        internal static bool EnforceArgRange(int arg, int maxValue, int argNumber)
        {
            if (arg < 1 || arg > maxValue)
            {
                ModEntry.SMonitor.Log($"Incorrect argument range. Argument {argNumber} should be of a value between 1 and {maxValue} (inclusive)", LogLevel.Error);
                return false;
            }
            return true;
        }


        /// <summary>Checks that the argument given is able to be parsed into an integer.</summary>
        /// <param name="arg">The argument to be checked</param>
        /// <param name="argNumber">The numbered order of the argument for the command (e.g. the first argument would be argNumber = 1)</param>
        /// <returns>Returns true if the given argument can be parsed as an int. Otherwise gives a console error report and returns false.</returns>
        internal static bool EnforceArgTypeInt(string arg, int argNumber)
        {
            if (!int.TryParse(arg, out int parsedArg))
            {
                ModEntry.SMonitor.Log($"Incorrect argument type given for argument {argNumber}. Expected type: int", LogLevel.Error);
                return false;
            }
            return true;
        }


        /// <summary>Checks that the given argument is of a recognized creature category.</summary>
        /// <param name="arg">The argument to be checked</param>
        /// <param name="argNumber">The numbered order of the argument for the command (e.g. the first argument would be argNumber = 1)</param>
        /// <returns>Returns true if the given argument is one of the strings "animal", "pet", or "horse". Otherwise gives a console error report and returns false.</returns>
        internal static bool EnforceArgTypeCategory(string arg, int argNumber)
        {
            string type = ModEntry.Sanitize(arg);

            if (type != "animal" && type != "pet" && type != "horse")
            {
                ModEntry.SMonitor.Log($"Incorrect argument type given for argument {argNumber}. Expected one of creature categories: animal, pet, horse", LogLevel.Error);
                return false;
            }
            return true;
        }


        /// <summary>Checks that the given argument is of a recognized creature group or recognized creature type.</summary>
        /// <param name="arg">The argument to be checked</param>
        /// <returns>Returns true if the given argument is stored in CreatureGroups or is a known FarmAnimal, Pet, or Horse type. Otherwise gives a console error report and returns false.</returns>
        internal static bool EnforceArgTypeGroup(string arg)
        {
            string type = ModEntry.Sanitize(arg);
            List<string> handledTypes = ModApi.GetHandledAllTypes();

            if (!CreatureGroups.Contains(type) && !handledTypes.Contains(type))
            {
                ModEntry.SMonitor.Log($"Argument given isn't one of {string.Join(", ", CreatureGroups)}, or a handled creature type. Handled types:\n{string.Join(", ", handledTypes)}", LogLevel.Error);
                return false;
            }

            return true;
        }






        /******************
        ** Print Strings
        ******************/

        /// <summary>Prints the the requested creature information from the list_animals console command.</summary>
        internal static void PrintRequestedCreatures(string arg)
        {
            string type = ModEntry.Sanitize(arg);

            // Handle animal portion of "all" argument and the "animal" argument
            if (type == "all" || type == "animal")
            {
                List<string> animalInfo = new List<string>();

                foreach (FarmAnimal animal in ModEntry.GetAnimals())
                    animalInfo.Add(GetPrintString(animal));

                ModEntry.SMonitor.Log("Animals:", LogLevel.Alert);
                ModEntry.SMonitor.Log($"{string.Join(", ", animalInfo)}", LogLevel.Info);
            }
            // Handle coop animal types only
            else if (type == "coop")
            {
                List<string> coopInfo = new List<string>();

                foreach (FarmAnimal animal in ModEntry.GetAnimals())
                {
                    if (animal.isCoopDweller())
                        coopInfo.Add(GetPrintString(animal));
                }

                ModEntry.SMonitor.Log("Coop Animals:", LogLevel.Alert);
                ModEntry.SMonitor.Log($"{string.Join(", ", coopInfo)}", LogLevel.Info);
            }
            // Handle barn animal types only
            else if (type == "barn")
            {
                List<string> barnInfo = new List<string>();

                foreach (FarmAnimal animal in ModEntry.GetAnimals())
                {
                    if (!animal.isCoopDweller())
                        barnInfo.Add(GetPrintString(animal));
                }

                ModEntry.SMonitor.Log("Barn Animals:", LogLevel.Alert);
                ModEntry.SMonitor.Log($"{string.Join(", ", barnInfo)}", LogLevel.Info);
            }
            // Handle chicken type arguments
            else if (type == "chicken")
            {
                List<string> chickenInfo = new List<string>();

                foreach (FarmAnimal animal in ModEntry.GetAnimals())
                {
                    string potentialChicken = ModEntry.Sanitize(animal.type.Value);

                    if (ModApi.IsChicken(potentialChicken))
                        chickenInfo.Add(GetPrintString(animal));
                }
                ModEntry.SMonitor.Log("Chickens:", LogLevel.Alert);
                ModEntry.SMonitor.Log($"{string.Join(", ", chickenInfo)}", LogLevel.Info);
            }
            // Handle cow type arguments
            else if (type == "cow")
            {
                List<string> cowInfo = new List<string>();

                foreach (FarmAnimal animal in ModEntry.GetAnimals())
                {
                    string potentialCow = ModEntry.Sanitize(animal.type.Value);

                    if (ModApi.IsCow(potentialCow))
                        cowInfo.Add(GetPrintString(animal));
                }
                ModEntry.SMonitor.Log("Cows:", LogLevel.Alert);
                ModEntry.SMonitor.Log($"{string.Join(", ", cowInfo)}", LogLevel.Info);
            }
            // Handle other animal type arguments
            else if (ModApi.GetHandledAnimalTypes().Contains(type))
            {
                List<string> animalInfo = new List<string>();

                foreach (FarmAnimal animal in ModEntry.GetAnimals())
                {
                    if (type == ModEntry.Sanitize(animal.type.Value))
                        animalInfo.Add(GetPrintString(animal));
                }
                ModEntry.SMonitor.Log($"{arg}s:", LogLevel.Alert);
                ModEntry.SMonitor.Log($"{string.Join(", ", animalInfo)}", LogLevel.Info);
            }


            // Handle the pet portion of the "all" argument and the "pet" argument
            if (type == "all" || type == "pet")
            {
                List<string> petInfo = new List<string>();

                foreach (Pet pet in ModEntry.GetPets())
                    if (!Stray.IsStray(pet))
                        petInfo.Add(GetPrintString(pet));

                ModEntry.SMonitor.Log("Pets:", LogLevel.Alert);
                ModEntry.SMonitor.Log($"{string.Join(", ", petInfo)}", LogLevel.Info);

            }
            else if (ModApi.GetHandledPetTypes().Contains(type))
            {
                List<string> petInfo = new List<string>();

                foreach (Pet pet in ModEntry.GetPets())
                {
                    if (type == ModEntry.Sanitize(pet.GetType().Name) && !Stray.IsStray(pet))
                        petInfo.Add(GetPrintString(pet));
                }
                ModEntry.SMonitor.Log($"{arg}s:", LogLevel.Alert);
                ModEntry.SMonitor.Log($"{string.Join(", ", petInfo)}", LogLevel.Info);
            }


            // Handle the horse portion of the "all" argument and the horse argument
            if (type == "all" || ModApi.GetHandledHorseTypes().Contains(type))
            {
                List<string> horseInfo = new List<string>();

                foreach (Horse horse in ModEntry.GetHorses())
                    if (!WildHorse.IsWildHorse(horse) && ModEntry.IsNotATractor(horse))
                        horseInfo.Add(GetPrintString(horse));

                ModEntry.SMonitor.Log("Horses:", LogLevel.Alert);
                ModEntry.SMonitor.Log($"{string.Join(", ", horseInfo)}", LogLevel.Info);
            }
        }


        /// <summary>Return the information on a pet or horse that the list_animals console command uses.
        internal static string GetPrintString(Character creature)
        {
            List<string> handledTypes = new List<string>();
            string name = creature.Name;
            string type = "";
            long shortID = ModEntry.GetShortID(creature);
            int skinID = 0;
            if (ModEntry.SkinMap.ContainsKey(shortID))
                skinID = ModEntry.SkinMap[shortID];

            switch (creature)
            {
                case Horse horse:
                    handledTypes = ModApi.GetHandledHorseTypes();
                    type = ModEntry.Sanitize(horse.GetType().Name);
                    break;

                case Pet pet:
                    handledTypes = ModApi.GetHandledPetTypes();
                    type = ModEntry.Sanitize(pet.GetType().Name);
                    break;

                case FarmAnimal animal:
                    handledTypes = ModApi.GetHandledAnimalTypes();
                    type = ModEntry.Sanitize(animal.type.Value);
                    break;

                default:
                    return "";
            }

            if (handledTypes.Contains(type))
            {
                return $"\n # {name}:  Type - {type}\n" +
                    $"Short ID:   {shortID}\n" +
                    $"Skin ID:    {skinID}";
            }
            else
            {
                return $"\n # {name}:  Type - {type}\n" +
                    "Skin type not handled\n";
            }
        }
    }
}
