using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Steamworks;
using Steamworks.Data;
using Object = UnityEngine.Object;

namespace LucidCatsLobby
{
    internal class LobbyMenu : MonoBehaviour
    {
        private sealed class RoomRow
        {
            public Lobby Lobby;
            public TMP_Text Name;
            public TMP_Text Count;
        }

        private readonly List<RoomRow> rows = new List<RoomRow>();
        private readonly Dictionary<ulong, string> ownerNames = new Dictionary<ulong, string>();

        private Component statsMenu;
        private Component settingsMenu;
        private Component lobbyMenu;
        private Transform panelTransform;

        private Transform grid;
        private Transform content;
        private GameObject rowTemplate;
        private float rowHeight;
        private TMP_Text statusText;
        private Lobby[] lobbies;
        private bool loading;
        private bool _showCodeInput;
        private string _codeInput = "";
        private static TMP_FontAsset _cnFont;

        public static void Create(Scene scene)
        {
            var go = new GameObject("Lobby (mod)");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<LobbyMenu>();
        }

        private void Start()
        {
            try
            {
                Build();
                LobbyPlugin.Log.LogInfo("Lobby added to the main menu.");
            }
            catch (Exception e)
            {
                LobbyPlugin.Log.LogError("Could not build the lobby menu: " + e);
            }
        }

        private void Build()
        {
            Transform root = FindMenuRoot();
            if (root == null)
                throw new Exception("Could not find 'Canvas/4x3' in the main menu.");

            Transform statsButton = Require(root, "Margins/grid/UIButton (stats)");
            Transform settingsButton = root.Find("Margins/grid/UIButton (settings)");
            Transform statsPanel = Require(root, "Stats menu");
            Transform settingsPanel = root.Find("Settings Menu");

            statsMenu = FindMenuComponent(statsPanel.gameObject);
            settingsMenu = settingsPanel != null ? FindMenuComponent(settingsPanel.gameObject) : null;

            BuildButton(statsButton, settingsButton);
            Transform panel = BuildPanel(statsPanel);
            panelTransform = panel;
            BuildRefreshButton(statsButton, panel);
            BuildCodeButton(statsButton, panel);
        }

        private Transform FindMenuRoot()
        {
            foreach (GameObject go in gameObject.scene.GetRootGameObjects())
            {
                if (go.name != "Canvas") continue;
                Transform found = go.transform.Find("4x3");
                if (found != null) return found;
            }
            return null;
        }

        private void BuildButton(Transform statsButton, Transform settingsButton)
        {
            GameObject button = Object.Instantiate(statsButton.gameObject, statsButton.parent, false);
            button.name = "UIButton (lobby)";
            button.transform.SetSiblingIndex(statsButton.GetSiblingIndex() + 1);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "Lobby";

            UnityEvent click = FindClickEvent(button);
            if (click == null)
                throw new Exception("Could not find the click event of the Lobby button.");

            for (int i = 0; i < click.GetPersistentEventCount(); i++)
                click.SetPersistentListenerState(i, UnityEventCallState.Off);
            click.AddListener(OnLobbyClicked);

            FindClickEvent(statsButton.gameObject)?.AddListener(CloseLobby);
            if (settingsButton != null)
                FindClickEvent(settingsButton.gameObject)?.AddListener(CloseLobby);
        }

        private Transform BuildPanel(Transform statsPanel)
        {
            var holder = new GameObject("Lobby Holder");
            holder.SetActive(false);

            GameObject panel = Object.Instantiate(statsPanel.gameObject, holder.transform, false);
            panel.name = "Lobby menu";
            foreach (Game.UI.LifetimeStatsDisplay stats in panel.GetComponentsInChildren<Game.UI.LifetimeStatsDisplay>(true))
                Object.DestroyImmediate(stats);

            // 标题
            Transform title = panel.transform.Find("Text (TMP)");
            if (title != null && title.GetComponent<TMP_Text>() != null)
                title.GetComponent<TMP_Text>().text = "Lobby";

            // 状态文本
            grid = Require(panel.transform, "grid");
            Transform nameTemplate = Require(grid, "Text name");
            statusText = CloneText(nameTemplate, panel.transform, "Lobby Status");
            var statusRect = (RectTransform)statusText.transform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.offsetMin = new Vector2(40f, -70f);
            statusRect.offsetMax = new Vector2(-40f, -40f);
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.text = "Searching...";

            // 保存行模板（挂到 panel 下并隐藏，避免被 holder 一起销毁）
            Transform rowTemplateTransform = Require(grid, "stat display");
            rowTemplate = Object.Instantiate(rowTemplateTransform.gameObject, panel.transform, false);
            rowTemplate.SetActive(false);

            foreach (Transform child in grid)
                Object.Destroy(child.gameObject);

            // 列表滚动：grid 作为视口，手动定位行
            var gridLayout = grid.GetComponent<LayoutGroup>();
            if (gridLayout != null) gridLayout.enabled = false;

            grid.gameObject.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Lobby Content", typeof(RectTransform));
            content = contentGo.transform;
            content.SetParent(grid, false);
            var contentRect = (RectTransform)content;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(0f, 0f);
            contentRect.offsetMax = new Vector2(0f, 0f);

            rowHeight = ((RectTransform)rowTemplate.transform).rect.height;
            if (rowHeight <= 0f) rowHeight = 30f;

            // grid 底部上缩，给左下角按钮留空间
            var gridRect = (RectTransform)grid;
            gridRect.offsetMin = new Vector2(gridRect.offsetMin.x, gridRect.offsetMin.y + 50f);

            var scroll = grid.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = (RectTransform)grid;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            panel.transform.SetParent(statsPanel.parent, false);
            panel.transform.SetSiblingIndex(statsPanel.GetSiblingIndex() + 1);
            Object.Destroy(holder);

            lobbyMenu = FindMenuComponent(panel);
            if (lobbyMenu == null)
                throw new Exception("The cloned panel has no Menu component.");

            return panel.transform;
        }

        private void BuildRefreshButton(Transform statsButton, Transform panel)
        {
            GameObject btn = Object.Instantiate(statsButton.gameObject, panel, false);
            btn.name = "Refresh Button";
            var rect = (RectTransform)btn.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(30f, 12f);
            rect.localScale = new Vector3(0.6f, 0.6f, 1f);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "Refresh";

            UnityEvent click = FindClickEvent(btn);
            if (click != null)
            {
                for (int i = 0; i < click.GetPersistentEventCount(); i++)
                    click.SetPersistentListenerState(i, UnityEventCallState.Off);
                click.AddListener(OnRefreshClicked);
            }
        }

        private void BuildCodeButton(Transform statsButton, Transform panel)
        {
            GameObject btn = Object.Instantiate(statsButton.gameObject, panel, false);
            btn.name = "Code Button";
            var rect = (RectTransform)btn.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(130f, 12f);
            rect.localScale = new Vector3(0.6f, 0.6f, 1f);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "Join by code";

            UnityEvent click = FindClickEvent(btn);
            if (click != null)
            {
                for (int i = 0; i < click.GetPersistentEventCount(); i++)
                    click.SetPersistentListenerState(i, UnityEventCallState.Off);
                click.AddListener(() =>
                {
                    _showCodeInput = true;
                    _codeInput = "";
                });
            }
        }

        private void OnRefreshClicked()
        {
            Refresh();
        }

        private RoomRow CreateRoomRow(Lobby lobby)
        {
            GameObject go = Object.Instantiate(rowTemplate, content, false);
            go.SetActive(true);

            // 手动定位：顶部锚定、全宽、固定行高、按序号排
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, rowHeight);
            rect.anchoredPosition = new Vector2(0f, -rows.Count * rowHeight);

            var row = new RoomRow
            {
                Lobby = lobby,
                Name = FindText(go.transform, "Text name"),
                Count = FindText(go.transform, "Text value"),
            };

            // 给房间行加中文字体，避免中文房主名显示口口口
            TMP_FontAsset cnFont = EnsureCnFont();
            if (row.Name != null && cnFont != null) row.Name.font = cnFont;
            if (row.Count != null && cnFont != null) row.Count.font = cnFont;

            Button button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                button.targetGraphic = img;
            button.onClick.AddListener(() => JoinLobby(lobby));

            return row;
        }

        private static TMP_FontAsset EnsureCnFont()
        {
            if (_cnFont != null) return _cnFont;
            try
            {
                string path = Path.Combine(Paths.PluginPath, "cnfont.ttf");
                Font osFont = null;
                if (File.Exists(path))
                    osFont = new Font(path);
                else
                    osFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 32);
                if (osFont != null)
                    _cnFont = TMP_FontAsset.CreateFontAsset(osFont);
            }
            catch (Exception e)
            {
                LobbyPlugin.Log.LogWarning("[字体] 中文字体加载失败: " + e.Message);
            }
            return _cnFont;
        }

        private void RefreshRows()
        {
            foreach (RoomRow row in rows)
                if (row.Name != null)
                    Object.Destroy(row.Name.transform.parent.gameObject);
            rows.Clear();

            if (lobbies == null) return;
            foreach (Lobby lobby in lobbies)
            {
                if (string.IsNullOrEmpty(lobby.GetData("HostSteamId"))) continue; // 只显示本游戏房间
                if (!string.IsNullOrEmpty(lobby.GetData("room_code"))) continue; // 带房间码的房隐藏（靠输码进）
                RoomRow row = CreateRoomRow(lobby);
                if (row.Name != null)
                    row.Name.text = GetOwnerName(lobby);
                if (row.Count != null)
                    row.Count.text = lobby.MemberCount + "/" + lobby.MaxMembers;
                rows.Add(row);
            }

            // 根据行数设置 content 高度，让 ScrollRect 能滚动
            if (content != null)
                ((RectTransform)content).sizeDelta = new Vector2(0f, rows.Count * rowHeight);
        }

        private string GetOwnerName(Lobby lobby)
        {
            if (ownerNames.TryGetValue(lobby.Id.Value, out string cached))
                return cached;
            string hostId = lobby.GetData("HostSteamId");
            return string.IsNullOrEmpty(hostId) ? "unknown" : hostId;
        }

        private void OnLobbyClicked()
        {
            // 置顶到最上层，像原 UI 那样盖住上一个面板
            if (panelTransform != null)
                panelTransform.SetAsLastSibling();
            CallMenu(statsMenu, "Close");
            CallMenu(settingsMenu, "Close");
            CallMenu(lobbyMenu, "Toggle");
            Refresh();
        }

        private void CloseLobby()
        {
            CallMenu(lobbyMenu, "Close");
        }

        private async void Refresh()
        {
            if (loading) return;
            loading = true;
            if (statusText != null) statusText.text = "Searching...";
            try
            {
                var query = SteamMatchmaking.LobbyList
                    .FilterDistanceWorldwide()
                    .WithSlotsAvailable(1)
                    .WithMaxResults(100);
                lobbies = await query.RequestAsync();
                int lucidCount = 0;
                if (lobbies != null)
                    foreach (var lb in lobbies)
                        if (!string.IsNullOrEmpty(lb.GetData("HostSteamId"))) lucidCount++;
                if (statusText != null)
                    statusText.text = "Rooms: " + lucidCount + "  (click a room to join)";
                LobbyPlugin.Log.LogInfo("[大厅] 搜索完成，共 " + lucidCount + " 个本游戏房间（总 " + (lobbies?.Length ?? 0) + "）");
                RefreshRows();
                LoadOwnerNames();
            }
            catch (Exception e)
            {
                if (statusText != null) statusText.text = "Search failed: " + e.Message;
                LobbyPlugin.Log.LogError("[大厅] 搜索失败: " + e);
            }
            finally
            {
                loading = false;
            }
        }

        private async void LoadOwnerNames()
        {
            if (lobbies == null) return;
            foreach (Lobby lobby in lobbies)
            {
                string hostId = lobby.GetData("HostSteamId");
                if (string.IsNullOrEmpty(hostId)) continue;
                ulong key = lobby.Id.Value;
                if (ownerNames.ContainsKey(key)) continue;
                try
                {
                    ulong id = ulong.Parse(hostId);
                    var friend = new Friend((SteamId)id);
                    await friend.RequestInfoAsync();
                    string name = friend.Name;
                    if (!string.IsNullOrEmpty(name))
                        ownerNames[key] = name;
                }
                catch { }
            }
            // 名字拿回来后刷新显示
            RefreshRows();
        }

        private void JoinLobby(Lobby lobby)
        {
            DoJoin(lobby);
        }

        private async void JoinByCode()
        {
            string code = _codeInput.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(code)) return;
            try
            {
                if (statusText != null) statusText.text = "Searching code " + code + "...";
                var query = SteamMatchmaking.LobbyList
                    .FilterDistanceWorldwide()
                    .WithKeyValue("room_code", code);
                var result = await query.RequestAsync();
                if (result != null && result.Length > 0)
                {
                    DoJoin(result[0]);
                }
                else
                {
                    if (statusText != null) statusText.text = "No room with code " + code;
                }
            }
            catch (Exception e)
            {
                if (statusText != null) statusText.text = "Join by code failed: " + e.Message;
                LobbyPlugin.Log.LogError("[大厅] 输码加入失败: " + e);
            }
        }

        private async void DoJoin(Lobby lobby)
        {
            try
            {
                if (statusText != null) statusText.text = "Joining...";
                var r = await lobby.Join();
                if (statusText != null) statusText.text = "Join result: " + r;
                LobbyPlugin.Log.LogInfo("[大厅] 加入结果: " + r + "  lobbyId=" + lobby.Id);
            }
            catch (Exception e)
            {
                if (statusText != null) statusText.text = "Join failed: " + e.Message;
                LobbyPlugin.Log.LogError("[大厅] 加入失败: " + e);
            }
        }

        private void OnGUI()
        {
            if (!_showCodeInput) return;

            float w = 340f, h = 140f;
            float x = (Screen.width - w) / 2f, y = (Screen.height - h) / 2f;
            GUI.Box(new Rect(x, y, w, h), "Enter room code");
            GUI.SetNextControlName("lobbyCodeField");
            _codeInput = GUI.TextField(new Rect(x + 20f, y + 40f, w - 40f, 30f), _codeInput);

            if (GUI.Button(new Rect(x + 20f, y + 80f, w - 40f, 32f), "Join"))
            {
                _showCodeInput = false;
                JoinByCode();
            }
        }

        // ==== 工具方法（照搬 Bestiary 的做法）====

        private static Transform Require(Transform parent, string path)
        {
            Transform found = parent.Find(path);
            if (found == null)
                throw new Exception("Could not find '" + path + "' under '" + parent.name + "'.");
            return found;
        }

        private static TMP_Text FindText(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static TMP_Text CloneText(Transform template, Transform parent, string newName)
        {
            GameObject go = Object.Instantiate(template.gameObject, parent, false);
            go.name = newName;
            return go.GetComponent<TMP_Text>();
        }

        private static Component FindMenuComponent(GameObject go)
        {
            foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                for (Type t = mb.GetType(); t != null; t = t.BaseType)
                    if (t.FullName == "HaniUtils.UI.Menu")
                        return mb;
            }
            return null;
        }

        private static UnityEvent FindClickEvent(GameObject go)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                for (Type t = mb.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                {
                    FieldInfo field = t.GetField("onClick", flags | BindingFlags.DeclaredOnly);
                    if (field != null && field.GetValue(mb) is UnityEvent unityEvent)
                        return unityEvent;
                }
            }
            return null;
        }

        private static void CallMenu(Component menu, string methodName)
        {
            if (menu == null) return;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                for (Type t = menu.GetType(); t != null; t = t.BaseType)
                {
                    MethodInfo method = t.GetMethod(methodName, flags | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                    if (method != null)
                    {
                        method.Invoke(menu, null);
                        return;
                    }
                }
                LobbyPlugin.Log.LogWarning("Menu method '" + methodName + "' not found.");
            }
            catch (Exception e)
            {
                LobbyPlugin.Log.LogWarning("Could not call " + methodName + ": " + (e.InnerException?.Message ?? e.Message));
            }
        }
    }
}
