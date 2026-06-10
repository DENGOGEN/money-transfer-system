using System.Windows;
using MoneyTransferApp.Services;

namespace MoneyTransferApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();
            _authService = AuthService.Instance;  // Используем Instance
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string phone = PhoneBox.Text.Trim();
            string pin = PinBox.Password;

            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(pin))
            {
                ErrorText.Text = "Заполните все поля";
                return;
            }

            if (_authService.Login(phone, pin))
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ErrorText.Text = "Неверный номер телефона или PIN-код";
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string phone = PhoneBox.Text.Trim();
            string pin = PinBox.Password;

            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(pin))
            {
                ErrorText.Text = "Заполните все поля";
                return;
            }

            if (pin.Length < 4)
            {
                ErrorText.Text = "PIN-код должен содержать минимум 4 символа";
                return;
            }

            if (_authService.Register(phone, pin))
            {
                MessageBox.Show("Регистрация успешна! Теперь вы можете войти.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                ErrorText.Text = "";
            }
            else
            {
                ErrorText.Text = "Пользователь с таким номером уже существует";
            }
        }
    }
}