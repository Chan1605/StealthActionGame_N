using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string next_scene; //디버그용
    [SerializeField] private Text percent_text;

    [Header("툴팁")]
    [SerializeField] private Text tip_text;
    [SerializeField] private Tips_Data tips_data;
    [SerializeField] private float tip_interval = 3f;

    [Header("로딩중")]
    [SerializeField] private Text loading_dots_text;
    [SerializeField] private string loading_base_text = "Loading";
    [SerializeField] private int max_dots = 7;
    [SerializeField] private float dot_interval = 0.5f;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fade_canvas_group;
    [SerializeField] private float fade_duration = 0.5f;

    [Header("디버그용")]
    [Range(0f, 1f)]
    [SerializeField] private float load_speed = 1f;

    private string[] tip_list;
    private Coroutine tip_co;
    private Coroutine dots_co;

    private void Awake()
    {
        tip_list = tips_data.Tip_List;
    }

    private void Start()
    {
        if (fade_canvas_group != null)
        {
            fade_canvas_group.alpha = 1f;
            fade_canvas_group.DOFade(0f, fade_duration);
        }

        StartCoroutine(LoadScene_co(next_scene));

        if (tip_text != null && tip_list != null && tip_list.Length > 0)
        {
            tip_co = StartCoroutine(Tooltip_co());
        }

        if (loading_dots_text != null)
        {
            dots_co = StartCoroutine(LoadDots_co());
        }
    }

    private IEnumerator LoadScene_co(string index)
    {
        AsyncOperation load_op = SceneManager.LoadSceneAsync(index);
        load_op.allowSceneActivation = false;

        float timer = 0f;
        float percentage = 0f;

        while (!load_op.isDone)
        {
            yield return null;
            timer += Time.deltaTime * load_speed;
            if (percentage >= 90)
            {
                percentage = Mathf.Lerp(percentage, 100, timer);
                if (percentage.Equals(100f))
                {
                    if (fade_canvas_group != null)
                    {
                        yield return fade_canvas_group.DOFade(1f, fade_duration).WaitForCompletion();
                    }

                    load_op.allowSceneActivation = true;
                }
            }
            else
            {
                percentage = Mathf.Lerp(percentage, load_op.progress * 100f, timer);
                if (percentage >= 90)
                {
                    timer = 0;
                }
                percent_text.text = percentage.ToString("0") + "%";
            }
        }

        if (tip_co != null) StopCoroutine(tip_co);
        if (dots_co != null) StopCoroutine(dots_co);
    }

    private IEnumerator Tooltip_co()
    {
        int previous_index = -1;
        WaitForSeconds wait = new WaitForSeconds(tip_interval);

        while (true)
        {
            int next_index;
            do
            {
                next_index = Random.Range(0, tip_list.Length);
            } while (next_index == previous_index && tip_list.Length > 1);

            previous_index = next_index;
            tip_text.text = tip_list[next_index];

            yield return wait;
        }
    }

    private IEnumerator LoadDots_co()
    {
        int dot_count = 0;
        WaitForSeconds wait = new WaitForSeconds(dot_interval);

        while (true)
        {

            string dots = "";
            for (int i = 0; i < dot_count; i++)
            {
                dots += ". ";
            }

            loading_dots_text.text = loading_base_text + "\n" + dots;
            yield return wait;
            dot_count = (dot_count + 1) % (max_dots + 1);
        }
    }
}