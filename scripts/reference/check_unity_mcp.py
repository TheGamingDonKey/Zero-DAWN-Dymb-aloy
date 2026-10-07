"""One bounded protocol check. Server handshake is separate from Unity Editor connectivity."""
import asyncio, json, os
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
root=Path(__file__).resolve().parents[2]
python=Path(os.environ['FOCUS_PYTHON'])
params=StdioServerParameters(command=str(Path.home()/'.local/bin/uvx.exe'),
    args=['--python',str(python),'--from','mcpforunityserver==10.0.0','mcp-for-unity','--transport','stdio'],
    env={**os.environ,'UNITY_MCP_STATUS_DIR':str(root/'.artifacts/mcp/status'),'UNITY_MCP_SKIP_STARTUP_CONNECT':'1'})
async def main():
    async with asyncio.timeout(45):
        async with stdio_client(params) as (read,write):
            async with ClientSession(read,write) as session:
                init=await session.initialize()
                tools=await session.list_tools(); resources=await session.list_resources()
                report={'server':init.model_dump(by_alias=True).get('serverInfo',init.model_dump().get('server_info')),'tool_count':len(tools.tools),
                    'tools':[t.name for t in tools.tools],'resources':[str(r.uri) for r in resources.resources],
                    'editor_round_trip_verified':False,'blocker':'Unity Editor licence unavailable'}
                # Read the real instance registry once; no scene mutations or repeated reconnects.
                instance=next((r for r in resources.resources if 'instances' in str(r.uri)),None)
                if instance:
                    result=await session.read_resource(instance.uri)
                    report['instance_registry']=[c.text for c in result.contents if hasattr(c,'text')]
                out=root/'.artifacts/research/unity-mcp-protocol.json';out.write_text(json.dumps(report,indent=2))
                print(json.dumps(report,indent=2))
asyncio.run(main())
