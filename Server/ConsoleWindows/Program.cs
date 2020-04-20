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

        private static UpdateVisitor updateVisitor = null;
        private static Computer computer = null;

        private static List<Results> ResultHistory = new List<Results>();

        static void Main()
        {
            var port = new SerialPort("COM3", 115200, Parity.None, 8, StopBits.One);

            try
            {
                port.Open();
            }
            catch
            {
                Console.WriteLine("could not open port");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("port open and sending stats, press ESC to stop");

            int lastIndex = 0;

            do
            {
                while (!Console.KeyAvailable && port.IsOpen)
                {
                    GetSystemInfo();

                    lastIndex++;

                    if (lastIndex > 9)
                        lastIndex = 0;

                    var payload = "drawer.UpdateScreen(" + ResultHistory.ToSmoothedData(HistorySmoothing).AsParameters(lastIndex) + ")";
                    port.WriteLine(payload + Environment.NewLine);

                    Thread.Sleep(2);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            port.Close();
            port.Dispose();

            Console.WriteLine("port closed");
            Console.ReadKey();
        }

        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }


        static void GetSystemInfo()
        {
            //Console.WriteLine("Getting System info");

            if (updateVisitor == null || computer == null)
            {
                updateVisitor = new UpdateVisitor();
                computer = new Computer();

                computer.RAMEnabled = true;
                computer.CPUEnabled = true;
                computer.GPUEnabled = true;
                //computer.MainboardEnabled = true;
                //computer.FanControllerEnabled = true;
                //computer.HDDEnabled = true;

            }

            computer.Open();
            computer.Accept(updateVisitor);


            List<ISensor> sensors = new List<ISensor>();

            foreach (var item in computer.Hardware)
                GetSensorsRecursive(item, ref sensors);

            //Console.WriteLine("Collected " + sensors.Count + " sensors");
            Results results = new Results();

            foreach (var sensor in sensors)
            {
                //Console.WriteLine(sensor.Hardware.HardwareType.ToString() + ": " + sensor.Hardware.Name + ": " + sensor.Name + ": " + sensor.SensorType.ToString() + ": " + sensor.Value.ToString());

                if (sensor.Hardware.HardwareType == HardwareType.CPU && sensor.SensorType == SensorType.Load && sensor.Index == 0)
                    results.CPUload = (float)sensor.Value;

                if (sensor.Hardware.HardwareType == HardwareType.CPU && sensor.SensorType == SensorType.Temperature && sensor.Name.ToLower().Contains("package"))
                    results.CPUtemp = (float)sensor.Value;

                if ((sensor.Hardware.HardwareType == HardwareType.GpuAti || sensor.Hardware.HardwareType == HardwareType.GpuNvidia) && sensor.SensorType == SensorType.Load)
                    results.GPUload = (float)sensor.Value;

                if ((sensor.Hardware.HardwareType == HardwareType.GpuAti || sensor.Hardware.HardwareType == HardwareType.GpuNvidia) && sensor.SensorType == SensorType.Temperature)
                    results.GPUtemp = (float)sensor.Value;

                if (sensor.Hardware.HardwareType == HardwareType.RAM && sensor.SensorType == SensorType.Load)
                    results.RAMpercentage = (float)sensor.Value;
            }

            ResultHistory.Add(results);

            if (ResultHistory.Count > HistorySmoothing)
                ResultHistory.RemoveAt(0);

            computer.Close();
        }

        private static void GetSensorsRecursive(IHardware item, ref List<ISensor> sensors)
        {
            foreach (var sub in item.SubHardware)
                GetSensorsRecursive(sub, ref sensors);

            sensors.AddRange(item.Sensors);
        }
    }

    internal struct Results
    {
        public float CPUload;
        public float CPUtemp;

        public float GPUload;
        public float GPUtemp;

        public float RAMpercentage;

        public string Time;
    }

    internal static class Extensions
    {
        internal static Results ToSmoothedData(this List<Results> history, int length)
        {
            var outStruct = new Results();

            int count = Math.Min(history.Count, length);

            outStruct.CPUload = history.TakeLast(count).Average(q => q.CPUload);
            outStruct.CPUtemp = history.TakeLast(count).Average(q => q.CPUtemp);
            outStruct.GPUload = history.TakeLast(count).Average(q => q.GPUload);
            outStruct.GPUtemp = history.TakeLast(count).Average(q => q.GPUtemp);
            outStruct.RAMpercentage = history.TakeLast(count).Average(q => q.RAMpercentage);

            outStruct.Time = DateTime.Now.ToString("T", CultureInfo.CreateSpecificCulture("de-DE"));

            return outStruct;
        }

        internal static void Log(this Results vals)
        {
            Console.WriteLine();
            Console.WriteLine("---RESULTS---");

            foreach (var field in typeof(Results).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                Console.WriteLine("{0}: {1}", field.Name, field.GetValue(vals));
            }
        }

        internal static string AsParameters(this Results vals, int dotIndex)
        {
            return string.Join(",", "'" + vals.Time + "'", (int)vals.CPUload, (int)vals.CPUtemp, (int)vals.GPUload, (int)vals.GPUtemp, (int)vals.RAMpercentage, dotIndex);
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> collection,
        int n)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), $"{nameof(n)} must be 0 or greater");

            LinkedList<T> temp = new LinkedList<T>();

            foreach (var value in collection)
            {
                temp.AddLast(value);
                if (temp.Count > n)
                    temp.RemoveFirst();
            }

            return temp;
        }
    }
}