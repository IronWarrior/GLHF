namespace GLHF
{
    public struct PlayerId
    {
        public uint Raw;
        
        public static PlayerId Host => MaxPlayers - 1;
        public const uint MaxPlayers = 16;

        public PlayerId(uint value)
        {
            Raw = value;
        }

        public static implicit operator PlayerId(uint value)
        {
            return new PlayerId(value);
        }

        public static implicit operator uint(PlayerId playerId)
        {
            return playerId.Raw;
        }
    }
}
