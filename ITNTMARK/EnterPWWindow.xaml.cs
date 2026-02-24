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

namespace ITNTMARK
{
    /// <summary>
    /// EnterPWWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EnterPWWindow : Window
    {
        bool useID = false;

        public EnterPWWindow()
        {
            InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.Loaded += new RoutedEventHandler(Login_Loaded);
        }

        void Login_Loaded(object sender, RoutedEventArgs e)
        {
            pwxEnterPassword.Focus();
        }

        public EnterPWWindow(string ColorString)
        {
            InitializeComponent();
            this.Owner = App.Current.MainWindow;
            pwxEnterPassword.Focus();
            Color color = (Color)ColorConverter.ConvertFromString(ColorString);
            this.Background = new SolidColorBrush(color);
        }

        private void btnEnterPWOK_Click(object sender, RoutedEventArgs e)
        {
            if (useID == true)
            {
                if ((((MainWindow)System.Windows.Application.Current.MainWindow).masterID.ToUpper() == tbxUserID.Text.ToUpper()) &&
                    (pwxEnterPassword.Password == ((MainWindow)System.Windows.Application.Current.MainWindow).masterpw))
                {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).PasswordFlag = 3;
                }
                else if ((((MainWindow)System.Windows.Application.Current.MainWindow).operatorID.ToUpper() == tbxUserID.Text.ToUpper()) &&
                    (pwxEnterPassword.Password == ((MainWindow)System.Windows.Application.Current.MainWindow).operatorpw))
                {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).PasswordFlag = 2;
                }
                else if ((((MainWindow)System.Windows.Application.Current.MainWindow).userID.ToUpper() == tbxUserID.Text.ToUpper()) &&
                    (pwxEnterPassword.Password == ((MainWindow)System.Windows.Application.Current.MainWindow).userpw))
                {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).PasswordFlag = 1;
                }
                else
                {
                    lblEnterPWResult.Content = "User ID or password is not correct.";
                    lblEnterPWResult2.Content = "Please check ID or password.";
                    this.Focus();
                    pwxEnterPassword.Focus();
                    return;
                }
            }
            else
            {
                if (pwxEnterPassword.Password == ((MainWindow)System.Windows.Application.Current.MainWindow).masterpw)
                {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).PasswordFlag = 3;
                }
                else if (pwxEnterPassword.Password == ((MainWindow)System.Windows.Application.Current.MainWindow).operatorpw)
                {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).PasswordFlag = 2;
                }
                else if (pwxEnterPassword.Password == ((MainWindow)System.Windows.Application.Current.MainWindow).userpw)
                {
                    ((MainWindow)System.Windows.Application.Current.MainWindow).PasswordFlag = 1;
                }
                else
                {
                    lblEnterPWResult.Content = "Password is not correct.";
                    lblEnterPWResult2.Content = "Please check your password.";
                    //this.Focus();
                    //pwxEnterPassword.Focus();
                    return;
                }
            }

            this.DialogResult = true;
        }

        private void pwxEnterPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.btnEnterPWOK_Click(sender, e);
                e.Handled = true;

                //this.Focus();
                //pwxEnterPassword.Focus();
            }
            //if((e.Key == Key.Delete) || (e.Key == Key.Back))
            //{
            //    lblEnterPWResult.Content = "";
            //    lblEnterPWResult2.Content = "";
            //}
            //e.Handled = true;
        }

        private void pwxEnterPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Delete) || (e.Key == Key.Back))
            {
                lblEnterPWResult.Content = "";
                lblEnterPWResult2.Content = "";
            }

        }
    }
}
