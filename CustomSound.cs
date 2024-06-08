using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace OVERKILL;

public class CustomSound
{
    public static Dictionary <string, AudioClip> cache = new Dictionary <string, AudioClip>();
    
    public static async Task<AudioClip> LoadAsync(string name)
    {
        if (cache.TryGetValue(name, out var cachedClip))
            return cachedClip;
        
        var assPath = Path.GetDirectoryName(typeof(CustomSound).Assembly.Location);
        OK.Log(assPath);

        var soundsDir = Path.Combine(assPath, "Sounds");
        var soundFilePath = Path.Combine(soundsDir, name);
        
        OK.Log(soundFilePath);

        if (!File.Exists(soundFilePath))
            return null;

        var asyncSrc = new System.Threading.Tasks.TaskCompletionSource <AudioClip>();
        NewMovement.Instance.StartCoroutine(LoadCoroutine(soundFilePath, asyncSrc));

        var clip = await asyncSrc.Task;
        cache.Add(name, clip);

        return clip;
    }

    private static IEnumerator LoadCoroutine(string soundFilePath, TaskCompletionSource <AudioClip> asyncSrc)
    {
        using (WWW www = new WWW("file:\\\\" + soundFilePath))
        {
            yield return www;
            asyncSrc.SetResult(www.GetAudioClip());
        }
    }
}
