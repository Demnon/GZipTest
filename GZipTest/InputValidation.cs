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
            Console.WriteLine("Проверка исходных данных...");

            if (s_InputData.Length != 3)
            {
                throw new Exception("Аргументы должны соответствовать следующему шаблону:" +
                    " compress/decompress [Полный путь к исходному файлу] [Полный путь к результирующему файлу].");
            }
            if (s_InputData[0].ToLower() != "compress" && s_InputData[0].ToLower() != "decompress")
            {
                throw new Exception("Первый аргумент должен быть compress или decompress.");
            }
            if (s_InputData[1].Length == 0 || s_InputData[2].Length == 0)
            {
                throw new Exception("Укажите имена исходного и результирующего файла " +
                    "как второй и третий параметры.");
            }
            if (s_InputData[1] == s_InputData[2])
            {
                throw new Exception("Исходный и конечный файлы не должны совпадать.");
            }

            FileInfo f_InputFile = new FileInfo(s_InputData[1]);
            FileInfo f_ResultingFile = new FileInfo(s_InputData[2]);

            if (f_InputFile.Exists == false)
            {
                throw new Exception("Исходный файл с таким полным путем не существует.");
            }
            if (f_InputFile.Extension == ".gz" && s_InputData[0].ToLower() == "compress")
            {
                throw new Exception("Исходный файл уже сжат.");
            }
            if (s_InputData[0].ToLower() == "decompress" && f_InputFile.Extension != ".gz")
            {
                throw new Exception("Исходный файл должен иметь расширение .gz.");
            }
            if (f_ResultingFile.Exists == true)
            {
                throw new Exception("Результирующий файл уже существует, укажите полный путь к новому файлу.");
            }

            Console.WriteLine("Проверка завершена.");
        }
    }
}
