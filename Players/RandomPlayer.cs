using TicTacToeAI.Models;

namespace TicTacToeAI.Players;

/// <summary>
/// Player 1: Plays completely randomly.
/// Used in the first stage of training (iterations 1-2000).
/// </summary>
public class RandomPlayer : ITrainingPlayer
{
    private readonly Random _random = new();

    public string Name => "Random Player";
    public string Description => "Plays randomly in any available position";

    public int ChooseMove(GameBoard board, char myPiece, char opponentPiece)
    {
        var available = board.GetAvailableMoves();
        if (available.Count == 0)
            throw new InvalidOperationException("No available moves");
        return available[_random.Next(available.Count)];
    }
}

