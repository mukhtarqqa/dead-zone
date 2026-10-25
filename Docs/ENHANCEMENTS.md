Последующее обновление карты и оформления: [REWORK.md](REWORK.md). Ниже — предыдущее состояние до переработки.

# Gameplay and presentation update — 2026-10-26

Implemented:

- Distinct weapon audio, pooled playback, reload cues, projectile trails, impact sparks and brief knockback for ordinary enemies.
- Editable district landmarks and cover: parking, clinic, market, memorial park and transit stop. Navigation regression: 7,996/7,996 nodes connected.
- Boss volleys telegraph before firing. Second phases begin at half health and increase attack pressure. Added boss phase transition/death effects.
- Original generated musical loops for menu, waves and bosses, with separate saved music volume.
- Unity Localization RU/KK string/asset tables, language settings, persisted language and a bundled dynamic font covering Kazakh glyphs.
- Per-character completed-match statistics: matches, victories, kills, damage, ally revives, play time and fastest victory. Menu statistics panel and per-player result rows. Result clock stops when the match ends.
- Ally health, down timer, revive progress and off-screen SOS indicators. Network snapshots include personal statistics and these states; reconnecting players retain ownership of their existing loot.
- Sukuna added last: 12 directional sprites, Curse King passive healing and the Malevolent Shrine active area attack. [SUKUNA.md](SUKUNA.md).
- Character speeds now use catalog settings. Defaults: Subaru 100 HP / 3.7 speed, Rem 120 / 3.4, Gojo 90 / 3.65, Sukuna 110 / 3.5.

Validation results are appended after the final build/network run. Protocol 8 requires all participants to update together.

Internet service integration still requires linking the user's Unity Cloud project. Automated local multi-process checks do not cover independent internet connections, real-world latency or a complete 20–30 minute balance playthrough.
