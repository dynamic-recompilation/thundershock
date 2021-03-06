using System;
using Thundershock.Core.Audio;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;

namespace Thundershock.Core
{
    public abstract class GameWindow
    {
        private int _mouseX;
        private int _mouseY;
        private Application _app;
        private string _windowTitle = "Thundershock Engine";
        private bool _canResize = true;
        private bool _borderless;
        private bool _fullscreen;
        private int _width = 640;
        private int _height = 480;
        private bool _vsync;
        
        public Application App => _app;
        
        /// <summary>
        /// Gets or sets a value indicating whether the primary mouse button is the right mouse button.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If <see cref="get_PrimaryMouseButtonIsRightMouseButton"/> returns true then the
        ///         engine will swap the Primary and Secondary mouse buttons before dispatching
        ///         MouseDown or MouseUp events.
        ///     </para>
        ///     <para>
        ///         This property allows the engine's configuration system to honor the user's preferred
        ///         primary mouse button setting. For Thundershock apps that don't use the configuration system
        ///         they can set the option through command-line or through the GraphicalAppBase class.
        ///     </para>
        ///     <para>
        ///         If you are deriving from GameWindow to add extra platform support, you do not need to
        ///         set this property nor do you need to read it. Simply dispatch mouse button events like
        ///         normal and GameWindow will deal with the swapping for you.
        ///     </para>
        /// </remarks>
        public bool PrimaryMouseButtonIsRightMouseButton { get; set; }

        public bool VSync
        {
            get => _vsync;
            set
            {
                if (_vsync != value)
                {
                    _vsync = value;
                    if (_app != null)
                        UpdateVSync();
                }
            }
        }
        
        public string Title
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    if (_app != null)
                        OnWindowTitleChanged();
                }
            }
        }
        
        public bool CanResize
        {
            get => _canResize;
            set
            {
                if (_canResize != value)
                {
                    _canResize = value;
                    if (_app != null)
                        OnWindowModeChanged();
                }
            }
        }


        public bool IsBorderless
        {
            get => _borderless;
            set
            {
                _borderless = value;
                if (_app != null)
                    OnWindowModeChanged();
            }
        }
        
        public bool IsFullScreen
        {
            get => _fullscreen;
            set
            {
                _fullscreen = value;
                if (_app != null)
                    OnWindowModeChanged();
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                if (value <= 0)
                    throw new InvalidOperationException("Window size must be greater than zero.");

                _width = value;

                if (_app != null)
                {
                    OnClientSizeChanged();
                }
            }
        }
        
        public int Height
        {
            get => _height;
            set
            {
                if (value <= 0)
                    throw new InvalidOperationException("Window size must be greater than zero.");

                _height = value;

                if (_app != null)
                {
                    OnClientSizeChanged();
                }
            }
        }

        
        public void Show(Application app)
        {
            if (_app != null)
                throw new InvalidOperationException("Window is already open.");

            _app = app ?? throw new ArgumentNullException(nameof(app));

            Initialize();
            UpdateVSync();
        }

        public void Update()
        {
            if (_app == null)
                throw new InvalidOperationException("Window is not open.");

            OnUpdate();
        }

        public void Close()
        {
            if (_app == null)
                throw new InvalidOperationException("Window already closed...");

            OnClosed();
        }

        protected abstract void OnClosed();
        protected abstract void OnUpdate();
        protected abstract void Initialize();
        
        protected virtual void OnClientSizeChanged() {}
        protected virtual void OnWindowTitleChanged() {}
        protected virtual void OnWindowModeChanged() {}

        protected virtual void DispatchMouseButton(MouseButton button, ButtonState state)
        {
            if (PrimaryMouseButtonIsRightMouseButton)
            {
                if (button == MouseButton.Primary)
                    button = MouseButton.Secondary;
                else if (button == MouseButton.Secondary)
                    button = MouseButton.Primary;
            }
            
            var evt = new MouseButtonEventArgs(_mouseX, _mouseY, button, state);
            if (evt.IsPressed)
            {
                MouseDown?.Invoke(this, evt);
            }
            else
            {
                MouseUp?.Invoke(this, evt);
            }
        }
        
        protected void ReportMousePosition(int x, int y)
        {
            var deltaX = x - _mouseX;
            var deltaY = y - _mouseY;

            if (deltaX != 0 || deltaY != 0)
            {
                var evt = new MouseMoveEventArgs(x, y, deltaX, deltaY);
                MouseMove?.Invoke(this, evt);
            }

            _mouseX = x;
            _mouseY = y;
        }

        protected void ReportMouseScroll(int wheel, int delta, ScrollDirection direction)
        {
            var evt = new MouseScrollEventArgs(_mouseX, _mouseY, wheel, delta, direction);

            MouseScroll?.Invoke(this, evt);
        }

        protected void ReportClientSize(int width, int height)
        {
            _width = width;
            _height = height;
        }
        
        protected void DispatchKeyEvent(Keys key, char character, bool isPressed, bool isRepeated, bool isText)
        {
            var evt = null as KeyEventArgs;

            if (isText)
            {
                evt = new KeyCharEventArgs(key, character);
                KeyChar?.Invoke(this, evt as KeyCharEventArgs);
            }
            else
            {
                evt = new KeyEventArgs(key);

                if (isPressed)
                {
                    KeyDown?.Invoke(this, evt);
                }
                else
                {
                    KeyUp?.Invoke(this, evt);
                }
            }
        }

        public event EventHandler<MouseScrollEventArgs> MouseScroll;
        public event EventHandler<MouseMoveEventArgs> MouseMove; 
        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;
        public event EventHandler<KeyCharEventArgs> KeyChar;
        public event EventHandler<MouseButtonEventArgs> MouseDown;
        public event EventHandler<MouseButtonEventArgs> MouseUp;
        
        protected virtual void UpdateVSync() {}
    }
}