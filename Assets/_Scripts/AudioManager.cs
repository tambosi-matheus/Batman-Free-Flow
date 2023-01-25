using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [SerializeField] AudioClip[] punches;

    private void Awake()
    {
        Instance = this;
    }

    public AudioClip GetRandomPunch() => punches[Random.Range(0, punches.Length)];
}
