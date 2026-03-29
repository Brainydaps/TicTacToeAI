using TicTacToeAI.Pages;
using TicTacToeAI.Services;

namespace TicTacToeAI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services
            builder.Services.AddSingleton<RewardCalculator>();
            builder.Services.AddSingleton<QTableGenerator>();
            builder.Services.AddSingleton<TrainingService>();

            // Register pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<TrainingPage>();
            builder.Services.AddTransient<PlayPage>();

#if DEBUG
            System.Diagnostics.Debug.WriteLine("Debug logging enabled");
#endif

            return builder.Build();
        }
    }
}
