using UnityEngine;

namespace CompanyNine.Voxel
{
    public class Noise
    {
        private long _seed;
        private OpenSimplex2F _noiseGenerator;

        public Noise(long seed)
        {
            _seed = seed;
            _noiseGenerator = new OpenSimplex2F(seed);
        }

        public float Get2DNoise(Vector2 position, float offset,
            float scale)
        {
            var posX = (position.x / VoxelData.ChunkWidth)
                * scale + offset;
            var posY = (position.y / VoxelData.ChunkWidth)
                * scale + offset;
            return (float) (_noiseGenerator.Noise2(posX, posY) * 0.5 + 0.5);
        }

        public bool Get3DNoise(Vector3 position, float offset,
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

        public bool Get3DSimplex(Vector3 position, float offset,
            float scale, float threshold)
        {
            var x = (position.x + offset) * scale;
            var y = (position.y + offset) * scale;
            var z = (position.z + offset) * scale;

            var noise = _noiseGenerator.Noise3_XZBeforeY(x, y, z) * .5 + .5;

            return noise > threshold;
        }
    }
}