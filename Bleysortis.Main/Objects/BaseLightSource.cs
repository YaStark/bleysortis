using OpenTK;

namespace Bleysortis.Main.Objects
{
    public class BaseLightSource : BaseObject
    {
        public Vector4? Ambient { get; set; }
        public Vector4? Diffuse { get; set; }
        public Vector4? Specular { get; set; }
        public float Attenuation { get; set; } = 1;
        public bool Enabled { get; set; } = true;

        public BaseLightSource()
            : base(ObjectKind.Light)
        {
        }
    }
}
