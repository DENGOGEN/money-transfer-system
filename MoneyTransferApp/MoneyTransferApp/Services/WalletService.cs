using System;
using System.Collections.Generic;
using System.Linq;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Services
{
    public class WalletService
    {
        private static WalletService _instance;
        private static readonly object _lock = new object();

        private static List<Wallet> _wallets = new List<Wallet>();

        private WalletService() { }

        public static WalletService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new WalletService();
                    }
                    return _instance;
                }
            }
        }

        public Wallet GetWalletByUserId(string userId)
        {
            return _wallets.FirstOrDefault(w => w.UserId == userId);
        }

        public void CreateWallet(string userId)
        {
            if (GetWalletByUserId(userId) == null)
            {
                _wallets.Add(new Wallet { UserId = userId, Balance = 0, Frozen = 0 });
            }
        }

        public bool TopUp(string userId, decimal amount)
        {
            var wallet = GetWalletByUserId(userId);
            if (wallet == null) return false;

            wallet.Balance += amount;
            return true;
        }

        public bool Withdraw(string userId, decimal amount)
        {
            var wallet = GetWalletByUserId(userId);
            if (wallet == null) return false;

            if (wallet.AvailableBalance < amount) return false;

            wallet.Balance -= amount;
            return true;
        }

        public bool Freeze(string userId, decimal amount)
        {
            var wallet = GetWalletByUserId(userId);
            if (wallet == null) return false;

            if (wallet.AvailableBalance < amount) return false;

            wallet.Frozen += amount;
            wallet.Balance -= amount;
            return true;
        }

        public bool Unfreeze(string userId, decimal amount)
        {
            var wallet = GetWalletByUserId(userId);
            if (wallet == null) return false;

            wallet.Frozen -= amount;
            wallet.Balance += amount;
            return true;
        }

        public decimal GetBalance(string userId)
        {
            var wallet = GetWalletByUserId(userId);
            return wallet?.Balance ?? 0;
        }

        public decimal GetAvailableBalance(string userId)
        {
            var wallet = GetWalletByUserId(userId);
            return wallet?.AvailableBalance ?? 0;
        }

        // ==================== ПОПОЛНЕНИЕ ПО КАРТЕ ====================

        public bool TopUpByCard(string userId, decimal amount, string cardNumber)
        {
            var wallet = GetWalletByUserId(userId);
            if (wallet == null) return false;

            // Валидация
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Replace(" ", "").Length < 16)
                return false;

            if (amount < 100 || amount > 100000)
                return false;

            // Пополнение баланса
            wallet.Balance += amount;

            // Создаём транзакцию пополнения
            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                FromUserId = null,
                ToUserId = userId,
                ToCardMask = MaskCardNumber(cardNumber),
                Amount = amount,
                Commission = 0,
                Type = "topup",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.Now,
                TwoFaConfirmed = false
            };

            TransferService.Instance.AddTransaction(transaction);

            return true;
        }

        private string MaskCardNumber(string cardNumber)
        {
            var cleanNumber = cardNumber.Replace(" ", "");
            if (string.IsNullOrEmpty(cleanNumber) || cleanNumber.Length < 8)
                return "****";
            return cleanNumber.Substring(0, 4) + " **** **** " + cleanNumber.Substring(cleanNumber.Length - 4);
        }
    }
}