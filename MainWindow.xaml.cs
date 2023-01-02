using Microsoft.Win32;
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
        float SleepTime = 5 * 60; // Sleep after X minutes
        bool SleepTriggered = false;

        public MainWindow()
        {
            InitializeComponent();
            SystemEvents.PowerModeChanged += OnPowerChange;
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

            CPUActivityValue.Content = string.Format("{0}", systemPerformance.HighestUtilization);
            CPUIdleThresholdValue.Content = string.Format("{0}", systemPerformance.CPUIdleThresholdPct);
            CPUIdleTimeValue.Content = string.Format("{0}", systemPerformance.CPUIdleTimer.idleTime.ToString("F0"));
            NetworkIdleThresholdValue.Content = string.Format("{0}", systemPerformance.NetworkIdleThreshold);
            NetworkIdleTimeValue.Content = string.Format("{0}", systemPerformance.NetworkIdleTimer.idleTime.ToString("F0"));
            NetworkSentValue.Content = string.Format("{0}", systemPerformance.TotalSent);
            SleepTriggeredValue.Content = string.Format("{0}", SleepTriggered);
            SleepIdleTimeValue.Content = string.Format("{0}", SleepTime);

            // Shutdown when timer reached
            if (systemPerformance.NetworkIdleTimer.idleTime > SleepTime &&
                systemPerformance.CPUIdleTimer.idleTime > SleepTime)
            {
                if(!SleepTriggered)
                {
                    SleepTriggered = true;
                    SystemFunctions.SetSuspendState(false, true, true);
                }
            }
        }

        

        private void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    SleepTriggered = false;
                    systemPerformance.ResetTimers();
                    break;
                case PowerModes.Suspend:
                    break;
            }
        }
    }
}
