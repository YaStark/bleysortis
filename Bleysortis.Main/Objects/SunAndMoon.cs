using OpenTK;
using System;

namespace Bleysortis.Main.Objects
{
    public class SunAndMoon : BaseMeshObject
    {
        private static readonly Color _colorSunrise = Color.FromArgb(1, 255, 207, 72);
        private static readonly Color _colorSunset = Color.FromArgb(1, 246, 71, 71);

        private static readonly Color _colorMoonrise = Color.FromArgb(1, 71, 159, 246);
        private static readonly Color _colorMoonset = Color.FromArgb(1, 116, 72, 255);

        private readonly int _radius;
        private readonly int _cx;
        private readonly int _cy;
        private int _daytime = 12 * 60 * 1000;
        private int _period;

        private bool _day = true;

        public BaseLightSource Sun { get; } = new BaseLightSource();

        public SunAndMoon(int cx, int cy, int radius)
        {
            _cx = cx;
            _cy = cy;
            _radius = radius;
            _period = _daytime / 2;
            AddChildren(Sun);
        }

        public override void OnTick(int delayMs)
        {
            _period += delayMs;
            if (_period > _daytime)
            {
                _period -= _daytime;
                _day = !_day;
            }

            float angle = _period * MathF.PI / _daytime;
            float sin = MathF.Sin(angle);
            float sin4 = sin * sin * sin * sin;
            if (_day)
            {
                bool am = _period < _daytime / 2;
                Sun.Center = new Vector3(_cx + _radius * MathF.Cos(angle), _cy, _radius * sin);

                var r = am
                    ? _colorSunrise.R * (1 - sin4) / 255 + sin4
                    : _colorSunset.R * (1 - sin4) / 255 + sin4;

                var g = am
                    ? _colorSunrise.G * (1 - sin4) / 255 + sin4
                    : _colorSunset.G * (1 - sin4) / 255 + sin4;

                var b = am
                    ? _colorSunrise.B * (1 - sin4) / 255 + sin4
                    : _colorSunset.B * (1 - sin4) / 255 + sin4;

                Sun.Diffuse = new Vector4(r, g, b, 0);
                Sun.Attenuation = 1 / (sin4 * .7f + .01f);
            } 
            else
            {
                bool am = _period > _daytime / 2;

                var r = am
                    ? _colorMoonrise.R * (1 - sin4) / 255 + sin
                    : _colorMoonset.R * (1 - sin4) / 255 + sin;

                var g = am
                    ? _colorMoonrise.G * (1 - sin4) / 255 + sin
                    : _colorMoonset.G * (1 - sin4) / 255 + sin;

                var b = am
                    ? _colorMoonrise.B * (1 - sin4) / 255 + sin
                    : _colorMoonset.B * (1 - sin4) / 255 + sin;

                Sun.Center = new Vector3(_cx + _radius * MathF.Cos(angle), _cy, _radius * sin);
                Sun.Diffuse = new Vector4(r, g, b, 0);
                Sun.Attenuation = 1 / (sin4 * .2f + .01f);
            }

            base.OnTick(delayMs);
        }
    }
}
