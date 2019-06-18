using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Characters;

namespace AnimalSkinner.Framework
{

    class PotentialPet
    {
        /// <summary>RNG for selecting randomized aspects</summary>
        private readonly Random Randomizer = new Random();

        internal string PetType;
        internal int SkinID;



        /// <summary>Creates a new PotentialPet</summary>
        internal PotentialPet()
        {
            PetType = ModEntry.PetAssets.Keys.ToList()[Randomizer.Next(0, ModEntry.PetAssets.Count)];
            SkinID = Randomizer.Next(1, ModEntry.PetAssets[PetType].Count + 1);
        }


        /// <summary>Creates a new Pet from the PotentialPet's information</summary>
        internal Pet CreatePet(string name)
        {
            Pet pet = (Pet)Activator.CreateInstance(ModEntry.PetTypeMap[PetType], (int)Game1.player.position.X, (int)Game1.player.position.Y);
            pet.Name = name;
            pet.displayName = name;
            pet.Sprite = new AnimatedSprite(ModEntry.GetSkinFromID(PetType, SkinID).AssetKey, 28, 32, 32);

            return pet;
        }
    }
}
