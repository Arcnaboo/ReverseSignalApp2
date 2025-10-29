namespace ReverseSignalApp.Services
{
    public class AuthService
    {
        private static string user = "Admin";
        private static string pass = "Berat1234";


        public static async Task<bool> LoginAsync(string username, string password)
        {
            await Task.Delay(100);

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return false;
            }
            else if (username == user && password == pass)
            {
                return true;
            }
            return false;

        }
    }
}
