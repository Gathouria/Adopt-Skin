using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdoptSkin
{
    class ModConfig
    {
        /// <summary>Determines whether wild adoptable horses can spawn in the map after the player obtains a stable</summary>
        public bool WildHorses { get; set; } = true;

        /// <summary>Determines whether stray pets will appear at Marnie's after the player obtains a pet</summary>
        public bool StrayAnimals { get; set; } = true;

        /// <summary>Whether or not to allow horses being ridden to fit through any area that the player can normally walk through</summary>
        public bool OneTileHorse { get; set; } = true;

        public string HorseWhistleKey { get; set; } = "R";

        /// <summary>Whether or not to display extra console output. Will be FALSE by default</summary>
        public bool DetailedConsoleOutput { get; set; } = true;

        /// <summary>Whether or not to allow debugging commands for Adopt & Skin. Will be FALSE by default</summary>
        public bool DebuggingMode { get; set; } = true;
    }
}
