using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Image checkMarkImage;
    [SerializeField] private float drawSpeed = 2.0f;
    [SerializeField] private int startPosition;

    private Bridge bridge;
    private int curPosition;
    private RPS currentChoice = RPS.None;
    private Coroutine checkMarkCoroutine;

    private void Start()
    {
        bridge = Bridge.Instance;
        curPosition = startPosition;
        SetPosition(curPosition);
        checkMarkImage.fillAmount = 0f;
    }

    public void ResetChoice()
    {
        currentChoice = RPS.None;
        checkMarkImage.fillAmount = 0f;
    }

    public RPS GetChoice()
    {
        return currentChoice;
    }

    public void SetChoice(RPS choice)
    {
        currentChoice = choice;
        if (choice != RPS.None)
        {
            if (checkMarkCoroutine != null)
            {
                StopCoroutine(checkMarkCoroutine);
            }
            checkMarkCoroutine = StartCoroutine(DrawCheckMark());
        }
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
        transform.position = new Vector3(bridge.GetBlockX(curPosition), transform.position.y, transform.position.z);
    }

    private IEnumerator DrawCheckMark()
    {
        checkMarkImage.fillAmount = 0f;
        while (checkMarkImage.fillAmount < 1f)
        {
            checkMarkImage.fillAmount += Time.deltaTime * drawSpeed;
            yield return null;
        }

        checkMarkImage.fillAmount = 1f;
    }
}
