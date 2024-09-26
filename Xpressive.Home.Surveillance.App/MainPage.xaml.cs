using System.ComponentModel;
using System.Net.Http.Json;
using Shiny.Jobs;
using JobInfo = Shiny.Jobs.JobInfo;

namespace Xpressive.Home.Surveillance.App
{
    public partial class MainPage : ContentPage
    {
        public const string MainControllerAddress = "http://192.168.1.56:5417/api";
        private readonly HttpClient _httpClient = new HttpClient();

        public MainPage()
        {
            InitializeComponent();
            Status.Instance.PropertyChanged += OnIsArmedChanged;
        }

        private void OnIsArmedChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                StatusImage.Source = Status.Instance.IsArmed
                    ? ImageSource.FromFile("elipse_red.png")
                    : ImageSource.FromFile("elipse_green.png");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void OnArmClicked(object? sender, EventArgs e)
        {
            try
            {
                _httpClient.PostAsync($"{MainControllerAddress}/arm", new StringContent(string.Empty));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void OnDisarmClicked(object? sender, EventArgs e)
        {
            try
            {
                _httpClient.PostAsync($"{MainControllerAddress}/disarm", new StringContent(string.Empty));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    public class AlarmDeviceStatus
    {
        public string DeviceType { get; set; }
        public bool IsArmed { get; set; }
    }

    public class Status : Shiny.NotifyPropertyChanged
    {
        private static readonly Lazy<Status> _instance = new Lazy<Status>(() => new Status());
        private bool _isArmed;

        private Status()
        {
        }

        public static Status Instance => _instance.Value;

        public bool IsArmed
        {
            get => _isArmed;
            set
            {
                _isArmed = value;
                RaisePropertyChanged();
            }
        }
    }

    public class UpdateStatusJob : IJob
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancelToken);

                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var address = $"{MainPage.MainControllerAddress}/status";
                    var status = await _httpClient.GetFromJsonAsync<AlarmDeviceStatus>(address, cancelToken);
                    Status.Instance.IsArmed = status.IsArmed;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
