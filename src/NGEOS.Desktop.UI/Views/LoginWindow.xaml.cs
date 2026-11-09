using System.Windows;
using NGEOS.Desktop.UI.ViewModels;

namespace NGEOS.Desktop.UI.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            LoginPwdBox.Password = "Admin@12";
        }

        private void LoginPwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = LoginPwdBox.Password;
            }
        }

        private void RegPwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = RegPwdBox.Password;
            }
        }

        private void AutoFillAdmin_Click(object sender, RoutedEventArgs e)
        {
            LoginPwdBox.Password = "Admin@12";
        }
    }
}
