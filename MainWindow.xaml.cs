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

        double shutdownTime = 60;

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
        }

        void Tick(object sender, EventArgs e)
        {
            systemPerformance.Update();

            var cpuActivityValue = (Label)this.FindName("CPUActivityValue");
            cpuActivityValue.Content = string.Format("{0}", systemPerformance.HighestUtilization);
            var cpuIdleThresholdValue = (Label)this.FindName("CPUIdleThresholdValue");
            cpuIdleThresholdValue.Content = string.Format("{0}", systemPerformance.CPUIdleThresholdPct);
            var CPUIdleTimeValue = (Label)this.FindName("CPUIdleTimeValue");
            CPUIdleTimeValue.Content = string.Format("{0}", systemPerformance.CPUIdleTimer.idleTime);
            var NetworkIdleThresholdValue = (Label)this.FindName("NetworkIdleThresholdValue");
            NetworkIdleThresholdValue.Content = string.Format("{0}", systemPerformance.NetworkIdleThreshold);
            var NetworkIdleTimeValue = (Label)this.FindName("NetworkIdleTimeValue");
            NetworkIdleTimeValue.Content = string.Format("{0}", systemPerformance.NetworkIdleTimer.idleTime);
            var NetworkSentValue = (Label)this.FindName("NetworkSentValue");
            NetworkSentValue.Content = string.Format("{0}", systemPerformance.TotalSent);
            //var networkIdleValue = (Label)this.FindName("NetworkIdleValue");
            //networkIdleValue.Content = string.Format("{0}", systemPerformance.stopwatch.Elapsed.TotalSeconds);

            if(systemPerformance.NetworkIdleTimer.idleTime > shutdownTime &&
                systemPerformance.CPUIdleTimer.idleTime > shutdownTime)
            {
                SystemActions.Shutdown();
            }
        }
    }
}
