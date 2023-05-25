using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartSleep
{
    class SystemPerformance
    {
        public float HighestCPUUtilization = 0;
        public float CPUIdleThresholdPct = 36.0f;
        public long NetworkIdleThreshold = 100000;
        public long NetworkSentPerTick = 0;

        public Dictionary<string, CounterSample> cs = new Dictionary<string, CounterSample>();
        public Dictionary<string, long> networkInterfaces = new Dictionary<string, long>();
        public ResourceIdleTimer CPUIdleTimer = new ResourceIdleTimer();
        public ResourceIdleTimer NetworkIdleTimer = new ResourceIdleTimer();        

        PerformanceCounter _cpuPC = new PerformanceCounter("Processor Information", "% Processor Time");
        PerformanceCounterCategory _cpuPI = new PerformanceCounterCategory("Processor Information");        
        string[] instances;
        Regex getCpuAndCore = new Regex(@"(\d+),(\d+)", RegexOptions.Compiled);

        public static float Calculate(CounterSample oldSample, CounterSample newSample)
        {
            float difference = newSample.RawValue - oldSample.RawValue;
            float timeInterval = newSample.TimeStamp100nSec - oldSample.TimeStamp100nSec;
            if (timeInterval != 0)
                return 100.0f * (1.0f - (difference / timeInterval));
            else
                return 0;
        }

        public void Init()
        {
            instances = _cpuPI.GetInstanceNames();

            foreach (var s in instances)
            {
                _cpuPC.InstanceName = s;
                cs.Add(s, _cpuPC.NextSample());
            }
        }

        public void Update()
        {
            HighestCPUUtilization = 0;
            foreach (var s in instances)
            {
                _cpuPC.InstanceName = s;
                var oldSample = cs[s];
                cs[s] = _cpuPC.NextSample();

                var m = getCpuAndCore.Match(s);
                if (m.Success)
                {
                    float utilization = Calculate(oldSample, cs[s]);
                    if (HighestCPUUtilization < utilization)
                        HighestCPUUtilization = utilization;
                }
            }

            NetworkSentPerTick = 0;
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                long lastSent = 0;
                if(networkInterfaces.ContainsKey(ni.Name))
                    lastSent = networkInterfaces[ni.Name];
                var bytesSent = ni.GetIPv4Statistics().BytesSent;
                networkInterfaces[ni.Name] = bytesSent;
                var sendChange = bytesSent - lastSent;
                NetworkSentPerTick += sendChange;
            }

            CPUIdleTimer.Update(HighestCPUUtilization > CPUIdleThresholdPct);
            NetworkIdleTimer.Update(NetworkSentPerTick > NetworkIdleThreshold);
        }
    }
}