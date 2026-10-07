"""Generate the reusable Focus research booklet. Original diagrams, explicit evidence levels."""
from pathlib import Path
import math, json
from reportlab.pdfgen import canvas
from reportlab.lib.colors import HexColor
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import Paragraph
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from xml.sax.saxutils import escape

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/reference'
OUT.mkdir(parents=True,exist_ok=True)
PALETTE=[('Field violet','#9D63FF'),('Telemetry cyan','#1DD9E0'),('Tagged magenta','#EB61CF'),('Resource amber','#F2B34E'),('Primary text','#E8EDF5'),('Panel blue','#121B32'),('Unavailable red','#DD526D'),('Ready green','#75D8A4')]
SOURCES=[
('S1','Territory Studio - Zero Dawn UI concepts','https://territorystudio.com/project/horizon-zero-dawn/'),
('S2','The Ghost Monkey - Forbidden West Focus UI','https://www.theghostmonkey.com/hfw-focus'),
('S3','PlayStation - official gameplay reveal, 2021','https://www.youtube.com/watch?v=wQATS4HOxdo'),
('S4','PlayStation - Forbidden West gameplay guidance','https://www.playstation.com/en-us/games/horizon-forbidden-west/'),
('S5','Meta - passthrough camera API overview','https://developers.meta.com/vr/documentation/spatial-sdk/spatial-sdk-pca-overview/'),
('S6','Meta - Interaction SDK UI','https://developers.meta.com/vr/documentation/unity/unity-isdk-create-ui/'),
('S7','Coplay - Unity MCP source and setup','https://github.com/CoplayDev/unity-mcp'),
('S8','OpenAI - Codex MCP configuration','https://learn.chatgpt.com/docs/extend/mcp?surface=cli'),
('S9','Secondary index - Focus game/lore catalogue','https://horizon.fandom.com/wiki/Focus')]

CHAPTERS=[
('Aloy\'s Focus','Visual and engineering reference / Quest 3S / first iteration',[
('Purpose','A reusable design reference for reconstructing the visible Focus experience and extending it incrementally. The fidelity target is the game reference; the deliverable is a real mixed reality application with explicit limits.'),
('Read the evidence labels','Observed = inspected primary visual or developer reference. Proposed = our design or engineering choice. Prepared = authored source or assets. Verified = a completed check with retained evidence. Headset verified = measured or observed on Shane\'s Quest 3S.'),
('Current milestone','The passthrough baseline exists at commit 452d6e4. This increment adds research, original assets and runtime preparation. A newly authored feature is not proved by the older baseline APK.'),
('Research date','October 7, 2026. Versioned first-study reference. Colour values and dimensions below are estimates for implementation, not extracted Guerrilla production specifications.')],'hero'),
('Choose a reference family','Zero Dawn and Forbidden West have related, distinct visual systems',[
('Zero Dawn','Use its violet scan-field identity as the reference for the surrounding effect. Territory\'s portfolio is explicitly concept development: translucent triangulation and hexagonal component motifs are useful process references, not proof that every concept shipped unchanged. [S1]'),
('Forbidden West','The designer\'s portfolio establishes a circular reticle, radial component sections, information panels and changing component states. Our original reticle study follows that structural vocabulary. [S2]'),
('Avoid accidental mixtures','Keep separate style profiles for each game in later iterations. The current study explores shared language and is not an exact recreation of either complete shipped interface. Record the selected game, scene and state for each fidelity comparison.'),
('Observed footage','Official PlayStation reveal: sampled frames at 06:10-06:24 show the wearable on Aloy; 15:25 shows a closer character shot; 16:25 visibly shows yellow/orange climbing marks. The transcript locates the Focus explanation at 16:22-16:26. Reviewed selected segments, not the entire nineteen-minute video. [S3]')],'reference'),
('The wearable silhouette','A small temple device, separate from the XR interface',[
('Observed form','The Focus is a compact triangular device worn beside Aloy\'s right temple. Its cool luminous detail reads differently from the warmer environmental and combat markers. The gameplay close-ups establish placement and overall silhouette, not reliable millimetre measurements. [S3]'),
('Original device study','Our mesh uses a pointed triangular prism, a dark metallic outer shell, a luminous blue inset, a smaller pale cap and four etched channels. It is intentionally simple, original geometry to test the authoring and export pipeline.'),
('Proposed proportions','Study height: 50 mm; thickness: 8 mm. These are model dimensions chosen for consistent metre-based assets, not canonical device dimensions. The enlarged presentation instance is six times larger and must never define the exported scale.'),
('Role in the headset','A rendered device icon can indicate activation; a physical prop is optional. A Quest app cannot replace the headset with a tiny neural-link device. The useful first output is the visual overlay the wearer sees.')],'device'),
('Colour as information','Estimated sRGB design tokens; tune on the device',[
('Violet and blue','Reserve violet for the scan-field identity and quieter foundation geometry. Blue supplies reticle foundations and panel depth. A full-screen saturated wash would make passthrough difficult to read.'),
('Cyan and magenta','Use cyan for telemetry and available geometry, and magenta for a deliberately tagged selection. These colours need shape or text cues as well; colour alone cannot identify state.'),
('Amber and red','Use amber for discovered resources or named components in practice content. Reserve red for unavailable/destroyed states and actual status errors. A bright dot should not imply a real-world threat assessment.'),
('Text and contrast','Primary text is near-white over a dark blue backing. Smaller labels should remain opaque enough to read over a bright room. Emission can bleach the palette; compare rendered RGB appearance, not only shader inputs.')],'palette'),
('Shape grammar','Reusable geometry rather than a wall of decoration',[
('Central reticle','Concentric rings, a tiny centre square, spaced telemetry ticks and lower radial component wedges. The designer\'s progression shows partial segments appearing during scanning, with differentiated tagged and destroyed states. [S2]'),
('Information cards','Title and symbol first, then a variable body sized to the subject. A simple object needs less detail than a machine. Keep a consistent diamond badge and coherent title/body hierarchy. [S2]'),
('Spatial decoration','Triangulated lattices, fine edges and restrained particles can suggest a field. Concept hexagons are an alternate Zero Dawn motif; do not replace the Forbidden West radial system indiscriminately. [S1]'),
('Original icon set','Build readable diamonds, arrows, rings and brackets from geometry. Add distinct original component symbols only when they carry information. Do not fill the demo with unexplained pseudo-data.')],'grammar'),
('States and transitions','Runtime availability and target information are separate',[
('Runtime states','Unavailable: XR has not become ready. Ready: scan can start. Scanning: one finite pulse is active. Suspended: the app is paused, unfocused or the head is not tracked. Returning to Ready never replays a cancelled scan.'),
('Input admission','Each pointer source needs valid tracking and observed physical neutral. Cancellation, loss of hand tracking, pointer exit and SDK Unselect are not proof of a released pinch or trigger. A held input must not restart on recovery.'),
('Target states','For future practice targets, distinguish Unknown, Available, Selected, Tagged and Destroyed. Display authored status for virtual content. A component marker is not a claim that we inspected a real device.'),
('Failure visibility','Hand tracking loss should explain why hand input is temporarily unavailable while leaving controller fallback possible. Head tracking loss cancels visuals and sound. Restore a usable Ready state after valid tracking returns.')],'states'),
('Motion and sound','Proposed implementation timings, not copied game constants',[
('Accepted Scan','One activation starts a 1.25-second effect. Capture the world-space origin once from the head position. An expanding lattice stays attached to that origin while the wearer moves; it does not drag along with the camera.'),
('Pulse schedule','0.00 s: acknowledgement and small field. 0.15-0.95 s: expansion. 0.95-1.25 s: fade and cleanup. A faint envelope limits brightness; no persistent strobe or forced camera movement is part of the design.'),
('Audio design','Generate an original short electronic chirp with a rising pitch and soft attack/release. Play it once for an accepted activation, at a modest level. Duplicate requests do not add more audio. Suspension stops active sound.'),
('Reticle motion','Reveal partial rings and component sections as information becomes available. Avoid spinning the entire reading surface continuously. The Blender study contains a small three-keyframe motion to verify animation authoring; the exported study mesh is static.')],'motion'),
('Capability catalogue','A game capability is not evidence of a physical sensor',[
('How to use this matrix','The catalogue includes gameplay and narrative categories. Primary UI/game references substantiate scanning, component information, tagging and datapoint/notebook presentation. The secondary lore index is a discovery aid for broader fiction and needs scene-specific validation before a faithful implementation. [S2, S4, S9]'),
('Implementation rule','Reproduce the interaction or presentation independently from the source of information. An authored virtual machine can have parts and metadata; a real machine needs identification, a trusted database and registration. Those are separate development tasks.')],'catalogue'),
('What Quest 3S can supply','Real XR input and compositing; later perception is possible',[
('Passthrough','The headset compositor can show the real room behind our rendered objects. The baseline requests passthrough and hand tracking. It does not request camera permission and does not inspect raw camera images.'),
('Hands and controllers','Meta\'s Interaction SDK supplies tracked hand/controller data and ray/poke interaction machinery. We still need to wire controls, test the source identity and neutral rules, and physically verify performance. Installed SDKs alone do not provide working product behaviour. [S6]'),
('Camera access later','Meta currently supports a passthrough camera API on Quest 3 and Quest 3S. A later recognition feature is technically possible, with the required OS support and permissions; it needs a model, confidence handling, latency testing and data handling. Do not equate passthrough display with object recognition. [S5]'),
('Hard boundaries','No Quest software measures neural activity or makes a physical hologram float in the room without the headset. Through-wall highlights for authored virtual objects are rendering tricks, not a new real-world sensing ability.')],'architecture'),
('Mixed reality layout','Starting values to test with the wearer',[
('Control panel','Place the initial world-space panel roughly 0.8 m ahead and slightly below eye level. Use a large Scan hit area and a clear Ready/Scanning/Suspended label. A headset user must be able to understand it without inspecting Unity.'),
('Placement and reach','Direct poke is useful only if the panel is within comfortable reach; ray selection remains primary at the default distance. Offer repositioning in a later step. Metre scale, text size and perceived depth must be checked on the actual headset.'),
('Legibility','Keep the centre of vision relatively open. A compact card and thin field mesh are preferable to opaque full-screen geometry. Inspect a bright room, dark room and patterned background. The headset is the final colour/contrast reference.'),
('Practice content','If targets are included, label them VIRTUAL PRACTICE. No object names, health values or weak spots should appear on real room objects unless their information source is explicit and accurate.')],'layout'),
('Authoring and toolchain','Keep the build independent of live agent integrations',[
('Unity path','C# describes behaviour; scenes/prefabs store the configured rig and controls; shaders/materials draw effects. Unity exports the Android project and IL2CPP native code; the bounded Gradle wrapper packages the APK. This is a standalone Quest application.'),
('Blender path','MCP commands run real Blender Python. The original study has material nodes, polygon meshes and keyframes, a saved blend source and FBX exports. Keep blend source outside Assets so Unity does not depend on launching the Store Blender executable to import it.'),
('Unity MCP','Coplay\'s package adds Editor tools; a local Python server exposes them to an MCP client. Pin Editor package and server to the same version. Verify a harmless scene/console round trip inside this project, never configure all unrelated clients. [S7, S8]'),
('Model capability assessment','The agent can author code, procedural shapes, shaders, sound synthesis and Blender scripts, then inspect real outputs. This does not establish specialist sculpting quality or perceptual fidelity. Judge the produced assets and tests, not a claim about model capability.')],'architecture'),
('Acceptance and delivery','Evidence needed before a demo is called ready',[
('Offline checks','Run meaningful domain tests, actual Unity EditMode and PlayMode tests, inspect a URP preview, make a fresh Core export/APK, verify its signature and manifest, and record the hash with the source commit. The older baseline remains a separate diagnostic build.'),
('Physical checks','Install and launch on Shane\'s Quest 3S. Verify real passthrough; left/right hand ray pinch; poke where reachable; controller fallback; held-input recovery; pause/head tracking loss; comfortable layout; and measured frame timing. A 72 Hz target is not a measured frame rate.'),
('Critic rubric','Experience 3 points; visual/audio quality 2; engineering/reproducibility 2; research/booklet 2; GitHub handoff 1. Without headset evidence, the ceiling is 7/10. A requested score of 9-9.5 does not override the evidence.'),
('Handoff','Commit source, original assets, this booklet and its generator, pinned packages and continuation instructions. Attach an APK to GitHub only with its matching commit and verified hash. Report blockers and unverified items explicitly.')],'states'),
('Reference register','Primary references first; links are clickable',[
('Research method','Inspected primary portfolio images for reticles, panels and concept lattice motifs. Inspected selected official gameplay frames and the developer transcript; no claim that every video or fictional ability has been exhaustively examined.'),
('Fidelity work still needed','Choose one shipped game/version and repeat comparable scan/selection/datapoint scenes. Compare silhouette, placement, thickness, colour, timing and state transitions side by side. Perform the same checks in stereo on the headset. Expand the catalogue only with new evidence.'),
('Refresh policy','Check vendor documentation when changing SDKs or adding camera/scene APIs. Keep chosen package versions pinned until there is a concrete reason to update. Record a research revision when sources or implementation claims change.')],'sources')]

CAPABILITIES=[
('Scan field / pulse','Rendered XR geometry','First demo'),
('Reticle / information cards','UI and authored metadata','First demo / extend'),
('Machine identity / components','Virtual object database','Practice content next'),
('Real object identification','Camera + perception model','Later research'),
('Weakness / resistance data','Trusted authored database','Virtual only'),
('Tag targets or components','Selection + persistent markers','Later increment'),
('Tracks / patrol paths','Authored paths; real sensing separate','Later increment'),
('Climbing / resource highlights','Known geometry / authored data','Virtual only initially'),
('Datapoints / notebook','Text/audio records + saved state','Later increment'),
('Holographic recordings','Animated rendered 3D content','Later assets'),
('Communications / shared Focus','Networking + spatial alignment','Later system'),
('World map / navigation','Spatial model + localization','Later system'),
('Remote machine control','Authorized real device protocol','Separate robotics work'),
('Through-wall / hidden information','Virtual rendering only','No physical sensing claim'),
('Neural link / full-dive','Requires absent hardware/science','Unavailable'),
('Unaided physical hologram','Requires display hardware','Unavailable')]

pdfmetrics.registerFont(TTFont('FocusText',r'C:\Windows\Fonts\segoeui.ttf'))
pdfmetrics.registerFont(TTFont('FocusBold',r'C:\Windows\Fonts\seguisb.ttf'))
W,H=595.28,841.89
c=canvas.Canvas(str(OUT/'Aloy Focus Reference.pdf'),pagesize=(W,H))
c.setTitle('Aloy Focus - Visual and Engineering Reference')
c.setAuthor('Shane / FocusCore development')
style=ParagraphStyle('body',fontName='FocusText',fontSize=10.5,leading=15.5,textColor=HexColor('#CCD5E5'))
def paragraph(text,x,y,width=499):
    p=Paragraph(escape(text),style); _,height=p.wrap(width,H)
    assert y-height>72, ('Page overflow',text[:45],y,height)
    p.drawOn(c,x,y-height); return y-height-13
def ring(cx,cy,r=65):
    c.setLineWidth(1.3); c.setStrokeColor(HexColor('#9D63FF')); c.circle(cx,cy,r)
    for i in range(48):
        a=i*math.tau/48
        c.setStrokeColor(HexColor('#1DD9E0' if i%3 else '#9D63FF'))
        c.line(cx+math.cos(a)*(r+5),cy+math.sin(a)*(r+5),cx+math.cos(a)*(r+8),cy+math.sin(a)*(r+8))
    c.setFillColor(HexColor('#E8EDF5')); c.rect(cx-1.5,cy-1.5,3,3,stroke=0,fill=1)
def triangle(cx,cy,size=65):
    for s,col in ((1,'#64748B'),(.83,'#3BA9EB'),(.64,'#D9E7F0')):
        path=c.beginPath(); path.moveTo(cx-size*s,cy); path.lineTo(cx+size*.60*s,cy+size*.80*s); path.lineTo(cx+size*.70*s,cy-size*.65*s); path.close()
        c.setFillColor(HexColor(col)); c.drawPath(path,fill=1,stroke=0)
def illustration(kind):
    bottom=555
    c.setFillColor(HexColor('#111D32')); c.roundRect(48,bottom,499,153,12,fill=1,stroke=0)
    if kind=='palette':
        for i,(name,hexcode) in enumerate(PALETTE):
            x=65+(i%4)*122;y=650-(i//4)*70
            c.setFillColor(HexColor(hexcode));c.roundRect(x,y,103,22,3,fill=1,stroke=0)
            c.setFillColor(HexColor('#DDE5F2'));c.setFont('FocusText',8);c.drawString(x,y-12,name);c.drawString(x,y-24,hexcode)
    elif kind in ('hero','device','reference'):
        triangle(148,630,53);ring(402,640,50)
        c.setFillColor(HexColor('#BCD0E7'));c.setFont('FocusText',9)
        c.drawString(87,566,'ORIGINAL DEVICE STUDY');c.drawString(344,566,'RADIAL UI STUDY')
    elif kind=='states':
        labels=['UNAVAILABLE','READY','SCANNING','SUSPENDED']
        for i,label in enumerate(labels):
            x=65+i*122
            c.setFillColor(HexColor(['#DD526D','#75D8A4','#9D63FF','#F2B34E'][i]));c.circle(x+47,642,12,fill=1,stroke=0)
            c.setFont('FocusBold',8);c.setFillColor(HexColor('#DDE5F2'));c.drawCentredString(x+47,601,label)
    elif kind=='motion':
        for i in range(4):
            ring(108+i*123,640,15+i*10)
            c.setFont('FocusText',9);c.setFillColor(HexColor('#BCD0E7'));c.drawCentredString(108+i*123,574,['0.00 s','0.15 s','0.95 s','1.25 s'][i])
    elif kind=='layout':
        c.setStrokeColor(HexColor('#BCD0E7'));c.circle(110,632,18);c.line(130,632,265,632)
        c.setFillColor(HexColor('#182948'));c.roundRect(274,590,224,86,7,fill=1,stroke=0)
        c.setFillColor(HexColor('#9D63FF'));c.roundRect(294,605,100,36,5,fill=1,stroke=0)
        c.setFillColor(HexColor('#E8EDF5'));c.setFont('FocusBold',12);c.drawString(325,617,'SCAN')
        c.setFont('FocusText',9);c.drawString(164,651,'~0.8 m');c.drawString(410,620,'READY')
    elif kind=='architecture':
        for i,label in enumerate(['INPUT','DOMAIN','VISUALS / AUDIO']):
            x=65+i*163;c.setStrokeColor(HexColor('#1DD9E0'));c.roundRect(x,615,143,44,7,fill=0,stroke=1)
            c.setFillColor(HexColor('#DDE5F2'));c.setFont('FocusBold',9);c.drawCentredString(x+71,633,label)
        c.setFont('FocusText',9);c.drawString(67,574,'Unity / Meta SDK                    tested state                      original rendering')
    else:
        ring(145,632,47)
        c.setStrokeColor(HexColor('#1DD9E0'));c.line(205,632,250,632)
        for i in range(3):
            c.setFillColor(HexColor('#334663'));c.roundRect(265,663-i*30,241,20,3,fill=1,stroke=0)
    return 525

CHAPTERS.insert(-1, ('Observed reticle states','Primary visual plate / analysis reference, not runtime content',[
('Attribution','Focus interface designs from the Forbidden West UI designer portfolio. Copyright Guerrilla Games, 2022. Used here as a credited reference for analysis. The reference image is not included in the game assets. [S2]'),
('Comparison method','Compare ring spacing, lower component wedges and state cues against the original study. The prototype is an approximation; matching these structures does not establish pixel-perfect fidelity.')],'reticle-plate'))
CHAPTERS.insert(-1, ('Original asset study','Actual Blender render / original geometry / October 7, 2026',[
('Produced assets','The triangular wearable study and radial reticle were built with procedural Blender geometry, saved as an editable blend source and exported as FBX. This image is a real render of those assets, not a proposed screenshot of the Quest application.'),
('Engineering boundary','The reticle is combined into one mesh with colour material groups. The source contains a small three-frame-keyed animation; the exported reticle is static. Unity import, stereo rendering and on-headset appearance still need verification.')],'asset-plate'))

for index,(title,subtitle,sections,kind) in enumerate(CHAPTERS):
    c.setFillColor(HexColor('#081120'));c.rect(0,0,W,H,fill=1,stroke=0)
    c.setFillColor(HexColor('#9D63FF'));c.rect(48,789,35,3,fill=1,stroke=0)
    c.setFont('FocusBold',9);c.drawString(94,786,'FOCUS / DESIGN + ENGINEERING / 01')
    c.setFillColor(HexColor('#E8EDF5'));c.setFont('FocusBold',26);c.drawString(48,745,title)
    c.setFont('FocusText',10);c.setFillColor(HexColor('#A8BAD2'));c.drawString(48,722,subtitle)
    if kind=='catalogue':
        y=692
        for j,(ability,method,stage) in enumerate(CAPABILITIES):
            c.setFillColor(HexColor('#142137' if j%2==0 else '#0D192B'));c.rect(48,y-21,499,25,fill=1,stroke=0)
            c.setFillColor(HexColor('#E8EDF5'));c.setFont('FocusBold',8.2);c.drawString(57,y-10,ability)
            c.setFont('FocusText',7.5);c.drawString(240,y-10,method);c.drawString(430,y-10,stage)
            y-=27
        y-=15
    elif kind in ('reticle-plate','asset-plate'):
        from reportlab.lib.utils import ImageReader
        image_path=OUT/'images'/('hfw-reticle.png' if kind=='reticle-plate' else 'focus-asset-study.png')
        if not image_path.is_file(): raise FileNotFoundError('Reference plate missing: '+str(image_path))
        image=ImageReader(str(image_path));iw,ih=image.getSize()
        h=min(405,499*ih/iw);w=h*iw/ih
        c.drawImage(image,48+(499-w)/2,690-h,width=w,height=h)
        y=690-h-30
    elif kind=='sources': y=685
    else: y=illustration(kind)
    for heading,text in sections:
        c.setFillColor(HexColor('#E8EDF5'));c.setFont('FocusBold',11);c.drawString(48,y,heading);y-=18
        y=paragraph(text,48,y)
    if kind=='sources':
        for sid,label,url in SOURCES:
            c.setFillColor(HexColor('#1DD9E0'));c.setFont('FocusBold',9.5);c.drawString(48,y,sid+' / '+label)
            c.linkURL(url,(48,y-3,547,y+11),relative=0,thickness=0)
            y-=28
        assert y>72
    c.setStrokeColor(HexColor('#334663'));c.line(48,54,547,54)
    c.setFillColor(HexColor('#A8BAD2'));c.setFont('FocusText',8)
    c.drawString(48,36,'SHANE / QUEST 3S / RESEARCH 2026-10-07 / VALUES MARKED PROPOSED')
    c.drawRightString(547,36,f'{index+1:02d} / {len(CHAPTERS):02d}')
    c.showPage()
c.save()
(OUT/'focus-reference.json').write_text(json.dumps({'date':'2026-10-07','palette':PALETTE,'capabilities':CAPABILITIES,'sources':SOURCES,'chapters':CHAPTERS},indent=2),encoding='utf-8')
print(OUT/'Aloy Focus Reference.pdf')
