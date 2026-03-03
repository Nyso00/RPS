using Unity.VisualScripting;
using UnityEngine;

public class NetworkInputManager : NetworkSingleton<NetworkInputManager>
{
    [SerializeField] private GameObject playerButtonUI;
    private GameControls controls;

    protected override void Awake()
    {
        base.Awake();

        controls = new GameControls();
        controls.Player1.Rock.performed += ctx => NetworkGameManager.Instance.player1.SetChoice(RPS.Rock);
        controls.Player1.Paper.performed += ctx => NetworkGameManager.Instance.player1.SetChoice(RPS.Paper);
        controls.Player1.Scissors.performed += ctx => NetworkGameManager.Instance.player1.SetChoice(RPS.Scissors);
    }

    public void SetInputAvailable(bool available)
    {
        if (available)
        {
            controls.Enable();
        }
        else
        {
            controls.Disable();
        }
        playerButtonUI.SetActive(available);
    }

    public override void OnDestroy()
    {
        controls?.Dispose();
        base.OnDestroy();
    }
}
