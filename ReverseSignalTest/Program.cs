using System;
using ReverseSignalApp.Services; // Enigma3Service burada

namespace ReverseSignalTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "ArcCorp E3 Test Console";
            Console.WriteLine("=== 🧠 Enigma3Service Test ===\n");

            var e3 = new Enigma3Service();
            var testGuid = new Guid("2b150884-be96-4854-85b8-d7e63101ca46");

            Console.Write("Metni gir: ");
            string? plaintext = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(plaintext))
            {
                Console.WriteLine("❌ Boş metin girdin kardeş.");
                return;
            }

            try
            {
                string encrypted = e3.Encrypt(testGuid, plaintext);
                Console.WriteLine($"\n🔒 Şifreli: {encrypted}");

                string decrypted = e3.Decrypt(testGuid, encrypted);
                Console.WriteLine($"🔓 Çözülmüş: {decrypted}");

                Console.WriteLine("\n✅ Test tamamlandı, sistem stabil.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Hata: {ex.Message}");
            }

            Console.Write("Kapatmak için bir tuşa bas...");
            Console.ReadKey();
        }
    }
}
