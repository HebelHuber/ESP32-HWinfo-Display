using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenHardwareMonitor.Hardware;

namespace ConsoleWindows
{
    class Program
    {
        private const int packetsperSecond = 10;
        private const int SmoothingSeconds = 2;

        private static int HistorySmoothing => packetsperSecond * SmoothingSeconds;
        private static int TimeoutMS => 1000 / packetsperSecond;

        private static List<Results> ResultHistory = new List<Results>();
        private static SerialPort port;
        static int lastIndex = 0;
        static HardwareInfo info = new HardwareInfo();

        static void Main()
        {

            do
            {
                while (!Console.KeyAvailable)
                {
                    if (port != null && port.IsOpen)
                    {
                        // communicate
                        DataLoop();
                        Thread.Sleep(TimeoutMS);
                    }
                    else
                    {
                        // probe for ports
                        ProbingLoop();
                        Thread.Sleep(2000);
                    }
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            port.Close();
            port.Dispose();

            Console.WriteLine("port closed");
            Console.ReadKey();
        }

        private static void ProbingLoop()
        {
            Console.WriteLine("probing");

            var ports = SerialPort.GetPortNames().ToList();

            if (ports.Count == 0)
            {
                Console.WriteLine("No serial ports!");
                return;
            }

            // try to open ports
            port = null;

            foreach (var portName in ports)
            {
                var tempPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);

                try
                {
                    tempPort.Open();
                    port = tempPort;
                    Console.Write(" - success: " + portName);
                    Console.WriteLine();
                }
                catch
                {
                    Console.WriteLine("could not open port " + portName);
                }
            }
        }

        private static void DataLoop()
        {
            ResultHistory.Add(info.GetSystemInfo());

            if (ResultHistory.Count > HistorySmoothing)
                ResultHistory.RemoveAt(0);

            lastIndex++;

            if (lastIndex > 9)
                lastIndex = 0;

            var payload = "drawer.UpdateScreen(" + ResultHistory.ToSmoothedData(HistorySmoothing).AsParameters(lastIndex) + ")";
            port.WriteLine(payload + Environment.NewLine);

            Console.Write(".");
        }
    }
}