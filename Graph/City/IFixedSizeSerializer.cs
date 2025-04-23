using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph.City
{
    using System.IO;

    /// <summary>
    /// Rozhraní pro serializátor, který zajišťuje zápis a čtení
    /// objektu typu T do/z proudu s PŘEDEM DEFINOVANOU PEVNOU velikostí v bytech.
    /// Implementace je zodpovědná za padding nebo ořezání dat.
    /// </summary>
    public interface IFixedSizeSerializer<T>
    {
        /// <summary>
        /// Vrací PEVNOU velikost v bytech, kterou serializovaný objekt typu T zabere.
        /// </summary>
        int GetFixedSize();

        /// <summary>
        /// Zapíše data objektu 'instance' do proudu. Musí zapsat PŘESNĚ GetFixedSize() bytů.
        /// Implementace musí řešit padding/ořezání.
        /// </summary>
        /// <param name="writer">BinaryWriter pro zápis.</param>
        /// <param name="instance">Instance objektu k zápisu (může být null).</param>
        void Write(BinaryWriter writer, T instance);

        /// <summary>
        /// Přečte PŘESNĚ GetFixedSize() bytů z proudu a deserializuje je na objekt typu T.
        /// </summary>
        /// <param name="reader">BinaryReader pro čtení.</param>
        /// <returns>Deserializovaná instance objektu T (může být null).</returns>
        T Read(BinaryReader reader);
    }
}
