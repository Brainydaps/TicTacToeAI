using TicTacToeAI.Models;

namespace TicTacToeAI.Services;

/// <summary>
/// Generates all valid board states and initializes Q-values.
/// Valid states: X count greater than or equal to O count, X count at most O count plus 1,
/// no winner unless it was the last move.
/// Q-values: -1.0 for occupied cells, 0.0 for empty cells.
/// </summary>
public class QTableGenerator
{
    /// <summary>
    /// Generate a complete Q-table with all valid board states where it's the agent's turn.
    /// When agentPiece is 'X': generates states where X count == O count (X's turn).
    /// When agentPiece is 'O': generates states where X count == O count + 1 (O's turn).
    /// </summary>
    public Dictionary<string, Dictionary<int, double>> GenerateQTable(char agentPiece = 'X')
    {
        var qTable = new Dictionary<string, Dictionary<int, double>>();
        var board = new char[9];
        for (int i = 0; i < 9; i++) board[i] = 'E';

        GenerateStatesRecursive(board, qTable, agentPiece);
        return qTable;
    }

    private void GenerateStatesRecursive(char[] board, Dictionary<string, Dictionary<int, double>> qTable, char agentPiece)
    {
        int xCount = board.Count(c => c == 'X');
        int oCount = board.Count(c => c == 'O');

        // Validate: X count must be >= O count and <= O count + 1
        if (xCount > oCount + 1) return;
        if (xCount < oCount) return;

        // Check if game is already over (someone won)
        var gb = new GameBoard(board);
        char winner = gb.CheckWinner();
        if (winner == 'X' || winner == 'O' || winner == 'D')
            return; // Terminal state, don't add to Q-table

        // Add states where it's the agent's turn
        bool isAgentsTurn = agentPiece == 'X' ? (xCount == oCount) : (xCount == oCount + 1);

        if (isAgentsTurn)
        {
            string state = new string(board);
            if (!qTable.ContainsKey(state))
            {
                var actions = new Dictionary<int, double>();
                for (int i = 0; i < 9; i++)
                {
                    actions[i] = board[i] == 'E' ? 0.0 : -1.0;
                }
                qTable[state] = actions;
            }
        }

        // Continue generating states by placing the next piece
        char nextPiece = xCount == oCount ? 'X' : 'O';

        for (int i = 0; i < 9; i++)
        {
            if (board[i] == 'E')
            {
                board[i] = nextPiece;
                GenerateStatesRecursive(board, qTable, agentPiece);
                board[i] = 'E'; // backtrack
            }
        }
    }
}

