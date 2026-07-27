using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 짧은 안내 토스트. 잠금 안내("준비 중입니다. 곧 만나요."),
    /// 배정 불가 등 실패 피드백에 사용한다.
    /// 원칙(사운드 명세 5-1): 실패에 경고음/부정 표현을 쓰지 않는다 → 문구도 담백하게.
    /// 씬의 UI 루트 최상단(다른 팝업보다 위)에 1개 배치.
    /// </summary>
    public class ToastController : MonoBehaviour
    {
        public static ToastController Instance { get; private set; }

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _showSeconds = 1.6f;
        [SerializeField] private float _fadeSeconds = 0.2f;

        // 자주 쓰는 잠금 문구 상수
        public const string Locked = "준비 중입니다. 곧 만나요.";
        public const string LockedRegion = "이 지역은 아직 길이 열리지 않았어요.";

        private readonly Queue<string> _queue = new();
        private Coroutine _runner;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;   // 토스트는 입력을 막지 않는다
                _group.interactable = false;
                // GameObject를 끄지 않는다: 이 컨트롤러가 _group과 같은 오브젝트(Toast)에 있어도
                // 자기 자신을 비활성화해 코루틴이 멈추는 일이 없도록 alpha만으로 표시/숨김한다.
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>토스트 표시(큐잉).</summary>
        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _queue.Enqueue(message);
            if (_runner == null) _runner = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (_group == null || _label == null)
            {
                Debug.LogError("[ToastController] _group / _label 참조가 비어 있습니다.");
                _queue.Clear();
                _runner = null;
                yield break;
            }

            while (_queue.Count > 0)
            {
                _label.text = _queue.Dequeue();

                yield return Fade(0f, 1f);
                yield return new WaitForSecondsRealtime(_showSeconds);
                yield return Fade(1f, 0f);
            }

            _runner = null;
        }

        private IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            while (t < _fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(from, to, t / _fadeSeconds);
                yield return null;
            }
            _group.alpha = to;
        }
    }
}
