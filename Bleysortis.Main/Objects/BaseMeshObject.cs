using OpenTK;
using System.Collections.Generic;

namespace Bleysortis.Main
{
    public class BaseMeshObject : BaseObject
    {
        private readonly List<BaseObject> _children = new();

        private Triangle[] _mesh;
        public Vector3 Scale { get; set; } = new Vector3(1, 1, 1);

        protected BaseMeshObject()
            : base(ObjectKind.Mesh)
        {
        }

        public virtual bool RequestRebuildMesh()
        {
            return false;
        }

        public IEnumerable<BaseObject> EnumerateChildren()
        {
            return _children;
        }

        public Triangle[] GetMesh()
        {
            if (_mesh == null || RequestRebuildMesh())
            {
                _mesh = OnGetMesh();
            }

            return _mesh;
        }

        protected virtual Triangle[] OnGetMesh()
        {
            return null;
        }

        public override void OnTick(int delayMs)
        {
            foreach (var child in _children)
            {
                child.OnTick(delayMs);
            }

            base.OnTick(delayMs);
        }

        protected void AddChildren(params BaseObject[] children)
        {
            _children.AddRange(children);
        }
    }
}
