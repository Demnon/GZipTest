using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest
{
    // Методы работы с очередью из блоков данных: добавление и удаление из очередей
    public class WorkQueue
    {
        // Поле очередь
        Queue<DataBlock> q_Queue;
        // Id добавляемых в очереди блоков
        int i_IdDataBlock;
        // Объект блокировки для работы класса Monitor 
        object o_Locker;
        // Переменная типа bool - маркер ожидания конца очереди, если он поднят, значит, любая очередь наполняться больше не будет
        bool b_WaitingForEndOfQueue;

        public WorkQueue()
        {
            q_Queue = new Queue<DataBlock>();
            i_IdDataBlock = 0;
            o_Locker = new object();
            b_WaitingForEndOfQueue = false;
        }

        public int GetCountQueue
        {
            get
            {
                return q_Queue.Count;
            }
        }

        public bool GetWaitingForEndOfQueue
        {
            get
            {
                return b_WaitingForEndOfQueue;
            }
        }

        // Добавление в очередь несжатых данных (например, считанных из исходного несжатого файла)
        public void AddInQueueUncompressedData(byte[] b_Data)
        {
            // Блокировка
            lock (o_Locker)
            {
                DataBlock d_DataBlock = new DataBlock(i_IdDataBlock, b_Data);
                q_Queue.Enqueue(d_DataBlock);
                i_IdDataBlock++;

                // Сигнал потокам в режиме ожидания
                Monitor.PulseAll(o_Locker); 
            }
        }

        // Запись в очередь сжатых данных
        public void AddQueueCompressedData(DataBlock d_DataBlock)
        {
            int i_Id = d_DataBlock.GetId;

            lock (o_Locker)
            {
                while (i_Id != i_IdDataBlock)
                {
                    Monitor.Wait(o_Locker);
                }

                q_Queue.Enqueue(d_DataBlock);
                i_IdDataBlock++;

                Monitor.PulseAll(o_Locker);
            }
        }

        // Удаление из любой очереди (объект обслужен)
        public DataBlock DeleteFromQueue()
        {
            lock (o_Locker)
            {
                while (q_Queue.Count == 0 && b_WaitingForEndOfQueue == false)
                {
                    Monitor.Wait(o_Locker);
                }

                if (q_Queue.Count == 0)
                {
                    return null;
                }
                return q_Queue.Dequeue();
            }
        }

        // Установление маркера
        public void SetWaitingForEndOfQueue()
        {
            lock (o_Locker)
            {
                b_WaitingForEndOfQueue = true;

                Monitor.PulseAll(o_Locker);
            }
        }
    }
}
