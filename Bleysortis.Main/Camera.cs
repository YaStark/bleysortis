using OpenTK;
using System;

namespace Bleysortis.Main
{
    public class Camera
    {
        private float _minScale;
        private float _maxScale;
        private Vector3 _eye;
        private Vector3 _eyeTarget;
        private Matrix4 _cameraMatrix;
        private Matrix4 _projectionMatrix;
        private Vector3 _forward;
        private Vector3 _left;
        private Vector3 _up;

        public float Scale { get; private set; }
        public PointF Position { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float Angle { get; private set; }

        public Camera(float x, float y, float scale)
        {
            Position = new PointF(x, y);
            Scale = scale;
        }

        public Camera SetScale(float scale)
        {
            Scale = scale;
            RecalcPosition();
            return this;
        }

        public Camera SetupScale(float min, float max)
        {
            _minScale = min;
            _maxScale = max;
            return this;
        }

        public void Offset(float dx, float dy)
        {
            Position = new PointF(Position.X + dx * Scale, Position.Y + dy * Scale);
            RecalcPosition();
        }

        public Ray GetRay(int x, int y)
        {
            var leftRatio = 1 - x * 2f / Width;
            var upRatio = 1 - y * 2f / Height;
            return new Ray(_eye, _forward + _left * leftRatio + _up * upRatio);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return _projectionMatrix;
        }

        public Matrix4 GetCameraMatrix()
        {
            return _cameraMatrix;
        }

        public void SetViewport(int width, int height, float angle)
        {
            Width = width;
            Height = height;
            Angle = angle;
            RecalcPosition();
            RecalcViewport();
        }

        private void RecalcPosition()
        {
            Scale = Math.Max(_minScale, Math.Min(_maxScale, Scale));
            _eye = new Vector3(Position.X, Position.Y, Scale);
            _eyeTarget = new Vector3(Position.X, Position.Y + 3, 0);
            _cameraMatrix = Matrix4.LookAt(_eye, _eyeTarget, new Vector3(0, 0, 1));
        }

        private void RecalcViewport()
        {
            _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                   MathHelper.DegreesToRadians(Angle), Width * 1f / Height, 0.5f, 100.0f);

            var len = MathF.Tan(MathHelper.DegreesToRadians(Angle / 2f));
            var len2 = len * Width / Height;
            _forward = (_eyeTarget - _eye).Normalized();
            _left = Vector3.Cross(new Vector3(0, 0, 1), _forward).Normalized() * len2;
            _up = Vector3.Cross(_forward, _left).Normalized() * len;
        }
    }
}
