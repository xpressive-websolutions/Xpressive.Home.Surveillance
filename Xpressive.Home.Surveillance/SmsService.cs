using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Meadow;

namespace Xpressive.Home.Surveillance
{
    public class SmsService
    {
        private static readonly Lazy<SmsService> _instance =
            new Lazy<SmsService>(() => new SmsService());

        private static readonly HttpClient _httpClient = new HttpClient();
        private string _userName;
        private string _password;
        private string _originator;
        private string[] _recipients;

        private SmsService() { }

        public static SmsService Instance => _instance.Value;

        public void Init(string userName, string password, string originator, string[] recipients)
        {
            _userName = userName;
            _password = password;
            _originator = originator;
            _recipients = recipients;
        }

        public async Task SendSms(string deviceName)
        {
            try
            {
                var body = new
                {
                    UserName = _userName,
                    Password = _password,
                    Originator = _originator,
                    Recipients = _recipients,
                    MessageText = $"ALARM ({deviceName})",
                };

                var json = new MicroJsonSerializer().Serialize(body);
                var client = new HttpClient();
                var content = new StringContent(json, Encoding.UTF8);
                var response = await _httpClient.PostAsync("http://json.aspsms.com/SendTextSMS", content);

                Resolver.Log.Info($"Sent SMS: {response.StatusCode} - {response.ReasonPhrase}");
            }
            catch (Exception e)
            {
                Resolver.Log.Error("Error while sending SMS: " + e.Message);
            }
        }
    }
}
