namespace CompanyNine.Voxel.Terrain
{
    [System.Serializable]
    public class Lode
    {
        public string nodeName;
        public byte blockId;
        public int minSpawnHeight;
        public int maxSpawnHeight;
        public float scale;
        public float threshold;
        public float noiseOffset;
    }
}