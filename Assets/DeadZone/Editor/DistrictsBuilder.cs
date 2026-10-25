using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
namespace DeadZone.Editor
{
    public static class DistrictsBuilder
    {
        [MenuItem("Dead Zone/Detail city districts")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/DeadZoneCity.unity");
            if(GameObject.Find("District landmarks")!=null){Debug.Log("DZ_DISTRICTS_ALREADY_READY");return;}
            var root=new GameObject("District landmarks");Sprite sprite=null;
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/DeadZone/CityArt/solid.asset"))if(asset is Sprite s)sprite=s;
            if(sprite==null)throw new System.Exception("City solid sprite missing");
            GameObject Block(string name,Vector2 point,Vector2 size,Color color,bool solid=false,int order=-15)
            {
                var obj=new GameObject(name);obj.layer=20;obj.transform.SetParent(root.transform);obj.transform.position=point;obj.transform.localScale=size;
                var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingLayerID=ZoneMap.Sorting(20);sr.sortingOrder=order;
                if(solid)obj.AddComponent<BoxCollider2D>().size=Vector2.one;return obj;
            }
            void Sign(string name,Vector2 point,string label,Color color)
            {
                Block(name+" backing",point,new Vector2(3,.5f),new Color(.055f,.085f,.1f),false,-1);
                var obj=new GameObject(name);obj.transform.SetParent(root.transform);obj.transform.position=point;var text=obj.AddComponent<TextMeshPro>();text.font=TMP_Settings.defaultFontAsset;text.text=label;text.fontSize=2.1f;text.color=color;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(3,.5f);text.GetComponent<MeshRenderer>().sortingLayerID=ZoneMap.Sorting(20);text.GetComponent<MeshRenderer>().sortingOrder=0;
            }
            var yellow=new Color(.72f,.63f,.35f);var steel=new Color(.23f,.3f,.33f);
            Sign("Parking sign",new Vector2(-16,-4.5f),"P · PARKING",yellow);
            for(int i=0;i<4;i++)
            {
                float x=-22+i*3;Block("Parking bay",new Vector2(x,-6),new Vector2(.05f,2.4f),yellow);
                if(i%2==0){Block("Parking cover",new Vector2(x+1,-6.4f),new Vector2(1.9f,.9f),steel,true,4);Block("Car glass",new Vector2(x+1,-6.4f),new Vector2(.55f,.65f),new Color(.12f,.2f,.27f),false,5);}
            }
            Sign("Clinic sign",new Vector2(10,8.35f),"+ · CLINIC",new Color(.4f,.95f,.75f));
            Block("Clinic cross vertical",new Vector2(11,8.8f),new Vector2(.15f,.7f),new Color(.4f,.95f,.75f),false,0);Block("Clinic cross horizontal",new Vector2(11,8.8f),new Vector2(.6f,.15f),new Color(.4f,.95f,.75f),false,0);
            Sign("Market sign",new Vector2(20,8.35f),"MARKET",new Color(1,.67f,.36f));
            for(int i=0;i<2;i++){Block("Market counter",new Vector2(18+i*3,5.1f),new Vector2(1.6f,.65f),new Color(.43f,.28f,.18f),true,4);Block("Market canopy",new Vector2(18+i*3,5.5f),new Vector2(1.8f,.22f),i==0?new Color(.65f,.28f,.25f):new Color(.28f,.5f,.47f),false,5);}
            Sign("Park sign",new Vector2(-14,16.1f),"MEMORIAL PARK",new Color(.53f,.78f,.48f));
            foreach(float x in new[]{-18f,-10f,10f,18f})
            {
                Block("Park bench",new Vector2(x,15.5f),new Vector2(1.5f,.35f),new Color(.42f,.30f,.20f),true,4);
                Block("Bench back",new Vector2(x,15.7f),new Vector2(1.6f,.10f),steel,false,5);
            }
            Sign("Station sign",new Vector2(-18,-13.6f),"TRANSIT · 05",new Color(.43f,.71f,.9f));
            for(int i=0;i<3;i++)Block("Transit bollard",new Vector2(-21+i*.75f,-8.2f),new Vector2(.18f,.35f),yellow,true,4);
            foreach(var point in new[]{new Vector2(-5.8f,3),new Vector2(5.8f,-3),new Vector2(-23,7),new Vector2(23,-6)})
            {
                Block("Street light base",point,new Vector2(.22f,.22f),steel,true,4);Block("Street light glow",point+Vector2.up*.4f,new Vector2(.28f,.1f),new Color(1,.8f,.45f),false,6);
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("DZ_DISTRICTS_READY: parking, clinic, market, park, transit; editable cover and landmarks");
        }
    }
}
