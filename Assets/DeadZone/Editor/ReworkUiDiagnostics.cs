using System;
using UnityEditor;
using UnityEngine;
namespace DeadZone.Editor
{
    public static class ReworkUiDiagnostics
    {
        [MenuItem("Dead Zone/Rework/Inspect combat UI")]
        public static void Inspect()
        {
            var g=DeadZoneGame.Instance;if(!EditorApplication.isPlaying||g==null)throw new Exception("Enter Play Mode");if(!g.running)g.Begin();Canvas.ForceUpdateCanvases();var map=UnityEngine.Object.FindAnyObjectByType<ZoneMinimap>(FindObjectsInactive.Include);if(map==null)throw new Exception("Minimap absent");if(!map.isActiveAndEnabled){Debug.Log("DZ_MINIMAP_WAIT: retry after HUD activation frame");return;}map.SetVerticesDirty();Canvas.ForceUpdateCanvases();var renderer=map.GetComponent<CanvasRenderer>();var mesh=renderer.GetMesh();
            Debug.Log("DZ_MINIMAP_INSPECT: rect="+map.rectTransform.rect+" color="+map.color+" active="+map.isActiveAndEnabled+" cull="+renderer.cull+" vertices="+mesh.vertexCount+" shader="+map.materialForRendering.shader.name+" texture="+map.mainTexture.name+" rendererColor="+renderer.GetColor()+" alpha="+renderer.GetInheritedAlpha());
        }
        [MenuItem("Dead Zone/Rework/Preview control help")]
        public static void Controls(){DeadZoneGame.Instance.GetComponent<ZoneOptions>().ShowControls();}
    }
}
