using System;

namespace GZipTest
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("Архиватор");
                Console.WriteLine("--------------------------------------");
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
                Console.WriteLine("--------------------------------------");
                return 1;
            }
        }
    }
}
