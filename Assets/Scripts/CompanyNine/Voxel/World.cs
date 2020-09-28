using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CompanyNine.Voxel.Chunk;
using CompanyNine.Voxel.Terrain;
using NaughtyAttributes;
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

        private readonly List<ChunkCoordinate> _chunksToCreate =
            new List<ChunkCoordinate>();

        private ChunkCoordinate _currentPlayerChunk;

        public ChunkCoordinate CurrentPlayerChunk => _currentPlayerChunk;

        private bool _isCreatingChunks;
        private Noise _noiseGenerator;
        public GameObject debugScreen;

        private void Start()
        {
            Random.InitState(seed);
            _noiseGenerator = new Noise(seed);
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

            _currentPlayerChunk =
                ChunkCoordinate.FromWorldPosition(player.position);
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
                    var coord = ChunkCoordinate.Of(x, z);

                    if (!IsChunkInWorld(coord))
                    {
                        continue;
                    }

                    var chunk = new Chunk.Chunk(this, coord);
                    chunk.Init();
                    _chunks[x][z] = chunk;
                    var endTime = Time.realtimeSinceStartup;
                    _chunkCreationTime.Add((endTime - beginTime) * 1000);
                    _activeChunks.Add(coord);
                }
            }

            player.position = spawnPosition;
        }

        private void Update()
        {
            var chunkCoordinate =
                ChunkCoordinate.FromWorldPosition(player.position);

            if (!chunkCoordinate.Equals(_currentPlayerChunk))
            {
                UpdateViewDistance(_currentPlayerChunk, chunkCoordinate);
                _currentPlayerChunk = chunkCoordinate;
            }

            if (_chunksToCreate.Count > 0 && !_isCreatingChunks)
            {
                StartCoroutine(nameof(CreateChunks));
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                debugScreen.SetActive(!debugScreen.activeSelf);
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

        private IEnumerator CreateChunks()
        {
            _isCreatingChunks = true;

            while (_chunksToCreate.Count > 0)
            {
                _chunks[_chunksToCreate[0].X][_chunksToCreate[0].Z].Init();
                _chunksToCreate.RemoveAt(0);
                yield return null;
            }

            _isCreatingChunks = false;
        }

        private void ActivateChunk(ChunkCoordinate coord)
        {
            if (!IsChunkInWorld(coord))
            {
                return;
            }

            var chunk = _chunks[coord.X][coord.Z];

            // Check if active; if not, activate it.
            if (chunk == null)
            {
                _chunks[coord.X][coord.Z] = new Chunk.Chunk(this, coord);
                _chunksToCreate.Add(coord);
            }
            else if (!chunk.IsActive)
            {
                chunk.IsActive = true;
            }

            _activeChunks.Add(coord);
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
                                 _noiseGenerator.Get2DNoise(xzPos, 0,
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
                        if (_noiseGenerator.Get3DSimplex(position,
                            lode.noiseOffset,
                            lode.scale, lode.threshold))
                        {
                            voxelValue = lode.blockId;
                        }
                    }
                }
            }

            return voxelValue;
        }


        private static bool IsChunkInWorld(ChunkCoordinate chunkCoordinate)
        {
            return IsChunkInWorld(chunkCoordinate.X, chunkCoordinate.Z);
        }

        private static bool IsChunkInWorld(int x, int z)
        {
            return x >= 0 && x <= VoxelData.WorldSizeInChunks - 1 && z >= 0 &&
                   z <= VoxelData.WorldSizeInChunks - 1;
        }

        public bool IsVoxelSolid(Vector3 worldPosition)
        {
            return IsVoxelSolid(worldPosition.x, worldPosition.y,
                worldPosition.z);
        }


        public bool IsVoxelSolid(float x, float y, float z)
        {
            if (!IsVoxelInWorld(x, y, z))
            {
                return false;
            }

            var coord = ChunkCoordinate.FromWorldPosition(x, z);

            // if (!IsChunkInWorld(coord) || y < 0 || y > VoxelData.ChunkHeight)
            // {
            //     return false;
            // }
            var chunk = _chunks[coord.X][coord.Z];

            if (chunk != null && chunk.Initialized)
            {
                return blockTypes[chunk.GetVoxelFromWorldPosition(x, y, z)]
                    .IsSolid;
            }

            return blockTypes[GetVoxel(new Vector3(x, y, z))].IsSolid;
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
            return face switch
            {
                VoxelData.Face.Back => BackTexture,
                VoxelData.Face.Front => FrontTexture,
                VoxelData.Face.Top => TopTexture,
                VoxelData.Face.Bottom => BottomTexture,
                VoxelData.Face.Left => LeftTexture,
                VoxelData.Face.Right => RightTexture,
                _ => throw new ArgumentException("Invalid Face: " + face)
            };
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