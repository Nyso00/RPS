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
    public float extraRoundStartDelay = 1.0f;

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

    private IEnumerator PlayRounds()
    {
        yield return new WaitForSeconds(startDelay); // Initial delay before starting rounds

        while (currentRound < maxRounds + maxExtraRounds)
        {
            timerGauge.fillAmount = 0f;
            currentRound++;

            player1.ResetChoice();
            player2.ResetChoice();

            if (currentRound == maxRounds + 1)
            {
                roundText.text = "Ready for Extra Round...";
                roundDuration = extraRoundDuration;
                yield return new WaitForSeconds(extraRoundStartDelay);
                roundText.text = "Extra Round!";
            }
            else if (IsExtraRound())
            {
                roundDuration *= extraRoundDurationDecrement;
            }
            else
            {
                roundText.text = $"Round {currentRound}";
            }

            bool destroyBlock = currentDestroyPhase < blockDestroyRounds.Length && currentRound == blockDestroyRounds[currentDestroyPhase];
            if (destroyBlock)
            {
                Bridge.Instance.BeforeDestroyBlock();
            }

            InputManager.Instance.SetInputAvailable(true);

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
                CameraController.Instance.MoveCamera();
            }
            else if (winner == 2)
            {
                player1.Move(-1);
                player2.Move(-1);
                CameraController.Instance.MoveCamera();
            }

            if (CheckGameOver())
            {
                player1ChoiceImage.gameObject.SetActive(false);
                player2ChoiceImage.gameObject.SetActive(false);
                yield break; // Exit the coroutine if the game is over
            }

            if (destroyBlock)
            {
                Bridge.Instance.DestroyBlock();
                currentDestroyPhase++;
                if (CheckGameOver())
                {
                    player1ChoiceImage.gameObject.SetActive(false);
                    player2ChoiceImage.gameObject.SetActive(false);
                    yield break; // Exit the coroutine if the game is over
                }
            }

            yield return new WaitForSeconds(moveToNextRoundDelay); // Short delay between rounds

            player1ChoiceImage.gameObject.SetActive(false);
            player2ChoiceImage.gameObject.SetActive(false);
        }

        roundText.text = "Draw!";
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
            roundText.text = "Player 2 Wins!";
            return true;
        }
        else if (player2.HasLost())
        {
            Debug.Log("Player 1 Wins!");
            roundText.text = "Player 1 Wins!";
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

    private bool IsExtraRound()
    {
        return currentRound > maxRounds;
    }

}
