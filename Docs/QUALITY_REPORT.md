# Quality pass — 2026-10-23

## Changes

- Editable city arena with project-owned replacement materials; no old map dependency. All 8,068 walkable nodes are connected. One floor.
- Progressive waves, simultaneous enemy cap, safe spawn distance. Boss patterns: dash, spread shot, area attack, summons and radial bullets. Boss health exceeds one Gojo projectile.
- Wall-aware explosions, zero-clamped player health, summoned-enemy cleanup after boss death.
- Generated combat audio, damage numbers, camera shake, volume settings, pause/input blocking, crosshair, reload/ability bars and boss health bar.
- Directional sprites, weapon muzzle/reload fixes, Gojo purple projectile and Rem 40-second horn-and-mace form retained.
- Multiplayer counters/time/boss health snapshots; protocol 7.
- Removed unused packages. Export includes scene, editable assets, code, packages and project settings.

## Native verification

Quality regression passed: 8,068/8,068 navigation nodes reachable, owned scene dependencies, progressive spawning, boss balance, pause/input, blast-wall isolation, zero health clamp and advancing game frames (1 → 265).

Gojo checks passed: 16 directional cast poses, charge, 420 damage, piercing, 22 range, no repeated damage/self recharge, walls/floors and replica phases. Rem checks passed: 40-second start timer, no extension, 110 melee damage, cone/multiple targets, rate limit, no ammo use/self charge, walls/floors and expiry. Expiry uses a short test timer.

Windows build: Succeeded, 0 errors.

LAN smoke: Editor host + Windows standalone client passed authoritative movement/shooting with two players. Rem horn/mace, swing and gun restoration passed. Gojo charge/flight/impact, Subaru clock, barrier, mine, grenade, pulse, reload and floor effects were received by the client. Host detected the client's disconnect.

Clean-source verification completed on 2026-10-25 from a short Windows path with a newly created Library: scene dependencies checked, C# compiled, Windows build Succeeded with 0 errors. DZ_CLEAN_CHECKOUT_PASS. The deeply nested initial verification path exceeded Windows path limits for some package USS files; use a short checkout path as explained in README.

## Remaining work

Three characters are available. Full Kazakh localization and the final 20–30 minute balance pass remain. Generated sprites/sounds can be replaced with authored assets. LAN smoke testing does not replace a four-player long-session test or internet-service validation using the user's own Unity Cloud project.
