using UnityEngine;

namespace DeadZone
{
    // Both authority enemies and client ghosts enter through the same pose edge.
    public sealed class ZoneZombieVoice : MonoBehaviour
    {
        ZoneEnemy enemy;
        DeadZoneGame game;
        System.Random random;
        float health, readyAt, nextGrowlAt;
        int kind, pose;
        bool boss, wasAttacking;

        public DeadZoneGame Game => game;
        public int Kind => kind;
        public bool Boss => boss;
        public bool Audible => isActiveAndEnabled && game != null && game.running &&
            !game.paused && health > 0 && Time.time >= readyAt;

        public void Bind(ZoneEnemy value)
        {
            enemy = value;
            Initialize(value.game);
        }

        public void ObserveSnapshot(DeadZoneGame value, ZoneObjectState state)
        {
            if (game == null) Initialize(value);
            health = state.health;
            readyAt = Time.time + Mathf.Max(0, state.remaining);
            kind = state.kind;
            boss = state.boss;
            pose = state.animation;
        }

        void Initialize(DeadZoneGame value)
        {
            game = value;
            random = new System.Random(GetEntityId().GetHashCode() ^ 0x35a9);
            nextGrowlAt = Time.time + Between(1, 4);
        }

        void LateUpdate()
        {
            if (enemy != null)
            {
                health = enemy.health;
                readyAt = enemy.ReadyAt;
                kind = enemy.kind;
                boss = enemy.boss;
                pose = enemy.AnimationPose;
            }
            bool attacking = pose == 3;
            if (!Audible)
            {
                // Pausing, spawning or ending a match must not replay a pose on resume.
                wasAttacking = attacking;
                return;
            }
            var bank = ZoneZombieAudio.Bank;
            if (bank == null) return;
            if (attacking && !wasAttacking)
            {
                ZoneZombieAudio.Play(this, true);
                nextGrowlAt = Time.time + Between(bank.minGrowlInterval, bank.maxGrowlInterval);
            }
            wasAttacking = attacking;
            if (!attacking && Time.time >= nextGrowlAt)
            {
                nextGrowlAt = Time.time + Between(bank.minGrowlInterval, bank.maxGrowlInterval);
                ZoneZombieAudio.Play(this, false);
            }
        }

        float Between(float min, float max) => min + (float)random.NextDouble() * Mathf.Max(0, max - min);
        void OnDisable() => ZoneZombieAudio.Stop(this);
    }
}
