using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Globalization;

namespace ITNTMARK
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        //Mutex _mutex = null;

        //private void Application_Exit(object sender, ExitEventArgs e)
        //{
        //    if (_mutex != null)
        //    {
        //        _mutex.ReleaseMutex();
        //    }
        //}

        //private void Application_Startup(object sender, StartupEventArgs e)
        //{
        //    string mutexName = "ITNTMARK";
        //    bool isCreatedNew = false;
        //    try
        //    {
        //        _mutex = new Mutex(true, mutexName, out isCreatedNew);


        //        if (isCreatedNew)
        //        {
        //            base.OnStartup(e);
        //        }
        //        else
        //        {
        //            MessageBox.Show("Application already started.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
        //            Application.Exit();
        //            Application.Current.Shutdown();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace + "\n\n" + "Application Existing...", "Exception thrown");
        //        Application.Current.Shutdown();
        //    }
        //}
        Mutex _mutex = null;

        public App()
        {
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new cultu("en-US");
            //CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            //SelectCulture("en-US");
        }


        public static void SelectCulture(string culture)
        {
            // List all our resources       
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                dictionaryList.Add(dictionary);
            }
            // We want our specific culture       
            string requestedCulture = string.Format("StringResources.{0}.xaml", culture);
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);
            if (resourceDictionary == null)
            {
                //리소스를 찾을수 없다면 기본 리소스로 지정      
                requestedCulture = "StringResources.ko-KR.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);
            }
            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }

            //지역화 설정
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            string mutexName = "ITNTMARK";
            bool isCreatedNew = false;
            try
            {
                _mutex = new Mutex(true, mutexName, out isCreatedNew);

                if (isCreatedNew)
                {
                    base.OnStartup(e);
                }
                else
                {
                    MessageBox.Show("Application already started.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace + "\n\n" + "Application Existing...", "Exception thrown");
                Application.Current.Shutdown();
            }
        }
    }
}
