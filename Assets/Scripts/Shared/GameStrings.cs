public static class GameStrings
{
    // -----------------------------------------
    // const strings
    // -----------------------------------------

    // MainUI - server connection
    public const string ServerConnecting = "Connecting to Unity Server...";
    public const string ServerConnectFailed = "Server connection failed.";
    public const string ServerConnectSuccess = "Successfully connected to server.";

    // MainUI - host
    public const string RoomCreating = "Creating room...";
    public const string RoomCreateFailed = "Failed to create room.";

    // MainUI - client
    public const string RoomJoining = "Connecting to room...";
    public const string RoomJoinFailed = "Failed to join room. Please check your code.";
    public const string NoCodeInput = "Please enter a code.";

    // round states
    public const string ExtraRound = "Extra Round";
    public const string WaitingForPlayers = "Waiting for Players...";
    public const string Ready = "Ready...";
    public const string ReadyExtra = "Ready for Extra Round...";

    // game result
    public const string YouWin = "YOU WIN!";
    public const string YouLose = "YOU LOSE!";
    public const string Draw = "DRAW!";

    // notice text
    public const string NoticeWaitingOpponent = "Waiting for opponent...";
    public const string NoticeOpponentDisconnected = "Opponent has disconnected.";
    public const string NoticeRoomDisconnection = "Disconnected from room.";


    // -----------------------------------------
    // static string methods
    // -----------------------------------------

    // "Round 1", "Round 2"
    public static string Round(int n) => $"Round {n}";

    // "Join Code \n ABCDEF"
    public static string JoinCodeDisplay(string code) => $"Join Code\n{code}";

    // "Player 1 Wins!", "Player 2 Wins!"
    public static string PlayerWin(int playerNum) => $"Player {playerNum} Wins!";
}