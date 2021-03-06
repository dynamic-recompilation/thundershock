using System;
using System.Runtime.InteropServices;
using System.Text;
using Thundershock.Core;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;
using Thundershock.Core.Audio;

using Silk.NET.OpenGL;
using Thundershock.Core.Debugging;

namespace Thundershock.OpenGL
{
    public sealed class SdlGameWindow : GameWindow
    {
        private int _wheelX;
        private int _wheelY;
        private IntPtr _sdlWindow;
        private IntPtr _glContext;
        private Sdl.SdlEvent _event;
        
        protected override void OnUpdate()
        {
            PollEvents();

            // Swap the OpenGL buffers so we can see what was just rendered by
            // Thundershock.
            Sdl.SDL_GL_SwapWindow(_sdlWindow);
        }

        protected override void Initialize()
        {
            CreateSdlWindow();
        }

        private void SetupGlRenderer()
        {
            // Set up the OpenGL context attributes.
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlContextMajorVersion, 4);
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlContextMinorVersion, 5);
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlContextProfileMask,
                Sdl.SdlGLprofile.SdlGlContextProfileCore);
            
            Logger.Log("Setting up the SDL OpenGL renderer...");
            var ctx = Sdl.SDL_GL_CreateContext(_sdlWindow);
            if (ctx == IntPtr.Zero)
            {
                var err = Sdl.SDL_GetError();
                Logger.Log(err, LogLevel.Error);
                throw new Exception(err);
            }

            _glContext = ctx;
            Logger.Log("GL Context created.");

            // Make the newly created context the current one.
            Sdl.SDL_GL_MakeCurrent(_sdlWindow, _glContext);
        }
        
        private void PollEvents()
        {
            while (Sdl.SDL_PollEvent(out _event) != 0)
            {
                HandleSdlEvent();
            }
        }

        protected override void OnClosed()
        {
            DestroySdlWindow();
            Logger.Log("...done.");
        }

        private void HandleSdlEvent()
        {
            switch (_event.type)
            {
                case Sdl.SdlEventType.SdlWindowevent:
                    if (_event.window.windowEvent == Sdl.SdlWindowEventId.SdlWindoweventResized)
                    {
                        ReportClientSize(_event.window.data1, _event.window.data2);
                    }

                    break;
                case Sdl.SdlEventType.SdlQuit:

                    Logger.Log("SDL just told us to quit... Letting thundershock know about that.");
                    if (!App.Exit())
                    {
                        Logger.Log("Thundershock app cancelled the exit request.");
                    }

                    break;
                case Sdl.SdlEventType.SdlKeydown:
                case Sdl.SdlEventType.SdlKeyup:
                    var key = (Keys) _event.key.keysym.sym;
                    var repeat = _event.key.repeat != 0;
                    var isPressed = _event.key.state == Sdl.SdlPressed;

                    // Dispatch the event to thundershock.
                    DispatchKeyEvent(key, '\0', isPressed, repeat, false);
                    break;
                case Sdl.SdlEventType.SdlMousebuttondown:
                case Sdl.SdlEventType.SdlMousebuttonup:

                    var button = MapSdlMouseButton(_event.button.button);
                    var state = _event.button.state == Sdl.SdlPressed ? ButtonState.Pressed : ButtonState.Released;

                    DispatchMouseButton(button, state);
                    break;

                case Sdl.SdlEventType.SdlMousewheel:

                    var xDelta = _event.wheel.x * 16;
                    var yDelta = _event.wheel.y * 16;

                    if (_event.wheel.direction == (uint) Sdl.SdlMouseWheelDirection.SdlMousewheelFlipped)
                    {
                        xDelta = xDelta * -1;
                        yDelta = yDelta * -1;
                    }

                    if (yDelta != 0)
                    {
                        _wheelY += yDelta;
                        ReportMouseScroll(_wheelY, yDelta, ScrollDirection.Vertical);
                    }

                    if (xDelta != 0)
                    {
                        _wheelX += xDelta;
                        ReportMouseScroll(_wheelX, xDelta, ScrollDirection.Horizontal);
                    }

                    break;
                case Sdl.SdlEventType.SdlTextinput:
                    string text;

                    unsafe
                    {
                        var count = Sdl.SdlTextinputeventTextSize;
                        var end = 0;
                        while (end < count && _event.text.text[end] > 0)
                            end++;

                        fixed (byte* bytes = _event.text.text)
                        {
                            var span = new ReadOnlySpan<byte>(bytes, end);
                            text = Encoding.UTF8.GetString(span);
                        }
                    }

                    foreach (var character in text)
                    {
                        var ckey = (Keys) character;
                        DispatchKeyEvent(ckey, character, false, false, true);
                    }

                    break;
                case Sdl.SdlEventType.SdlMousemotion:
                    ReportMousePosition(_event.motion.x, _event.motion.y);
                    break;
            }
        }

        private MouseButton MapSdlMouseButton(uint button)
        {
            return button switch
            {
                Sdl.SdlButtonLeft => MouseButton.Primary,
                Sdl.SdlButtonRight => MouseButton.Secondary,
                Sdl.SdlButtonMiddle => MouseButton.Middle,
                Sdl.SdlButtonX1 => MouseButton.BrowserForward,
                Sdl.SdlButtonX2 => MouseButton.BrowserBack,
                _ => throw new NotSupportedException()
            };
        }
        
        protected override void OnWindowTitleChanged()
        {
            Sdl.SDL_SetWindowTitle(_sdlWindow, Title);
        }

        private uint GetWindowModeFlags()
        {
            var flags = 0x00u;

            if (IsBorderless)
            {
                flags |= (uint) Sdl.SdlWindowFlags.SdlWindowBorderless;
            }

            if (IsFullScreen)
            {
                if (IsBorderless)
                    flags |= (uint) Sdl.SdlWindowFlags.SdlWindowFullscreenDesktop;
                else
                    flags |= (uint) Sdl.SdlWindowFlags.SdlWindowFullscreen;
            }

            return flags;
        }

        protected override void OnWindowModeChanged()
        {
            // DestroySdlWindow();
            // CreateSdlWindow();

            var fsFlags = 0u;
            if (IsBorderless && IsFullScreen)
                fsFlags |= (uint) Sdl.SdlWindowFlags.SdlWindowFullscreenDesktop;
            else if (IsFullScreen)
                fsFlags |= (uint) Sdl.SdlWindowFlags.SdlWindowFullscreen;
            
            Sdl.SDL_SetWindowResizable(_sdlWindow, CanResize ? Sdl.SdlBool.SdlTrue : Sdl.SdlBool.SdlFalse);
            Sdl.SDL_SetWindowBordered(_sdlWindow, IsBorderless ? Sdl.SdlBool.SdlFalse : Sdl.SdlBool.SdlTrue);
            Sdl.SDL_SetWindowFullscreen(_sdlWindow, fsFlags);

            if (fsFlags > 0)
            {
                Sdl.SDL_SetWindowPosition(_sdlWindow, Sdl.SdlWindowposCentered, Sdl.SdlWindowposCentered);
            }

            
            base.OnWindowModeChanged();
        }

        protected override void OnClientSizeChanged()
        {
            // Resize the SDL window.
            Sdl.SDL_SetWindowSize(_sdlWindow, Width, Height);
            Sdl.SDL_SetWindowPosition(_sdlWindow, Sdl.SdlWindowposCentered, Sdl.SdlWindowposCentered);

            ReportClientSize(Width, Height);
            base.OnClientSizeChanged();
        }

        private void CreateSdlWindow()
        {
            // Housekeeping?
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlRedSize, 8);
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlGreenSize, 8);
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlBlueSize, 8);
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlDepthSize, 24);
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlStencilSize, 8);
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlDoublebuffer, 1);

            var flags = GetWindowModeFlags();
            flags |= (uint) Sdl.SdlWindowFlags.SdlWindowOpengl;
            flags |= (uint) Sdl.SdlWindowFlags.SdlWindowShown;
            
            Logger.Log("Creating an SDL Window...");
            _sdlWindow = Sdl.SDL_CreateWindow(Title, Sdl.SDL_WINDOWPOS_CENTERED_DISPLAY(0), Sdl.SDL_WINDOWPOS_CENTERED_DISPLAY(0), Width, Height,
                (Sdl.SdlWindowFlags) flags);
            Logger.Log("SDL window is up. (640x480, SDL_WINDOW_SHOWN | SDL_WINDOW_OPENGL)");

            Sdl.SDL_SetWindowResizable(_sdlWindow, CanResize ? Sdl.SdlBool.SdlTrue : Sdl.SdlBool.SdlFalse);
            
            SetupGlRenderer();
        }

        private void DestroySdlWindow()
        {
            Logger.Log("Destroying current GL renderer...");
            Sdl.SDL_GL_DeleteContext(_glContext);
            _glContext = IntPtr.Zero;
            
            Logger.Log("Destroying the SDL window...");
            Sdl.SDL_DestroyWindow(_sdlWindow);
        }

        protected override void UpdateVSync()
        {
            Logger.Log("V-Sync status: " + (VSync ? "On" : "Off"));

            Sdl.SDL_GL_SetSwapInterval(VSync ? 1 : 0);
        }
    }
}