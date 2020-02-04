using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace GZipTest
{
    // Класс для контроля параметров программы, задаваемых в командной строке
    public static class InputValidation
    {
        // Метод проверки входных данных
        public static void TestInputData(string[] s_InputData)
        {
            Console.WriteLine("Verification input data...\n");

            if (s_InputData.Length != 3)
            {
                throw new Exception("Arguments must match the following pattern:" +
                    " compress/decompress [Name source file] [Name resulting file].");
            }
            if (s_InputData[0].ToLower() != "compress" && s_InputData[0].ToLower() != "decompress")
            {
                throw new Exception("First argument should be compress/decompress.");
            }
            if (s_InputData[1].Length == 0 || s_InputData[2].Length == 0)
            {
                throw new Exception("Specify the names of the source and resulting files " +
                    "as the second and third parameters.");
            }
            if (s_InputData[1] == s_InputData[2])
            {
                throw new Exception("Source and resulting file must not match.");
            }

            FileInfo f_InputFile = new FileInfo(s_InputData[1]);
            FileInfo f_ResultingFile = new FileInfo(s_InputData[2]);

            if (f_InputFile.Exists == false)
            {
                throw new Exception("Source file with this name does not exist.");
            }
            if (f_InputFile.Extension == ".gz" && s_InputData[0].ToLower() == "compress")
            {
                throw new Exception("Source file is already compressed.");
            }
            if (s_InputData[0].ToLower() == "decompress" && f_InputFile.Extension != ".gz")
            {
                throw new Exception("Source file must have extension '.gz'.");
            }
            if (f_ResultingFile.Exists == true)
            {
                throw new Exception("Resulting file already exists, specify a new file name.");
            }

            Console.WriteLine("Verification completed.\n");
        }
    }
}
