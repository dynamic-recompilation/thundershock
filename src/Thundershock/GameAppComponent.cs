using System;
using Thundershock.Core;

namespace Thundershock
{
    public abstract class GameAppComponent : IGlobalComponent
    {
        AppBase IGlobalComponent.App => this.App;

        public GameAppBase App { get; private set; }

        public void Initialize(AppBase app)
        {
            if (app is GameAppBase gameApp)
            {
                App = gameApp ?? throw new ArgumentNullException(nameof(app));

                OnLoad();
            }
            else
            {
                throw new InvalidOperationException("GameAppComponent objects can only be initialized by a GameApp or GameAppBase.");
            }
        }

        public void Unload()
        {
            OnUnload();
            App = null;
        }

        public void Update(GameTime gameTime)
        {
            OnUpdate(gameTime);
        }

        protected virtual void OnLoad() {}
        protected virtual void OnUnload() {}

        protected virtual void OnUpdate(GameTime gameTime) {}

    }
}