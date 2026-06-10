using MoneyTransferApp.Models;
using MoneyTransferApp.Services;
using MoneyTransferApp.Views;
using System;
using System.Windows;
using System.Windows.Threading;

namespace MoneyTransferApp
{
    public partial class MainWindow : Window
    {
        private readonly TransferService _transferService;
        private readonly WalletService _walletService;
        private readonly AuthService _authService;
        private Transaction _pendingTransaction;
        private DispatcherTimer _balanceTimer;

        public MainWindow()
        {
            InitializeComponent();
            _transferService = TransferService.Instance;
            _walletService = WalletService.Instance;
            _authService = AuthService.Instance;

            LoadUserData();
            LoadHistory();
            StartBalanceRefreshTimer();
        }

        private void LoadUserData()
        {
            var user = _authService.CurrentUser;
            if (user != null)
            {
                UserPhoneText.Text = user.Phone;
                ProfilePhone.Text = user.Phone;
                ProfileVerified.Text = user.IsVerified ? "Подтверждён" : "Не подтверждён";
                RefreshBalance();
            }
        }

        private void RefreshBalance()
        {
            var user = _authService.CurrentUser;
            if (user != null)
            {
                var balance = _walletService.GetBalance(user.Id);
                BalanceText.Text = $"Баланс: {balance:F2} ₽";
                ProfileBalance.Text = $"{balance:F2} ₽";
            }
        }

        private void LoadHistory()
        {
            var user = _authService.CurrentUser;
            if (user != null)
            {
                var history = _transferService.GetUserTransactions(user.Id);
                HistoryGrid.ItemsSource = history;
            }
        }

        private void StartBalanceRefreshTimer()
        {
            _balanceTimer = new DispatcherTimer();
            _balanceTimer.Interval = TimeSpan.FromSeconds(5);
            _balanceTimer.Tick += (s, e) => RefreshBalance();
            _balanceTimer.Start();
        }

        private void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string recipient = RecipientBox.Text.Trim();
                string amountText = AmountBox.Text.Trim();

                if (string.IsNullOrEmpty(recipient))
                {
                    MessageBox.Show("Введите номер телефона или карты получателя",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(amountText, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Введите корректную сумму",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = _authService.CurrentUser;
                if (user == null)
                {
                    MessageBox.Show("Пользователь не авторизован. Пожалуйста, войдите снова.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Перенаправление на окно входа
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();
                    this.Close();
                    return;
                }

                // Проверка баланса
                var currentBalance = _walletService.GetBalance(user.Id);
                if (currentBalance < amount)
                {
                    MessageBox.Show($"Недостаточно средств. Доступно: {currentBalance:F2} ₽",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Определение типа перевода
                string type = TransferTypeBox.SelectedIndex == 0 ? "p2p" : "c2c";

                // Создание транзакции
                var transaction = _transferService.CreateTransfer(user.Id, recipient, amount, type);

                // Проверка на 2FA
                if (_authService.NeedTwoFactor(amount))
                {
                    _pendingTransaction = transaction;
                    TwoFactorGrid.Visibility = Visibility.Visible;

                    // Симуляция отправки SMS
                    NotificationService.Instance.Send2FACode(user.Id, "123456");
                }
                else
                {
                    ProcessTransfer(transaction);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcessTransfer(Transaction transaction, string twoFactorCode = null)
        {
            if (_transferService.ProcessTransfer(transaction, twoFactorCode))
            {
                MessageBox.Show($"Перевод на сумму {transaction.Amount:F2} ₽ выполнен успешно!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                RefreshBalance();
                LoadHistory();

                RecipientBox.Clear();
                AmountBox.Clear();
            }
            else
            {
                MessageBox.Show($"Ошибка при выполнении перевода. Статус: {transaction.Status}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Confirm2FAButton_Click(object sender, RoutedEventArgs e)
        {
            string code = TwoFactorCodeBox.Text.Trim();

            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Введите код подтверждения",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_pendingTransaction != null)
            {
                ProcessTransfer(_pendingTransaction, code);
                _pendingTransaction = null;
            }

            TwoFactorGrid.Visibility = Visibility.Collapsed;
            TwoFactorCodeBox.Clear();
        }

        private void Cancel2FAButton_Click(object sender, RoutedEventArgs e)
        {
            _pendingTransaction = null;
            TwoFactorGrid.Visibility = Visibility.Collapsed;
            TwoFactorCodeBox.Clear();

            MessageBox.Show("Перевод отменён",
                "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshBalanceButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshBalance();
            MessageBox.Show("Баланс обновлён",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _balanceTimer?.Stop();
            _authService.Logout();

            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _balanceTimer?.Stop();
            base.OnClosing(e);
        }
        // Добавьте этот метод в класс MainWindow

        private void TopUpButton_Click(object sender, RoutedEventArgs e)
        {
            var topUpWindow = new Views.TopUpWindow();
            topUpWindow.Owner = this;
            topUpWindow.ShowDialog();

            // Обновляем баланс после закрытия окна пополнения
            RefreshBalance();
            LoadHistory();
        }
    }
}