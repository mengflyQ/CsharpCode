﻿// C# example.
using System;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor.Build;
using Random = System.Random;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using EasyRoads3Dv3;

public class VoyagerMenu
{

    public static string GetPlatformName()
    {
#if UNITY_EDITOR
        return GetPlatformForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
#else
        return GetPlatformForAssetBundles(Application.platform);
#endif
    }

#if UNITY_EDITOR
    private static string GetPlatformForAssetBundles(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.WebGL:
                return "WebGL";
            //			case BuildTarget.WebPlayer:
            //				return "WebPlayer";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "Windows";
            case BuildTarget.StandaloneOSXIntel:
            //			case BuildTarget.StandaloneOSX:
            //				return "OSX";
            // SavePredictionState more build targets for your own.
            // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
            default:
                return null;
        }
    }
#endif

    private static string GetPlatformForAssetBundles(RuntimePlatform platform)
    {
        switch (platform)
        {
            case RuntimePlatform.Android:
                return "Android";
            case RuntimePlatform.IPhonePlayer:
                return "iOS";
            case RuntimePlatform.WebGLPlayer:
                return "WebGL";
            case RuntimePlatform.WindowsPlayer:
                return "Windows";
            case RuntimePlatform.OSXPlayer:
                return "OSX";
            // SavePredictionState more build targets for your own.
            // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
            default:
                return null;
        }
    }

    private const string TargetDir = "../../../release/";

    [MenuItem("Voyager/Build OC")]
    public static void BuildOC()
    {
        BakeOc.LoadScenes(
            new List<string>
            {
                "dynamicscene",
                "dynamicscene",
                "dynamicscene",
                "dynamicscene",
                "dynamicscene",
                "dynamicscene",
                "dynamicscene",
                "dynamicscene",
                "dynamicscene",
                "dynamicscene",
                "dynamicscene",
                "dynamicscene",
                "dynamicscene"
           },
            new List<string>
            {
                "AdditiveScene",
                "002 2x1",
                "002 3x1",
                "002 2x2",
                "002 3x2",
                "002 3x3",
                "002 4x3",
                "002 5x4",
                "002 2x5",
                "002 4x5",
                "002 5x5",
                "002 4x6",
                "002 5x7"
            });
        BakeOc.Bake();
    }

    [MenuItem("Voyager/Build Streaming Level")]
    public static void BuildStreamingLevel()
    {
        BuildStreamingLevel(null);
    }

    public static void BuildStreamingLevel(List<string> scenePaths)
    {
        if (scenePaths == null)
            scenePaths = StreamingLevelBuilder.DefaultSceneList();

        StreamingLevelBuilder.Build(scenePaths);
    }


    public static void BeforeBuildPlayer()
    {
        CopySpecificSteamingAsset();
        AssetDatabase.Refresh();
    }

    static void CopySpecificSteamingAsset()
    {
        string audioRelativePath = @"Assets/Sound/WiseBank/";
        string o_path = Path.Combine(Application.dataPath, audioRelativePath);
        VoyagerMenuHelper.FixSlashes(ref o_path);
        VoyagerMenuHelper.DirectoryCopy(o_path, Application.streamingAssetsPath, true, ".bnk");
    }

    public static void FinalizeGameRoadNetwork()
    {
        string scenePath = @"Assets/Maps/maps/0001/Scenes/AdditiveScene.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        ERModularBase modularBase = UnityEngine.Object.FindObjectOfType<ERModularBase>();
        if (modularBase == null)
        {
            Debug.LogError("FinalizeGameRoadNetwork error, can't find ERModularBase Component");
            return;
        }

        OOCQDCQOOC.OCOODQOOQO(modularBase);
        UnityEngine.Object.DestroyImmediate(modularBase);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);
        Resources.UnloadUnusedAssets();
    }
    //====================================== 以下是打包用的函数 ======================================
    public static void BuildClient()
    {
        //build develop client
        BuildClient_dev();
        //build release client
        BuildClient_rls();
    }

    public static void BuildHallclient()
    {
        //build develop hallclient
        BuildHallclient_dev();
        //build release hallclient
        BuildHallclient_rls();

    }

    public static void BuildServer()
    {
        //build develop server
        BuildServer_dev();
        //build release server
        BuildServer_rls();
    }

    [MenuItem("Voyager/Build AssetBundle/packBuild")]
    public static void BuildAssetBundles()
    {
        Debug.LogFormat("Begin FinalizeGameRoadNetwork at: {0}", DateTime.Now);
        FinalizeGameRoadNetwork();
        Debug.LogFormat("Begin BuildOC at: {0}", DateTime.Now);
        BuildOC();
        BuildStreamingLevel();
        //we select compressed Assetbundles 
        Debug.LogFormat("Begin BuildAssetBundles at: {0}", DateTime.Now);
        BuildAssetBundles_compress();
        Debug.LogFormat("End BuildAssetBundles at: {0}", DateTime.Now);
    }

    //====================================== 以上是打包用的函数 ======================================

    [MenuItem("Voyager/Build Client/Develop")]
    public static void BuildClient_dev()
    {
        BeforeBuildPlayer();
        // Get filename.
        //string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        var destPath = Path.GetFullPath(TargetDir + "client/client_develop.exe");
        string[] levels = new string[] { "Assets/Assets/ClientScene.unity" };
        // Build develop client player.
        string msg = BuildPipeline.BuildPlayer(levels, destPath, BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging);
        EditorUtility.DisplayDialog("Info", "Build client_develop.exe at " + destPath + ": " + msg, "OK");
    }

    [MenuItem("Voyager/Build Client/Release")]
    public static void BuildClient_rls()
    {
        BeforeBuildPlayer();
        // Get filename.
        //string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        var destPath = Path.GetFullPath(TargetDir + "client/client.exe");
        string[] levels = new string[] { "Assets/Assets/ClientScene.unity" };
        // Build release client player.
        string msg = BuildPipeline.BuildPlayer(levels, destPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
        EditorUtility.DisplayDialog("Info", "Build client.exe at " + destPath + ": " + msg, "OK");
    }

    [MenuItem("Voyager/Build Robot/Release")]
    public static void BuildRobot_rls()
    {
        BeforeBuildPlayer();
        // Get filename.
        //string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        var destPath = Path.GetFullPath(TargetDir + "client/robot.exe");
        string[] levels = new string[] { "Assets/Assets/RobotScene.unity" };
        // Build release client player.
        string msg = BuildPipeline.BuildPlayer(levels, destPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
        EditorUtility.DisplayDialog("Info", "Build robot.exe at " + destPath + ": " + msg, "OK");
    }

    [MenuItem("Voyager/Build Hallclient/Develop")]
    public static void BuildHallclient_dev()
    {
        BeforeBuildPlayer();
        // Get filename.
        //string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        var destPath = Path.GetFullPath(TargetDir + "client/hallclient_develop.exe");
        string[] levels = new string[] { "Assets/Assets/Hall.unity", "Assets/Assets/ClientScene.unity", "Assets/Assets/RobotScene.unity" };
        // Build develop hallclient player.
        string msg = BuildPipeline.BuildPlayer(levels, destPath, BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging);
        EditorUtility.DisplayDialog("Info", "Build hallclient_develop.exe at " + destPath + ": " + msg, "OK");
    }

    [MenuItem("Voyager/Build Hallclient/Release")]
    public static void BuildHallclient_rls()
    {
        BeforeBuildPlayer();
        // Get filename.
        //string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        var destPath = Path.GetFullPath(TargetDir + "client/hallclient.exe");
        string[] levels = new string[] { "Assets/Assets/Hall.unity", "Assets/Assets/ClientScene.unity", "Assets/Assets/RobotScene.unity" };
        // Build release hallclient player.
        string msg = BuildPipeline.BuildPlayer(levels, destPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
        EditorUtility.DisplayDialog("Info", "Build hallclient.exe at " + destPath + ": " + msg, "OK");
    }

    [MenuItem("Voyager/Build Server/Develop")]
    public static void BuildServer_dev()
    {
        // Get filename.
        //string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        var destPath = Path.GetFullPath(TargetDir + "server/voyager_server_develop.exe");
        string[] levels = new string[] { "Assets/Assets/ServerScene.unity" };
        // Build develop server player.
        string msg = BuildPipeline.BuildPlayer(levels, destPath, BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging);
        EditorUtility.DisplayDialog("Info", "Build voyager_server_develop.exe at " + destPath + ": " + msg, "OK");
    }

    [MenuItem("Voyager/Build Server/Release")]
    public static void BuildServer_rls()
    {
        // Get filename.
        //string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        var destPath = Path.GetFullPath(TargetDir + "server/voyager_server.exe");
        string[] levels = new string[] { "Assets/Assets/ServerScene.unity" };
        // Build release server player.
        string msg = BuildPipeline.BuildPlayer(levels, destPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
        EditorUtility.DisplayDialog("Info", "Build voyager_server.exe at " + destPath + ": " + msg, "OK");
    }

    [MenuItem("Voyager/Build AssetBundle/UncompressedAssetBundle")]
    public static void BuildAssetBundles_uncompress()
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();
        // Choose the output path according to the build target.
        string outputPath = Path.GetFullPath(Path.Combine(TargetDir + "AssetBundles", GetPlatformName()));
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
        var manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows64);
        if (manifest == null)
            throw new Exception("Build Asset Bundle fail, for manifest is null");
        EditorUtility.DisplayDialog("Info", "Build Assets at " + outputPath + ", result" + manifest, "OK");
    }

    [MenuItem("Voyager/Build AssetBundle/ChunkBasedCompression")]
    public static void BuildAssetBundles_compress()
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();
        // Choose the output path according to the build target.
        string outputPath = Path.GetFullPath(Path.Combine(TargetDir + "AssetBundles", GetPlatformName()));
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
        var manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        if (manifest == null)
            throw new Exception("Build Asset Bundle fail, for manifest is null");
        EditorUtility.DisplayDialog("Info", "Build Assets at " + outputPath + ", result" + manifest.GetAllAssetBundles().Length, "OK");
    }

    [MenuItem("Voyager/Build AssetBundle/AppendHashToAssetBundleName")]
    public static void BuildAssetBundles_hash()
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();
        // Choose the output path according to the build target.
        string outputPath = Path.GetFullPath(Path.Combine(TargetDir + "AssetBundles", GetPlatformName()));
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
        var manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.AppendHashToAssetBundleName, BuildTarget.StandaloneWindows64);
        if (manifest == null)
            throw new Exception("Build Asset Bundle fail, for manifest is null");
        EditorUtility.DisplayDialog("Info", "Build Assets at " + outputPath + ", result" + manifest, "OK");
    }

    [MenuItem("Voyager/Build AssetBundle/ForceRebuildAssetBundle")]
    public static void BuildAssetBundles_rebuild()
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();
        // Choose the output path according to the build target.
        string outputPath = Path.GetFullPath(Path.Combine(TargetDir + "AssetBundles", GetPlatformName()));
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
        var manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
        if (manifest == null)
            throw new Exception("Build Asset Bundle fail, for manifest is null");
        EditorUtility.DisplayDialog("Info", "Build Assets at " + outputPath + ", result" + manifest, "OK");
    }

    [MenuItem("Build/Clean Cache")]
    public static void CleanCache()
    {
#if UNITY_2017_1_OR_NEWER
        Caching.ClearCache();
#else
        Caching.CleanCache();
#endif
    }

    private static StringBuilder stringBuilder;
    public static void PreBuildCheck(string batchDir)
    {
        if (string.IsNullOrEmpty(batchDir))
        {
            Debug.LogError("PreBuildCheck error, batchPath is empty");
            return;
        }

        // 检测配置文件
        string configPath = Path.Combine(batchDir, "PreBuildCheckConfig.txt");
        if (!File.Exists(configPath))
        {
            Debug.LogError("PreBuildCheck error, config file can't found, path:" + configPath);
            return;
        }

        // 日志文件
        string logPath = Path.Combine(batchDir, "PreBuildCheckLog.txt");
        using (FileStream fs = File.Create(logPath))
        {
            stringBuilder = new StringBuilder();
            Application.logMessageReceived += LogCallback;

            // 执行config
            var lines = File.ReadAllLines(configPath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                EditorApplication.ExecuteMenuItem(line);
            }

            Byte[] info = new UTF8Encoding(true).GetBytes(stringBuilder.ToString());
            fs.Write(info, 0, info.Length);

            Application.logMessageReceived -= LogCallback;
            stringBuilder = null;
        }
    }

    private static void LogCallback(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
        {
            stringBuilder.Append(condition);
            stringBuilder.AppendLine();
            stringBuilder.Append(stackTrace);
            stringBuilder.AppendLine();
        }
    }

}