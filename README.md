# Point Cloud Data Generator

Point clouds are generated from mesh scenes. A detailed documentation will follow soon. 

## Dependencies

* [Pyntcloud](https://github.com/mati3230/pyntcloud)
* Selenium
* Numpy
* Scipy
* Pandas
* Matplotlib
* Requests
* Pyunpack
* Patool
* [TriLib](https://assetstore.unity.com/packages/tools/modeling/trilib-model-loader-package-91777)

The dependencies can be installed via pip: 
pip install -r requirements.txt
Pyntcloud has to be installed seperately (see [github](https://github.com/mati3230/pyntcloud.git)).

To run and build the [Unity](https://unity.com/) Project in the directory "./scene_compositor", the [TriLib](https://assetstore.unity.com/packages/tools/modeling/trilib-model-loader-package-91777) library has to be acquired. 

## Usage

A build of the scene_compositor project should be placed in "./scene_compositor/Build/Compositor/Compositor.exe". The point cloud generation process can be started with:

python build_database.py
