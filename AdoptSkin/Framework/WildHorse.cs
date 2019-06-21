using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace AdoptSkin.Framework
{
    class WildHorse
    {

        /// <summary>Outdoor locations in which wild horses can spawn</summary>
        internal static List<string> SpawningMaps = new List<string>
        {
            "Forest", "BusStop", "Mountain", "Town", "Railroad", "Beach"
        };
        /// <summary>RNG for selecting randomized aspects</summary>
        private readonly Random Randomizer = new Random();
        /// <summary>The identifying number for a wild horse</summary>
        internal static readonly int WildID = 3000;

        internal Horse HorseInstance;
        public int SkinID { get; }
        public GameLocation Map { get; }
        public Vector2 Tile { get; }



        /// <summary>Creates a new WildHorse for placement on the map.</summary>
        internal WildHorse()
        {
            SkinID  = Randomizer.Next(1, ModEntry.HorseAssets["horse"].Count + 1);

            string mapName = SpawningMaps[Randomizer.Next(0, SpawningMaps.Count)];
            Map = Game1.getLocationFromName(mapName);
            Tile = GetRandomSpawnLocation(Map);

            HorseInstance = new Horse(new Guid(), (int)Tile.X, (int)Tile.Y);
            HorseInstance.Sprite = new AnimatedSprite(ModEntry.GetSkinFromID("horse", SkinID).AssetKey, 7, 32, 32);
            HorseInstance.Manners = WildID;

            HorseInstance.Name = "Wild horse";
            HorseInstance.displayName = "Wild horse";

            Map.addCharacter(HorseInstance);
            Game1.warpCharacter(HorseInstance, Map, Tile);

            if (ModEntry.Config.DetailedConsoleOutput)
            {
                string message = $"A wild horse has been spotted at: {Map.Name} -- {Tile.X}, {Tile.Y}";
                ModEntry.SMonitor.Log(message, LogLevel.Debug);
                Game1.chatBox.addInfoMessage(message);
            }
        }


        /// <summary>Remove this WildHorse's Horse instance from its map</summary>
        internal void RemoveFromWorld()
        {
            Game1.removeThisCharacterFromAllLocations(this.HorseInstance);
        }


        /// <summary>Returns a tile from the given map, that is reasonably accessible, to spawn the WildHorse at</summary>
        internal static Vector2 GetRandomSpawnLocation(GameLocation map)
        {
            // Make sure the tile is reasonably accessible
            Vector2 randomTile = map.getRandomTile();
            while (map.isOpenWater(int.Parse(randomTile.X.ToString()), int.Parse(randomTile.Y.ToString())) ||
                map.isBehindTree(randomTile) ||
                map.isBehindBush(randomTile) ||
                map.isCollidingWithWarpOrDoor(new Rectangle((int)randomTile.X, (int)randomTile.Y, 1, 1)) != null ||
                !map.isTilePassable(new xTile.Dimensions.Location((int)randomTile.X, (int)randomTile.Y), Game1.viewport) ||
                !map.isTileLocationTotallyClearAndPlaceableIgnoreFloors(randomTile) ||
                !map.isTileLocationTotallyClearAndPlaceableIgnoreFloors(new Vector2(randomTile.X + 1, randomTile.Y)))
            {
                randomTile = map.getRandomTile();
            }

            return randomTile;
        }
    }
}
