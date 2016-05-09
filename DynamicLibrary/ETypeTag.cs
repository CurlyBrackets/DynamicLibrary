using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    public enum ETypeTag : byte
    {
        Boolean,
        Byte,
        ByteArray,
        Char,
        CharArray,
        Decimal,
        Double,
        Float,
        Int,
        Long,
        SByte,
        Short,
        String,
        Uint,
        Ulong,
        Ushort,
        Object,
        Null,
    }

    public static class TypeTagExtentions
    {

        private static readonly Dictionary<Type, ETypeTag> TagSwitch = new Dictionary<Type, ETypeTag>()
        {
            [typeof(bool)] = ETypeTag.Boolean,
            [typeof(byte)] = ETypeTag.Byte,
            [typeof(byte[])] = ETypeTag.ByteArray,
            [typeof(char)] = ETypeTag.Char,
            [typeof(char[])] = ETypeTag.CharArray,
            [typeof(decimal)] = ETypeTag.Decimal,
            [typeof(double)] = ETypeTag.Double,
            [typeof(float)] = ETypeTag.Float,
            [typeof(int)] = ETypeTag.Int,
            [typeof(long)] = ETypeTag.Long,
            [typeof(sbyte)] = ETypeTag.SByte,
            [typeof(short)] = ETypeTag.Short,
            [typeof(string)] = ETypeTag.String,
            [typeof(uint)] = ETypeTag.Uint,
            [typeof(ulong)] = ETypeTag.Ulong,
            [typeof(ushort)] = ETypeTag.Ushort,
        };
        private static readonly Dictionary<ETypeTag, Type> TypeSwitch = TagSwitch.ToDictionary(x => x.Value, x => x.Key);

        public static ETypeTag Tag(this Type t)
        {
            if (TagSwitch.ContainsKey(t))
                return TagSwitch[t];
            return ETypeTag.Object;
        }

        public static Type Type(this ETypeTag tag)
        {
            if (TypeSwitch.ContainsKey(tag))
                return TypeSwitch[tag];
            return null;
        }
    }

}
