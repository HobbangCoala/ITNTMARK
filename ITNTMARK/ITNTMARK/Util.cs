//using DevExpress.XtraPrinting.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Drawing;
//using DevExpress.Xpf.Core;
//using System.Windows.Interop;
//using System.Windows.Shapes;
using ITNTCOMMON;

#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTUTIL
{
    static class Util
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        [DllImport("kernel32")]
        private static extern uint GetPrivateProfileInt(string section, string key, int def, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileSection(string section, Byte[] PairValue, int size, string filePath);
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("kernel32")]
        public static extern int GetPrivateProfileSectionNames(byte[] pszReturnBuffer, int nSize, string lpFileName);

        //int byteCount = GetPrivateProfileSection(sectionName, returnValue, 32768 * 2, iniPath);

        static object lockObject = new object();

        public static double PXPERMM = 16.68d;
        public static double FONTSCALE = 1.00d;
        public const double orgWidth = 1024;
        public const double orgHeight = 768;

        public static int CAMERA_WIDTH = 2048;
        public static int CAMERA_HEIGHT = 400;

        public static int SCREEN_WIDTH = 1920;
        public static int SCREEN_HEIGHT = 1080;

        public static int SCREEN_LEFT = 0;
        public static int SCREEN_TOP = 0;

        //public static MainWindow main;

        static int DisplayScreenNumber { get; set; }
        static Util()
        {
            string numstring = "1";
            Util.GetPrivateProfileValue("SYSTEM", "MONITORNUM", "1", ref numstring, "VisionConfig.ini");
            int dispNumber = Convert.ToInt32(numstring);
            //if (System.Windows.Forms.SystemInformation.MonitorCount < dispNumber)
            //    dispNumber = 1;

            string value = "16.68"; 
            Util.GetPrivateProfileValue("CONFIG", "PXPERMM", "16.68", ref value, "Parameter\\ImageConfig.ini");
            PXPERMM = Convert.ToDouble(value);

            Util.GetPrivateProfileValue("CONFIG", "FONTSCALE", "1.00", ref value, "Parameter\\ImageConfig.ini");
            FONTSCALE = Convert.ToDouble(value);

            PXPERMM *= FONTSCALE;

            Util.GetPrivateProfileValue("CONFIG", "WIDTH", "2048", ref value, "CameraConfig.ini");
            CAMERA_WIDTH = Convert.ToInt32(value);

            Util.GetPrivateProfileValue("CONFIG", "HEIGHT", "400", ref value, "CameraConfig.ini");
            CAMERA_HEIGHT = Convert.ToInt32(value);

            Util.GetPrivateProfileValue("SCREEN", "WIDTH", "1920", ref value, "VisionConfig.ini");
            SCREEN_WIDTH = Convert.ToInt32(value);

            Util.GetPrivateProfileValue("SCREEN", "HEIGHT", "1080", ref value, "VisionConfig.ini");
            SCREEN_HEIGHT = Convert.ToInt32(value);

            Util.GetPrivateProfileValue("SCREEN", "LEFT", "0", ref value, "VisionConfig.ini");
            SCREEN_LEFT = Convert.ToInt32(value);

            Util.GetPrivateProfileValue("SCREEN", "TOP", "0", ref value, "VisionConfig.ini");
            SCREEN_TOP = Convert.ToInt32(value);

            DisplayScreenNumber = dispNumber;
        }
        //public static void SetMainWidnow(Window win)
        //{
        //    Util.main = (MainWindow)win;
        //}

        public static int Ascii2Hex(string src, ref ushort dest)
        {
            if ((src == null) || (src.Length <= 0))
            {
                dest = 0;
                return -1;
            }

            string tmp;
            if (src.Length > 2)
                tmp = src.Substring(0, 1);

            return 0;
        }

        public static void SetFullScreenSize(Window window)
        {
            //int dispNumber = 0;
            //int showScreenNum = 0;
            //try
            //{
            //    if (System.Windows.Forms.SystemInformation.MonitorCount >= DisplayScreenNumber)
            //        dispNumber = DisplayScreenNumber;
            //    string dispString = string.Format("\\\\.\\DISPLAY{0}", dispNumber);
            //    for (int i = 0; i < System.Windows.Forms.SystemInformation.MonitorCount; i++)
            //    {
            //        if (System.Windows.Forms.Screen.AllScreens[i].DeviceName == dispString)
            //        {
            //            showScreenNum = i;
            //            break;
            //        }
            //    }

            //    System.Drawing.Rectangle workingArea = System.Windows.Forms.Screen.AllScreens[showScreenNum].WorkingArea;

            //    window.WindowStartupLocation = WindowStartupLocation.Manual;
            //    window.Left = workingArea.Left;
            //    window.Top = workingArea.Top;
            //    window.Width = workingArea.Width;
            //    window.Height = workingArea.Height;
            //    window.WindowState = WindowState.Maximized;
            //    window.WindowStyle = WindowStyle.None;
            //    window.ResizeMode = ResizeMode.NoResize;
            //}
            //catch (Exception ex)
            //{
            //    ITNTTraceLog.Instance.Trace(0, "SetFullSize Exception : {0}, {1}", ex.HResult, ex.Message);
            //}
        }

        public static void GetScreenSize(out double width, out double height)
        {
            //int dispNumber = 1;
            //if (System.Windows.Forms.SystemInformation.MonitorCount >= DisplayScreenNumber)
            //    dispNumber = DisplayScreenNumber;

            //string dispString = string.Format("\\\\.\\DISPLAY{0}", dispNumber);
            //int showScreenNum = 0;
            //for (int i = 0; i < System.Windows.Forms.SystemInformation.MonitorCount; i++)
            //{
            //    if (System.Windows.Forms.Screen.AllScreens[i].DeviceName == dispString)
            //    {
            //        showScreenNum = i;
            //        break;
            //    }
            //}
            //System.Drawing.Rectangle workingArea = System.Windows.Forms.Screen.AllScreens[showScreenNum].WorkingArea;
            ////System.Drawing.Rectangle workingArea = System.Windows.Forms.Screen.AllScreens[dispNumber].WorkingArea;
            //width = workingArea.Width;
            //height = workingArea.Height;
            width = 1920;
            height = 1080;
        }

        //public static void ChangeSize(ScaleTransform scale, double orgWidth, double orgHeight, double width, double height, Window window)
        //{
        //    scale.ScaleX = width / orgWidth;
        //    scale.ScaleY = height / orgHeight;

        //    FrameworkElement rootElement = window.Content as FrameworkElement;

        //    rootElement.LayoutTransform = scale;
        //}

        public static void ChangeSize(Window window, double width=orgWidth, double height=orgHeight)
        {
            double realWidth = 1280;
            double realHeight = 1024;
            GetScreenSize(out realWidth, out realHeight);
            ScaleTransform scale = new ScaleTransform();
            scale.ScaleX = realWidth / width;
            scale.ScaleY = realHeight / height;

            FrameworkElement rootElement = window.Content as FrameworkElement;

            rootElement.LayoutTransform = scale;
        }

        //public static int GetPrivateProfileValue(string section, string key, string def, StringBuilder retVal, int size, string fileName)
        //{
        //    int val = 0;
        //    //            string curDir = AppDomain.CurrentDomain.BaseDirectory;
        //    //            string filepath = curDir + fileName;
        //    string className = "Util";
        //    string funcName = "GetPrivateProfileValue";

        //    try
        //    {
        //        val = GetPrivateProfileString(section, key, def, retVal, size, fileName);
        //    }
        //    catch(Exception ex)
        //    {
        //        //ITNTTraceLog.Instance.Trace(0, "Util::GetPrivateProfileValue() Exception : {0:X}, {1}", ex.HResult, ex.Message);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    }
        //    return val;
        //}

        public static uint GetPrivateProfileValueUINT(string section, string key, int def, string fileName)
        {
            uint val = 0;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filepath = curDir + fileName;
            string className = "Util";
            string funcName = "GetPrivateProfileValueUINT";

            try
            {
                //val = GetPrivateProfileInt(section, key, def, fileName);
                val = GetPrivateProfileInt(section, key, def, filepath);
            }
            catch (Exception ex)
            {
                //ITNTTraceLog.Instance.Trace(0, "Util::GetPrivateProfileValue() Exception : {0:X}, {1}", ex.HResult, ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return val;
        }

        public static double GetPrivateProfileValueDouble(string section, string key, double def, string fileName)
        {
            int val = 0;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filepath = curDir + fileName;
            StringBuilder sb = new StringBuilder();
            sb.Capacity = 1024;
            string retstring = def.ToString();
            double result = 0.0d;
            string className = "Util";
            string funcName = "GetPrivateProfileValueDouble";

            try
            {
                for (int i = 0; i < 3; i++)
                {
                    val = GetPrivateProfileString(section, key, retstring, sb, sb.Capacity, filepath);
                    if ((val > 0) && (sb.ToString().Length > 0))
                    {
                        retstring = sb.ToString();
                        break;
                        //val = GetPrivateProfileString(section, key, def, sb, 0, filepath);
                        //if ((val > 0) && (sb.ToString().Length > 0))
                        //{
                        //    retstring = sb.ToString();
                        //    break;
                        //}
                    }
                }
                double.TryParse(retstring, out result);
                //result = retstring;
            }
            catch (Exception ex)
            {
                result = def;
                //ITNTTraceLog.Instance.Trace(0, "Util::GetPrivateProfileValue()2 Exception : {0:X}, {1}", ex.HResult, ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return result;
        }

        public static byte GetPrivateProfileValueByte(string section, string key, byte def, string fileName)
        {
            byte val = 0;
            uint tmp = 0;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filepath = curDir + fileName;
            string className = "Util";
            string funcName = "GetPrivateProfileValueUINT";

            try
            {
                tmp = GetPrivateProfileInt(section, key, def, filepath);
                val = (byte)tmp;
            }
            catch (Exception ex)
            {
                //ITNTTraceLog.Instance.Trace(0, "Util::GetPrivateProfileValue() Exception : {0:X}, {1}", ex.HResult, ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return val;
        }

        public static int GetPrivateProfileValue(string section, string key, string def, ref string result, string fileName)
        {
            int val = 0;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filepath = curDir + fileName;
            StringBuilder sb = new StringBuilder();
            sb.Capacity = 1024;
            string retstring = def;
            string className = "Util";
            string funcName = "GetPrivateProfileValue";

            try
            {
                for(int i = 0; i < 3; i++)
                {
                    val = GetPrivateProfileString(section, key, def, sb, sb.Capacity, filepath);
                    if ((val <= 0) || (sb.ToString().Length <= 0))
                        retstring = def;
                    else
                    {
                        retstring = sb.ToString();
                        break;
                        //val = GetPrivateProfileString(section, key, def, sb, 0, filepath);
                        //if ((val > 0) && (sb.ToString().Length > 0))
                        //{
                        //    retstring = sb.ToString();
                        //    break;
                        //}
                    }
                }
                result = retstring;
            }
            catch(Exception ex)
            {
                result = def;
                //ITNTTraceLog.Instance.Trace(0, "Util::GetPrivateProfileValue()2 Exception : {0:X}, {1}", ex.HResult, ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return val;
        }

        public static long WritePrivateProfileValue(string section, string key, string val, string fileName)
        {
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filepath = curDir + fileName;
            long ret = 0;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    ret = WritePrivateProfileString(section, key, val, filepath);
                    if (ret > 0)
                        break;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "Util::WritePrivateProfileValue() Exception : {0}, {1}", ex.HResult, ex.Message);
            }

            return ret;
        }
        //public static long WritePrivateProfileValue2(string section, string key, string val, string fileName)
        //{
        //    long ret = 0;
        //    ret = WritePrivateProfileString(section, key, val, fileName);

        //    return ret;
        //}

        public static Dictionary<string, string> GetKeyValuesInSection(string sectionName, string iniPath, byte savenullstringoption)
        {
            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            byte[] returnValue = new byte[32768 * 2];
            int pos = 0;
            int byteCount = GetPrivateProfileSection(sectionName, returnValue, 32768 * 2, iniPath);
            while (true)
            {
                if (pos >= byteCount)   break;

                int count = 0;
                while (true)
                {
                    if (returnValue[pos + count] == 0)      break;
                    count++;
                }

                string txt = Encoding.Default.GetString(returnValue, pos, count);

                int equalPos = txt.IndexOf('=');
                if (equalPos == -1)
                {
                    if(savenullstringoption != 0)
                        keyValues.Add(txt, "");
                }
                else
                {
                    string keyname = txt.Substring(0, equalPos);
                    string keyvalue = txt.Substring(equalPos + 1);
                    if((keyvalue.Length > 0) ||
                        ((keyvalue.Length <= 0) && (savenullstringoption != 0)))
                        keyValues.Add(keyname, keyvalue);
                }

                pos += count + 1;
            }

            return keyValues;
        }

        //public static void delay_ms(int num)
        //{
        //    MainWindow mw = (MainWindow)System.Windows.Application.Current.MainWindow;
        //    Thread.Sleep(num);
        //}

        //public static void undelay_ms(int num)
        //{
        //    MainWindow mw = (MainWindow)System.Windows.Application.Current.MainWindow;
        //    mw.Dispatcher.Invoke((ThreadStart)(() => { }), DispatcherPriority.ApplicationIdle);
        //    Thread.Sleep(num);
        //}

        public static BitmapImage GetImageSource(string filename)
        {
            string className = "Util";
            string funcName = "GetImageSource";
            BitmapImage img = null;
            try
            {
                if (filename != null)
                {
                    BitmapImage image = new BitmapImage();
                    using (FileStream stream = File.OpenRead(filename))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                    }
                    img = image;
                }
            }
            catch (Exception ex)
            {
                //ITNTTraceLog.Instance.Trace(0, "Util::GetImageSource() Exception : {0:X}, {1}", ex.HResult, ex.Message);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION : CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return img;
        }

        public static ImageBrush GetImageBrush(string filename)
        {
            ImageBrush ib = null;
            BitmapImage img = new BitmapImage();
            FileInfo fi = new FileInfo(filename);
            if (fi.Exists == true)
            {
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnDemand;
                img.CreateOptions = BitmapCreateOptions.DelayCreation;

                img.UriSource = new Uri(filename, UriKind.RelativeOrAbsolute);
                img.EndInit();

                ib = new ImageBrush(img);
            }
            return ib;
        }

        public static int GetPrivateProfileKey(string section, string data, ref string retkey, string filename)
        {
            //int retval = 0;
            //Dictionary<string, string> keyValues = new Dictionary<string, string>();
            int maxSize = 4096 * 2;
            byte[] returnValue = new byte[maxSize];
            string sectionvalue = "";

            int byteCount = GetPrivateProfileSection(section, returnValue, maxSize, filename);
            sectionvalue = Encoding.Default.GetString(returnValue);//, pos);//, count);
            sectionvalue = sectionvalue.Replace("\0\0", "");
            string[] values = sectionvalue.Split('\0');
            if (values.Length <= 0)
                return -1;

            for(int i = 0; i < values.Length; i++)
            {
                if(values[i].Length > 0)
                {
                    string[] value = values[i].Split('=');
                    if (value.Length < 2)
                        continue;
                    if (value[1] == data)
                    {
                        retkey = value[0];
                        return 0;
                    }
                }
            }
            return -2;
        }

        public static int GetPrivateProfileKeyData(string section, Dictionary<string, string> defstring, ref Dictionary<string,string> retdata, string filename)
        {
            int maxSize = 4096 * 2;
            byte[] returnValue = new byte[maxSize];
            string sectionvalue = "";
            //Dictionary<string, string> keydata = new Dictionary<string, string>();
            int byteCount = 0;
            try
            {
                byteCount = GetPrivateProfileSection(section, returnValue, maxSize, filename);
                sectionvalue = Encoding.Default.GetString(returnValue);//, pos);//, count);
                sectionvalue = sectionvalue.Replace("\0\0", "");
                string[] values = sectionvalue.Split('\0');
                if (values.Length <= 0)
                    return -1;

                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i].Length > 0)
                    {
                        string[] vals = values[i].Split('=');
                        if (vals.Length < 2)
                            continue;
                        retdata.Add(vals[0], vals[1]);
                    }
                }

                if(retdata.Count <= 0)
                {
                    retdata = defstring;
                }

                return 0;
            }
            catch (Exception ex)
            {
                retdata = defstring;
                return ex.HResult;
            }
        }

        //public static string getCurrentClassFunctionName()
        //{

        //}


        //public static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        //{
        //    IntPtr hBitmap = bitmap.GetHbitmap();
        //    BitmapImage retval;

        //    try
        //    {
        //        retval = (BitmapImage)Imaging.CreateBitmapSourceFromHBitmap(
        //                     hBitmap,
        //                     IntPtr.Zero,
        //                     Int32Rect.Empty,
        //                     BitmapSizeOptions.FromEmptyOptions());
        //    }
        //    finally
        //    {
        //        DeleteObject(hBitmap);
        //    }

        //    return retval;
        //}

        //public static Bitmap Hobject2Bitmap(HalconDotNet.HObject HImg)
        //{
        //    if (HImg == null)
        //        return null;

        //    try
        //    {
        //        Bitmap bmpImg;
        //        HalconDotNet.HTuple Channels;
        //        HalconDotNet.HOperatorSet.CountChannels(HImg, out Channels);
        //        if (Channels.I == 1)
        //        {
        //            System.Drawing.Imaging.PixelFormat pixelFmt = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
        //            HalconDotNet.HTuple hpointer, type, width, height;
        //            const int Alpha = 255;
        //            Int64[] ptr = new Int64[2];
        //            HalconDotNet.HOperatorSet.GetImagePointer1(HImg, out hpointer, out type, out width, out height);
        //            bmpImg = new Bitmap(width.I, height.I, pixelFmt);
        //            System.Drawing.Imaging.ColorPalette pal = bmpImg.Palette;
        //            for (int i = 0; i < 256; i++)
        //                pal.Entries[i] = System.Drawing.Color.FromArgb(Alpha, i, i, i);

        //            bmpImg.Palette = pal;
        //            System.Drawing.Imaging.BitmapData bitmapData = bmpImg.LockBits(new System.Drawing.Rectangle(0, 0, width.I, height.I), System.Drawing.Imaging.ImageLockMode.ReadWrite, pixelFmt);
        //            int PixelsSize = System.Drawing.Bitmap.GetPixelFormatSize(bitmapData.PixelFormat) / 8;
        //            Console.WriteLine(bitmapData.Scan0);
        //            ptr[0] = (Int64)bitmapData.Scan0;
        //            ptr[1] = (Int64)hpointer.IP;
        //            if (width % 4 == 0)
        //                CopyMemory(ptr[0], ptr[1], width * height * PixelsSize);
        //            else
        //            {
        //                ptr[1] += width;
        //                CopyMemory(ptr[0], ptr[1], width * PixelsSize);
        //                ptr[0] += width;
        //            }
        //            bmpImg.UnlockBits(bitmapData);
        //        }
        //        else if (Channels.I == 3)
        //        {
        //            System.Drawing.Imaging.PixelFormat pixelFmt = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
        //            HalconDotNet.HTuple hred, hgreen, hblue, type, width, height;
        //            HalconDotNet.HOperatorSet.GetImagePointer3(HImg, out hred, out hgreen, out hblue, out type, out width, out height);
        //            bmpImg = new System.Drawing.Bitmap(width.I, height.I, pixelFmt);
        //            System.Drawing.Imaging.BitmapData bitmapData = bmpImg.LockBits(new System.Drawing.Rectangle(0, 0, width.I, height.I), System.Drawing.Imaging.ImageLockMode.ReadWrite, pixelFmt);
        //            unsafe
        //            {
        //                byte* data = (byte*)bitmapData.Scan0;
        //                byte* hr = (byte*)hred.IP;
        //                byte* hg = (byte*)hgreen.IP;
        //                byte* hb = (byte*)hblue.IP;
        //                for (int i = 0; i < width.I * height.I; i++)
        //                {
        //                    *(data + (i * 3)) = (*(hb + i));
        //                    *(data + (i * 3) + 1) = *(hg + i);
        //                    *(data + (i * 3) + 2) = *(hr + i);
        //                }
        //            }
        //            bmpImg.UnlockBits(bitmapData);
        //        }
        //        else if (Channels.I == 4)


        //        {


        //            System.Drawing.Imaging.PixelFormat pixelFmt = System.Drawing.Imaging.PixelFormat.Format32bppRgb;


        //            HalconDotNet.HTuple hred, hgreen, hblue, type, width, height;
        //            HalconDotNet.HOperatorSet.GetImagePointer3(HImg, out hred, out hgreen, out hblue, out type, out width, out height);

        //            bmpImg = new System.Drawing.Bitmap(width.I, height.I, pixelFmt);
        //            System.Drawing.Imaging.BitmapData bitmapData = bmpImg.LockBits(new System.Drawing.Rectangle(0, 0, width.I, height.I), System.Drawing.Imaging.ImageLockMode.ReadWrite, pixelFmt);
        //            unsafe
        //            {
        //                byte* data = (byte*)bitmapData.Scan0;
        //                byte* hr = (byte*)hred.IP;
        //                byte* hg = (byte*)hgreen.IP;
        //                byte* hb = (byte*)hblue.IP;
        //                for (int i = 0; i < width.I * height.I; i++)
        //                {
        //                    *(data + (i * 4)) = *(hb + i);
        //                    *(data + (i * 4) + 1) = *(hg + i);
        //                    *(data + (i * 4) + 2) = *(hr + i);
        //                    *(data + (i * 4) + 3) = 255;
        //                }
        //                bmpImg.UnlockBits(bitmapData);
        //            }
        //        }
        //        else
        //        {
        //            bmpImg = null;
        //        }
        //        return bmpImg;
        //    }
        //    catch (HalconDotNet.HalconException ex)
        //    {
        //        Console.WriteLine("In ImageTypeConverter.Hobject2Bitmap: " + ex.Message);
        //        return null;
        //    }


        //    catch (System.Exception ex)


        //    {


        //        Console.WriteLine("In ImageTypeConverter.Hobject2Bitmap: " + ex.Message);


        //        return null;


        //    }





        //}



    }
}
