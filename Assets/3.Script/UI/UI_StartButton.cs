using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_StartButton : MonoBehaviour
{
    public void StartLoading()
    {
        GameManager.Instance.LoadWithLoadingScreen("ProtoCapy_Enemy");
    }
}
