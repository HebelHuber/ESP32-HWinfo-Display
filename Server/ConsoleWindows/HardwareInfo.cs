using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleWindows
{
    internal class HardwareInfo
    {
        private UpdateVisitor updateVisitor = null;
        private Computer computer = null;

        internal class UpdateVisitor : IVisitor
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


        internal Results GetSystemInfo()
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

            computer.Close();

            return results;
        }

        internal void GetSensorsRecursive(IHardware item, ref List<ISensor> sensors)
        {
            foreach (var sub in item.SubHardware)
                GetSensorsRecursive(sub, ref sensors);

            sensors.AddRange(item.Sensors);
        }
    }
}
