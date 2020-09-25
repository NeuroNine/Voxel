using UnityEngine;

namespace CompanyNine.Voxel.Terrain
{
    [CreateAssetMenu(fileName = "BiomeAttributes", menuName = "BiomeAttribute")]
    public class BiomeAttributes : ScriptableObject
    {
        public new string name;
        public int solidGroundHeight;
        /// <summary>
        /// Max height above solidGroundHeight.
        /// </summary>
        public int terrainHeight;
        public float terrainScale;
        public Lode[] lodes;
    }
}