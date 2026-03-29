using TicTacToeAI.Pages;

namespace TicTacToeAI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(TrainingPage), typeof(TrainingPage));
            Routing.RegisterRoute(nameof(PlayPage), typeof(PlayPage));
        }
    }
}
