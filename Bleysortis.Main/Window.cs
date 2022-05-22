using Bleysortis.Main.Objects;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;

namespace Bleysortis.Main
{
    public class Window : GameWindow
    {
        private const float VIEW_ANGLE_DEG = 45;

        private readonly List<int> _availableLightSources = new();
        private readonly Dictionary<BaseLightSource, int> _lightSources = new();

        private bool _mouseDownLeft;
        private bool _mouseDownRight;
        private Point _mouseCoordinates;
        private PointF _cameraPos;
        private PointF _angleDeg;
        private float _scale = 5;

        private Vector3 _cam00;
        private Vector3 _cam01;
        private Vector3 _cam10;
        private Vector3 _cam11;

        private Game _game = new Game();
        private Matrix4 _projectionMatrix;

        protected override void OnLoad(EventArgs e)
        {
            _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                   MathHelper.DegreesToRadians(VIEW_ANGLE_DEG), Width * 1f / Height, 0.5f, 100.0f);

            _cameraPos = new PointF(2, -4);
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1f);
            GL.ShadeModel(ShadingModel.Smooth);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Normalize);
            GL.Enable(EnableCap.ColorMaterial);
            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);

            _availableLightSources.Add((int)LightName.Light0);
            _availableLightSources.Add((int)LightName.Light1);
            _availableLightSources.Add((int)LightName.Light2);
            _availableLightSources.Add((int)LightName.Light3);

            _game.OnStart();
            base.OnLoad(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            _scale -= e.Delta * 0.15f;
            _scale = Math.Max(2f, _scale);
            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            _mouseDownLeft = e.Mouse.LeftButton == ButtonState.Pressed;
            _mouseDownRight = e.Mouse.RightButton == ButtonState.Pressed;
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            _mouseDownLeft = e.Mouse.LeftButton == ButtonState.Pressed;
            _mouseDownRight = e.Mouse.RightButton == ButtonState.Pressed;
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            var dfx = (float)(_mouseCoordinates.X - e.X) / Width;
            var dfy = (float)(_mouseCoordinates.Y - e.Y) / Height;

            if (_mouseDownLeft)
            {
                _cameraPos.X += dfx * _scale;
                _cameraPos.Y -= dfy * _scale;
            }

            if(_mouseDownRight)
            {
                _angleDeg.X = TrimDegree(_angleDeg.X + (float)dfx * 360f);
                _angleDeg.Y = TrimDegree(_angleDeg.Y + (float)dfy * 360f);
            }

            _mouseCoordinates = e.Position;
            base.OnMouseMove(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            // GL.PolygonMode(MaterialFace.Back, PolygonMode.Line);
            GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref _projectionMatrix);

            GL.MatrixMode(MatrixMode.Modelview);
            var eye = new Vector3(_cameraPos.X, _cameraPos.Y, _scale);
            var eyeTarget = new Vector3(_cameraPos.X, _cameraPos.Y + 3, 0);
            var cameraMatrix = Matrix4.LookAt(eye, eyeTarget, new Vector3(0, 0, 1));
            GL.LoadMatrix(ref cameraMatrix);

            var len = MathF.Tan(MathHelper.DegreesToRadians(VIEW_ANGLE_DEG / 2f));
            var len2 = len * Width / Height;
            Vector3 forward = (eyeTarget - eye).Normalized();
            Vector3 left = Vector3.Cross(new Vector3(0, 0, 1), forward).Normalized() * len2;
            Vector3 right = -left;
            Vector3 up = Vector3.Cross(forward, left).Normalized() * len;
            Vector3 down = -up;

            _cam00 = new Ray(eye, forward + left + up).IntersectWithZ();
            _cam01 = new Ray(eye, forward + right + up).IntersectWithZ();
            _cam10 = new Ray(eye, forward + left + down).IntersectWithZ();
            _cam11 = new Ray(eye, forward + right + down).IntersectWithZ();

            foreach (var item in _game.EnumerateObjects())
            {
                Render(item);
            }

            SwapBuffers();

            base.OnRenderFrame(e);
        }

        private Vector3 Raycast(Matrix4 rev, int screenX, int screenY)
        {
            var vec = new Vector4(screenX * 2f / Width - 1, 1 - screenY * 2f / Height, 0, 1);
            var ws = rev * vec;
            return new Vector3(ws.X / ws.W, ws.Y / ws.W, ws.Z / ws.W);
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                   MathHelper.DegreesToRadians(VIEW_ANGLE_DEG), Width * 1f / Height, 0.5f, 100.0f);

            base.OnResize(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
            _game.Dispose();
        }

        private static float TrimDegree(float value)
        {
            if (value < 0) return TrimDegree(value + 360);
            if (value > 360) return TrimDegree(value - 360);
            return value;
        }

        private void Render(BaseObject obj, Matrix3? parentMatrix = null)
        {
            switch (obj.Kind)
            {
                case ObjectKind.Mesh:
                    if(obj is BaseMeshObject mesh)
                    {
                        GL.PushMatrix();
                        GL.Translate(mesh.Center);
                        GL.Scale(mesh.Scale);

                        if(mesh.Center.X > _cam00.X - 2
                            && mesh.Center.X < _cam01.X + 2
                            && mesh.Center.Y < _cam00.Y + 2
                            && mesh.Center.Y > _cam10.Y - 2)
                        {
                            RenderMesh(mesh);
                        }

                        foreach (var child in mesh.EnumerateChildren())
                        {
                            Render(child);
                        }

                        GL.PopMatrix();
                    }

                    break;

                case ObjectKind.Light:
                    RenderLight(obj as BaseLightSource, parentMatrix ?? Matrix3.Identity);
                    break;
            }
        }

        private void RenderMesh(BaseMeshObject item)
        {
            var mesh = item?.GetMesh();
            if (mesh == null) return;

            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(Color.White);

            for (int i = 0; i < mesh.Length; i++)
            {
                var triangle = mesh[i];
                for (int j = 0; j < triangle.Points.Length; j++)
                {
                    if (triangle.Normales.Length == 1)
                    {
                        GL.Normal3(triangle.Normales[0]);
                    }
                    else
                    {
                        GL.Normal3(triangle.Normales[j]);
                    }

                    if(triangle.Colors.Length == 1)
                    {
                        GL.Color3(triangle.Colors[0]);
                    }
                    else
                    {
                        GL.Color3(triangle.Colors[j]);
                    }

                    GL.Vertex3(triangle.Points[j]);
                }
            }

            GL.End();
        }

        private void RenderLight(BaseLightSource light, Matrix3 parentMatrix)
        {
            if (light == null) return;

            if(!_lightSources.ContainsKey(light))
            {
                if (_availableLightSources.Count == 0) return;

                var available = _availableLightSources[0];
                _availableLightSources.RemoveAt(0);
                _lightSources[light] = available;
            }

            var cap = (EnableCap)_lightSources[light];
            var name = (LightName)_lightSources[light];
            if (light.Enabled)
            {
                GL.Enable(cap);
            } 
            else
            {
                GL.Disable(cap);
                return;
            }

            var center = Vector3.Transform(parentMatrix, light.Center);
            GL.Light(name, LightParameter.Position, new Vector4(center, 1));
            GL.Light(name, LightParameter.ConstantAttenuation, light.Attenuation);
            if(light.Ambient.HasValue) GL.Light(name, LightParameter.Ambient, light.Ambient.Value);
            if(light.Diffuse.HasValue) GL.Light(name, LightParameter.Diffuse, light.Diffuse.Value);
            if(light.Specular.HasValue) GL.Light(name, LightParameter.Specular, light.Specular.Value);
        }
    }
}
