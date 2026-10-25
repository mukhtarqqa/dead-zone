using System;
using UnityEngine;

namespace DeadZone
{
    [Serializable]
    public struct ZoneCommand
    {
        public Vector2 move, aim;
        public bool fire, reload, swap, dash, ability, interact, interactHeld, grenade, mine, drop;
        public static ZoneCommand Read(Camera camera)
        {
            var k = ZoneKeyboard.Current;
            return new ZoneCommand
            {
                move = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0)).normalized,
                aim = camera.ScreenToWorldPoint(Input.mousePosition), fire = Input.GetMouseButton(0),
                reload = k.rKey.wasPressedThisFrame, swap = k.tabKey.wasPressedThisFrame, dash = k.leftShiftKey.wasPressedThisFrame,
                ability = k.qKey.wasPressedThisFrame, interact = k.eKey.wasPressedThisFrame, interactHeld = k.eKey.isPressed,
                grenade = k.gKey.wasPressedThisFrame, mine = k.fKey.wasPressedThisFrame, drop = Input.GetKeyDown(KeyCode.Space)
            };
        }
        public void ClearEdges() { reload = swap = dash = ability = interact = grenade = mine = drop = false; }
        public void Merge(ZoneCommand next) { move = next.move; aim = next.aim; fire = next.fire; interactHeld = next.interactHeld; reload |= next.reload; swap |= next.swap; dash |= next.dash; ability |= next.ability; interact |= next.interact; grenade |= next.grenade; mine |= next.mine; drop |= next.drop; }
        public bool IsFinite => float.IsFinite(move.x) && float.IsFinite(move.y) && float.IsFinite(aim.x) && float.IsFinite(aim.y);
    }
}
