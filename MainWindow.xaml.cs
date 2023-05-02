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
        PowerManagement powerManagement = new PowerManagement();

        double shutdownTime = 5 * 60;
        bool _logDirty = false;

        public MainWindow()
        {
            Logger.Init();            
            Logger.Log("Starting application");

            InitializeComponent();
        }

        void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            timer.Tick += new EventHandler(Tick);
            timer.Start();

            systemPerformance.Init();
            teamcityConnection.Init();

            //var debugButton = (Button)this.FindName("DebugButton");
            //debugButton.Click += DebugButton_Click;

            Logger.OnLog += Logger_OnLog;
            UpdateLog();
        }

        /*void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("DebugButton");
        }*/

        void Tick(object sender, EventArgs e)
        {
            systemPerformance.Update();
            teamcityConnection.Update();

            var buildIdleTime = teamcityConnection.IsAgentRequired ? 0.0f : (float)Math.Floor((DateTime.Now - teamcityConnection.lastActiveTime).TotalSeconds);
            var networkIdleTime = (float)Math.Floor(systemPerformance.NetworkIdleTimer.idleTime);

            float idleTime = MathF.Min(networkIdleTime, buildIdleTime);

            var ShutdownTimeValue = (Label)this.FindName("ShutdownTimeValue");
            ShutdownTimeValue.Content = string.Format("{0}", shutdownTime - idleTime);

            var AgentStatusValue = (Label)this.FindName("AgentStatusValue");
            AgentStatusValue.Content = string.Format("{0}", teamcityConnection.AgentState);
            var BuildIDValue = (Label)this.FindName("BuildIDValue");
            BuildIDValue.Content = string.Format("{0}", teamcityConnection._buildID != -1 ? teamcityConnection._buildID : "None");
            var AgentIdleValue = (Label)this.FindName("AgentIdleValue");
            AgentIdleValue.Content = string.Format("{0}", buildIdleTime);

            if(_logDirty)
            {
                _logDirty = false;
                UpdateLog();
            }

            if (idleTime >= shutdownTime)
            {
                powerManagement.Sleep();
            }
        }

        void UpdateLog()
        {
            var outputValue = (TextBox)this.FindName("OutputValue");
            outputValue.Text = Logger.LogText;
        }

        void Logger_OnLog(string txt)
        {
            _logDirty = true;
        }
    }
}