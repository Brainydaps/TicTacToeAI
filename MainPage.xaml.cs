using TicTacToeAI.Models;
using TicTacToeAI.Pages;

namespace TicTacToeAI;

public partial class MainPage : ContentPage
{
    private static readonly string[] AgentNames =
        ["Balanced", "Quick Myopic", "Quick Overplanner", "Slow Myopic", "Slow Overplanner"];

    private string? _selectedAgentName;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnAgentSelected(object? sender, EventArgs e)
    {
        if (AgentPicker.SelectedIndex < 0) return;

        _selectedAgentName = AgentNames[AgentPicker.SelectedIndex];
        var agent = QLearningAgent.Create(_selectedAgentName);

        AgentNameLabel.Text = _selectedAgentName;
        AgentParamsLabel.Text = $"Learning Rate (α): {agent.Alpha}  •  Discount Factor (γ): {agent.Gamma}";

        bool hasXTable = agent.HasQTable('X');
        bool hasOTable = agent.HasQTable('O');
        bool hasAny = hasXTable || hasOTable;

        string xStatus = hasXTable ? "✅ X" : "❌ X";
        string oStatus = hasOTable ? "✅ O" : "❌ O";
        AgentQTableLabel.Text = hasAny
            ? $"Q-Tables: {xStatus}  •  {oStatus}"
            : "⚠️ No Q-Tables yet — train the agent first!";

        AgentInfoFrame.IsVisible = true;
        TrainButton.IsEnabled = true;
        PlayButton.IsEnabled = hasAny;
    }

    private async void OnTrainClicked(object? sender, EventArgs e)
    {
        if (_selectedAgentName == null) return;
        await Shell.Current.GoToAsync($"{nameof(TrainingPage)}?AgentName={_selectedAgentName}");
    }

    private async void OnPlayClicked(object? sender, EventArgs e)
    {
        if (_selectedAgentName == null) return;
        await Shell.Current.GoToAsync($"{nameof(PlayPage)}?AgentName={_selectedAgentName}");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Refresh Q-table status when returning from training
        if (_selectedAgentName != null)
        {
            var agent = QLearningAgent.Create(_selectedAgentName);
            bool hasXTable = agent.HasQTable('X');
            bool hasOTable = agent.HasQTable('O');
            bool hasAny = hasXTable || hasOTable;

            string xStatus = hasXTable ? "✅ X" : "❌ X";
            string oStatus = hasOTable ? "✅ O" : "❌ O";
            AgentQTableLabel.Text = hasAny
                ? $"Q-Tables: {xStatus}  •  {oStatus}"
                : "⚠️ No Q-Tables yet — train the agent first!";
            PlayButton.IsEnabled = hasAny;
        }
    }
}
