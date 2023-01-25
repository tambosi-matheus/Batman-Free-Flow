using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class Util
{    
    public static async Task UMove(this Transform t, Vector3 target, float duration)
    {
        var dist = (t.position - target).magnitude;
        var mDuration = (int)(duration * 1000);
        float  time = 0;
        while(time < duration)
        {
            Debug.Log("Im here!");
            t.position = Vector3.Lerp(t.position, t.position, dist / mDuration);
            time += Time.deltaTime;
            await Task.Yield();
        }
    }
}
