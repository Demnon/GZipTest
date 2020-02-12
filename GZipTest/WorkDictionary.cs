using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    // Методы работы со словарем из блоков данных: добавление и удаление из словаря
    public class WorkDictionary
    {
        // Поле словарь
        Dictionary<int,DataBlock> d_Dictionary;
        // Id блоков в словаре
        int i_IdDataBlock;
        // Объект блокировки для работы класса Monitor 
        object o_Locker;
        // Переменная типа bool - маркер ожидания конца словаря, если он поднят, значит, словарь наполняться больше не будет
        bool b_WaitingForEndOfDictionary;

        public WorkDictionary()
        {
            d_Dictionary = new Dictionary<int, DataBlock>();
            i_IdDataBlock = 0;
            o_Locker = new object();
        }

        public int GetCountDictionary
        {
            get
            {
                return d_Dictionary.Count;
            }
        }

        // Запись в словарь сжатых данных
        public void AddDictionaryCompressedData(DataBlock d_DataBlock)
        {
            lock (o_Locker)
            {
                d_Dictionary.Add(d_DataBlock.GetId,d_DataBlock);

                Monitor.PulseAll(o_Locker);
            }
        }

        // Получение данных из словаря (объект обслужен)
        public DataBlock DeleteFromDictionary()
        {
            lock (o_Locker)
            {
                while (d_Dictionary.Count == 0 && !b_WaitingForEndOfDictionary)
                {
                    Monitor.Wait(o_Locker);
                }

                if (d_Dictionary.Count == 0)
                {
                    return null;
                }

                while (d_Dictionary.ContainsKey(i_IdDataBlock) == false && !b_WaitingForEndOfDictionary)
                {
                    Monitor.Wait(o_Locker);
                }

                DataBlock d_DataBlock = d_Dictionary[i_IdDataBlock];
                d_Dictionary.Remove(i_IdDataBlock);
                i_IdDataBlock++;
                return d_DataBlock;
            }
        }

        // Установление маркера
        public void SetWaitingForEndOfDictionary()
        {
            lock (o_Locker)
            {
                b_WaitingForEndOfDictionary = true;

                Monitor.PulseAll(o_Locker);
            }
        }
    }
}
