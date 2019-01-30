﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using DigitalOpus.MB.Core;
using System.IO;
using UltimateFracturing;

namespace ArtPlugins
{
    [CustomEditor(typeof(AggHierarchicalCluster))]
    [DisallowMultipleComponent]
    public class AggHierarchicalClusterEditor : Editor
    {
        #region 打包构建相关
        [MenuItem("GameObject/Create Other/Mesh Baker/UsedInBuildGame/自动合并并替换所有室外物件")]
        public static void ReplacePropsOutHousesIn64Scenes()
        {
            string dir = @"Assets/Maps/maps/0001/Scenes";
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    string path = string.Format("{0}/002 {1}x{2}.unity", dir, i, j);
                    EditorUtility.DisplayProgressBar(string.Empty, path, (i * 8f + j) / 64f);
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    ReplacePropsOutHouses(scene);
                    EditorSceneManager.CloseScene(scene, true);
                    Resources.UnloadUnusedAssets();
                }
            }
            EditorUtility.ClearProgressBar();
        }

        //[MenuItem("Tool/ReplacePropsOutHouses")]
        //public static void ReplacePropsOutHouseInCurrentScenes()
        //{
        //    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        //    {
        //        Scene scene = EditorSceneManager.GetSceneAt(i);
        //        ReplacePropsOutHouses(scene);
        //    }
        //}

        private static void ReplacePropsOutHouses(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogErrorFormat("ReplacePropsOutHouses error, scene is not valid, name:{0}", scene.name);
                return;
            }

            string[] ss = scene.name.Split(' ');
            if (ss.Length != 2)
            {
                Debug.LogErrorFormat("ReplacePropsOutHouses error, scene name is wrong, name:{0}", scene.name);
                return;
            }

            string objName = string.Format("{0}Object", ss[1]);
            var roots = scene.GetRootGameObjects();
            GameObject objGo = null;
            foreach (GameObject root in roots)
            {
                if (root.name.Equals(objName))
                {
                    objGo = root;
                    break;
                }
            }
            if (objGo == null)
            {
                Debug.LogErrorFormat("ReplacePropsOutHouses error, can't find objGo, sceneName:{0} objName:{1}", scene.name, objName);
                return;
            }

            AggHierarchicalCluster cluster = objGo.GetComponent<AggHierarchicalCluster>();
            if (cluster == null)
            {
                cluster = objGo.AddComponent<AggHierarchicalCluster>();
                cluster.maxSize = 40f;
            }
            SplitClusterSpace(cluster);
            ComputeCombineMesh(cluster, cluster.saveDir, true);
        }

        public static void ReplacePropsInsideAndOutsideHouses()
        {
            Debug.LogFormat("++++++++++++++Start ReplacePropsInsideAndOutsideHouses, time:{0}", System.DateTime.Now.ToString());
            MeshCombineBoundsToolEditor.ReplacePropsInHouses();
            ReplacePropsOutHousesIn64Scenes();
            MeshCombineBoundsToolEditor.SetAtlasBundleNames();
            Debug.LogFormat("++++++++++++++Finish ReplacePropsInsideAndOutsideHouses, time:{0}", System.DateTime.Now.ToString());
        }
        #endregion

        private SerializedProperty leavesProp;
        private SerializedProperty maxSizeProp;
        private SerializedProperty leavesGuiFoldProp;
        private SerializedProperty combinesProp;
        private SerializedProperty enableGizmoProp;
        private SerializedProperty gizmoColorProp;
        private SerializedProperty wireModeProp;
        private SerializedProperty showSingleProp;

        private GUIContent buildSpaceSplitGui = new GUIContent("Build Space Split");
        private string splitHelpMsg = "Please click 'Build Space Split' button to preview combines";
        private GUIContent activeGui = new GUIContent("Active");
        private GUIContent disactiveGui = new GUIContent("Disactive");
        private GUIContent locateGui = new GUIContent("Locate");
        private GUIContent centerGui = new GUIContent("Center");
        private GUIContent sizeGui = new GUIContent("Size");
        private GUIContent goesGui = new GUIContent("Gameobjects");
        private GUIContent generateMcGui = new GUIContent("Generate Meshcombines");
        private GUIContent selectGui = new GUIContent("Select");
        private GUIContent saveDirGui = new GUIContent("Save Dir");
        private GUIContent checkErrorsGui = new GUIContent("Check Allocate Errors");

        private GUIStyle _sceneLabelStyle = null;
        private GUIStyle sceneLabelStyle
        {
            get
            {
                if (_sceneLabelStyle == null)
                {
                    _sceneLabelStyle = new GUIStyle(EditorStyles.miniTextField);
                    _sceneLabelStyle.alignment = TextAnchor.MiddleCenter;
                }
                return _sceneLabelStyle;
            }
        }

        private void Awake()
        {
            enableGizmoProp = serializedObject.FindProperty("enableGizmo");
            gizmoColorProp = serializedObject.FindProperty("gizmoColor");
            wireModeProp = serializedObject.FindProperty("wireMode");
            leavesProp = serializedObject.FindProperty("leaves");
            maxSizeProp = serializedObject.FindProperty("maxSize");
            leavesGuiFoldProp = serializedObject.FindProperty("isLeavesGuiFold");
            combinesProp = serializedObject.FindProperty("combines");
            showSingleProp = serializedObject.FindProperty("showSingle");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AggHierarchicalCluster cluster = target as AggHierarchicalCluster;

            EditorGUILayout.PropertyField(enableGizmoProp);
            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(!enableGizmoProp.boolValue);
            EditorGUILayout.PropertyField(gizmoColorProp);
            EditorGUILayout.PropertyField(wireModeProp);
            EditorGUILayout.PropertyField(showSingleProp);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(maxSizeProp);
            if (maxSizeProp.floatValue < 1f) maxSizeProp.floatValue = 1f;
            if (GUILayout.Button(buildSpaceSplitGui))
            {
                if (cluster != null)
                {
                    SplitClusterSpace(cluster);
                    serializedObject.Update();

                    if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Repaint();
                }
            }

            if (cluster.combines.Count > 0)
            {
                // show combines
                EditorGUILayout.BeginVertical(GUI.skin.box);
                for (int i = 0; i < cluster.combines.Count; i++)
                {
                    var combine = cluster.combines[i];
                    var combineProp = combinesProp.GetArrayElementAtIndex(i);
                    var goesProp = combineProp.FindPropertyRelative("gameObjects");
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("MeshCombine" + i.ToString(), GUILayout.MaxWidth(120f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(activeGui))
                    {
                        foreach (GameObject go in combine.gameObjects) if (go != null) go.SetActive(true);
                    }
                    if (GUILayout.Button(disactiveGui))
                    {
                        foreach (GameObject go in combine.gameObjects) if (go != null) go.SetActive(false);
                    }
                    if (GUILayout.Button(selectGui))
                    {
                        Selection.objects = combine.gameObjects.ToArray();
                    }
                    if (GUILayout.Button(locateGui))
                    {
                        if (SceneView.lastActiveSceneView != null)
                        {
                            SceneView.lastActiveSceneView.LookAt(combine.node.bounds.center);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Vector3Field(centerGui, combine.node.bounds.center);
                    EditorGUILayout.Vector3Field(sizeGui, combine.node.bounds.size);
                    EditorGUILayout.PropertyField(goesProp, true);
                    EditorGUI.indentLevel--;
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();

                // error check button
                if (GUILayout.Button(checkErrorsGui))
                {
                    Dictionary<GameObject, List<string>> dict = new Dictionary<GameObject, List<string>>();

                    for (int i = 0; i < cluster.combines.Count; i++)
                    {
                        List<GameObject> goes = cluster.combines[i].gameObjects;
                        for (int k = 0; k < goes.Count; k++)
                        {
                            GameObject go = goes[k];
                            if (go != null)
                            {
                                List<string> list = null;
                                if (!dict.TryGetValue(go, out list))
                                {
                                    list = new List<string>();
                                    dict.Add(go, list);
                                }
                                list.Add("MC" + k.ToString());
                            }
                        }
                    }

                    foreach (var pair in dict)
                    {
                        if (pair.Value.Count > 1)
                        {
                            string msg = string.Empty;
                            foreach (var v in pair.Value) msg += " " + v;
                            Debug.LogErrorFormat(pair.Key, "go:{0} was assigned to multiple meshcombines:{1}", pair.Key.name, msg);
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(splitHelpMsg, MessageType.Info);
            }

            // generate meshcombines button
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(saveDirGui, GUILayout.MaxWidth(70f));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(cluster.saveDir);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button(selectGui, GUILayout.MaxWidth(50f)))
            {
                string dir = EditorUtility.OpenFolderPanel("title", Application.dataPath, null);
                if (!string.IsNullOrEmpty(dir) && dir.StartsWith(Application.dataPath))
                {
                    dir = dir.Replace(Application.dataPath, "Assets");
                    cluster.saveDir = dir;
                    serializedObject.Update();
                }
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button(generateMcGui))
            {
                ComputeCombineMesh(cluster, cluster.saveDir);
            }

            if (this != null) serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (Event.current.alt && enableGizmoProp.boolValue)
            {
                AggHierarchicalCluster cluster = target as AggHierarchicalCluster;
                if (cluster != null)
                {
                    for (int i = 0; i < cluster.combines.Count; i++)
                    {
                        string label = "MC" + i.ToString();
                        Vector3 center = cluster.combines[i].node.bounds.center;
                        Handles.Label(center, label, sceneLabelStyle);
                    }
                }
            }
        }

        public static void SplitClusterSpace(AggHierarchicalCluster cluster)
        {
            if (cluster == null)
            {
                Debug.LogErrorFormat("AggHierarchicalCluster.SplitClusterSpace error, cluster is null");
                return;
            }

            cluster.ClearLeaves();
            for (int i = 0; i < cluster.transform.childCount; i++)
            {
                MultiTag mt = cluster.transform.GetChild(i).GetComponent<MultiTag>();
                if (mt != null && mt.enabled && mt.gameObject.activeSelf && mt.IsOutsideProp() && mt.GetComponentInChildren<FracturedObject>() == null) cluster.AddLeaf(mt.gameObject);
            }
            cluster.BuildBinaryTree();
            cluster.Split();
        }

        public static void ComputeCombineMesh(AggHierarchicalCluster cluster, string saveDir, bool autoReplace = false)
        {
            if (cluster == null)
            {
                Debug.LogError("AggHierarchicalClusterEditor.ComputeCombineMesh error, cluster is null");
                return;
            }

            if (cluster.combines.Count <= 0)
            {
                EditorUtility.DisplayDialog("title", "请分配待合并的网格", "OK");
                return;
            }

            if (string.IsNullOrEmpty(saveDir))
            {
                EditorUtility.DisplayDialog("title", "请选择Baker Result的保存路径", "OK");
                return;
            }

            string sceneName = cluster.gameObject.scene.name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "Empty";
            Debug.LogFormat("+++++++++++++++++start combine outside props, sceneName:{0} time:{1} maxSize:{2}", sceneName, System.DateTime.Now.ToString(), cluster.maxSize);

            // handle each combine mesh
            List<GameObject> mcs = new List<GameObject>();
            for (int i = 0; i < cluster.combines.Count; i++)
            {
                List<GameObject> goes = cluster.combines[i].gameObjects;
                List<GameObject> mrs = new List<GameObject>();
                List<MultiTagBase> mts = new List<MultiTagBase>();

                // 单物体无需合并
                if (goes.Count <= 0 || !cluster.combines[i].multiGoesFlag) continue;

                foreach (GameObject go in goes)
                {
                    if (go != null)
                    {
                        var rs = go.GetComponentsInChildren<MeshRenderer>();
                        foreach (var r in rs)
                        {
                            if (r != null)
                            {
                                bool ok = false;
                                if (r.sharedMaterials.Length > 0)
                                {
                                    MeshFilter mf = r.GetComponent<MeshFilter>();
                                    if (mf != null && mf.sharedMesh != null) ok = true;

                                    if (ok)
                                    {
                                        foreach (var mat in r.sharedMaterials)
                                        {
                                            if (mat == null)
                                            {
                                                ok = false;
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (ok)
                                {
                                    mrs.Add(r.gameObject);
                                }
                                else
                                {
                                    Debug.LogErrorFormat(r.gameObject, "{0}'s {1} miss materials and meshes, sceneName:{2} time:{3} maxSize:{4} MC{5}",
                                        go.name, r.gameObject.name, sceneName, System.DateTime.Now.ToString(), cluster.maxSize, i.ToString());
                                }
                            }
                        }

                        var mt = go.GetComponent<MultiTag>();
                        if (mt != null) mts.Add(mt);
                    }
                }

                if (mrs.Count <= 0)
                {
                    Debug.LogFormat("{0}'s MeshCombine{1} doesn't contains any meshrenderers with maxsize:{2}, please check carefully", sceneName, i, cluster.maxSize);
                    continue;
                }

                Debug.LogFormat("start combine{0} sceneName:{1} time:{2} maxSize:{3}", i.ToString(), sceneName, System.DateTime.Now.ToString(), cluster.maxSize);

                // generate baker
                GameObject bakerGo = new GameObject(string.Format("TextureBaker({0}-MC{1})", sceneName, i.ToString()));
                bakerGo.transform.position = Vector3.zero;
                MB3_MeshBakerGrouper grouper = bakerGo.AddComponent<MB3_MeshBakerGrouper>();
                grouper.data.clusterByLODLevel = true;
                MB3_TextureBaker baker = bakerGo.AddComponent<MB3_TextureBaker>();
                baker.packingAlgorithm = MB2_PackingAlgorithmEnum.MeshBakerTexturePacker;
                baker.maxAtlasSize = 4096;
                baker.atlasPadding = 0;
                baker.doMultiMaterial = true;
                baker.doMultiMaterialSplitAtlasesIfOBUVs = true;
                baker.doMultiMaterialSplitAtlasesIfTooBig = true;
                baker.considerNonTextureProperties = false;
                List<GameObject> gs = baker.GetObjectsToCombine();
                gs.Clear();
                foreach (GameObject go in mrs)
                {
                    if (go != null && !gs.Contains(go)) gs.Add(go);
                }

                Debug.LogFormat("finish generate baker:{0} sceneName:{1} maxSize:{2} time:{3}", i.ToString(), sceneName, cluster.maxSize, System.DateTime.Now.ToString());

                // create and save bake results
                string dir = string.Format("{0}/{1}/MC{2}", saveDir, sceneName, i);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                string resultPath = string.Format("{0}/BakerResult.asset", dir);
                MB3_TextureBakerEditorInternal.CreateCombinedMaterialAssets(baker, resultPath);

                Debug.LogFormat("finish create&save bake result:{0} time:{1} sceneName:{2} maxSize:{3}", i.ToString(), System.DateTime.Now.ToString(), sceneName, cluster.maxSize);

                // configure multiple materials
                SerializedObject so = new SerializedObject(baker);
                SerializedProperty resultMatsProp = so.FindProperty("resultMaterials");
                MB3_TextureBakerEditorInternal.ConfigureMutiMaterialsFromObjsToCombine(baker, resultMatsProp, so);

                Debug.LogFormat("finish configure multiple materials:{0} time:{1} sceneName:{2} maxSize:{3}", i.ToString(), System.DateTime.Now.ToString(), sceneName, cluster.maxSize);

                // bake materials into combined material
                baker.CreateAtlases((msg, progress) => { EditorUtility.DisplayProgressBar("Combining Meshes", msg, progress); }, true, new MB3_EditorMethods());
                EditorUtility.ClearProgressBar();
                if (baker.textureBakeResults != null) EditorUtility.SetDirty(baker.textureBakeResults);

                Debug.LogFormat("finish create atlases:{0} time:{1} sceneName:{2} maxSize:{3}", i.ToString(), System.DateTime.Now.ToString(), sceneName, cluster.maxSize);

                // generate mesh bakers
                if (grouper.grouper == null) grouper.grouper = grouper.CreateGrouper(grouper.clusterType, grouper.data);
                if (grouper.grouper.d == null) grouper.grouper.d = grouper.data;
                grouper.grouper.DoClustering(baker, grouper);

                Debug.LogFormat("finish generate mesh bakers:{0} time:{1} sceneName:{2} maxSize:{3}", i.ToString(), System.DateTime.Now.ToString(), sceneName, cluster.maxSize);

                // bake all child meshbakers
                MB3_MeshBakerCommon[] meshBakers = grouper.GetComponentsInChildren<MB3_MeshBakerCommon>();
                for (int j = 0; j < meshBakers.Length; j++)
                {
                    bool createdDummyMaterialBakeResult;
                    MB3_MeshBakerEditorFunctions.BakeIntoCombined(meshBakers[j], out createdDummyMaterialBakeResult);
                }

                Debug.LogFormat("finish bake all child meshbakers:{0} time:{1} sceneName:{2} maxSize:{3}", i.ToString(), System.DateTime.Now.ToString(), sceneName, cluster.maxSize);

                // re-layout combined meshes
                GameObject mcGo = new GameObject(string.Format("{0}-MC{1}", sceneName, i), typeof(LODGroup));
                SceneManager.MoveGameObjectToScene(mcGo, cluster.gameObject.scene);
                mcGo.transform.position = cluster.combines[i].node.bounds.center;
                for (int j = 0; j < meshBakers.Length; j++)
                {
                    MB3_MeshBakerCommon meshBaker = meshBakers[j];
                    GameObject resultGo = meshBaker.meshCombiner.resultSceneObject;
                    if (resultGo == null)
                    {
                        Debug.LogErrorFormat("can't find resultGo, meshbakers:{0} time:{1} sceneName:{2} maxSize:{3}", i.ToString(), System.DateTime.Now.ToString(), sceneName, cluster.maxSize);
                        continue;
                    }
                    Transform child = resultGo.transform.GetChild(0);
                    child.SetParent(mcGo.transform);
                    child.name = mcGo.name + "_LOD" + j.ToString();
                    DestroyImmediate(meshBaker.meshCombiner.resultSceneObject);
                }

                // set lod
                LODGroup lodGroup = mcGo.GetComponent<LODGroup>();
                var lods = lodGroup.GetLODs();
                for (int j = 0; j < mcGo.transform.childCount; j++)
                {
                    if (j < lods.Length)
                    {
                        Renderer r = mcGo.transform.GetChild(j).GetComponent<Renderer>();
                        lods[j].renderers = new Renderer[] { r };
                    }
                }
                lodGroup.SetLODs(lods);

                // combine mesh colliders
                List<Collider> colliders = new List<Collider>();
                foreach (GameObject g in gs)
                {
                    MultiTag mt = g.GetComponentInParent<MultiTag>();
                    if (mt != null)
                    {
                        Collider[] cs = mt.GetComponentsInChildren<Collider>();
                        colliders.AddRange(cs);
                    }
                }
                int num = 0;
                foreach (Collider collider in colliders)
                {
                    GameObject colliderGo = Instantiate(collider.gameObject);
                    colliderGo.name = collider.gameObject.name + "_" + num.ToString();
                    colliderGo.transform.SetParent(mcGo.transform);
                    colliderGo.transform.position = collider.gameObject.transform.position;
                    colliderGo.transform.localScale = Vector3.one;
                    colliderGo.transform.rotation = collider.gameObject.transform.rotation;

                    MeshRenderer mr = colliderGo.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        DestroyImmediate(mr);
                        mr = null;
                    }

                    num++;
                }

                Debug.LogFormat("finish re-layout&set lod&combine colliders:{0} time:{1} sceneName:{2} maxSize:{3}", i.ToString(), System.DateTime.Now.ToString(), sceneName, cluster.maxSize);

                // save all meshes assets
                Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
                var mfList = mcGo.GetComponentsInChildren<MeshFilter>();
                foreach (var mf in mfList)
                {
                    if (mf != null && mf.sharedMesh != null)
                    {
                        MeshUtility.Optimize(mf.sharedMesh);
                        meshes.Add(mf.name, mf.sharedMesh);
                    }
                }
                foreach (var pair in meshes)
                {
                    string tempPath = AssetDatabase.GetAssetPath(pair.Value);
                    if (string.IsNullOrEmpty(tempPath))
                    {
                        string meshPath = string.Format("{0}/{1}.asset", dir, pair.Key);
                        AssetDatabase.CreateAsset(pair.Value, meshPath);
                    }
                }

                // set multi tag
                MultiTag newMt = mcGo.AddComponent<MultiTag>();
                MultiTag.CombineMultiTags(newMt, mts);

                // destroy bakerGo
                if (bakerGo != null)
                {
                    DestroyImmediate(bakerGo);
                    bakerGo = null;
                }

                // record combined mesh gameObject
                mcs.Add(mcGo);

                Debug.LogFormat("finish save all meshes&set multi tag&destroy bakerGo:{0} time:{1} sceneName:{2} maxSize:{3}", i.ToString(), System.DateTime.Now.ToString(), sceneName, cluster.maxSize);
            }

            Debug.LogFormat("+++++++++++++++++finish combine outside props, sceneName:{0} time:{1}", sceneName, System.DateTime.Now.ToString());

            // replace origin props
            if (autoReplace && mcs.Count > 0)
            {
                Scene scene = cluster.gameObject.scene;

                // re-parent combined mesh gameobject
                for (int i = 0; i < mcs.Count; i++)
                {
                    mcs[i].transform.SetParent(cluster.transform);
                }

                // delete orgin props
                for (int i = 0; i < cluster.combines.Count; i++)
                {
                    List<GameObject> goes = cluster.combines[i].gameObjects;
                    for (int k = goes.Count - 1; k >= 0; k--)
                    {
                        if (goes[k] != null) DestroyImmediate(goes[k]);
                    }
                }

                // remove AggHierarchicalCluster Component
                DestroyImmediate(cluster);
                cluster = null;

                // save scene
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.LogFormat("+++++++++++++++++finish replace outside props, sceneName:{0} time:{1}", sceneName, System.DateTime.Now.ToString());
        }
    }
}