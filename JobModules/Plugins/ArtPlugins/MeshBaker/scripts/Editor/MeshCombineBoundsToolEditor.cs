﻿
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using DigitalOpus.MB.Core;
using UltimateFracturing;

namespace ArtPlugins
{
    [CustomEditor(typeof(MeshCombineBoundsTool))]
    [DisallowMultipleComponent]
    public class MeshCombineBoundsToolEditor : Editor
    {
        #region 自动化打包替换相关

        [MenuItem("GameObject/Create Other/Mesh Baker/UsedInBuildGame/设置0001Map物件图集Bundle名称")]
        public static void SetAtlasBundleNames()
        {
            string dir = @"Assets/Maps/maps/0001/CombineMeshesResults";
            string[] guids = AssetDatabase.FindAssets("t:Texture", new string[] { dir });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;

                string[] ss = tex.name.Split('-');
                string bundle = "none";
                if (ss.Length > 2) bundle = ss[2];

                path = path.Replace('\\', '/');
                ss = path.Split('/');
                string texDir = ss[ss.Length - 2];
                string texParentDir = ss[ss.Length - 3];

                bundle = string.Format("maps_textures/{0}_{1}_{2}", texParentDir, texDir, bundle);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null) importer.SetAssetBundleNameAndVariant(bundle, null);
            }
        }

        /// <summary>
        /// 用合并替换房间内的小物件
        /// </summary>
        [MenuItem("GameObject/Create Other/Mesh Baker/UsedInBuildGame/自动合并并替换所有室内物件")]
        public static void ReplacePropsInHouses()
        {
            string[] dirs = new string[] { "Assets/Maps/Prefabs/building" };
            string[] guids = AssetDatabase.FindAssets("t:Prefab", dirs);

            Debug.LogFormat("===================start replace props in houses:{0}======================", System.DateTime.Now.ToString());
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EditorUtility.DisplayProgressBar("title", path, (float)count++ / guids.Length);

                Debug.LogFormat("load and Instantiate house:{0} start time:{1}", path, System.DateTime.Now.ToString());
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                Debug.LogFormat("load and Instantiate house:{0} finish time:{1}", path, System.DateTime.Now.ToString());

                MeshCombineBoundsTool meshCombineBoundsTool = instance.GetComponent<MeshCombineBoundsTool>();
                if (meshCombineBoundsTool == null)
                {
                    Debug.LogErrorFormat("Building:{0} prefab can't find MeshCombineBoundsTool component", path);
                    DestroyImmediate(instance);
                    continue;
                }

                if (meshCombineBoundsTool.checkError)
                {
                    Debug.LogErrorFormat("Building:{0} prefab find combine error flag, please first fix the error", path);
                    DestroyImmediate(instance);
                    continue;
                }

                if (string.IsNullOrEmpty(meshCombineBoundsTool.bakerResultPath))
                {
                    Debug.LogErrorFormat("Building:{0} prefab can't find save result path, please first assign the save path", path);
                    DestroyImmediate(instance);
                    continue;
                }

                if (meshCombineBoundsTool.combines.Count <= 0)
                {
                    Debug.LogErrorFormat("Building:{0} prefab can't any meshes need to be combined", path);
                    DestroyImmediate(instance);
                    continue;
                }

                // combine and replace props
                Debug.LogFormat("combine house:{0} start time:{1}", path, System.DateTime.Now.ToString());
                MeshCombineBoundsToolEditor.ComputeCombineMesh(meshCombineBoundsTool, meshCombineBoundsTool.bakerResultPath, true);
                Debug.LogFormat("combine house:{0} finish time:{1}", path, System.DateTime.Now.ToString());

                // replace the prefab
                Debug.LogFormat("replace house:{0} start time:{1}", path, System.DateTime.Now.ToString());
                PrefabUtility.ReplacePrefab(instance, prefab, ReplacePrefabOptions.ConnectToPrefab);
                Debug.LogFormat("replace house:{0} finish time:{1}", path, System.DateTime.Now.ToString());

                // destroy instance
                DestroyImmediate(instance);
            }
            EditorUtility.ClearProgressBar();
            Debug.LogFormat("===================end replace props in houses:{0}======================", System.DateTime.Now.ToString());
        }

        #endregion

        private string noMeshCombineTip = "There is no combined meshes, please click plus button to add one";
        private GUIContent minusIconGui, plusIconGui;
        private GUIContent grabGui = new GUIContent("Grab");
        private GUIContent fitPropsGui = new GUIContent("Fit Bound");
        private GUIContent locateGui = new GUIContent("Locate");
        private GUIContent copyGui = new GUIContent("Copy");
        private GUIContent pasteGui = new GUIContent("Paste");
        private GUIContent goesGui = new GUIContent("Goes");
        private GUIContent removeGui = new GUIContent("remove");
        private GUIContent dragDropGui = new GUIContent("please drag gameObjects and drop here to add them to goes list!");
        private GUIContent selectGui = new GUIContent("select");
        private GUIContent bakerResultPathGui = new GUIContent("Baker Result Path");

        private GUIContent generateMCsGui = new GUIContent("Generate MeshCombines");
        private GUIContent generateMCsWithReplaceGui = new GUIContent("Generate MeshCombines (Automatically Replace)");

        private SerializedProperty combinesProp;
        private SerializedProperty enableHandleProp;
        private SerializedProperty enableGizmoProp, wireModeProp, gizmoColorProp;
        private SerializedProperty bakerResultPathProp;
        private SerializedProperty checkErrorProp;

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

        private GUIStyle _boxLabelStyle = null;
        private GUIStyle boxLabelStyle
        {
            get
            {
                if (_boxLabelStyle == null)
                {
                    _boxLabelStyle = new GUIStyle(EditorStyles.textArea);
                    _boxLabelStyle.alignment = TextAnchor.MiddleCenter;
                }
                return _boxLabelStyle;
            }
        }

        private MeshCombineBoundsTool meshCombineBoundsTool;

        private void Awake()
        {
            minusIconGui = new GUIContent(EditorGUIUtility.IconContent("Toolbar Minus"));
            plusIconGui = new GUIContent(EditorGUIUtility.IconContent("Toolbar Plus"));

            combinesProp = serializedObject.FindProperty("combines");
            enableHandleProp = serializedObject.FindProperty("enableHandle");
            enableGizmoProp = serializedObject.FindProperty("enableGizmo");
            wireModeProp = serializedObject.FindProperty("wireMode");
            gizmoColorProp = serializedObject.FindProperty("gizmoColor");
            bakerResultPathProp = serializedObject.FindProperty("bakerResultPath");
            checkErrorProp = serializedObject.FindProperty("checkError");

            meshCombineBoundsTool = target as MeshCombineBoundsTool;
            if (meshCombineBoundsTool != null) meshCombineBoundsTool.InitAllProps();
        }

        private void OnDestroy()
        {
            if (meshCombineBoundsTool != null) meshCombineBoundsTool.ClearAllProps();
        }

        public override void OnInspectorGUI()
        {
            meshCombineBoundsTool = target as MeshCombineBoundsTool;
            bool checkError = false;
            bool dragFrame = false;

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(enableHandleProp);
            EditorGUILayout.PropertyField(enableGizmoProp);
            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(!enableGizmoProp.boolValue);
            EditorGUILayout.PropertyField(wireModeProp);
            EditorGUILayout.PropertyField(gizmoColorProp);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;

            EditorGUILayout.Separator();
            if (meshCombineBoundsTool.combines.Count <= 0)
            {
                // no combined meshes tip
                EditorGUILayout.HelpBox(noMeshCombineTip, MessageType.None);
            }
            else
            {
                int delCombineKey = -1;
                for (int i = 0; i < meshCombineBoundsTool.combines.Count; i++)
                {
                    var meshCombine = meshCombineBoundsTool.combines[i];

                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("MeshCombine" + i.ToString());
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(minusIconGui) && EditorUtility.DisplayDialog("title", "是否确定删除该合并网格?", "OK", "Cancel"))
                    {
                        delCombineKey = i;
                    }
                    EditorGUILayout.EndHorizontal();

                    SerializedProperty boundsProp = combinesProp.GetArrayElementAtIndex(i).FindPropertyRelative("bounds");

                    int delBoundKey = -1;
                    for (int k = 0; k < meshCombine.bounds.Count; k++)
                    {
                        SerializedProperty boundProp = boundsProp.GetArrayElementAtIndex(k);
                        SerializedProperty centerProp = boundProp.FindPropertyRelative("center");
                        SerializedProperty sizeProp = boundProp.FindPropertyRelative("size");
                        SerializedProperty rotationProp = boundProp.FindPropertyRelative("rotation");
                        SerializedProperty goesFoldProp = boundProp.FindPropertyRelative("goesFold");

                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Bound" + k.ToString(), GUILayout.MaxWidth(50f));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(grabGui, GUILayout.MaxHeight(22f)))
                        {
                            Vector3 pos = meshCombineBoundsTool.transform.TransformPoint(centerProp.vector3Value);
                            Quaternion rot = meshCombineBoundsTool.transform.rotation * Quaternion.Euler(rotationProp.vector3Value);
                            Collider[] cs = Physics.OverlapBox(pos, sizeProp.vector3Value / 2f, rot);
                            List<GameObject> list = new List<GameObject>();
                            foreach (var c in cs)
                            {
                                if (c == null) continue;

                                MultiTag mt = c.GetComponentInParent<MultiTag>();
                                if (mt == null || !mt.IsProp()) continue;

                                Door door = mt.GetComponent<Door>();
                                if (door != null) continue;

                                if (!list.Contains(mt.gameObject)) list.Add(mt.gameObject);
                            }

                            meshCombine.bounds[k].AddGameObjects(list);
                            serializedObject.Update();
                        }
                        if (GUILayout.Button(fitPropsGui, GUILayout.MaxHeight(22f)))
                        {
                            int count = meshCombine.bounds[k].GetGoesCount();
                            List<Renderer> rs = new List<Renderer>();
                            for (int j = 0; j < count; j++)
                            {
                                GameObject go = meshCombine.bounds[k].GetGameObject(j);
                                if (go != null)
                                {
                                    Renderer[] rr = go.GetComponentsInChildren<Renderer>(true);
                                    rs.AddRange(rr);
                                }
                            }
                            if (rs.Count > 0)
                            {
                                Bounds bounds = rs[0].bounds;
                                foreach (Renderer r in rs)
                                {
                                    if (r != null)
                                    {
                                        bounds.Encapsulate(r.bounds);
                                    }
                                }
                                centerProp.vector3Value = meshCombineBoundsTool.transform.InverseTransformPoint(bounds.center);
                                sizeProp.vector3Value = bounds.size;
                                rotationProp.vector3Value = Quaternion.Inverse(meshCombineBoundsTool.transform.rotation).eulerAngles;
                            }
                        }
                        if (GUILayout.Button(locateGui, GUILayout.MaxHeight(22f)))
                        {
                            if (SceneView.lastActiveSceneView != null)
                            {
                                Vector3 center = meshCombineBoundsTool.transform.TransformPoint(meshCombine.bounds[k].center);
                                SceneView.lastActiveSceneView.LookAt(center);
                            }
                        }
                        if (GUILayout.Button(copyGui, GUILayout.MaxHeight(22f)))
                        {
                            string buffer = string.Format("copybounds->|{0},{1},{2}|{3},{4},{5}|{6},{7},{8}",
                            centerProp.vector3Value.x, centerProp.vector3Value.y, centerProp.vector3Value.z,
                            sizeProp.vector3Value.x, sizeProp.vector3Value.y, sizeProp.vector3Value.z,
                            rotationProp.vector3Value.x, rotationProp.vector3Value.y, rotationProp.vector3Value.z);
                            EditorGUIUtility.systemCopyBuffer = buffer;
                        }
                        if (GUILayout.Button(pasteGui, GUILayout.MaxHeight(22f)))
                        {
                            if (string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer) || !EditorGUIUtility.systemCopyBuffer.StartsWith("copybounds->"))
                            {
                                EditorUtility.DisplayDialog("title", "不存在合适的拷贝数据", "OK");
                            }
                            else
                            {
                                string[] ss = EditorGUIUtility.systemCopyBuffer.Split('|');

                                // center
                                string[] values = ss[1].Split(',');
                                centerProp.vector3Value = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));

                                // size
                                values = ss[2].Split(',');
                                sizeProp.vector3Value = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));

                                // rotation
                                values = ss[3].Split(',');
                                rotationProp.vector3Value = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
                            }
                        }
                        EditorGUI.BeginDisabledGroup(meshCombine.bounds.Count == 1);
                        if (GUILayout.Button(minusIconGui) && EditorUtility.DisplayDialog("title", "是否确定删除该包围盒", "OK", "Cancel"))
                        {
                            delBoundKey = k;
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(centerProp);
                        EditorGUILayout.PropertyField(sizeProp);
                        EditorGUILayout.PropertyField(rotationProp);
                        if (meshCombine.bounds[k].CheckBoundDuplicate() || meshCombine.bounds[k].CheckIllegalProps())
                            serializedObject.Update();
                        // EditorGUILayout.PropertyField(gameObjectsProp, true);
                        goesFoldProp.boolValue = EditorGUILayout.Foldout(goesFoldProp.boolValue, goesGui);
                        if (goesFoldProp.boolValue)
                        {
                            int delGoKey = -1;
                            EditorGUI.indentLevel++;
                            int count = meshCombine.bounds[k].GetGoesCount();
                            for (int j = 0; j < count; j++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.ObjectField(meshCombine.bounds[k].GetGameObject(j), typeof(GameObject), false);
                                EditorGUI.EndDisabledGroup();
                                if (GUILayout.Button(removeGui))
                                {
                                    delGoKey = j;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                            if (delGoKey != -1)
                            {
                                meshCombine.bounds[k].RemoveOneGameObject(delGoKey);
                                serializedObject.Update();

                                if (SceneView.lastActiveSceneView != null)
                                {
                                    SceneView.lastActiveSceneView.Repaint();
                                }
                            }

                            Rect rect = GUILayoutUtility.GetRect(0, 30f, GUILayout.ExpandWidth(true));
                            GUI.Box(rect, dragDropGui, boxLabelStyle);
                            switch (Event.current.type)
                            {
                                case EventType.DragUpdated:
                                case EventType.DragPerform:
                                    {
                                        if (rect.Contains(Event.current.mousePosition))
                                        {
                                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                                            if (Event.current.type == EventType.DragPerform)
                                            {
                                                dragFrame = true;
                                                DragAndDrop.AcceptDrag();

                                                foreach (object obj in DragAndDrop.objectReferences)
                                                {
                                                    GameObject objGo = obj as GameObject;
                                                    if (objGo != null)
                                                    {
                                                        meshCombine.bounds[k].AddOneGameObject(objGo);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        EditorGUI.indentLevel--;
                        EditorGUILayout.EndVertical();
                    }
                    if (delBoundKey != -1)
                    {
                        meshCombine.RemoveOneBounds(delBoundKey);
                        serializedObject.Update();

                        if (SceneView.lastActiveSceneView != null)
                        {
                            SceneView.lastActiveSceneView.Repaint();
                        }
                    }

                    {
                        // check meshcombine duplicate
                        if (!dragFrame)
                        {
                            var dict = meshCombine.CheckMCDuplicate();
                            bool firstShow = true;
                            foreach (var pair in dict)
                            {
                                if (pair.Value.Count > 1 && firstShow)
                                {
                                    EditorGUILayout.BeginVertical(GUI.skin.box);
                                    EditorGUILayout.HelpBox("Some gameObjects were assigned to multiple bounds, please fix them", MessageType.Error);
                                    firstShow = false;
                                }

                                if (pair.Value.Count > 1)
                                {
                                    EditorGUI.indentLevel++;
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.ObjectField(pair.Key, typeof(GameObject), true);
                                    EditorGUI.EndDisabledGroup();
                                    string msg = string.Empty;
                                    foreach (int v in pair.Value)
                                    {
                                        msg += string.Format(" Bound{0}", v.ToString());
                                    }
                                    EditorGUILayout.LabelField(msg);
                                    EditorGUI.indentLevel--;
                                    if (!checkError) checkError = true;
                                }
                            }
                            if (!firstShow) EditorGUILayout.EndVertical();
                        }
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(plusIconGui))
                    {
                        Vector3 center = Vector3.zero;
                        if (SceneView.lastActiveSceneView != null)
                        {
                            center = meshCombineBoundsTool.transform.InverseTransformPoint(SceneView.lastActiveSceneView.pivot);
                        }
                        meshCombine.AddOneBounds(center);
                        serializedObject.Update();
                        if (SceneView.lastActiveSceneView != null)
                        {
                            SceneView.lastActiveSceneView.Repaint();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
                if (delCombineKey != -1)
                {
                    meshCombineBoundsTool.RemoveOneMeshCombine(delCombineKey);
                    serializedObject.Update();

                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.Repaint();
                    }
                }

                // check duplicate
                {
                    if (!dragFrame)
                    {
                        var dict = meshCombineBoundsTool.CheckMCDuplicate();
                        bool firstShow = true;
                        foreach (var pair in dict)
                        {
                            if (pair.Value.Count > 1 && firstShow)
                            {
                                EditorGUILayout.BeginVertical(GUI.skin.box);
                                EditorGUILayout.HelpBox("Some gameObjects were assigned to multiple meshcombines and bounds, please fix them", MessageType.Error);
                                firstShow = false;
                            }

                            if (pair.Value.Count > 1)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.ObjectField(pair.Key, typeof(GameObject), true);
                                EditorGUI.EndDisabledGroup();
                                string msg = string.Empty;
                                foreach (string s in pair.Value)
                                {
                                    msg += " " + s;
                                }
                                EditorGUILayout.LabelField(msg);
                                EditorGUI.indentLevel--;
                                if (!checkError) checkError = true;
                            }
                        }
                        if (!firstShow) EditorGUILayout.EndVertical();
                    }
                }
            }

            // plus combinedmesh button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(plusIconGui))
            {
                Vector3 center = Vector3.zero;
                if (SceneView.lastActiveSceneView != null)
                {
                    center = meshCombineBoundsTool.transform.InverseTransformPoint(SceneView.lastActiveSceneView.pivot);
                }
                meshCombineBoundsTool.AddOneMeshCombine(center);
                serializedObject.Update();
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();

            // check unassigned props
            {
                List<GameObject> list = meshCombineBoundsTool.CheckUnassignedProps();
                if (list != null && list.Count > 0)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.HelpBox("Some gameObjects were unassigned, please check carefully", MessageType.Warning);
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(true);
                    foreach (GameObject go in list)
                    {
                        if (go != null)
                        {
                            EditorGUILayout.ObjectField(go, typeof(GameObject), true);
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                }
            }

            // generateBakersGui
            EditorGUILayout.Separator();
            if (!dragFrame)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(bakerResultPathGui, GUILayout.MaxWidth(110f));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(GUIContent.none, bakerResultPathProp.stringValue);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button(selectGui, GUILayout.MaxWidth(50f)))
                {
                    string folder = EditorUtility.OpenFolderPanel("title", bakerResultPathProp.stringValue, null);
                    if (!string.IsNullOrEmpty(folder))
                    {
                        if (folder.Contains(Application.dataPath))
                        {
                            bakerResultPathProp.stringValue = folder.Replace(Application.dataPath, "Assets");
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("title", "请选择工程内的文件夹", "OK");
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button(generateMCsGui))
            {
                if (checkError)
                {
                    EditorUtility.DisplayDialog("title", "请先修复检查出的错误", "OK");
                }
                else
                {
                    ComputeCombineMesh(meshCombineBoundsTool, bakerResultPathProp.stringValue, false);
                }
            }
            // if (GUILayout.Button(generateMCsWithReplaceGui))
            // {
            //     if (checkError)
            //     {
            //         EditorUtility.DisplayDialog("title", "请先修复检查出的错误", "OK");
            //     }
            //     else
            //     {
            //         ComputeCombineMesh(meshCombineBoundsTool, bakerResultPathProp.stringValue, true);

            //         // because has delete the component so stop to continue
            //         return;
            //     }
            // }

            // record error flag
            checkErrorProp.boolValue = checkError;

            // apply changes
            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI()
        {
            if (meshCombineBoundsTool == null || !meshCombineBoundsTool.enableGizmo) return;

            for (int i = 0; i < meshCombineBoundsTool.combines.Count; i++)
            {
                for (int k = 0; k < meshCombineBoundsTool.combines[i].bounds.Count; k++)
                {
                    var st = meshCombineBoundsTool.combines[i].bounds[k];
                    Vector3 pos = st.center;
                    pos = meshCombineBoundsTool.transform.TransformPoint(pos);

                    if (enableHandleProp.boolValue)
                    {
                        Vector3 rotation = st.rotation;
                        Quaternion rot = meshCombineBoundsTool.transform.rotation * Quaternion.Euler(rotation);
                        Vector3 size = st.size;

                        EditorGUI.BeginChangeCheck();
                        if (UnityEditor.Tools.current == UnityEditor.Tool.Rotate)               // rotation handle
                        {
                            rot = Handles.RotationHandle(rot, pos);
                        }
                        else if (UnityEditor.Tools.current == UnityEditor.Tool.Scale)           // scale handle
                        {
                            size = Handles.ScaleHandle(size, pos, rot, 1);
                        }
                        else                                                                    // move handle
                        {
                            pos = Handles.PositionHandle(pos, rot);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(meshCombineBoundsTool, "change TRS");

                            pos = meshCombineBoundsTool.transform.InverseTransformPoint(pos);
                            rot = Quaternion.Inverse(meshCombineBoundsTool.transform.rotation) * rot;
                            st.center = pos;
                            st.rotation = rot.eulerAngles;
                            st.size = size;

                            serializedObject.Update();
                        }
                    }

                    if (Event.current.alt)
                    {
                        string label = string.Format("MC{0}-Bound{1}", i, k);
                        Handles.Label(pos, label, sceneLabelStyle);
                    }
                }
            }
        }

        public static void ComputeCombineMesh(MeshCombineBoundsTool meshCombineBoundsTool, string saveDir, bool autoReplace = false)
        {
            if (meshCombineBoundsTool == null)
            {
                Debug.LogError("MeshCombineBoundsToolEditor.ComputeCombineMesh error, meshCombineBoundsTool is null");
                return;
            }

            if (meshCombineBoundsTool.combines.Count <= 0)
            {
                EditorUtility.DisplayDialog("title", "请分配待合并的网格", "OK");
                return;
            }

            if (string.IsNullOrEmpty(saveDir))
            {
                EditorUtility.DisplayDialog("title", "请选择Baker Result的保存路径", "OK");
                return;
            }

            string buildingName = meshCombineBoundsTool.name;
            Debug.LogFormat("++++++++++start combine building:{0} time:{1}", buildingName, System.DateTime.Now.ToString());

            List<GameObject> mcs = new List<GameObject>();
            for (int i = 0; i < meshCombineBoundsTool.combines.Count; i++)
            {
                List<GameObject> mrs = new List<GameObject>();
                List<MultiTagBase> mts = new List<MultiTagBase>();
                for (int j = 0; j < meshCombineBoundsTool.combines[i].bounds.Count; j++)
                {
                    int count = meshCombineBoundsTool.combines[i].bounds[j].GetGoesCount();
                    for (int k = 0; k < count; k++)
                    {
                        GameObject go = meshCombineBoundsTool.combines[i].bounds[j].GetGameObject(k);
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
                                        Debug.LogErrorFormat(r.gameObject, "{0}'s {1} miss materials and meshes, buildName:{2} MC{3} Bounds{4} time:{5}",
                                            go.name, r.gameObject.name, buildingName, i, j, System.DateTime.Now.ToString());
                                    }
                                }

                                var mt = go.GetComponent<MultiTag>();
                                if (mt != null) mts.Add(mt);
                            }
                        }
                    }
                }

                if (mrs.Count <= 0)
                {
                    Debug.LogErrorFormat("{0}'s MeshCombine{1} doesn't contains any meshrenderers, please check carefully!!!", buildingName, i);
                    continue;
                }

                Debug.LogFormat("start combine{0} time:{1}", i.ToString(), System.DateTime.Now.ToString());

                // generate baker
                GameObject bakerGo = new GameObject(string.Format("TextureBaker({0}-MC{1})", buildingName, i.ToString()));
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

                Debug.LogFormat("finish generate baker:{0}, time:{1}", i.ToString(), System.DateTime.Now.ToString());               // 0s

                // create and save bake results
                string dir = string.Format("{0}/{1}/MC{2}", saveDir, buildingName, i);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                string resultPath = string.Format("{0}/BakerResult.asset", dir);
                MB3_TextureBakerEditorInternal.CreateCombinedMaterialAssets(baker, resultPath);

                Debug.LogFormat("finish create&save bake result:{0} time:{1}", i.ToString(), System.DateTime.Now.ToString());       // 2s

                // configure multiple materials
                SerializedObject so = new SerializedObject(baker);
                SerializedProperty resultMatsProp = so.FindProperty("resultMaterials");
                MB3_TextureBakerEditorInternal.ConfigureMutiMaterialsFromObjsToCombine(baker, resultMatsProp, so);

                Debug.LogFormat("finish configure multiple materials:{0} time:{1}", i.ToString(), System.DateTime.Now.ToString());  // 1s

                // bake materials into combined material
                baker.CreateAtlases((msg, progress) => { EditorUtility.DisplayProgressBar("Combining Meshes", msg, progress); }, true, new MB3_EditorMethods());
                EditorUtility.ClearProgressBar();
                if (baker.textureBakeResults != null) EditorUtility.SetDirty(baker.textureBakeResults);

                Debug.LogFormat("finish create atlases:{0} time:{1}", i.ToString(), System.DateTime.Now.ToString());                // 18s

                // generate mesh bakers
                if (grouper.grouper == null) grouper.grouper = grouper.CreateGrouper(grouper.clusterType, grouper.data);
                if (grouper.grouper.d == null) grouper.grouper.d = grouper.data;
                grouper.grouper.DoClustering(baker, grouper);

                Debug.LogFormat("finish generate mesh bakers:{0} time:{1}", i.ToString(), System.DateTime.Now.ToString());          // 0s         

                // bake all child meshbakers
                MB3_MeshBakerCommon[] meshBakers = grouper.GetComponentsInChildren<MB3_MeshBakerCommon>();
                for (int j = 0; j < meshBakers.Length; j++)
                {
                    bool createdDummyMaterialBakeResult;
                    MB3_MeshBakerEditorFunctions.BakeIntoCombined(meshBakers[j], out createdDummyMaterialBakeResult);
                }

                Debug.LogFormat("finish bake all child meshbakers:{0} time:{1}", i.ToString(), System.DateTime.Now.ToString());     // 0s

                // re-layout combined meshes
                GameObject mcGo = new GameObject(string.Format("{0}-MC{1}", buildingName, i), typeof(LODGroup));
                EditorSceneManager.MoveGameObjectToScene(mcGo, meshCombineBoundsTool.gameObject.scene);
                //mcGo.transform.position = meshCombineBoundsTool.transform.position;
                Bounds bound = new Bounds(meshCombineBoundsTool.combines[i].bounds[0].center, meshCombineBoundsTool.combines[i].bounds[0].size);
                for (int j = 0; j < meshCombineBoundsTool.combines[i].bounds.Count; j++)
                {
                    bound.Encapsulate(new Bounds(meshCombineBoundsTool.combines[i].bounds[j].center, meshCombineBoundsTool.combines[i].bounds[j].size));
                }
                mcGo.transform.position = meshCombineBoundsTool.transform.TransformPoint(bound.center);
                for (int j = 0; j < meshBakers.Length; j++)
                {
                    MB3_MeshBakerCommon meshBaker = meshBakers[j];
                    GameObject resultGo = meshBaker.meshCombiner.resultSceneObject;
                    if (resultGo == null)
                    {
                        Debug.LogErrorFormat("can't find resultGo, buildName:{0} meshbakers:{1} time:{2}", buildingName, i.ToString(), System.DateTime.Now.ToString());
                        continue;
                    }
                    Transform child = meshBaker.meshCombiner.resultSceneObject.transform.GetChild(0);
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
                    };
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
                    GameObject colliderGo = Instantiate<GameObject>(collider.gameObject);
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

                Debug.LogFormat("finish re-layout&set lod&combine combined colliders:{0} time:{1}", i.ToString(), System.DateTime.Now.ToString());          // 0s

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

                Debug.LogFormat("finish save all meshes&set multi tag&destroy bakerGo:{0} time:{1}", i.ToString(), System.DateTime.Now.ToString());         // 1s
            }

            Debug.LogFormat("++++++++++finish combine building:{0} time:{1}", buildingName, System.DateTime.Now.ToString());            // 232s

            // replace origin props
            if (autoReplace && mcs.Count > 0)
            {
                // re-parent combined mesh gameObject
                for (int i = 0; i < mcs.Count; i++)
                {
                    mcs[i].transform.SetParent(meshCombineBoundsTool.transform);
                }

                // delete origin props
                for (int i = 0; i < meshCombineBoundsTool.combines.Count; i++)
                {
                    for (int j = 0; j < meshCombineBoundsTool.combines[i].bounds.Count; j++)
                    {
                        int count = meshCombineBoundsTool.combines[i].bounds[j].GetGoesCount();
                        for (int k = count - 1; k >= 0; k--)
                        {
                            GameObject go = meshCombineBoundsTool.combines[i].bounds[j].GetGameObject(k);
                            if (go != null) DestroyImmediate(go);
                        }
                    }
                }

                // remove MeshCombineBoundsTool component
                DestroyImmediate(meshCombineBoundsTool);
                meshCombineBoundsTool = null;
            }

            Debug.LogFormat("++++++++++finish replace building:{0} time:{1}", buildingName, System.DateTime.Now.ToString());            // 0s
        }
    }
}