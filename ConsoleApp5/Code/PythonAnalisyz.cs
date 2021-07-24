using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    public partial class Program
    {
        //-------------------------------------------------------------------------------------------//
        //---------------------------------Анализ фотографий------------------------------------------//
        //-------------------------------------------------------------------------------------------//
        static string PythonAnalisys()
        {
            string textFromFile = string.Empty;
            try
            {
                string command = "analyzer.py";
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "python";
                start.Arguments = string.Format("{0}", command);
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();

                        FileStream file = new FileStream("analisys_photos.txt", FileMode.Open);
                        byte[] array = new byte[file.Length];
                        file.Read(array, 0, array.Length);
                        textFromFile = System.Text.Encoding.UTF8.GetString(array);
                        file.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return textFromFile;
        }
    }

}
