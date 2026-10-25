using UnityEngine;
using UnityEngine.Audio;

public class AudioChanger : MonoBehaviour
{
    [Tooltip("The audio mixer snapshot that this component can change the audio tracks to.")]
    public AudioMixerSnapshot audioMixerSnapshot;
    [Tooltip("The time in seconds the audio mixer will take to fade to the new snapshot.")]
    public float snapshotTransitionTime = 1f;

    public void SetAudioMixerSnapshot()
    {
        audioMixerSnapshot.TransitionTo(snapshotTransitionTime);
    }
}
