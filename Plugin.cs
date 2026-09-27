using System;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;

namespace LucidCatsLobby
{
    [BepInPlugin("lucidcats.lobby", "Lucid Cats 联机大厅", "1.1.0")]
    public class LobbyPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("=== Lucid Cats 联机大厅插件启动 ===");
            SceneManager.sceneLoaded += OnSceneLoaded;

            var go = new GameObject("LucidCatsLobby_RoomCodeManager");
            DontDestroyOnLoad(go);
            go.AddComponent<LobbyRoomCodeManager>();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
                LobbyMenu.Create(scene);
        }
    }

    // 常驻对象：主机按 F5 在「公开 / 有房间码」之间切换
    public class LobbyRoomCodeManager : MonoBehaviour
    {
        private bool _hasCode;
        private string _roomCode = "";
        private string _hint = "";
        private float _hintUntil;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f5Key.wasPressedThisFrame)
                ToggleRoomCode();
        }

        private void ToggleRoomCode()
        {
            try
            {
                var nm = NetworkManager.Singleton;
                if (nm == null || !nm.IsHost)
                {
                    ShowHint("Only the host can set a room code");
                    return;
                }

                var lobby = GetCurrentLobby();
                if (lobby == null)
                {
                    ShowHint("No lobby yet");
                    return;
                }

                _hasCode = !_hasCode;
                if (_hasCode)
                {
                    _roomCode = GenerateRoomCode();
                    lobby.Value.SetData("room_code", _roomCode);
                    ShowHint("Room code set: " + _roomCode);
                }
                else
                {
                    _roomCode = "";
                    lobby.Value.DeleteData("room_code");
                    ShowHint("Room is public (no code)");
                }
                LobbyPlugin.Log.LogInfo("[房间码] " + (_hasCode ? "已设码: " + _roomCode : "已公开（无码）"));
            }
            catch (Exception e)
            {
                LobbyPlugin.Log.LogWarning("[房间码] 切换失败: " + e.Message);
            }
        }

        private static string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // 去掉易混淆的 0O1I
            var sb = new StringBuilder();
            for (int i = 0; i < 6; i++)
                sb.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
            return sb.ToString();
        }

        private Lobby? GetCurrentLobby()
        {
            var t = FindType("HaniUtils.Steam.Netcode.SteamLobbyManager");
            if (t == null) return null;

            object obj = null;
            try
            {
                var objs = Resources.FindObjectsOfTypeAll(t);
                if (objs != null && objs.Length > 0) obj = objs[0];
            }
            catch { }

            if (obj == null) return null;

            var f = t.GetField("currentLobby", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return null;

            var v = f.GetValue(obj);
            if (v == null) return null;
            return (Lobby)v;
        }

        private static Type FindType(string fullName)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = a.GetType(fullName);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        private void ShowHint(string text)
        {
            _hint = text;
            _hintUntil = Time.unscaledTime + 3f;
        }

        // 左上角短暂提示 + 持续状态
        private void OnGUI()
        {
            if (Time.unscaledTime < _hintUntil && !string.IsNullOrEmpty(_hint))
            {
                var s = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
                GUI.Label(new Rect(24f, 24f, 600f, 40f), _hint, s);
            }

            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsHost)
            {
                var st = new GUIStyle(GUI.skin.label) { fontSize = 18 };
                string line = _hasCode
                    ? "Room code: " + _roomCode + "  (F5 = make public)"
                    : "Room: public  (F5 = set code)";
                GUI.Label(new Rect(24f, 64f, 420f, 30f), line, st);
            }
        }
    }
}
