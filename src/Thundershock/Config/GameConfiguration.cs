﻿namespace Thundershock.Config
{
    public class GameConfiguration
    {
        public bool IsFullscreen = true;
        public bool VSync = true;
        public bool FixedTimeStepping = true;
        public bool SwapMouseButtons = false;
        
        public EffectsConfiguration Effects = new EffectsConfiguration();

        public string Resolution { get; set; } = string.Empty;
    }
}