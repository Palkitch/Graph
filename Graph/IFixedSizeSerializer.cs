using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph
{
    using System.IO;


    public interface IFixedSizeSerializer<T>
    {
        int GetFixedSize();
        void Write(BinaryWriter writer, T instance);
        T Read(BinaryReader reader);
    }
}
