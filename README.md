# UniGLTF

[glTF](https://github.com/KhronosGroup/glTF) 2.0 importer and exporter for Unity 5.6 or later

![duck](doc/duck.png)
![animation](doc/animation.gif)

# License

* [MIT license](LICENSE)

# See also

* https://github.com/ousttrue/UniGLTF/wiki

# Sample Models

* https://github.com/KhronosGroup/glTF-Sample-Models

## Huge model required Unity2017.3 or later

* [Mesh.IndexFormat(from 2017.3)](https://docs.unity3d.com/ScriptReference/Mesh-indexFormat.html) is required

example. SciFiHelmet(70074vertices)

![SciFiHelmet](doc/SciFiHelmet.png)

# Download

* https://github.com/ousttrue/UniGLTF/releases

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

