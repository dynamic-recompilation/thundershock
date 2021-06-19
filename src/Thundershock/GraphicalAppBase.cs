using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Thundershock.Config;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock
{
    public abstract class GraphicalAppBase : AppBase
    {
        private bool _borderless = false;
        private bool _fullscreen = false;
        private int _width;
        private int _height;
        private GameWindow _gameWindow;
        private bool _aboutToExit = false;
        private Stopwatch _frameTimer = new();
        private TimeSpan _totalGameTime;
        private GameLoop _game;
        
        public bool SwapMouseButtons
        {
            get => _gameWindow.PrimaryMouseButtonIsRightMouseButton;
            set => _gameWindow.PrimaryMouseButtonIsRightMouseButton = value;
        }
        
        public bool IsBorderless
        {
            get => _borderless;
            protected set => _borderless = value;
        }

        public bool IsFullScreen
        {
            get => _fullscreen;
            protected set => _fullscreen = value;
        }

        public int ScreenWidth => _width;
        public int ScreenHeight => _height;
        
        protected sealed override void Bootstrap()
        {
            Logger.Log("Creating the game window...");
            _gameWindow = CreateGameWindow();
            _gameWindow.Show(this);
            Logger.Log("Game window created.");

            PreInit();
            
            RunLoop();

            Logger.Log("RunLoop just returned. That means we're about to die.");
            _gameWindow.Close();
            _gameWindow = null;
            Logger.Log("Game window destroyed.");
        }

        private void RunLoop()
        {
            Init();
            PostInit();
            
            while (!_aboutToExit)
            {
                var gameTime = _frameTimer.Elapsed;
                var frameTime = gameTime - _totalGameTime;

                var gameTimeInfo = new GameTime(frameTime, gameTime);
                
                _gameWindow.GraphicsProcessor.Clear(Color.Black);

                _game.Update(gameTimeInfo);
                _game.Render(gameTimeInfo);
                
                _gameWindow.Update();
                
                _totalGameTime = gameTime;
            }
        }

        protected override void BeforeExit(AppExitEventArgs args)
        {
            // call the base method to dispatch the event to the rest of the engine.
            base.BeforeExit(args);
            
            // Terminate the game loop if args.Cancelled isn't set
            if (!args.CancelExit)
            {
                _aboutToExit = true;
            }
        }

        private void PreInit()
        {
            Logger.Log("PreInit reached. Setting up core components.");
            RegisterComponent<ConfigurationManager>();

            OnPreInit();
        }

        private void Init()
        {
            _game = new GameLoop(this, _gameWindow);
            
            OnInit();
        }

        private void PostInit()
        {
            OnPostInit();

            _frameTimer.Start();
        }

        protected void ApplyGraphicsChanges()
        {
            _gameWindow.IsBorderless = _borderless;
            _gameWindow.IsFullScreen = _fullscreen;
            
            // TODO: V-Sync, Fixed Time Stepping, Monitor Positioning
            _gameWindow.Width = _width;
            _gameWindow.Height = _height;
        }

        protected void SetScreenSize(int width, int height, bool apply = false)
        {
            _width = width;
            _height = height;
            
            if (apply) ApplyGraphicsChanges();
        }
        
        protected virtual void OnPreInit() {}
        protected virtual void OnInit() {}
        protected virtual void OnPostInit() {}


        protected abstract GameWindow CreateGameWindow();

        protected void LoadScene<T>() where T : Scene, new()
        {
            _game.LoadScene<T>();
        }
    }
}