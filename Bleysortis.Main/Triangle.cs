using OpenTK;

namespace Bleysortis.Main
{
    public class Triangle
    {
        public Vector3[] Points { get; }
        public Vector3[] Normales { get; private set; }
        public Color[] Colors { get; private set; }

        public Triangle(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            Points = new[] { point1, point2, point3 };
        }

        public Triangle SetDefaultNormale()
        {
            var vecA = Points[1] - Points[0];
            var vecB = Points[2] - Points[1];
            Normales = new[] { Vector3.Cross(vecA, vecB).Normalized() };
            return this;
        }

        public Triangle SetColor(Color color)
        {
            Colors = new[] { color };
            return this;
        }

        public Color? GetColor(int i) 
        {
            if (i < 0 || i >= Colors.Length || Colors == null) return null;
            return Colors.Length == 1 ? Colors[0] : Colors[i];
        }

        public Vector3? GetNormale(int i)
        {
            if (i < 0 || i >= Normales.Length || Normales == null) return null;
            return Normales.Length == 1 ? Normales[0] : Normales[i];
        }

        public Triangle SetColors(Color color1, Color color2, Color color3)
        {
            Colors = new[] { color1, color2, color3 };
            return this;
        }

        public Triangle SetNormaleUp()
        {
            Normales = new[] { new Vector3(0, 0, 1) };
            return this;
        }

        public Triangle CloneWithPoints()
        {
            return new Triangle(Points[0], Points[1], Points[2]);
        }
    }
}
