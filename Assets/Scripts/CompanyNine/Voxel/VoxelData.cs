using System.Collections.Generic;
using UnityEngine;

public static class VoxelData
{
    public const int ChunkWidth = 10;
    public const int ChunkHeight = 25;

    public const int WorldSizeInChunks = 100;
    public const long WorldSizeInVoxels = WorldSizeInChunks * ChunkWidth;

    public const int TextureAtlasSizeInBlocks = 4;
    public static float NormalizedBlockTextureSize => 1f / (float) TextureAtlasSizeInBlocks;

    /// <summary>
    /// View Distance in Chunks
    /// </summary>
    public const int ViewDistance = 10;
    
    public const int AbsoluteDistanceLength = ViewDistance * 2 + 1;
    
    public static readonly Vector3[] VoxelVertices =
    {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),
    };

    /**
     * Direction for each face to check if another voxel rests against it or not.
     * Note: Order is always Back, Front, Top, Bottom, Left, Right
     */
    public static readonly Dictionary<Face, Vector3Int> ChecksByFace = new Dictionary<Face, Vector3Int>()
    {
        {Face.Back, new Vector3Int(0, 0, -1)},
        {Face.Front, new Vector3Int(0, 0, 1)},
        {Face.Top, new Vector3Int(0, 1, 0)},
        {Face.Bottom, new Vector3Int(0, -1, 0)},
        {Face.Left, new Vector3Int(-1, 0, 0)},
        {Face.Right, new Vector3Int(1, 0, 0)}
    };

    /**
     * Describes how to draw each face using two triangles per face (6 faces per cube). Each face is desribed in the
     * same manner. The bottom triangle is listed in the following clockwise order: bottom-left vertex, top-left 
     * vertex, bottom-right vertex. The second triangle is described in the following clockwise order: 
     * bottom-right vertex, top-left vertex, top-right vertex. As bottom-right, and top-left are both repeated
     * they do not need to be stored in the face array.
     * <p>
     * Note: Order is always Back, Front, Top, Bottom, Left, Right
     */
    public static readonly int[,] VoxelTriangles =
    {
        {0, 3, 1, 2}, // Back Face
        {5, 6, 4, 7}, // Front Face
        {3, 7, 2, 6}, // Top Face
        {1, 5, 0, 4}, // Bottom Face
        {4, 7, 0, 3}, // Left Face
        {1, 2, 5, 6} // Right Face
    };

    public enum Face
    {
        Back,
        Front,
        Top,
        Bottom,
        Left,
        Right
    }
}