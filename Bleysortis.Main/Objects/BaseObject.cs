using OpenTK;

namespace Bleysortis.Main
{
    public class BaseObject
    {
        public Vector3 Center { get; set; }
        public ObjectKind Kind { get; }

        protected BaseObject(ObjectKind kind)
        {
            Kind = kind;
        }

        public virtual void OnTick(int delayMs)
        {
        }
    }
}
