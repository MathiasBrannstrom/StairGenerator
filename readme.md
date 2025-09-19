# StairGenerator

A Windows WPF application for generating 3D stair meshes with various configurations. Built with C# and .NET 8, featuring real-time 3D visualization and OBJ export functionality.

## Features

- **Real-time 3D Visualization**: Interactive 3D viewport with mouse controls
- **Multiple Stair Types**: Single stairs, linear stairwells, and rectangular spirals (in development)
- **Mesh Export**: Export generated stairs to OBJ format
- **Interactive Controls**: Mouse rotation, zoom, and parameter adjustment
- **Dynamic Level Management**: Add/remove stair levels with live updates

## Project Structure

```
StairGenerator/
├── MainWindow.xaml          # Main UI layout with tabbed interface
├── MainWindow.xaml.cs       # UI logic and event handling
├── Models.cs                # Data models and enums
├── MeshGenerator.cs         # 3D mesh generation logic
├── MeshExporter.cs          # OBJ file export functionality
├── StairGenerator.csproj    # Project configuration
└── README.md               # This file
```

## Architecture Overview

### Core Classes

#### `MainWindow.xaml.cs`
- **Purpose**: Main application window and UI management
- **Key Responsibilities**:
  - 3D camera controls (rotation, zoom, positioning)
  - Tab management for different stair types
  - Dynamic UI generation for level management
  - Input validation and error handling
  - Coordinate axes visualization

#### `MeshGenerator.cs`
- **Purpose**: Central hub for all 3D mesh generation
- **Key Methods**:
  - `GenerateSingleStairMesh()`: Basic straight staircase
  - `GenerateLinearStairwellMesh()`: Back-and-forth stairwell with platforms
  - `GenerateRectangularSpiralMesh()`: Placeholder for spiral stairs (needs refactoring)
- **Design Pattern**: Static methods with clear separation of concerns

#### `MeshExporter.cs`
- **Purpose**: Handle mesh export to various formats
- **Current Support**: OBJ format with vertex and face data
- **Unit Handling**: Converts from display units (meters) back to millimeters for export

#### `Models.cs`
- **Purpose**: Data structures and enums
- **Contents**:
  - `StairType`: Enum for stair types (Single, LinearStairwell, RectangularSpiral)
  - `SpiralDirection`: Enum for spiral directions (Forward, Left, Backward, Right)
  - `StairLevel`: Class representing a level with step count

### Coordinate System

- **X-axis**: Red, represents width (left-right)
- **Y-axis**: Green, represents height (up-down)
- **Z-axis**: Blue, represents depth (forward-backward)
- **Units**: Input in millimeters, display in meters, export back to millimeters

### 3D Rendering Details

#### Mesh Structure
- **Vertices**: Point3D coordinates in 3D space
- **Triangles**: Index-based triangle definitions for surfaces
- **Normals**: Surface normal vectors for proper lighting
- **Materials**: Diffuse materials with color variations

#### Lighting System
- **Direction**: Light comes from above (Direction="0,1,0")
- **Surface Normals**: Critical for proper lighting - incorrect winding causes dark surfaces
- **Triangle Winding**: Counter-clockwise for front-facing surfaces

#### Camera Controls
- **Rotation**: Spherical coordinate system (theta/phi angles)
- **Zoom**: Distance-based scaling with limits
- **Auto-positioning**: Automatic camera adjustment based on mesh bounds
- **Mouse Events**: Captured on Border container for full viewport interaction

## Implementation Details

### Single Stair Generation
- **Algorithm**: Sequential step placement with height/depth progression
- **Components**: Top surfaces and front faces only (no side walls)
- **Coordinate Calculation**: Linear progression with actual step height normalization

### Linear Stairwell Generation
- **Algorithm**: Alternating forward/backward flights with platforms
- **Direction Logic**: Even levels go forward (left side), odd levels go backward (right side)
- **Platform Positioning**: 
  - Platforms placed one step height above last step
  - X-position alternates between left (0) and right (platformWidth - stairWidth)
  - Z-position calculated based on platform depth and direction
- **Connection Logic**: Platforms connect stair flights with proper spacing

### Mesh Generation Patterns

#### Step Creation (`AddStepToMeshWithOffset`)
- **Top Surface**: 4 vertices forming rectangle at step height
- **Front Face**: 4 vertices forming vertical riser
- **Winding Order**: Critical for lighting - different for forward vs backward stairs
- **Offset Handling**: X-position offset for stair placement within platform width

#### Platform Creation (`GeneratePlatform`)
- **6-sided Blocks**: Complete rectangular platforms with all faces
- **Positioning**: Based on starting Z position and direction
- **Dimensions**: Configurable width and depth
- **Height**: One step above connecting stair level

## Known Issues & Technical Debt

### Rectangular Spiral (Needs Refactoring)
- **Current State**: Placeholder implementation removed due to positioning issues
- **Required Refactoring**: Need modular directional stair generation
- **Planned Approach**: Create reusable method for generating stairs in any direction from any starting point

### Lighting Issues
- **Backward Stairs**: Triangle winding partially fixed but may need further refinement
- **Normal Calculation**: Some surfaces may have incorrect normals affecting lighting

### Code Organization Opportunities
- **Mesh Generation**: Could benefit from more modular approach
- **Direction Handling**: Needs abstraction for better reusability
- **Platform Logic**: Complex positioning logic could be simplified

## Development Guidelines

### Adding New Stair Types
1. Add enum value to `StairType` in `Models.cs`
2. Create generation method in `MeshGenerator.cs`
3. Add UI tab in `MainWindow.xaml`
4. Implement validation and event handlers in `MainWindow.xaml.cs`
5. Update export logic if needed

### Mesh Generation Best Practices
- **Unit Consistency**: Always convert mm to meters for display
- **Triangle Winding**: Maintain counter-clockwise winding for front faces
- **Normal Vectors**: Ensure proper surface normals for lighting
- **Index Management**: Use `baseIndex` pattern for vertex indexing

### UI Development Guidelines
- **Parameter Validation**: Always validate user inputs with clear error messages
- **Dynamic Content**: Use programmatic UI generation for variable content (levels)
- **Event Handling**: Proper event subscription/unsubscription for dynamic controls

### Testing Approach
- **Visual Verification**: Use 3D viewport for immediate feedback
- **Export Testing**: Verify OBJ files in external 3D software
- **Parameter Edge Cases**: Test with extreme values and edge cases
- **Cross-platform**: Ensure Windows-specific dependencies are documented

## Future Development

### Planned Refactoring
1. **Directional Stair Generation**: Create `GenerateStairInDirection()` method
2. **Modular Platform System**: Separate platform generation with better positioning
3. **Material System**: Enhanced materials and textures
4. **Export Formats**: Add STL, PLY, and other 3D formats

### Feature Roadmap
- **Spiral Staircase**: Complete rectangular spiral implementation
- **Curved Stairs**: Support for curved and helical stairs
- **Railings**: Add handrail generation
- **Measurements**: Visual dimension display
- **Building Codes**: Validation against building code requirements

## Dependencies

- **.NET 8.0**: Target framework
- **WPF**: Windows Presentation Foundation for UI
- **System.Windows.Media.Media3D**: 3D graphics and mesh handling
- **Microsoft.Win32**: File dialogs for export functionality

## Build & Run

```bash
# Build the project
dotnet build

# Run the application
dotnet run
```

## Contributing

When contributing to this project:
1. Follow the existing code patterns and structure
2. Ensure proper triangle winding for new mesh generation
3. Test with various parameter combinations
4. Update this README when adding significant features
5. Consider the planned refactoring when making changes

## Technical Notes

- **Performance**: Large stair counts may impact real-time rendering
- **Memory Usage**: Mesh data stored in memory during generation and display
- **File Size**: OBJ exports can be large for complex stairwells
- **Platform Support**: Windows-only due to WPF dependency 