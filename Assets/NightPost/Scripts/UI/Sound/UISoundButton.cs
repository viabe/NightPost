using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UISoundButton : MonoBehaviour
{
    [SerializeField] private SFXController uiSoundController;
    [SerializeField] private ESFXType soundType = ESFXType.None;
    [SerializeField] private bool isSoundEnabled = true;
    [SerializeField, Range(0f, 1f)] private float buttonVolume = 1f;

    private Button button;


    /// <summary>
    /// 버튼 클릭 효과음 이벤트를 등록함
    /// </summary>
    private void OnEnable()
    {
        // 버튼을 조회하지 못했다면 다시 조회함
        if (button == null) button = GetComponent<Button>();

        // 버튼을 조회하지 못했다면 이벤트를 등록하지 않음
        if (button == null) return;

        // 버튼 클릭 효과음 이벤트를 등록함
        button.onClick.AddListener(PlaySound);
    }

    /// <summary>
    /// 버튼 클릭 효과음 이벤트를 해제함
    /// </summary>
    private void OnDisable()
    {
        // 버튼이 없다면 처리하지 않음
        if (button == null) return;

        // 버튼 클릭 효과음 이벤트를 해제함
        button.onClick.RemoveListener(PlaySound);
    }

    /// <summary>
    /// 버튼에 설정된 UI 효과음을 재생함
    /// </summary>
    private void PlaySound()
    {
        // 버튼 효과음이 비활성화되어 있다면 재생하지 않음
        if (!isSoundEnabled) return;

        // UI 효과음 컨트롤러가 없다면 재생하지 않음
        if (uiSoundController == null) return;

        // None 타입이라면 재생하지 않음
        if (soundType == ESFXType.None) return;

        // 버튼에 설정된 볼륨으로 효과음을 재생함
        uiSoundController.PlaySFX(soundType, buttonVolume);
    }
}
