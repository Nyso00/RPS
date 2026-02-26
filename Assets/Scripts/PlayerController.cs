using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private int frontSign;
    [SerializeField] private int startPosition;
    private Bridge bridge;
    private int curPosition;

    private RPS currentChoice = RPS.None;

    void Start()
    {
        bridge = Bridge.Instance;
        curPosition = startPosition;
        SetPosition(curPosition);
    }

    public void ResetChoice()
    {
        currentChoice = RPS.None;
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
        SetPosition(curPosition + direction);
    }

    public bool HasLost()
    {
        return bridge.IsOutOfRange(curPosition);
    }

    private void SetPosition(int position)
    {
        curPosition = position;
        transform.position = new Vector3(bridge.getBlockX(curPosition), transform.position.y, transform.position.z);
    }
}
