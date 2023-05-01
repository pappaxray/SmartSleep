using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SmartSleep
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        SystemPerformance systemPerformance = new SystemPerformance();
        TeamCityConnection teamcityConnection = new TeamCityConnection();

        double shutdownTime = 5 * 60;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            timer.Tick += new EventHandler(Tick);
            timer.Start();

            systemPerformance.Init();
            teamcityConnection.Init();
        }

        void Tick(object sender, EventArgs e)
        {
            systemPerformance.Update();
            teamcityConnection.Update();

            var buildIdleTime = teamcityConnection.IsAgentRequired ? 0.0f : (float)Math.Floor((DateTime.Now - teamcityConnection.lastActiveTime).TotalSeconds);
            var networkIdleTime = (float)Math.Floor(systemPerformance.NetworkIdleTimer.idleTime);

            float idleTime = MathF.Min(networkIdleTime, buildIdleTime);

            var ShutdownTimeValue = (Label)this.FindName("ShutdownTimeValue");
            ShutdownTimeValue.Content = string.Format("{0}", shutdownTime - idleTime);

            var NetworkIdleThresholdValue = (Label)this.FindName("NetworkIdleThresholdValue");
            NetworkIdleThresholdValue.Content = string.Format("{0}", systemPerformance.NetworkIdleThreshold);
            var NetworkIdleTimeValue = (Label)this.FindName("NetworkIdleTimeValue");
            NetworkIdleTimeValue.Content = string.Format("{0}", networkIdleTime);
            var NetworkSentValue = (Label)this.FindName("NetworkSentValue");
            NetworkSentValue.Content = string.Format("{0}", systemPerformance.NetworkSentPerTick);

            var BuildStatusValue = (Label)this.FindName("BuildStatusValue");
            BuildStatusValue.Content = string.Format("{0}", teamcityConnection.BuildState);
            var BuildIdleValue = (Label)this.FindName("BuildIdleValue");
            BuildIdleValue.Content = string.Format("{0}", buildIdleTime);

            if (idleTime >= shutdownTime)
            {
                //SystemActions.Shutdown();
                SystemActions.Sleep();
            }
        }
    }
}
