using System;
using System.Collections.Generic;
using System.Linq;
using CompanyNine.Voxel.Chunk;
using CompanyNine.Voxel.Terrain;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CompanyNine.Voxel
{
    public class World : MonoBehaviour
    {
        public Transform player;
        public Vector3 spawnPosition;
        public Material material;

        public const float Gravity = -9.8f;
        public int seed;
        public BiomeAttributes biome;
        public Material Material => material;

        private readonly List<float> _chunkCreationTime = new List<float>(
            VoxelData
                .WorldSizeInChunks * VoxelData.WorldSizeInChunks);

        public readonly BlockType[] blockTypes =
        {
            BlockType.WithSingleTexture("Air", BlockTexture.Bedrock,
                false), // air block
            BlockType.WithSingleTexture("Bedrock",
                BlockTexture.Bedrock), // bedrock block
            BlockType.WithSingleTexture("Stone",
                BlockTexture.Stone), // stone block
            BlockType.WithSingleTexture("Dirt", BlockTexture.Dirt),
            BlockType.WithSingleTexture("Planks",
                BlockTexture.Plank), // planks block
            BlockType.WithWrappedSideTexture("Log", BlockTexture.LogSide,
                BlockTexture.LogTop), // log block
            BlockType.WithWrappedSideTexture("Grass", BlockTexture.GrassSide,
                BlockTexture.GrassTop, BlockTexture.Dirt), // grass block
            BlockType.WithSingleTexture("Sand", BlockTexture.Sand),
        };

        private readonly Chunk.Chunk[][] _chunks =
            new Chunk.Chunk[VoxelData.WorldSizeInChunks][];

        private readonly HashSet<ChunkCoordinate> _activeChunks =
            new HashSet<ChunkCoordinate>();

        private ChunkCoordinate _currentPlayerChunk;

        private void Start()
        {
            Random.InitState(seed);
            spawnPosition = new Vector3(
                (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f,
                VoxelData.ChunkHeight + 2f,
                (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);

            var beginTime = Time.realtimeSinceStartup;

            GenerateWorld();

            var endTime = Time.realtimeSinceStartup;

            Debug.Log(
                $"Startup Time to generate World: {endTime - beginTime}s");
            Debug.Log(
                $"Average Chunk Creation Time is: {_chunkCreationTime.Average()}");

            _currentPlayerChunk = FindChunkCoordinate(player.position);
        }

        private void GenerateWorld()
        {
            for (var i = 0; i < _chunks.Length; i++)
            {
                _chunks[i] = new Chunk.Chunk[VoxelData.WorldSizeInChunks];
            }

            const int midPoint = VoxelData.WorldSizeInChunks / 2;
            for (var x = midPoint - VoxelData.ViewDistance;
                x <= midPoint + VoxelData.ViewDistance;
                x++)
            {
                for (var z = midPoint - VoxelData.ViewDistance;
                    z <= midPoint + VoxelData.ViewDistance;
                    z++)
                {
                    var beginTime = Time.realtimeSinceStartup;
                    CreateNewChunk(x, z);
                    var endTime = Time.realtimeSinceStartup;
                    _chunkCreationTime.Add((endTime - beginTime) * 1000);
                }
            }

            player.position = spawnPosition;
        }

        private void Update()
        {
            var chunkCoordinate = FindChunkCoordinate(player.position);
            if (!chunkCoordinate.Equals(_currentPlayerChunk))
            {
                //  UpdateViewDistance(_currentPlayerChunk, chunkCoordinate);
                _currentPlayerChunk = chunkCoordinate;
            }
        }


        private void UpdateViewDistance(ChunkCoordinate previous,
            ChunkCoordinate current)
        {
            var chunksToLoad = ChunkUtility.LoadChunks(previous, current);
            var chunksToUnload = ChunkUtility.UnloadChunks(previous, current);

            foreach (var coord in chunksToLoad)
            {
                ActivateChunk(coord);
            }

            foreach (var coord in chunksToUnload)
            {
                if (!IsChunkInWorld(coord))
                {
                    continue;
                }

                _chunks[coord.X][coord.Z].IsActive = false;
            }
        }

        private void ActivateChunk(ChunkCoordinate coord)
        {
            if (!IsChunkInWorld(coord))
            {
                return;
            }

            Chunk.Chunk chunk;
            try
            {
                chunk = _chunks[coord.X][coord.Z];
            }
            catch (NullReferenceException)
            {
                Debug.Log($"NPE accessing {coord}");
                throw;
            }

            // Check if it active, if not, activate it.
            if (chunk == null)
            {
                CreateNewChunk(coord.X, coord.Z);
            }
            else if (!chunk.IsActive)
            {
                chunk.IsActive = true;
                _activeChunks.Add((coord));
            }
        }

        /// <summary>
        /// Returns the block id of the requested voxel based on its world position.
        /// </summary>
        /// <param name="position">The <u>world</u> position of the voxel.</param>
        /// <returns>The block id of the voxel</returns>
        public ushort GetVoxel(Vector3 position)
        {
            var yPos = Mathf.FloorToInt(position.y);
            var xzPos = new Vector2(position.x, position.z);

            /* IMMUTABLE PASS */

            // if outside world its air
            if (!IsVoxelInWorld(position))
            {
                return 0;
            }

            // if bottom of the world, then bedrock
            if (yPos == 0)
            {
                return 1;
            }
            /* BASIC PASS */

            var terrainHeight =
                Mathf.FloorToInt(biome.terrainHeight *
                                 Noise.Get2DPerlinNoise(xzPos, 0,
                                     biome.terrainScale)) +
                biome.solidGroundHeight;
            ushort voxelValue;
            // for anything above our terrain height return air
            if (yPos > terrainHeight)
            {
                voxelValue = 0;
            }
            // put grass at the terrain height
            else if (yPos == terrainHeight)
            {
                voxelValue = 6;
            }
            // place dirt in the first few layers below the grass
            else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            {
                voxelValue = 3; // dirt
            }
            else
            {
                // otherwise return stone
                voxelValue = 2;
            }

            /* SECOND PASS */

            if (voxelValue == 2)
            {
                foreach (var lode in biome.lodes)
                {
                    if (yPos >= lode.minSpawnHeight &&
                        yPos <= lode.maxSpawnHeight)
                    {
                        if (Noise.Get3DPerlinNoise(position, lode.noiseOffset,
                            lode.scale, lode.threshold))
                        {
                            voxelValue = lode.blockId;
                        }
                    }
                }
            }

            return voxelValue;
        }


        private void CreateNewChunk(int x, int z)
        {
            if (!IsChunkInWorld(x, z))
            {
                return;
            }

            var coord = ChunkCoordinate.Of(x, z);
            var chunk = new Chunk.Chunk(this, coord);

            _chunks[x][z] = chunk;
            _activeChunks.Add(coord);
        }

        private static ChunkCoordinate FindChunkCoordinate(
            Vector3 worldPosition)
        {
            return ChunkCoordinate.Of(
                Mathf.FloorToInt(worldPosition.x / VoxelData.ChunkWidth),
                Mathf.FloorToInt(worldPosition.z / VoxelData.ChunkWidth));
        }

        private static ChunkCoordinate FindChunkCoordinate(
            float worldPositionX, float worldPositionZ)
        {
            return ChunkCoordinate.Of(
                Mathf.FloorToInt(worldPositionX / VoxelData.ChunkWidth),
                Mathf.FloorToInt(worldPositionZ / VoxelData.ChunkWidth));
        }


        // ReSharper disable once UnusedMember.Local
        private static bool IsChunkInWorld(ChunkCoordinate chunkCoordinate)
        {
            return IsChunkInWorld(chunkCoordinate.X, chunkCoordinate.Z);
        }

        private static bool IsChunkInWorld(int x, int z)
        {
            return x >= 0 && x <= VoxelData.WorldSizeInChunks - 1 && z >= 0 &&
                   z <= VoxelData.WorldSizeInChunks - 1;
        }

        public bool IsVoxelSolid(float x, float y, float z)
        {
            if (!IsVoxelInWorld(x, y, z))
            {
                return false;
            }

            var chunkCoordinate = FindChunkCoordinate(x, z);

            var xCheck = Mathf.FloorToInt(x);
            var yCheck = Mathf.FloorToInt(y);
            var zCheck = Mathf.FloorToInt(z);

            xCheck -= chunkCoordinate.X * VoxelData.ChunkWidth;
            zCheck -= chunkCoordinate.Z * VoxelData.ChunkWidth;

            var blockType = _chunks[chunkCoordinate.X][chunkCoordinate.Z]
                .blockIdArray[xCheck][yCheck][zCheck];

            return blockTypes[blockType].IsSolid;
        }

        private static bool IsVoxelInWorld(Vector3 voxelPosition)
        {
            return IsVoxelInWorld(voxelPosition.x,
                voxelPosition.y,
                voxelPosition.z);
        }

        private static bool IsVoxelInWorld(float x, float y, float z)
        {
            return x >= 0 &&
                   x < VoxelData.WorldSizeInVoxels &&
                   y >= 0 &&
                   y < VoxelData.ChunkHeight &&
                   z >= 0 &&
                   z < VoxelData.WorldSizeInVoxels;
        }
    }

    [Serializable]
    public class BlockType
    {
        public string BlockName { get; private set; }
        public bool IsSolid { get; private set; }
        public BlockTexture BackTexture { get; private set; }
        public BlockTexture FrontTexture { get; private set; }
        public BlockTexture TopTexture { get; private set; }
        public BlockTexture BottomTexture { get; private set; }
        public BlockTexture LeftTexture { get; private set; }
        public BlockTexture RightTexture { get; private set; }

        public BlockType(
            string blockName,
            bool isSolid,
            BlockTexture backTexture,
            BlockTexture frontTexture,
            BlockTexture topTexture,
            BlockTexture bottomTexture,
            BlockTexture leftTexture,
            BlockTexture rightTexture)
        {
            BlockName = blockName;
            IsSolid = isSolid;
            BackTexture = backTexture;
            FrontTexture = frontTexture;
            TopTexture = topTexture;
            BottomTexture = bottomTexture;
            LeftTexture = leftTexture;
            RightTexture = rightTexture;
        }

        public static BlockType WithSingleTexture(string blockName,
            BlockTexture singleTexture, bool isSolid = true)
        {
            return new BlockType(blockName, isSolid, singleTexture,
                singleTexture, singleTexture, singleTexture,
                singleTexture, singleTexture);
        }

        public static BlockType WithWrappedSideTexture(string blockName,
            BlockTexture sideTexture,
            BlockTexture topBottomTexture, bool isSolid = true)
        {
            return new BlockType(blockName, isSolid, sideTexture,
                sideTexture, topBottomTexture, topBottomTexture,
                sideTexture, sideTexture);
        }

        public static BlockType WithWrappedSideTexture(string blockName,
            BlockTexture sideTexture,
            BlockTexture topTexture, BlockTexture bottomTexture,
            bool isSolid = true)
        {
            return new BlockType(blockName, isSolid, sideTexture,
                sideTexture, topTexture, bottomTexture,
                sideTexture, sideTexture);
        }

        public BlockTexture GetBlockTexture(VoxelData.Face face)
        {
            switch (face)
            {
                case VoxelData.Face.Back:
                    return BackTexture;
                case VoxelData.Face.Front:
                    return FrontTexture;
                case VoxelData.Face.Top:
                    return TopTexture;
                case VoxelData.Face.Bottom:
                    return BottomTexture;
                case VoxelData.Face.Left:
                    return LeftTexture;
                case VoxelData.Face.Right:
                    return RightTexture;
                default:
                    throw new ArgumentException("Invalid Face: " + face);
            }
        }
    }

    [Serializable]
    public sealed class BlockTexture
    {
        public static readonly BlockTexture
            Stone = new BlockTexture(0, "Stone");

        public static readonly BlockTexture Dirt = new BlockTexture(1, "Dirt");

        public static readonly BlockTexture GrassSide =
            new BlockTexture(2, "Grass Side");

        public static readonly BlockTexture Coal = new BlockTexture(3, "Coal");

        public static readonly BlockTexture
            Plank = new BlockTexture(4, "Plank");

        public static readonly BlockTexture LogSide =
            new BlockTexture(5, "Log Side");

        public static readonly BlockTexture LogTop =
            new BlockTexture(6, "Log Top");

        public static readonly BlockTexture GrassTop =
            new BlockTexture(7, "Grass Top");

        public static readonly BlockTexture Cobblestone =
            new BlockTexture(8, "Cobblestone");

        public static readonly BlockTexture
            Bedrock = new BlockTexture(9, "Bedrock");

        public static readonly BlockTexture Sand = new BlockTexture(10, "Sand");

        public static readonly BlockTexture Bricks =
            new BlockTexture(11, "Bricks");

        public static readonly BlockTexture FurnaceCold =
            new BlockTexture(12, "Furnace Cold");

        public static readonly BlockTexture FurnaceBack =
            new BlockTexture(13, "Furance Back");

        public static readonly BlockTexture FurnaceHot =
            new BlockTexture(14, "Furance Hot");

        public static readonly BlockTexture FurnaceSide =
            new BlockTexture(15, "Furnace Side");

        public int Id { get; private set; }
        public string Name { get; private set; }

        private BlockTexture(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public static List<BlockTexture> AllTextures()
        {
            return new List<BlockTexture>
            {
                Stone, Dirt, GrassSide, Coal, Plank, LogSide, LogTop, GrassTop,
                Cobblestone, Bedrock, Sand, Bricks,
                FurnaceCold, FurnaceBack, FurnaceHot, FurnaceSide
            };
        }
    }

    public enum ChunkAxis
    {
        X,
        Z
    }
}