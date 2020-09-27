using System;
using System.Collections.Generic;

namespace CompanyNine.Voxel.Chunk
{
    public static class ChunkUtility
    {
        /// <summary>
        /// Calculate the necessary new chunks to load based on the previous
        /// coordinate and the current coordinate.
        /// </summary>
        /// <param name="previousCoordinate">The coordinate we started in. Cannot be null.</param>
        /// <param name="currentCoordinate">The coordinate we are currently in. Cannot be null.</param>
        public static IEnumerable<ChunkCoordinate> LoadChunks(ChunkCoordinate
            previousCoordinate, ChunkCoordinate currentCoordinate)
        {
            return FindChunks(previousCoordinate, currentCoordinate);
        }

        /// <summary>
        /// Calculate the necessary new chunks to unload based on the previous
        /// coordinate and the current coordinate.
        /// <param name="previousCoordinate">The coordinate we started in. Cannot be null.</param>
        /// <param name="currentCoordinate">The coordinate we are currently in. Cannot be null.</param>
        /// </summary>
        public static IEnumerable<ChunkCoordinate> UnloadChunks(ChunkCoordinate
            previousCoordinate, ChunkCoordinate currentCoordinate)
        {
            return FindChunks(currentCoordinate, previousCoordinate);
        }

        private static IEnumerable<ChunkCoordinate> FindChunks(ChunkCoordinate
            startingCoordinate, ChunkCoordinate endingCoordinate)
        {
            var distance = endingCoordinate - startingCoordinate;
            var xDirection =
                Math.Sign(endingCoordinate.X - startingCoordinate.X);
            var zDirection =
                Math.Sign(endingCoordinate.Z - startingCoordinate.Z);

            if (xDirection == 0) // we only moved in the z direction
            {
                return FindChunksInDirection(endingCoordinate, distance.Z,
                    zDirection,
                    ChunkAxis.Z);
            }

            if (zDirection == 0) // we moved only in the x direction
            {
                return FindChunksInDirection(endingCoordinate, distance.X,
                    xDirection,
                    ChunkAxis.X);
            }

            // we moved diagonally
            var chunkCoordinates = FindChunksInDirection(endingCoordinate,
                distance.X, xDirection, ChunkAxis.X);
            chunkCoordinates.UnionWith(FindChunksInDirection(
                endingCoordinate,
                distance.Z, zDirection, ChunkAxis.Z));

            return chunkCoordinates;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="distance"></param>
        /// <param name="direction">Represents the direction we are moving in, must be -1 or 1.</param>
        /// <param name="directionAxis"></param>
        private static HashSet<ChunkCoordinate> FindChunksInDirection(
            ChunkCoordinate coordinate, int distance, int direction,
            ChunkAxis directionAxis)
        {
            var coords = new HashSet<ChunkCoordinate>();

            int rangeCoordinate;
            int directionCoordinate;
            if (directionAxis == ChunkAxis.X)
            {
                rangeCoordinate = coordinate.Z;
                directionCoordinate = coordinate.X;
            }
            else
            {
                rangeCoordinate = coordinate.X;
                directionCoordinate = coordinate.Z;
            }

            // if distance is only one then don't need to do any calculations
            if (Math.Abs(distance) == 1)
            {
                for (var r = rangeCoordinate - VoxelData.ViewDistance;
                    r <= rangeCoordinate + VoxelData.ViewDistance;
                    r++)
                {
                    var d = directionCoordinate +
                            VoxelData.ViewDistance * direction;
                    coords.Add(ConstructCoordinate(directionAxis, d, r));
                }
            }
            else
            {
                // The edge of the active chunks we are heading towards.
                var leadingEdge = DetermineLeadingEdge(distance,
                    directionCoordinate, direction);
                var farEdge = directionCoordinate +
                              VoxelData.ViewDistance * direction;

                var minEdge = Math.Min(leadingEdge, farEdge);
                var maxEdge = Math.Max(leadingEdge, farEdge);

                for (var r = rangeCoordinate - VoxelData.ViewDistance;
                    r <= rangeCoordinate + VoxelData.ViewDistance;
                    r++)
                {
                    for (var d = minEdge;
                        d <= maxEdge;
                        d++)
                    {
                        coords.Add(ConstructCoordinate(directionAxis, d, r));
                    }
                }
            }

            return coords;
        }

        /// <summary>
        /// Determines the leading edge for chunk loading calculations.
        ///
        /// Note that all parameters are assumed to be on the same axis.
        /// </summary>
        /// <param name="distance"> The distance moved on the axis.</param>
        /// <param name="coordinate">The current coordinate on this axis.</param>
        /// <param name="direction">The direction of movement on this axis.</param>
        /// <returns></returns>
        private static int DetermineLeadingEdge(int distance, int coordinate,
            int direction)
        {
            return Math.Abs(distance) > VoxelData.AbsoluteDistanceLength
                ? coordinate - VoxelData.ViewDistance * direction
                : coordinate - (VoxelData.ViewDistance * direction)
                + (VoxelData.AbsoluteDistanceLength * direction) - distance;
        }


        private static ChunkCoordinate ConstructCoordinate(
            ChunkAxis directionAxis,
            int d, int r)
        {
            if (directionAxis == ChunkAxis.X)
            {
                return ChunkCoordinate.Of(d, r);
            }

            var coord = ChunkCoordinate.Of(r, d);
            return coord;
        }
    }
}