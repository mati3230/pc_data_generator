using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace SceneCompositor
{
    public class Compositor : MonoBehaviour
    {
        /// <summary>
        /// Assignment of a wall type to a wall game object
        /// </summary>
        Dictionary<WallType, GameObject> wallTypeToGO;
        [SerializeField, Tooltip("Min and max width of the rooms")]
        Vector2 roomWidths = new Vector2(5f, 7f);
        [SerializeField, Tooltip("Min and max height of the rooms")]
        Vector2 roomHeights = new Vector2(3f, 4f);
        [SerializeField, Tooltip("Min and max depth of the rooms")]
        Vector2 roomDepths = new Vector2(5f, 7f);
        Vector3 roomSize;

        [SerializeField, Tooltip("Seperation character of the csv file")]
        char sep = ';';
        ContextInformation contextInformation;
        ObjectIO objectIO;

        [SerializeField, Tooltip("Min and max number of objects in the scene")]
        Vector2Int nObjects = new Vector2Int(2, 5);

        [SerializeField, Tooltip("If an object has more width and depth then the ones of the room, the object is scaled to the room size with taking this margin into account")]
        float scaleMargin = 2f;

        [SerializeField, Tooltip("Max magnitude of the angles in the corresponding axis of a random rotation of an object")]
        Vector3 maxAngles = new Vector3(10f, 180f, 10f);
        /// <summary>
        /// List of models which cannot be loaded
        /// </summary>
        List<string> blackList;
        List<GameObject> models = new List<GameObject>();
        [SerializeField, Tooltip("Maximum nr of renderer of a model"), Range(10, 60)]
        int maxRenderer = 30;
        [SerializeField, Tooltip("If false, objects will be attached with box colliders")]
        bool meshCollider = false;
        [SerializeField, Range(1f, 10f), Tooltip("How long will be gravity applied till the scene will be exported?")]
        float gravityDuration = 3f;
        [SerializeField, Tooltip("Will send message when .ply data is exported. If false, just one loop of .ply exportation will be conducted.")]
        bool useTcp = false;
        [SerializeField, Tooltip("IP of the program which transforms .ply files to .csv point cloud")]
        string ipAddress = "127.0.0.1";
        IPAddress address;
        [SerializeField, Range(80, 20000), Tooltip("Port of the program which transforms .ply files to .csv point cloud")]
        int port = 5005;
        TcpClient client;
        NetworkStream stream;
        [SerializeField, Tooltip("How many scenes should be created")]
        uint nScenes = 1000000;
        uint currentNScenes = 0;
        string objectsDir;

        void Start() {
            StartSceneCreation();
        }

        /// <summary>
        /// Buggy (nr is not always correct - especially with high number (e.g. 1000).).
        /// Search for the nr of the last created scene and increment that number to continue 
        /// the scene creation process with the next nr. 
        /// </summary>
        void SetCurrentScene() {
            string pointcloudDir = objectsDir + "/PointcloudScenes";
            currentNScenes = 0;
            foreach (string fileDir in Directory.GetFiles(pointcloudDir)) {
                if (!fileDir.EndsWith(".csv")) continue;
                string[] strs = fileDir.Split('_', '.');
                int nr = 0;
                foreach (string str in strs) {
                    uint tmp_nr = 0;
                    if (!UInt32.TryParse(str, out tmp_nr)) continue;
                    nr = (int)tmp_nr;
                    break;
                }
                currentNScenes = (uint)Mathf.Max(currentNScenes, nr);
                currentNScenes++;
            }
        }

        void StartSceneCreation() {
            if (!Directory.Exists(Application.dataPath + "/StreamingAssets"))
                throw new Exception("Directory '" + Application.dataPath + "/StreamingAssets' does not exist");
            if (!File.Exists(Application.dataPath + "/StreamingAssets/ObjectsDirectory.txt"))
                throw new Exception("File '" + Application.dataPath + "/StreamingAssets/ObjectsDirectory.txt' does not exist");
            string[] lines = File.ReadAllLines(Application.dataPath + "/StreamingAssets/ObjectsDirectory.txt");
            if (lines.Length == 0)
                throw new Exception("File '" + Application.dataPath + "/StreamingAssets/ObjectsDirectory.txt' is empty");
            // objectsDir specifies the path to the objects directory which is stored in ObjectsDirectory.txt
            objectsDir = lines[0];
            if (!Directory.Exists(objectsDir))
                throw new Exception("Directory '" + objectsDir + "' does not exist");

            SetCurrentScene();
            CreateRoom();

            // init helpers and ensure that dependencies such as csv files are available
            objectIO = new ObjectIO();
            contextInformation = new ContextInformation();
            if (!contextInformation.TryLoadObjectEntries(objectsDir, sep))
                throw new Exception("Error while loading .objects.csv in path '" + objectsDir + "'");
            if (!contextInformation.TryLoadHeights(objectsDir, sep))
                throw new Exception("Error while loading .scales.csv");
            if (!contextInformation.TryLoadBlackList(objectsDir, out blackList))
                throw new Exception("Error while loading blacklist.txt");

            // set up connection to program that converts the ply to csv
            if (useTcp) {
                if (!IPAddress.TryParse(ipAddress, out address))
                    throw new Exception("IP '" + ipAddress + "' is invalid");
                client = new TcpClient(ipAddress, port);
                stream = client.GetStream();
            }

            LoadObjects();
        }

        #region ObjectPlacement
        void ClearObjects() {
            for (int i = 1; i < models.Count; i++) {
                Destroy(models[i]);
            }
            models.RemoveRange(1, models.Count - 1);
        }

        /// <summary>
        /// clear the models list and remove objects from current scene
        /// </summary>
        void ClearModels() {
            for (int i = 0; i < models.Count; i++) {
                Destroy(models[i]);
            }
            models = new List<GameObject>();
        }

        void LoadObjects() {
            Debug.Log("----------------------------");
            ClearModels();
            CreateRoom();

            int n = UnityEngine.Random.Range(nObjects.x, nObjects.y);
            int i = 0;
            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            while (i < n) {
                // get random object
                ContextInformation.ObjectEntry entry = contextInformation.GetRandomEntry();
                Debug.Log(entry);
                // write last model to a crash log file for debugging
                System.IO.File.WriteAllText(@"./CrashLog.txt", entry.ToString());
                if (blackList.Contains(entry.FileName)) continue;

                string filename = objectsDir + "/../" + entry.FileDir;
                GameObject modelGO = null;
                if (!objectIO.TryLoadObject(filename, out modelGO)) continue;
                Model model = new Model(modelGO);
                // filter models with to many renderers for performance reasons
                if (model._Renderer.Length > maxRenderer) {
                    Destroy(modelGO);
                    continue;
                }
                if (!model.BoundsExists) {
                    Destroy(modelGO);
                    continue;
                }

                modelGO.name = entry.FileName;
                float height = contextInformation.GetHeight(entry);

                models.Add(modelGO);

                Scale(model, height, roomSize, scaleMargin);
                PlaceObjectRoom(model);

                Rigidbody rigidbody = model.AddPhysics(meshCollider);
                rigidbodies.Add(rigidbody);

                i++;
            }
            foreach (Rigidbody r in rigidbodies) {
                r.useGravity = true;
            }
            Invoke("Export", gravityDuration);
        }

        /// <summary>
        /// Export the models list to .ply files and call LoadObjects at the end. 
        /// Export and LoadObjects build the scene creation loop.
        /// </summary>
        void Export() { 
            string pointcloudDir = objectsDir + "/PointcloudScenes/Scene_" + currentNScenes;
            // create temp directory for the current scene
            if (!Directory.Exists(pointcloudDir))
                Directory.CreateDirectory(pointcloudDir);
            else {
                foreach (string fileDir in Directory.GetFiles(pointcloudDir)) {
                    File.Delete(fileDir);
                }
            }

            // the sum of all areas of the meshes in the scene is send to the .ply to point cloud program
            float area = 0f;
            objectIO.ExportPly(pointcloudDir, models, sep, out area);
            Debug.Log("Models exported, Area: " + area);

            if (useTcp) {
                if(currentNScenes < nScenes) {
                    // processing command for the client to convert .ply's to point clouds
                    string message = currentNScenes.ToString() + sep + Convert.ToString(area, CultureInfo.InvariantCulture);
                    // wait for the answer of the client (success or error)
                    string answer = SendMessageToClient(message);
                    if(answer != "error") 
                        currentNScenes++;
                    if(currentNScenes == nScenes) { // finished
                        stream.Close();
                        client.Close();
                        return;
                    }
                    LoadObjects();
                }
            }
        }

        /// <summary>
        /// Send message via tcp to .ply to point cloud converter.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        string SendMessageToClient(string message) {
            Byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);

            data = new Byte[256];

            String responseData = String.Empty;
            // blocking operation
            Int32 bytes = stream.Read(data, 0, data.Length);
            return System.Text.Encoding.UTF8.GetString(data, 0, bytes);
        }

        /// <summary>
        /// Translate model such that it fits the room.
        /// </summary>
        /// <param name="_model"></param>
        void PlaceObjectRoom(Model _model) {
            Vector3 size = _model._Bounds.size;
            float xD = Mathf.Max(0f, (roomSize.x - size.x) / 2);
            float yD = Mathf.Max(0f, (roomSize.y - size.y) / 2);
            float zD = Mathf.Max(0f, (roomSize.z - size.z) / 2);

            Vector3 randomRoomPos = new Vector3(
                UnityEngine.Random.Range(-xD, xD),
                UnityEngine.Random.Range(-yD, yD),
                UnityEngine.Random.Range(-zD, zD)
                );
            _model._Model.transform.position = randomRoomPos;
            Vector3 euler = new Vector3(
                UnityEngine.Random.Range(-maxAngles.x, maxAngles.x),
                UnityEngine.Random.Range(-maxAngles.y, maxAngles.y),
                UnityEngine.Random.Range(-maxAngles.z, maxAngles.z)
                );
            _model._Model.transform.Rotate(euler, Space.World);
            Vector3 deviation = GetDeviation(roomSize, _model);
            _model._Model.transform.position -= deviation;
        }

        /// <summary>
        /// Scale model such that it fits the room.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="height"></param>
        /// <param name="roomSize"></param>
        /// <param name="margin"></param>
        public void Scale(Model model, float height, Vector3 roomSize, float margin) {
            model.Scale(height / model._Bounds.size.y);
            if (model._Bounds.size.x > roomSize.x) model.Scale(roomSize.x / (model._Bounds.size.x + margin));
            if (model._Bounds.size.z > roomSize.z) model.Scale(roomSize.z / (model._Bounds.size.z + margin));
        }

        /// <summary>
        /// Calculate the greatest distance of model to the center of the room.
        /// </summary>
        /// <param name="roomSize"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        Vector3 GetDeviation(Vector3 roomSize, Model model) {
            Vector3 maxDeviation = Vector3.zero;
            float xSign = 1, ySign = 1, zSign = 1;
            Vector3 roomSizeHalf = roomSize / 2;
            foreach (GameObject bbO in model.BoundingBoxObjects) {
                Vector3 corner = bbO.transform.position;
                float abs = Mathf.Abs(corner.x);
                // is the corner out of the room?
                if (abs > roomSizeHalf.x) {
                    // is deviation abs - roomSizeHalf.x more than the current max deviation
                    maxDeviation.x = Mathf.Max(maxDeviation.x, abs - roomSizeHalf.x);
                    xSign = Mathf.Sign(corner.x);
                }
                abs = Mathf.Abs(corner.y);
                if (abs > roomSizeHalf.y) {
                    maxDeviation.y = Mathf.Max(maxDeviation.y, abs - roomSizeHalf.y);
                    ySign = Mathf.Sign(corner.y);
                }
                abs = Mathf.Abs(corner.z);
                if (abs > roomSizeHalf.z) {
                    maxDeviation.z = Mathf.Max(maxDeviation.z, abs - roomSizeHalf.z);
                    zSign = Mathf.Sign(corner.z);
                }
            }
            // apply the right sign such that the object will be dragged in the right direction
            maxDeviation.x *= xSign;
            maxDeviation.y *= ySign;
            maxDeviation.z *= zSign;
            return maxDeviation;
        }
        #endregion

        #region Room

        void CreateRoom() {
            wallTypeToGO = new Dictionary<WallType, GameObject>();
            // parent
            GameObject room = new GameObject();
            room.name = "Room";
            foreach (WallType wallType in Enum.GetValues(typeof(WallType))) {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wall.name = wallType.ToString();
                wallTypeToGO.Add(wallType, wall);
                wall.transform.parent = room.transform;

                models.Add(wall);
            }
            Room();
            // origin should be the middle of the room
            room.transform.Translate(new Vector3(0, -roomSize.y/2));
        }

        /// <summary>
        /// Transformation of the walls to set up the room.
        /// </summary>
        void Room() {
            roomSize = RandomRoomSize();
            wallTypeToGO[WallType.Bottom].transform.localScale = new Vector3(roomSize.x, roomSize.z, 1f);
            wallTypeToGO[WallType.Ceiling].transform.localScale = new Vector3(roomSize.x, roomSize.z, 1f);
            wallTypeToGO[WallType.Front].transform.localScale = new Vector3(roomSize.x, roomSize.y, 1f);
            wallTypeToGO[WallType.Back].transform.localScale = new Vector3(roomSize.x, roomSize.y, 1f);
            wallTypeToGO[WallType.Left].transform.localScale = new Vector3(roomSize.y, roomSize.z, 1f);
            wallTypeToGO[WallType.Right].transform.localScale = new Vector3(roomSize.y, roomSize.z, 1f);
            wallTypeToGO[WallType.Bottom].transform.Rotate(Vector3.right, 90f);
            wallTypeToGO[WallType.Bottom].transform.Rotate(Vector3.forward, 90f);
            wallTypeToGO[WallType.Bottom].transform.Rotate(Vector3.forward, 90f);
            wallTypeToGO[WallType.Ceiling].transform.Translate(new Vector3(0f, roomSize.y, 0f));
            wallTypeToGO[WallType.Ceiling].transform.Rotate(Vector3.right, -90f);
            wallTypeToGO[WallType.Front].transform.Translate(new Vector3(0f, roomSize.y / 2));
            wallTypeToGO[WallType.Front].transform.Translate(new Vector3(0f, 0f, roomSize.z / 2));
            wallTypeToGO[WallType.Back].transform.Translate(new Vector3(0f, roomSize.y / 2));
            wallTypeToGO[WallType.Back].transform.Translate(new Vector3(0f, 0f, -roomSize.z / 2));
            wallTypeToGO[WallType.Back].transform.Rotate(Vector3.up, 180f);
            wallTypeToGO[WallType.Left].transform.Translate(new Vector3(0f, roomSize.y / 2));
            wallTypeToGO[WallType.Left].transform.Translate(new Vector3(roomSize.x / 2, 0f));
            wallTypeToGO[WallType.Left].transform.Rotate(Vector3.up, 90f);
            wallTypeToGO[WallType.Left].transform.Rotate(Vector3.forward, 90f);
            wallTypeToGO[WallType.Right].transform.Translate(new Vector3(0f, roomSize.y / 2));
            wallTypeToGO[WallType.Right].transform.Translate(new Vector3(-roomSize.x / 2, 0f));
            wallTypeToGO[WallType.Right].transform.Rotate(Vector3.up, -90f);
            wallTypeToGO[WallType.Right].transform.Rotate(Vector3.forward, 90f);
        }

        Vector3 RandomRoomSize() {
            float width = UnityEngine.Random.Range(roomWidths.x, roomWidths.y);
            float height = UnityEngine.Random.Range(roomHeights.x, roomHeights.y);
            float depth = UnityEngine.Random.Range(roomDepths.x, roomDepths.y);
            return new Vector3(width, height, depth);
        }

        enum WallType
        {
            Bottom,
            Ceiling, 
            Front,
            Back,
            Left,
            Right
        }
        #endregion Room

        /// <summary>
        /// Container class for a unity gameobject that mainly computes the edges of the bounding box of a 3D model. 
        /// A Model or unity gameObject has multiple child models with renderer components. Every child got a bounding
        /// box which will be stored in this class. 
        /// </summary>
        public class Model
        {
            // The parent object.
            public GameObject _Model { get; private set; }
            public Bounds _Bounds { get; private set; }
            /// <summary>
            /// False, if there are no renderer
            /// </summary>
            public bool BoundsExists { get; private set; }
            /// <summary>
            /// List of objects which represent the corners of the bounding box of the model.
            /// </summary>
            public List<GameObject> BoundingBoxObjects { get; private set; }
            public Renderer[] _Renderer { get; private set; }
            public Model(GameObject model) {
                _Model = model;
                Renderer[] renderer = model.GetComponentsInChildren<Renderer>();
                if (renderer == null) {
                    Destroy(model);
                    BoundsExists = false;
                }
                if (renderer.Length == 0) {
                    Destroy(model);
                    BoundsExists = false;
                }
                this._Renderer = renderer;
                BoundsExists = true;
                Vector3 min = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);
                Vector3 max = new Vector3(Single.MinValue, Single.MinValue, Single.MinValue);
                // compute the minimum and maximum edges of the bounding box of a model
                foreach (Renderer rend in renderer) {
                    Bounds bounds = rend.bounds;
                    Vector3 minTmp = bounds.min;
                    Vector3 maxTmp = bounds.max;
                    min.x = Mathf.Min(min.x, minTmp.x);
                    min.y = Mathf.Min(min.y, minTmp.y);
                    min.z = Mathf.Min(min.z, minTmp.z);
                    max.x = Mathf.Max(max.x, maxTmp.x);
                    max.y = Mathf.Max(max.y, maxTmp.y);
                    max.z = Mathf.Max(max.z, maxTmp.z);
                }
                // size and center of the whole bounding box
                Vector3 size = max - min;
                Vector3 center = (max + min) / 2;
                _Bounds = new Bounds(center, size);
                SetBoundingObjects();
            }
            public void Scale(float scale) {
                _Model.transform.localScale *= scale;
                Vector3 size = scale * _Bounds.size;
                Vector3 max = scale * _Bounds.max;
                Vector3 min = scale * _Bounds.min;
                Vector3 center = (max + min) / 2;
                _Bounds = new Bounds(center, size);
            }

            /// <summary>
            /// Calculate the position of each corner of the bounding box (e.g. ldb = left down bottom). 
            /// </summary>
            void SetBoundingObjects() {
                Vector3 ldb = new Vector3(_Bounds.min.x, _Bounds.min.y, _Bounds.min.z);
                Vector3 lub = new Vector3(_Bounds.min.x, _Bounds.max.y, _Bounds.min.z);
                Vector3 ldf = new Vector3(_Bounds.min.x, _Bounds.min.y, _Bounds.max.z);
                Vector3 luf = new Vector3(_Bounds.min.x, _Bounds.max.y, _Bounds.max.z);
                Vector3 rdb = new Vector3(_Bounds.max.x, _Bounds.min.y, _Bounds.min.z);
                Vector3 rub = new Vector3(_Bounds.max.x, _Bounds.max.y, _Bounds.min.z);
                Vector3 rdf = new Vector3(_Bounds.max.x, _Bounds.min.y, _Bounds.max.z);
                Vector3 ruf = new Vector3(_Bounds.max.x, _Bounds.max.y, _Bounds.max.z);

                BoundingBoxObjects = new List<GameObject>() {
                    CreateBoundingBoxObject(ldb),
                    CreateBoundingBoxObject(lub),
                    CreateBoundingBoxObject(ldf),
                    CreateBoundingBoxObject(luf),
                    CreateBoundingBoxObject(rdb),
                    CreateBoundingBoxObject(rub),
                    CreateBoundingBoxObject(rdf),
                    CreateBoundingBoxObject(ruf)
                };
            }

            /// <summary>
            /// Gameobject for the corner of the bounding of the model.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns></returns>
            GameObject CreateBoundingBoxObject(Vector3 pos) {
                GameObject bbObject = new GameObject();
                bbObject.transform.position = pos;
                bbObject.transform.parent = _Model.transform;
                return bbObject;
            }

            public Rigidbody AddPhysics(bool meshCollider) {
                Rigidbody rigidbody = _Model.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                if (meshCollider) {
                    foreach (Renderer rend in _Renderer) {
                        MeshCollider collider = rend.gameObject.AddComponent<MeshCollider>();
                        collider.convex = true;
                    }
                } 
                else {
                    foreach (Renderer rend in _Renderer) {
                        rend.gameObject.AddComponent<BoxCollider>();
                    }
                }
                return rigidbody;
            }
        }
    }
}
