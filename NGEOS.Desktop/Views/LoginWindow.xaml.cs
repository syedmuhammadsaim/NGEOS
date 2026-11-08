using System;
using System.Windows;
using NGEOS.Desktop.ViewModels;

namespace NGEOS.Desktop.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            App.Trace("LoginWindow: constructor entered");
            try
            {
                InitializeComponent();
                App.Trace("LoginWindow: InitializeComponent succeeded");
            }
            catch (Exception ex)
            {
                App.Trace($"LoginWindow: InitializeComponent CRASH: {ex}");
                throw;
            }
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = PasswordInput.Password;
            }
        }

        private void AutoFill_Click(object sender, RoutedEventArgs e)
        {
            PasswordInput.Password = "Admin@123";
        }
    }
}
