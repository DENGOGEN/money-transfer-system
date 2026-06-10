using System;
using System.Windows;

namespace MoneyTransferApp.Services
{
    public class NotificationService
    {
        private static NotificationService _instance;
        private static readonly object _lock = new object();

        private NotificationService() { }

        public static NotificationService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new NotificationService();
                    }
                    return _instance;
                }
            }
        }

        public void SendSuccessNotification(string userId, decimal amount)
        {

            MessageBox.Show($"Перевод на сумму {amount:F2} руб. выполнен успешно!",
                "Уведомление",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public void Send2FACode(string userId, string code)
        {

            MessageBox.Show($"Ваш код подтверждения: {code}",
                "2FA Код",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public void SendRegistrationLink(string phone)
        {
            MessageBox.Show($"На номер {phone} отправлена ссылка для регистрации",
                "Приглашение",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}