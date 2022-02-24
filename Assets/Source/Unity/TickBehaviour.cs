using UnityEngine;

namespace GLHF
{
    public class TickBehaviour : MonoBehaviour
    {
        public Runner Runner { get; set; }
        public StateObject Object { get; set; }

        public virtual void TickStart() { }
        public virtual void TickUpdate() { }
        public virtual void RenderStart() { }
        public virtual void Render() { }
    }
}