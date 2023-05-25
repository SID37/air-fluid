# Reference

The package implements a fluid simulation similar to what is described in [[1]](#reference-links). In fact, viscosity is not implemented now (and is not planned), so this is Euler fluid.

## API

### AirFluid.AirFluid

The main class that stores the simulation vector field in the form of 3d textures. Textures can be accessed in the VFX graph via AirFluidBinder.

| Property | Description |
|----------|-------------|
| Blocks | The size of the simulation area in blocks, each block is a grid of 16x16x16 voxels and initially has a side length of 1 meter. The size of the simulation can be changed by changing the Scale field in Transform, but it needs to be changed in all directions |
| Iterations | The number of steps of the projection stage. This property directly affects the performance and quality of the simulation, for a more detailed explanation, see [[2]](#reference-links) |

### AirFluid.AirWindSource

Wind source, requires a collider trigger on the object. (Implemented SphereCollider, BoxCollider and CapsuleCollider).

| Property | Description |
|----------|-------------|
| Force | Binding to a VFX variable of type Transform |

### AirFluid.AirFluidBinder

Implements VFXBinderBase.

| Property | Description |
|----------|-------------|
| Transform Property | Binding to a VFX variable of type Transform |
| Field Property     | Binding to a VFX variable of type Texture3D |
| Fluids             | Input variable of the type AirFluid.AirFluid, which should be bound to an object on the scene|

## Reference links

1. J. Stam. Stable Fluids. In SIGGRAPH 99 Conference Proceedings, Annual Conference Series, August 1999, 121-128.

1. J. Stam. Real-time fluid dynamics for games. Proceedings of the game developer confer-ence, vol 18, 2003.
