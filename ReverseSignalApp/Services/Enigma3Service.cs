using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace ReverseSignalApp.Services
{
    public class Enigma3Service
    {
        private readonly Dictionary<int, List<Dictionary<char, char>>> KeyDictionary;
        private const string AllowedChars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+[]{}|;:,.<>?/\\~` ";

        public Enigma3Service()
        {
            KeyDictionary = new Dictionary<int, List<Dictionary<char, char>>>();

            for (int i = 0; i < 10; i++)
            {
                var fixedKey = GenerateDeterministicKey(i);

                var plainToEnc = new Dictionary<char, char>();
                var encToPlain = new Dictionary<char, char>();

                for (int j = 0; j < AllowedChars.Length; j++)
                {
                    char plainChar = AllowedChars[j];
                    char encChar = fixedKey[j];

                    plainToEnc[plainChar] = encChar;
                    encToPlain[encChar] = plainChar;
                }

                KeyDictionary[i] = new List<Dictionary<char, char>> { plainToEnc, encToPlain };
            }
            var x = 5;
        }

        public string Encrypt(Guid id, string plaintext)
        {
            int keyIndex = GetKeyIndexFromGuid(id);
            var plainToEnc = KeyDictionary[keyIndex][0];
            var sb = new StringBuilder();

            foreach (char c in plaintext)
            {
                if (!plainToEnc.TryGetValue(c, out char encChar))
                    throw new InvalidOperationException($"Character '{c}' not allowed for encryption.");
                sb.Append(encChar);
            }

            return sb.ToString();
        }

        public string Decrypt(Guid id, string encryptedText)
        {
            int keyIndex = GetKeyIndexFromGuid(id);
            var encToPlain = KeyDictionary[keyIndex][1];
            var sb = new StringBuilder();

            foreach (char c in encryptedText)
            {
                if (!encToPlain.TryGetValue(c, out char plainChar))
                    throw new InvalidOperationException($"Character '{c}' not recognized in decryption.");
                sb.Append(plainChar);
            }

            return sb.ToString();
        }

        private int GetKeyIndexFromGuid(Guid id)
        {
            byte[] bytes = id.ToByteArray();
            BigInteger bigInt = new BigInteger(bytes.Concat(new byte[] { 0 }).ToArray());
            bigInt = BigInteger.Abs(bigInt);
            return (int)(bigInt % 10);
        }

        private string GenerateDeterministicKey(int keyIndex)
        {
            int seed = GetDeterministicSeed(keyIndex);
            var rng = new Random(seed);

            var allowedCharsList = AllowedChars.ToList();
            allowedCharsList = allowedCharsList.OrderBy(_ => rng.Next()).ToList();
            return new string(allowedCharsList.ToArray());
        }

        private int GetDeterministicSeed(int keyIndex)
        {
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(keyIndex.ToString()));
            return BitConverter.ToInt32(hashBytes, 0);
        }
    }
}
