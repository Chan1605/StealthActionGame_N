using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private RectTransform mapRotator;
    [SerializeField] private RectTransform mapImage;
    [SerializeField] private RectTransform playerIcon;

    [SerializeField] private Image mapImageComponent;

    [Header("추적 대상")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;

    [Header("미니맵 세팅")]
    [SerializeField] private float mapScale = 10f;
    [SerializeField] private List<FloorData> floorList;

    [Header("아이콘 트래킹")]
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private List<Transform> enemyTransformList;
    [SerializeField] private List<UI_MinimapIcon> enemyIcons;

    [SerializeField] private GameObject enemyIcon_Prefab;
    [SerializeField] private GameObject targetIcon_Prefab;

    private int currentFloorIndex = -1;
    private Vector3 pastPlayerTransform;

    [SerializeField] UI_MinimapIcon targetIcon;
    private Transform currentObjectTransform;

    private void Awake()
    {
        targetIcon = Instantiate(targetIcon_Prefab, iconContainer).GetComponent<UI_MinimapIcon>();
    }
    private void Start()
    {
        playerTransform =  GameObject.FindWithTag("Player").transform;
        cameraTransform = Camera.main.transform;

        Transform map = transform.GetChild(1).GetChild(0);

        mapRotator = map.parent.GetComponent<RectTransform>();
        mapImage = map.GetComponent<RectTransform>();
        mapImageComponent = map.GetComponent<Image>();

        playerIcon = transform.GetChild(3).GetComponent<RectTransform>();
        targetIcon.gameObject.SetActive(false);

        UpdateFloor();
    }


    private void Update()
    {
        if (playerTransform == null || cameraTransform == null)
        {
            return;
        }

        if (playerTransform.position.y != pastPlayerTransform.y)
        {
            UpdateFloor();
        }

        UpdatePlayerIcon();
        UpdateMapTransform();
        UpdateEnemyIcons();
        UpdateTargetIcon();

        pastPlayerTransform = playerTransform.position;


    }

    private void UpdateFloor()
    {
        float playerY = playerTransform.position.y;
        int nowFloorIndex = currentFloorIndex;

        for (int i = 0; i < floorList.Count; i++)
        {
            if (floorList[i].minHeight <= playerY)
            {
                nowFloorIndex = i;
                continue;
            }
            else
            {
                break;
            }
        }

        if(currentFloorIndex != nowFloorIndex)
        {
            mapImageComponent.sprite = floorList[nowFloorIndex].mapSprite;
            currentFloorIndex = nowFloorIndex;
        }

    }

    private void UpdateMapTransform()
    {
        float cameraY = cameraTransform.eulerAngles.y;
        mapRotator.localEulerAngles = new Vector3 (mapRotator.localEulerAngles.x, mapRotator.localEulerAngles.y, cameraY);

        if (playerTransform.position.x != pastPlayerTransform.x || playerTransform.position.z != pastPlayerTransform.z)
        {
            mapImage.anchoredPosition = new Vector2(-playerTransform.position.x*mapScale, -playerTransform.position.z*mapScale);
        }
    }

    private void UpdatePlayerIcon()
    {
        float rotationDiff = playerTransform.localEulerAngles.y - cameraTransform.localEulerAngles.y;

        playerIcon.localEulerAngles = 
            new Vector3(
                0, 
                0, 
                -rotationDiff);
    }

    private void UpdateEnemyIcons()
    {
        if (enemyTransformList.Count == 0 || enemyIcons ==null)
        {
            return;
        }

        for (int i = enemyTransformList.Count -1; i >= 0; i--)
        {
            if (enemyTransformList[i] == null)
            {
                if (enemyIcons[i] != null)
                {
                    Destroy(enemyIcons[i].gameObject);
                }
                enemyIcons.RemoveAt(i);
                enemyTransformList.RemoveAt(i);
                continue;
            }

            Vector2 mapPos = GetClampedUIPosition(enemyTransformList[i].position);
            enemyIcons[i].UpdateIcon(mapPos, playerTransform.position.y, enemyTransformList[i].position.y);
        }
    }

    private Vector2 GetClampedUIPosition(Vector3 targetWorldPos)
    {
        Vector3 offset = targetWorldPos - playerTransform.position;

        Vector2 uiPos = new Vector2(offset.x, offset.z) * mapScale;

        float angleRad = cameraTransform.eulerAngles.y * Mathf.Deg2Rad;
        float s = Mathf.Sin(angleRad);
        float c = Mathf.Cos(angleRad);

        float rotatedX = uiPos.x * c - uiPos.y * s;
        float rotatedY = uiPos.x * s + uiPos.y * c;
        Vector2 finalPos = new Vector2(rotatedX, rotatedY);

        float halfWidth = iconContainer.rect.width * 0.5f;
        float halfHeight = iconContainer.rect.height * 0.5f;

        finalPos.x = Mathf.Clamp(finalPos.x, -halfWidth, halfWidth);
        finalPos.y = Mathf.Clamp(finalPos.y, -halfHeight, halfHeight);

        return finalPos;
    }

    public void RegisterEnemy(Transform enemyTransform)
    {
        GameObject newIcon = Instantiate(enemyIcon_Prefab, iconContainer);
        enemyTransformList.Add(enemyTransform);
        enemyIcons.Add(newIcon.GetComponent<UI_MinimapIcon>());
    }

    public void SetObjectTarget(Transform targetTransform)
    {
        currentObjectTransform = targetTransform;

        if(currentObjectTransform == null)
        {
            targetIcon.gameObject.SetActive(false);
        }
        else
        {
            targetIcon.gameObject.SetActive(true);
        }
    }

    private void UpdateTargetIcon()
    {
        if(currentObjectTransform == null || targetIcon == null)
        {
            return;
        }

        Vector2 mapPos = GetClampedUIPosition(currentObjectTransform.position);
        targetIcon.UpdateIcon(mapPos, playerTransform.position.y, currentObjectTransform.position.y);
    }

    public void UnregisterEnemy(Transform enemyTransform)
    {
        int index = enemyTransformList.IndexOf(enemyTransform);

        if (index != -1)
        {
            if(enemyIcons[index] != null)
            {
                Destroy(enemyIcons[index].gameObject);
            }

            enemyIcons.RemoveAt(index);
            enemyTransformList.RemoveAt(index);
        }
    }
}
