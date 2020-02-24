using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Make multiple audio sources start at the same time
/// </summary>
public class ISMSync : MonoBehaviour {

    /// <summary>
    /// List of audio sources to be played in sync
    /// </summary>
    public List<AudioSource> audioSourcesToSync;

    /// <summary>
    /// Time reserved to start all the audio sources
    /// </summary>
    public double syncedStartTime = 0.1;

    
    void Start ()
    {
        audioSourcesToSync = new List<AudioSource>();
        ISMReverb[] reverbs = FindObjectsOfType<ISMReverb>();
        foreach (ISMReverb reverb in reverbs)
        {
            audioSourcesToSync.Add(reverb.GetComponent<AudioSource>());
        }
        // Calculate starting time for the sources
        double startTime = AudioSettings.dspTime + syncedStartTime;
        // Schedule the sources to start from the beginning of their 
        // respective clips
        foreach (AudioSource src in audioSourcesToSync)
        {
            src.clip.LoadAudioData();
            src.timeSamples = 0;
            src.PlayScheduled(startTime);
        }
	}
}
