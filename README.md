# UniGLTF

[glTF](https://github.com/KhronosGroup/glTF) importer for Unity using [ScriptedImporter](https://docs.unity3d.com/ScriptReference/Experimental.AssetImporters.ScriptedImporter.html)

* Unity2017.3.0f3
* glTF-2.0

![duck](doc/duck.png)
![duck_assets](doc/duck_assets.png)
![animation](Recordings/animation.gif)

## Humanoid Helper

* model position is origin
* model look at +z orientation
* model root node rotation is Quatenion.identity

![gizmo](doc/BoneMappingGizmo.png)
![inspector](doc/BoneMappingInspector.png)
![humanoid](Recordings/humanoid.gif)

## Features & ToDo

* [x] gltf
* [x] glb
* [ ] Sample scene and model
* [ ] Unity-5.6 compatibility
* [x] Asset Importer/Exporter
* [x] Runtime Importer/Exporter

|features    |importer|exporter|memo     |
|------------|--------|--------|---------|
|**material**|        |        |
|color       |o       |o       |
|color texture|o      |o       |
|sampler     |
|PBR         |
|**mesh**    |        |        |
|positions   |o       |o       |reverse-z
|normals     |o       |o       |reverse-z
|uv          |o       |o       |reverse-z
|tangent     |        |        |?
|primitive   |o       |o       |todo:sharing attributes|
|boneweight  |o       |o       |
|blend shape |o       |        |
|**animation**|
|transform    |o      |        |as generic AnimationClip
|**node**    |
|translation |o       |o       |reverse-z
|rotation    |o       |o       |reverse-z
|scale       |o       |o
|matrix      |o       |-       |reverse-z
|**skinning**|
|avatar      |o       |        |
|avatar params|       |        |
|bone name    |       |        |rename to bone name
|bindmatrix   |o      |        |


* not implemented
    * Camera
    * Light

## Sample Models

* https://github.com/KhronosGroup/glTF-Sample-Models/tree/master/2.0
