using System;
using System.Threading;
using System.IO;

namespace GZipTest
{
    // Класс чтения/записи файлов
    class WorkWithReadAndWrite
    {
        // Имена входного и выходного файлов
        string s_SourceFile, s_ResultingFile;
        // Размер блока (в байтах)
        int i_BlockSize;
        // Очередь чтения
        WorkQueue w_ReadQueue;
        // Словарь записи
        WorkDictionary w_WriteDictionary;
        // События синхронизации всех потоков
        ManualResetEvent[] m_ThreadsEnd;
        // Блокировка потока чтения при переполнении буферов
        LockReadThread l_LockReadThread;
        // Маркер ошибки, произошедшей в любом потоке
        bool b_Error;

        public WorkWithReadAndWrite(string s_SourceFile, string s_ResultingFile, int i_BlockSize, WorkQueue w_ReadQueue, WorkDictionary w_WriteDictionary, ManualResetEvent[] m_ThreadsEnd)
        {
            b_Error = false;
            this.s_SourceFile = s_SourceFile;
            this.s_ResultingFile = s_ResultingFile;
            this.i_BlockSize = i_BlockSize;
            this.w_ReadQueue = w_ReadQueue;
            this.w_WriteDictionary = w_WriteDictionary;

            this.m_ThreadsEnd = m_ThreadsEnd;
            l_LockReadThread = new LockReadThread(i_BlockSize,this.w_ReadQueue,this.w_WriteDictionary);
        }

        public bool GetError
        {
            get
            {
                return b_Error;
            }
        }

        // Чтение исходного файла для сжатия
        public void ReadFromSourceFileForCompress(object o_IndexThreads)
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
                        l_LockReadThread.StopReadingThread();
                    }
                }
            }
            catch (Exception ex)
            {
                b_Error = true;
                Console.WriteLine("Ошибка во время чтения! " + ex.Message);
                Console.WriteLine("--------------------------------------");
            }

            // Сообщение главному потоку, что поток чтения закончился
            m_ThreadsEnd[(int)o_IndexThreads].Set();
        }

        // Чтение исходного файла для распаковки
        public void ReadFromSourceFileForDecompress(object o_IndexThreads)
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
                        l_LockReadThread.StopReadingThread();
                    }
                }   
            }
            catch (Exception ex)
            {
                b_Error = true;
                Console.WriteLine("Ошибка во время чтения! " + ex.Message);
                Console.WriteLine("--------------------------------------");
            }

            // Сообщение главному потоку, что поток чтения закончился
            m_ThreadsEnd[(int)o_IndexThreads].Set();
        }

        // Запись в файл сжатых данных
        public void WriteInResultingFileCompressedData(object o_IndexThreads)
        {
            try
            {
                using (FileStream f_ResultingFileCompressedData = new FileStream(s_ResultingFile, FileMode.Append))
                {
                    bool b_Test = false;

                    // Получение сжатых блоков из словаря и запись в файл
                    while (!b_Test)
                    {
                        DataBlock d_DataBlock = w_WriteDictionary.DeleteFromDictionary();

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
                        l_LockReadThread.StartReadingThreadAfterStop();
                    }
                }   
            }
            catch (Exception ex)
            {
                b_Error = true;
                Console.WriteLine("Ошибка во время записи! " + ex.Message);
                Console.WriteLine("--------------------------------------");
            }

            // Сообщение главному потоку, что поток записи закончился
            m_ThreadsEnd[(int)o_IndexThreads].Set();
        }

        // Запись в файл распакованных данных
        public void WriteInResultingFileDecompressedData(object o_IndexThreads)
        {
            try
            {
                using (FileStream f_ResultingFileDeCompressedData = new FileStream(s_ResultingFile, FileMode.Append))
                {
                    bool b_Test = false;

                    // Получение распакованных блоков из очереди и запись в файл
                    while (!b_Test)
                    {
                        DataBlock d_DataBlock = w_WriteDictionary.DeleteFromDictionary();

                        if (d_DataBlock == null)
                        {
                            b_Test = true;
                        }
                        else
                        {
                            f_ResultingFileDeCompressedData.Write(d_DataBlock.GetData, 0, d_DataBlock.GetData.Length);
                        }

                        //Запуск потока чтения, если он был остановлен из-за переполнения
                        l_LockReadThread.StartReadingThreadAfterStop();
                    }
                }  
            }
            catch (Exception ex)
            {
                b_Error = true;
                Console.WriteLine("Ошибка во время записи! " + ex.Message);
                Console.WriteLine("--------------------------------------");
            }

            // Сообщение главному потоку, что поток записи закончился
            m_ThreadsEnd[(int)o_IndexThreads].Set();
        }
    }
}
