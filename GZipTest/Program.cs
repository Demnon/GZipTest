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

                ControlThreads c_ControlThreads = new ControlThreads(args[1], args[2]);

                // Запуск потоков на сжатие/распаковку файла
                if (args[0].ToLower() == "compress")
                {
                    c_ControlThreads.RunningCompression();
                }
                else
                {
                    c_ControlThreads.RunningDecompression();
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
