using System.Collections.Generic;
using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 팝업 스택 관리자. 씬의 UI 루트에 1개 배치한다.
    /// _popupRoot 하위의 모든 BaseView를 자동 수집해 UIScreenId로 등록한다.
    /// - 반투명 딤(dimmer)은 열린 팝업이 하나라도 있으면 표시
    /// - Android 뒤로가기 처리는 Input System 연결 후 확장(현재 미포함)
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        [SerializeField] private Transform _popupRoot;   // 팝업 BaseView들의 부모
        [SerializeField] private GameObject _dimmer;     // 반투명 배경(선택)

        private readonly Dictionary<UIScreenId, BaseView> _map = new();
        private readonly List<BaseView> _stack = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_popupRoot != null)
            {
                // 비활성 팝업까지 포함해 수집
                var views = _popupRoot.GetComponentsInChildren<BaseView>(includeInactive: true);
                foreach (var v in views)
                {
                    if (v.Id == UIScreenId.None)
                    {
                        Debug.LogWarning($"[PopupManager] Id가 None인 뷰는 등록되지 않습니다: {v.name}", v);
                        continue;
                    }
                    if (_map.ContainsKey(v.Id))
                    {
                        Debug.LogError($"[PopupManager] 중복된 UIScreenId={v.Id} ({v.name})", v);
                        continue;
                    }
                    _map[v.Id] = v;
                    v.InitHidden();
                }
            }

            if (_dimmer != null) _dimmer.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>등록된 뷰를 형변환해서 가져온다. (없으면 null)</summary>
        public T Get<T>(UIScreenId id) where T : BaseView
            => _map.TryGetValue(id, out var v) ? v as T : null;

        /// <summary>Id로 팝업 열기. 열린 뷰를 반환.</summary>
        public BaseView Open(UIScreenId id)
        {
            if (!_map.TryGetValue(id, out var view))
            {
                Debug.LogError($"[PopupManager] 등록되지 않은 UIScreenId={id}");
                return null;
            }
            Push(view);
            return view;
        }

        /// <summary>모델과 함께 열기(제네릭 뷰용).</summary>
        public T Open<TModel, T>(UIScreenId id, TModel model) where T : BaseView<TModel>
        {
            var view = Get<T>(id);
            if (view == null)
            {
                Debug.LogError($"[PopupManager] {typeof(T).Name} (Id={id}) 를 찾을 수 없습니다.");
                return null;
            }
            if (!_stack.Contains(view)) _stack.Add(view);
            view.Open(model);
            RefreshDimmer();
            return view;
        }

        /// <summary>뷰 인스턴스를 직접 스택에 올려 연다.</summary>
        public void Push(BaseView view)
        {
            if (view == null) return;
            if (!_stack.Contains(view)) _stack.Add(view);
            view.Open();
            RefreshDimmer();
        }

        /// <summary>특정 뷰 닫기.</summary>
        public void Close(BaseView view)
        {
            if (view == null) return;
            _stack.Remove(view);
            view.Close();
            RefreshDimmer();
        }

        /// <summary>Id로 닫기.</summary>
        public void Close(UIScreenId id)
        {
            if (_map.TryGetValue(id, out var v)) Close(v);
        }

        /// <summary>가장 위 팝업 닫기(뒤로가기 등).</summary>
        public void CloseTop()
        {
            if (_stack.Count == 0) return;
            Close(_stack[_stack.Count - 1]);
        }

        /// <summary>모든 팝업 닫기.</summary>
        public void CloseAll()
        {
            for (int i = _stack.Count - 1; i >= 0; i--) _stack[i].Close();
            _stack.Clear();
            RefreshDimmer();
        }

        public bool HasOpenPopup => _stack.Count > 0;

        private void RefreshDimmer()
        {
            if (_dimmer != null) _dimmer.SetActive(_stack.Count > 0);
        }
    }
}
