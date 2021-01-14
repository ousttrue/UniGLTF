> I decided to integrate this library with UniVRM for maintenance reason (submodule burdensome). Continue updating within UniVRM

* [UniGLTF in UniVRM](https://github.com/vrm-c/UniVRM/tree/master/Assets/UniGLTF)

UniVRM-xxx.unitypackage contains UniGLTF.

* https://github.com/vrm-c/UniVRM/releases

UniGLTF is currently separated from UniVRM-0.63.2 for stand-alone use.

* https://vrm.dev/docs/univrm/gltf/unigltf/

# UniGLTF

[glTF](https://github.com/KhronosGroup/glTF) 2.0 importer and exporter for Unity 5.6 or later

Improved material importer(UniGLTF-1.21) ! 

Below is imported from [DamagedHelmet](https://github.com/KhronosGroup/glTF-Sample-Models/tree/master/2.0/DamagedHelmet). Using unity standard shader.

![standard shader](doc/pbr_to_standard.png)


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

## API

* https://github.com/ousttrue/UniGLTF/wiki/Rutime-API

