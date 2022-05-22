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
        private Point _mouseCoordinates;

        private Vector3 _cam00;
        private Vector3 _cam01;
        private Vector3 _cam10;

        private Game _game = new Game();
        private Camera _camera = new Camera(2, -4, 5).SetupScale(2, 20);

        protected override void OnLoad(EventArgs e)
        {
            _camera.SetViewport(Width, Height, VIEW_ANGLE_DEG);
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
            _camera.SetScale(_camera.Scale - e.Delta * 0.15f);
            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            _mouseDownLeft = e.Mouse.LeftButton == ButtonState.Pressed;
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            _mouseDownLeft = e.Mouse.LeftButton == ButtonState.Pressed;
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            var dfx = (float)(_mouseCoordinates.X - e.X) / Width;
            var dfy = (float)(_mouseCoordinates.Y - e.Y) / Height;

            if (_mouseDownLeft)
            {
                _camera.Offset(dfx, -dfy);
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
            var projMx = _camera.GetProjectionMatrix();
            GL.LoadMatrix(ref projMx);

            GL.MatrixMode(MatrixMode.Modelview);
            var cameraMx = _camera.GetCameraMatrix();
            GL.LoadMatrix(ref cameraMx);

            _cam00 = _camera.GetRay(0, 0).IntersectWithZ();
            _cam01 = _camera.GetRay(Width - 1, 0).IntersectWithZ();
            _cam10 = _camera.GetRay(0, Height - 1).IntersectWithZ();

            foreach (var item in _game.EnumerateObjects())
            {
                Render(item);
            }

            SwapBuffers();
            base.OnRenderFrame(e);
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            _camera.SetViewport(Width, Height, VIEW_ANGLE_DEG);
            base.OnResize(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
            _game.Dispose();
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
            for (int i = 0; i < mesh.Length; i++)
            {
                var triangle = mesh[i];
                for (int j = 0; j < triangle.Points.Length; j++)
                {
                    triangle.GetNormale(j).DoIfNotNull(nor => GL.Normal3(nor));
                    triangle.GetColor(j).DoIfNotNull(color => GL.Color3(color));
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
            if (!light.Enabled)
            {
                GL.Disable(cap);
                return;
            }

            GL.Enable(cap);
            var center = Vector3.Transform(parentMatrix, light.Center);
            GL.Light(name, LightParameter.Position, new Vector4(center, 1));
            GL.Light(name, LightParameter.ConstantAttenuation, light.Attenuation);
            if(light.Ambient.HasValue) GL.Light(name, LightParameter.Ambient, light.Ambient.Value);
            if(light.Diffuse.HasValue) GL.Light(name, LightParameter.Diffuse, light.Diffuse.Value);
            if(light.Specular.HasValue) GL.Light(name, LightParameter.Specular, light.Specular.Value);
        }
    }
}
