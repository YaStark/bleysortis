using OpenTK;

namespace Bleysortis.Main
{
    public class Ray
    {
        private readonly Vector3 _end;
        public Vector3 Start { get; }
        public Vector3 Direction { get; }

        public Ray(Vector3 start, Vector3 direction)
        {
            Start = start;
            Direction = direction;
            _end = start + direction;
        }

        public Vector3 IntersectWithZ()
        {
            var zs = -Start.Z / (_end.Z - Start.Z);
            var y = zs * (_end.Y - Start.Y) + Start.Y;
            var x = zs * (_end.X - Start.X) + Start.X;
            return new Vector3(x, y, 0);
        }
    }
}
