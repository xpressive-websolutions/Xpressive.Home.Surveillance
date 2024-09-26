using System;

namespace Xpressive.Home.Surveillance
{
    public class MainController
    {
        private static readonly Lazy<MainController> _instance =
            new  Lazy<MainController>(() => new MainController());

        private MainController() { }

        public static MainController Instance => _instance.Value;

        public bool IsArmed { get; set; }
    }
}
