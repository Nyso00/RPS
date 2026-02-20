using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    bool inputAvailable = false;

    // Update is called once per frame
    void Update()
    {
        if (!inputAvailable)
            return;

        if (Input.GetKeyDown(KeyCode.A)) GameManager.Instance.player1.SetChoice(RPS.Rock);
        if (Input.GetKeyDown(KeyCode.S)) GameManager.Instance.player1.SetChoice(RPS.Paper);
        if (Input.GetKeyDown(KeyCode.D)) GameManager.Instance.player1.SetChoice(RPS.Scissors);

        if (Input.GetKeyDown(KeyCode.LeftArrow)) GameManager.Instance.player2.SetChoice(RPS.Rock);
        if (Input.GetKeyDown(KeyCode.DownArrow)) GameManager.Instance.player2.SetChoice(RPS.Paper);
        if (Input.GetKeyDown(KeyCode.RightArrow)) GameManager.Instance.player2.SetChoice(RPS.Scissors);
    }

    public void SetInputAvailable(bool available)
    {
        inputAvailable = available;
    }
}
