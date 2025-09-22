using System.IO;
using System.Text;
using System.Windows.Media.Media3D;
using System.Globalization;

namespace StairGenerator
{
    public static class MeshExporter
    {
        public static void ExportToOBJ(MeshGeometry3D mesh, string filename)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Stair Generator OBJ Export - Units: meters");
            sb.AppendLine();

            // Export vertices in meters with high precision
            foreach (var position in mesh.Positions)
            {
                double x = position.X;
                double y = position.Y;
                double z = position.Z;
                // Use invariant culture to ensure period as decimal separator for OBJ standard
                sb.AppendLine($"v {x.ToString("F9", CultureInfo.InvariantCulture)} {y.ToString("F9", CultureInfo.InvariantCulture)} {z.ToString("F9", CultureInfo.InvariantCulture)}");
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