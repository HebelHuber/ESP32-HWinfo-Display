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
        private const int HistorySmoothing = 20;

        private static List<Results> ResultHistory = new List<Results>();
        private static SerialPort port;

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
                    }
                    else
                    {
                        // probe for ports
                        ProbingLoop();

                        Thread.Sleep(500);
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
                }
                catch
                {
                    Console.WriteLine("could not open port " + portName);
                }
            }
        }

        private static void DataLoop()
        {
            Console.WriteLine("port open and sending stats, press ESC to stop");

            int lastIndex = 0;
            HardwareInfo info = new HardwareInfo();

            do
            {
                while (!Console.KeyAvailable && port.IsOpen)
                {
                    ResultHistory.Add(info.GetSystemInfo());

                    if (ResultHistory.Count > HistorySmoothing)
                        ResultHistory.RemoveAt(0);

                    lastIndex++;

                    if (lastIndex > 9)
                        lastIndex = 0;

                    var payload = "drawer.UpdateScreen(" + ResultHistory.ToSmoothedData(HistorySmoothing).AsParameters(lastIndex) + ")";
                    port.WriteLine(payload + Environment.NewLine);

                    Thread.Sleep(2);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }
}