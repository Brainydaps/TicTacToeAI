using TicTacToeAI.Models;

namespace TicTacToeAI.Players;

/// <summary>
/// Interface for all training opponents.
/// They can play as either 'X' or 'O'.
/// </summary>
public interface ITrainingPlayer
{
    string Name { get; }
    string Description { get; }

    /// <summary>
    /// Choose a move, playing as the specified piece.
    /// </summary>
    int ChooseMove(GameBoard board, char myPiece, char opponentPiece);
}

