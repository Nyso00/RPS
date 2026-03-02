using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    public GameObject playerButtonUI;
    // private bool inputAvailable = false;
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

    // Update is called once per frame
    // void Update()
    // {
    //     if (!inputAvailable)
    //         return;

    //     if (Input.GetKeyDown(KeyCode.A)) GameManager.Instance.player1.SetChoice(RPS.Rock);
    //     if (Input.GetKeyDown(KeyCode.S)) GameManager.Instance.player1.SetChoice(RPS.Paper);
    //     if (Input.GetKeyDown(KeyCode.D)) GameManager.Instance.player1.SetChoice(RPS.Scissors);

    //     if (Input.GetKeyDown(KeyCode.LeftArrow)) GameManager.Instance.player2.SetChoice(RPS.Rock);
    //     if (Input.GetKeyDown(KeyCode.DownArrow)) GameManager.Instance.player2.SetChoice(RPS.Paper);
    //     if (Input.GetKeyDown(KeyCode.RightArrow)) GameManager.Instance.player2.SetChoice(RPS.Scissors);
    // }

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
}
