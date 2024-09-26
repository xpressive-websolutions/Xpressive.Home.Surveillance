using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Jobs;
using JobInfo = Shiny.Jobs.JobInfo;

namespace Xpressive.Home.Surveillance.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseShiny()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            var updateStatusJobInfo = new JobInfo("UpdateStatusJob", typeof(UpdateStatusJob))
            {
                BatteryNotLow = true,
                DeviceCharging = false,
                RunOnForeground = true,
                RequiredInternetAccess = InternetAccess.Any,
            };

            builder.Services.AddJob(updateStatusJobInfo);

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
