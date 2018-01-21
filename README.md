# UniGLTF

[glTF](https://github.com/KhronosGroup/glTF) importer for Unity using [ScriptedImporter](https://docs.unity3d.com/ScriptReference/Experimental.AssetImporters.ScriptedImporter.html)

* Unity2017.3.0f3
* glTF-2.0

![duck](doc/duck.png)
![duck_assets](doc/duck_assets.png)
![animation](Recordings/animation.gif)

## Humanoid Helper

![gizmo](doc/BoneMappingGizmo.png)
![inspector](doc/BoneMappingInspector.png)

## Implemented

* [x] AssetImporter
* [x] Runtime Loader

* Format
    * [x] gltf
    * [x] gltf-embeded
    * [x] glb

* Coordinate
    * [x] z-back to z-forward

* Mesh
    * [x] positions, normals, uv
    * [ ] tangent
    * [x] submesh(primitives)
    * [x] skinning
    * [ ] BlendShape(todo: separate mesh to with blendshape and without blendshape)

* Material
    * [x] color
    * [x] texture
    * [ ] PBR

* AnimationClip
    * [x] transform

* Humanoid
    * [x] manual avatar build

* Camera
    * not implemented

* Light
    * not implemented


## Sample Models

* https://github.com/KhronosGroup/glTF-Sample-Models/tree/master/2.0

