using System;
using UnityEditor;
using UnityEngine;

namespace DeadZone.Editor
{
    // Audio icons are editor overlays, not game art. Keep them out of Play Mode
    // while restoring the developer's Scene view preferences after the session.
    [InitializeOnLoad]
    public static class PlayModeAudioIcons
    {
        static readonly string Key = "DeadZone.PlayModeAudioIcons." + Application.dataPath + ".";

        static PlayModeAudioIcons()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.quitting += Restore;
            EditorApplication.delayCall += Reconcile;
        }

        static void Reconcile()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) Hide();
            else Restore();
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode) Hide();
            else if (state == PlayModeStateChange.EnteredEditMode) Restore();
        }

        static void Hide()
        {
            // SessionState survives the domain reload when entering or leaving Play.
            if (!SessionState.GetBool(Key + "Captured", false))
            {
                Capture(typeof(AudioSource));
                Capture(typeof(AudioListener));
                SessionState.SetBool(Key + "Captured", true);
            }
            SetIcon(typeof(AudioSource), false);
            SetIcon(typeof(AudioListener), false);
        }

        static void Capture(Type type)
        {
            bool known = GizmoUtility.TryGetGizmoInfo(type, out var info) && info.hasIcon;
            SessionState.SetBool(Key + type.Name + ".Known", known);
            if (known) SessionState.SetBool(Key + type.Name + ".Enabled", info.iconEnabled);
        }

        static void SetIcon(Type type, bool enabled)
        {
            if (GizmoUtility.TryGetGizmoInfo(type, out var info) && info.hasIcon)
                GizmoUtility.SetIconEnabled(type, enabled);
        }

        static void Restore()
        {
            if (!SessionState.GetBool(Key + "Captured", false)) return;
            Restore(typeof(AudioSource));
            Restore(typeof(AudioListener));
            SessionState.EraseBool(Key + "Captured");
        }

        static void Restore(Type type)
        {
            if (SessionState.GetBool(Key + type.Name + ".Known", false))
                SetIcon(type, SessionState.GetBool(Key + type.Name + ".Enabled", true));
            SessionState.EraseBool(Key + type.Name + ".Known");
            SessionState.EraseBool(Key + type.Name + ".Enabled");
        }
    }
}
