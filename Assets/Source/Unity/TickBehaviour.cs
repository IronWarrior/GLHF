using UnityEngine;

namespace GLHF
{
    public abstract class TickBehaviour : MonoBehaviour
    {
        public Simulation Simulation { get; set; }
        public StateObject Object { get; set; }

        public virtual void Initialized() { }
        public virtual void TickStart() { }
        public virtual void TickUpdate() { }
        public virtual void TickDestroy() { }
        public virtual void RenderStart() { }
        public virtual void Render() { }
    }
}