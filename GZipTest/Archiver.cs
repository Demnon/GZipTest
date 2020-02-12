using System;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    // Класс, управляющий процессами сжатия/распаковки файлов
    public class Archiver
    { 
        // Очередь чтения
        WorkQueue w_ReadQueue;
        // Словарь записи
        WorkDictionary w_WriteDictionary;
        // События синхронизации всех потоков
        ManualResetEvent[] m_ThreadsEnd;
        // Маркер ошибки, произошедшей в любом потоке
        bool b_Error;

        public Archiver(WorkQueue w_ReadQueue, WorkDictionary w_WriteDictionary, ManualResetEvent[] m_ThreadsEnd)
        {
            b_Error = false;
            this.w_ReadQueue = w_ReadQueue;
            this.w_WriteDictionary = w_WriteDictionary;
            this.m_ThreadsEnd = m_ThreadsEnd;
        }

        public bool GetError
        {
            get
            {
                return b_Error;
            }
        }

        // Сжатие блоков данных
        public void Compress(object o_IndexThreads)
        {
            try
            {
                bool b_Test = false;

                // Получение блока из очереди чтения, его компрессия и добавление в словарь записи
                while (!b_Test)
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
                            w_WriteDictionary.AddDictionaryCompressedData(d_CompressedDatablock);
                        }
                    }
                } 
            }
            catch (Exception ex)
            {
                b_Error = true;
                Console.WriteLine("Ошибка во время сжатия! " + ex.Message);
                Console.WriteLine("--------------------------------------");
            }

            // Сообщение, что поток закончился
            m_ThreadsEnd[(int)o_IndexThreads].Set();
        }

        // Распаковка блоков данных
        public void Decompress(object o_IndexThreads)
        {
            try
            {
                bool b_Test = false;

                // Получение блока из очереди чтения, его распаковка и добавление в словарь записи
                while (!b_Test)
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
                                w_WriteDictionary.AddDictionaryCompressedData(d_DecompressedDatablock);
                            }
                        }
                    }
                } 
            }
            catch (Exception ex)
            {
                b_Error = true;
                Console.WriteLine("Ошибка во время распаковки! " + ex.Message);
                Console.WriteLine("--------------------------------------");
            }

            // Сообщение, что поток закончился
            m_ThreadsEnd[(int)o_IndexThreads].Set();
        }
    }
}
