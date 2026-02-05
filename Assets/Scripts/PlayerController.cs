using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private int frontSign;

    private RPS currentChoice = RPS.None;
    private int score = 0;

    public void ResetChoice()
    {
        currentChoice = RPS.None;
    }

    public RPS getChoice()
    {
        return currentChoice;
    }

    public RPS GetChoice()
    {
        return currentChoice;
    }

    public void SetChoice(RPS choice)
    {
        currentChoice = choice;
    }

    public void Move(int direction)
    {
        score += direction;
    }
}
