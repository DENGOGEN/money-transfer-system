using System;
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
        private readonly UserService _userService;

        private AuthService()
        {
            _userService = UserService.Instance;
        }

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
            var user = _userService.GetUserByPhone(phone);
            if (user == null) return false;

            var pinHash = ComputeSha256Hash(pin);
            if (user.PinHash != pinHash) return false;

            _currentUser = user;
            _currentUser.LastActivity = DateTime.Now;
            return true;
        }

        // Метод для получения пользователя после регистрации
        public User LoginAndGetUser(string phone, string pin)
        {
            var user = _userService.GetUserByPhone(phone);
            if (user == null) return null;

            var pinHash = ComputeSha256Hash(pin);
            if (user.PinHash != pinHash) return null;

            _currentUser = user;
            _currentUser.LastActivity = DateTime.Now;
            return _currentUser;
        }

        public void Logout()
        {
            _currentUser = null;
        }

        public bool Register(string phone, string pin)
        {
            if (_userService.GetUserByPhone(phone) != null)
                return false;

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Phone = phone,
                PinHash = ComputeSha256Hash(pin),
                IsVerified = false,
                Status = "active",
                DailySentAmount = 0,
                LastActivity = DateTime.Now
            };

            _userService.AddUser(user);

            // Создать кошелёк для нового пользователя
            WalletService.Instance.CreateWallet(user.Id);

            // Выдать виртуальную карту
            Card card = CardService.Instance.CreateCardForUser(user.Id);

            // Сохранить ID карты в профиле пользователя
            user.AssignedCardId = card.Id;
            _userService.UpdateUser(user);

            return true;
        }

        public bool NeedTwoFactor(decimal amount)
        {
            return amount > 5000;
        }

        public bool VerifyTwoFactorCode(string code)
        {
            // В реальном приложении - проверка через SMS
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