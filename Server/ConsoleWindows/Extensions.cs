using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleWindows
{
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
