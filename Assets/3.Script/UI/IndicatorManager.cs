using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndicatorManager : MonoBehaviour
{
    [Header("UI ¿¬°á")]
    [SerializeField] private RectTransform container;
    [SerializeField] private GameObject Arrow_Prefab;

    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera cam;

    [SerializeField] private List<Transform> enemyTransform = new List<Transform>();
    private List<UI_DirectIndicatorArrow> DirArrows = new List<UI_DirectIndicatorArrow>();

    private void Start()
    {
        playerTransform = GameObject.FindWithTag("Player").transform;
        cam = Camera.main;
    }

    private void Update()
    {
        if (enemyTransform.Count == 0)
        {
            return;
        }

        UpdateDirIndicator();
    }

    private void UpdateDirIndicator()
    {
        for (int i = enemyTransform.Count-1; i >= 0 ; i--)
        {
            if(enemyTransform[i]==null)
            {
                if (DirArrows[i] != null)
                {
                    Destroy(DirArrows[i].gameObject);
                }
                DirArrows.RemoveAt(i);
                enemyTransform.RemoveAt(i);
                continue;
            }

            Vector3 viewportPos = cam.WorldToViewportPoint(enemyTransform[i].position);

            bool isOffScreen = false;

            if(!(viewportPos.x>0 && viewportPos.x<1)||
                !(viewportPos.y>0 && viewportPos.y<1)||
                viewportPos.z<0)
            {
                isOffScreen = true;
            }

            if(!isOffScreen)
            {
                DirArrows[i].SetActive(false);
                continue;
            }

            DirArrows[i].SetActive(true);

            if(viewportPos.z < 0)
            {
                viewportPos.x = 1 - viewportPos.x;
                viewportPos.y = 1 - viewportPos.y;
            }

            Vector2 centerPos = new Vector2(0.5f, 0.5f);
            Vector2 dir = new Vector2(viewportPos.x, viewportPos.y) - centerPos;



            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

            DirArrows[i].UpdateIndicator(angle);
        }
    }

    public void RegisterEnemy(Transform enemy)
    {
        GameObject newArrow = Instantiate(Arrow_Prefab, container);
        enemyTransform.Add(enemy);
        DirArrows.Add(newArrow.GetComponent<UI_DirectIndicatorArrow>());

        newArrow.SetActive(false);
    }

    public void UnregisterEnemy(Transform enemy)
    {
        int index = enemyTransform.IndexOf(enemy);

        if(index != -1)
        {
            if(DirArrows[index] != null)
            {
                Destroy(DirArrows[index].gameObject);
            }

            DirArrows.RemoveAt(index);
            enemyTransform.RemoveAt(index);
        }
    }

}
