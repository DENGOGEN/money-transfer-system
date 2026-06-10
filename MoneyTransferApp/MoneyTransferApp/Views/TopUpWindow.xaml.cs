using MoneyTransferApp.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MoneyTransferApp.Views
{
    public partial class TopUpWindow : Window
    {
        private readonly WalletService _walletService;
        private readonly AuthService _authService;

        public TopUpWindow()
        {
            InitializeComponent();
            _walletService = WalletService.Instance;
            _authService = AuthService.Instance;
        }

        private void QuickAmount_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                AmountBox.Text = button.Tag.ToString();
            }
        }

        private void CardNumberBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Простое форматирование без авто-форматирования
            // чтобы избежать конфликтов
        }

        private void TopUpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка суммы
                if (!decimal.TryParse(AmountBox.Text, out decimal amount) || amount <= 0)
                {
                    ErrorText.Text = "Введите корректную сумму";
                    return;
                }

                if (amount < 100)
                {
                    ErrorText.Text = "Минимальная сумма пополнения: 100 ₽";
                    return;
                }

                if (amount > 100000)
                {
                    ErrorText.Text = "Максимальная сумма пополнения: 100 000 ₽";
                    return;
                }

                // Проверка номера карты
                string cardNumber = CardNumberBox.Text.Replace(" ", "");
                if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 16)
                {
                    ErrorText.Text = "Введите корректный номер карты (16 цифр)";
                    return;
                }

                var user = _authService.CurrentUser;
                if (user == null)
                {
                    ErrorText.Text = "Пользователь не авторизован";
                    return;
                }

                // Пополнение баланса
                if (_walletService.TopUpByCard(user.Id, amount, cardNumber))
                {
                    MessageBox.Show($"Баланс успешно пополнен на {amount:F2} ₽!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    ErrorText.Text = "Ошибка при пополнении. Проверьте данные карты.";
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}