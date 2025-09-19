using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace StairGenerator
{
    public static class MeshGenerator
    {
        public static MeshGeometry3D GenerateSingleStairMesh(double overallHeight, double stepHeight, double stepLength, double stairWidth)
        {
            var mesh = new MeshGeometry3D();
            var positions = new Point3DCollection();
            var triangleIndices = new Int32Collection();
            var normals = new Vector3DCollection();

            // Convert to meters for display
            stepHeight /= 1000.0;
            stepLength /= 1000.0;
            stairWidth /= 1000.0;

            int stepCount = (int)Math.Ceiling(overallHeight / (stepHeight * 1000.0));
            double actualStepHeight = (overallHeight / 1000.0) / stepCount;

            for (int step = 0; step < stepCount; step++)
            {
                double currentHeight = step * actualStepHeight;
                double nextHeight = (step + 1) * actualStepHeight;
                double currentDepth = step * stepLength;
                double nextDepth = (step + 1) * stepLength;

                AddStepToMesh(positions, triangleIndices, normals,
                    currentHeight, nextHeight, currentDepth, nextDepth, stairWidth);
            }

            mesh.Positions = positions;
            mesh.TriangleIndices = triangleIndices;
            mesh.Normals = normals;
            return mesh;
        }

        public static MeshGeometry3D GenerateLinearStairwellMesh(List<StairLevel> stairLevels, double stepHeight, double stepLength, double stairWidth, double platformWidth, double platformDepth)
        {
            var mesh = new MeshGeometry3D();
            var positions = new Point3DCollection();
            var triangleIndices = new Int32Collection();
            var normals = new Vector3DCollection();

            // Convert to meters for display
            stepHeight /= 1000.0;
            stepLength /= 1000.0;
            stairWidth /= 1000.0;
            platformWidth /= 1000.0;
            platformDepth /= 1000.0;

            double currentZ = 0;
            double currentY = 0;

            for (int levelIndex = 0; levelIndex < stairLevels.Count; levelIndex++)
            {
                var level = stairLevels[levelIndex];
                bool isForward = levelIndex % 2 == 0;
                bool isLeftSide = levelIndex % 2 == 0; // Forward on left, backward on right

                // Calculate stair starting position
                double stairStartX = isLeftSide ? 0 : (platformWidth - stairWidth);
                
                // For stairs after the first level, adjust starting Z position to connect to platform
                double stairStartZ = currentZ;
                if (levelIndex > 0)
                {
                    // Start from the near edge of the platform (where previous stair ended)
                    if (isForward)
                        stairStartZ = currentZ + platformDepth;  // Start at front of platform
                    else
                        stairStartZ = currentZ - platformDepth;  // Start at back of platform
                }

                // Generate stairs for this level
                GenerateStairLevel(positions, triangleIndices, normals, 
                    stairStartX, stairStartZ, currentY, level.StepCount, stepHeight, stepLength, stairWidth, isForward);

                // Update position after stairs
                if (isForward)
                    currentZ = stairStartZ + level.StepCount * stepLength;
                else
                    currentZ = stairStartZ - level.StepCount * stepLength;
                
                currentY += level.StepCount * stepHeight;

                // Generate platform after stairs (except for the last level)
                if (levelIndex < stairLevels.Count - 1)
                {
                    // Platform should be one step height above the last step
                    double platformY = currentY + stepHeight;
                    GeneratePlatform(positions, triangleIndices, normals,
                        currentZ, platformY, platformWidth, platformDepth, stepHeight, isForward);

                    // Update position after platform (currentZ now points to far end of platform)
                    if (isForward)
                        currentZ += platformDepth;
                    else
                        currentZ -= platformDepth;
                }
            }

            mesh.Positions = positions;
            mesh.TriangleIndices = triangleIndices;
            mesh.Normals = normals;
            return mesh;
        }

        private static void GenerateStairLevel(Point3DCollection positions, Int32Collection triangleIndices, Vector3DCollection normals,
            double startX, double startZ, double startY, int stepCount, double stepHeight, double stepLength, double stairWidth, bool isForward)
        {
            for (int step = 0; step < stepCount; step++)
            {
                double currentHeight = startY + step * stepHeight;
                double nextHeight = startY + (step + 1) * stepHeight;
                
                double currentDepth, nextDepth;
                if (isForward)
                {
                    currentDepth = startZ + step * stepLength;
                    nextDepth = startZ + (step + 1) * stepLength;
                }
                else
                {
                    currentDepth = startZ - step * stepLength;
                    nextDepth = startZ - (step + 1) * stepLength;
                }

                AddStepToMeshWithOffset(positions, triangleIndices, normals,
                    currentHeight, nextHeight, currentDepth, nextDepth, stairWidth, startX, isForward);
            }
        }

        private static void GeneratePlatform(Point3DCollection positions, Int32Collection triangleIndices, Vector3DCollection normals,
            double startZ, double startY, double platformWidth, double platformDepth, double stepHeight, bool previousWasForward)
        {
            int baseIndex = positions.Count;

            double endZ = previousWasForward ? startZ + platformDepth : startZ - platformDepth;
            double minZ = Math.Min(startZ, endZ);
            double maxZ = Math.Max(startZ, endZ);
            double bottomY = startY - stepHeight;

            // Top surface vertices
            positions.Add(new Point3D(0, startY, minZ));           // 0
            positions.Add(new Point3D(platformWidth, startY, minZ)); // 1
            positions.Add(new Point3D(platformWidth, startY, maxZ)); // 2
            positions.Add(new Point3D(0, startY, maxZ));             // 3

            // Bottom surface vertices
            positions.Add(new Point3D(0, bottomY, minZ));           // 4
            positions.Add(new Point3D(platformWidth, bottomY, minZ)); // 5
            positions.Add(new Point3D(platformWidth, bottomY, maxZ)); // 6
            positions.Add(new Point3D(0, bottomY, maxZ));             // 7

            // Front face vertices (at maxZ)
            positions.Add(new Point3D(0, bottomY, maxZ));           // 8
            positions.Add(new Point3D(platformWidth, bottomY, maxZ)); // 9
            positions.Add(new Point3D(platformWidth, startY, maxZ)); // 10
            positions.Add(new Point3D(0, startY, maxZ));             // 11

            // Back face vertices (at minZ)
            positions.Add(new Point3D(0, bottomY, minZ));           // 12
            positions.Add(new Point3D(platformWidth, bottomY, minZ)); // 13
            positions.Add(new Point3D(platformWidth, startY, minZ)); // 14
            positions.Add(new Point3D(0, startY, minZ));             // 15

            // Left face vertices (at X=0)
            positions.Add(new Point3D(0, bottomY, minZ));           // 16
            positions.Add(new Point3D(0, bottomY, maxZ));           // 17
            positions.Add(new Point3D(0, startY, maxZ));             // 18
            positions.Add(new Point3D(0, startY, minZ));             // 19

            // Right face vertices (at X=platformWidth)
            positions.Add(new Point3D(platformWidth, bottomY, minZ)); // 20
            positions.Add(new Point3D(platformWidth, bottomY, maxZ)); // 21
            positions.Add(new Point3D(platformWidth, startY, maxZ)); // 22
            positions.Add(new Point3D(platformWidth, startY, minZ)); // 23

            // Add normals for all faces
            // Top surface normals (pointing up)
            var topNormal = new Vector3D(0, 1, 0);
            for (int i = 0; i < 4; i++) normals.Add(topNormal);

            // Bottom surface normals (pointing down)
            var bottomNormal = new Vector3D(0, -1, 0);
            for (int i = 0; i < 4; i++) normals.Add(bottomNormal);

            // Front face normals (pointing forward)
            var frontNormal = new Vector3D(0, 0, 1);
            for (int i = 0; i < 4; i++) normals.Add(frontNormal);

            // Back face normals (pointing backward)
            var backNormal = new Vector3D(0, 0, -1);
            for (int i = 0; i < 4; i++) normals.Add(backNormal);

            // Left face normals (pointing left)
            var leftNormal = new Vector3D(-1, 0, 0);
            for (int i = 0; i < 4; i++) normals.Add(leftNormal);

            // Right face normals (pointing right)
            var rightNormal = new Vector3D(1, 0, 0);
            for (int i = 0; i < 4; i++) normals.Add(rightNormal);

            // Add triangles for all faces
            // Top surface (reverse winding for upward-facing)
            AddQuadTriangles(triangleIndices, baseIndex + 0, baseIndex + 3, baseIndex + 2, baseIndex + 1);

            // Bottom surface
            AddQuadTriangles(triangleIndices, baseIndex + 4, baseIndex + 5, baseIndex + 6, baseIndex + 7);

            // Front face
            AddQuadTriangles(triangleIndices, baseIndex + 8, baseIndex + 11, baseIndex + 10, baseIndex + 9);

            // Back face
            AddQuadTriangles(triangleIndices, baseIndex + 12, baseIndex + 13, baseIndex + 14, baseIndex + 15);

            // Left face
            AddQuadTriangles(triangleIndices, baseIndex + 16, baseIndex + 19, baseIndex + 18, baseIndex + 17);

            // Right face
            AddQuadTriangles(triangleIndices, baseIndex + 20, baseIndex + 21, baseIndex + 22, baseIndex + 23);
        }

        private static void AddStepToMesh(Point3DCollection positions, Int32Collection triangleIndices, Vector3DCollection normals,
            double currentHeight, double nextHeight, double currentDepth, double nextDepth, double width)
        {
            int baseIndex = positions.Count;

            // Step top surface (rectangle)
            positions.Add(new Point3D(0, nextHeight, currentDepth));         // 0
            positions.Add(new Point3D(width, nextHeight, currentDepth));     // 1
            positions.Add(new Point3D(width, nextHeight, nextDepth));        // 2
            positions.Add(new Point3D(0, nextHeight, nextDepth));            // 3
            
            // Top surface normals (pointing up)
            var topNormal = new Vector3D(0, 1, 0);
            normals.Add(topNormal);
            normals.Add(topNormal);
            normals.Add(topNormal);
            normals.Add(topNormal);

            // Step front face (rectangle)
            positions.Add(new Point3D(0, currentHeight, currentDepth));      // 4
            positions.Add(new Point3D(width, currentHeight, currentDepth));  // 5
            positions.Add(new Point3D(width, nextHeight, currentDepth));     // 6
            positions.Add(new Point3D(0, nextHeight, currentDepth));         // 7
            
            // Front face normals (pointing forward)
            var frontNormal = new Vector3D(0, 0, -1);
            normals.Add(frontNormal);
            normals.Add(frontNormal);
            normals.Add(frontNormal);
            normals.Add(frontNormal);

            // Add triangles for top surface (reverse winding for upward-facing)
            AddQuadTriangles(triangleIndices, baseIndex + 0, baseIndex + 3, baseIndex + 2, baseIndex + 1);

            // Add triangles for front face
            AddQuadTriangles(triangleIndices, baseIndex + 4, baseIndex + 7, baseIndex + 6, baseIndex + 5);
        }

        private static void AddStepToMeshWithOffset(Point3DCollection positions, Int32Collection triangleIndices, Vector3DCollection normals,
            double currentHeight, double nextHeight, double currentDepth, double nextDepth, double width, double offsetX, bool isForward)
        {
            int baseIndex = positions.Count;

            // Step top surface (rectangle) - offset by startX
            positions.Add(new Point3D(offsetX, nextHeight, currentDepth));              // 0
            positions.Add(new Point3D(offsetX + width, nextHeight, currentDepth));     // 1
            positions.Add(new Point3D(offsetX + width, nextHeight, nextDepth));        // 2
            positions.Add(new Point3D(offsetX, nextHeight, nextDepth));                // 3
            
            // Top surface normals (pointing up)
            var topNormal = new Vector3D(0, 1, 0);
            normals.Add(topNormal);
            normals.Add(topNormal);
            normals.Add(topNormal);
            normals.Add(topNormal);

            // Step front face (rectangle) - offset by startX
            positions.Add(new Point3D(offsetX, currentHeight, currentDepth));          // 4
            positions.Add(new Point3D(offsetX + width, currentHeight, currentDepth));  // 5
            positions.Add(new Point3D(offsetX + width, nextHeight, currentDepth));     // 6
            positions.Add(new Point3D(offsetX, nextHeight, currentDepth));             // 7
            
            // Front face normals - use same normal for both directions
            var frontNormal = new Vector3D(0, 0, -1);
            normals.Add(frontNormal);
            normals.Add(frontNormal);
            normals.Add(frontNormal);
            normals.Add(frontNormal);

            // Add triangles for top surface (reverse winding for upward-facing)
            AddQuadTriangles(triangleIndices, baseIndex + 0, baseIndex + 3, baseIndex + 2, baseIndex + 1);

            // Add triangles for front face - reverse winding for backward stairs to fix lighting
            if (isForward)
                AddQuadTriangles(triangleIndices, baseIndex + 4, baseIndex + 7, baseIndex + 6, baseIndex + 5);
            else
                AddQuadTriangles(triangleIndices, baseIndex + 7, baseIndex + 4, baseIndex + 5, baseIndex + 6);
        }

        private static void AddQuadTriangles(Int32Collection triangleIndices, int p0, int p1, int p2, int p3)
        {
            // First triangle
            triangleIndices.Add(p0);
            triangleIndices.Add(p1);
            triangleIndices.Add(p2);

            // Second triangle
            triangleIndices.Add(p0);
            triangleIndices.Add(p2);
            triangleIndices.Add(p3);
        }
    }
}