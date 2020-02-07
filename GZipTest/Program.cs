using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    class Program
    {
        static int Main(string[] args)
        {
            // Параметры для отладки
            args = new string[3];
             args[0] = "compress";
             args[1] = @"C:\\Users\\Odinc\\Desktop\\Родник.mp4";
             args[2] = @"C:\\Users\\Odinc\\Desktop\\res.gz";

            /*args[0] = "decompress";
            args[1] = @"C:\\Users\\Odinc\\Desktop\\res.gz";
            args[2] = @"C:\\Users\\Odinc\\Desktop\\Родник.mp4";*/

            try
            {
                // Проверка входных данных
                InputValidation.TestInputData(args);

                Archiver a_Archiver = new Archiver(args[1], args[2]);

                // Выбор программы (compress или decompress
                if (args[0].ToLower() == "compress")
                {
                    a_Archiver.RunningCompression();
                }
                else
                {
                    a_Archiver.RunningDecompression();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка! " + ex.Message);
                return 1;
            }
        }
    }
}
