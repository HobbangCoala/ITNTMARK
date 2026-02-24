using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ITNTUTIL;
using ITNTCOMMON;

namespace ITNTMARK
{
    /// <summary>
    /// ChangePasswordWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        public ChangePasswordWindow(string ColorString)
        {
            InitializeComponent();
            Color color = (Color)ColorConverter.ConvertFromString(ColorString);
            this.Background = new SolidColorBrush(color);
        }

        private void btnEnterPWOK_Click(object sender, RoutedEventArgs e)
        {
            //if (tbxUserID.Text.Length <= 0)
            //{
            //    lblChangePWResult.Content = "Enter ID";
            //    return;
            //}

            if (pwxChangePasswordOld.Password.Length <= 0)
            {
                lblChangePWResult.Content = "Enter current password";
                lblChangePWResult2.Content = "";
                return;
            }

            if (pwxChangePasswordNew.Password.Length <= 0)
            {
                lblChangePWResult.Content = "Enter new password";
                lblChangePWResult2.Content = "";
                return;
            }

            if (pwxChangePasswordVerify.Password.Length <= 0)
            {
                lblChangePWResult.Content = "Enter vefify password";
                lblChangePWResult2.Content = "";
                return;
            }

            if (pwxChangePasswordNew.Password.CompareTo(pwxChangePasswordVerify.Password) != 0)
            {
                lblChangePWResult.Content = "Passwords are not same.";
                lblChangePWResult2.Content = "Please confirm passwords.";
                return;
            }

            if (pwxChangePasswordOld.Password == ((MainWindow)System.Windows.Application.Current.MainWindow).masterpw)
            {
                ((MainWindow)System.Windows.Application.Current.MainWindow).PasswordFlag = 3;
                Util.WritePrivateProfileValue("OPTION", "Mater", pwxChangePasswordNew.Password, Constants.PARAMS_INI_FILE);
                ((MainWindow)System.Windows.Application.Current.MainWindow).masterpw = pwxChangePasswordNew.Password;
            }
            else if (pwxChangePasswordOld.Password == ((MainWindow)System.Windows.Application.Current.MainWindow).operatorpw)
            {
                ((MainWindow)System.Windows.Application.Current.MainWindow).PasswordFlag = 2;
                Util.WritePrivateProfileValue("OPTION", "Operator", pwxChangePasswordNew.Password, Constants.PARAMS_INI_FILE);
                ((MainWindow)System.Windows.Application.Current.MainWindow).operatorpw = pwxChangePasswordNew.Password;
            }
            else if (pwxChangePasswordOld.Password == ((MainWindow)System.Windows.Application.Current.MainWindow).userpw)
            {
                ((MainWindow)System.Windows.Application.Current.MainWindow).PasswordFlag = 1;
                Util.WritePrivateProfileValue("OPTION", "User", pwxChangePasswordNew.Password, Constants.PARAMS_INI_FILE);
                ((MainWindow)System.Windows.Application.Current.MainWindow).userpw = pwxChangePasswordNew.Password;
            }
            else
            {
                lblChangePWResult.Content = "Current password is not correct.";
                lblChangePWResult2.Content = "Please check your password.";
                this.Focus();
                pwxChangePasswordOld.Focus();
                return;
            }

            lblChangePWResult.Content = "Password is changed.";
            lblChangePWResult2.Content = "";

            DialogResult = true;
        }

        private void pwxChangePasswordNew_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                pwxChangePasswordVerify.Focus();
                e.Handled = true;
            }
        }

        private void pwxChangePasswordVerify_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.btnEnterPWOK_Click(sender, e);
                e.Handled = true;
            }
        }

        private void pwxChangePasswordVerify_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Delete) || (e.Key == Key.Back))
            {
                lblChangePWResult.Content = "";
                lblChangePWResult2.Content = "";
            }
        }

        private void pwxChangePasswordNew_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Delete) || (e.Key == Key.Back))
            {
                lblChangePWResult.Content = "";
                lblChangePWResult2.Content = "";
            }
        }

        private void pwxChangePasswordOld_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Delete) || (e.Key == Key.Back))
            {
                lblChangePWResult.Content = "";
                lblChangePWResult2.Content = "";
            }
        }

        private void pwxChangePasswordOld_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                pwxChangePasswordNew.Focus();
                e.Handled = true;
            }
        }
    }
}
