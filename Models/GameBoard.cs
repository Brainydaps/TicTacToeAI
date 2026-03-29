namespace TicTacToeAI.Models;

/// <summary>
/// Represents a Tic-Tac-Toe board with 9 cells.
/// Cells are: 'X', 'O', or 'E' (empty).
/// Board layout:
///   0 | 1 | 2
///   ---------
///   3 | 4 | 5
///   ---------
///   6 | 7 | 8
/// </summary>
public class GameBoard
{
    public char[] Cells { get; private set; }

    public static readonly int[][] WinningLines =
    [
        [0, 1, 2], // top row
        [3, 4, 5], // middle row
        [6, 7, 8], // bottom row
        [0, 3, 6], // left column
        [1, 4, 7], // middle column
        [2, 5, 8], // right column
        [0, 4, 8], // diagonal
        [2, 4, 6]  // anti-diagonal
    ];

    public static readonly int[] Corners = [0, 2, 6, 8];
    public static readonly int Center = 4;
    public static readonly int[] Edges = [1, 3, 5, 7];

    public GameBoard()
    {
        Cells = ['E', 'E', 'E', 'E', 'E', 'E', 'E', 'E', 'E'];
    }

    public GameBoard(char[] cells)
    {
        Cells = (char[])cells.Clone();
    }

    public GameBoard Clone()
    {
        return new GameBoard(Cells);
    }

    /// <summary>
    /// Returns the state string, e.g., "EXOEXOXEE"
    /// </summary>
    public string GetStateString()
    {
        return new string(Cells);
    }

    public List<int> GetAvailableMoves()
    {
        var moves = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            if (Cells[i] == 'E')
                moves.Add(i);
        }
        return moves;
    }

    public void MakeMove(int position, char player)
    {
        if (position < 0 || position > 8)
            throw new ArgumentOutOfRangeException(nameof(position));
        if (Cells[position] != 'E')
            throw new InvalidOperationException($"Position {position} is already occupied by {Cells[position]}");
        if (player != 'X' && player != 'O')
            throw new ArgumentException("Player must be 'X' or 'O'", nameof(player));

        Cells[position] = player;
    }

    /// <summary>
    /// Returns 'X', 'O', 'D' (draw), or 'N' (no winner yet).
    /// </summary>
    public char CheckWinner()
    {
        foreach (var line in WinningLines)
        {
            char a = Cells[line[0]], b = Cells[line[1]], c = Cells[line[2]];
            if (a != 'E' && a == b && a == c)
                return a;
        }

        // Check for draw (no empty cells)
        if (!Cells.Contains('E'))
            return 'D';

        return 'N'; // game not over
    }

    /// <summary>
    /// Check if a specific player has won.
    /// </summary>
    public bool HasPlayerWon(char player)
    {
        return CheckWinner() == player;
    }

    /// <summary>
    /// Returns the count of a given cell type.
    /// </summary>
    public int CountOf(char cellType)
    {
        return Cells.Count(c => c == cellType);
    }

    /// <summary>
    /// Check if a move at the given position for the given player would win.
    /// </summary>
    public bool WouldWin(int position, char player)
    {
        if (Cells[position] != 'E') return false;
        var clone = Clone();
        clone.MakeMove(position, player);
        return clone.HasPlayerWon(player);
    }

    /// <summary>
    /// Returns positions where the given player can win in one move.
    /// </summary>
    public List<int> GetWinningMoves(char player)
    {
        return GetAvailableMoves().Where(m => WouldWin(m, player)).ToList();
    }

    /// <summary>
    /// Check if a move contributes to a potential winning line (line with own pieces and empty, no opponent).
    /// </summary>
    public bool MoveContributesToWinnableLine(int position, char player)
    {
        char opponent = player == 'X' ? 'O' : 'X';
        foreach (var line in WinningLines)
        {
            if (!line.Contains(position)) continue;

            // Check if this line has no opponent pieces
            bool hasOpponent = false;
            foreach (int cell in line)
            {
                if (Cells[cell] == opponent)
                {
                    hasOpponent = true;
                    break;
                }
            }

            if (!hasOpponent)
            {
                // Count own pieces in this line (including the new move)
                int ownCount = 0;
                int emptyCount = 0;
                foreach (int cell in line)
                {
                    if (cell == position || Cells[cell] == player)
                        ownCount++;
                    else if (Cells[cell] == 'E')
                        emptyCount++;
                }

                // The move contributes to a winnable line if there are own pieces and empty cells
                if (ownCount >= 2 && emptyCount >= 0)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if a move is along a line that contains opponent pieces.
    /// </summary>
    public bool MoveIsOnOpponentLine(int position, char player)
    {
        char opponent = player == 'X' ? 'O' : 'X';
        foreach (var line in WinningLines)
        {
            if (!line.Contains(position)) continue;
            foreach (int cell in line)
            {
                if (Cells[cell] == opponent)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if a move creates a potential win next move (2 in a line with 1 empty).
    /// This checks AFTER the move is made.
    /// </summary>
    public bool MoveCreatesWinThreat(int position, char player)
    {
        // Simulate the move
        var clone = Clone();
        clone.MakeMove(position, player);

        // Check if any line now has 2 of player's pieces and 1 empty
        foreach (var line in WinningLines)
        {
            if (!line.Contains(position)) continue;

            int playerCount = 0;
            int emptyCount = 0;
            foreach (int cell in line)
            {
                if (clone.Cells[cell] == player) playerCount++;
                else if (clone.Cells[cell] == 'E') emptyCount++;
            }

            if (playerCount == 2 && emptyCount == 1)
                return true;
        }
        return false;
    }

    public override string ToString()
    {
        return $"{Cells[0]}|{Cells[1]}|{Cells[2]}\n" +
               $"{Cells[3]}|{Cells[4]}|{Cells[5]}\n" +
               $"{Cells[6]}|{Cells[7]}|{Cells[8]}";
    }
}

