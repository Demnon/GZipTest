using System;
using System.Threading;

namespace GZipTest
{
    // Класс, управляющий всеми потоками
    public class ControlThreads
    {
        // Имена входного и выходного файлов
        string s_SourceFile, s_ResultingFile;
        // Размер блока (в байтах)
        const int i_BlockSize = 1000000;
        // Число процессоров компьютера, доступных для работы
        int i_CountProcessors;
        // Очередь чтения
        WorkQueue w_ReadQueue;
        // Словарь записи
        WorkDictionary w_WriteDictionary;
        // События синхронизации всех потоков
        ManualResetEvent[] m_ThreadsEnd;
        // Работа с файлами: чтение и запись
        WorkWithReadAndWrite w_WorkWithReadAndWrite;
        // Сжатие и распаковка
        Archiver a_Archiver;

        public ControlThreads(string s_SourceFile, string s_ResultingFile)
        {
            this.s_SourceFile = s_SourceFile;
            this.s_ResultingFile = s_ResultingFile;

            w_ReadQueue = new WorkQueue();
            w_WriteDictionary = new WorkDictionary();

            //Получение числа процессоров
            Console.WriteLine("Получение характеристик компьютера...");

            // Количество потоков: два потока на чтение и запись соответственно, и количество потоков на compress/decompress,
            // Равное числу ядер компьютера
            i_CountProcessors = Environment.ProcessorCount;
            m_ThreadsEnd = new ManualResetEvent[i_CountProcessors + 2];

            // Класс работы с файлами
            w_WorkWithReadAndWrite = new WorkWithReadAndWrite(this.s_SourceFile, this.s_ResultingFile, i_BlockSize, w_ReadQueue, w_WriteDictionary, m_ThreadsEnd);

            Console.WriteLine("Характеристики компьютера получены.");

            // Класс сжатия/распаковки
            a_Archiver = new Archiver(this.w_ReadQueue, this.w_WriteDictionary, this.m_ThreadsEnd);
        }

        // Запуск потоков чтения, сжатия и записи
        public void RunningCompression()
        {
            Console.WriteLine("Сжатие файла "+s_SourceFile+"...");

            // Событие синхронизации для чтения
            m_ThreadsEnd[0] = new ManualResetEvent(false);
            // Запуск потока чтения
            Thread t_ThreadReading = new Thread(new ParameterizedThreadStart(w_WorkWithReadAndWrite.ReadFromSourceFileForCompress));
            t_ThreadReading.Start(0);

            //Запуск потоков сжатия
            Thread[] t_ThreadsCompress = new Thread[i_CountProcessors];

            for (int i = 1, j = 0; i < (i_CountProcessors + 1); i++, j++)
            {
                m_ThreadsEnd[i] = new ManualResetEvent(false);

                t_ThreadsCompress[j] = new Thread(new ParameterizedThreadStart(a_Archiver.Compress));
                t_ThreadsCompress[j].Start(i);
            }

            // Запуск потока записи
            m_ThreadsEnd[i_CountProcessors + 1] = new ManualResetEvent(false);

            Thread t_ThreadWriting = new Thread(new ParameterizedThreadStart(w_WorkWithReadAndWrite.WriteInResultingFileCompressedData));
            t_ThreadWriting.Start((i_CountProcessors + 1));

            // Ожидание окончании работ всех запущенных потоков
            for (int i = 0; i < m_ThreadsEnd.Length; i++)
            {
                m_ThreadsEnd[i].WaitOne();

                // Поток чтения завершил работу, установка маркера ожидания конца очереди чтения
                if (i == 0)
                {
                    w_ReadQueue.SetWaitingForEndOfQueue();
                }

                // Последний поток compress/decompress завершил работу, установка маркера ожидания конца словаря записи
                if (i == m_ThreadsEnd.Length - 2)
                {
                    w_WriteDictionary.SetWaitingForEndOfDictionary();
                }
            }

            // Если ошибка в одном из потоков, то работа не выполнена
            if (!w_WorkWithReadAndWrite.GetError && !a_Archiver.GetError)
            {
                Console.WriteLine("Сжатие файла "+s_SourceFile+" завершено! Архив находится по следующему пути: "+s_ResultingFile);
                Console.WriteLine("--------------------------------------");
            }
        }

        // Запуск потоков чтения, распаковки и записи
        public void RunningDecompression()
        {
            Console.WriteLine("Распаковка архива "+s_SourceFile+"...");

            // Событие синхронизации для чтения
            m_ThreadsEnd[0] = new ManualResetEvent(false);
            // Запуск потока чтения
            Thread t_ThreadReading = new Thread(new ParameterizedThreadStart(w_WorkWithReadAndWrite.ReadFromSourceFileForDecompress));
            t_ThreadReading.Start(0);

            //Запуск потоков распаковки
            Thread[] t_ThreadsDecompress = new Thread[i_CountProcessors];

            for (int i = 1, j = 0; i < (i_CountProcessors + 1); i++, j++)
            {
                m_ThreadsEnd[i] = new ManualResetEvent(false);

                t_ThreadsDecompress[j] = new Thread(new ParameterizedThreadStart(a_Archiver.Decompress));
                t_ThreadsDecompress[j].Start(i);
            }

            // Запуск потока записи
            m_ThreadsEnd[i_CountProcessors + 1] = new ManualResetEvent(false);

            Thread t_ThreadWriting = new Thread(new ParameterizedThreadStart(w_WorkWithReadAndWrite.WriteInResultingFileDecompressedData));
            t_ThreadWriting.Start((i_CountProcessors + 1));

            // Ожидание окончании работ всех запущенных потоков
            for (int i = 0; i < m_ThreadsEnd.Length; i++)
            {
                m_ThreadsEnd[i].WaitOne();

                // Поток чтения завершил работу, установка маркера ожидания конца очереди чтения
                if (i == 0)
                {
                    w_ReadQueue.SetWaitingForEndOfQueue();
                }

                // Последний поток compress/decompress завершил работу, установка маркера ожидания конца словаря записи
                if (i == m_ThreadsEnd.Length - 2)
                {
                    w_WriteDictionary.SetWaitingForEndOfDictionary();
                }
            }

            // Если ошибка в одном из потоков, то работа не выполнена
            if (!w_WorkWithReadAndWrite.GetError && !a_Archiver.GetError)
            {
                Console.WriteLine("Распаковка архива "+s_SourceFile+" завершена! Файл находится по следующему пути: "+s_ResultingFile);
                Console.WriteLine("--------------------------------------");
            }
        }
    }
}
