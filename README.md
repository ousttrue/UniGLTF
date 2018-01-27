# UniGLTF

[glTF](https://github.com/KhronosGroup/glTF) importer for Unity using [ScriptedImporter](https://docs.unity3d.com/ScriptReference/Experimental.AssetImporters.ScriptedImporter.html)

* Unity2017.3.0f3
* glTF-2.0

![duck](doc/duck.png)
![duck_assets](doc/duck_assets.png)
![animation](Recordings/animation.gif)

## Features & ToDo

* [x] gltf
* [x] glb
* [ ] Sample scene and model
* [ ] Unity-5.6 compatibility
* [x] Asset Importer/Exporter
* [x] Runtime Importer/Exporter

### material & texture

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|color        |o       |o       |/materials/#/pbrMetallicRoughness/baseColorFactor
|color texture|o       |o       |/materials/#/pbrMetallicRoughness/baseColorTexture
|sampler      |
|multi uv     |
|PBR          |

### mesh

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|positions    |o       |o       |/meshes/#/primitives/#/attributes/POSITION, reverse-z
|normals      |o       |o       |/meshes/#/primitives/#/attributes/NORMAL, reverse-z
|uv           |o       |o       |/meshes/#/primitives/#/attributes/TEXCOORD_0, reverse-y
|tangent      |        |        |/meshes/#/primitives/#/attributes/TANGENT, ?
|primitive    |o       |o       |todo:sharing attributes|

```cs
var mesh=new Mesh();
mesh.vertices = positions;
mesh.normals = normals;
mesh.uvs = uv;

mesh.submeshCount=primitiveCount;
for(int i=0; i<primitiveCount; ++i)
{
    mesh.setSubmesh(i, primitive[i].indices);
}    
```

#### morph

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|blend shape  |o       |        |/meshes/#/primitives/#/targets

```cs
mesh.setBlendShape
```

#### skin

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|boneweight   |o       |o       |/meshes/#/primitives/#/attributes/(JOINTS_0|WEIGHTS_0)
|bindmatrix   |o       |        |/skins/#/inverseBindMatrices
|skeleton     |        |        |/skins/#/skeleton
|joints       |o       |        |/skins/#/joints

```cs
mesh.boneWeights=boneWeights;
mesh.binds=inverseBindMatrices

skin=go.addCompoenent<SkinnedMeshRenderer>();
skin.bones
skin.root
```

### node

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|translation  |o       |o       |/nodes/#/tlanslation, reverse-z
|rotation     |o       |o       |/nodes/#/rotation, reverse-z
|scale        |o       |o       |/nodes/#/scale
|matrix       |o       |-       |/nodes/#/matrix, reverse-z

### animation

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|transform    |o       |        |/animations/#/channels/#/target/path/(tlanslation,rotation,scale), as generic AnimationClip

### not implemented

* Camera
* Light

## Humanoid Helper

* model position is origin
* model look at +z orientation
* model root node rotation is Quatenion.identity

![gizmo](doc/BoneMappingGizmo.png)
![inspector](doc/BoneMappingInspector.png)
![humanoid](Recordings/humanoid.gif)

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|avatar       |o       |        |
|avatar params|        |        |
|bone name    |        |        |rename to bone name

## Sample Models

* https://github.com/KhronosGroup/glTF-Sample-Models/tree/master/2.0

