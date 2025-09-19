using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using System.IO;
using System.Text;

namespace StairGenerator
{
    public partial class MainWindow : Window
    {
        private MeshGeometry3D? currentStairMesh;
        private bool isRotating = false;
        private Point lastMousePosition;
        private double cameraDistance = 5.0;
        private double cameraTheta = 45.0; // Horizontal rotation angle
        private double cameraPhi = 30.0;   // Vertical rotation angle
        private Point3D modelCenter = new Point3D(0, 0, 0);

        public MainWindow()
        {
            InitializeComponent();
            
            // Add mouse event handlers for 3D rotation
            ViewportContainer.MouseDown += Viewport_MouseDown;
            ViewportContainer.MouseMove += Viewport_MouseMove;
            ViewportContainer.MouseUp += Viewport_MouseUp;
            ViewportContainer.MouseWheel += Viewport_MouseWheel;
            
            // Add coordinate axes
            CreateCoordinateAxes();
        }

        private void CreateCoordinateAxes()
        {
            var axesGroup = new Model3DGroup();
            
            // X-axis (Red)
            axesGroup.Children.Add(CreateAxisLine(new Point3D(0, 0, 0), new Point3D(1, 0, 0), Colors.Red));
            
            // Y-axis (Green) 
            axesGroup.Children.Add(CreateAxisLine(new Point3D(0, 0, 0), new Point3D(0, 1, 0), Colors.Green));
            
            // Z-axis (Blue)
            axesGroup.Children.Add(CreateAxisLine(new Point3D(0, 0, 0), new Point3D(0, 0, 1), Colors.Blue));
            
            var axesVisual = new ModelVisual3D();
            axesVisual.Content = axesGroup;
            
            MainViewport.Children.Add(axesVisual);
        }

        private GeometryModel3D CreateAxisLine(Point3D start, Point3D end, Color color)
        {
            var mesh = new MeshGeometry3D();
            var positions = new Point3DCollection();
            var triangleIndices = new Int32Collection();
            
            // Create a thin cylinder for the axis line
            double radius = 0.02;
            int segments = 8;
            
            var direction = end - start;
            var length = direction.Length;
            direction.Normalize();
            
            // Create cylinder vertices
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                
                // Bottom circle
                positions.Add(new Point3D(start.X + x, start.Y + z, start.Z));
                // Top circle  
                positions.Add(new Point3D(end.X + x, end.Y + z, end.Z));
            }
            
            // Create triangles for cylinder sides
            for (int i = 0; i < segments; i++)
            {
                int current = i * 2;
                int next = ((i + 1) % (segments + 1)) * 2;
                
                // First triangle
                triangleIndices.Add(current);
                triangleIndices.Add(current + 1);
                triangleIndices.Add(next);
                
                // Second triangle
                triangleIndices.Add(current + 1);
                triangleIndices.Add(next + 1);
                triangleIndices.Add(next);
            }
            
            mesh.Positions = positions;
            mesh.TriangleIndices = triangleIndices;
            
            var material = new DiffuseMaterial(new SolidColorBrush(color));
            return new GeometryModel3D(mesh, material);
        }

        private void Viewport_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                isRotating = true;
                lastMousePosition = e.GetPosition(ViewportContainer);
                ViewportContainer.CaptureMouse();
            }
        }

        private void Viewport_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isRotating && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(ViewportContainer);
                var deltaX = currentPosition.X - lastMousePosition.X;
                var deltaY = currentPosition.Y - lastMousePosition.Y;

                // Update rotation angles
                cameraTheta += deltaX * 0.5; // Horizontal rotation
                cameraPhi -= deltaY * 0.5;   // Vertical rotation

                // Clamp vertical rotation to prevent flipping
                cameraPhi = Math.Max(-89, Math.Min(89, cameraPhi));

                UpdateCameraPosition();
                lastMousePosition = currentPosition;
            }
        }

        private void Viewport_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isRotating)
            {
                isRotating = false;
                ViewportContainer.ReleaseMouseCapture();
            }
        }

        private void Viewport_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Zoom in/out
            var zoomFactor = e.Delta > 0 ? 0.9 : 1.1;
            cameraDistance *= zoomFactor;
            cameraDistance = Math.Max(0.5, Math.Min(50, cameraDistance)); // Clamp distance
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            var camera = (PerspectiveCamera)MainViewport.Camera;
            
            // Convert spherical coordinates to Cartesian
            var thetaRad = cameraTheta * Math.PI / 180.0;
            var phiRad = cameraPhi * Math.PI / 180.0;
            
            var x = modelCenter.X + cameraDistance * Math.Cos(phiRad) * Math.Cos(thetaRad);
            var y = modelCenter.Y + cameraDistance * Math.Sin(phiRad);
            var z = modelCenter.Z + cameraDistance * Math.Cos(phiRad) * Math.Sin(thetaRad);
            
            camera.Position = new Point3D(x, y, z);
            camera.LookDirection = new Vector3D(modelCenter.X - x, modelCenter.Y - y, modelCenter.Z - z);
            camera.UpDirection = new Vector3D(0, 1, 0);
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInputs(out double overallHeight, out double stepHeight, out double stepLength, out double stairWidth))
                    return;

                currentStairMesh = GenerateStairMesh(overallHeight, stepHeight, stepLength, stairWidth);
                DisplayStair(currentStairMesh);
                
                int stepCount = (int)Math.Ceiling(overallHeight / stepHeight);
                StatusTextBlock.Text = $"Generated stair with {stepCount} steps";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        private bool ValidateInputs(out double overallHeight, out double stepHeight, out double stepLength, out double stairWidth)
        {
            overallHeight = stepHeight = stepLength = stairWidth = 0;

            if (!double.TryParse(OverallHeightTextBox.Text, out overallHeight) || overallHeight <= 0)
            {
                StatusTextBlock.Text = "Invalid overall height";
                return false;
            }

            if (!double.TryParse(StepHeightTextBox.Text, out stepHeight) || stepHeight <= 0)
            {
                StatusTextBlock.Text = "Invalid step height";
                return false;
            }

            if (!double.TryParse(StepLengthTextBox.Text, out stepLength) || stepLength <= 0)
            {
                StatusTextBlock.Text = "Invalid step length";
                return false;
            }

            if (!double.TryParse(StairWidthTextBox.Text, out stairWidth) || stairWidth <= 0)
            {
                StatusTextBlock.Text = "Invalid stair width";
                return false;
            }

            return true;
        }

        private MeshGeometry3D GenerateStairMesh(double overallHeight, double stepHeight, double stepLength, double stairWidth)
        {
            var mesh = new MeshGeometry3D();
            var positions = new Point3DCollection();
            var triangleIndices = new Int32Collection();
            var normals = new Vector3DCollection();

            int stepCount = (int)Math.Ceiling(overallHeight / stepHeight);
            double actualStepHeight = overallHeight / stepCount;

            for (int step = 0; step < stepCount; step++)
            {
                double currentHeight = step * actualStepHeight;
                double nextHeight = (step + 1) * actualStepHeight;
                double currentDepth = step * stepLength / 1000.0; // Convert to meters for display
                double nextDepth = (step + 1) * stepLength / 1000.0;

                // Create step geometry
                AddStepToMesh(positions, triangleIndices, normals,
                    currentHeight / 1000.0, nextHeight / 1000.0, 
                    currentDepth, nextDepth, 
                    stairWidth / 1000.0);
            }

            mesh.Positions = positions;
            mesh.TriangleIndices = triangleIndices;
            mesh.Normals = normals;
            return mesh;
        }

        private void AddStepToMesh(Point3DCollection positions, Int32Collection triangleIndices, Vector3DCollection normals,
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

        private void AddQuadTriangles(Int32Collection triangleIndices, int p0, int p1, int p2, int p3)
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

        private void DisplayStair(MeshGeometry3D mesh)
        {
            var frontMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.LightGray));
            var backMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DarkGray));
            var materialGroup = new MaterialGroup();
            materialGroup.Children.Add(frontMaterial);
            materialGroup.Children.Add(backMaterial);
            
            var geometry = new GeometryModel3D(mesh, materialGroup);
            geometry.BackMaterial = backMaterial;
            
            StairModelVisual.Content = geometry;

            // Auto-adjust camera position based on stair size
            AdjustCamera(mesh);
        }

        private void AdjustCamera(MeshGeometry3D mesh)
        {
            if (mesh.Positions.Count == 0) return;

            var bounds = mesh.Bounds;
            
            // Calculate model center
            modelCenter = new Point3D(
                bounds.X + bounds.SizeX / 2,
                bounds.Y + bounds.SizeY / 2,
                bounds.Z + bounds.SizeZ / 2
            );
            
            // Calculate appropriate distance
            double maxDimension = Math.Max(Math.Max(bounds.SizeX, bounds.SizeY), bounds.SizeZ);
            cameraDistance = maxDimension * 2;
            
            // Reset rotation angles for new model
            cameraTheta = 45.0;
            cameraPhi = 30.0;
            
            UpdateCameraPosition();
        }

        private MeshGeometry3D GenerateStairMeshForExport(double overallHeight, double stepHeight, double stepLength, double stairWidth)
        {
            var mesh = new MeshGeometry3D();
            var positions = new Point3DCollection();
            var triangleIndices = new Int32Collection();

            int stepCount = (int)Math.Ceiling(overallHeight / stepHeight);
            double actualStepHeight = overallHeight / stepCount;

            for (int step = 0; step < stepCount; step++)
            {
                double currentHeight = step * actualStepHeight;
                double nextHeight = (step + 1) * actualStepHeight;
                double currentDepth = step * stepLength; // Keep in millimeters
                double nextDepth = (step + 1) * stepLength;

                // Create step geometry in millimeters
                AddStepToExportMesh(positions, triangleIndices, 
                    currentHeight, nextHeight, 
                    currentDepth, nextDepth, 
                    stairWidth);
            }

            mesh.Positions = positions;
            mesh.TriangleIndices = triangleIndices;
            return mesh;
        }

        private void AddStepToExportMesh(Point3DCollection positions, Int32Collection triangleIndices,
            double currentHeight, double nextHeight, double currentDepth, double nextDepth, double width)
        {
            int baseIndex = positions.Count;

            // Step top surface (rectangle)
            positions.Add(new Point3D(0, nextHeight, currentDepth));         // 0
            positions.Add(new Point3D(width, nextHeight, currentDepth));     // 1
            positions.Add(new Point3D(width, nextHeight, nextDepth));        // 2
            positions.Add(new Point3D(0, nextHeight, nextDepth));            // 3

            // Step front face (rectangle)
            positions.Add(new Point3D(0, currentHeight, currentDepth));      // 4
            positions.Add(new Point3D(width, currentHeight, currentDepth));  // 5
            positions.Add(new Point3D(width, nextHeight, currentDepth));     // 6
            positions.Add(new Point3D(0, nextHeight, currentDepth));         // 7

            // Add triangles for top surface (reverse winding for upward-facing)
            AddQuadTriangles(triangleIndices, baseIndex + 0, baseIndex + 3, baseIndex + 2, baseIndex + 1);

            // Add triangles for front face
            AddQuadTriangles(triangleIndices, baseIndex + 4, baseIndex + 7, baseIndex + 6, baseIndex + 5);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentStairMesh == null)
            {
                StatusTextBlock.Text = "No stair to export. Generate a stair first.";
                return;
            }

            if (!ValidateInputs(out double overallHeight, out double stepHeight, out double stepLength, out double stairWidth))
                return;

            var saveDialog = new SaveFileDialog
            {
                Filter = "OBJ Files (*.obj)|*.obj|All Files (*.*)|*.*",
                DefaultExt = "obj"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // Generate mesh in original millimeter scale for export
                    var exportMesh = GenerateStairMeshForExport(overallHeight, stepHeight, stepLength, stairWidth);
                    ExportToOBJ(exportMesh, saveDialog.FileName);
                    StatusTextBlock.Text = $"Exported to {Path.GetFileName(saveDialog.FileName)}";
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = $"Export failed: {ex.Message}";
                }
            }
        }

        private void ExportToOBJ(MeshGeometry3D mesh, string filename)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Stair Generator OBJ Export");
            sb.AppendLine();

            // Export vertices
            foreach (var position in mesh.Positions)
            {
                sb.AppendLine($"v {position.X:F6} {position.Y:F6} {position.Z:F6}");
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