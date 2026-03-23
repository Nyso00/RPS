using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VisualManager : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private Image timerGauge;
    [SerializeField] private Image player1ChoiceImage;
    [SerializeField] private Image player2ChoiceImage;

    [SerializeField] private Sprite rockSprite;
    [SerializeField] private Sprite paperSprite;
    [SerializeField] private Sprite scissorsSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {

    }

    // Update is called once per frame
    private void Update()
    {

    }
}
