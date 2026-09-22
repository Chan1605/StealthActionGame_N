using UnityEngine;

// 상호작용하면 UI_DocumentViewer에 이미지를 띄우는 액션.
// 문서가 열려 있는 동안 isHoldingPlayer = true → PlayerInteractionRunner가 이동/카메라 잠금 유지.
public class ReadDocumentAction : InteractionAction
{
    [Header("Document")]
    [SerializeField] private Sprite[] pages;

    private bool _isReading;
    private DummyDamageable _userHealth;

    public override bool isHoldingPlayer
    {
        get
        {
            return _isReading;
        }
    }

    protected override bool CanExecute(Transform user)
    {
        if (pages == null || pages.Length == 0)
        {
            return false;
        }

        if (UI_DocumentViewer.Instance == null || UI_DocumentViewer.Instance.IsOpen)
        {
            return false;
        }

        // 시체를 들고 있는 동안에는 읽지 않는다.
        CarrySystem carry = user.GetComponent<CarrySystem>();
        if (carry != null && carry.isCarrying)
        {
            return false;
        }

        return true;
    }

    protected override void OnExecute(Transform user)
    {
        _isReading = true;

        // 읽는 도중 사망하면 창을 닫는다.
        _userHealth = user.GetComponent<DummyDamageable>();
        if (_userHealth != null)
        {
            _userHealth.OnDied += HandleUserDied;
        }

        PlayerInput input = user.GetComponent<PlayerInput>();
        UI_DocumentViewer.Instance.Open(pages, input, HandleClosed);
    }

    private void HandleUserDied()
    {
        if (UI_DocumentViewer.Instance != null)
        {
            UI_DocumentViewer.Instance.Close();
        }
    }

    private void HandleClosed()
    {
        _isReading = false;

        if (_userHealth != null)
        {
            _userHealth.OnDied -= HandleUserDied;
            _userHealth = null;
        }
    }

    private void OnDisable()
    {
        if (_isReading && UI_DocumentViewer.Instance != null)
        {
            UI_DocumentViewer.Instance.Close();
        }
    }
}
