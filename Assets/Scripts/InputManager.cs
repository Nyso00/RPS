using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    [SerializeField] private GameObject playerButtonUI;
    private GameControls controls;

    protected override void Awake()
    {
        base.Awake();

        controls = new GameControls();
        controls.Player1.Rock.performed += ctx => GameManager.Instance.player1.SetChoice(RPS.Rock);
        controls.Player1.Paper.performed += ctx => GameManager.Instance.player1.SetChoice(RPS.Paper);
        controls.Player1.Scissors.performed += ctx => GameManager.Instance.player1.SetChoice(RPS.Scissors);

        controls.Player2.Rock.performed += ctx => GameManager.Instance.player2.SetChoice(RPS.Rock);
        controls.Player2.Paper.performed += ctx => GameManager.Instance.player2.SetChoice(RPS.Paper);
        controls.Player2.Scissors.performed += ctx => GameManager.Instance.player2.SetChoice(RPS.Scissors);
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

    protected override void OnDestroy()
    {
        controls?.Dispose();
        base.OnDestroy();
    }
}
