using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MRWorkshop.Editor
{
    [InitializeOnLoad]
    public static class WorkshopUnityMcp
    {
        const string RequestedKey = "MRWorkshop.ProjectMcpRequested";
        static WorkshopUnityMcp()
        {
            // Package startup must not rewrite other Codex/Claude/etc client configurations.
            SessionState.SetBool("MCPForUnity.StartupConfigRewrite.Ran",true);
            // Hub launches do not inherit our shell's environment. Keep discovery scoped
            // to this Editor process so GUI and CLI clients use the same project registry.
            var status = Path.GetFullPath(Path.Combine(Application.dataPath,"../../.artifacts/workshop/mcp/status"));
            Directory.CreateDirectory(status);
            Environment.SetEnvironmentVariable("UNITY_MCP_STATUS_DIR",status,EnvironmentVariableTarget.Process);
            // Hub does not inherit Codex's bundled Python PATH. Make the existing
            // runtime visible to this Editor only, without changing Windows PATH.
            var pythonDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cache/codex-runtimes/codex-primary-runtime/dependencies/python");
            var editorPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (File.Exists(Path.Combine(pythonDirectory,"python.exe")) &&
                !Array.Exists(editorPath.Split(Path.PathSeparator),p=>string.Equals(p,pythonDirectory,StringComparison.OrdinalIgnoreCase)))
                Environment.SetEnvironmentVariable("PATH",pythonDirectory + Path.PathSeparator + editorPath,EnvironmentVariableTarget.Process);
            // Unity reloads the scripting domain when entering/exiting PlayMode.
            // Resume only a bridge explicitly requested for this Editor session.
            if (!Application.isBatchMode && SessionState.GetBool(RequestedKey,false))
                EditorApplication.delayCall += Start;
        }

        [MenuItem("Workshop/Start Project MCP Bridge")]
        public static void Start()
        {
            SessionState.SetBool(RequestedKey,true);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNITY_MCP_STATUS_DIR")))
                throw new InvalidOperationException("Use scripts/workshop/Open.ps1 for the isolated project status directory.");
            var port = Type.GetType("MCPForUnity.Editor.Helpers.PortManager, MCPForUnity.Editor",true);
            var bridge = Type.GetType("MCPForUnity.Editor.Services.Transport.Transports.StdioBridgeHost, MCPForUnity.Editor",true);
            port.GetMethod("SetPreferredPort",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{54486});
            bridge.GetMethod("Start",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
            Debug.Log("Workshop project MCP bridge requested on54486. Verify a scene/console round trip before claiming connectivity.");
        }
    }
}
