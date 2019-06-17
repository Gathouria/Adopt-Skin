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
    class WildHorse
    {

        /// <summary>Outdoor locations in which wild horses can spawn</summary>
        internal static List<string> SpawningMaps = new List<string>
        {
            "forest", "busstop", "mountain", "town", "railroad", "beach"
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
            string type = ModEntry.HorseAssets.Keys.ToList()[Randomizer.Next(0, ModEntry.HorseAssets.Count)];
            SkinID  = Randomizer.Next(1, ModEntry.HorseAssets[type].Count + 1);

            string mapName = SpawningMaps[Randomizer.Next(0, SpawningMaps.Count)];
            Map = GetMapFromName(mapName);
            Tile = GetRandomSpawnLocation(Map);

            HorseInstance = new Horse(new Guid(), (int)Tile.X, (int)Tile.Y);
            HorseInstance.Sprite = new AnimatedSprite(ModEntry.GetSkinFromID(ModEntry.Sanitize(HorseInstance.GetType().Name), SkinID).AssetKey, 7, 32, 32);
            HorseInstance.Manners = WildID;

            HorseInstance.Name = "Wild horse";
            HorseInstance.displayName = "Wild horse";

            Map.addCharacter(HorseInstance);
            Game1.warpCharacter(HorseInstance, Map, Tile);

            ModEntry.SMonitor.Log($"A wild horse has been spotted at: {Map.Name} -- {Tile.X}, {Tile.Y}", LogLevel.Debug);
        }


        /// <summary>
        /// Creates a new WildHorse using the format that save file data is loaded in, looking on the map to find its associated Horse instance
        /// The list should contain, in order and in string format, the SkinID, Map name, Tile.X, and Tile.Y
        /// </summary>
        internal WildHorse(List<string> info)
        {
            SkinID = int.Parse(info[0]);
            Map = GetMapFromName(info[1]);
            Tile = new Vector2(int.Parse(info[2]), int.Parse(info[3]));

            // Find the WildHorse on the map
            foreach (Horse horse in ModEntry.GetHorses())
            {
                if (horse.Manners == WildID)
                {
                    HorseInstance = horse;
                    horse.Sprite = new AnimatedSprite(ModEntry.GetSkinFromID(ModEntry.Sanitize(HorseInstance.GetType().Name), SkinID).AssetKey, 7, 32, 32);
                    horse.Manners = WildID;

                    HorseInstance.Name = "Wild horse";
                    HorseInstance.displayName = "Wild horse";

                    Game1.removeThisCharacterFromAllLocations(horse);
                    Map.addCharacter(horse);
                    Game1.warpCharacter(horse, Map, Tile);

                    ModEntry.SMonitor.Log($"A wild horse was seen at: {Map.Name} -- {Tile.X}, {Tile.Y}", LogLevel.Debug);

                    break;
                }
            }

            
        }


        /// <summary>Remove this WildHorse's Horse instance from the map</summary>
        internal void Remove()
        {
            Game1.removeThisCharacterFromAllLocations(this.HorseInstance);
        }


        /// <summary>Returns the GameLocation associated with the given GameLocation.Name</summary>
        internal GameLocation GetMapFromName(string name)
        {
            foreach (GameLocation loc in Game1.locations)
            {
                if (loc.Name.ToLower() == name.ToLower())
                {
                    return loc;
                }
            }
            return null;
        }


        /// <summary>Returns a tile from the given map, that is reasonably accessible, to spawn the WildHorse at</summary>
        internal Vector2 GetRandomSpawnLocation(GameLocation map)
        {
            // Make sure the tile is reasonably accessible
            Vector2 randomTile = map.getRandomTile();
            while (map.isOpenWater(int.Parse(randomTile.X.ToString()), int.Parse(randomTile.Y.ToString())) ||
                map.isBehindTree(randomTile) ||
                map.isCollidingWithWarpOrDoor(new Rectangle((int)randomTile.X, (int)randomTile.Y, 1, 1)) != null ||
                !map.isTileLocationTotallyClearAndPlaceableIgnoreFloors(randomTile) ||
                !map.isTileLocationTotallyClearAndPlaceableIgnoreFloors(new Vector2(randomTile.X + 1, randomTile.Y)))
            {
                randomTile = map.getRandomTile();
            }

            return randomTile;
        }




        /* 
         internal KeyValuePair<GameLocation, Vector2> GetRandomSpawnLocation()
        {
            // Select random spawning location map
            string locationName = SpawningMaps[CreatureRandom.Next(0, SpawningMaps.Count)];
            GameLocation randomLocation = null;
            foreach (GameLocation loc in Game1.locations)
            {
                if (loc.Name.ToLower() == locationName)
                    randomLocation = loc;
            }
            Earth.Monitor.Log($"Map: {randomLocation}", LogLevel.Info);

            // Make sure the tile is reasonably accessible
            Vector2 randomTile = randomLocation.getRandomTile();
            while (randomLocation.isOpenWater(int.Parse(randomTile.X.ToString()), int.Parse(randomTile.Y.ToString())) ||
                randomLocation.isBehindTree(randomTile) ||
                randomLocation.isCollidingWithWarpOrDoor(new Rectangle((int)randomTile.X, (int)randomTile.Y, 1, 1)) != null ||
                randomLocation.isObjectAt((int)randomTile.X, (int)randomTile.Y) ||
                !randomLocation.isTileLocationTotallyClearAndPlaceableIgnoreFloors(randomTile))
            {
                randomTile = randomLocation.getRandomTile();
            }

            return new KeyValuePair<GameLocation, Vector2>(randomLocation, randomTile);
        }

         internal void SummonAHorse()
        {
            string horseType = ModEntry.HorseAssets.Keys.ToArray()[CreatureRandom.Next(0, ModEntry.HorseAssets.Keys.Count)];
            int horseSkin = CreatureRandom.Next(1, ModEntry.HorseAssets[horseType].Count + 1);

            SummonedHorse = new Horse(new Guid(), 70, 20);//(Horse)Activator.CreateInstance(ModEntry.HorseTypeMap[horseType], new Guid(), (int)Game1.player.position.X, (int)Game1.player.position.Y);
            SummonedHorse.Sprite = new AnimatedSprite(ModEntry.GetSkinFromID(horseType, horseSkin).AssetKey, 0, 32, 32);
            SummonedHorseSkin = horseSkin;

            // Spawn wild horse at a random location
            KeyValuePair<GameLocation, Vector2> spawnLocation = GetRandomSpawnLocation();
            spawnLocation.Key.addCharacter(SummonedHorse);
            Game1.warpCharacter(SummonedHorse, spawnLocation.Key, spawnLocation.Value);

            Earth.Monitor.Log($"Horse summoned to: {SummonedHorse.currentLocation} -- {SummonedHorse.getTileX()}, {SummonedHorse.getTileY()}", LogLevel.Info);

            ModEntry.SHelper.Events.Input.ButtonReleased += this.CheckIfHorseClicked;
        }

        internal void CheckIfHorseClicked(object sender, ButtonReleasedEventArgs e)
        {

            Vector2 horseLocation = SummonedHorse.getTileLocation();
            if (e.Cursor.Tile.X >= horseLocation.X && e.Cursor.Tile.X <= (horseLocation.X + 2) && e.Cursor.Tile.Y >= horseLocation.Y && e.Cursor.Tile.Y <= horseLocation.Y + 2)
            {
                Game1.activeClickableMenu = new ConfirmationDialog("This appears to be an escaped horse from a neighboring town. They look tired, but friendly. Will you adopt this horse?", (who) =>
                {
                    if (Game1.activeClickableMenu is StardewValley.Menus.ConfirmationDialog cd)
                        cd.cancel();

                    Game1.activeClickableMenu = new NamingMenu(NameWildHorse, "What will you name this horse?");
                });
                
            }
        }
        */
    }
}
