Subdivision Renderer
--------------------

Requires SlimDX, D3D11 and .NET 4.0. 

Controls can be changed in the Controls class. To load additional models, place them into the Models folder (only quadrilateral meshes are supported).

The default controls are:

Movement:
- WASD : Change camera postion along Z and X Axis.
- Space, Ctrl : Change camera position along Y Axis.
- Hold & Move Left Mouse : Change camera target.
- Arrow Keys : Rotate model.
- R : Reset camera position.

Settings:
- 1-4 : Change shader (Linear, Phong, PN Quads, ACC).
- Q : Increase tessellation factor.
- E : Decrease tessellation factor.
- X : Toggle wireframe.
- F : Toggle flat shading.
- T : Toggle texture.
- N : Toggle normal overlay.
- C : Switch to next model.