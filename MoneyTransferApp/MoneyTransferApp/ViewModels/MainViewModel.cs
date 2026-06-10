using System.ComponentModel;
using System.Runtime.CompilerServices;
using MoneyTransferApp.Services;

namespace MoneyTransferApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _currentUserPhone;
        private decimal _currentBalance;

        public MainViewModel()
        {
            AuthService = AuthService.Instance;
            TransferService = TransferService.Instance;
            WalletService = WalletService.Instance;

            RefreshBalance();
        }

        public AuthService AuthService { get; }
        public TransferService TransferService { get; }
        public WalletService WalletService { get; }

        public string CurrentUserPhone
        {
            get => _currentUserPhone;
            set
            {
                _currentUserPhone = value;
                OnPropertyChanged();
            }
        }

        public decimal CurrentBalance
        {
            get => _currentBalance;
            set
            {
                _currentBalance = value;
                OnPropertyChanged();
            }
        }

        public void RefreshBalance()
        {
            if (AuthService.CurrentUser != null)
            {
                CurrentBalance = WalletService.GetBalance(AuthService.CurrentUser.Id);
                CurrentUserPhone = AuthService.CurrentUser.Phone;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}