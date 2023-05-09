namespace GLHF
{
    // TODO: Can just use unsafe to allow the user to define
    // their own input types.
    [System.Serializable]
    public struct StateInput
    {
        public UnityEngine.Vector3 MoveDirection;
        public bool Fire;
    }
}
