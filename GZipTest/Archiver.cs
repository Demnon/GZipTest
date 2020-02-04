using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipTest
{
    // Класс, управляющий процессами сжатия/распаковки файлов, а также чтения и записи
    class Archiver
    {
        // Имена входного и выходного файлов
        string s_SourceFile, s_ResultingFile;
        // Число процессоров компьютера, доступных для работы
        int i_CountProcessors;
        // Размер блока, на который делится входной файл при сжатии
        const int i_BlockSize = 1000000;

        public Archiver(string s_SourceFile, string s_ResultingFile)
        {
            this.s_SourceFile = s_SourceFile;
            this.s_ResultingFile = s_ResultingFile;
            i_CountProcessors = Environment.ProcessorCount;
        }

        // Выполнение сжатия
        public void RunningCompression()
        {
            Console.WriteLine("Start compression...\n");
        }

        // Выполнение распаковки
        public void RunningDecompression()
        {
            Console.WriteLine("Start decompression...\n");
        }
    }
}
