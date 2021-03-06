using Thundershock.Core.Rendering;

using Silk.NET.OpenGL;

namespace Thundershock.OpenGL
{
    public sealed class GlTextureCollection : TextureCollection
    {
        private Core.Rendering.Texture[] _textures;
        private GL _gl;
        
        public GlTextureCollection(GL gl)
        {
            _textures = new Core.Rendering.Texture[32];
            _gl = gl;
        }

        public override int Count => _textures.Length;
        protected override Core.Rendering.Texture GetTexture(int index)
        {
            return _textures[index];
        }

        protected override void BindTexture(int index, Core.Rendering.Texture texture)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + index);
            if (texture == null)
            {
                _gl.BindTexture(GLEnum.Texture2D, 0);
            }
            else
            {
                _gl.BindTexture(GLEnum.Texture2D, texture.Id);
            }

            _textures[index] = texture;
        }
    }
}