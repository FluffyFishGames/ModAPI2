using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    internal static class Checksum
    {
        public static string Create(byte[] data, int hashParts = 2)
        {
            var lenPer = data.Length / hashParts;
            var start = 0;
            byte[] hash = new byte[hashParts * 8];
            for (var i = 0; i < hashParts; i++)
            {
                var h = xxHash64.Hash(new ReadOnlySpan<byte>(data, start, i == hashParts - 1 ? data.Length - start : lenPer));
                var hb = BitConverter.GetBytes(h);
                for (var j = 0; j < hb.Length; j++)
                    hash[i * 8 + j] = hb[j];
                start += lenPer;
            }

            var stringBuilder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                stringBuilder.Append(hash[i].ToString("x2"));
            }
            var hashStr = stringBuilder.ToString();
            return hashStr;
        }
    }
}
