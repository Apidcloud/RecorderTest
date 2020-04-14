using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildHelper
{
    public static void BuildWeb(){
        BuildHelper.Build(BuildTarget.WebGL);
    }

    public static void BuildWin()
    {
        BuildHelper.Build(BuildTarget.StandaloneWindows);
    }

    public static void BuildWin64()
    {
        BuildHelper.Build(BuildTarget.StandaloneWindows64);
    }

    public static void BuildMac()
    {
        BuildHelper.Build(BuildTarget.StandaloneOSX);
    }

    public static void BuildLinux()
    {
        BuildHelper.Build(BuildTarget.StandaloneLinux);
    }

    public static void BuildLinux64()
    {
        BuildHelper.Build(BuildTarget.StandaloneLinux64);
    }

    public static void BuildLinuxUniversal()
    {
        BuildHelper.Build(BuildTarget.StandaloneLinuxUniversal);
    }

    public static void Build(BuildTarget target)
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };

        buildPlayerOptions.locationPathName = target != BuildTarget.WebGL ? "Build/AutomaticRecording" : "WebGlBuild";
        buildPlayerOptions.target = target;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }
    }
}