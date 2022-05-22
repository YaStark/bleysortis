using OpenTK;

namespace Bleysortis.Main
{
    public enum GroundType
    {
        Sand,
        Grass,
        Dirt,
        Rock,
        Snow
    }

    public static class GroundTypeEx
    {
        public static Color GetColor(this GroundType groundType)
        {
            switch (groundType)
            {
                case GroundType.Dirt:
                    return Color.FromArgb(255, 162, 101, 62);
                
                case GroundType.Grass:
                    return Color.FromArgb(255, 63, 155, 11);

                case GroundType.Sand:
                    return Color.FromArgb(255, 240, 219, 125);

                case GroundType.Rock:
                    return Color.FromArgb(255, 122, 122, 122);

                case GroundType.Snow:
                    return Color.FromArgb(255, 238, 233, 233);

                default:
                    return Color.Red;
            }
        }
    }
}
