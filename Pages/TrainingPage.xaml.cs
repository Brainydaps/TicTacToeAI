using TicTacToeAI.Models;
using TicTacToeAI.Services;

namespace TicTacToeAI.Pages;

[QueryProperty(nameof(AgentName), "AgentName")]
public partial class TrainingPage : ContentPage
{
    private readonly TrainingService _trainingService;
    private QLearningAgent? _agent;
    private CancellationTokenSource? _cts;
    private int _totalWins, _totalLosses, _totalDraws;
    private TrainingMode _selectedMode = TrainingMode.AsX;
    private LearningGraphDrawable _graphDrawable = new();

    // Track cumulative points for graph — separate from per-phase W/L/D
    private int _graphTotalGames;
    private double _graphTotalPoints;
    // Track last reported iteration per phase to calculate per-game results from deltas
    private int _lastPhaseWins, _lastPhaseLosses, _lastPhaseDraws;

    public string AgentName
    {
        set
        {
            _agent = QLearningAgent.Create(value);
            _agent.LoadQTable();
            AgentNameLabel.Text = value;
            UpdateAgentParamsLabel();
        }
    }

    public TrainingPage(TrainingService trainingService)
    {
        InitializeComponent();
        _trainingService = trainingService;
        LearningGraphView.Drawable = _graphDrawable;
    }

    private void UpdateAgentParamsLabel()
    {
        if (_agent == null) return;
        AgentParamsLabel.Text = $"α = {_agent.Alpha}  •  γ = {_agent.Gamma}  •  X Q-Table: {_agent.QTableX.Count}  •  O Q-Table: {_agent.QTableO.Count}";
    }

    private void OnIterationsChanged(object? sender, ValueChangedEventArgs e)
    {
        int value = ((int)(e.NewValue / 100)) * 100; // Round to nearest 100
        if (value < 100) value = 100;
        IterationsSlider.Value = value;
        IterationsLabel.Text = value.ToString();
    }

    private void OnModeX(object? sender, EventArgs e)
    {
        _selectedMode = TrainingMode.AsX;
        UpdateModeUI();
    }

    private void OnModeO(object? sender, EventArgs e)
    {
        _selectedMode = TrainingMode.AsO;
        UpdateModeUI();
    }

    private void OnModeBoth(object? sender, EventArgs e)
    {
        _selectedMode = TrainingMode.Both;
        UpdateModeUI();
    }

    private void UpdateModeUI()
    {
        // Reset all borders
        ModeXBorder.BackgroundColor = Color.FromArgb("#16213e");
        ModeXBorder.Stroke = Color.FromArgb("#333333");
        ModeOBorder.BackgroundColor = Color.FromArgb("#16213e");
        ModeOBorder.Stroke = Color.FromArgb("#333333");
        ModeBothBorder.BackgroundColor = Color.FromArgb("#16213e");
        ModeBothBorder.Stroke = Color.FromArgb("#333333");

        switch (_selectedMode)
        {
            case TrainingMode.AsX:
                ModeXBorder.BackgroundColor = Color.FromArgb("#4a1a1a");
                ModeXBorder.Stroke = Color.FromArgb("#e94560");
                ModeDescriptionLabel.Text = "Agent plays first as X — original training";
                break;
            case TrainingMode.AsO:
                ModeOBorder.BackgroundColor = Color.FromArgb("#103040");
                ModeOBorder.Stroke = Color.FromArgb("#4fc3f7");
                ModeDescriptionLabel.Text = "Agent plays second as O — learns defensive play";
                break;
            case TrainingMode.Both:
                ModeBothBorder.BackgroundColor = Color.FromArgb("#3a3a1a");
                ModeBothBorder.Stroke = Color.FromArgb("#ffcc44");
                ModeDescriptionLabel.Text = "Train as X then O — learns both perspectives (2× games)";
                break;
        }
    }

    private async void OnStartTraining(object? sender, EventArgs e)
    {
        if (_agent == null) return;

        _cts = new CancellationTokenSource();
        _totalWins = _totalLosses = _totalDraws = 0;
        _graphTotalGames = 0;
        _graphTotalPoints = 0;
        _lastPhaseWins = _lastPhaseLosses = _lastPhaseDraws = 0;

        // Reset graph
        _graphDrawable.Clear();

        // UI state: training started
        StartTrainingButton.IsEnabled = false;
        StartTrainingButton.Text = "⏳ Training...";
        CancelButton.IsVisible = true;
        ProgressFrame.IsVisible = true;
        GraphFrame.IsVisible = true;
        CompletionFrame.IsVisible = false;
        LearningPercentLabel.Text = "Learning: 0.0%";

        int iterations = (int)IterationsSlider.Value;
        int totalPlayers = _selectedMode == TrainingMode.Both ? 8 : 4;

        var progress = new Progress<TrainingProgress>(p =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Mark new phase on graph
                if (p.IsNewPhase)
                {
                    string shortLabel = p.TrainingModeLabel == "as X" ? "X:" : "O:";
                    _graphDrawable.MarkPhase($"{shortLabel}{p.PlayerName}");
                    _lastPhaseWins = 0;
                    _lastPhaseLosses = 0;
                    _lastPhaseDraws = 0;
                    return;
                }

                string modeTag = p.TrainingModeLabel != "" ? $" ({p.TrainingModeLabel})" : "";
                CurrentPlayerLabel.Text = $"Player {p.PlayerIndex}/{p.TotalPlayers}: {p.PlayerName}{modeTag}";
                TrainingProgressBar.Progress = p.ProgressPercent / 100.0;
                ProgressLabel.Text = $"Game {p.CurrentIteration}/{p.TotalIterations}";
                WinsLabel.Text = p.Wins.ToString();
                LossesLabel.Text = p.Losses.ToString();
                DrawsLabel.Text = p.Draws.ToString();

                double overallProgress = ((p.PlayerIndex - 1) * iterations + p.CurrentIteration) / (double)(p.TotalPlayers * iterations) * 100;
                OverallProgressLabel.Text = $"Overall: {overallProgress:F1}%";

                // Update graph with game results since last report
                // Calculate delta from last report
                int deltaWins = p.Wins - _lastPhaseWins;
                int deltaLosses = p.Losses - _lastPhaseLosses;
                int deltaDraws = p.Draws - _lastPhaseDraws;
                int deltaGames = deltaWins + deltaLosses + deltaDraws;

                if (deltaGames > 0)
                {
                    // Add points from delta batch
                    double deltaPoints = deltaWins * 1.0 + deltaDraws * 0.5;
                    _graphTotalPoints += deltaPoints;
                    _graphTotalGames += deltaGames;

                    double learningPct = _graphTotalGames > 0 ? _graphTotalPoints / _graphTotalGames * 100.0 : 0;

                    // Record as a single data point for the batch
                    _graphDrawable.RecordBatch(deltaGames, learningPct);

                    LearningPercentLabel.Text = $"Learning: {learningPct:F1}%";
                    LearningGraphView.Invalidate();
                }

                _lastPhaseWins = p.Wins;
                _lastPhaseLosses = p.Losses;
                _lastPhaseDraws = p.Draws;

                // Track totals across all players
                if (p.CurrentIteration == p.TotalIterations)
                {
                    _totalWins += p.Wins;
                    _totalLosses += p.Losses;
                    _totalDraws += p.Draws;
                }
            });
        });

        try
        {
            await _trainingService.TrainAgentAsync(_agent, iterations, progress, _cts.Token, _selectedMode);

            string modeText = _selectedMode switch
            {
                TrainingMode.AsX => "as X (first)",
                TrainingMode.AsO => "as O (second)",
                TrainingMode.Both => "as X & O (both sides)",
                _ => ""
            };
            int totalGames = _selectedMode == TrainingMode.Both ? iterations * 8 : iterations * 4;
            double finalLearning = _graphTotalGames > 0 ? _graphTotalPoints / _graphTotalGames * 100.0 : 0;

            CompletionFrame.IsVisible = true;
            CompletionSummary.Text = $"Trained {modeText}\n" +
                                     $"{totalPlayers} phases × {iterations} games each = {totalGames} total\n" +
                                     $"Total: {_totalWins}W / {_totalLosses}L / {_totalDraws}D\n" +
                                     $"Learning: {finalLearning:F1}%\n" +
                                     $"X Q-Table: {_agent.QTableX.Count} states  •  O Q-Table: {_agent.QTableO.Count} states";
        }
        catch (OperationCanceledException)
        {
            await DisplayAlertAsync("Cancelled", "Training was cancelled. Partial progress has been saved.", "OK");
            _agent.SaveQTable();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Training failed: {ex.Message}", "OK");
        }
        finally
        {
            StartTrainingButton.IsEnabled = true;
            StartTrainingButton.Text = "▶️ Start Full Training";
            CancelButton.IsVisible = false;
            UpdateAgentParamsLabel();
        }
    }

    private void OnCancelTraining(object? sender, EventArgs e)
    {
        _cts?.Cancel();
    }
}

