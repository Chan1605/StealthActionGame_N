using System;
using UnityEngine;
using UnityEngine.UI;

// 종이 등 상호작용 시 화면 중앙에 이미지를 띄우는 뷰어.
// E(Interact)로 다음 장 / 마지막 장에서 닫기.
public class UI_DocumentViewer : MonoBehaviour
{
    public static UI_DocumentViewer Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject container;      // 어두운 배경 + 이미지 묶음
    [SerializeField] private Image documentImage;
    [SerializeField] private Text pageText;              // 선택: "1 / 3"

    [Header("Input")]
    [SerializeField] private float inputBlockTime = 0.2f; // 열자마자 닫히는 것 방지

    private Sprite[] _pages;
    private int _index;
    private float _openedTime;
    private int _closedFrame = -1;
    private PlayerInput _input;
    private Action _onClosed;

    public bool IsOpen { get; private set; }

    // 문서가 열려 있거나 이번 프레임에 닫혔으면 true (같은 E 입력으로 다른 행동이 나가지 않게)
    public static bool IsBlockingInput
    {
        get
        {
            if (Instance == null)
            {
                return false;
            }

            return Instance.IsOpen || Instance._closedFrame == Time.frameCount;
        }
    }

    private void Awake()
    {
        Instance = this;

        if (container != null)
        {
            container.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Open(Sprite[] pages, PlayerInput input, Action onClosed)
    {
        if (pages == null || pages.Length == 0 || IsOpen)
        {
            return;
        }

        _pages = pages;
        _index = 0;
        _openedTime = Time.unscaledTime;
        _onClosed = onClosed;

        _input = input;
        if (_input != null)
        {
            _input.OnInteractPressed += HandleNext;
        }

        IsOpen = true;
        container.SetActive(true);
        ShowPage();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        Unsubscribe();

        IsOpen = false;
        _closedFrame = Time.frameCount;
        container.SetActive(false);
        _pages = null;

        Action callback = _onClosed;
        _onClosed = null;
        callback?.Invoke();
    }

    private void HandleNext()
    {
        // 일시정지 메뉴 중에는 무시
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (Time.unscaledTime - _openedTime < inputBlockTime)
        {
            return;
        }

        _index++;

        if (_index >= _pages.Length)
        {
            Close();
        }
        else
        {
            ShowPage();
        }
    }

    private void ShowPage()
    {
        documentImage.sprite = _pages[_index];
        documentImage.preserveAspect = true;

        if (pageText != null)
        {
            pageText.gameObject.SetActive(_pages.Length > 1);
            pageText.text = $"{_index + 1} / {_pages.Length}";
        }
    }

    private void Unsubscribe()
    {
        if (_input != null)
        {
            _input.OnInteractPressed -= HandleNext;
        }

        _input = null;
    }
}
