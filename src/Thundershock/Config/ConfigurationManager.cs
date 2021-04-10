﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Thundershock.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Config
{
    public class ConfigurationManager : GlobalComponent
    {
        private FileSystem _fs;
        private GameConfiguration _gameConfig = null;

        public event EventHandler ConfigurationLoaded;
        
        public GameConfiguration ActiveConfig => _gameConfig;
        
        public DisplayMode GetDisplayMode()
        {
            if (ParseDisplayMode(_gameConfig.Resolution, out int w, out int h))
            {
                var supported =
                    GraphicsAdapter.DefaultAdapter.SupportedDisplayModes.FirstOrDefault(x =>
                        x.Width == w && x.Height == h);

                if (supported != null)
                    return supported;
            }

            return GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        }

        public void ResetToDefaults()
        {
            _gameConfig = new GameConfiguration();
            SaveConfiguration();
            ApplyChanges();
        }
        
        public void SetDisplayMode(string value)
        {
            if (ParseDisplayMode(value, out int w, out int h))
            {
                var supported =
                    GraphicsAdapter.DefaultAdapter.SupportedDisplayModes.FirstOrDefault(x =>
                        x.Width == w && x.Height == h);

                if (supported == null)
                    throw new InvalidOperationException(
                        $"\"{value}\" is not a display mode that is supportred by the current video card (\"{GraphicsAdapter.DefaultAdapter.Description}\").");

                _gameConfig.Resolution = $"{w}x{h}";
            }
            else
            {
                throw new InvalidOperationException(
                    $"\"{value}\" is not a properly-formatted display mode string. Must be <width>x<height>, e.x: 1920x1080");
            }
        }
        
        public void ApplyChanges()
        {
            ConfigurationLoaded?.Invoke(this, EventArgs.Empty);
            SaveConfiguration();
        }

        public void DiscardChanges()
        {
            LoadInitialConfig();
        }
        
        protected override void OnLoad()
        {
            // Create the local data path if it does not already exist.
            if (!Directory.Exists(ThundershockPlatform.LocalDataPath))
                Directory.CreateDirectory(ThundershockPlatform.LocalDataPath);

            // Initialize a virtual file system for that path.
            _fs = FileSystem.FromHostDirectory(ThundershockPlatform.LocalDataPath);
            
            // Load the initial configuration.
            LoadInitialConfig();
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            
            // Save the configuration.
            SaveConfiguration();
        }

        private bool ParseDisplayMode(string displayMode, out int width, out int height)
        {
            var result = false;

            width = 0;
            height = 0;

            if (!string.IsNullOrWhiteSpace(displayMode))
            {
                var lowercase = displayMode.ToLower();

                var x = 'x';

                if (lowercase.Contains(x))
                {
                    var index = lowercase.IndexOf(x);

                    var wString = lowercase.Substring(0, index);
                    var hString = lowercase.Substring(index + 1);

                    if (int.TryParse(wString, out width) && int.TryParse(hString, out height))
                    {
                        result = true;
                    }
                }
            }
            
            return result;
        }
        
        private void SaveConfiguration()
        {
            var json = JsonSerializer.Serialize(_gameConfig, typeof(GameConfiguration), new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            });
            _fs.WriteAllText("/config.json", json);
        }
        
        private void LoadInitialConfig()
        {
            _gameConfig = null;

            if (_fs.FileExists("/config.json"))
            {
                var json = _fs.ReadAllText("/config.json");
                _gameConfig = JsonSerializer.Deserialize<GameConfiguration>(json, new JsonSerializerOptions
                {
                    IncludeFields = true
                });
            }
            else
            {
                _gameConfig = new GameConfiguration();
            }

            ConfigurationLoaded?.Invoke(this, EventArgs.Empty);
        }
    }
}