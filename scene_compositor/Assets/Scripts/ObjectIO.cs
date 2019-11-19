using UnityEngine;
using TriLib;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SceneCompositor
{
    /// <summary>
    /// Loading and exportation of models at runtime.
    /// </summary>
    public class ObjectIO : MonoBehaviour
    {
        private AssetLoaderOptions GetAssetLoaderOptions() {
            var assetLoaderOptions = AssetLoaderOptions.CreateInstance();
            assetLoaderOptions.DontLoadCameras = false;
            assetLoaderOptions.DontLoadLights = false;
            assetLoaderOptions.UseOriginalPositionRotationAndScale = true;
            assetLoaderOptions.DisableAlphaMaterials = true;
            assetLoaderOptions.MaterialShadingMode = MaterialShadingMode.Standard;
            assetLoaderOptions.AddAssetUnloader = true;
            assetLoaderOptions.AdvancedConfigs.Add(AssetAdvancedConfig.CreateConfig(AssetAdvancedPropertyClassNames.FBXImportDisableDiffuseFactor, true));
            return assetLoaderOptions;
        }

        public bool TryLoadObject(string filename, out GameObject model) {
            model = null;
            if (string.IsNullOrEmpty(filename)) return false;
            if (!File.Exists(filename)) return false;
            var assetLoaderOptions = GetAssetLoaderOptions();
            using (var assetLoader = new AssetLoader()) {
                try {
                    model = assetLoader.LoadFromFileWithTextures(filename, assetLoaderOptions);
                    if (assetLoader.MeshData == null || assetLoader.MeshData.Length == 0) return false;
                } catch (Exception) {
                    if (model != null) {
                        Destroy(model);
                    }
                    //Debug.LogError(exception.ToString());
                    return false;
                }
            }

            foreach (Camera camera in model.GetComponentsInChildren<Camera>()) {
                Destroy(camera);
            }

            return true;
        }

        /// <summary>
        /// Every mesh in the list of gameObjects will be exported as .ply. Additionally, a .csv containing some 
        /// information about the .ply files will be created. All files will be exported to the same path. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="gameObjects"></param>
        /// <param name="sep"></param>
        /// <param name="area"></param>
        public void ExportPly(string path, List<GameObject> gameObjects, char sep, out float area) {
            StringBuilder sbCsv = new StringBuilder();
            // Additional information of a .ply file
            sbCsv.AppendLine("Filename" + sep + "Area" + sep + "HasNormals" + sep + "HasColors");
            area = 0f;
            // Every gameObject will be exported as .ply
            for (int j = 0; j < gameObjects.Count; j++) {
                GameObject gameObject = gameObjects[j];
                float a = 0f;
                ToPly(gameObject, path, sep, ref sbCsv, out a);
                area += a;
            }
            File.WriteAllText(path + "/scene.csv", sbCsv.ToString());
        }

        /// <summary>
        /// Iterates over all meshes of a gameObject to export a .ply file for every mesh. Refer to wikipedia to get more information of a 
        /// .ply file. 
        /// </summary>
        /// <param name="go"></param>
        /// <param name="path"></param>
        /// <param name="sep"></param>
        /// <param name="sbCsv"></param>
        /// <param name="areaSum"></param>
        void ToPly(GameObject go, string path, char sep, ref StringBuilder sbCsv, out float areaSum) {
            // get the meshes
            List<MeshFilter> mfs = go.GetComponentsInChildren<MeshFilter>().Where(x => x.sharedMesh != null).ToList();
            int meshCount = mfs.Count;
            Mesh[] meshes = new Mesh[meshCount];
            areaSum = 0; // area of the tiangles of the whole gameObject

            for (int i = 0; i < meshCount; i++) {
                Transform t = mfs[i].transform;

                StringBuilder sbPly = new StringBuilder();

                // start of header 
                sbPly.AppendLine("ply");
                sbPly.AppendLine("format ascii 1.0");

                Mesh mesh = mfs[i].sharedMesh;

                Vector3[] v = mesh.vertices;
                int[] tris = mesh.triangles;
                Vector3[] n = mesh.normals;
                int vertex_count = v.Length;
                int tri_count = Mathf.FloorToInt(tris.Length / 3);
                bool isColored = mesh.colors.Length != 0;
                bool hasNormals = n != null;
                if (hasNormals) hasNormals = n.Length == v.Length;


                sbPly.AppendLine("element vertex " + vertex_count.ToString());
                sbPly.AppendLine("property float x");
                sbPly.AppendLine("property float y");
                sbPly.AppendLine("property float z");
                if (hasNormals) {
                    sbPly.AppendLine("property float nx");
                    sbPly.AppendLine("property float ny");
                    sbPly.AppendLine("property float nz");
                }
                if (isColored) {
                    sbPly.AppendLine("property uchar red");
                    sbPly.AppendLine("property uchar green");
                    sbPly.AppendLine("property uchar blue");
                }
                sbPly.AppendLine("element face " + tri_count.ToString());
                sbPly.AppendLine("property list uchar int vertex_indices");
                sbPly.AppendLine("end_header");

                // vertices
                if (isColored && hasNormals) {
                    Color[] c = mesh.colors;
                    for (int j = 0; j < v.Length; j++) {

                        v[j] = t.TransformPoint(v[j]);
                        n[j] = t.TransformDirection(n[j]);

                        Vector3 vc = new Vector3Int(Mathf.FloorToInt(c[j].r * 255), Mathf.FloorToInt(c[j].g * 255), Mathf.FloorToInt(c[j].b * 255));
                        sbPly.AppendLine(Convert.ToString(v[j].x, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(v[j].z, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(v[j].y, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(n[j].x, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(n[j].z, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(n[j].y, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(vc.x, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(vc.y, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(vc.z, CultureInfo.InvariantCulture));
                    }
                } else if (isColored) {
                    Color[] c = mesh.colors;
                    for (int j = 0; j < v.Length; j++) {

                        v[j] = t.TransformPoint(v[j]);

                        Vector3 vc = new Vector3Int(Mathf.FloorToInt(c[j].r * 255), Mathf.FloorToInt(c[j].g * 255), Mathf.FloorToInt(c[j].b * 255));
                        sbPly.AppendLine(Convert.ToString(v[j].x, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(v[j].z, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(v[j].y, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(vc.x, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(vc.y, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(vc.z, CultureInfo.InvariantCulture));
                    }
                } else if (hasNormals) {
                    for (int j = 0; j < v.Length; j++) {

                        v[j] = t.TransformPoint(v[j]);
                        n[j] = t.TransformDirection(n[j]);

                        sbPly.AppendLine(Convert.ToString(v[j].x, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(v[j].z, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(v[j].y, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(n[j].x, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(n[j].z, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(n[j].y, CultureInfo.InvariantCulture));
                    }
                } else { // vertexes only
                    for (int j = 0; j < v.Length; j++) {

                        v[j] = t.TransformPoint(v[j]);

                        sbPly.AppendLine(Convert.ToString(v[j].x, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(v[j].z, CultureInfo.InvariantCulture) + " " +
                            Convert.ToString(v[j].y, CultureInfo.InvariantCulture));
                    }
                }

                // faces
                float area = 0f;
                for (int j = 0; j < tris.Length; j += 3) {
                    sbPly.AppendLine("3" + " " + tris[j] + " " + tris[j + 1] + " " + tris[j + 2]);
                    area += AreaTriangle(v[tris[j]], v[tris[j + 1]], v[tris[j + 2]]);
                }

                areaSum += area;

                // write the additional information of the .ply in the .csv file
                sbCsv.Append(go.name + "_" + string.Format("{0:D4}", i) + ".ply");
                sbCsv.Append(sep);
                sbCsv.Append(Convert.ToString(area, CultureInfo.InvariantCulture));
                sbCsv.Append(sep);
                sbCsv.Append(hasNormals);
                sbCsv.Append(sep);
                sbCsv.Append(isColored);
                sbCsv.Append("\n");

                // write the .ply of the mesh
                File.WriteAllText(path + "/" + go.name + "_" + string.Format("{0:D4}", i) + ".ply", sbPly.ToString());
            }
        }

        float AreaTriangle(Vector3 a, Vector3 b, Vector3 c) {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            float theta = Mathf.PI * Vector3.Angle(ab, ac) / 180;
            return 0.5f * ab.magnitude * ac.magnitude * Mathf.Sin(theta);
        }
    }
}