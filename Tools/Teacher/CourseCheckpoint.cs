using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// This file stays outside Assets. Pipeline compiles it only when explicitly invoked.
public static class CourseCheckpoint
{
    private const string ScenePath = "Assets/Scenes/PlatformMain.unity";

    private static GameObject ObjectNamed(string name)
    {
        return SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .FirstOrDefault(gameObject => gameObject.name == name);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static object Prepare(int day)
    {
        Require(!EditorApplication.isPlaying, "Stop Play before preparing a checkpoint.");
        EditorSceneManager.OpenScene(ScenePath);
        foreach (string name in new[] { "MobileControls", "BestText", "SaveButton", "QuitButton", "ReloadStageButton", "LoadSettingsButton" })
        {
            int introduced = name == "MobileControls" ? 3 : name == "BestText" || name == "SaveButton" ? 4
                : name == "QuitButton" ? 5 : name == "ReloadStageButton" ? 6 : 13;
            GameObject gameObject = ObjectNamed(name);
            if (day < introduced && gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        // Match the required lesson scene; grid lines and post processing are optional.
        foreach (Transform transform in SceneManager.GetActiveScene().GetRootGameObjects().Select(root => root.transform).ToArray())
        {
            if (transform.name.StartsWith("Grid") || transform.name == "Global Volume")
            {
                UnityEngine.Object.DestroyImmediate(transform.gameObject);
            }
        }

        Camera camera = ObjectNamed("Main Camera").GetComponent<Camera>();
        camera.transform.position = new Vector3(0f, 23f, -15f);
        camera.transform.rotation = Quaternion.Euler(57f, 0f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = 13f;

        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.Refresh();
        foreach (string name in new[] { "Ground", "Player", "Coin" })
        {
            string oldPath = "Assets/Art/" + name + ".mat";
            string newPath = "Assets/Materials/" + name + "Material.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(oldPath) != null && AssetDatabase.LoadAssetAtPath<Material>(newPath) == null)
            {
                string error = AssetDatabase.MoveAsset(oldPath, newPath);
                Require(string.IsNullOrEmpty(error), error);
            }
        }

        if (day >= 2)
        {
            GameObject session = ObjectNamed("GameSession");
            AudioSource audio = session.GetComponent<AudioSource>();
            audio.clip = null;
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            audio.volume = 0.4f;
            ObjectNamed("MenuPanel").SetActive(true);
            ObjectNamed("HudPanel").SetActive(false);
            ObjectNamed("ResultPanel").SetActive(false);
            ObjectNamed("StartButton").GetComponentInChildren<TMP_Text>(true).text = "START";
            ObjectNamed("RestartButton").GetComponentInChildren<TMP_Text>(true).text = "RESTART";
            ObjectNamed("MenuButton").GetComponentInChildren<TMP_Text>(true).text = "MENU";
            if (day >= 5)
            {
                ObjectNamed("QuitButton").GetComponentInChildren<TMP_Text>(true).text = "QUIT";
            }
        }

        if (AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Collect.wav") != null)
        {
            AssetDatabase.DeleteAsset("Assets/Art/Collect.wav");
        }

        if (day >= 6)
        {
            Type loaderType = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("StageLoader")).First(type => type != null);
            Component oldLoader = ObjectNamed("GameSession").GetComponent(loaderType);
            if (oldLoader != null)
            {
                GameObject loaderObject = new GameObject("StageLoader");
                Component loader = loaderObject.AddComponent(loaderType);
                EditorUtility.CopySerialized(oldLoader, loader);
                UnityEngine.UI.Button reload = ObjectNamed("ReloadStageButton").GetComponent<UnityEngine.UI.Button>();
                for (int index = reload.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
                {
                    UnityEventTools.RemovePersistentListener(reload.onClick, index);
                }

                UnityEventTools.AddPersistentListener(reload.onClick, (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), loader, "LoadStage"));
                UnityEngine.Object.DestroyImmediate(oldLoader);
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Require(settings != null, "Addressables settings missing.");
            string remote = settings.profileSettings.GetProfileId("RemotePractice");
            if (day < 9 && !string.IsNullOrEmpty(remote))
            {
                settings.profileSettings.RemoveProfile(remote);
            }
            else if (day >= 9)
            {
                if (string.IsNullOrEmpty(remote))
                {
                    remote = settings.profileSettings.AddProfile("RemotePractice", settings.profileSettings.GetProfileId("Default"));
                }

                settings.profileSettings.SetValue(remote, AddressableAssetSettings.kRemoteBuildPath, "ServerData/[BuildTarget]");
                settings.profileSettings.SetValue(remote, AddressableAssetSettings.kRemoteLoadPath, "https://localhost:5443/[BuildTarget]");
            }

            settings.activeProfileId = settings.profileSettings.GetProfileId("Default");
            BundledAssetGroupSchema schema = settings.FindGroup("CourseStage").GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            settings.BuildRemoteCatalog = false;
            settings.ActivePlayModeDataBuilderIndex = 0;
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            EditorUtility.SetDirty(schema);
            EditorUtility.SetDirty(settings);
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        return Inspect(day);
    }

    public static object Inspect(int day)
    {
        Transform[] transforms = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        int missing = transforms.Sum(transform => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject));
        Require(missing == 0, "Missing MonoBehaviour scripts: " + missing);
        foreach (string name in new[] { "MobileControls", "BestText", "SaveButton", "QuitButton", "ReloadStageButton", "LoadSettingsButton" })
        {
            int introduced = name == "MobileControls" ? 3 : name == "BestText" || name == "SaveButton" ? 4
                : name == "QuitButton" ? 5 : name == "ReloadStageButton" ? 6 : 13;
            Require((ObjectNamed(name) != null) == (day >= introduced), name + " has the wrong introduction day.");
        }

        string[] scripts = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
        if (day == 1)
        {
            Require(scripts.Length == 0, "DAY 01 must contain no project C# scripts.");
            Require(ObjectNamed("GameSession") == null && ObjectNamed("Canvas") == null, "DAY 01 has premature gameplay objects.");
        }
        else
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("GameSession")).First(value => value != null);
            Component session = ObjectNamed("GameSession").GetComponent(type);
            SerializedObject serialized = new SerializedObject(session);
            foreach (string field in new[] { "player", "collectAudio", "menuPanel", "hudPanel", "resultPanel", "hud", "resultText", "statusText", "startButton" })
            {
                Require(serialized.FindProperty(field).objectReferenceValue != null, "Unwired GameSession." + field);
            }

            if (day >= 4)
            {
                Require(serialized.FindProperty("bestText").objectReferenceValue != null, "Unwired bestText.");
            }

            string[] names = { "StartButton", "RestartButton", "MenuButton", "SaveButton", "QuitButton", "ReloadStageButton", "LoadSettingsButton" };
            foreach (string name in names)
            {
                GameObject gameObject = ObjectNamed(name);
                if (gameObject == null)
                {
                    continue;
                }

                UnityEngine.UI.Button button = gameObject.GetComponent<UnityEngine.UI.Button>();
                Require(button.onClick.GetPersistentEventCount() == 1, "Unexpected button event count: " + name);
                Require(button.onClick.GetPersistentTarget(0) != null, "Missing event target: " + name);
            }

            GameObject stage = day >= 6 ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StageMain.prefab") : ObjectNamed("StageMain");
            Require(stage != null, "Stage missing.");
            Require(stage.GetComponentsInChildren<Transform>(true).Count(value => value.name.StartsWith("Coin")) == 10, "Expected ten coins.");
        }

        var result = new
        {
            day,
            unity = Application.unityVersion,
            scene = SceneManager.GetActiveScene().path,
            missingScripts = missing,
            projectScripts = scripts.Length,
            objects = transforms.Length,
            passed = true,
            checkedUtc = DateTime.UtcNow.ToString("O")
        };
        Directory.CreateDirectory("Evidence/Rebuild");
        File.WriteAllText($"Evidence/Rebuild/Day{day:00}-scene.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
