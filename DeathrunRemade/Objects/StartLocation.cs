namespace DeathrunRemade.Objects
{
    public struct StartLocation
    {
        public string Name;
        public float X;
        public float Y;
        public float Z;

        public StartLocation(string name, float x, float y, float z)
        {
            Name = name;
            X = x;
            Y = y;
            Z = z;
        }
    }
}