using System;
using System.Collections.Generic;
using System.Linq;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Services
{
    public class TransferService
    {
        private static TransferService _instance;
        private static readonly object _lock = new object();

        private static List<Transaction> _transactions = new List<Transaction>();

        private TransferService() { }

        public static TransferService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new TransferService();
                    }
                    return _instance;
                }
            }
        }

        public Transaction CreateTransfer(string fromUserId, string toPhoneOrCard, decimal amount, string type)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                FromUserId = fromUserId,
                Amount = amount,
                Type = type,
                Status = TransactionStatus.Draft,
                Commission = CalculateCommission(amount, type),
                CreatedAt = DateTime.Now
            };

            // Проверка получателя
            if (type == "p2p")
            {
                var recipient = UserService.Instance.GetUserByPhone(toPhoneOrCard);
                if (recipient != null)
                {
                    transaction.ToUserId = recipient.Id;
                }
                else
                {
                    transaction.Status = TransactionStatus.WaitingRegistration;
                }
            }
            else if (type == "c2c")
            {
                transaction.ToCardMask = MaskCardNumber(toPhoneOrCard);
            }

            _transactions.Add(transaction);
            return transaction;
        }

        public bool ProcessTransfer(Transaction transaction, string twoFactorCode = null)
        {
            try
            {
                // Проверка 2FA
                if (AuthService.Instance.NeedTwoFactor(transaction.Amount))
                {
                    if (string.IsNullOrEmpty(twoFactorCode) || !AuthService.Instance.VerifyTwoFactorCode(twoFactorCode))
                    {
                        transaction.Status = TransactionStatus.Cancelled;
                        return false;
                    }
                    transaction.TwoFaConfirmed = true;
                }

                transaction.Status = TransactionStatus.Processing;

                // Получаем кошельки
                var fromWallet = WalletService.Instance.GetWalletByUserId(transaction.FromUserId);
                if (fromWallet == null)
                {
                    transaction.Status = TransactionStatus.Failed;
                    return false;
                }

                // Проверка достаточности средств
                decimal totalAmount = transaction.Amount + transaction.Commission;
                if (fromWallet.AvailableBalance < totalAmount)
                {
                    transaction.Status = TransactionStatus.Failed;
                    return false;
                }

                // P2P перевод внутри системы
                if (!string.IsNullOrEmpty(transaction.ToUserId))
                {
                    var toWallet = WalletService.Instance.GetWalletByUserId(transaction.ToUserId);
                    if (toWallet == null)
                    {
                        transaction.Status = TransactionStatus.Failed;
                        return false;
                    }

                    // Списание у отправителя
                    bool withdrawSuccess = WalletService.Instance.Withdraw(transaction.FromUserId, transaction.Amount);
                    if (!withdrawSuccess)
                    {
                        transaction.Status = TransactionStatus.Failed;
                        return false;
                    }

                    // Зачисление получателю
                    bool depositSuccess = WalletService.Instance.TopUp(transaction.ToUserId, transaction.Amount);
                    if (!depositSuccess)
                    {
                        // Откат: возвращаем средства отправителю
                        WalletService.Instance.TopUp(transaction.FromUserId, transaction.Amount);
                        transaction.Status = TransactionStatus.Failed;
                        return false;
                    }

                    // Списание комиссии
                    if (transaction.Commission > 0)
                    {
                        WalletService.Instance.Withdraw(transaction.FromUserId, transaction.Commission);
                    }

                    transaction.Status = TransactionStatus.Completed;

                    // Обновление суточного лимита
                    UserService.Instance.UpdateDailySentAmount(transaction.FromUserId, transaction.Amount);

                    return true;
                }

                // C2C перевод на карту (симуляция)
                if (!string.IsNullOrEmpty(transaction.ToCardMask))
                {
                    // Списание суммы + комиссии
                    bool withdrawSuccess = WalletService.Instance.Withdraw(transaction.FromUserId, transaction.Amount + transaction.Commission);
                    if (!withdrawSuccess)
                    {
                        transaction.Status = TransactionStatus.Failed;
                        return false;
                    }

                    // Симуляция внешнего перевода
                    bool externalSuccess = SimulateExternalTransfer();
                    if (!externalSuccess)
                    {
                        // Откат: возвращаем средства
                        WalletService.Instance.TopUp(transaction.FromUserId, transaction.Amount + transaction.Commission);
                        transaction.Status = TransactionStatus.Failed;
                        return false;
                    }

                    transaction.Status = TransactionStatus.Completed;
                    UserService.Instance.UpdateDailySentAmount(transaction.FromUserId, transaction.Amount);
                    return true;
                }

                transaction.Status = TransactionStatus.Failed;
                return false;
            }
            catch (Exception ex)
            {
                transaction.Status = TransactionStatus.Failed;
                System.Diagnostics.Debug.WriteLine($"Transfer error: {ex.Message}");
                return false;
            }
        }

        private decimal CalculateCommission(decimal amount, string type)
        {
            if (type == "p2p") return 0;
            if (type == "c2c") return Math.Round(amount * 0.01m, 2);
            return 0;
        }

        private string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 8)
                return "****";
            string clean = cardNumber.Replace(" ", "");
            if (clean.Length >= 16)
                return clean.Substring(0, 4) + " **** **** " + clean.Substring(clean.Length - 4);
            return "****";
        }

        private bool SimulateExternalTransfer()
        {

            System.Threading.Thread.Sleep(100); 
            return true;
        }

        public List<Transaction> GetUserTransactions(string userId)
        {
            return _transactions
                .Where(t => t.FromUserId == userId || t.ToUserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public void AddTransaction(Transaction transaction)
        {
            _transactions.Add(transaction);
        }

        public bool IsStatusAllowed(TransactionStatus from, TransactionStatus to)
        {
            switch (from)
            {
                case TransactionStatus.Draft:
                    return to == TransactionStatus.Waiting2FA || to == TransactionStatus.Processing;
                case TransactionStatus.Waiting2FA:
                    return to == TransactionStatus.Processing || to == TransactionStatus.Cancelled;
                case TransactionStatus.Processing:
                    return to == TransactionStatus.WaitingRegistration ||
                           to == TransactionStatus.Completed ||
                           to == TransactionStatus.Cancelled;
                case TransactionStatus.WaitingRegistration:
                    return to == TransactionStatus.Completed || to == TransactionStatus.Cancelled;
                default:
                    return false;
            }
        }
    }
}