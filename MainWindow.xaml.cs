using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using System.IO;

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
        private List<StairLevel> stairLevels = new List<StairLevel> { new StairLevel(), new StairLevel() };

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
            
            // Initialize level UI
            RefreshLevelsUI();
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
                var selectedStairType = GetSelectedStairType();
                
                switch (selectedStairType)
                {
                    case StairType.Single:
                        GenerateSingleStair();
                        break;
                    case StairType.LinearStairwell:
                        GenerateLinearStairwell();
                        break;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        private StairType GetSelectedStairType()
        {
            if (StairTypeTabControl.SelectedItem == SingleStairTab)
                return StairType.Single;
            else if (StairTypeTabControl.SelectedItem == LinearStairwellTab)
                return StairType.LinearStairwell;
            else
                return StairType.Single; // Default
        }

        private void GenerateSingleStair()
        {
            if (!ValidateInputs(out double overallHeight, out double stepHeight, out double stepLength, out double stairWidth))
                return;

            currentStairMesh = MeshGenerator.GenerateSingleStairMesh(overallHeight, stepHeight, stepLength, stairWidth);
            DisplayStair(currentStairMesh);
            
            int stepCount = (int)Math.Ceiling(overallHeight / stepHeight);
            StatusTextBlock.Text = $"Generated single stair with {stepCount} steps";
        }

        private void GenerateLinearStairwell()
        {
            if (!ValidateLinearStairwellInputs(out double stepHeight, out double stepLength, out double stairWidth, out double platformWidth, out double platformDepth))
                return;

            bool clockwise = ClockwiseCheckBox.IsChecked ?? true;
            currentStairMesh = MeshGenerator.GenerateLinearStairwellMesh(stairLevels, stepHeight, stepLength, stairWidth, platformWidth, platformDepth, clockwise);
            DisplayStair(currentStairMesh);

            int totalSteps = stairLevels.Sum(level => level.StepCount);
            string direction = clockwise ? "clockwise" : "counter-clockwise";
            StatusTextBlock.Text = $"Generated {direction} linear stairwell with {stairLevels.Count} levels, {totalSteps} steps";
        }

        private bool ValidateLinearStairwellInputs(out double stepHeight, out double stepLength, out double stairWidth, out double platformWidth, out double platformDepth)
        {
            stepHeight = stepLength = stairWidth = platformWidth = platformDepth = 0;

            if (!double.TryParse(LinearStepHeightTextBox.Text, out stepHeight) || stepHeight <= 0)
            {
                StatusTextBlock.Text = "Invalid step height";
                return false;
            }

            if (!double.TryParse(LinearStepLengthTextBox.Text, out stepLength) || stepLength <= 0)
            {
                StatusTextBlock.Text = "Invalid step length";
                return false;
            }

            if (!double.TryParse(LinearStairWidthTextBox.Text, out stairWidth) || stairWidth <= 0)
            {
                StatusTextBlock.Text = "Invalid stair width";
                return false;
            }

            if (!double.TryParse(PlatformWidthTextBox.Text, out platformWidth) || platformWidth <= 0)
            {
                StatusTextBlock.Text = "Invalid platform width";
                return false;
            }

            if (!double.TryParse(PlatformDepthTextBox.Text, out platformDepth) || platformDepth <= 0)
            {
                StatusTextBlock.Text = "Invalid platform depth";
                return false;
            }

            return true;
        }






        private void AddLevelButton_Click(object sender, RoutedEventArgs e)
        {
            stairLevels.Add(new StairLevel());
            RefreshLevelsUI();
            UpdateTotalHeight();
        }

        private void RemoveLevelButton_Click(object sender, RoutedEventArgs e)
        {
            if (stairLevels.Count > 1)
            {
                stairLevels.RemoveAt(stairLevels.Count - 1);
                RefreshLevelsUI();
                UpdateTotalHeight();
            }
        }

        private void RefreshLevelsUI()
        {
            LevelsStackPanel.Children.Clear();
            
            for (int i = 0; i < stairLevels.Count; i++)
            {
                var levelPanel = CreateLevelPanel(i);
                LevelsStackPanel.Children.Add(levelPanel);
            }
            
            UpdateTotalHeight();
        }

        private StackPanel CreateLevelPanel(int levelIndex)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            
            // Level label
            var label = new TextBlock 
            { 
                Text = $"Level {levelIndex + 1}:", 
                Width = 60, 
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Steps textbox
            var stepsLabel = new TextBlock 
            { 
                Text = "Steps:", 
                Margin = new Thickness(5, 0, 5, 0), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            
            var stepsTextBox = new TextBox 
            { 
                Text = stairLevels[levelIndex].StepCount.ToString(), 
                Width = 40, 
                Tag = levelIndex 
            };
            stepsTextBox.TextChanged += StepsTextBox_TextChanged;
            
            panel.Children.Add(label);
            panel.Children.Add(stepsLabel);
            panel.Children.Add(stepsTextBox);
            
            return panel;
        }

        private void StepsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is int levelIndex)
            {
                if (int.TryParse(textBox.Text, out int steps) && steps > 0)
                {
                    stairLevels[levelIndex].StepCount = steps;
                    UpdateTotalHeight();
                }
            }
        }


        private void UpdateTotalHeight()
        {
            if (double.TryParse(LinearStepHeightTextBox?.Text ?? "180", out double stepHeight))
            {
                int totalSteps = stairLevels.Sum(level => level.StepCount);
                double totalHeight = totalSteps * stepHeight;
                TotalHeightTextBlock.Text = $"Total Height: {totalHeight:F0} mm ({totalSteps} steps)";
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



        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentStairMesh == null)
            {
                StatusTextBlock.Text = "No stair to export. Generate a stair first.";
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "OBJ Files (*.obj)|*.obj|All Files (*.*)|*.*",
                DefaultExt = "obj"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    MeshExporter.ExportToOBJ(currentStairMesh, saveDialog.FileName);
                    StatusTextBlock.Text = $"Exported to {Path.GetFileName(saveDialog.FileName)}";
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = $"Export failed: {ex.Message}";
                }
            }
        }


    }
}