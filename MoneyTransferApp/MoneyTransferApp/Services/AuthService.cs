using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Services
{
    public class AuthService
    {
        private static AuthService _instance;
        private static readonly object _lock = new object();

        private User _currentUser;

        private AuthService() { }

        public static AuthService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new AuthService();
                    }
                    return _instance;
                }
            }
        }

        public User CurrentUser => _currentUser;

        public bool Login(string phone, string pin)
        {
            var user = UserService.Instance.GetUserByPhone(phone);
            if (user == null) return false;

            var pinHash = ComputeSha256Hash(pin);
            if (user.PinHash != pinHash) return false;

            _currentUser = user;
            _currentUser.LastActivity = DateTime.Now;
            return true;
        }

        public void Logout()
        {
            _currentUser = null;
        }

        public bool Register(string phone, string pin)
        {
            if (UserService.Instance.GetUserByPhone(phone) != null)
                return false;

            var user = new User
            {
                Phone = phone,
                PinHash = ComputeSha256Hash(pin),
                IsVerified = false
            };

            UserService.Instance.AddUser(user);

            // Создать кошелёк для нового пользователя
            WalletService.Instance.CreateWallet(user.Id);

            return true;
        }

        public bool NeedTwoFactor(decimal amount)
        {
            return amount > 5000;
        }

        public bool VerifyTwoFactorCode(string code)
        {
            return code == "123456";
        }


        public string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}