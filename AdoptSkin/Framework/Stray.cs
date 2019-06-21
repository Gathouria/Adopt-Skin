using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Characters;

namespace AdoptSkin.Framework
{

    class Stray
    {
        /// <summary>RNG for selecting randomized aspects</summary>
        private readonly Random Randomizer = new Random();

        /// <summary>The GameLocation for Marnie's house</summary>
        internal static readonly GameLocation Marnies = Game1.getLocationFromName("AnimalShop");
        /// <summary>Warp location for a potential pet at the beginning of the day</summary>
        internal static Vector2 CreationLocation = new Vector2(11, 16);

        /// <summary>The identifying number for a potential pet</summary>
        internal static readonly int StrayID = 8000;

        internal Pet PetInstance;
        internal int SkinID;


        /// <summary>Creates a new Stray</summary>
        internal Stray()
        {
            string type = ModEntry.PetAssets.Keys.ToList()[Randomizer.Next(0, ModEntry.PetAssets.Count)];
            SkinID = Randomizer.Next(1, ModEntry.PetAssets[type].Count + 1);

            PetInstance = (Pet)Activator.CreateInstance(ModEntry.PetTypeMap[type], (int)CreationLocation.X, (int)CreationLocation.Y);
            PetInstance.Sprite = new AnimatedSprite(ModEntry.GetSkinFromID(type, SkinID).AssetKey, 28, 32, 32);
            PetInstance.Manners = StrayID;

            PetInstance.Name = "Stray";
            PetInstance.displayName = "Stray";

            Marnies.addCharacter(PetInstance);
            Game1.warpCharacter(PetInstance, Marnies, CreationLocation);
        }


        /// <summary>Remove this Stray's Pet instance from its map</summary>
        internal void RemoveFromWorld()
        {
            Game1.removeThisCharacterFromAllLocations(this.PetInstance);
        }
    }
}
