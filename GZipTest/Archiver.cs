using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    // Класс, управляющий процессами сжатия/распаковки файлов, а также чтения и записи
    public class Archiver
    {
        // Имена входного и выходного файлов
        string s_SourceFile, s_ResultingFile;
        // Размер блока (в байтах)
        const int i_BlockSize = 1000000;
        // Число процессоров компьютера, доступных для работы
        int i_CountProcessors;
        //Число доступной оперативной памяти (в Мб)
        int i_SizeMemory;
        // Очередь чтения
        WorkQueue w_ReadQueue;
        // Очередь записи
        WorkQueue w_WriteQueue;
        // События синхронизации всех потоков
        ManualResetEvent[] m_ThreadsEnd;
        // Событие синхронизации для блокировки потока чтения, если размер очередей превышает размер доступной оперативной памяти
        ManualResetEvent m_LockReadThread;
        

        public Archiver(string s_SourceFile, string s_ResultingFile)
        {
            this.s_SourceFile = s_SourceFile;
            this.s_ResultingFile = s_ResultingFile;
            w_ReadQueue = new WorkQueue();
            w_WriteQueue = new WorkQueue();

            //Получение доступной оперативной памяти и числа процессоров
            Console.WriteLine("Получение характеристик компьютера...");

            PerformanceCounter p_PerformanceCounter = new PerformanceCounter("Memory", "Available MBytes");
            i_SizeMemory = (int)(p_PerformanceCounter.NextValue()*0.5);
            i_CountProcessors = Environment.ProcessorCount;

            Console.WriteLine("Характеристики компьютера получены.");

            // Количество потоков: два потока на чтение и запись соответственно, и количество потоков на compress/decompress,
            // Равное числу ядер компьютера
            m_ThreadsEnd = new ManualResetEvent[i_CountProcessors + 2];

            m_LockReadThread = new ManualResetEvent(false);

        }

        // Выполнение сжатия
        public void RunningCompression()
        {
            Console.WriteLine("Сжатие файла...");

            // Событие синхронизации для чтения
            m_ThreadsEnd[0] = new ManualResetEvent(false);
            // Запуск потока чтения
            Thread t_ThreadReading = new Thread(new ParameterizedThreadStart(ReadFromSourceFileForCompress));
            t_ThreadReading.Start(0);

            //Запуск потоков сжатия
            Thread[] t_ThreadsCompress = new Thread[i_CountProcessors];

            for (int i = 1,j=0; i < (i_CountProcessors + 1); i++,j++)
            {
                m_ThreadsEnd[i] = new ManualResetEvent(false);

                t_ThreadsCompress[j] = new Thread(new ParameterizedThreadStart(Compress));
                t_ThreadsCompress[j].Start(i);
            }

            // Запуск потока записи
            m_ThreadsEnd[i_CountProcessors+1] = new ManualResetEvent(false);
            
            Thread t_ThreadWriting = new Thread(new ParameterizedThreadStart(WriteInResultingFileCompressedData));
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
                
                // Последний поток compress/decompress завершил работу, установка маркера ожидания конца очереди записи
                if (i == m_ThreadsEnd.Length-2)
                {
                    w_WriteQueue.SetWaitingForEndOfQueue();
                }
            }

            Console.WriteLine("Сжатие файла завершено!");
        }

        // Выполнение распаковки
        public void RunningDecompression()
        {
            Console.WriteLine("Распаковка архива...");

            // Событие синхронизации для чтения
            m_ThreadsEnd[0] = new ManualResetEvent(false);
            // Запуск потока чтения
            Thread t_ThreadReading = new Thread(new ParameterizedThreadStart(ReadFromSourceFileForDecompress));
            t_ThreadReading.Start(0);

            //Запуск потоков распаковки
            Thread[] t_ThreadsDecompress = new Thread[i_CountProcessors];

            for (int i = 1, j = 0; i < (i_CountProcessors + 1); i++, j++)
            {
                m_ThreadsEnd[i] = new ManualResetEvent(false);

                t_ThreadsDecompress[j] = new Thread(new ParameterizedThreadStart(Decompress));
                t_ThreadsDecompress[j].Start(i);
            }

            // Запуск потока записи
            m_ThreadsEnd[i_CountProcessors + 1] = new ManualResetEvent(false);

            Thread t_ThreadWriting = new Thread(new ParameterizedThreadStart(WriteInResultingFileDecompressedData));
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

                // Последний поток compress/decompress завершил работу, установка маркера ожидания конца очереди записи
                if (i == m_ThreadsEnd.Length - 2)
                {
                    w_WriteQueue.SetWaitingForEndOfQueue();
                }
            }

            Console.WriteLine("Распаковка архива завершена!");
        }

        // Чтение исходного файла для сжатия
        private void ReadFromSourceFileForCompress(object o_IndexThreads)
        {
            try
            {
                using (FileStream f_SourceFileForCompress = new FileStream(s_SourceFile, FileMode.Open))
                {
                    byte[] b_ReadDataBlock = null;
                    int ReadBytes = 0;

                    // Считывание файла и деление его на блоки заданного размера, добавление блоков в очередь
                    while (f_SourceFileForCompress.Position < f_SourceFileForCompress.Length)
                    {
                        long l_CurrentLength = f_SourceFileForCompress.Length - f_SourceFileForCompress.Position;

                        // Если размер файла меньше заданного размера блока
                        if (l_CurrentLength <= i_BlockSize)
                        {
                            ReadBytes = (int)l_CurrentLength;
                        }
                        else
                        {
                            ReadBytes = i_BlockSize;
                        }

                        b_ReadDataBlock = new byte[ReadBytes];

                        f_SourceFileForCompress.Read(b_ReadDataBlock, 0, ReadBytes);

                        w_ReadQueue.AddInQueueUncompressedData(b_ReadDataBlock);

                        // Остановка потока чтения, если переполнение
                        StopReadingThread();
                    }
                }

                // Сообщение главному потоку, что поток чтения закончился
                m_ThreadsEnd[(int)o_IndexThreads].Set();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка во время чтения! " + ex.Message);
            }
        }

        // Чтение исходного файла для распаковки
        private void ReadFromSourceFileForDecompress(object o_IndexThreads)
        {
            try
            {
                using (FileStream f_SourceFileForDeCompress = new FileStream(s_SourceFile, FileMode.Open))
                {
                    // Задаваемый id считываемых блоков
                    int i_IDReadDatablock = 0;
                    // Считывание файла поблочно, добавление блоков в очередь
                    while (f_SourceFileForDeCompress.Position < f_SourceFileForDeCompress.Length)
                    {
                        byte[] b_LengthReadDataBlock = new byte[8];
                        f_SourceFileForDeCompress.Read(b_LengthReadDataBlock, 0, b_LengthReadDataBlock.Length);
                        // Получаем длину блока
                        int i_LengthReadDataBlock = BitConverter.ToInt32(b_LengthReadDataBlock, 4);

                        byte[] b_ReadDatablock = new byte[i_LengthReadDataBlock];
                        b_LengthReadDataBlock.CopyTo(b_ReadDatablock, 0);

                        f_SourceFileForDeCompress.Read(b_ReadDatablock, 8, i_LengthReadDataBlock - 8);
                        int i_LengthDecompressedData = BitConverter.ToInt32(b_ReadDatablock, i_LengthReadDataBlock - 4);

                        DataBlock d_ReadDatablock = new DataBlock(i_IDReadDatablock, b_ReadDatablock, i_LengthDecompressedData);
                        w_ReadQueue.AddQueueCompressedData(d_ReadDatablock);
                        i_IDReadDatablock++;

                        // Остановка потока чтения, если переполнение
                        StopReadingThread();
                    }
                }

                // Сообщение главному потоку, что поток чтения закончился
                m_ThreadsEnd[(int)o_IndexThreads].Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка во время чтения! " + ex.Message);
            }
        }

        // Запись в файл сжатых данных
        private void WriteInResultingFileCompressedData(object o_IndexThreads)
        {
            try
            {
                using (FileStream f_ResultingFileCompressedData = new FileStream(s_ResultingFile, FileMode.Append))
                {
                    bool b_Test = false;

                    // Получение сжатых блоков из очереди и запись в файл
                    while (b_Test == false)
                    {
                        DataBlock d_DataBlock = w_WriteQueue.DeleteFromQueue();

                        if (d_DataBlock == null)
                        {
                            b_Test = true;
                        }
                        else
                        {
                            BitConverter.GetBytes(d_DataBlock.GetData.Length).CopyTo(d_DataBlock.GetData, 4);
                            f_ResultingFileCompressedData.Write(d_DataBlock.GetData, 0, d_DataBlock.GetData.Length);
                        }

                        //Запуск потока чтения, если он был остановлен из-за переполнения
                        StartReadingThreadAfterStop();
                    }
                }

                // Сообщение главному потоку, что поток записи закончился
                m_ThreadsEnd[(int)o_IndexThreads].Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка во время записи! " + ex.Message);
            }
        }

        // Запись в файл распакованных данных
        private void WriteInResultingFileDecompressedData(object o_IndexThreads)
        {
            try
            {
                using (FileStream f_ResultingFileDeCompressedData = new FileStream(s_ResultingFile, FileMode.Append))
                {
                    bool b_Test = false;

                    // Получение распакованных блоков из очереди и запись в файл
                    while (b_Test == false)
                    {
                        DataBlock d_DataBlock = w_WriteQueue.DeleteFromQueue();

                        if (d_DataBlock == null)
                        {
                            b_Test = true;
                        }
                        else
                        {
                            f_ResultingFileDeCompressedData.Write(d_DataBlock.GetData, 0, d_DataBlock.GetData.Length);
                        }

                        //Запуск потока чтения, если он был остановлен из-за переполнения
                        StartReadingThreadAfterStop();
                    }
                }

                // Сообщение главному потоку, что поток записи закончился
                m_ThreadsEnd[(int)o_IndexThreads].Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка во время записи! " + ex.Message);
            }
        }

        // Сжатие блоков данных
        private void Compress(object o_IndexThreads)
        {
            try
            {
                bool b_Test = false;

                // Получение блока из очереди чтения, его компрессия и добавление в очередь записи
                while (b_Test == false)
                {
                    DataBlock d_DataBlock = w_ReadQueue.DeleteFromQueue();

                    if (d_DataBlock == null)
                    {
                        b_Test = true;
                    }
                    else
                    {
                        using (MemoryStream m_MemoryStream = new MemoryStream())
                        {
                            using (GZipStream g_GZipStream = new GZipStream(m_MemoryStream, CompressionMode.Compress))
                            {
                                g_GZipStream.Write(d_DataBlock.GetData, 0, d_DataBlock.GetData.Length);
                            }

                            byte[] b_CompressedData = m_MemoryStream.ToArray();

                            DataBlock d_CompressedDatablock = new DataBlock(d_DataBlock.GetId, b_CompressedData);
                            w_WriteQueue.AddQueueCompressedData(d_CompressedDatablock);
                        }
                    }
                }
                // Сообщение, что поток закончился
                m_ThreadsEnd[(int)o_IndexThreads].Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка во время сжатия! " + ex.Message);
            }
        }

        // Распаковка блоков данных
        private void Decompress(object o_IndexThreads)
        {
            try
            {
                bool b_Test = false;

                // Получение блока из очереди чтения, его распаковка и добавление в очередь записи
                while (b_Test == false)
                {
                    DataBlock d_DataBlock = w_ReadQueue.DeleteFromQueue();
                    if (d_DataBlock == null)
                    {
                        b_Test = true;
                    }
                    else
                    {
                        using (MemoryStream m_MemoryStream = new MemoryStream(d_DataBlock.GetData))
                        {
                            using (GZipStream g_GZipStream = new GZipStream(m_MemoryStream, CompressionMode.Decompress))
                            {
                                byte[] b_DecompressData = new byte[d_DataBlock.GetLengthDecompressedData];
                                g_GZipStream.Read(b_DecompressData, 0, b_DecompressData.Length);

                                DataBlock d_DecompressedDatablock = new DataBlock(d_DataBlock.GetId, b_DecompressData);
                                w_WriteQueue.AddQueueCompressedData(d_DecompressedDatablock);
                            }
                        }
                    }
                }
                // Сообщение, что поток закончился
                m_ThreadsEnd[(int)o_IndexThreads].Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка во время сжатия! " + ex.Message);
            }
        }

        // Остановка потока чтения, если размер очередей достиг размер допустимой памяти
        private void StopReadingThread()
        {
            int i_BlockSizeInMB = i_BlockSize / 1000000;
            int i_SizeMemoryReadQueue = w_ReadQueue.GetCountQueue * i_BlockSizeInMB;
            int i_SizeMemoryWriteQueue = w_WriteQueue.GetCountQueue * i_BlockSizeInMB;
            int i_SizeMemoryAllQueue = i_SizeMemoryReadQueue + i_SizeMemoryWriteQueue;

            // Поток чтения в режим ожидания
            if (i_SizeMemoryAllQueue>=i_SizeMemory)
            {
                m_LockReadThread.Reset();
                m_LockReadThread.WaitOne();
            }
        }

        // Возобновление потока чтения, когда размер очередей уменьшился
        private void StartReadingThreadAfterStop()
        {
            int i_BlockSizeInMB = i_BlockSize / 1000000;
            int i_SizeMemoryReadQueue = w_ReadQueue.GetCountQueue * i_BlockSizeInMB;
            int i_SizeMemoryWriteQueue = w_WriteQueue.GetCountQueue * i_BlockSizeInMB;
            int i_SizeMemoryAllQueue = i_SizeMemoryReadQueue + i_SizeMemoryWriteQueue;
            // Возобновление работы потока чтения
            if (i_SizeMemoryAllQueue < (i_SizeMemory*0.4))
            {
                m_LockReadThread.Set();
            }
        }
    }
}
