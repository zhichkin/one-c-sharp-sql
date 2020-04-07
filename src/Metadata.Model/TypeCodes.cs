using System;
using System.Collections.Generic;

namespace OneCSharp.Metadata.Model
{
    public enum TypeCodes
    {
        Empty = 0,
        Boolean = -1,
        Char = -2,
        SByte = -3,
        Byte = -4,
        Int16 = -5,
        UInt16 = -6,
        Int32 = -7,
        UInt32 = -8,
        Int64 = -9,
        UInt64 = -10,
        Single = -12,
        Double = -13,
        Decimal = -14,
        DateTime = -15,
        Guid = -16,
        String = -17,
        Binary = -18,
        Object = -19,
        DBNull = -20,
        List = -21,
        ObjRef = -22
    }
    public sealed class MetaObjectTypeCodes
    {
        private readonly Dictionary<int, MetaObject> map = new Dictionary<int, MetaObject>();
        public void Register(MetaObject @object)
        {
            if (@object == null) throw new ArgumentNullException(nameof(@object));
            if(@object.TypeCode <= 0) throw new ArgumentOutOfRangeException(nameof(@object.TypeCode));
            _ = map.TryAdd(@object.TypeCode, @object);
        }
        public void Map(int code, MetaObject @object)
        {
            if (@object == null) throw new ArgumentNullException(nameof(@object));
            if (code <= 0) throw new ArgumentOutOfRangeException(nameof(code));
            _ = map.TryAdd(code, @object);
        }
        public MetaObject Find(int code)
        {
            if (code <= 0) throw new ArgumentOutOfRangeException(nameof(code));
            _ = map.TryGetValue(code, out MetaObject @object);
            return @object;
        }
    }
}