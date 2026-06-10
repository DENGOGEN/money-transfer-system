using MoneyTransferApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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
                // Для C2C сохраняем маскированный номер карты
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

                // Получаем кошелёк отправителя
                var fromWallet = WalletService.Instance.GetWalletByUserId(transaction.FromUserId);
                if (fromWallet == null)
                {
                    transaction.Status = TransactionStatus.Failed;
                    return false;
                }

                decimal totalAmount = transaction.Amount + transaction.Commission;
                if (fromWallet.AvailableBalance < totalAmount)
                {
                    transaction.Status = TransactionStatus.Failed;
                    return false;
                }

                // ========== P2P ПЕРЕВОД (по номеру телефона) ==========
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

                    // Списание комиссии (для P2P комиссия = 0)
                    if (transaction.Commission > 0)
                    {
                        WalletService.Instance.Withdraw(transaction.FromUserId, transaction.Commission);
                    }

                    transaction.Status = TransactionStatus.Completed;
                    UserService.Instance.UpdateDailySentAmount(transaction.FromUserId, transaction.Amount);

                    return true;
                }

                // ========== C2C ПЕРЕВОД (на банковскую карту) ==========
                if (!string.IsNullOrEmpty(transaction.ToCardMask))
                {
                    // Списание суммы + комиссии у отправителя
                    bool withdrawSuccess = WalletService.Instance.Withdraw(transaction.FromUserId, transaction.Amount + transaction.Commission);
                    if (!withdrawSuccess)
                    {
                        transaction.Status = TransactionStatus.Failed;
                        return false;
                    }

                    // В РЕАЛЬНОМ ПРИЛОЖЕНИИ: здесь был бы запрос к банковскому API
                    // Для ТЕСТИРОВАНИЯ: просто логируем перевод на карту

                    // Сохраняем информацию о переводе на карту
                    System.Diagnostics.Debug.WriteLine($"C2C Перевод: {transaction.Amount} руб. на карту {transaction.ToCardMask}");

                    // Опционально: добавить уведомление пользователю
                    NotificationService.Instance.SendSuccessNotification(transaction.FromUserId, transaction.Amount);

                    transaction.Status = TransactionStatus.Completed;
                    UserService.Instance.UpdateDailySentAmount(transaction.FromUserId, transaction.Amount);

                    // Показываем сообщение, что перевод на карту выполнен (в реальности деньги придут на карту)
                    MessageBox.Show($"Перевод на карту {transaction.ToCardMask} на сумму {transaction.Amount:F2} ₽ выполнен.\n\n" +
                        $"Комиссия: {transaction.Commission:F2} ₽\n" +
                        $"Списано с баланса: {transaction.TotalAmount:F2} ₽\n\n" +
                        $"ВНИМАНИЕ: Это тестовый режим. В реальном приложении деньги поступят на указанную карту.",
                        "Перевод на карту", MessageBoxButton.OK, MessageBoxImage.Information);

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
            if (type == "c2c") return Math.Round(amount * 0.01m, 2); // 1% комиссия
            return 0;
        }

        private string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber)) return "****";

            // Удаляем пробелы
            string clean = cardNumber.Replace(" ", "");

            if (clean.Length >= 16)
            {
                return clean.Substring(0, 4) + " **** **** " + clean.Substring(clean.Length - 4);
            }
            else if (clean.Length >= 8)
            {
                return clean.Substring(0, 4) + " **** " + clean.Substring(clean.Length - 4);
            }

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