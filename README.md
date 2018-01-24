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

## Implemented

* [ ] Sample scene and model

* Importer/Exporter
    * [x] AssetImporter
    * [x] Runtime Loader
    * [ ] Exporter(node rotation is cleared and avatar definition)

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
    * [ ] PBR
    * [x] texture
    * [ ] sampler

* AnimationClip
    * [x] transform

* Humanoid
    * [x] manual avatar build
    * [ ] avatar creation parameter
    * [ ] bone rename
    * [ ] bind matrix

* Camera
    * not implemented

* Light
    * not implemented

## Sample Models

* https://github.com/KhronosGroup/glTF-Sample-Models/tree/master/2.0

