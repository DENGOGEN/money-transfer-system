using System;
using System.Collections.Generic;
using System.Linq;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Services
{
    public class UserService
    {
        private static UserService _instance;
        private static readonly object _lock = new object();
        private static List<User> _users = new List<User>();

        private UserService() { }

        public static UserService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new UserService();
                    }
                    return _instance;
                }
            }
        }

        public User GetUserByPhone(string phone)
        {
            return _users.FirstOrDefault(u => u.Phone == phone);
        }

        public User GetUserById(string id)
        {
            return _users.FirstOrDefault(u => u.Id == id);
        }

        public void AddUser(User user)
        {
            _users.Add(user);
        }

        public bool UpdateUser(User user)
        {
            var index = _users.FindIndex(u => u.Id == user.Id);
            if (index == -1) return false;
            _users[index] = user;
            return true;
        }

        public bool CheckDailyLimit(string userId, decimal amount, bool isVerified)
        {
            var user = GetUserById(userId);
            if (user == null) return false;

            decimal dailyLimit = isVerified ? 100000 : 10000;
            return (user.DailySentAmount + amount) <= dailyLimit;
        }

        public void UpdateDailySentAmount(string userId, decimal amount)
        {
            var user = GetUserById(userId);
            if (user != null)
            {
                user.DailySentAmount += amount;
            }
        }

        // Метод для инициализации тестовых данных
        public void InitTestData()
        {

            if (_users.Count == 0)
            {

                var authService = AuthService.Instance;

                // Тестовый пользователь 1
                var user1 = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Phone = "+79001234567",
                    PinHash = authService.ComputeSha256Hash("1234"),
                    IsVerified = true,
                    Status = "active",
                    DailySentAmount = 0
                };
                _users.Add(user1);

                // Тестовый пользователь 2
                var user2 = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Phone = "+79007654321",
                    PinHash = authService.ComputeSha256Hash("4321"),
                    IsVerified = false,
                    Status = "active",
                    DailySentAmount = 0
                };
                _users.Add(user2);

                
                WalletService.Instance.CreateWallet(user1.Id);
                WalletService.Instance.CreateWallet(user2.Id);

                
                WalletService.Instance.TopUp(user1.Id, 10000);
                WalletService.Instance.TopUp(user2.Id, 5000);

            }
        }
    }
}