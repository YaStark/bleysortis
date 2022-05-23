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
        private readonly List<TransparentObjectInfo> _transparentInfo = new();

        private bool _mouseDownLeft;

        private Vector3 _cam00;
        private Vector3 _cam01;
        private Vector3 _cam10;

        private Game _game = new Game();
        private Camera _camera = new Camera(6, 0, 5).SetupScale(2, 20);
        private Vector3 _ptZx;

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

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);

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
            _ptZx = _camera.GetRay(e.X, e.Y).IntersectWithZ();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            _mouseDownLeft = e.Mouse.LeftButton == ButtonState.Pressed;
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (_mouseDownLeft)
            {
                var ptZ = _camera.GetRay(e.X, e.Y).IntersectWithZ();
                var pt = _ptZx - ptZ;
                _camera.Offset(pt.X, pt.Y);
                _ptZx = _camera.GetRay(e.X, e.Y).IntersectWithZ();
            }

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

            var tr = new TransparentObjectInfo(Vector3.Zero, new Vector3(1, 1, 1));
            foreach (var item in _game.EnumerateObjects())
            {
                Render(item, tr);
            }

            RenderTransparent(tr);

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

        private void Render(BaseObject obj, TransparentObjectInfo transparate, Matrix3? parentMatrix = null)
        {
            switch (obj.Kind)
            {
                case ObjectKind.Mesh:
                    if(obj is BaseMeshObject mesh)
                    {
                        GL.PushMatrix();
                        GL.Translate(mesh.Center);
                        GL.Scale(mesh.Scale);
                        var tr = new TransparentObjectInfo(mesh.Center, mesh.Scale);
                        if (mesh.Center.X > _cam00.X - 2
                            && mesh.Center.X < _cam01.X + 2
                            && mesh.Center.Y < _cam00.Y + 2
                            && mesh.Center.Y > _cam10.Y - 2)
                        {
                            RenderMesh(mesh, tr);
                        }

                        foreach (var child in mesh.EnumerateChildren())
                        {
                            Render(child, tr);
                        }

                        if(tr.Children.Count > 0 || tr.Triangles.Count > 0)
                        {
                            transparate.Children.Add(tr);
                        }

                        GL.PopMatrix();
                    }

                    break;

                case ObjectKind.Light:
                    RenderLight(obj as BaseLightSource, parentMatrix ?? Matrix3.Identity);
                    break;
            }
        }

        private void RenderMesh(BaseMeshObject item, TransparentObjectInfo transparent)
        {
            var mesh = item?.GetMesh();
            if (mesh == null)
            {
                return;
            }

            GL.Begin(PrimitiveType.Triangles);
            foreach (var triangle in mesh)
            {
                if (triangle.Transparent)
                {
                    transparent.Triangles.Add(triangle);
                }
                else
                {
                    RenderTriangle(triangle);
                }
            }

            GL.End();
        }

        private void RenderTransparent(TransparentObjectInfo item)
        {
            GL.PushMatrix();
            GL.Translate(item.Offset);
            GL.Scale(item.Scale);
            GL.Begin(BeginMode.Triangles);
            foreach (var triangle in item.Triangles)
            {
                RenderTriangle(triangle);
            }

            GL.End();

            foreach (var child in item.Children)
            {
                RenderTransparent(child);
            }

            GL.PopMatrix();
        }

        private static void RenderTriangle(Triangle triangle)
        {
            for (int i = 0; i < triangle.Points.Length; i++)
            {
                triangle.GetNormale(i).DoIfNotNull(nor => GL.Normal3(nor));
                triangle.GetColor(i).DoIfNotNull(color => GL.Color4(color));
                GL.Vertex3(triangle.Points[i]);
            }
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

    public class TransparentObjectInfo
    {
        public Vector3 Offset { get; }
        public Vector3 Scale { get; }
        public List<Triangle> Triangles { get; } = new();
        public List<TransparentObjectInfo> Children { get; } = new();

        public TransparentObjectInfo(Vector3 offset, Vector3 scale)
        {
            Offset = offset;
            Scale = scale;
        }
    }
}
