using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SceneCompositor
{
    public class ContextInformation
    {
        /// <summary>
        /// The Key is an object category (e.g. Plant) which is stored as string
        /// </summary>
        public Dictionary<string, List<ObjectEntry>> CategoryToObjects { private set; get; }
        /// <summary>
        /// Each category has a scale. Furthermore, scales for specific objects of a category can also be specified.
        /// For instance, a flower and a tree are both plants but they have a different scale.
        /// </summary>
        public Dictionary<string, List<HeightInformation>> CategoryToScales { private set; get; }
        /// <summary>
        /// Mapping of indexes to categories. See function AddCategory for more information. 
        /// </summary>
        public Dictionary<int, string> IndexToCategory { private set; get; }

        /// <summary>
        /// Load the objects which are listed in objectsDir/objects.csv and store them in CategoryToObjects
        /// </summary>
        /// <param name="objectsDir"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public bool TryLoadObjectEntries(string objectsDir, char sep) {
            string filename = objectsDir + "/objects.csv";
            if (!File.Exists(filename)) {
                Debug.LogError("Missing file: " + filename);
                return false;
            }

            CategoryToObjects = new Dictionary<string, List<ObjectEntry>>();
            IndexToCategory = new Dictionary<int, string>();

            // open the file
            var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            bool firstLine = true; // flag for skipping of the header
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8)) {
                // read all lines of the .csv
                string line;
                while ((line = streamReader.ReadLine()) != null) {
                    if (firstLine) { // skip the header
                        firstLine = false;
                        continue;
                    }
                    ObjectEntry entry = new ObjectEntry(line, sep);
                    AddCategory(entry.Category);
                    CategoryToObjects[entry.Category].Add(entry);
                }
            }
            return true;
        }

        public bool TryLoadHeights(string objsDir, char sep) {
            CategoryToScales = new Dictionary<string, List<HeightInformation>>();
            string filename = objsDir + "/../heights.csv";
            if (!File.Exists(filename)) return false;

            var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8)) {
                string line;
                while ((line = streamReader.ReadLine()) != null) {
                    HeightInformation heightInformation = new HeightInformation(line, sep);
                    if (!CategoryToScales.ContainsKey(heightInformation.Category)) CategoryToScales.Add(heightInformation.Category, new List<HeightInformation>());
                    CategoryToScales[heightInformation.Category].Add(heightInformation);
                }
            }
            return true;
        }

        /// <summary>
        /// Loads a list of objects that made problems during scene creation process. 
        /// </summary>
        /// <param name="objsDir"></param>
        /// <param name="blackList"></param>
        /// <returns></returns>
        public bool TryLoadBlackList(string objsDir, out List<string> blackList) {
            blackList = new List<string>();
            string filename = objsDir + "/../blacklist.txt";
            if (!File.Exists(filename)) return false;
            var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8)) {
                string line;
                while ((line = streamReader.ReadLine()) != null) {
                    blackList.Add(line);
                }
            }
            return true;
        }
        
        /// <summary>
        /// Get the height of the category (e.g. plant) of a specific object. 
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public float GetHeight(ObjectEntry entry) {
            List<HeightInformation> heights = CategoryToScales[entry.Category];
            // first entry of the heights contains a scale fallback height for the whole category
            HeightInformation categoryInformation = heights[0];
            
            // fallback height of that category
            float height = UnityEngine.Random.Range(categoryInformation.Height.x, categoryInformation.Height.y);

            // try to find a specific height for the object of that category
            // take fallback height if nothing will be found
            foreach (HeightInformation heightInformation in heights) {
                if (!entry.FileName.Contains(heightInformation.ObjectClass)) continue;
                CultureInfo cultureInfo = new CultureInfo("en-US");
                int idx = cultureInfo.CompareInfo.IndexOf(entry.FileName, heightInformation.ObjectClass, CompareOptions.IgnoreCase);
                if (idx == -1) continue;
                else {
                    height = UnityEngine.Random.Range(heightInformation.Height.x, heightInformation.Height.y);
                    break;
                }
            }

            return height;
        }
        
        /// <summary>
        /// Selects a random category and than a random object.
        /// </summary>
        /// <returns></returns>
        public ObjectEntry GetRandomEntry() {
            int n_categories = CategoryToObjects.Keys.Count;
            int category_idx = UnityEngine.Random.Range(1, n_categories + 1);
            string category = IndexToCategory[category_idx];
            List<ObjectEntry> entries = CategoryToObjects[category];
            int n_objects = entries.Count;
            int object_idx = UnityEngine.Random.Range(0, n_objects);
            ObjectEntry entry = entries[object_idx];
            return entry;
        }

        /// <summary>
        /// Each category is assigned to an index such that a category can be 
        /// selected by a random nr. 
        /// </summary>
        /// <param name="category"></param>
        void AddCategory(string category) {
            if (CategoryToObjects.ContainsKey(category)) return;
            int currentIdx = IndexToCategory.Keys.Count;
            IndexToCategory.Add(currentIdx + 1, category);
            CategoryToObjects.Add(category, new List<ObjectEntry>());
        }

        /// <summary>
        /// Class that represents an entry of the table objectsDir/../heights.csv. 
        /// The list is sorted such that the first entry of each category is a fallback height. 
        /// </summary>
        public class HeightInformation
        {
            public string ObjectClass { get; private set; }
            /// <summary>
            /// Min and max height of an object.
            /// </summary>
            public Vector2 Height { get; private set; }

            public string Category { get; private set; }

            public HeightInformation(string csvLine, char sep) {
                FromString(csvLine, sep);
            }

            void FromString(string csvLine, char sep) {
                CultureInfo ci = new CultureInfo("en-US");
                string[] values = csvLine.Split(sep);
                Category = values[0];
                ObjectClass = values[1];
                Height = new Vector2(Single.Parse(values[2], ci), Single.Parse(values[3], ci));
            }

            public override string ToString() {
                return string.Format("ObjectClass: {0}\nHeight: {1}", ObjectClass, Height);
            }
        }

        /// <summary>
        /// Class that represents an entry of the table objectsDir/objects.csv
        /// </summary>
        public class ObjectEntry
        {
            public string Category { get; private set; }
            public string FileName { get; private set; }
            public string FileDir { get; private set; }
            public string Material { get; private set; }
            /// <summary>
            /// n other files that are saved as (FileName_1, Dir_1), (FileName_2, Dir_2), ..., (FileName_n, Dir_n)
            /// </summary>
            public string OtherFiles { get; private set; }

            public ObjectEntry(string csvLine, char sep) {
                FromString(csvLine, sep);
            }

            void FromString(string csvLine, char sep) {
                string[] values = csvLine.Split(sep);
                Category = values[1];
                FileName = values[2];
                FileDir = values[3];
                Material = values[4];
                OtherFiles = values[5];
            }

            public override string ToString() {
                return string.Format("FileName: {0}\nFileDir: {1}\nMaterial: {2}\n OtherFiles: {3}\nCategory: {4}", FileName, FileDir, Material, OtherFiles, Category);
            }
        }
    }
}
