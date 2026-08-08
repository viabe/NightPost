using System.Collections.Generic;

namespace NightPost.UI
{
    /// <summary>
    /// 전체 화면 UI가 한 번에 하나만 열리도록 정리한다.
    ///
    /// 각 화면은 Awake에서 Register, OnDestroy에서 Unregister 하고,
    /// Open 시작 시 NotifyOpened를 부른다. 그러면 나머지 화면이 자동으로 닫힌다.
    ///
    /// 팝업(IUIScreen을 구현하지 않는 것)은 여기에 등록되지 않으므로
    /// 시설 강화 상세처럼 화면 위에 겹쳐 뜨는 창은 뒤 화면을 닫지 않는다.
    ///
    /// 씬 오브젝트가 필요 없도록 정적 클래스로 둔다. 씬이 바뀌면
    /// 각 화면이 파괴되며 스스로 등록을 해제한다.
    /// </summary>
    public static class UIScreenRouter
    {
        private static readonly List<IUIScreen> _screens = new();

        public static void Register(IUIScreen screen)
        {
            if (screen == null || _screens.Contains(screen)) return;
            _screens.Add(screen);
        }

        public static void Unregister(IUIScreen screen)
        {
            if (screen == null) return;
            _screens.Remove(screen);
        }

        /// <summary>
        /// 한 화면이 열렸음을 알린다. 열려 있던 다른 화면을 모두 닫는다.
        /// 화면 자신의 Open 처리보다 먼저 불러야 조작 잠금이 꼬이지 않는다.
        /// </summary>
        public static void NotifyOpened(IUIScreen opened)
        {
            if (opened == null) return;

            // Close 안에서 목록이 바뀔 수 있으므로 역순으로 순회한다.
            for (int i = _screens.Count - 1; i >= 0; i--)
            {
                IUIScreen screen = _screens[i];
                if (screen == null)
                {
                    _screens.RemoveAt(i);
                    continue;
                }
                if (ReferenceEquals(screen, opened)) continue;
                if (!screen.IsScreenOpen) continue;

                screen.Close();
            }
        }

        /// <summary>열려 있는 화면을 모두 닫는다.</summary>
        public static void CloseAll()
        {
            for (int i = _screens.Count - 1; i >= 0; i--)
            {
                IUIScreen screen = _screens[i];
                if (screen == null) { _screens.RemoveAt(i); continue; }
                if (screen.IsScreenOpen) screen.Close();
            }
        }
    }
}
