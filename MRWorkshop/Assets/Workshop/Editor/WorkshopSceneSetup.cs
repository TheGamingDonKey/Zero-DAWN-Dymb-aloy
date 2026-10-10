using System;
using System.IO;
using Oculus.Interaction;
using Oculus.Interaction.Editor.QuickActions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
namespace MRWorkshop.Editor
{
    public static class WorkshopSceneSetup
    {
        public const string ScenePath="Assets/Workshop/Scenes/Workshop.unity";
        public const string DesktopPath="Assets/Workshop/Scenes/DesktopWorkshop.unity";
        [MenuItem("Workshop/Generate Mixed Reality Workshop")]
        public static void Generate()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before regenerating the scene.");
            WorkshopProjectSetup.ConfigureAndValidate();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(WorkshopProjectSetup.PlatformPrefab);
            if(prefab==null) { WorkshopProjectSetup.GenerateBaseline(); prefab=AssetDatabase.LoadAssetAtPath<GameObject>(WorkshopProjectSetup.PlatformPrefab); }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("MRWorkshop");
            var platform=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform); platform.name="WorkshopPlatform";
            var rig=platform.GetComponentInChildren<OVRCameraRig>(true);
            if(rig==null)throw new InvalidOperationException("Platform camera rig missing.");
            var controller=root.AddComponent<WorkshopController>(); controller.head=rig.centerEyeAnchor;
            var bench=new GameObject("Workbench").transform; bench.SetParent(root.transform); bench.position=new Vector3(0,1,1.1f); controller.workbench=bench;
            MakeShape("Worksurface",PrimitiveType.Cube,bench,new Vector3(0,0,0),new Vector3(.94f,.035f,.66f),Material("Slate",new Color(.045f,.07f,.105f)));
            var cyan=Material("Cyan",new Color(.16f,.86f,.91f),true);
            var amber=Material("Amber",new Color(1,.63f,.18f),true);
            var pale=Material("Pale",new Color(.74f,.86f,.88f));
            var purple=Material("Violet",new Color(.58f,.39f,.95f));
            MakeShape("Front trim",PrimitiveType.Cube,bench,new Vector3(0,.02f,-.331f),new Vector3(.94f,.009f,.007f),cyan,false);
            var cell=MakePart(controller,bench,"POWER CELL",PartKind.PowerCell,PrimitiveType.Cylinder,new Vector3(-.29f,.125f,-.09f),new Vector3(.075f,.065f,.075f),amber);
            MakeShape("Cell cap",PrimitiveType.Cylinder,cell.transform,new Vector3(0,1.03f,0),new Vector3(.92f,.12f,.92f),pale,false);
            var cube=MakePart(controller,bench,"MODULE",PartKind.Cube,PrimitiveType.Cube,new Vector3(-.035f,.125f,-.09f),Vector3.one*.115f,cyan);
            var orb=MakePart(controller,bench,"ORB",PartKind.Orb,PrimitiveType.Sphere,new Vector3(.22f,.125f,-.09f),Vector3.one*.125f,purple);
            controller.parts=new[]{cell,cube,orb};
            var socket=new GameObject("Power Socket / cell only").transform; socket.SetParent(bench,false); socket.localPosition=new Vector3(0,.125f,.19f); controller.socket=socket;
            MakeShape("Socket mount",PrimitiveType.Cylinder,socket,new Vector3(0,-.083f,0),new Vector3(.19f,.025f,.19f),pale,false);
            Ring(socket,cyan,.082f,-.055f);
            Label(bench,"POWER SOCKET",new Vector3(0,.028f,.065f),new Vector2(260,42),.00055f,20);
            var mechanism=new GameObject("Powered rotor").transform; mechanism.SetParent(bench,false); mechanism.localPosition=new Vector3(.32f,.03f,.20f);
            MakeShape("Rotor pedestal",PrimitiveType.Cylinder,mechanism,Vector3.zero,new Vector3(.14f,.025f,.14f),pale,false);
            var rotor=new GameObject("Rotor").transform; rotor.SetParent(mechanism,false); rotor.localPosition=new Vector3(0,.085f,0); controller.rotor=rotor;
            MakeShape("Rotor hub",PrimitiveType.Sphere,rotor,Vector3.zero,Vector3.one*.05f,amber,false);
            for(int i=0;i<3;i++)
            {
                var bladePivot=new GameObject("Blade pivot").transform; bladePivot.SetParent(rotor,false); bladePivot.localRotation=Quaternion.Euler(0,i*120,0);
                MakeShape("Blade",PrimitiveType.Cube,bladePivot,new Vector3(.075f,0,0),new Vector3(.13f,.013f,.035f),cyan,false);
            }
            var panel=new GameObject("Workshop controls",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>(); panel.transform.SetParent(bench,false);
            panel.renderMode=RenderMode.WorldSpace; panel.worldCamera=rig.centerEyeAnchor.GetComponent<Camera>();
            var panelRect=(RectTransform)panel.transform; panelRect.sizeDelta=new Vector2(680,265); panelRect.localScale=Vector3.one*.00095f; panelRect.localPosition=new Vector3(0,.47f,.34f);
            panel.gameObject.AddComponent<Image>().color=new Color(.025f,.045f,.08f,.94f);
            Text(panelRect,"MR WORKSHOP / ASSEMBLY 01",new Vector2(-315,95),new Vector2(630,32),26);
            Text(panelRect,"Grab the cell. Release in the socket. Switch on the rotor.",new Vector2(-315,62),new Vector2(630,30),20);
            string[] actions={"Power","Reset","Rotate","Smaller","Larger"};
            for(int i=0;i<actions.Length;i++)Button(panelRect,controller,actions[i],new Vector2(-252+i*126,17));
            controller.status=Text(panelRect,"WAITING FOR HEADSET TRACKING",new Vector2(-315,-111),new Vector2(630,91),19);
            QuickActionsAPI.AddRayCanvasInteraction(panel.gameObject); QuickActionsAPI.AddPokeCanvasInteraction(panel.gameObject);
            var light=new GameObject("Workshop Key Light",typeof(Light)).GetComponent<Light>(); light.type=LightType.Directional; light.intensity=1.4f; light.shadows=LightShadows.None; light.transform.rotation=Quaternion.Euler(50,-30,0);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.42f,.48f,.55f);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Workshop scene save failed.");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(bench.position+Vector3.up*.25f,Quaternion.Euler(20,0,0),1.2f);
            Debug.Log("MR Workshop scene generated / three SDK-grabbable parts / cell socket / powered rotor. Headset acceptance pending.");
        }
        [MenuItem("Workshop/Open Desktop Workshop (Mouse and Keyboard)")]
        public static void OpenDesktop()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before opening another scene.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)==null)Generate();
            EditorSceneManager.OpenScene(ScenePath);
            var controller=UnityEngine.Object.FindFirstObjectByType<WorkshopController>(); controller.desktopMode=true;
            GameObject.Find("WorkshopPlatform").SetActive(false);
            foreach(var part in controller.parts)
                foreach(var component in part.GetComponentsInChildren<MonoBehaviour>(true))
                    if(component.GetType().Namespace?.StartsWith("Oculus.Interaction",StringComparison.Ordinal)==true)component.enabled=false;
            controller.workbench.Find("Workshop controls").gameObject.SetActive(false);
            var camera=new GameObject("Desktop simulation camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>(); camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,1.65f,-.23f); camera.transform.LookAt(new Vector3(0,1.06f,1.15f)); camera.fieldOfView=44;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.018f,.03f,.055f); camera.allowHDR=false;
            var input=controller.gameObject.AddComponent<WorkshopDesktopInput>(); input.workshop=controller; input.view=camera;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),DesktopPath);
            // The desktop scene is intentionally excluded from the Android build.
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            EditorApplication.isPlaying=true;
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        }
        static WorkshopPart MakePart(WorkshopController controller,Transform parent,string name,PartKind kind,PrimitiveType shape,Vector3 position,Vector3 scale,Material material)
        {
            var go=MakeShape(name,shape,parent,position,scale,material); QuickActionsAPI.AddGrabInteraction(go);
            var grab=go.GetComponent<Grabbable>(); if(grab==null)throw new InvalidOperationException("SDK Grabbable missing on "+name);
            grab.MaxGrabPoints=1; grab.InjectOptionalThrowWhenUnselected(false); grab.ForceKinematicDisabled=false;
            var body=go.GetComponent<Rigidbody>(); body.isKinematic=true; body.useGravity=false;
            var part=go.AddComponent<WorkshopPart>(); part.kind=kind; part.partId=kind.ToString(); part.workshop=controller;
            Label(parent,name,position+new Vector3(0,-.094f,-.106f),new Vector2(200,36),.00065f,20);
            return part;
        }
        static GameObject MakeShape(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static Material Material(string name,Color color,bool unlit=false)
        {
            Directory.CreateDirectory("Assets/Workshop/Art");
            string path="Assets/Workshop/Art/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { var shader=Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"); if(shader==null)throw new InvalidOperationException("URP shader missing."); material=new Material(shader); AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor",color); EditorUtility.SetDirty(material); return material;
        }
        static void Ring(Transform parent,Material material,float radius,float height)
        {
            var line=new GameObject("Socket cyan ring",typeof(LineRenderer)).GetComponent<LineRenderer>(); line.transform.SetParent(parent,false);
            line.useWorldSpace=false; line.loop=true; line.sharedMaterial=material; line.widthMultiplier=.004f; line.positionCount=48;
            for(int i=0;i<48;i++) { float a=i*Mathf.PI*2/48; line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,height,Mathf.Sin(a)*radius)); }
        }
        static void Label(Transform parent,string content,Vector3 position,Vector2 size,float scale,int points)
        {
            var canvas=new GameObject(content+" label",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>(); canvas.transform.SetParent(parent,false); canvas.renderMode=RenderMode.WorldSpace;
            var rect=(RectTransform)canvas.transform; rect.sizeDelta=size; rect.localScale=Vector3.one*scale; rect.localPosition=position;
            Text(rect,content,-size*.5f,size,points);
        }
        static Text Text(RectTransform parent,string content,Vector2 position,Vector2 size,int points)
        {
            var text=new GameObject("Label",typeof(RectTransform),typeof(Text)).GetComponent<Text>(); var rect=(RectTransform)text.transform;
            rect.SetParent(parent,false); rect.sizeDelta=size; rect.pivot=Vector2.zero; rect.anchoredPosition=position;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text=content; text.fontSize=points; text.color=new Color(.87f,.95f,1); text.raycastTarget=false;
            return text;
        }
        static void Button(RectTransform parent,WorkshopController controller,string action,Vector2 position)
        {
            var go=new GameObject(action,typeof(RectTransform),typeof(Image),typeof(UnityEngine.UI.Button),typeof(WorkshopButton)); var rect=(RectTransform)go.transform;
            rect.SetParent(parent,false); rect.sizeDelta=new Vector2(116,45); rect.anchoredPosition=position;
            var image=go.GetComponent<Image>(); image.color=new Color(.09f,.27f,.34f); go.GetComponent<UnityEngine.UI.Button>().targetGraphic=image;
            var input=go.GetComponent<WorkshopButton>(); input.workshop=controller; input.action=action;
            var text=Text(rect,action.ToUpperInvariant(),new Vector2(-58,-22.5f),new Vector2(116,45),19); text.alignment=TextAnchor.MiddleCenter;
        }
    }
}
