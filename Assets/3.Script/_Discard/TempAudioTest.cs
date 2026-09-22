using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class TempAudioTest : MonoBehaviour
{

    [SerializeField] EventReference hitSound;

    private void OnTriggerEnter(Collider other)
    {
        AudioManager.Instance.PlayOneShot(hitSound, transform.position);
    }
}
