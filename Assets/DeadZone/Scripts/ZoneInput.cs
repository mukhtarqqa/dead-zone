using UnityEngine;

namespace DeadZone
{
    // The imported map uses Legacy Input. Keep this prototype compatible without an Editor restart.
    public sealed class ZoneKey
    {
        readonly KeyCode code;
        public ZoneKey(KeyCode code) { this.code = code; }
        public bool isPressed => Input.GetKey(code);
        public bool wasPressedThisFrame => Input.GetKeyDown(code);
    }
    public sealed class ZoneKeyboard
    {
        public static readonly ZoneKeyboard Current = new();
        public readonly ZoneKey wKey = new(KeyCode.W), aKey = new(KeyCode.A), sKey = new(KeyCode.S), dKey = new(KeyCode.D);
        public readonly ZoneKey digit1Key = new(KeyCode.Alpha1), digit2Key = new(KeyCode.Alpha2), digit3Key = new(KeyCode.Alpha3), digit4Key = new(KeyCode.Alpha4);
        public readonly ZoneKey enterKey = new(KeyCode.Return), escapeKey = new(KeyCode.Escape), tabKey = new(KeyCode.Tab), rKey = new(KeyCode.R), qKey = new(KeyCode.Q), eKey = new(KeyCode.E), gKey = new(KeyCode.G), fKey = new(KeyCode.F), leftShiftKey = new(KeyCode.LeftShift);
    }
    public sealed class ZoneMouse
    {
        public static readonly ZoneMouse Current = new();
        public readonly MousePosition position = new();
        public readonly MouseButton leftButton = new();
        public sealed class MousePosition { public Vector2 ReadValue() => Input.mousePosition; }
        public sealed class MouseButton { public bool isPressed => Input.GetMouseButton(0); }
    }
}
