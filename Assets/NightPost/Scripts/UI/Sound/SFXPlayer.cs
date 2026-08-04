using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    [SerializeField] private SFXController sfxController;
    [SerializeField] private ESFXType soundType = ESFXType.None;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

    /// <summary>
    /// 오브젝트에 설정된 효과음을 재생함
    /// </summary>
    public void PlaySound()
    {
        // 효과음 컨트롤러가 없다면 재생하지 않음
        if (sfxController == null) return;

        // None 타입이라면 재생하지 않음
        if (soundType == ESFXType.None) return;

        // 설정된 볼륨으로 효과음을 재생함
        sfxController.PlaySFX(soundType, soundVolume);
    }
}
