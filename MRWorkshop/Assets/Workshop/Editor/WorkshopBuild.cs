using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MRWorkshop.Editor
{
    public static class WorkshopBuild
    {
        public static void BuildBaseline()
        {
            BuildScene(WorkshopProjectSetup.BaselineScene);
        }

        private static void BuildScene(string scene)
        {
            var output = ReadOutput();
            var export = ReadExportPath();
            WorkshopProjectSetup.ConfigureAndValidate();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
            {
                if (scene == WorkshopProjectSetup.BaselineScene) WorkshopProjectSetup.GenerateBaseline();
                else WorkshopSceneSetup.Generate();
            }
            EditorSceneManager.OpenScene(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(export ?? output));
            var previousExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            BuildReport report;
            try
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = export != null;
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { scene },
                    locationPathName = export ?? output,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
            }
            finally { EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExport; }
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Focus Android build " + report.summary.result + "; errors=" + report.summary.totalErrors);
            if (export != null)
            {
                if (!File.Exists(Path.Combine(export, "launcher/build.gradle"))) throw new IOException("Export produced no launcher Gradle project.");
                Debug.Log("Focus Android Gradle export succeeded: " + export + "; native compilation and APK packaging remain pending.");
                return;
            }
            if (!File.Exists(output) || new FileInfo(output).Length == 0)
                throw new IOException("Successful build report produced no nonempty APK.");
            Debug.Log("Focus APK succeeded: " + output + "; bytes=" + new FileInfo(output).Length + "; headset validation pending.");
        }

        public static void BuildWorkshop()
        {
            BuildScene(WorkshopSceneSetup.ScenePath);
        }

        private static string ReadOutput()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-workshopOutput");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing -workshopOutput APK path.");
            var output = Path.GetFullPath(args[index + 1]);
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.artifacts/apks")) + Path.DirectorySeparatorChar;
            if (!output.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("APK output must be inside this checkout's .artifacts/apks directory.");
            if (File.Exists(output)) throw new IOException("Build output already exists; use a fresh invocation path.");
            return output;
        }

        private static string ReadExportPath()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-workshopExportPath");
            if (index < 0) return null;
            if (index + 1 >= args.Length) throw new ArgumentException("Missing export path.");
            var repo = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            string key;
            using (var sha = SHA256.Create())
                key = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(repo.ToLowerInvariant()))).Replace("-", "").Substring(0,12).ToLowerInvariant();
            var cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".fbc", key);
            var owner = Path.Combine(cache, "project.txt");
            if (!File.Exists(owner) || !string.Equals(File.ReadAllText(owner).Trim(), repo, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Export cache ownership check failed.");
            var export = Path.GetFullPath(args[index + 1]);
            var projects = Path.GetFullPath(Path.Combine(cache, "projects")) + Path.DirectorySeparatorChar;
            if (!export.StartsWith(projects, StringComparison.OrdinalIgnoreCase) || Directory.Exists(export))
                throw new ArgumentException("Export must use a fresh directory in this checkout's short build cache.");
            return export;
        }
    }
}
