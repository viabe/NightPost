using System.Collections.Generic;
using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// UI에서 효과음을 부르기 위한 접근점.
    /// 컨트롤러마다 SFXController를 인스펙터로 연결하면 슬롯이 너무 많아지므로,
    /// 씬에 하나만 두고 정적으로 접근한다(ToastController와 같은 방식).
    ///
    /// 볼륨은 사운드 명세 §1-2를 따른다. UI 0.50 / 핵심 연출 0.70.
    /// PlayOneShot이라 소리가 겹치므로, 같은 효과음이 짧은 간격으로 반복되면
    /// 한 번만 울리도록 쿨다운을 둔다(편지 여러 통이 한꺼번에 도착하는 경우 등).
    /// </summary>
    public class UISoundPlayer : MonoBehaviour
    {
        public static UISoundPlayer Instance { get; private set; }

        [SerializeField] private SFXController _sfx;

        [Header("볼륨 (사운드 명세 §1-2)")]
        [SerializeField, Range(0f, 1f)] private float _uiVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _accentVolume = 0.7f;

        [Header("중복 방지")]
        [Tooltip("같은 효과음이 이 시간 안에 다시 요청되면 무시한다.")]
        [SerializeField] private float _cooldownSeconds = 0.12f;

        private readonly Dictionary<ESFXType, float> _lastPlayedAt = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_sfx == null) Debug.LogError("[UISound] SFXController 미연결 — 효과음이 재생되지 않는다", this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>일반 UI 효과음.</summary>
        public static void Play(ESFXType type)
        {
            if (Instance != null) Instance.PlayInternal(type, Instance._uiVolume);
        }

        /// <summary>강조 효과음(답장 도착·해금·강화 등). 조금 크게 재생한다.</summary>
        public static void PlayAccent(ESFXType type)
        {
            if (Instance != null) Instance.PlayInternal(type, Instance._accentVolume);
        }

        private void PlayInternal(ESFXType type, float volume)
        {
            if (_sfx == null || type == ESFXType.None) return;

            float now = Time.unscaledTime;
            if (_lastPlayedAt.TryGetValue(type, out float last) && now - last < _cooldownSeconds) return;
            _lastPlayedAt[type] = now;

            _sfx.PlaySFX(type, volume);
        }
    }
}
