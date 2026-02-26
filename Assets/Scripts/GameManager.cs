using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public enum RPS { None, Rock, Paper, Scissors }

public class GameManager : Singleton<GameManager>
{
    public PlayerController player1;
    public PlayerController player2;

    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private Image timerGauge;
    [SerializeField] private Image player1ChoiceImage;
    [SerializeField] private Image player2ChoiceImage;

    public Sprite rockSprite;
    public Sprite paperSprite;
    public Sprite scissorsSprite;


    [Header("시간 변수")]
    public float startDelay = 1.0f;
    public float roundDuration = 3.0f;
    public float revealToMoveDelay = 0.5f;
    public float moveToNextRoundDelay = 0.5f;
    public float extraRoundDuration = 5.0f;
    public float extraRoundDurationDecrement = 0.9f;

    [Header("라운드 변수")]
    public int maxRounds = 30;
    public int maxExtraRounds = 10;
    public int[] blockDestroyRounds = new int[4];

    private int currentRound = 0;
    private int currentDestroyPhase = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(PlayRounds());
    }

    IEnumerator PlayRounds()
    {
        yield return new WaitForSeconds(startDelay); // Initial delay before starting rounds

        while (currentRound < maxRounds)
        {
            currentRound++;

            player1.ResetChoice();
            player2.ResetChoice();

            InputManager.Instance.SetInputAvailable(true);
            roundText.text = $"Round {currentRound}";

            bool destroyBlock = currentDestroyPhase < blockDestroyRounds.Length && currentRound == blockDestroyRounds[currentDestroyPhase];
            if (destroyBlock)
            {
                Bridge.Instance.BeforeDestroyBlock();
            }

            float passedTime = 0f;
            while (passedTime < roundDuration)
            {
                passedTime += Time.deltaTime;

                timerGauge.fillAmount = passedTime / roundDuration;

                yield return null;
            }
            timerGauge.fillAmount = 1f;

            InputManager.Instance.SetInputAvailable(false);

            int winner = FindWinner();

            yield return new WaitForSeconds(revealToMoveDelay); // Short delay before moving players

            if (winner == 1)
            {
                player1.Move(1);
                player2.Move(1);
            }
            else if (winner == 2)
            {
                player1.Move(-1);
                player2.Move(-1);
            }

            if (destroyBlock)
            {
                Bridge.Instance.DestroyBlock();
                currentDestroyPhase++;
            }

            if (CheckGameOver())
            {
                yield break; // Exit the coroutine if the game is over
            }

            yield return new WaitForSeconds(moveToNextRoundDelay); // Short delay between rounds

            player1ChoiceImage.gameObject.SetActive(false);
            player2ChoiceImage.gameObject.SetActive(false);
        }
    }

    private int FindWinner()
    {
        RPS p1Choice = player1.GetChoice();
        RPS p2Choice = player2.GetChoice();

        if (p1Choice != RPS.None)
        {
            player1ChoiceImage.sprite = GetChoiceSprite(p1Choice);
            player1ChoiceImage.gameObject.SetActive(true);
        }
        if (p2Choice != RPS.None)
        {
            player2ChoiceImage.sprite = GetChoiceSprite(p2Choice);
            player2ChoiceImage.gameObject.SetActive(true);
        }

        if (p1Choice == p2Choice)
        {
            Debug.Log($"It's a tie! Both players chose {p1Choice}");
            return 0;
        }
        else if ((p1Choice == RPS.Rock && p2Choice == RPS.Scissors) ||
                 (p1Choice == RPS.Paper && p2Choice == RPS.Rock) ||
                 (p1Choice == RPS.Scissors && p2Choice == RPS.Paper) ||
                 (p1Choice != RPS.None && p2Choice == RPS.None))
        {
            Debug.Log($"Player 1 wins the round! ({p1Choice} beats {p2Choice})");
            return 1;
        }
        else
        {
            Debug.Log($"Player 2 wins the round! ({p2Choice} beats {p1Choice})");
            return 2;
        }
    }

    private bool CheckGameOver()
    {
        if (player1.HasLost())
        {
            Debug.Log("Player 2 Wins!");
            return true;
        }
        else if (player2.HasLost())
        {
            Debug.Log("Player 1 Wins!");
            return true;
        }
        return false;
    }

    private Sprite GetChoiceSprite(RPS choice)
    {
        switch (choice)
        {
            case RPS.Rock:
                return rockSprite;
            case RPS.Paper:
                return paperSprite;
            case RPS.Scissors:
                return scissorsSprite;
            default:
                return null;
        }
    }

}
