"""One bounded protocol check. Server handshake is separate from Unity Editor connectivity."""
import argparse, asyncio, json, os, uuid
from datetime import datetime, timezone
from pathlib import Path
root=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--diagnostic',choices=['input','rules'])
parser.add_argument('--wait-seconds',type=int,default=150)
options=parser.parse_args()
if not 0 <= options.wait_seconds <= 180:
    parser.error('--wait-seconds must be between0 and180')

def atomic_write(path, text):
    """Publish a complete report while leaving its previous copy intact on failure."""
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary=path.with_name(path.name+'.part-'+uuid.uuid4().hex)
    try:
        with temporary.open('x',encoding='utf-8') as output:
            output.write(text)
            output.flush()
            os.fsync(output.fileno())
        os.replace(temporary,path)
    finally:
        temporary.unlink(missing_ok=True)


def save_report(report):
    name='unity-mcp-protocol.json' if not options.diagnostic else 'unity-mcp-diagnostic-'+options.diagnostic+'.json'
    artifacts=root/'.artifacts/workshop'
    snapshot=artifacts/(name.removesuffix('.json')+'-'+report['invocation_id']+'.json')
    report['invocation_report']=str(snapshot.relative_to(root))
    text=json.dumps(report,indent=2)
    atomic_write(snapshot,text)
    atomic_write(artifacts/name,text)
    print(text)


async def main():
    report={'invocation_id':datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S')+'-'+uuid.uuid4().hex[:8],
        'started_at':datetime.now(timezone.utc).isoformat(),'action':options.diagnostic or 'connection',
        'passed':False,'editor_round_trip_verified':False,'blocker':'No matching project Editor discovered'}
    try:
        # Import failures also receive fresh failure evidence.
        from mcp import ClientSession, StdioServerParameters
        from mcp.client.stdio import stdio_client
        python=Path(os.environ['FOCUS_PYTHON'])
        params=StdioServerParameters(command=str(Path.home()/'.local/bin/uvx.exe'),
            args=['--python',str(python),'--from','mcpforunityserver==10.0.0','mcp-for-unity','--transport','stdio'],
            env={**os.environ,'UNITY_MCP_STATUS_DIR':str(root/'.artifacts/workshop/mcp/status'),'UNITY_MCP_SKIP_STARTUP_CONNECT':'1'})
        async with asyncio.timeout(45+options.wait_seconds):
            async with stdio_client(params) as (read,write):
                async with ClientSession(read,write) as session:
                    init=await session.initialize()
                    tools=await session.list_tools(); resources=await session.list_resources()
                    report.update({'server':init.model_dump(by_alias=True).get('serverInfo',init.model_dump().get('server_info')),
                        'tool_count':len(tools.tools),'tools':[t.name for t in tools.tools],'resources':[str(r.uri) for r in resources.resources]})
                    # Read the real instance registry once; no repeated reconnects.
                    instance=next((r for r in resources.resources if 'instances' in str(r.uri)),None)
                    if instance:
                        result=await session.read_resource(instance.uri)
                        report['instance_registry']=[c.text for c in result.contents if hasattr(c,'text')]
                        registry=json.loads(report['instance_registry'][0])
                        expected=str(root/'MRWorkshop/Assets').replace('\\','/').lower()
                        matches=[i for i in registry.get('instances',[]) if i.get('path','').replace('\\','/').lower()==expected]
                        if len(matches)==1:
                            selection=await session.call_tool('set_active_instance',{'instance':matches[0]['id']})
                            report['selection']=selection.model_dump(by_alias=True).get('structuredContent')
                            if report['selection'] and report['selection'].get('success'):
                                scene=await session.call_tool('manage_scene',{'action':'get_active'})
                                report['active_scene']=scene.model_dump(by_alias=True).get('structuredContent')
                                report['editor_round_trip_verified']=bool(report['active_scene'] and report['active_scene'].get('success'))
                                report['blocker']=None if report['editor_round_trip_verified'] else 'Scene query failed'
                                if report['editor_round_trip_verified'] and options.diagnostic:
                                    prefix='input-proof-' if options.diagnostic=='input' else 'proof-'
                                    evidence_root=root/'.artifacts/workshop'
                                    before=set(evidence_root.glob(prefix+'*'))
                                    menu='Workshop/Run Finite Desktop Input Diagnostic (Injected Devices)' if options.diagnostic=='input' else 'Workshop/Run Finite Workshop Diagnostic (Simulated Input)'
                                    request=await session.call_tool('execute_menu_item',{'menu_path':menu})
                                    report['diagnostic_request']=request.model_dump(by_alias=True).get('structuredContent')
                                    if not (report['diagnostic_request'] or {}).get('success'):
                                        raise RuntimeError('Diagnostic menu request failed; no retry performed')
                                    # Observe this ONE job, including partially written files.
                                    deadline=asyncio.get_running_loop().time()+options.wait_seconds
                                    while True:
                                        created=set(evidence_root.glob(prefix+'*'))-before
                                        if len(created)>1:
                                            raise RuntimeError('Diagnostic produced ambiguous fresh evidence folders')
                                        evidence=next(iter(created))/'result.json' if created else None
                                        if evidence and evidence.is_file():
                                            try:
                                                diagnostic_result=json.loads(evidence.read_text())
                                            except (OSError,json.JSONDecodeError):
                                                pass
                                            else:
                                                break
                                        if asyncio.get_running_loop().time()>=deadline:
                                            raise RuntimeError('Requested diagnostic produced no result within its bounded startup/runtime wait')
                                        await asyncio.sleep(1)
                                    report['diagnostic_evidence']=str(evidence.relative_to(root))
                                    report['diagnostic_result']=diagnostic_result
                                    report['console']=(await session.call_tool('read_console',{'action':'get','types':['error'],'count':20,'include_stacktrace':True})).model_dump(by_alias=True).get('structuredContent')
                                    if not (report['console'] or {}).get('success'):
                                        raise RuntimeError('Post-diagnostic console query failed; inspect the diagnostic evidence separately')
                                    report['console_query_verified']=True
                                    if report['console'].get('data'):
                                        raise RuntimeError('Post-diagnostic console returned error entries; inspect the preserved console and diagnostic evidence')
                    if not report['editor_round_trip_verified']:
                        raise RuntimeError(report['blocker'] or 'Editor round trip was not verified; no automatic retry')
                    if options.diagnostic and not report.get('diagnostic_result',{}).get('passed'):
                        raise RuntimeError('Diagnostic did not pass; inspect its actual evidence')
        report['passed']=True
    except BaseException as error:
        report['error']=str(error) or type(error).__name__
        report['error_type']=type(error).__name__
        raise
    finally:
        report['completed_at']=datetime.now(timezone.utc).isoformat()
        save_report(report)
asyncio.run(main())
