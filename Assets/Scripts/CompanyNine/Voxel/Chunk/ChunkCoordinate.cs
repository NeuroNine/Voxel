namespace CompanyNine.Voxel.Chunk
{
    public readonly struct ChunkCoordinate
    {
        public int X { get; }
        public int Z { get; }

        public static readonly ChunkCoordinate Zero = Of(0, 0);

        private ChunkCoordinate(int x, int z)
        {
            X = x;
            Z = z;
        }

        public static ChunkCoordinate Of(int x, int z)
        {
            return new ChunkCoordinate(x, z);
        }


        public override string ToString()
        {
            return $"{X},{Z}";
        }

        private bool Equals(ChunkCoordinate other)
        {
            return X == other.X && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj.GetType() == GetType() && Equals((ChunkCoordinate) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }

        public static ChunkCoordinate operator -(ChunkCoordinate a,
            ChunkCoordinate b) => Of(a.X - b.X, a.Z - b.Z);

        public static ChunkCoordinate operator +(ChunkCoordinate a,
            ChunkCoordinate b) => Of(a.X + b.X, a.Z + b.Z);

        public static ChunkCoordinate operator +(ChunkCoordinate a,
            int b) => Of(a.X + b, a.Z + b);
    }
}