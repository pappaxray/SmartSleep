using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SmartSleep
{
    internal class Logger
    {
        static string _logPath = "SmartSleep.log";
        static FileStream _logStream;
        static UTF8Encoding _utfEncoder = new UTF8Encoding(true);

        static public string LogText { get { return _logText; } }
        static string _logText = "";

        public delegate void OnLogCB(string txt);
        static public event OnLogCB OnLog;

        static public void Init()
        {
            _logStream = File.Open(_logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        }

        static public void Log(string text)
        {
            var str = $"{DateTime.Now.ToString("HH:mm:ss")} {text}\n";
            _logStream.Write(_utfEncoder.GetBytes(str));
            _logStream.Flush();

            _logText += str;
            OnLog?.Invoke(str);
        }
    }
}