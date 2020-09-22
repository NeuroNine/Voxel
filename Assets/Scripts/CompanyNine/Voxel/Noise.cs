using UnityEngine;

namespace CompanyNine.Voxel
{
    public static class Noise
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

        public static bool Get3DPerlinNoise(Vector3 position, float offset,
            float scale, float threshold)
        {
            var x = (position.x + offset + +.1f) * scale;
            var y = (position.y + offset + +.1f) * scale;
            var z = (position.z + offset + +.1f) * scale;


            var xy = Mathf.PerlinNoise(x, y);
            var xz = Mathf.PerlinNoise(x, z);
            var yz = Mathf.PerlinNoise(y, z);

            var yx = Mathf.PerlinNoise(y, x);
            var zx = Mathf.PerlinNoise(z, x);
            var zy = Mathf.PerlinNoise(z, y);

            var noise = (xy + yz + xz + yx + zy + zx) / 6f;

            return noise > threshold;
        }
    }
}