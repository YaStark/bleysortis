using System;

namespace Bleysortis.Main
{
    public enum HexDirection
    {
        /// <summary>
        /// Right
        /// </summary>
        E,

        /// <summary>
        /// Top right
        /// </summary>
        NE,

        /// <summary>
        /// Top left
        /// </summary>
        NW,

        /// <summary>
        /// Left
        /// </summary>
        W,

        /// <summary>
        /// Bottom left
        /// </summary>
        SW,

        /// <summary>
        /// Bottom right
        /// </summary>
        SE
    }

    public static class HexDirectionExt
    {
        public static HexDirection Opposite(this HexDirection direction)
        {
            return (HexDirection)(((int)direction + 3) % 6);
        }

        public static HexDirection ByAndleDeg(int angleDeg)
        {
            return (HexDirection)((int)Math.Round(angleDeg / 60f) % 6);
        }

        public static HexDirection Next(this HexDirection direction, int count = 1)
        {
            return (HexDirection)(((int)direction + count) % 6);
        }
    }
}
