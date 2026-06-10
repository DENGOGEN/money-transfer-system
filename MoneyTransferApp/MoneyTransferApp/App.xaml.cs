using System.Windows;
using MoneyTransferApp.Services;

namespace MoneyTransferApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Инициализация тестовых данных
            UserService.Instance.InitTestData();
        }
    }
}