using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Reporting;

public static class CourseBuild
{
    public static object Main(int day, bool remote = false, bool player = true)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            throw new InvalidOperationException("This validation builds Windows. Select the Windows profile first.");
        }

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        settings.activeProfileId = settings.profileSettings.GetProfileId(remote ? "RemotePractice" : "Default");
        BundledAssetGroupSchema schema = settings.FindGroup("CourseStage").GetSchema<BundledAssetGroupSchema>();
        schema.BuildPath.SetVariableByName(settings, remote ? AddressableAssetSettings.kRemoteBuildPath : AddressableAssetSettings.kLocalBuildPath);
        schema.LoadPath.SetVariableByName(settings, remote ? AddressableAssetSettings.kRemoteLoadPath : AddressableAssetSettings.kLocalLoadPath);
        settings.BuildRemoteCatalog = false;
        settings.ActivePlayModeDataBuilderIndex = 0;
        EditorUtility.SetDirty(schema);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        ProjectConfigData.GenerateBuildLayout = true;
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult content);
        if (!string.IsNullOrEmpty(content.Error))
        {
            throw new InvalidOperationException(content.Error);
        }

        if (!player)
        {
            if (remote)
            {
                settings.ActivePlayModeDataBuilderIndex = settings.DataBuilders.FindIndex(builder => builder != null && builder.GetType().Name == "BuildScriptPackedPlayMode");
                if (settings.ActivePlayModeDataBuilderIndex < 0)
                {
                    throw new InvalidOperationException("Packed play mode builder missing.");
                }
            }

            AssetDatabase.SaveAssets();
            return new { day, content = "Succeeded", remote };
        }

        string output = $"Builds/Rebuilt/Day{day:00}/PlatformCoinLab.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/PlatformMain.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        var result = new
        {
            day,
            result = report.summary.result.ToString(),
            errors = report.summary.totalErrors,
            warnings = report.summary.totalWarnings,
            output,
            bytes = report.summary.totalSize,
            seconds = report.summary.totalTime.TotalSeconds,
            checkedUtc = DateTime.UtcNow.ToString("O")
        };
        Directory.CreateDirectory("Evidence/Rebuild");
        File.WriteAllText($"Evidence/Rebuild/Day{day:00}-windows-build.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("Windows build failed. Inspect the Editor log.");
        }

        return result;
    }
}
