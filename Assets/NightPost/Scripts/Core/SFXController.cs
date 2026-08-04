using System.Collections.Generic;
using UnityEngine;

public class SFXController : MonoBehaviour
{
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private List<SFXTrackData> sfxTracks = new();

    private Dictionary<ESFXType, SFXTrackData> sfxClipDict = new();

    /// <summary>
    /// 등록된 UI 효과음을 검사하고 빠르게 조회할 수 있도록 초기화함
    /// </summary>
    public bool Initialize()
    {
        // UI 효과음 재생에 사용할 AudioSource가 없다면 초기화하지 않음
        if (sfxAudioSource == null) return false;

        // 등록된 UI 효과음 목록이 없다면 초기화하지 않음
        if(sfxTracks == null ||  sfxTracks.Count == 0) return false;

        // 기존 UI 효과음 조회 딕셔너리를 초기화함
        sfxClipDict.Clear();

        // 등록된 전체 UI 효과음 데이터를 순회함
        foreach (SFXTrackData soundTrack in sfxTracks)
        {
            // 유효하지 않은 UI 효과음 데이터는 제외함
            if(soundTrack == null) continue;

            // None 타입은 실제 효과음으로 등록하지 않음
            if(soundTrack.SoundType == ESFXType.None) continue;

            // AudioClip이 등록되지 않은 데이터는 제외함
            if(soundTrack.AudioClip == null) continue;

            // 이미 등록된 효과음 타입은 중복 등록하지 않음
            if (sfxClipDict.ContainsKey(soundTrack.SoundType)) continue;

            // 효과음 타입과 AudioClip을 조회 딕셔너리에 등록함
            sfxClipDict.Add(soundTrack.SoundType, soundTrack);
        }

        // 재생 가능한 UI 효과음이 없다면 초기화 실패를 반환함
        if(sfxClipDict.Count <= 0) return false;

        // UI 효과음은 반복하지 않도록 설정함
        sfxAudioSource.loop = false;
        sfxAudioSource.playOnAwake = false;

        // 초기화 성공을 반환함
        return true;
    }
    /// <summary>
    /// 지정한 종류의 UI 효과음을 전달받은 볼륨으로 한 번 재생함
    /// </summary>
    public bool PlaySFX(ESFXType soundType, float volume)
    {
        // UI 효과음 재생에 사용할 AudioSource가 없다면 재생하지 않음
        if (sfxAudioSource == null) return false;

        // None 타입이라면 재생하지 않음
        if (soundType == ESFXType.None) return false;

        // 지정한 효과음 타입에 해당하는 데이터를 조회함
        if (!sfxClipDict.TryGetValue(soundType, out SFXTrackData soundTrack)) return false;

        // 등록된 AudioClip이 없다면 재생하지 않음
        if (soundTrack.AudioClip == null) return false;

        // 전달받은 볼륨을 0부터 1 사이로 보정함
        volume = Mathf.Clamp01(volume);

        // 버튼에 설정된 볼륨으로 효과음을 한 번 재생함
        sfxAudioSource.PlayOneShot(soundTrack.AudioClip, volume);

        // UI 효과음 재생 성공을 반환함
        return true;
    }

    /// <summary>
    /// UI 효과음 볼륨을 0부터 1 사이의 값으로 변경함
    /// </summary>
    public bool SetVolume(float volume)
    {
        // UI 효과음 재생에 사용할 AudioSource가 없다면 변경하지 않음
        if (sfxAudioSource == null) return false;

        // 전달받은 볼륨을 0부터 1 사이로 보정함
        volume = Mathf.Clamp01(volume);

        // 보정한 볼륨을 UI 효과음 AudioSource에 적용함
        sfxAudioSource.volume = volume;

        // 볼륨 변경 성공을 반환함
        return true;
    }
    /// <summary>
    /// 현재 적용된 UI 효과음 볼륨을 반환함
    /// </summary>
    public float GetVolume()
    {
        // UI 효과음 재생에 사용할 AudioSource가 없다면 0을 반환함
        if (sfxAudioSource == null) return 0;

        // 현재 UI 효과음 AudioSource에 적용된 볼륨을 반환함
        return sfxAudioSource.volume;
    }
}
