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
        bool _isIdle = false;

        public ResourceIdleTimer()
        {
            stopwatch = Stopwatch.StartNew();
        }

        public void Update(bool isActive)
        {
            if (!isActive)
            {
                if (!_isIdle)
                {
                    _isIdle = true;
                    stopwatch.Restart();
                }
            }
            else
            {
                if (_isIdle)
                {
                    _isIdle = false;
                    stopwatch.Stop();
                    stopwatch.Reset();
                }
            }
        }
    }
}