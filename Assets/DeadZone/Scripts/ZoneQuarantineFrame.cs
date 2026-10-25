using UnityEngine;
using UnityEngine.UI;
namespace DeadZone
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ZoneQuarantineFrame : MaskableGraphic
    {
        public static void Attach(GameObject panel,Vector2 size,bool backdrop=false)
        {
            if(!backdrop&&(size.x<280||size.y<70))return;
            var obj=new GameObject("Worn quarantine frame",typeof(RectTransform),typeof(CanvasRenderer));obj.transform.SetParent(panel.transform,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;obj.AddComponent<ZoneQuarantineFrame>().raycastTarget=false;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;if(r.width<80||r.height<40)return;
            Color steel=new(.47f,.56f,.43f,.38f),wear=new(.5f,.48f,.37f,.13f),rust=new(.72f,.28f,.19f,.45f);
            float length=r.height>200?20:11;
            foreach(float x in new[]{r.xMin+4,r.xMax-4})foreach(float y in new[]{r.yMin+4,r.yMax-4})
            {
                float dx=x<r.center.x?1:-1,dy=y<r.center.y?1:-1;Line(vh,new Vector2(x,y),new Vector2(x+dx*length,y),1,steel);Line(vh,new Vector2(x,y),new Vector2(x,y+dy*length),1,steel);
            }
            for(int i=0;i<6;i++){float x=r.xMax-92+i*11;Line(vh,new Vector2(x,r.yMax-9),new Vector2(x+5,r.yMax-16),2.5f,rust);}
            for(int i=0;i<14;i++)
            {
                float x=r.xMin+14+Mathf.Repeat(i*67.39f,r.width-40),y=i%2==0?r.yMin+6+Mathf.Repeat(i*1.7f,9):r.yMax-7-Mathf.Repeat(i*2.1f,6);
                Line(vh,new Vector2(x,y),new Vector2(Mathf.Min(x+3+i%4*4,r.xMax-8),y+(i%3-1)),.7f,wear);
            }
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            Vector2 d=(b-a).normalized,n=new(-d.y,d.x);n*=width*.5f;int v=vh.currentVertCount;
            vh.AddVert(a-n,c,Vector2.zero);vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);
        }
    }
}
