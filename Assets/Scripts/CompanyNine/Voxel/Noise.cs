using UnityEngine;

namespace CompanyNine.Voxel
{
    public class Noise
    {
        public static float Get2DPerlinNoise(Vector2 position, float offset,
            float scale)
        {
            var posX = (position.x + .1f) / VoxelData.ChunkWidth
                * scale + offset;
            var posY = (position.y + .1f) / VoxelData.ChunkWidth
                * scale + offset;
            return Mathf.PerlinNoise(posX, posY);
        }
    }
}