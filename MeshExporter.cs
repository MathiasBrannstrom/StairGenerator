using System.IO;
using System.Text;
using System.Windows.Media.Media3D;

namespace StairGenerator
{
    public static class MeshExporter
    {
        public static void ExportToOBJ(MeshGeometry3D mesh, string filename)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Stair Generator OBJ Export");
            sb.AppendLine();

            // Export vertices - convert from meters back to millimeters for export
            foreach (var position in mesh.Positions)
            {
                double x = position.X * 1000.0; // Convert back to mm
                double y = position.Y * 1000.0;
                double z = position.Z * 1000.0;
                sb.AppendLine($"v {x:F6} {y:F6} {z:F6}");
            }

            sb.AppendLine();

            // Export faces (triangles)
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                int v1 = mesh.TriangleIndices[i] + 1;     // OBJ is 1-indexed
                int v2 = mesh.TriangleIndices[i + 1] + 1;
                int v3 = mesh.TriangleIndices[i + 2] + 1;
                sb.AppendLine($"f {v1} {v2} {v3}");
            }

            File.WriteAllText(filename, sb.ToString());
        }
    }
}