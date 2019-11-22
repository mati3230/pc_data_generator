# Point Cloud Data Generator

Point clouds are generated from mesh scenes. A detailed documentation will follow soon. 
The project was developed in context of the [smart segmentation](https://github.com/mati3230/smartsegmentation) project. 
The generated point clouds can be used in the [segmentation environment](https://github.com/mati3230/segmentation). 

## Requirements

The code is test on python 3.6 and 3.7 and [Unity](https://unity.com/) 2019.3.0a7. 

* [Mozilla Firefox](https://www.mozilla.org/de/exp/firefox/new/)
* [Unity](https://unity.com/) 2019.3.0a7
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

## Installation

Download and install [Mozilla Firefox](https://www.mozilla.org/de/exp/firefox/new/) and [Unity](https://unity.com/) 2019.3.0a7. 
The python requirements can be installed via pip: 

*pip install -r requirements.txt*

[Pyntcloud](https://github.com/mati3230/pyntcloud) has to be installed seperately.

1. Clone https://github.com/mati3230/pyntcloud.git
2. pip install -e ./pyntcloud

To run and build the [Unity](https://unity.com/) Project in the directory "./scene_compositor", the [TriLib](https://assetstore.unity.com/packages/tools/modeling/trilib-model-loader-package-91777) library has to be acquired and to be integrated into the project.

## Usage

A build of the scene_compositor project should be placed in *./scene_compositor/Build/Compositor/Compositor.exe*. The scene_compositor project has to be opened with [Unity](https://unity.com/). The point cloud generation process can be started with:

*python build_database.py*

## scene_compositor Parameters

The following parameters appear in the Unity inspector and can be changed by the user. 

|Parameter|Description|
| - | - |
| Room Widths | Min and max width of the rooms |
| Room Heights | Min and max height of the rooms |
| Room Depths | Min and max depth of the rooms |
| Sep | Seperation character of the csv files |
| N Objects | Min and max number of objects in the scene |
| Scale Margin | If an object has more width and depth then the ones of the room, the object is scaled to the room size with taking this margin into account |
| Max Angles | Max magnitude of the angles in the corresponding axis of a random rotation of an object |
| Max Renderer | Maximum nr of renderer of a model |
| Mesh Collider | If false, objects will be attached with box colliders |
| Gravity Duration | How long will be gravity applied till the scene will be exported? |
| Use Tcp | Will send message when .ply data is exported. If false, just one loop of .ply exportation will be conducted. |
| Ip Address | IP of the program which transforms .ply files to .csv point cloud |
| Port | Port of the program which transforms .ply files to .csv point cloud |
| N Scenes | How many scenes should be created |