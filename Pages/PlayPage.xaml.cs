using TicTacToeAI.Models;

namespace TicTacToeAI.Pages;

[QueryProperty(nameof(AgentName), "AgentName")]
public partial class PlayPage : ContentPage
{
    private QLearningAgent? _agent;
    private GameBoard _board = new();
    private bool _isPlayerTurn;
    private bool _gameOver;
    private int _agentWins, _playerWins, _draws;

    // Side selection: human picks X or O
    private char _humanPiece = 'O'; // default: human plays O
    private char _agentPiece = 'X'; // default: agent plays X

    private Button[] _cellButtons = null!;

    public string AgentName
    {
        set
        {
            _agent = QLearningAgent.Create(value);
            _agent.LoadQTable();
            AgentInfoLabel.Text = $"{value} (α={_agent.Alpha}, γ={_agent.Gamma})";
        }
    }

    public PlayPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _cellButtons = [Cell0, Cell1, Cell2, Cell3, Cell4, Cell5, Cell6, Cell7, Cell8];
        UpdateSidePickerUI();
        StartNewGame();
    }

    private void OnPickX(object? sender, EventArgs e)
    {
        if (_humanPiece == 'X') return;
        _humanPiece = 'X';
        _agentPiece = 'O';
        UpdateSidePickerUI();
        // Reset scores when switching sides
        _agentWins = _playerWins = _draws = 0;
        AgentWinsLabel.Text = "0";
        PlayerWinsLabel.Text = "0";
        DrawsLabel.Text = "0";
        StatusLabel.TextColor = Color.FromArgb("#eaeaea");
        StartNewGame();
    }

    private void OnPickO(object? sender, EventArgs e)
    {
        if (_humanPiece == 'O') return;
        _humanPiece = 'O';
        _agentPiece = 'X';
        UpdateSidePickerUI();
        // Reset scores when switching sides
        _agentWins = _playerWins = _draws = 0;
        AgentWinsLabel.Text = "0";
        PlayerWinsLabel.Text = "0";
        DrawsLabel.Text = "0";
        StatusLabel.TextColor = Color.FromArgb("#eaeaea");
        StartNewGame();
    }

    private void UpdateSidePickerUI()
    {
        if (_humanPiece == 'X')
        {
            PickXBorder.BackgroundColor = Color.FromArgb("#4a1a1a");
            PickXBorder.Stroke = Color.FromArgb("#e94560");
            PickOBorder.BackgroundColor = Color.FromArgb("#16213e");
            PickOBorder.Stroke = Color.FromArgb("#333333");
            SideInfoLabel.Text = "You play X (first) • Agent plays O";
        }
        else
        {
            PickOBorder.BackgroundColor = Color.FromArgb("#103040");
            PickOBorder.Stroke = Color.FromArgb("#4fc3f7");
            PickXBorder.BackgroundColor = Color.FromArgb("#16213e");
            PickXBorder.Stroke = Color.FromArgb("#333333");
            SideInfoLabel.Text = "Agent plays X (first) • You play O";
        }
    }

    private void StartNewGame()
    {
        _board = new GameBoard();
        _gameOver = false;

        // Set the agent's active Q-table to match the side it's playing
        _agent?.SetActiveSide(_agentPiece);

        // Reset board UI
        foreach (var btn in _cellButtons)
        {
            btn.Text = "";
            btn.IsEnabled = false;
            btn.BackgroundColor = Color.FromArgb("#16213e");
            btn.TextColor = Color.FromArgb("#eaeaea");
        }

        if (_humanPiece == 'X')
        {
            // Human goes first as X
            _isPlayerTurn = true;
            StatusLabel.Text = "Your turn — tap a cell";
            EnableAvailableCells();
        }
        else
        {
            // Agent goes first as X
            _isPlayerTurn = false;
            StatusLabel.Text = "AI is thinking...";
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(400), () =>
            {
                if (!_gameOver) AgentMove();
            });
        }
    }

    private void AgentMove()
    {
        if (_agent == null || _gameOver) return;

        // Agent uses its active Q-table (already set to correct side)
        int action = _agent.ChooseAction(_board, isTraining: false);

        // Place the agent's actual piece on the real board
        _board.MakeMove(action, _agentPiece);

        // Update UI for agent's move
        var btn = _cellButtons[action];
        btn.Text = _agentPiece == 'X' ? "X" : "O";
        btn.TextColor = _agentPiece == 'X'
            ? Color.FromArgb("#e94560")  // Red for X
            : Color.FromArgb("#4fc3f7"); // Blue for O
        btn.IsEnabled = false;

        // Animate the button
        AnimateCell(btn);

        // Check winner
        char winner = _board.CheckWinner();
        if (winner != 'N')
        {
            EndGame(winner);
            return;
        }

        // Player's turn
        _isPlayerTurn = true;
        StatusLabel.Text = "Your turn — tap a cell";
        EnableAvailableCells();
    }

    private async void OnCellClicked(object? sender, EventArgs e)
    {
        if (!_isPlayerTurn || _gameOver || sender is not Button btn) return;

        int index = int.Parse(btn.ClassId);
        if (_board.Cells[index] != 'E') return;

        // Player makes move with their chosen piece
        _board.MakeMove(index, _humanPiece);
        btn.Text = _humanPiece == 'X' ? "X" : "O";
        btn.TextColor = _humanPiece == 'X'
            ? Color.FromArgb("#e94560")  // Red for X
            : Color.FromArgb("#4fc3f7"); // Blue for O
        btn.IsEnabled = false;
        AnimateCell(btn);

        _isPlayerTurn = false;

        // Check winner
        char winner = _board.CheckWinner();
        if (winner != 'N')
        {
            EndGame(winner);
            return;
        }

        // Disable all cells while AI thinks
        DisableAllCells();
        StatusLabel.Text = "AI is thinking...";

        // Small delay for UX
        await Task.Delay(300);
        AgentMove();
    }

    private void EndGame(char winner)
    {
        _gameOver = true;
        DisableAllCells();

        if (winner == 'D')
        {
            _draws++;
            StatusLabel.Text = "🤝 It's a draw!";
            StatusLabel.TextColor = Color.FromArgb("#ffff44");
        }
        else if (winner == _agentPiece)
        {
            _agentWins++;
            StatusLabel.Text = $"❌ Agent wins! (played {_agentPiece})";
            StatusLabel.TextColor = Color.FromArgb("#e94560");
            HighlightWinningLine(winner);
        }
        else
        {
            _playerWins++;
            StatusLabel.Text = $"⭕ You win! 🎉 (played {_humanPiece})";
            StatusLabel.TextColor = Color.FromArgb("#4fc3f7");
            HighlightWinningLine(winner);
        }

        AgentWinsLabel.Text = _agentWins.ToString();
        PlayerWinsLabel.Text = _playerWins.ToString();
        DrawsLabel.Text = _draws.ToString();
    }

    private void OnNewGame(object? sender, EventArgs e)
    {
        StatusLabel.TextColor = Color.FromArgb("#eaeaea");
        StartNewGame();
    }

    private void EnableAvailableCells()
    {
        for (int i = 0; i < 9; i++)
        {
            _cellButtons[i].IsEnabled = _board.Cells[i] == 'E';
        }
    }

    private void DisableAllCells()
    {
        foreach (var btn in _cellButtons)
            btn.IsEnabled = false;
    }

    private void HighlightWinningLine(char winner)
    {
        foreach (var line in GameBoard.WinningLines)
        {
            if (_board.Cells[line[0]] == winner &&
                _board.Cells[line[1]] == winner &&
                _board.Cells[line[2]] == winner)
            {
                var highlightColor = winner == 'X'
                    ? Color.FromArgb("#3d1020")
                    : Color.FromArgb("#103040");

                foreach (int idx in line)
                {
                    _cellButtons[idx].BackgroundColor = highlightColor;
                }
                break;
            }
        }
    }

    private async void AnimateCell(Button btn)
    {
        await btn.ScaleToAsync(1.15, 80, Easing.CubicOut);
        await btn.ScaleToAsync(1.0, 80, Easing.CubicIn);
    }
}

