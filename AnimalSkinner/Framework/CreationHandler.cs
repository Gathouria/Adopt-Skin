using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.Menus;
using StardewValley.Characters;
using StardewValley.Buildings;

namespace AnimalSkinner.Framework
{
    class CreationHandler
    {
        /************************
        ** Fields
        *************************/

        /// <summary>Randomizer for logic within CreationHandler instances.</summary>
        private readonly Random Randomizer = new Random();

        /// <summary>Reference to Animal Skinner's ModEntry. Used to access creature information and print information to the monitor when necessary.</summary>
        internal ModEntry Earth;

        /// <summary>Whether or not a potential pet is available for adoption at Marnie's currently</summary>
        internal bool CanAdopt;

        /// <summary>The pet available for adoption at Marnies for the current day.</summary>
        //internal PotentialPet PetInfo { get => PetInfo; set => NewPotentialPet(); }
        internal PotentialPet PetInfo;
        /// <summary>The wild horse available for adoption somewhere in the map. This variable is null if there is no current wild horse.</summary>
        //internal WildHorse HorseInfo { get => HorseInfo; set => NewWildHorse(); }
        internal WildHorse HorseInfo;






        /// <summary>The handler and creator for potential pets and wild horses to adopt</summary>
        /// <param name="modentry"></param>
        internal CreationHandler(ModEntry modentry)
        {
            Earth = modentry;
        }


        /// <summary>Returns true if a pet in available for adoption at Marnie's.</summary>
        public bool CanAdoptNow()
        {
            return CanAdopt ? true : false;
        }


        internal void GetHorseInfo()
        {
            if (HorseInfo == null)
                ModEntry.SMonitor.Log("No wild horse exists", LogLevel.Info);
            else
                ModEntry.SMonitor.Log($"Wild horse located at {HorseInfo.Map}: {HorseInfo.Tile}\nSkin ID {HorseInfo.SkinID}", LogLevel.Info);
        }


        /// <summary>Resets variables that change each day</summary>
        internal void ProcessNewDay(object sender, DayStartedEventArgs e)
        {
            // Don't load new potential pets or wild horses if it's already been loaded from the save data
            if (Earth.NewDayFromSave)
            {
                Earth.NewDayFromSave = false;
                return;
            }
            else
            {
                NewPotentialPet();
                NewWildHorse();
            }
        }


        /// <summary>Creates a new PotentialPet at Marnie's. Used for new save files.</summary>
        internal void NewPotentialPet()
        {
            PetInfo = new PotentialPet();
            CanAdopt = true;
        }


        /// <summary>Checks the chance for a WildHorse to spawn if a stable exists on the farm, and spawns one if it should. Used for new save files.</summary>
        internal void NewWildHorse(bool overrideChance = false)
        {
            // If the previous day had a WildHorse, remove it from the map
            if (HorseInfo != null)
                HorseInfo.Remove();

            // Check chance for a WildHorse spawn, and spawn a new WildHorse if there should be one
            if (Randomizer.Next(0, 4) == 0 || overrideChance)
            {
                foreach (Building b in Game1.getFarm().buildings)
                    if (b is Stable || overrideChance)
                    {
                        HorseInfo = new WildHorse();
                        break;
                    }
            }
        }


        /// <summary>Creates a PotentialPet using the save file's given information</summary>
        internal void LoadPotentialPet(List<string> petInfo)
        {
            PetInfo = new PotentialPet(petInfo);
        }


        /// <summary>Creates a WildHorse using the save file's given information</summary>
        internal void LoadWildHorse(List<string> horseInfo)
        {
            // If a horse wasn't in town upon save, null information was stored for it
            if (horseInfo.Count == 1)
                HorseInfo = null;
            else if (horseInfo.Count == 4)
                HorseInfo = new WildHorse(horseInfo);
            else
                HorseInfo = null;
        }


        /// <summary>
        /// Returns the PotentialPet info in the same format that Animal Skinner places it into the save file.
        /// The first item in the list holds the PotentialPet's type, and the second item should be the PotentialPet's SkinID
        /// </summary>
        internal List<string> GetPetSaveInfo()
        {
            if (PetInfo == null)
                this.NewPotentialPet();

            return new List<string> { PetInfo.PetType, PetInfo.SkinID.ToString() };
        }


        /// <summary>
        /// Returns the current wild horses's information in the same format that Animal Skinner places it in the save file.
        /// The list will contain, in order and in string format, the SkinID, Map name, Tile.X, and Tile.Y
        /// </summary>
        internal List<string> GetHorseSaveInfo()
        {
            if (HorseInfo == null)
                return new List<string> { "null" };

            return new List<string> { HorseInfo.SkinID.ToString(), HorseInfo.Map.Name.ToLower(), HorseInfo.Tile.X.ToString(), HorseInfo.Tile.Y.ToString() };
        }






        /*****************************
         ** P E T   A D O P T I O N **
         *****************************/

        // ** TODO: Function with dialogue asking if you wish to adopt this stray **

        internal void AdoptPet()
        {
            Game1.activeClickableMenu = new NamingMenu(PetNamer, "Name:");
        }


        internal void PetNamer(string petName)
        {
            Pet pet = PetInfo.CreatePet(petName);
            Earth.AddCreature(pet, PetInfo.SkinID);
            pet.warpToFarmHouse(Game1.player);

            // Disable adoption again for today
            CanAdopt = false;

            // Exit the naming menu
            Game1.drawObjectDialogue($"Adopted {petName}.");
        }






        /*********************************
         ** H O R S E   A D O P T I O N **
         *********************************/

        /// <summary>Check to see if the player is attempting to interact with the wild horse</summary>
        internal void HorseCheck(object sender, ButtonPressedEventArgs e)
        {
            if (HorseInfo != null &&
                e.Button.Equals(SButton.MouseRight) &&
                HorseInfo.HorseInstance.withinPlayerThreshold(3))
            {
                if ((int)e.Cursor.Tile.X >= HorseInfo.HorseInstance.getLeftMostTileX().X && (int)e.Cursor.Tile.X <= HorseInfo.HorseInstance.getRightMostTileX().X &&
                    (int)e.Cursor.Tile.Y >= HorseInfo.HorseInstance.getTileY() - 1 && (int)e.Cursor.Tile.Y <= HorseInfo.HorseInstance.getTileY() + 1)
                {

                    Game1.activeClickableMenu = new ConfirmationDialog("This appears to be an escaped horse from a neighboring town. \n\nIt looks tired, but friendly. Will you adopt this horse?", (who) =>
                    {
                        if (Game1.activeClickableMenu is StardewValley.Menus.ConfirmationDialog cd)
                            cd.cancel();

                        Game1.activeClickableMenu = new NamingMenu(HorseNamer, "What will you name this horse?");
                    }, (who) =>
                    {
                        // Exit the naming menu
                        Game1.drawObjectDialogue($"You leave the creature to rest for now. It's got a big, bright world ahead of it.");
                    });
                }
            }
        }

        internal void AdoptHorse()
        {
            Game1.activeClickableMenu = new NamingMenu(HorseNamer, "What will you name this horse?");
        }


        internal void HorseNamer(string horseName)
        {
            // Name Horse and add to Animal Skinner database
            HorseInfo.HorseInstance.Name = horseName;
            HorseInfo.HorseInstance.displayName = horseName;
            Earth.AddCreature(HorseInfo.HorseInstance, HorseInfo.SkinID);

            // Horse is no longer a WildHorse to keep track of
            HorseInfo = null;

            // Exit the naming menu
            Game1.drawObjectDialogue($"Adopted {horseName}.");
        }
    }
}
