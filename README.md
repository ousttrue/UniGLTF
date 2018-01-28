# UniGLTF

[glTF](https://github.com/KhronosGroup/glTF) importer for Unity using [ScriptedImporter](https://docs.unity3d.com/ScriptReference/Experimental.AssetImporters.ScriptedImporter.html)

* Unityt5.6.3
* Unity2017.3.0f3
* glTF-2.0

![duck](doc/duck.png)
![duck_assets](doc/duck_assets.png)
![animation](Recordings/animation.gif)

## Issues

* crash unity when reimport. workaround, move target file to out of Assets. Launch unity, then move the file to Assets folder.

## Coordinate(Right-handed and Left-handed)

* Both GLTF and Unity is, x-right and y-up.
* GLTF is z-backward
* Unity is z-forward

```cs
Vector3 reversed = new Vector3(src.x, src.y, -src.z);
```

```cs
float angle;
Vector3 axis;
src.ToAngleAxis(out angle, out axis);
Quaternion reversed = Quaternion.AngleAxis(-angle, new Vector3(axis.x, aixs.y, -axis.z));
```

```cs
Matrix reversed = src;
m.m20 *= -1;
m.m21 *= -1;
m.m22 *= -1;
m.m23 *= -1;
```

This should be done in global coordinate, but animation curve contains local coordinate.

## Importer

* [x] asset(ScriptedImporter) (Unity-2017 or new)
* [x] asset(AssetPostprocessor.OnPostprocessAllAssets) (Unity-5.6)
* runtime [Assets] - [gltf] - [import]

## Exporter

* asset([right click] - [gltf] - [export]
* runtime
* [validation](http://github.khronos.org/glTF-Validator/)

## Features & ToDo

* [x] gltf
* [x] glb
* [ ] Sample scene and model
* [ ] Separate editor code
* [x] Unity-5.6 compatibility

### material & texture

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|color        |o       |o       |/materials/#/pbrMetallicRoughness/baseColorFactor
|color texture|o       |o       |/materials/#/pbrMetallicRoughness/baseColorTexture
|sampler      |o       |o       |/samplers
|multi uv     |
|PBR          |

```cs
var texture=new Texture2D(2, 2);
texture.LoadImage(bytes, true);
texture.wrapModeU = TextureWrapMode.Clamp;
texture.wrapModeV = TextureWrapMode.Clamp;
texture.filterMode = FilterMode.Point;

var shader=Shader.Find("Standard");
var material=new Material(shader);
material.color = pbrMetallicRoughness.baseColorFactor;
material.mainTexture = texture;
```

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
mesh.uv = uv;

mesh.subMeshCount=primitiveCount;
for(int i=0; i<primitiveCount; ++i)
{
    mesh.setTriangles(primitive[i].indices, i);
}    
```

#### morph

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|blend shape  |o       |        |/meshes/#/primitives/#/targets

```cs
foreach(var target in targets)
{
    mesh.addBlendSapeFrame(target.name, 100.0f, target.POSITION, target.NORMAL, target.TEXCOORD_0);
}
```

#### skin

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|boneweight   |o       |o       |/meshes/#/primitives/#/attributes/(JOINTS_0|WEIGHTS_0)
|bindmatrix   |o       |        |/skins/#/inverseBindMatrices
|skeleton     |o       |        |/skins/#/skeleton
|joints       |o       |        |/skins/#/joints

```cs
mesh.boneWeights=boneWeights;
mesh.bindposes=inverseBindMatrices

var skin=go.addCompoenent<SkinnedMeshRenderer>();
skin.bones=joints;
skin.rootBone=skeleton;
```

### node

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|translation  |o       |o       |/nodes/#/tlanslation, reverse-z
|rotation     |o       |o       |/nodes/#/rotation, reverse-z
|scale        |o       |o       |/nodes/#/scale
|matrix       |o       |-       |/nodes/#/matrix, reverse-z

### animation

* reverse-z. May be in global coordinate...
* AnimationUtility.GetCurveBindings

|features     |importer|exporter|memo     |
|-------------|--------|--------|---------|
|TRS          |o       |WIP     |/anmations
|interpolation|

```cs
foreach (var binding in AnimationUtility.GetCurveBindings(clip))
{
    var curve = AnimationUtility.GetEditorCurve(clip, binding);
    foreach(var key in curve.keys)
    {

    }
}
```

#### legacy

for Animation component.

```
var clip=new AnimationClip();
clip.legacy=true;
```

#### generic

for Animator component.

```
var clip=new AnimationClip();
```

#### humanoid

for Animator component. require humanoid avatar.

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

