# CSharp DirectX Raytracing Tutorials
This repository contain tutorials demostrating how to use DirectX12 Raytracing with CSharp. The Nvidia original C++ tutorials can be found [here](https://github.com/NVIDIAGameWorks/DxrTutorials). The DirectX12 CSharp binding used was [Vortice](https://github.com/amerkoleci/Vortice.Windows).

## Requirements:

- A GPU that supports DXR (Such as NVIDIA's Volta or Turing hardware)
- Windows 10 RS5 (version 1809)
- [Windows 10 SDK version 1809 (10.0.17763.0)](https://developer.microsoft.com/en-us/windows/downloads/sdk-archive)
- Visual Studio 2019

## Tutorials

### [Tutorial 01 Create Window](01-CreateWindow/)

![alt Create Window](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/CreateWindow.png)

### [Tutorial 02 Initialize DXR](02-InitDXR/)

![alt Initialize DXR](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/InitializeDXR.png)

### [Tutorial 03 Acceleration Structure](03-AccelerationStructure/)

Nothing to show

### [Tutorial 04 Raytracing PipelineState](04-RtPipelineState/)

Nothing to show

### [Tutorial 05 Shader Table](05-ShaderTable/)

Nothing to show

### [Tutorial 06 Raytrace](06-Raytrace/)

Nothing to show

### [Tutorial 07 Basic Shaders](07-BasicShaders/)

![alt Draw triangle](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/DrawTriangle.png)

### [Tutorial 08 Instancing](08-Instancing/)

![alt Instancing](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/Instancing.png)

### [Tutorial 09 Constant Buffer](09-ConstantBuffer/)

![alt Color Constant Buffer](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/ConstantBuffer.png)

### [Tutorial 10 Per Instance Constant Buffer](10-PerInstanceConstantBuffer/)

![alt Individual Constant Buffer](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/ConstantBuffers.png)

### [Tutorial 11 Second Geometry](11-SecondGeometry/)

![alt Add plane geometry](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/Plane.png)

### [Tutorial 12 Per Geometry Hit Shader](12-PerGeometryHitShader/)

![alt Triangle and Plane HitShaders](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/TriangleAndPlaneHitShader.png)

### [Tutorial 13 Second Ray Type](13-SecondRayType/)

![alt Simple Shadow](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/Shadow.png)

### [Tutorial 14 Refit](14-Refit/)

![alt Rotate triangles](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/UpdateGeometryTransform.png)

## Extra Tutorials

After I ported Raytracing DXR Nvidia tutorials to CSharp I think that would be a great idea to extend theses tutorials with some more. So I am going to add new extra raytracing tutorials to explain how to create more complex raytracing scenes.

### [Tutorial 15 Primitives](15-Primitives/)

How to create a Raytracing Acceleration Structure from vertex and index geometry buffers.
![alt Primitives](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/Primitives.png)

### [Tutorial 16 Lighting](16-Lighting/)

How to lighting mesh using Raytracing pipeline. The acceleration Structures only have information about the vertex position of the mesh so we need to pass vertexBuffer and indexBuffer information to the shader to reconstruct the vertex information after a hit.
![alt Primitives](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/Lighting.png)

### [Tutorial 17 Shadow](17-Shadow/)

How to project shadows using Raytracing pipeline. In this tutorial, we are going to add a second geometry (ground) to the Acceleration Structure and throw a second ray to know whether a hit point is in shadow.
![alt Primitives](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/Shadow01.png)

### [Tutorial 18 Reflection](18-Reflection/)

How to make reflection meshes using Raytracing pipeline. In this tutorial, we are going to add a third reflected ray for each hit point recursively to calculate the final color (4 rebounds maximum deep). I also added fresnel and visibility falloff to improve the render. Most of the important changes are in the shader.
![alt Primitives](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/Reflection.png)


### [Tutorial 19 xxx] under construction


### [Tutorial 20 Instancing](20-Scene/)

How to do instancing using Raytracing pipeline. In this tutorial, we are going to use the Top Level Acceleration Structure to instance multiple geometries defined in the Bottom Level Acceleration Structure and create a scene. In addition, we will add two constant buffers (Scene parameters and Primitive parameters) to pass scene configuration to shader.
![alt Primitives](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/InstancingScene.png)


### [Tutorial 21 Materials](21-Scene-PerInstanceConstantBuffer/)

How to provide a constant buffer per instance defined in the Top Level AS using the Raytracing pipeline. In this tutorial, we are going to see a mechanism to bind different resources to different instances of the same geometry. It will be useful to provide different materials for our Raytracing scenes.
![alt Primitives](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/ConstantBufferPerInstance.png)

### [Tutorial 22 Load Models](22-LoadModel/)

How to load a Gltf model and render with DirectX12 Raytracing pipeline. Acceleration structure only stores vertex positions so you need pass Index and Vertex buffer to the shader to figure out another vertex attributes (Normals, Texcoords, etc.) when a ray hits a model's triangle.
![alt Primitives](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/GLTFModel.png)


### [Tutorial 23 Load Textures](23-LoadTexture/)

How to load a texture and applies to our gltf model with DirectX12 Raytracing pipeline. Acceleration structure only stores vertex positions so you need pass Index and Vertex buffer to the shader to figure out another vertex attributes (Normals, Texcoords, etc.) when a ray hits a model's triangle.
![alt Primitives](https://github.com/Jorgemagic/CSharpDirectXRaytracing/blob/master/Screenshots/LoadTextures.png)