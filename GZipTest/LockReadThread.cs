using System;
using System.Threading;
using System.Diagnostics;

namespace GZipTest
{
    // Класс блокировки потока чтения, если размер буферов превышает размер доступной оперативной памяти
    public class LockReadThread
    {
        // Размер блока (в байтах)
        int i_BlockSize;
        // Число доступной оперативной памяти (в Мб)
        int i_SizeMemory;
        // Очередь чтения
        WorkQueue w_ReadQueue;
        // Словарь записи
        WorkDictionary w_WriteDictionary;
        // Событие синхронизации для блокировки потока чтения, если размер буферов превышает размер доступной оперативной памяти
        ManualResetEvent m_LockReadThread;

        public LockReadThread(int i_BlockSize, WorkQueue w_ReadQueue, WorkDictionary w_WriteDictionary)
        {
            this.i_BlockSize = i_BlockSize;
            this.w_ReadQueue = w_ReadQueue;
            this.w_WriteDictionary = w_WriteDictionary;

            // Получение доступной оперативной памяти
            PerformanceCounter p_PerformanceCounter = new PerformanceCounter("Memory", "Available MBytes");
            i_SizeMemory = (int)(p_PerformanceCounter.NextValue() * 0.5);

            m_LockReadThread = new ManualResetEvent(false);
        }

        // Остановка потока чтения, если размер очередей достиг размер допустимой памяти
        public void StopReadingThread()
        {
            int i_BlockSizeInMB = i_BlockSize / 1000000;
            int i_SizeMemoryReadQueue = w_ReadQueue.GetCountQueue * i_BlockSizeInMB;
            int i_SizeMemoryWriteQueue = w_WriteDictionary.GetCountDictionary * i_BlockSizeInMB;
            int i_SizeMemoryAllQueue = i_SizeMemoryReadQueue + i_SizeMemoryWriteQueue;

            // Поток чтения в режим ожидания
            if (i_SizeMemoryAllQueue >= i_SizeMemory)
            {
                m_LockReadThread.Reset();
                m_LockReadThread.WaitOne();
            }
        }

        // Возобновление потока чтения, когда размер очередей уменьшился
        public void StartReadingThreadAfterStop()
        {
            int i_BlockSizeInMB = i_BlockSize / 1000000;
            int i_SizeMemoryReadQueue = w_ReadQueue.GetCountQueue * i_BlockSizeInMB;
            int i_SizeMemoryWriteQueue = w_WriteDictionary.GetCountDictionary * i_BlockSizeInMB;
            int i_SizeMemoryAllQueue = i_SizeMemoryReadQueue + i_SizeMemoryWriteQueue;
            // Возобновление работы потока чтения
            if (i_SizeMemoryAllQueue < (i_SizeMemory * 0.4))
            {
                m_LockReadThread.Set();
            }
        }
    }
}
