using System.Collections;
using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 모든 화면/팝업의 공통 베이스.
    /// 열기/닫기 생명주기와 CanvasGroup 페이드를 통일한다.
    /// 규칙(역할분담표 3.1): 뷰는 저장 데이터를 직접 수정하지 않는다.
    /// 시스템에 요청하고, 결과 이벤트를 받아 Bind로 갱신만 한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseView : MonoBehaviour
    {
        [Header("BaseView")]
        [SerializeField] private UIScreenId _id = UIScreenId.None;
        [SerializeField] private float _fadeDuration = 0.15f;

        /// <summary>PopupManager 등록 키.</summary>
        public UIScreenId Id => _id;

        /// <summary>현재 열려 있는지 여부.</summary>
        public bool IsOpen { get; private set; }

        private CanvasGroup _cg;
        protected CanvasGroup CanvasGroup => _cg != null ? _cg : (_cg = GetComponent<CanvasGroup>());

        private Coroutine _fadeRoutine;

        /// <summary>
        /// PopupManager가 시작 시 호출. 즉시 숨김 상태로 초기화하고 비활성화한다.
        /// (BaseView 스스로 Awake에서 끄면 재활성화 시점과 꼬일 수 있어 매니저가 명시 호출)
        /// </summary>
        internal void InitHidden()
        {
            IsOpen = false;
            var cg = CanvasGroup;
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        /// <summary>모델 없이 열기.</summary>
        public void Open()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            OnOpen();
            StartFade(1f, interactable: true);
        }

        /// <summary>닫기. 페이드 아웃 후 비활성화.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            OnClose();
            StartFade(0f, interactable: false, deactivateOnEnd: true);
        }

        /// <summary>열릴 때 1회 호출. 파생 뷰가 UI 구성/구독을 여기서 한다.</summary>
        protected virtual void OnOpen() { }

        /// <summary>닫힐 때 1회 호출. 구독 해제/정리를 여기서 한다.</summary>
        protected virtual void OnClose() { }

        private void StartFade(float target, bool interactable, bool deactivateOnEnd = false)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            if (!gameObject.activeInHierarchy)
            {
                // 비활성 상태면 즉시 반영
                ApplyInstant(target, interactable, deactivateOnEnd);
                return;
            }
            _fadeRoutine = StartCoroutine(FadeRoutine(target, interactable, deactivateOnEnd));
        }

        private void ApplyInstant(float target, bool interactable, bool deactivateOnEnd)
        {
            var cg = CanvasGroup;
            cg.alpha = target;
            cg.interactable = interactable;
            cg.blocksRaycasts = interactable;
            if (deactivateOnEnd) gameObject.SetActive(false);
        }

        private IEnumerator FadeRoutine(float target, bool interactable, bool deactivateOnEnd)
        {
            var cg = CanvasGroup;
            cg.blocksRaycasts = interactable; // 열 때는 즉시 입력 차단막 활성
            float start = cg.alpha;
            float t = 0f;

            if (_fadeDuration > 0f)
            {
                while (t < _fadeDuration)
                {
                    t += Time.unscaledDeltaTime; // 정산 중 timeScale 영향 없이 동작
                    cg.alpha = Mathf.Lerp(start, target, t / _fadeDuration);
                    yield return null;
                }
            }

            cg.alpha = target;
            cg.interactable = interactable;
            cg.blocksRaycasts = interactable;
            if (deactivateOnEnd) gameObject.SetActive(false);
            _fadeRoutine = null;
        }
    }

    /// <summary>
    /// 모델을 받아 바인딩하는 뷰의 베이스.
    /// 사용 예: public class MorningReportController : BaseView&lt;DeliveryReport&gt; { ... }
    /// </summary>
    public abstract class BaseView<TModel> : BaseView
    {
        protected TModel Model { get; private set; }

        /// <summary>모델과 함께 열기.</summary>
        public void Open(TModel model)
        {
            Model = model;
            Open(); // base.Open() -> OnOpen() -> Bind(Model)
        }

        protected sealed override void OnOpen() => Bind(Model);

        /// <summary>모델을 실제 UI 요소에 반영한다.</summary>
        protected abstract void Bind(TModel model);
    }
}
