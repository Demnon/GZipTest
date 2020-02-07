using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipTest
{
    // Блок данных, на которые делится входной файл
    public class DataBlock
    {
        // Id блока
        int i_Id;
        // Данные блока
        byte[] b_Data;
        // Длина данных блока (нужна для распаковки)
        int i_LengthDecompressedData;

        public DataBlock(int i_Id, byte[] b_Data, int i_LengthDecompressedData = 0)
        {
            this.i_Id = i_Id;
            this.b_Data = b_Data;
            this.i_LengthDecompressedData = i_LengthDecompressedData;
        }

        public int GetId
        {
            get
            {
                return i_Id;
            }
        }

        public byte[] GetData
        {
            get
            {
                return b_Data;
            }
        }

        public int GetLengthDecompressedData
        {
            get
            {
                return i_LengthDecompressedData;
            }
        }
    }
}
