﻿namespace TrafficManager.UI.SubTools.SpeedLimits {
    using System.Collections.Generic;
    using TrafficManager.API.Traffic.Data;
    using TrafficManager.State;
    using TrafficManager.UI.Textures;

    /// <summary>Produces list of speed limits to offer to user in the palette.</summary>
    public static class PaletteGenerator {
        /// <summary>Produces list of speed limits to offer user in the palette.</summary>
        /// <param name="unit">What kind of speed limit list is required.</param>
        /// <returns>
        ///     List from smallest to largest speed with the given unit. Zero (no limit) is
        ///     not added to the list. The values are in-game speeds as float.
        /// </returns>
        public static List<SetSpeedLimitAction> AllSpeedLimits(SpeedUnit unit) {
            var result = new List<SetSpeedLimitAction>();

            // SpeedLimitTextures textures = TMPELifecycle.Instance.Textures.SpeedLimits;

            switch (unit) {
                case SpeedUnit.Kmph:
                    for (var km = RoadSignThemes.KMPH_STEP;
                         km <= RoadSignThemes.UPPER_KMPH;
                         km += RoadSignThemes.KMPH_STEP) {
                        result.Add(SetSpeedLimitAction.SetOverride(SpeedValue.FromKmph(km)));
                    }

                    break;
                case SpeedUnit.Mph:
                    for(var mi = RoadSignThemes.MPH_STEP;
                        mi <= RoadSignThemes.UPPER_MPH;
                        mi += RoadSignThemes.MPH_STEP) {
                        result.Add(SetSpeedLimitAction.SetOverride(SpeedValue.FromMph(mi)));
                    }

                    break;
                case SpeedUnit.CurrentlyConfigured:
                    // Automatically choose from the config
                    return AllSpeedLimits(GlobalConfig.Instance.Main.GetDisplaySpeedUnit());
            }

            return result;
        }
    }
}