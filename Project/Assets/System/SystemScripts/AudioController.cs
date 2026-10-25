using UnityEngine;
using UnityEngine.Audio;

public class AudioController : MonoBehaviour
{
    public static AudioController instance;
    public AudioMixer mixer;
    public AudioMixerSnapshot defaultSnapshot;
    public float snapshotTransitionTime = 1f;

    public void Awake()
    {
        if (instance != null)
        {
            Debug.Log("WARNING: More than one AudioController detected!");
        }
        instance = this;
    }

    public static AudioMixer GetMixer()
    {
        return instance.mixer;
    }

    public void SetAudioMixerSnapshot(AudioMixerSnapshot aSnapshot)
    {
        aSnapshot.TransitionTo(snapshotTransitionTime);
    }

    public void ResetSnapshot(float time)
    {
        instance.defaultSnapshot.TransitionTo(time);
    }

    public void SetSnapshotTransitionTime(float time)
    {
        snapshotTransitionTime = time;
    }
}
