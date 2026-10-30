using UnityEngine;

namespace DeadZone
{
    public enum ZoneExtractionPhase { None, Arriving, Boarding, Departing, Complete }

    public sealed partial class DeadZoneGame
    {
        public const int BossGoal = 5;
        public const float HelicopterArrivalSeconds = 7, HelicopterDepartureSeconds = 6.5f, BoardingRadius = 3;
        public int BossesDefeated { get; private set; }
        public ZoneExtractionPhase ExtractionPhase { get; private set; }
        public float ExtractionAge { get; private set; }
        public Vector2 ExtractionLanding { get; private set; }
        public Vector2 ExtractionEntry { get; private set; }
        public Vector2 ExtractionExit { get; private set; }
        public ZoneHelicopterView Helicopter { get; private set; }
        public bool Extracting => ExtractionPhase != ZoneExtractionPhase.None;
        public bool ExtractionCinematic => running && (ExtractionPhase == ZoneExtractionPhase.Arriving || ExtractionPhase == ZoneExtractionPhase.Departing);
        int defeatedBossWaves;
        Vector3 extractionCameraStart, extractionCameraReturn;
        bool extractionReturning;

        public bool Participating(ZonePlayer actor) => actor != null && actor.Connected && (network == null || !network.Active ||
            (network.ClientOnly ? actor.Connected : network.IsParticipantConnected(actor.OwnerId)));
        public int ExtractionCrew { get { int count=0; foreach(var actor in players) if(Participating(actor)) count++; return count; } }
        public int ExtractionSeated { get { int count=0; foreach(var actor in players) if(Participating(actor)&&actor.Boarded) count++; return count; } }

        void ResetExtraction(bool resetPassengers=true)
        {
            BossesDefeated=defeatedBossWaves=0; ExtractionPhase=ZoneExtractionPhase.None; ExtractionAge=0;
            extractionReturning=false;
            if(Helicopter!=null) Destroy(Helicopter.gameObject); Helicopter=null;
            if(resetPassengers)foreach(var actor in players) if(actor!=null) actor.Boarded=false;
        }

        void RecordBossDefeat()
        {
            int bit=1<<Mathf.Clamp(wave-1,0,BossGoal-1);
            if((defeatedBossWaves&bit)!=0)return;
            defeatedBossWaves|=bit; BossesDefeated=Mathf.Min(BossGoal,BossesDefeated+1);
        }

        public void BeginExtraction()
        {
            if(!IsAuthority||!running||Extracting||BossesDefeated<BossGoal)return;
            ResetBossAlerts(); pendingEnemies=groupRemaining=0; spawnCluster=null; bossPhase=false; breakUntil=0;
            foreach(var enemy in enemies.ToArray()) if(enemy!=null) Destroy(enemy.gameObject); enemies.Clear();
            foreach(var bullet in FindObjectsByType<ZoneBullet>()) Destroy(bullet.gameObject);
            ExtractionLanding=FindExtractionLanding();
            Rect bounds=arena!=null?arena.Bounds:new Rect(-28,-22,56,44);
            ExtractionEntry=new Vector2(bounds.xMin+2,Mathf.Min(bounds.yMax-7,ExtractionLanding.y+16));
            ExtractionExit=new Vector2(bounds.xMax+5,bounds.yMax+4);
            foreach(var actor in players) if(Participating(actor)&&(actor.Dead||actor.Downed)) actor.Revive(true);
            ChangeExtractionPhase(ZoneExtractionPhase.Arriving);
            message="Вертолёт прибыл";
            EnsureHelicopter(); PresentHelicopter();
        }

        Vector2 FindExtractionLanding()
        {
            Vector2 center=arena!=null?arena.Bounds.center:Vector2.zero;
            if(ZoneMap.Current==null)return center;
            bool Clear(Vector2 p)
            {
                if(!ZoneMap.Current.Walkable(p,20))return false;
                for(int i=0;i<12;i++)
                {
                    float a=i*Mathf.PI/6;
                    if(!ZoneMap.Current.Walkable(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*2.6f,20))return false;
                }
                return true;
            }
            if(Clear(center))return center;
            Vector2 result=center;float best=float.MaxValue;
            foreach(var node in ZoneMap.Current.WalkableNodes)
            {
                if(node.z!=20)continue;Vector2 point=ZoneMap.Current.WorldPoint(node);float distance=(point-center).sqrMagnitude;
                if(distance>=best||!Clear(point))continue;
                bool reachable=true;foreach(var actor in players)if(Participating(actor)&&!ZoneMap.Current.Reachable(point,20,actor)){reachable=false;break;}
                if(reachable){best=distance;result=point;}
            }
            return result;
        }

        void ChangeExtractionPhase(ZoneExtractionPhase phase)
        {
            ExtractionPhase=phase; ExtractionAge=0; extractionReturning=false;
            if(gameCamera!=null)extractionCameraStart=gameCamera.transform.position;
        }

        void EnsureHelicopter()
        {
            if(Helicopter!=null||!Extracting)return;
            var prefab=Resources.Load<GameObject>("Extraction/RescueHelicopter");
            var obj=prefab!=null?Instantiate(prefab):ZoneHelicopterView.CreateModel();
            obj.name="Rescue helicopter";obj.layer=20;Helicopter=obj.GetComponent<ZoneHelicopterView>();
        }

        public bool CanBoardHelicopter(ZonePlayer actor) => running && ExtractionPhase==ZoneExtractionPhase.Boarding &&
            Participating(actor)&&!actor.Boarded&&!actor.Dead&&!actor.Downed&&actor.gameObject.layer==20&&
            Vector2.Distance(actor.transform.position,ExtractionLanding)<=BoardingRadius;

        public bool TryBoardHelicopter(ZonePlayer actor)
        {
            if(!IsAuthority||paused||!CanBoardHelicopter(actor))return false;
            SeatPassenger(actor);
            if(ExtractionCrew>0&&ExtractionSeated==ExtractionCrew)ChangeExtractionPhase(ZoneExtractionPhase.Departing);
            return true;
        }

        void SeatPassenger(ZonePlayer actor)
        {
            if(actor.Dead||actor.Downed)actor.Revive(true);
            actor.Boarded=true;actor.Oni.Cancel();actor.GetComponent<ZoneHeroVoice>()?.Stop();
            var body=actor.GetComponent<Rigidbody2D>();body.linearVelocity=Vector2.zero;body.simulated=false;
            actor.GetComponent<Collider2D>().enabled=false;
            actor.transform.position=ExtractionLanding;
        }

        // The host advances phases; replicas only animate the phase and clock sent by the host.
        public void TickExtraction(float delta)
        {
            if(!Extracting)return;
            if(ExtractionPhase==ZoneExtractionPhase.Complete){ExtractionAge+=Time.unscaledDeltaTime;return;}
            if(!running||paused)return;
            ExtractionAge+=Mathf.Max(0,delta);
            if(IsAuthority)
            {
                if(ExtractionPhase==ZoneExtractionPhase.Arriving&&ExtractionAge>=HelicopterArrivalSeconds)
                    ChangeExtractionPhase(ZoneExtractionPhase.Boarding);
                if(ExtractionPhase==ZoneExtractionPhase.Boarding)
                {
                    foreach(var actor in players)if(Participating(actor)&&(actor.Dead||actor.Downed))actor.Revive(true);
                    if(ExtractionCrew>0&&ExtractionSeated==ExtractionCrew)ChangeExtractionPhase(ZoneExtractionPhase.Departing);
                }
                if(ExtractionPhase==ZoneExtractionPhase.Departing)
                {
                    // A reconnect during the departure joins its existing team's evacuation.
                    foreach(var actor in players)if(Participating(actor)&&!actor.Boarded)SeatPassenger(actor);
                    if(ExtractionAge>=HelicopterDepartureSeconds){ChangeExtractionPhase(ZoneExtractionPhase.Complete);Finish(true);}
                }
            }
            EnsureHelicopter();PresentHelicopter();
            if(Helicopter!=null)foreach(var actor in players)if(actor!=null&&actor.Boarded)actor.transform.position=Helicopter.transform.position;
        }

        public Vector2 HelicopterPoint(ZoneExtractionPhase phase,float age)
        {
            if(phase==ZoneExtractionPhase.Arriving)
                return Vector2.Lerp(ExtractionEntry,ExtractionLanding,Mathf.SmoothStep(0,1,Mathf.Clamp01(age/5.8f)));
            if(phase==ZoneExtractionPhase.Departing||phase==ZoneExtractionPhase.Complete)
                return Vector2.Lerp(ExtractionLanding,ExtractionExit,Mathf.SmoothStep(0,1,Mathf.Clamp01(((phase==ZoneExtractionPhase.Complete?HelicopterDepartureSeconds:age)-.8f)/5.5f)));
            return ExtractionLanding;
        }

        void PresentHelicopter()
        {
            if(Helicopter==null)return;
            float altitude=ExtractionPhase==ZoneExtractionPhase.Arriving?Mathf.Lerp(4,0,Mathf.SmoothStep(0,1,ExtractionAge/HelicopterArrivalSeconds)):
                ExtractionPhase==ZoneExtractionPhase.Boarding?0:ExtractionPhase==ZoneExtractionPhase.Complete?4:Mathf.Lerp(0,4,Mathf.Clamp01(ExtractionAge/1.8f));
            Vector2 direction=ExtractionPhase==ZoneExtractionPhase.Arriving?ExtractionLanding-ExtractionEntry:ExtractionExit-ExtractionLanding;
            float heading=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90;
            if(ExtractionPhase==ZoneExtractionPhase.Arriving)heading=Mathf.LerpAngle(heading,0,Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.5f,6.5f,ExtractionAge)));
            else if(ExtractionPhase==ZoneExtractionPhase.Boarding)heading=0;
            else heading=Mathf.LerpAngle(0,heading,Mathf.SmoothStep(0,1,ExtractionAge/1.2f));
            Helicopter.transform.position=HelicopterPoint(ExtractionPhase,ExtractionAge);
            Helicopter.Present(altitude,heading,ExtractionPhase==ZoneExtractionPhase.Boarding,ExtractionSeated);
        }

        bool ExtractionCamera(Vector3 follow,float delta,out Vector3 point)
        {
            point=gameCamera.transform.position;
            if(!ExtractionCinematic&&ExtractionPhase!=ZoneExtractionPhase.Complete)return false;
            if(ExtractionPhase==ZoneExtractionPhase.Complete)return true;
            Vector3 focus=Helicopter!=null?Helicopter.FocusPoint:(Vector3)ExtractionLanding;
            focus.z=point.z;if(arena!=null)focus=arena.ClampCamera(focus,gameCamera);
            if(ExtractionPhase==ZoneExtractionPhase.Arriving&&ExtractionAge>=HelicopterArrivalSeconds-1.2f)
            {
                if(!extractionReturning){extractionReturning=true;extractionCameraReturn=point;}
                point=Vector3.Lerp(extractionCameraReturn,follow,Mathf.SmoothStep(0,1,Mathf.InverseLerp(HelicopterArrivalSeconds-1.2f,HelicopterArrivalSeconds,ExtractionAge)));
            }
            else point=ExtractionAge<.8f?Vector3.Lerp(extractionCameraStart,focus,Mathf.SmoothStep(0,1,ExtractionAge/.8f)):
                Vector3.Lerp(point,focus,1-Mathf.Exp(-12*Mathf.Max(0,delta)));
            return true;
        }

        public void ApplyExtraction(ZoneWorld world)
        {
            BossesDefeated=Mathf.Clamp(world.bossesDefeated,0,BossGoal);
            var phase=(ZoneExtractionPhase)Mathf.Clamp(world.extractionPhase,0,(int)ZoneExtractionPhase.Complete);
            if(phase==ZoneExtractionPhase.None)
            {
                if(Extracting)ResetExtraction();BossesDefeated=Mathf.Clamp(world.bossesDefeated,0,BossGoal);return;
            }
            if(phase!=ExtractionPhase)ChangeExtractionPhase(phase);
            ExtractionAge=Mathf.Max(0,world.extractionAge);
            ExtractionLanding=world.extractionLanding;ExtractionEntry=world.extractionEntry;ExtractionExit=world.extractionExit;
            EnsureHelicopter();PresentHelicopter();
        }
    }
}
