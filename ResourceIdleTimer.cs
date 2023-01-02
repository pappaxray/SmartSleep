using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSleep
{
    public class ResourceIdleTimer
    {
        public Stopwatch stopwatch;
        public double idleTime => stopwatch.Elapsed.TotalSeconds;
        bool _isActive = false;

        public ResourceIdleTimer()
        {
            stopwatch = Stopwatch.StartNew();
            stopwatch.Start();
        }

        public void Update(bool isActive)
        {
            if (isActive)
            {
                if (!_isActive)
                {
                    _isActive = true;
                    stopwatch.Stop();
                    stopwatch.Reset();
                }
            }
            else
            {
                if (_isActive)
                {
                    _isActive = false;
                    stopwatch.Restart();
                }
            }
        }

        public void Reset()
        {
            _isActive = false;
            stopwatch.Restart();
        }
    }
}