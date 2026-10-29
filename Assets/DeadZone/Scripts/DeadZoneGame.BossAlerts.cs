using System.Collections.Generic;
using UnityEngine;

namespace DeadZone
{
    // Part of the existing game/camera controller; no second camera or input controller.
    public sealed partial class DeadZoneGame
    {
        public const float BossApproachSeconds=.5f, BossHoldSeconds=1f, BossReturnSeconds=.5f;
        public enum BossCameraPhase { Follow, Approach, Hold, Return }
        public BossCameraPhase BossFocusPhase { get; private set; }
        public Transform BossFocusTarget => bossFocusTarget;
        public IReadOnlyList<Transform> BossTargets => bossTargets;
        readonly List<Transform> bossTargets=new();
        readonly HashSet<Transform> announcedBosses=new();
        readonly Queue<Transform> bossFocusQueue=new();
        Transform bossFocusTarget;
        ZonePlayer bossFocusActor;
        Vector3 bossFocusStart;
        float bossFocusElapsed;
        Vector3 lastCameraFollowPoint;
        bool hasCameraFollowPoint;

        public bool BossIsAlive(Transform target)
        {
            if(target==null||!target.gameObject.activeInHierarchy)return false;
            if(target.TryGetComponent<ZoneEnemy>(out var enemy))
                return enemy.game==this&&enemy.boss&&enemy.health>0&&enemies.Contains(enemy);
            return target.TryGetComponent<ZoneGhost>(out var ghost)&&ghost.BossAlive;
        }

        public void NotifyBossSpawned(ZoneEnemy enemy)
        {
            if(enemy!=null&&enemy.game==this&&enemy.boss&&enemy.health>0)RegisterBossAlert(enemy.transform);
        }
        public void NotifyBossSpawned(ZoneGhost ghost)
        {
            if(ghost!=null&&ghost.BossAlive)RegisterBossAlert(ghost.transform);
        }
        void RegisterBossAlert(Transform target)
        {
            if(!running||!BossIsAlive(target)||!announcedBosses.Add(target))return;
            bossTargets.Add(target);bossFocusQueue.Enqueue(target);
        }
        public void RemoveBossAlert(Transform target)
        {
            bossTargets.Remove(target);
            // Keep its announcement until the next match: stale snapshots cannot replay an intro.
        }
        public void ResetBossAlerts()
        {
            bossTargets.Clear();announcedBosses.Clear();bossFocusQueue.Clear();
            BossFocusPhase=BossCameraPhase.Follow;bossFocusTarget=null;bossFocusActor=null;bossFocusElapsed=0;
        }
        void PruneBossAlerts()
        {
            if(!running||Restarting){ResetBossAlerts();return;}
            for(int i=bossTargets.Count-1;i>=0;i--)if(!BossIsAlive(bossTargets[i]))bossTargets.RemoveAt(i);
        }
        Vector3 BossCameraPoint(Vector3 point)
        {
            point.z=gameCamera.transform.position.z;
            return arena!=null?arena.ClampCamera(point,gameCamera):point;
        }
        void ReturnFromBoss(Vector3 current)
        {
            BossFocusPhase=BossCameraPhase.Return;bossFocusStart=current;bossFocusElapsed=0;
            bossFocusTarget=null;
        }
        Vector3 AdvanceBossCamera(Vector3 followTarget, ZonePlayer tracked, float delta)
        {
            Vector3 current=gameCamera.transform.position;
            if(BossFocusPhase==BossCameraPhase.Follow&&running&&!paused&&player!=null&&!player.Dead)
            {
                while(bossFocusQueue.Count>0)
                {
                    var next=bossFocusQueue.Dequeue();if(!BossIsAlive(next))continue;
                    bossFocusTarget=next;bossFocusActor=tracked;bossFocusStart=current;bossFocusElapsed=0;
                    BossFocusPhase=BossCameraPhase.Approach;break;
                }
            }
            if(BossFocusPhase==BossCameraPhase.Follow)
                return Vector3.Lerp(current,followTarget,1-Mathf.Exp(-8*delta));
            if((BossFocusPhase==BossCameraPhase.Approach||BossFocusPhase==BossCameraPhase.Hold)&&
                (!BossIsAlive(bossFocusTarget)||player==null||player.Dead||bossFocusActor!=tracked))ReturnFromBoss(current);
            if(!paused)bossFocusElapsed+=Mathf.Max(0,delta);
            Vector3 result;
            switch(BossFocusPhase)
            {
                case BossCameraPhase.Approach:
                    result=Vector3.Lerp(bossFocusStart,BossCameraPoint(bossFocusTarget.position),
                        Mathf.SmoothStep(0,1,bossFocusElapsed/BossApproachSeconds));
                    if(bossFocusElapsed>=BossApproachSeconds){BossFocusPhase=BossCameraPhase.Hold;bossFocusElapsed-=BossApproachSeconds;}
                    break;
                case BossCameraPhase.Hold:
                    result=BossCameraPoint(bossFocusTarget.position);
                    if(bossFocusElapsed>=BossHoldSeconds)
                    {
                        BossFocusPhase=BossCameraPhase.Return;bossFocusStart=result;bossFocusElapsed-=BossHoldSeconds;
                        bossFocusTarget=null;
                    }
                    break;
                default:
                    result=Vector3.Lerp(bossFocusStart,followTarget,Mathf.SmoothStep(0,1,bossFocusElapsed/BossReturnSeconds));
                    if(bossFocusElapsed>=BossReturnSeconds)
                    {
                        BossFocusPhase=BossCameraPhase.Follow;bossFocusTarget=null;bossFocusActor=null;bossFocusElapsed=0;
                    }
                    break;
            }
            return result;
        }
    }
}
