using UnityEngine;

public static class GameAudioUtility
{
    public static void StopAllAudioSources()
    {
        AudioSource[] audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }
}
