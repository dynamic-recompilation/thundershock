using System;
using System.Diagnostics;
using Thundershock.Core;
using Thundershock.Core.Input;

namespace Thundershock
{
    public abstract class Layer
    {
        private GraphicalApplication _app;

        protected GraphicalApplication App => _app;
        
        public void Initialize(GraphicalApplication app)
        {
            Debug.Assert(_app == null, "Layer is already initialized.");
            _app = app ?? throw new ArgumentNullException(nameof(app));
            OnInit();
        }

        
        public void Unload()
        {
            Debug.Assert(_app != null, "Layer isn't initialized.");
            OnUnload();
            _app = null;
        }
        
        public void Update(GameTime gameTime)
        {
            Debug.Assert(_app != null, "Layer isn't initialized.");
            OnUpdate(gameTime);
        }

        public void Render(GameTime gameTime)
        {
            Debug.Assert(_app != null, "Layer isn't initialized.");
            OnRender(gameTime);
        }

        protected abstract void OnInit();
        protected abstract void OnUnload();

        protected abstract void OnUpdate(GameTime gameTime);
        
        protected virtual void OnRender(GameTime gameTime) {}

        public virtual bool KeyDown(KeyEventArgs e)
        {
            return false;
        }
        
        public virtual bool KeyUp(KeyEventArgs e)
        {
            return false;
        }

        public virtual bool KeyChar(KeyCharEventArgs e)
        {
            return false;
        }

        public virtual bool MouseMove(MouseMoveEventArgs e)
        {
            return false;
        }

        public virtual bool MouseDown(MouseButtonEventArgs e)
        {
            return false;
        }

        public virtual bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }

        public virtual bool MouseScroll(MouseScrollEventArgs e)
        {
            return false;
        }
        
        protected void Remove()
        {
            _app.LayerManager.RemoveLayer(this);
        }
    }
}