using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace MRWorkshop.Editor
{
    public static class WorkshopBootstrap
    {
        public static void Open()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(WorkshopSceneSetup.ScenePath)==null)WorkshopSceneSetup.Generate();
            else EditorSceneManager.OpenScene(WorkshopSceneSetup.ScenePath);
            WorkshopUnityMcp.Start();
        }
        [MenuItem("Workshop/Open Mixed Reality Scene")]
        public static void OpenScene()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before opening MR scene.");
            if(!File.Exists(WorkshopSceneSetup.ScenePath))WorkshopSceneSetup.Generate();
            else EditorSceneManager.OpenScene(WorkshopSceneSetup.ScenePath);
        }
    }
}
