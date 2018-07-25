# UniGLTF

[glTF](https://github.com/KhronosGroup/glTF) importer and exporter

* Unityt5.6.3
* glTF-2.0

![duck](doc/duck.png)
![animation](doc/animation.gif)

* https://github.com/ousttrue/UniGLTF/wiki

# License

* [MIT license](LICENSE)

# Sample Models

* https://github.com/KhronosGroup/glTF-Sample-Models

Exclude SciFiHelmet(70074vertices), all model can import.

[Mesh.IndexFormat(from 2017.3)](https://docs.unity3d.com/ScriptReference/Mesh-indexFormat.html) allows a huge mesh, Otherwise mesh division required.

![SciFiHelmet](doc/SciFiHelmet.png)

# Download

* https://github.com/ousttrue/UniGLTF/releases

## Install

* import [unitypackage](https://github.com/ousttrue/UniGLTF/releases)

# Usage

## Import as prefab

* drop gltf folder or glb file into Assets folder

![duck_prefab](doc/duck_prefab.png)

or

* editor mode
* menu [UniGLTF] - [Import] 
* open gltf file(gltf, glb, zip) from out of Asset Folder

## Import in runTime

```cs
string path; // gltf, glb or zip(include gltf)

var context = gltfImporter.Load(path);
context.ShowMeshes();

GameObject root = context.Root;
```

## Export from scene

* select target root GameObject in scene(GameObect must be empty root, because target become gltf's ``/scene``. A scene includes nodes.
* menu [UniGLTF] - [Export]
* support only glb format

## Export in runTime

```cs
GameObject go; // export target
string path; // glb write path

var gltf = gltfExporter.Export(go);
var bytes = gltf.ToGlbBytes();
File.WriteAllBytes(path, bytes);
```

* support only glb format

