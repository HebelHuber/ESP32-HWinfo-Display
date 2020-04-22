using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenHardwareMonitor.Hardware;

namespace ConsoleWindows
{
    class Program
    {
        private const int RateInSeconds = 60;
        private const string urlCPU = @"http://madhome:8123/api/states/sensor.madwolf_CPU_temp";
        private const string urlGPU = @"http://madhome:8123/api/states/sensor.madwolf_GPU_temp";
        private const string AccessToken = @"eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiI0MmNiYWIxY2U2MDk0N2NhOGFkNzM3ZTg1ZGFiZTY3ZCIsImlhdCI6MTU4NzU0OTI1OSwiZXhwIjoxOTAyOTA5MjU5fQ.0XTj2dgh6viJVL37klxdgtO74Y7rOhqKNyTcUjicnh0";

        private static HardwareInfo info = new HardwareInfo();


        static void Main()
        {
            do
            {
                while (!Console.KeyAvailable)
                {
                    try
                    {
                        var data = info.GetSystemInfo();

                        Send(urlCPU, GetPayload((int)data.CPUtemp));
                        Send(urlGPU, GetPayload((int)data.GPUtemp));
                    }
                    catch { }

                    Thread.Sleep(RateInSeconds * 1000);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            Console.WriteLine("Sending stopped");
            Console.ReadKey();
        }

        private static void Send(string url, byte[] payload)
        {
            var myWebRequest = WebRequest.Create(url);
            var myHttpWebRequest = (HttpWebRequest)myWebRequest;
            myHttpWebRequest.PreAuthenticate = true;
            myHttpWebRequest.Method = "POST";
            myHttpWebRequest.Headers.Add("Authorization", "Bearer " + AccessToken);
            myHttpWebRequest.Accept = "application/json";

            Stream requestStream = myHttpWebRequest.GetRequestStream();
            requestStream.Write(payload, 0, payload.Length);
            requestStream.Close();

            var myWebResponse = myWebRequest.GetResponse();
            var responseStream = myWebResponse.GetResponseStream();
            if (responseStream != null)
            {
                var myStreamReader = new StreamReader(responseStream, Encoding.Default);
                var replyJson = myStreamReader.ReadToEnd();
                Console.WriteLine("Response:" + Environment.NewLine + replyJson);

                responseStream.Close();
                myWebResponse.Close();
            }
        }

        private static byte[] GetPayload(int number)
        {
            var payload = "{\"state\": " + number + ", \"attributes\": {\"unit_of_measurement\": \"°C\"}}";
            return Encoding.UTF8.GetBytes(payload);
        }
    }
}