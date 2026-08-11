using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.Model
{
    public class GameSettings
    {
        public static readonly GameSettings Defaults = new();

        // supposedly breeding time is a constant 5 minutes
        public TimeSpan BreedingTime { get; set; } = TimeSpan.FromMinutes(5);

        // actual breeding time can be extended if the parents are idle and not actually at
        // the breeding farm. assume parents are never idle except for sleep/night time, and
        // assume equal time for day+night, meaning the parents are actively breeding for
        // half the time of each day and the number of total eggs produced is cut in half
        //
        // (i.e., average effective breeding time is doubled)
        [JsonIgnore]
        public TimeSpan AvgBreedingTime => BreedingTime * 2;

        public bool MultipleBreedingFarms { get; set; } = false;

        // highest level you're willing to raise a pal to; a pal can only know (and therefore pass down)
        // active skills it learns at or below this level
        public int MaxPalLevel { get; set; } = GameConstants.MaxPalLevel;

        // when set, an owned pal is limited to the skills it knows at its current level instead of
        // assuming it gets raised to MaxPalLevel
        public bool UseCurrentPalLevels { get; set; } = false;

        // pals which still have to be bred or caught arrive unleveled, so UseCurrentPalLevels leaves
        // them with only the skills they know on arrival
        [JsonIgnore]
        public int NewPalSkillLevel => UseCurrentPalLevels ? 1 : MaxPalLevel;

        // base time for incubating a massive egg without any e.g. temperature buffs
        public TimeSpan MassiveEggIncubationTime { get; set; } = TimeSpan.FromHours(2);

        public bool MultipleIncubators { get; set; } = true;

        public int PlayerPartySize { get; set; } = 5;

        public Dictionary<LocationType, int> LocationTypeGridWidths { get; set; } = new()
        {
            { LocationType.PlayerParty, 5 },
            { LocationType.Palbox, 6 },
            { LocationType.Base, 5 },
            { LocationType.ViewingCage, 6 },
            { LocationType.DimensionalPalStorage, 6 },
            { LocationType.GlobalPalStorage, 6 },

            // (not real, just so it's defined)
            { LocationType.Custom, 8 },
        };

        public Dictionary<LocationType, int?> LocationTypeGridHeights { get; set; } = new()
        {
            { LocationType.Palbox, 5 },
            { LocationType.DimensionalPalStorage, 5 },
            { LocationType.GlobalPalStorage, 5 },

            { LocationType.PlayerParty, null },
            { LocationType.Base, null },
            { LocationType.ViewingCage, null },
            { LocationType.Custom, null },
        };
    }
}
