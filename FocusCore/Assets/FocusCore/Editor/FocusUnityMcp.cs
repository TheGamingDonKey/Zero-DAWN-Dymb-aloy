using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FocusCore.Editor
{
    [InitializeOnLoad]
    public static class FocusUnityMcp
    {
        static FocusUnityMcp()
        {
            // Package startup must not rewrite other Codex/Claude/etc client configurations.
            SessionState.SetBool("MCPForUnity.StartupConfigRewrite.Ran",true);
        }

        [MenuItem("Focus/Start Project MCP Bridge")]
        public static void Start()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNITY_MCP_STATUS_DIR")))
                throw new InvalidOperationException("Use scripts/Start-UnityMcpEditor.ps1 for the isolated project status directory.");
            var port = Type.GetType("MCPForUnity.Editor.Helpers.PortManager, MCPForUnity.Editor",true);
            var bridge = Type.GetType("MCPForUnity.Editor.Services.Transport.Transports.StdioBridgeHost, MCPForUnity.Editor",true);
            port.GetMethod("SetPreferredPort",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{54484});
            bridge.GetMethod("Start",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
            Debug.Log("Focus project MCP bridge requested. Verify a scene/console round trip before claiming connectivity.");
        }
    }
}
