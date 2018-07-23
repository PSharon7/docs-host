using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace docs.host
{
    public class HashUtility
    {
        public static string GetSha1HashString(string input)
            => GetSha1HashString(new MemoryStream(Encoding.UTF8.GetBytes(input)));

        public static string GetSha1HashString(Stream stream)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var hash = sha1.ComputeHash(stream);
                var formatted = new StringBuilder(2 * hash.Length);
                foreach (byte b in hash)
                {
                    formatted.AppendFormat("{0:x2}", b);
                }

                return formatted.ToString();
            }
        }
    }
}
