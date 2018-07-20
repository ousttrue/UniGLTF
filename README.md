# UniGLTF

[glTF](https://github.com/KhronosGroup/glTF) importer and exporter

* Unityt5.6.3
* glTF-2.0

![duck](doc/duck.png)
![duck_prefab](doc/duck_prefab.png)
![animation](doc/animation.gif)

* [Samples](https://github.com/ousttrue/UniGLTF_Test)

## License

* [MIT license](LICENSE)

## Download

* https://github.com/ousttrue/UniGLTF/releases

## Install

* import [unitypackage](https://github.com/ousttrue/UniGLTF/releases)

## Usage

* drop gltf folder or glb file into Assets folder

or

* menu [Assets] - [gltf] - [import]

## Importer

* [x] asset(ScriptedImporter) (Unity-2017 or new)
* [x] asset(AssetPostprocessor.OnPostprocessAllAssets) (Unity-5.6)

* runtime [Assets] - [gltf] - [import]

### Sample Models

* https://github.com/KhronosGroup/glTF-Sample-Models

Exclude SciFiHelmet(70074vertices), all model can import.

[Mesh.IndexFormat(from 2017.3)](https://docs.unity3d.com/ScriptReference/Mesh-indexFormat.html) allows a huge mesh, Otherwise mesh division required.

![SciFiHelmet](doc/SciFiHelmet.png)

### Can load gltf in zip archive

* https://github.com/ousttrue/UniGLTF_Test/blob/master/Assets/UniGLTF.Samples/LoaderFromHttp/LoadFromHttp.cs

## Exporter

* asset([right click] - [gltf] - [export]
* runtime
* [validation](http://github.khronos.org/glTF-Validator/)

## Experimental ScriptedImporter for Unity2017

[ScriptedImporter](https://docs.unity3d.com/ScriptReference/Experimental.AssetImporters.ScriptedImporter.html)

* Unity2017.3.0f3

![duck_assets](doc/duck_assets.png)

ScriptedImporter removed.

## Humanoid Helper

* move to [UniHumanoid](https://github.com/ousttrue/UniHumanoid)

