using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Subtitle : MonoBehaviour
{
    [SerializeField] private Text subtitleTextUI;
    [SerializeField] private GameObject container;

    [SerializeField] private Transform playerTransform;

    private Coroutine currentRoutine;

    private void Awake()
    {
        {
            container.SetActive(false);
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.Log("플레이어 트랜스폼 미할당");
        }
    }

    public void PlaySequence(List<Narration_Data> dataList, Vector3 pos)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(Subtitle_co(dataList, pos));
    }

    private IEnumerator Subtitle_co(List<Narration_Data> dataList, Vector3 pos)
    {
        container.SetActive(true);

        foreach (Narration_Data data in dataList)
        {
            subtitleTextUI.text = data.subtitleText;

            if (!data.audioEvent.IsNull)
            {
                if (!data.isObjectSound)
                {
                    AudioManager.Instance.PlayOneShot(data.audioEvent, playerTransform.position);
                }
                else
                {
                    AudioManager.Instance.PlayOneShot(data.audioEvent, pos);
                }
            }

            yield return new WaitForSeconds(data.duration);
        }

        container.SetActive(false);
    }
}
