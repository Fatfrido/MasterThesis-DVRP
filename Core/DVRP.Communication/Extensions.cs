using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DVRP.Communication
{
    public static class Extensions
    {
        public static byte[] Serialize<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static T Deserialize<T>(this byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }
    }
}
