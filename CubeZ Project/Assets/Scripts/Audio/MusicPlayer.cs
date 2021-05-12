﻿using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour, IInvokerMono
    {
    [SerializeField] private AudioClip[] musicList;

    [SerializeField] private AudioClip[] musicListCached;

    private AudioSource audioSource;

    private AudioDataManager audioManager;

    private bool lerping = false;

    private AudioClip lastAudioClip;

    private AudioClip selectedTrack;
        // Use this for initialization
        void Start()
        {
        if (musicList.Length == 0)
        {
            throw new MusicPlayerException("music list is emtry");
        }
        if (AudioDataManager.Manager == null)
        {
            throw new MusicPlayerException("audio manager not found");
        }
        if (!TryGetComponent(out audioSource))
        {
            throw new MusicPlayerException("music list is emtry");
        }


        audioManager = AudioDataManager.Manager;

        audioManager.onFXVolumeChanged += ChangeVolume;
        audioManager.onMusicEnabled += SetStatusMusic;

        musicListCached = GetClipsWithArrayClips(musicListCached, musicList);
        StartCoroutine(WaitNewTrack());



    }

    private void SetStatusMusic(bool enabled)
    {
        if (enabled)
        {
            if (!audioSource.isPlaying)
            {
                NewTrack();
            }
        }

        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    private void NewTrack()
    {

        if (musicList.Length == 0)
        {
            selectedTrack = musicList[0];
        }
        else
        {
            SelectRansomTrack();

        }

        if (!audioManager.GetMusicEnabled())
        {
            return;
        }

        StartCoroutine(LerpingVolume());
        PlayTrack(selectedTrack);
    }

    private void SelectRansomTrack()
    {
        AudioClip[] tracks = musicList.Where(track => track != lastAudioClip).ToArray();
        selectedTrack = tracks[Random.Range(0, tracks.Length)];
    }

    public void CallInvokingEveryMethod(Action method, float time)
    {
        InvokeRepeating(method.Method.Name, time, time);
    }

    public void CallInvokingMethod(Action method, float time)
    {
        Invoke(method.Method.Name, time);
    }

    private void ChangeVolume (float value)
    {
        if (!lerping)
        {
            audioSource.volume = value;
        }
    }

    private IEnumerator LerpingVolume()
    {
        lerping = true;
        float lerpValue = 0;
        while (true)
        {
            float fpsRate = 1.0f / 60.0f;

            yield return new WaitForSeconds(fpsRate);
            lerpValue += fpsRate;
            audioSource.volume = Mathf.Lerp(0, audioManager.GetVolumeMusic(), lerpValue);

            if (lerpValue >= 1)
            {
                lerping = false;
                yield break;
            }
        }
    }

    private IEnumerator WaitNewTrack ()
    {
        NewTrack();


        while (true)
        {
        yield return new WaitForSecondsRealtime(selectedTrack.length + 0.1f * Time.timeScale);
            if (audioManager.GetMusicEnabled())
            {
             NewTrack();
            PlayTrack(selectedTrack);
            }

        }

        
    }

    private void PlayTrack (AudioClip track)
    {


        audioSource.clip = track;
        lastAudioClip = track;
        audioSource.Play();
    }

    public void ReplaceTrack (AudioClip track)
    {
        if (track == null)
        {
            throw new MusicPlayerException("track is null");
        }

        audioSource.Stop();
        CancelInvoke();
        StartCoroutine(LerpingVolume());


        musicList = new AudioClip[1];

        musicList[0] = track;
        if (audioManager.GetMusicEnabled())
        {
        PlayTrack(track);
        }


    }

    public void ReturnOriginalListMusic ()
    {

        CancelInvoke();


        musicList = GetClipsWithArrayClips(musicList, musicListCached);


        audioSource.Stop();
        NewTrack();
    }

    private AudioClip[] GetClipsWithArrayClips (AudioClip[] arrayTarget, AudioClip[] arrayGet)
    {
        arrayTarget = new AudioClip[arrayGet.Length];

        for (int i = 0; i < arrayGet.Length; i++)
        {
           arrayTarget[i] = arrayGet[i];
        }

        return arrayTarget;
    }

    public AudioClip[] GetActiveTrackList ()
    {
        AudioClip[] clips = new AudioClip[musicList.Length];
        musicList.CopyTo(clips, 0);
        return clips;
    }

    public void SetTrackList (AudioClip[] tracks)
    {
        musicList = GetClipsWithArrayClips(musicList, tracks);
        NewTrack();
    }


}