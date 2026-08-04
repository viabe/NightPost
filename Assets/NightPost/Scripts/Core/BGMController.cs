using System.Collections.Generic;
using UnityEngine;

public class BGMController : MonoBehaviour
{
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private List<BGMTrackData> bgmTracks = new();

    private Dictionary<EBGMType, BGMTrackData> bgmTrackDict = new();
    private EBGMType currentBGMType = EBGMType.None;
    private float bgmMasterVolume = 1f;

    /// <summary>
    /// 등록된 BGM 데이터를 검사하고 빠르게 조회할 수 있도록 초기화함
    /// </summary>
    public bool Initialize()
    {
        // BGM 재생에 사용할 AudioSource가 없다면 초기화하지 않음
        if (bgmAudioSource == null) return false;

        // 등록된 BGM 목록이 없다면 초기화하지 않음
        if (bgmTracks == null || bgmTracks.Count <= 0) return false;

        // 기존 BGM 조회 딕셔너리를 초기화함
        bgmTrackDict.Clear();

        // 등록된 전체 BGM 데이터를 순회함
        foreach (BGMTrackData bgmTrack in bgmTracks)
        {
            // 유효하지 않은 BGM 데이터는 제외함
            if (bgmTrack == null) continue;

            // None 타입은 실제 재생 곡으로 등록하지 않음
            if (bgmTrack.BGMType == EBGMType.None) continue;

            // AudioClip이 등록되지 않은 데이터는 제외함
            if (bgmTrack.AudioClip == null) continue;

            // 이미 등록된 BGM 타입은 중복 등록하지 않음
            if (bgmTrackDict.ContainsKey(bgmTrack.BGMType)) continue;

            // BGM 타입과 BGM 데이터를 조회 딕셔너리에 등록함
            bgmTrackDict.Add(bgmTrack.BGMType, bgmTrack);
        }

        // 재생 가능한 BGM이 하나도 없다면 초기화 실패를 반환함
        if (bgmTrackDict.Count <= 0) return false;

        // AudioSource에 설정된 볼륨을 전체 BGM 볼륨으로 저장함
        bgmMasterVolume = Mathf.Clamp01(bgmAudioSource.volume);

        // BGM 반복 재생을 활성화함
        bgmAudioSource.loop = true;

        // 초기화 성공을 반환함
        return true;
    }

    /// <summary>
    /// 지정한 종류의 BGM을 곡별 볼륨을 적용하여 즉시 재생함
    /// </summary>
    public bool PlayBGM(EBGMType bgmType)
    {
        // BGM 재생에 사용할 AudioSource가 없다면 재생하지 않음
        if (bgmAudioSource == null) return false;

        // BGM 타입이 None이라면 재생하지 않음
        if (bgmType == EBGMType.None) return false;

        // 지정한 BGM 타입에 해당하는 BGM 데이터를 조회함
        if (!bgmTrackDict.TryGetValue(bgmType, out BGMTrackData bgmTrack)) return false;

        // 등록된 AudioClip이 없다면 재생하지 않음
        if (bgmTrack.AudioClip == null) return false;

        // 전체 볼륨과 곡별 볼륨을 조합함
        float finalVolume = bgmMasterVolume * bgmTrack.BgmVolume;

        // 최종 볼륨을 0부터 1 사이로 보정하여 적용함
        bgmAudioSource.volume = Mathf.Clamp01(finalVolume);

        // 현재 같은 BGM이 정상적으로 재생 중이라면 다시 시작하지 않음
        if (currentBGMType == bgmType && bgmAudioSource.clip == bgmTrack.AudioClip && bgmAudioSource.isPlaying) return true;

        // AudioSource에 조회한 AudioClip을 등록함
        bgmAudioSource.clip = bgmTrack.AudioClip;

        // BGM을 재생함
        bgmAudioSource.Play();

        // 현재 재생 중인 BGM 타입을 저장함
        currentBGMType = bgmType;

        // BGM 재생 성공을 반환함
        return true;
    }

    /// <summary>
    /// 현재 재생 중인 BGM을 정지함
    /// </summary>
    public bool StopBGM()
    {
        // BGM 재생에 사용할 AudioSource가 없다면 정지하지 않음
        if (bgmAudioSource == null) return false;

        // 현재 재생 중인 BGM을 정지함
        bgmAudioSource.Stop();

        // AudioSource에 등록된 기존 클립을 제거함
        bgmAudioSource.clip = null;

        // 현재 BGM 타입을 None으로 초기화함
        currentBGMType = EBGMType.None;

        // BGM 정지 성공을 반환함
        return true;
    }

    /// <summary>
    /// 전체 BGM 볼륨을 0부터 1 사이의 값으로 변경함
    /// </summary>
    public bool SetVolume(float volume)
    {
        // BGM 재생에 사용할 AudioSource가 없다면 변경하지 않음
        if (bgmAudioSource == null) return false;

        // 전달받은 볼륨을 0부터 1 사이로 보정함
        bgmMasterVolume = Mathf.Clamp01(volume);

        // 현재 재생 중인 BGM 데이터를 조회함
        if (bgmTrackDict.TryGetValue(currentBGMType, out BGMTrackData bgmTrack))
        {
            // 전체 볼륨과 현재 곡의 개별 볼륨을 조합함
            float finalVolume = bgmMasterVolume * bgmTrack.BgmVolume;

            // 최종 볼륨을 AudioSource에 적용함
            bgmAudioSource.volume = Mathf.Clamp01(finalVolume);
        }
        else
        {
            // 재생 중인 BGM이 없다면 전체 볼륨만 적용함
            bgmAudioSource.volume = bgmMasterVolume;
        }

        // 볼륨 변경 성공을 반환함
        return true;
    }

    /// <summary>
    /// 현재 설정된 전체 BGM 볼륨을 반환함
    /// </summary>
    public float GetVolume()
    {
        // BGM 재생에 사용할 AudioSource가 없다면 0을 반환함
        if (bgmAudioSource == null) return 0.0f;

        // 현재 설정된 전체 BGM 볼륨을 반환함
        return bgmMasterVolume;
    }
}
