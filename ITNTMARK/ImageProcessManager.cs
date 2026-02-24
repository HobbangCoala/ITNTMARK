using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Media = System.Windows.Media;
//using System.Drawing;
//using System.Drawing.Imaging;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
//using HalconDotNet;
using System.Diagnostics;
using ITNTUTIL;
using ITNTCOMMON;
using System.Threading;
using System.Xml.Linq;
using System.Windows.Markup;
using System.Windows.Media.Media3D;
//using S7.Net.Types;
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public class ImageProcessManager
    {
        private Canvas canvas;
        static string sDeviceCode = DeviceCode.Device_APP;
        static string sDeviceName = DeviceName.Device_APP;


        public ImageProcessManager()
        {
            canvas = new Canvas();
        }


        public static int GetFontSize(string fontName, byte bHeadType, double density, out double fontsizeX, out double fontsizeY, out double shiftVal, out string ErrorCode)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = "";
            FileInfo fi;
            string fileline = "";
            List<string> FontDataLaser = new List<string>();

            try
            {
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;
                shiftVal = 0.0d;

                //filename = curDir + "FONTL\\S_" + fontName + ".FON";
                if (bHeadType == 0)
                    filename = curDir + "FONT/S_" + fontName + ".FON";
                else
                {
                    if (density > 1)
                        filename = curDir + "FONTL/S_" + fontName + ".FON";
                    else
                        filename = curDir + "FONTL/D_" + fontName + ".FON";
                }

                fi = new FileInfo(filename);
                if (fi.Exists == false)
                {
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);
                    return -3;
                }

                using (StreamReader sr = new StreamReader(filename))
                {
                    string[] fontsize;
                    fileline = sr.ReadLine();
                    fontsize = fileline.Split(',');
                    if (fontsize.Length < 2)
                    {
                        //System.Windows.MessageBox.Show("Font File is not valid");
                        //imgsource = null;
                        ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                        ErrorCode = "00HMF0000002";
                        return -4;
                    }
                    double.TryParse(fontsize[0], out fontsizeX);
                    double.TryParse(fontsize[1], out fontsizeY);
                    if (fontsize.Length >= 3)
                        double.TryParse(fontsize[2], out shiftVal);
                }
                ErrorCode = "";
                ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return 0;
            }
            catch (Exception ex)
            {
                ErrorCode = string.Format("00HE{0:X8}", Math.Abs(ex.HResult));
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;
                shiftVal = 0.0d;
                //ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}", ErrorCode));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int GetFontDataAllEx(VinNoInfo vininfo, byte bHeadType, double density, ref Dictionary<int, List<FontDataClass>> MyData, ref double fontsizeX, ref double fontsizeY, ref double shiftVal, ref string ErrorCode)
        {
            //string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            //string funcName = MethodBase.GetCurrentMethod().Name;
            string className = "ImageProcessManager";
            string funcName = "GetFontDataAllEx";
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = "";
            FileInfo fi;
            string fileline;
            List<string> FontDataClass = new List<string>();
            double subShift = 0;

            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            try
            {
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;

                //string value = "";
                //Util.GetPrivateProfileValue("USEFONT", "TYPE", "0", ref value, "FONT.ini");
                //if (value != "0")
                //    filename = curDir + "FONTL\\S_" + vin.fontName + ".FON";
                //else
                //filename = curDir + "FONT\\S_" + vin.fontName + ".FON";

                if (bHeadType == 0)
                    filename = curDir + "FONT/S_" + vininfo.fontName + ".FON";
                else
                {
                    if (density > 1)
                        filename = curDir + "FONTL/S_" + vininfo.fontName + ".FON";
                    else
                        filename = curDir + "FONTL/D_" + vininfo.fontName + ".FON";
                }

                fi = new FileInfo(filename);
                if (fi.Exists == false)
                {
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);
                    return -3;
                }

                using (StreamReader sr = new StreamReader(filename))
                {
                    string[] pointList;
                    string[] point;

                    string[] fontsize;
                    fileline = sr.ReadLine();
                    fontsize = fileline.Split(',');
                    if (fontsize.Length < 2)
                    {
                        //System.Windows.MessageBox.Show("Font File is not valid");
                        //imgsource = null;
                        fontsizeX = 0.0d;
                        fontsizeY = 0.0d;
                        ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                        ErrorCode = "00HMF0000002";
                        return -4;
                    }

                    fontsizeX = Math.Max((Convert.ToDouble(fontsize[0])), 0.01d);
                    fontsizeY = Math.Max((Convert.ToDouble(fontsize[1])), 0.01d);
                    if (fontsize.Length >= 3)
                        double.TryParse(fontsize[2], out shiftVal);
                    else
                        shiftVal = 0;

                    subShift = shiftVal;
                    if ((shiftVal <= 0) && (bHeadType == 0))
                    {
                        if (vininfo.fontName == "5X7")
                            subShift = 3.0d;
                        else if (vininfo.fontName == "11X16" || vininfo.fontName == "OCR")
                            subShift = 5.0d;
                    }

                    FontDataClass tmpfd = new FontDataClass();// { X = -1.0d, Y = -1.0d, Z = -1.0d, Flag = -1 };
                    List<FontDataClass> tmplist = new List<FontDataClass>();
                    tmplist.Add(tmpfd);
                    MyData.Add(0, tmplist);

                    int idx = 1;

                    while ((fileline = sr.ReadLine()) != null)
                    {
                        List<FontDataClass> FontList = new List<FontDataClass>();
                        //FontDataClass fd;
                        FontDataClass fd = new FontDataClass();
                        if (fileline.Length > 0)
                        {
                            pointList = fileline.Split(';');
                            if (pointList.Count() > 0)
                            {
                                bool find = false;
                                for (int i = 0; i < pointList.Count(); i++)
                                {
                                    point = pointList[i].Split(',');
                                    if (point.Count() >= 3)
                                    {
                                        fd.vector3d.X = Convert.ToDouble(point[0]);
                                        fd.vector3d.Y = (fontsizeY + subShift - Convert.ToDouble(point[1]) - 1);
                                        ////if ((vin.fontName == "HMC5") || (vin.fontName == "OCR"))
                                        ////    fd.Y = (fontsizeY - Convert.ToDouble(point[1]));
                                        ////else if (vin.fontName == "5X7")
                                        ////    fd.Y = (fontsizeY + 3.0d - Convert.ToDouble(point[1]));
                                        ////else
                                        ////    fd.Y = (fontsizeY + 5.0d - Convert.ToDouble(point[1]));

                                        ////Read Font List Fix 210801 James Cho
                                        //if (vininfo.fontName == "5X7")
                                        //    fd.Y = (fontsizeY + 3.0d - Convert.ToDouble(point[1]));
                                        //else if (vininfo.fontName == "11X16" || vininfo.fontName == "OCR")
                                        //    fd.Y = (fontsizeY + 5.0d - Convert.ToDouble(point[1]));
                                        //else
                                        //    fd.Y = (fontsizeY - Convert.ToDouble(point[1]));

                                        fd.Flag = Convert.ToInt32(point[2]);
                                        FontList.Add(fd);
                                        find = true;
                                    }
                                    //find = true;
                                }
                                if (find == false)
                                {
                                    fd.vector3d.X = -1.0d;
                                    fd.vector3d.Y = -1.0d;
                                    fd.Flag = -1;
                                    FontList.Add(fd);
                                }
                            }
                            else
                            {
                                fd.vector3d.X = -1.0d;
                                fd.vector3d.Y = -1.0d;
                                fd.Flag = -1;
                                FontList.Add(fd);
                            }
                        }
                        else
                        {
                            fd.vector3d.X = -1.0d;
                            fd.vector3d.Y = -1.0d;
                            fd.Flag = -1;
                            FontList.Add(fd);
                        }
                        MyData.Add(idx, FontList);
                        idx++;
                    }
                }

                if (MyData.Count <= 32)
                {
                    //System.Windows.MessageBox.Show("Invalid font file.");
                    //imgsource = null;
                    ErrorCode = "00HMF0000001";
                    ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font file data  is invalid (count = {0}", MyData.Count), Thread.CurrentThread.ManagedThreadId);
                    return -5;
                }
                ErrorCode = "";
                ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return 0;
            }
            catch (Exception ex)
            {
                ErrorCode = string.Format("00HE{0:X8}", Math.Abs(ex.HResult));
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;
                //ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}", ErrorCode));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        //public static int GetFontDataEx(VinNoInfo vininfo, byte bHeadType, double density, ref List<List<FontDataClass>> MyData, ref List<List<FontDataClass>> reverseData, ref double fontsizeX, ref double fontsizeY, ref double shiftVal, ref string ErrorCode)
        //public static int GetFontDataEx(VinNoInfo vininfo, byte bHeadType, double density, double rotateAngle, ref List<List<FontDataClass>> fontData, ref double fontsizeX, ref double fontsizeY, ref double shiftVal, ref string ErrorCode)
        public static ITNTResponseArgs GetFontDataEx(VinNoInfo vininfo, byte bHeadType, double density, byte fontdiretion, ref List<List<FontDataClass>> fontData, ref double fontsizeX, ref double fontsizeY, ref double shiftVal, ref string ErrorCode)
        {
            string className = "ImageProcessManager";
            string funcName = "GetFontDataEx";
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = "";
            FileInfo fi;
            string lineFontValue = "";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string log = "";
            string sCurrentFunc = "GET FONT DATA";

            try
            {
                ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                fontsizeX = 7.0d;
                fontsizeY = 4.0d;
                shiftVal = 0;

                if (bHeadType == 0)
                    filename = curDir + "FONT/S_" + vininfo.fontName + ".FON";
                else
                {
                    if (density > 1)
                        filename = curDir + "FONTL/S_" + vininfo.fontName + ".FON";
                    else
                        filename = curDir + "FONTL/D_" + vininfo.fontName + ".FON";
                }

                fi = new FileInfo(filename);
                if (fi.Exists == false)
                {
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);

                    retval.execResult = ErrorCodeConstant.ERROR_FILE_NOT_FOUND;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorMessage = "NO FONT FILE FOUND - " + filename;
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_PARAM + Constants.ERROR_NO_FONT_FILE;

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    return retval;
                }

                using (StreamReader sr = new StreamReader(filename))
                {
                    string[] fontsize;
                    lineFontValue = sr.ReadLine();
                    fontsize = lineFontValue.Split(',');
                    if (fontsize.Length < 2)
                    {
                        ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                        fontsizeX = 7.0d;
                        fontsizeY = 4.0d;
                        shiftVal = 0;
                    }
                    else
                    {
                        double.TryParse(fontsize[0], out fontsizeX);
                        double.TryParse(fontsize[1], out fontsizeY);
                        //fontsizeX = Math.Max(Convert.ToDouble(fontsize[0]), 0.01d);
                        //fontsizeY = Math.Max(Convert.ToDouble(fontsize[1]), 0.01d);
                        if (fontsize.Length >= 3)
                            double.TryParse(fontsize[2], out shiftVal);
                        //shiftVal = Math.Max(Convert.ToDouble(fontsize[2]), 0.0d);
                        else
                        {
                            if (bHeadType == 0)
                            {
                                if (vininfo.fontName == "5X7")
                                    shiftVal = 3.0d;
                                else if (vininfo.fontName == "11X16" || vininfo.fontName == "OCR")
                                    shiftVal = 5.0d;
                            }
                        }
                        //shiftVal = 0;
                    }

                    for (int i = 0; i < vininfo.vinNo.Length; i++)
                    {
                        //List<FontDataClass> fontlist = new List<FontDataClass>();
                        List<FontDataClass> rfontlist = new List<FontDataClass>();
                        string[] pointList;
                        string[] point;

                        int charNum = (int)vininfo.vinNo[i] - 1;
                        lineFontValue = File.ReadLines(filename).Skip(charNum).Take(1).First();
                        //FontDataClass fd;
                        if (lineFontValue.Length > 0)
                        {
                            pointList = lineFontValue.Split(';');
                            if (pointList.Count() > 2)
                            {
                                //bool find = false;
                                double x = -1, y = -1, ry = -1;
                                int flag = -1;

                                for (int j = 0; j < pointList.Count(); j++)
                                {
                                    point = pointList[j].Split(',');
                                    if (point.Count() >= 3)
                                    {
                                        //FontDataClass fd = new FontDataClass();
                                        //FontDataClass rfd = new FontDataClass();

                                        double.TryParse(point[0], out x);
                                        double.TryParse(point[1], out y);
                                        if (fontdiretion == 0)
                                            ry = y - shiftVal;
                                        else
                                            ry = (fontsizeY - 1) + shiftVal - y;
                                        int.TryParse(point[2], out flag);
                                        //fontlist.Add(new FontDataClass(x, y, 0, flag));
                                        rfontlist.Add(new FontDataClass(x, ry, 0, flag));
                                    }
                                }
                            }
                            //else
                            //{
                            //    fd.vector3d.X = 0;
                            //    fd.vector3d.Y = 0;
                            //    fd.Flag = -1;
                            //    rfd.vector3d.X = 0;
                            //    rfd.vector3d.Y = 0;
                            //    rfd.Flag = -1;
                            //    fontlist.Add(fd);
                            //    rfontlist.Add(rfd);
                            //}
                        }
                        //MyData.Add(fontlist);
                        fontData.Add(rfontlist);
                    }
                }

                //if (MyData.Count <= 32)
                //{
                //    //System.Windows.MessageBox.Show("Invalid font file.");
                //    //imgsource = null;
                //    ErrorCode = "00HMF0000001";
                //    ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font file data  is invalid (count = {0}", MyData.Count), Thread.CurrentThread.ManagedThreadId);
                //    return -5;
                //}
                ErrorCode = "";
                ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                retval.execResult = 0;
                return retval;
            }
            catch (Exception ex)
            {
                ErrorCode = string.Format("00HE{0:X8}", Math.Abs(ex.HResult));
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
        }

        public static ITNTResponseArgs GetFontDataOneEx(char vinChar, string fontName, byte bHeadType, double density, byte fontdirection, ref List<FontDataClass> fontData, out double fontsizeX, out double fontsizeY, out double shiftVal, out string ErrorCode)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;

            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = "";
            string lineFontValue = "";
            FileInfo fi;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sCurrentFunc = "GET FONT DATA";
            //double subShift = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                fontsizeX = 4.0d;
                fontsizeY = 7.0d;
                shiftVal = 0.0d;

                if (bHeadType == 0)
                    filename = curDir + "FONT/S_" + fontName + ".FON";
                else
                {
                    if (density > 1)
                        filename = curDir + "FONTL/S_" + fontName + ".FON";
                    else
                        filename = curDir + "FONTL/D_" + fontName + ".FON";
                }

                fi = new FileInfo(filename);
                if (fi.Exists == false)
                {
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);

                    retval.execResult = ErrorCodeConstant.ERROR_FILE_NOT_FOUND;
                    retval.errorInfo.sErrorMessage = "FONT FILE IS NOT FOUND";
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_PARAM + Constants.ERROR_NO_FONT_FILE;

                    retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                    retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    return retval;
                }

                using (StreamReader sr = new StreamReader(filename))
                {
                    string[] pointList;
                    string[] point;

                    string[] fontsize;
                    lineFontValue = sr.ReadLine();
                    fontsize = lineFontValue.Split(',');
                    if (fontsize.Length < 2)
                    {
                        ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                        fontsizeX = 4.0d;
                        fontsizeY = 7.0d;
                        shiftVal = 0;
                    }
                    else
                    {
                        double.TryParse(fontsize[0], out fontsizeX);
                        double.TryParse(fontsize[1], out fontsizeY);
                        if (fontsize.Length >= 3)
                            double.TryParse(fontsize[2], out shiftVal);
                        else
                        {
                            if (bHeadType == 0)
                            {
                                if (fontName == "5X7")
                                    shiftVal = 3.0d;
                                else if (fontName == "11X16" || fontName == "OCR")
                                    shiftVal = 5.0d;
                            }
                        }

                        //fontsizeX = Math.Max(Convert.ToDouble(fontsize[0]), 0.01d);
                        //fontsizeY = Math.Max(Convert.ToDouble(fontsize[1]), 0.01d);
                        //if (fontsize.Length >= 3)
                        //{
                        //    shiftVal = Math.Max(Convert.ToDouble(fontsize[2]), 0.01d);

                        //}
                        //else
                        //    shiftVal = 0;
                    }

                    //fontsizeX = Math.Max((Convert.ToDouble(fontsize[0])), 1.0d);
                    //fontsizeY = Math.Max((Convert.ToDouble(fontsize[1])), 1.0d);

                    int charNum = (int)vinChar - 1;
                    lineFontValue = File.ReadLines(filename).Skip(charNum).Take(1).First();
                    //FontDataClass fd;
                    if (lineFontValue.Length > 0)
                    {
                        pointList = lineFontValue.Split(';');
                        if (pointList.Count() > 0)
                        {
                            //bool find = false;
                            double x = -1, y = -1, ry = -1;
                            int flag = -1;

                            for (int i = 0; i < pointList.Count(); i++)
                            {
                                point = pointList[i].Split(',');
                                if (point.Count() >= 3)
                                {
                                    double.TryParse(point[0], out x);
                                    double.TryParse(point[1], out y);
                                    if (fontdirection == 0)
                                        ry = y - shiftVal;
                                    else
                                        ry = (fontsizeY - 1) + shiftVal - y;
                                    int.TryParse(point[2], out flag);
                                    fontData.Add(new FontDataClass(x, ry, 0, flag));

                                    //fd.vector3d.X = Convert.ToDouble(point[0]);
                                    //if(fontdirection == 0)
                                    //    fd.vector3d.Y = Convert.ToDouble(point[1]) - shiftVal;
                                    //else
                                    //    fd.vector3d.Y = (fontsizeY - 1) + shiftVal - Convert.ToDouble(point[1]);

                                    //fd.Flag = Convert.ToInt32(point[2]);
                                    //fontData.Add(fd);
                                }
                            }
                        }
                    }
                }

                ErrorCode = "";
                ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ErrorCode = string.Format("00HE{0:X8}", Math.Abs(ex.HResult));
                fontsizeX = 4.0d;
                fontsizeY = 7.0d;
                shiftVal = 0;
                //ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}", ErrorCode));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //return ex.HResult;

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                return retval;
            }
        }



        //public ITNTResponseArgs MakeImageFromFontEx(VinNoInfo vin, Thickness margin, byte bHeadType, double density, byte saveFlag, byte fontdiretion, out string ErrorCode, string saveFile = "ImageFromFont.jpg")
        //{
        //    //DateTime beginTime = DateTime.Now;

        //    double pitch = vin.pitch;
        //    double width = vin.width;
        //    double height = vin.height;
        //    double fontsizeX = 4;
        //    double fontsizeY = 7;
        //    double shiftVal = 0;
        //    //List<List<FontDataClass>> MyData = new List<List<FontDataClass>>();
        //    List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();

        //    string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = MethodBase.GetCurrentMethod().Name;

        //    //Stopwatch sw = new Stopwatch();
        //    //int retval = 0;
        //    ITNTResponseArgs retval = new ITNTResponseArgs(128);

        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //        ErrorCode = "";
        //        if (vin.vinNo.Length <= 0)
        //        {
        //            ErrorCode = "00HMV0000001";
        //            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : INVALID VINNUMBER", Thread.CurrentThread.ManagedThreadId);
        //            retval.sErrorMessage = "NO VIN";
        //            retval.execResult = -1;
        //            return retval;
        //        }

        //        //int retval = GetFontDataEx(vin, bHeadType, density, ref MyData, ref reverseData, ref fontsizeX, ref fontsizeY, ref shiftVal, ref ErrorCode);
        //        retval = GetFontDataEx(vin, bHeadType, density, fontdiretion, ref fontData, ref fontsizeX, ref fontsizeY, ref shiftVal, ref ErrorCode);
        //        if (retval.execResult != 0)
        //        {
        //            return retval;
        //        }

        //        if (canvas.CheckAccess())
        //        {
        //            MakeNSaveFileEx(vin, bHeadType, density, fontsizeX, fontsizeY, shiftVal, margin, fontData, saveFlag, saveFile);
        //        }
        //        else
        //        {
        //            canvas.Dispatcher.Invoke(new Action(delegate
        //            {
        //                MakeNSaveFileEx(vin, bHeadType, density, fontsizeX, fontsizeY, shiftVal, margin, fontData, saveFlag, saveFile);
        //            }));
        //        }

        //        ErrorCode = "";
        //        //sw.Stop();
        //        retval.execResult = 0;
        //        ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //        //ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("END (TIMESPAN  = {0})", sw.ElapsedMilliseconds), Thread.CurrentThread.ManagedThreadId);
        //        return retval;
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorCode = string.Format("00HE{0:X8}", Math.Abs(ex.HResult));
        //        fontsizeX = 0.0d;
        //        fontsizeY = 0.0d;

        //        retval.sErrorCode = "MAKE IMAGE EXCEPTION : " + ex.Message;
        //        retval.execResult = ex.HResult;
        //        //ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}", ErrorCode));
        //        ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION2 - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        return retval;
        //    }
        //}

        public void SaveFontTiff(BitmapSource img, string fileName)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            string path = System.IO.Path.GetDirectoryName(fileName);
            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);

            try
            {
                var stream = new FileStream(fileName, FileMode.Create);
                var encoder = new TiffBitmapEncoder();
                //var myTextBlock = new TextBlock();
                //myTextBlock.Text = "Codec Author is: " + encoder.CodecInfo.Author.ToString();
                encoder.Compression = TiffCompressOption.Zip;
                encoder.Frames.Add(BitmapFrame.Create(img));
                encoder.Save(stream);

                //IFormatter formatter = new BinaryFormatter();
                //Stream stream = File.Create(fileName); //System.IO

                //JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                //encoder.QualityLevel = 80;

                //encoder.Frames.Add(BitmapFrame.Create(img));
                //encoder.Save(stream);

                stream.Dispose();
                ////stream.Flush(); //Dispose나 Flush 둘중에 아무거나 사용해도 됨.
                stream.Close();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        public Media.ImageSource ToImageSource(FrameworkElement obj)
        {
            // Save current canvas transform
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            Media.Transform transform = obj.LayoutTransform;
            obj.LayoutTransform = null;

            // fix margin offset as well
            Thickness margin = obj.Margin;
            obj.Margin = new Thickness(0, 0, margin.Right - margin.Left, margin.Bottom - margin.Top);

            // Get the size of canvas
            System.Windows.Size size = new System.Windows.Size(obj.Width, obj.Height);

            // force control to Update
            obj.Measure(size);
            obj.Arrange(new Rect(size));

            obj.UpdateLayout();
            RenderTargetBitmap bmp = new RenderTargetBitmap(
                (int)obj.Width, (int)obj.Height, 0, 0, Media.PixelFormats.Default);

            bmp.Render(obj);

            // return values as they were before
            obj.LayoutTransform = transform;
            obj.Margin = margin;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return bmp;
        }

        public void SaveFontImage(BitmapSource img, string fileName)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            //string path = System.IO.Path.GetDirectoryName(fileName);
            //if (Directory.Exists(path) == false)
            //    Directory.CreateDirectory(path);

            try
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = File.Create(fileName); //System.IO

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 80;

                encoder.Frames.Add(BitmapFrame.Create(img));
                encoder.Save(stream);

                stream.Dispose();
                //stream.Flush(); //Dispose나 Flush 둘중에 아무거나 사용해도 됨.
                stream.Close();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Exception - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private void MakeNSaveFile(VinNoInfo vin, double width, double height, double pitch, Thickness margin, double fontsizeX, double fontsizeY, Dictionary<int, List<FontDataClass>> MyData, byte saveFlag, string saveFile = "ImageFromFont.jpg")
        {
            double TextWidth = ((vin.vinNo.Length - 1) * pitch + width) * Util.PXPERMM;
            double TextHeight = Util.PXPERMM * height;

            //Canvas canvas = new Canvas();
            canvas.Width = TextWidth + margin.Left + margin.Right;
            canvas.Height = TextHeight + margin.Top + margin.Bottom;
            canvas.UpdateLayout();

            string value = "";
            Media.Color bcolor;
            Util.GetPrivateProfileValue("CANVAS", "Background", "", ref value, "ImageConfig.ini");
            if (value.Length <= 0)
                canvas.Background = Media.Brushes.Gray;
            else
            {
                bcolor = (Media.Color)Media.ColorConverter.ConvertFromString(value);
                canvas.Background = new Media.SolidColorBrush(bcolor);
            }

            Media.Brush brush;
            Util.GetPrivateProfileValue("CANVAS", "Foreground", "", ref value, "ImageConfig.ini");
            if (value.Length <= 0)
                brush = new Media.SolidColorBrush(Media.Color.FromArgb(255, (byte)50, (byte)50, (byte)50));
            else
            {
                Media.Color fcolor = (Media.Color)Media.ColorConverter.ConvertFromString(value);
                brush = new Media.SolidColorBrush(fcolor);
            }

            //Media.Brush brush = new Media.SolidColorBrush(Media.Color.FromArgb(255, (byte)200, (byte)200, (byte)200));
            //canvas.Background = Media.Brushes.Gray;

            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
            rect.Width = canvas.Width;
            rect.Height = canvas.Height;

            double startX = Math.Max(0, (rect.Width - TextWidth) / 2);
            double startY = Math.Max(0, (rect.Height - TextHeight) / 2);
            Canvas.SetLeft(rect, 0.0d);
            Canvas.SetTop(rect, 0.0d);

            canvas.Children.Add(rect);
            canvas.Children.Clear();
            /***********************************
            1 inch  25.4mm
            1 inch  72 pt
            1 inch  96 px        dpi
            1 mm    2.83465 pt
            1 mm    3.7795 px    dpi/ 25.4
            ***********************************/
            double CharHeight = height * Util.PXPERMM;
            double CharWidth = width * Util.PXPERMM;
            double pitch_px = pitch * Util.PXPERMM;
            int charNum;

            for (int i = 0; i < vin.vinNo.Length; i++)
            {
                charNum = (int)vin.vinNo[i] - 1;
                if ((charNum < 32) || (charNum > 128))
                {
                    continue;
                }
                else
                {
                    Line[] line = new Line[MyData[charNum].Count];
                    int index = 0;
                    for (int j = 0; j < MyData[charNum].Count; j++)
                    {
                        if (MyData[charNum][j].Flag == 1)
                        {
                            line[index] = new Line();
                            line[index].Stroke = brush;
                            line[index].StrokeThickness = vin.thickness * Util.PXPERMM;
                            line[index].StrokeStartLineCap = Media.PenLineCap.Round;
                            line[index].StrokeEndLineCap = Media.PenLineCap.Round;
                            line[index].StrokeLineJoin = Media.PenLineJoin.Round;

                            line[index].X1 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / (fontsizeX - 1)+ 0 + pitch_px * (double)i;
                            line[index].Y1 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / (fontsizeY - 1);
                        }
                        else if (MyData[charNum][j].Flag == 2)
                        {
                            if (line[index] != null)
                            {
                                line[index].X2 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / (fontsizeX - 1) + 0 + pitch_px * (double)(i);
                                line[index].Y2 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / (fontsizeY - 1);

                                //test.Add(line[index]);

                                canvas.Children.Add(line[index]);
                                index++;
                            }
                            line[index] = new Line();
                            line[index].Stroke = brush;
                            line[index].StrokeThickness = vin.thickness * Util.PXPERMM;
                            line[index].StrokeStartLineCap = Media.PenLineCap.Round;
                            line[index].StrokeEndLineCap = Media.PenLineCap.Round;
                            line[index].StrokeLineJoin = Media.PenLineJoin.Round;

                            line[index].X1 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / (fontsizeX - 1) + 0 + pitch_px * (double)(i);
                            line[index].Y1 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / (fontsizeY - 1);
                        }
                        else if (MyData[charNum][j].Flag == 4)
                        {
                            if (line[index] != null)
                            {
                                line[index].X2 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / (fontsizeX - 1) + 0 + pitch_px * (double)(i);
                                line[index].Y2 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / (fontsizeY - 1);
                                //test.Add(line[index]);
                                canvas.Children.Add(line[index]);
                            }
                        }
                        else
                        {
                        }
                    }
                }
            }
            Media.ImageSource img = ToImageSource(canvas); //첫번째 호출 함수
            if (img != null)
            {
                if (saveFlag == 0)
                    SaveFontImage(img as BitmapSource, saveFile); //두번째 호출함수
                else
                    SaveFontTiff(img as BitmapSource, saveFile); //두번째 호출함수
            }
        }

        private void MakeNSaveFileEx(VinNoInfo vininfo, byte bHeadType, double density, double fontsizeX, double fontsizeY, double shiftVal, Thickness margin, List<List<FontDataClass>> MyData, byte saveFlag, string saveFile = "ImageFromFont.jpg")
        {
            double TextWidth = 0;
            double TextHeight = 0;
            string value = "";
            Media.Color bcolor;
            string className = "ImageProcessManager";
            string funcName = "MakeNSaveFileEx";
            Media.Brush brush;
            double startX = 0;
            double startY = 0;
            double CharHeight = 0;
            double CharWidth = 0;
            double pitch_px = 0;
            int charNum;
            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                TextWidth = ((vininfo.vinNo.Length - 1) * vininfo.pitch + vininfo.width) * Util.PXPERMM;
                TextHeight = Util.PXPERMM * vininfo.height;

                //Canvas canvas = new Canvas();
                canvas.Width = TextWidth + margin.Left + margin.Right;
                canvas.Height = TextHeight + margin.Top + margin.Bottom;
                canvas.UpdateLayout();

                Util.GetPrivateProfileValue("CANVAS", "Background", "", ref value, "ImageConfig.ini");
                if (value.Length <= 0)
                    canvas.Background = Media.Brushes.Gray;
                else
                {
                    bcolor = (Media.Color)Media.ColorConverter.ConvertFromString(value);
                    canvas.Background = new Media.SolidColorBrush(bcolor);
                }

                Util.GetPrivateProfileValue("CANVAS", "Foreground", "", ref value, "ImageConfig.ini");
                if (value.Length <= 0)
                    brush = new Media.SolidColorBrush(Media.Color.FromArgb(255, (byte)50, (byte)50, (byte)50));
                else
                {
                    Media.Color fcolor = (Media.Color)Media.ColorConverter.ConvertFromString(value);
                    brush = new Media.SolidColorBrush(fcolor);
                }

                //Media.Brush brush = new Media.SolidColorBrush(Media.Color.FromArgb(255, (byte)200, (byte)200, (byte)200));
                //canvas.Background = Media.Brushes.Gray;

                rect.Width = canvas.Width;
                rect.Height = canvas.Height;

                startX = Math.Max(0, (rect.Width - TextWidth) / 2);
                startY = Math.Max(0, (rect.Height - TextHeight) / 2);
                Canvas.SetLeft(rect, 0.0d);
                Canvas.SetTop(rect, 0.0d);

                canvas.Children.Add(rect);
                canvas.Children.Clear();
                /***********************************
                1 inch  25.4mm
                1 inch  72 pt
                1 inch  96 px        dpi
                1 mm    2.83465 pt
                1 mm    3.7795 px    dpi/ 25.4
                ***********************************/
                CharHeight = vininfo.height * Util.PXPERMM;
                CharWidth = vininfo.width * Util.PXPERMM;
                pitch_px = vininfo.pitch * Util.PXPERMM;

                for (int i = 0; i < vininfo.vinNo.Length; i++)
                {
                    charNum = (int)vininfo.vinNo[i] - 1;
                    if ((charNum < 32) || (charNum > 128))
                    {
                        continue;
                    }
                    else
                    {
                        Line[] line = new Line[MyData[charNum].Count];
                        int index = 0;
                        for (int j = 0; j < MyData[charNum].Count; j++)
                        {
                            if (MyData[charNum][j].Flag == 1)
                            {
                                line[index] = new Line();
                                line[index].Stroke = brush;
                                line[index].StrokeThickness = vininfo.thickness * Util.PXPERMM;
                                line[index].StrokeStartLineCap = Media.PenLineCap.Round;
                                line[index].StrokeEndLineCap = Media.PenLineCap.Round;
                                line[index].StrokeLineJoin = Media.PenLineJoin.Round;

                                line[index].X1 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / (fontsizeX - 1) + 0 + pitch_px * (double)i;
                                line[index].Y1 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / (fontsizeY - 1);
                            }
                            else if (MyData[charNum][j].Flag == 2)
                            {
                                if (line[index] != null)
                                {
                                    line[index].X2 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / (fontsizeX - 1) + 0 + pitch_px * (double)(i);
                                    line[index].Y2 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / (fontsizeY - 1);

                                    //test.Add(line[index]);

                                    canvas.Children.Add(line[index]);
                                    index++;
                                }
                                line[index] = new Line();
                                line[index].Stroke = brush;
                                line[index].StrokeThickness = vininfo.thickness * Util.PXPERMM;
                                line[index].StrokeStartLineCap = Media.PenLineCap.Round;
                                line[index].StrokeEndLineCap = Media.PenLineCap.Round;
                                line[index].StrokeLineJoin = Media.PenLineJoin.Round;

                                line[index].X1 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / (fontsizeX - 1) + 0 + pitch_px * (double)(i);
                                line[index].Y1 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / (fontsizeY - 1);
                            }
                            else if (MyData[charNum][j].Flag == 4)
                            {
                                if (line[index] != null)
                                {
                                    line[index].X2 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / (fontsizeX - 1) + 0 + pitch_px * (double)(i);
                                    line[index].Y2 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / (fontsizeY - 1);
                                    //test.Add(line[index]);
                                    canvas.Children.Add(line[index]);
                                }
                            }
                            else
                            {
                            }
                        }
                    }
                }
                Media.ImageSource img = ToImageSource(canvas); //첫번째 호출 함수
                if (img != null)
                {
                    if (saveFlag == 0)
                        SaveFontImage(img as BitmapSource, saveFile); //두번째 호출함수
                    else
                        SaveFontTiff(img as BitmapSource, saveFile); //두번째 호출함수
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        public BitmapImage ConvertWriteablebitmapToBitmapimage(WriteableBitmap wbm)
        {
            BitmapImage bmpimg = new BitmapImage();
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(wbm));
                    encoder.Save(stream);
                    bmpimg.BeginInit();
                    bmpimg.CacheOption = BitmapCacheOption.OnLoad;
                    bmpimg.StreamSource = stream;
                    bmpimg.EndInit();
                    bmpimg.Freeze();
                }
                return bmpimg;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "HalconControl", "BitmapImage", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return null;
            }
        }


        public static ITNTResponseArgs GetPatternValue(string name, byte byheadType, ref PatternValueEx pattern)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string sCurrentFunc = "GET PATTERN VALUE";

            try
            {
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                FileInfo fi = new FileInfo(patternfile);
                if (fi.Exists == false)
                {
                    //retval.execResult = -1;
                    //retval.errorInfo.sErrorMessage = "PATTERN FILE NOT FOUND - " + name;
                    //retval.sErrorMessage = "NO PATTERN FILE FOUND (" + patternfile + ")";

                    retval.execResult = ErrorCodeConstant.ERROR_PATTERN_NOTFOUND;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    //retval.errorInfo.sErrorMessage = "NO MARKING DATA FOUND";
                    retval.errorInfo.sErrorMessage = "PATTERN FILE NOT FOUND - " + name;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_PARAM + Constants.ERROR_NO_PATTERN_FILE;

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    return retval;
                }

                pattern.name = name;
                retval = GetPatternFontValue(name, byheadType, ref pattern.fontValue);
                if(retval.execResult != 0) return retval;
                retval = GetPatternSpeedValue(name, byheadType, ref pattern.speedValue);
                if (retval.execResult != 0) return retval;
                retval = GetPatternHeadValue(name, byheadType, ref pattern.headValue);
                if (retval.execResult != 0) return retval;
                retval = GetPatternPositionValue(name, byheadType, ref pattern.positionValue);
                if (retval.execResult != 0) return retval;
                retval = GetPatternLaserValue(name, byheadType, ref pattern.laserValue);
                if (retval.execResult != 0) return retval;
                retval = GetPatternScanValue(name, byheadType, ref pattern.scanValue);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "ImageProcessManager", "GetPatternData", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //retval.execResult = ex.HResult;
                //retval.errorInfo.sErrorMessage = retval.errorInfo."EXCEPTION = " + ex.Message;
                //retval.sErrorMessage = "PATTERN DATA EXCEPTION : " + ex.Message;


                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = sDeviceCode + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                //recvArg.execResult = (int)COMMUNICATIONERROR.ERR_PORT_NOT_OPENED;
                retval.errorInfo.devErrorInfo.sDeviceName = sDeviceName;
                retval.errorInfo.devErrorInfo.sDeviceCode = sDeviceCode;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;

                return retval;
            }
        }

        private static bool SectionNameExist(string filename, string section)
        {
            string[] sections = GetSectionNames(filename);

            if (sections.Length <= 0)
                return false;

            if (sections.Contains(section))
                return true;
            else
                return false;
        }

        private static string[] GetSectionNames(string path)
        {
            byte[] buffer = new byte[1024];
            Util.GetPrivateProfileSectionNames(buffer, buffer.Length, path);
            string allSections = System.Text.Encoding.Default.GetString(buffer);
            string[] sectionNames = allSections.Split('\0');
            return sectionNames;
        }

        public static void GetStartPointLinear(int count, Point CP, Point START_XY, double PITCH, double ANG, ref List<Point> POS)
        {
            int i;
            Point pt = new Point();
            try
            {
                for (i = 0; i <= count - 1; i++)
                {
                    pt = new Point();
                    pt = Rotate_Point(START_XY.X + (i * PITCH), START_XY.Y, CP.X, CP.Y, ANG);
                    POS.Add(pt);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetStartPointLinear Exception - {0}, {1}", ex.HResult, ex.Message);
            }
        }

        public static void GetStartPointLinear(int count, Vector3D CP, Vector3D START_XY, double PITCH, double ANG, ref List<Vector3D> POS)
        {
            int i;
            Vector3D pt = new Vector3D();
            try
            {
                for (i = 0; i <= count - 1; i++)
                {
                    pt = Rotate_Point2(START_XY.X + (i * PITCH), START_XY.Y, CP.X, CP.Y, ANG);
                    POS.Add(pt);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetStartPointLinear Exception - {0}, {1}", ex.HResult, ex.Message);
            }
        }

        public static Point Rotate_Point(double tx, double ty, double cx, double cy, double deg)
        {
            //MarkController.MPOINT returnValue = default;
            Point returnValue = new Point();
            double nx;
            double ny;
            double q;

            try
            {
                q = deg * System.Math.PI / 180;

                System.Double cosq = System.Math.Cos(q);
                System.Double sinq = System.Math.Sin(q);
                tx -= cx;
                ty -= cy;

                nx = tx * cosq - ty * sinq;// double.Parse(Microsoft.VisualBasic.Strings.Format(tx * cosq - ty * sinq, "000.0000"));
                ny = ty * cosq + tx * sinq;//double.Parse(Microsoft.VisualBasic.Strings.Format(ty * cosq + tx * sinq, "000.0000"));
                nx += cx;
                ny += cy;
                returnValue.X = nx;
                returnValue.Y = ny;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Rotate_Point Exception - {0}, {1}", ex.HResult, ex.Message);
            }
            return returnValue;
        }

        public static Vector3D Rotate_Point2(double tx, double ty, double cx, double cy, double deg)
        {
            //MarkController.MPOINT returnValue = default;
            Vector3D retval = new Vector3D();
            double nx;
            double ny;
            double q;

            try
            {
                q = deg * System.Math.PI / 180;

                System.Double cosq = System.Math.Cos(q);
                System.Double sinq = System.Math.Sin(q);
                tx -= cx;
                ty -= cy;

                nx = tx * cosq - ty * sinq;// double.Parse(Microsoft.VisualBasic.Strings.Format(tx * cosq - ty * sinq, "000.0000"));
                ny = ty * cosq + tx * sinq;//double.Parse(Microsoft.VisualBasic.Strings.Format(ty * cosq + tx * sinq, "000.0000"));
                nx += cx;
                ny += cy;
                retval.X = nx;
                retval.Y = ny;
                retval.Z = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Rotate_Point Exception - {0}, {1}", ex.HResult, ex.Message);
            }
            return retval;
        }

        public static string GetFontFileName(byte byHeadType, string fontName, /*string patternName,*/ double density)
        {
            string className = "ImageProcessManager";
            string funcName = "GetFontFileName";
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            //string value = "";
            string retval = "";

            try
            {
                if (byHeadType == 0)
                    retval = curDir + "FONT/S_" + fontName + ".FON";
                else
                {
                    if (density > 1)
                        retval = curDir + "FONTL/S_" + fontName + ".FON";
                    else
                        retval = curDir + "FONTL/D_" + fontName + ".FON";
                }
            }
            catch (Exception ex)
            {
                retval = curDir + "FONT/S_" + fontName + ".FON";
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END - " + retval, Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        //GetPatternFontValue(      name, ref pattern.fontValue);
        //GetPatternSpeedValue(     name, ref pattern.speedValue);
        //GetPatternHeadValue(      name, ref pattern.headValue);
        //GetPatternPositionValue(  name, ref pattern.positionValue);
        //GetPatternLaserValue(     name, ref pattern.laserValue);
        //GetPatternScanValue(      name, ref pattern.scanValue);


        public static ITNTResponseArgs GetPatternFontValue(string name, byte byheadType, ref FontValue font)
        {
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetPatternFontValue";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);

            try
            {
                retval.errorInfo.sErrorFunc = "GET PATTERN FONT VALUE";
                FileInfo fi = new FileInfo(patternfile);
                if(fi.Exists == false)
                {
                    retval.execResult = -1;
                    retval.errorInfo.sErrorMessage = "PATTERN FILE NOT FOUND - " + name;
                    //retval.sErrorMessage = "NO PATTERN FILE : " + patternfile;
                    return retval;
                }
                Util.GetPrivateProfileValue("FONT", "NAME", "OCR", ref font.fontName, patternfile); // load FONT
                font.width = (double)Util.GetPrivateProfileValueDouble("FONT", "WIDTH", 4, patternfile);
                font.height = (double)Util.GetPrivateProfileValueDouble("FONT", "HEIGHT", 7, patternfile);
                font.pitch = (double)Util.GetPrivateProfileValueDouble("FONT", "PITCH", 6, patternfile);
                font.rotateAngle = (double)Util.GetPrivateProfileValueDouble("FONT", "ROTATEANGLE", 0, patternfile);
                font.strikeCount = (short)Util.GetPrivateProfileValueUINT("FONT", "STRIKECOUNT", 0, patternfile);
                font.thickness = (double)Util.GetPrivateProfileValueDouble("FONT", "THICKNESS", 0.5, patternfile);

                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = retval.errorInfo.sErrorFunc + " = " + ex.Message;

                //retval.sErrorMessage = "FONT DATA EXCEPTION : " + ex.Message; 
                return retval;
            }
        }


        public static ITNTResponseArgs GetPatternSpeedValue(string name, byte byheadType, ref SpeedValue speedValue)
        {
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetPatternSpeedValue";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);

            try
            {
                retval.errorInfo.sErrorFunc = "GET PATTERN SPEED VALUE";
                FileInfo fi = new FileInfo(patternfile);
                if (fi.Exists == false)
                {
                    retval.execResult = -1;
                    retval.errorInfo.sErrorMessage = "PATTERN FILE NOT FOUND - " + name;
                    //retval.sErrorMessage = "NO PATTERN FILE : " + patternfile;
                    return retval;
                }

                //speed value
                if (byheadType == 0)
                {
                    speedValue.initSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("LOAD", "INITIALSPEED", 20, patternfile);
                    speedValue.targetSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("LOAD", "TARGETSPEED", 40, patternfile);
                    speedValue.accelSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("LOAD", "ACCELERATION", 15, patternfile);
                    speedValue.decelSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("LOAD", "DECELERATION", 15, patternfile);

                    //speedValue.initSpeed4Home = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "INITIALSPEED", 50, patternfile);
                    //speedValue.targetSpeed4Home = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "TARGETSPEED", 100, patternfile);
                    //speedValue.accelSpeed4Home = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "ACCELERATION", 400, patternfile);
                    //speedValue.decelSpeed4Home = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "DECELERATION", 400, patternfile);

                    speedValue.initSpeed4Fast = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "INITIALSPEED", 50, patternfile);
                    speedValue.targetSpeed4Fast = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "TARGETSPEED", 100, patternfile);
                    speedValue.accelSpeed4Fast = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "ACCELERATION", 400, patternfile);
                    speedValue.decelSpeed4Fast = (short)Util.GetPrivateProfileValueUINT("NOLOAD", "DECELERATION", 400, patternfile);
                }
                else
                {
                    speedValue.initSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("MARKVECTOR", "INITIALSPEED", 20, patternfile);
                    speedValue.targetSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("MARKVECTOR", "TARGETSPEED", 40, patternfile);
                    speedValue.accelSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("MARKVECTOR", "ACCELERATION", 15, patternfile);
                    speedValue.decelSpeed4MarkV = (short)Util.GetPrivateProfileValueUINT("MARKVECTOR", "DECELERATION", 15, patternfile);

                    speedValue.initSpeed4Home = (short)Util.GetPrivateProfileValueUINT("MOVINGHOME", "INITIALSPEED", 50, patternfile);
                    speedValue.targetSpeed4Home = (short)Util.GetPrivateProfileValueUINT("MOVINGHOME", "TARGETSPEED", 100, patternfile);
                    speedValue.accelSpeed4Home = (short)Util.GetPrivateProfileValueUINT("MOVINGHOME", "ACCELERATION", 400, patternfile);
                    speedValue.decelSpeed4Home = (short)Util.GetPrivateProfileValueUINT("MOVINGHOME", "DECELERATION", 400, patternfile);

                    speedValue.initSpeed4Fast = (short)Util.GetPrivateProfileValueUINT("MOVINGFAST", "INITIALSPEED", 50, patternfile);
                    speedValue.targetSpeed4Fast = (short)Util.GetPrivateProfileValueUINT("MOVINGFAST", "TARGETSPEED", 100, patternfile);
                    speedValue.accelSpeed4Fast = (short)Util.GetPrivateProfileValueUINT("MOVINGFAST", "ACCELERATION", 400, patternfile);
                    speedValue.decelSpeed4Fast = (short)Util.GetPrivateProfileValueUINT("MOVINGFAST", "DECELERATION", 400, patternfile);

                    speedValue.initSpeed4MarkR = (short)Util.GetPrivateProfileValueUINT("MARKRASTER", "INITIALSPEED", 50, patternfile);
                    speedValue.targetSpeed4MarkR = (short)Util.GetPrivateProfileValueUINT("MARKRASTER", "TARGETSPEED", 100, patternfile);
                    speedValue.accelSpeed4MarkR = (short)Util.GetPrivateProfileValueUINT("MARKRASTER", "ACCELERATION", 400, patternfile);
                    speedValue.decelSpeed4MarkR = (short)Util.GetPrivateProfileValueUINT("MARKRASTER", "DECELERATION", 400, patternfile);

                    speedValue.initSpeed4Measure = (short)Util.GetPrivateProfileValueUINT("MOVINGMEASURE", "INITIALSPEED", 50, patternfile);
                    speedValue.targetSpeed4Measure = (short)Util.GetPrivateProfileValueUINT("MOVINGMEASURE", "TARGETSPEED", 100, patternfile);
                    speedValue.accelSpeed4Measure = (short)Util.GetPrivateProfileValueUINT("MOVINGMEASURE", "ACCELERATION", 400, patternfile);
                    speedValue.decelSpeed4Measure = (short)Util.GetPrivateProfileValueUINT("MOVINGMEASURE", "DECELERATION", 400, patternfile);

                    speedValue.initSpeed4Clean = (short)Util.GetPrivateProfileValueUINT("MOVINGCLEAN", "INITIALSPEED", 50, patternfile);
                    speedValue.targetSpeed4Clean = (short)Util.GetPrivateProfileValueUINT("MOVINGCLEAN", "TARGETSPEED", 100, patternfile);
                    speedValue.accelSpeed4Clean = (short)Util.GetPrivateProfileValueUINT("MOVINGCLEAN", "ACCELERATION", 400, patternfile);
                    speedValue.decelSpeed4Clean = (short)Util.GetPrivateProfileValueUINT("MOVINGCLEAN", "DECELERATION", 400, patternfile);
                }



                speedValue.solOnTime = (short)Util.GetPrivateProfileValueUINT("SOLENOID", "SOLONTIME", 10, patternfile);
                speedValue.solOffTime = (short)Util.GetPrivateProfileValueUINT("SOLENOID", "SOLOFFTIME", 10, patternfile);
                speedValue.dwellTime = (short)Util.GetPrivateProfileValueUINT("SOLENOID", "DWELLTIME", 10, patternfile);

                retval.execResult = 0;
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = retval.errorInfo.sErrorFunc + " = " + ex.Message;
                //retval.sErrorMessage = "SPEED DATA EXCEPTION : " + ex.Message;
                return retval;
            }
        }

        public static ITNTResponseArgs GetPatternHeadValue(string name, byte byheadType, ref HeadValue headval)
        {
            //string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetPatternHeadValue";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string value = "";

            try
            {
                retval.errorInfo.sErrorFunc = "GET PATTERN HEAD VALUE";

                FileInfo fi = new FileInfo(Constants.MARKING_INI_FILE);
                if (fi.Exists == false)
                {
                    retval.execResult = -1;
                    retval.errorInfo.sErrorMessage = "PATTERN FILE NOT FOUND - " + name;
                    //retval.sErrorMessage = "NO MARK SETTING FILE : " + Constants.MARKING_INI_FILE;
                    return retval;
                }

                headval.stepLength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                headval.max_X = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_X", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                headval.max_Y = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                headval.max_Z = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Z", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                headval.angleDegree = (double)Util.GetPrivateProfileValueDouble("SENSOR", "ANGLEDEGREE", 0, Constants.MARKING_INI_FILE);// "MarkHeader.ini");
                headval.sensorPosition = (byte)Util.GetPrivateProfileValueUINT("SENSOR", "POSITION", 0, Constants.MARKING_INI_FILE);// "MarkHeader.ini");
                headval.spatterType = (byte)Util.GetPrivateProfileValueUINT("SENSOR", "SPATTER", 0, Constants.MARKING_INI_FILE);// "MarkHeader.ini");

                headval.park3DPos.X = (double)Util.GetPrivateProfileValueDouble("PARKING", "X_POSITION", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                headval.park3DPos.Y = (double)Util.GetPrivateProfileValueDouble("PARKING", "Y_POSITION", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                headval.park3DPos.Z = (double)Util.GetPrivateProfileValueDouble("PARKING", "Z_POSITION", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");

                headval.home3DPos.X = (double)Util.GetPrivateProfileValueDouble("POSITION", "HOME_X", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                headval.home3DPos.Y = (double)Util.GetPrivateProfileValueDouble("POSITION", "HOME_Y", 40, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                headval.home3DPos.Z = (double)Util.GetPrivateProfileValueDouble("POSITION", "HOME_Z", 110, Constants.MARKING_INI_FILE);//"MarkHeader.ini");

                headval.rasterSP = (double)Util.GetPrivateProfileValueDouble("POSITION", "RASTERSP", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                headval.rasterEP = (double)Util.GetPrivateProfileValueDouble("POSITION", "RASTEREP", 0, Constants.MARKING_INI_FILE);// "MarkHeader.ini");

                headval.distance0Position = (double)Util.GetPrivateProfileValueDouble("POSITION", "DISTANCE0POSITION", 47.2, Constants.MARKING_INI_FILE);// "MarkHeader.ini");

                headval.markDelayTime1 = (short)Util.GetPrivateProfileValueUINT("MARKDELAYTIME", "1", 0, Constants.MARKING_INI_FILE);// "MarkHeader.ini");
                headval.markDelayTime2 = (short)Util.GetPrivateProfileValueUINT("MARKDELAYTIME", "2", 0, Constants.MARKING_INI_FILE);// "MarkHeader.ini");

                Util.GetPrivateProfileValue("MARK", "SKIPPLATE", "0", ref value, Constants.MARKING_INI_FILE);// "MarkHeader.ini");
                byte.TryParse(value, out headval.bySkipPlateCheck);

                Util.GetPrivateProfileValue("OPTION", "SLOPE", "1.0", ref value, Constants.MARKING_INI_FILE);// "MarkHeader.ini");
                double.TryParse(value, out headval.slope);

                Util.GetPrivateProfileValue("OPTION", "SLOPE4MANUAL", "4.0", ref value, Constants.MARKING_INI_FILE);// "MarkHeader.ini");
                double.TryParse(value, out headval.slope4Manual);

                retval.execResult = 0;
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = retval.errorInfo.sErrorFunc + " = " + ex.Message;
                //retval.sErrorMessage = "HEAD DATA EXCEPTION : " + ex.Message;
                return retval;
            }
        }

        public static ITNTResponseArgs GetPatternPositionValue(string name, byte byheadType, ref PositionValue positionValue)
        {
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetPatternPositionValue";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);

            try
            {
                //if (name.Length <= 0)
                //    patternfile = Constants.PATTERN_PATH + "Pattern_DEFAULT.ini";
                retval.errorInfo.sErrorFunc = "GET PATTERN POSITION VALUE";

                FileInfo fi = new FileInfo(patternfile);
                if (fi.Exists == false)
                {
                    retval.execResult = -1;
                    retval.errorInfo.sErrorMessage = "PATTERN FILE NOT FOUND - " + name;
                    //retval.sErrorMessage = "NO PATTERN FILE : " + patternfile;
                    return retval;
                }

                FileInfo fi2 = new FileInfo(Constants.MARKING_INI_FILE);
                if (fi2.Exists == false)
                {
                    retval.execResult = -1;
                    retval.errorInfo.sErrorMessage = "PATTERN FILE NOT FOUND - " + Constants.MARKING_INI_FILE;
                    //retval.sErrorMessage = "NO MARK SETTING FILE : " + Constants.MARKING_INI_FILE;
                    return retval;
                }

                //position value
                if (byheadType == 0)
                {
                    positionValue.center3DPos.X = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSX", 20, patternfile);
                    positionValue.center3DPos.Y = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSY", 50, patternfile);
                    positionValue.center3DPos.Z = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSZ", 20, patternfile);
                }
                else
                {
                    positionValue.center3DPos.X = (double)Util.GetPrivateProfileValueDouble("POSITION", "STARTPOSX", 20, patternfile);
                    positionValue.center3DPos.Y = (double)Util.GetPrivateProfileValueDouble("POSITION", "STARTPOSY", 50, patternfile);
                    positionValue.center3DPos.Z = (double)Util.GetPrivateProfileValueDouble("POSITION", "STARTPOSZ", 20, patternfile);
                    positionValue.plateMode = (byte)Util.GetPrivateProfileValueUINT("POSITION", "PLATEMODE", 7, patternfile);
                }

                positionValue.checkDistanceHeight = Util.GetPrivateProfileValueDouble("POSITION", "CHECKDISTANCEHEIGHT", 40.0, patternfile);

                positionValue.teachingZHeight = Util.GetPrivateProfileValueDouble("POSITION", "TEACHING_Z_AXIS", 40.0, patternfile);

                //positionValue.cleaningHeight = Util.GetPrivateProfileValueDouble("LASERSOURCE", "CLEANPOSITION", 40.0, patternfile);

                //positionValue.park3DPos.X = (double)Util.GetPrivateProfileValueDouble("PARKING", "X_POSITION", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                //positionValue.park3DPos.Y = (double)Util.GetPrivateProfileValueDouble("PARKING", "Y_POSITION", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                //positionValue.park3DPos.Z = (double)Util.GetPrivateProfileValueDouble("PARKING", "Z_POSITION", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");

                //positionValue.home3DPos.X = (double)Util.GetPrivateProfileValueDouble("POSITION", "HOME_X", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                //positionValue.home3DPos.Y = (double)Util.GetPrivateProfileValueDouble("POSITION", "HOME_Y", 40, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                //positionValue.home3DPos.Z = (double)Util.GetPrivateProfileValueDouble("POSITION", "HOME_Z", 110, Constants.MARKING_INI_FILE);//"MarkHeader.ini");

                //positionValue.rasterSP = (double)Util.GetPrivateProfileValueDouble("POSITION", "RASTERSP", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
                //positionValue.rasterEP = (double)Util.GetPrivateProfileValueDouble("POSITION", "RASTEREP", 0, Constants.MARKING_INI_FILE);// "MarkHeader.ini");

                retval.execResult = 0;
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = retval.errorInfo.sErrorFunc + " = " + ex.Message;
                //retval.sErrorMessage = "POSITION DATA EXCEPTION : " + ex.Message;
                return retval;
            }
        }

        public static ITNTResponseArgs GetPatternLaserValue(string name, byte byheadType, ref LaserValue laserValue)
        {
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetPatternLaserValue";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            string value = "";

            try
            {
                retval.errorInfo.sErrorFunc = "GET PATTERN LASER VALUE";

                FileInfo fi = new FileInfo(patternfile);
                if (fi.Exists == false)
                {
                    retval.execResult = -1;
                    retval.errorInfo.sErrorMessage = "PATTERN FILE NOT FOUND - " + name;
                    //retval.sErrorMessage = "NO PATTERN FILE : " + patternfile;
                    return retval;
                }

                // laser value
                laserValue.waveformNum = (short)Util.GetPrivateProfileValueUINT("LASERSOURCE", "WAVEFORMPROFILE", 0, patternfile);
                laserValue.waveformClean = (short)Util.GetPrivateProfileValueUINT("LASERSOURCE", "WAVEFORMCLEAN", 0, patternfile);
                //laserValue.phaseComp = (short)Util.GetPrivateProfileValueUINT("LASERSOURCE", "PHASECOMP", 0, patternfile);
                Util.GetPrivateProfileValue("LASERSOURCE", "PHASECOMP", "55.0", ref laserValue.sPhaseComp, patternfile);
                //Single.TryParse(value, out laserValue.phaseComp);

                laserValue.density = (short)Util.GetPrivateProfileValueUINT("LASERSOURCE", "DENSITY", 0, patternfile);
                laserValue.cleanPosition = (double)Util.GetPrivateProfileValueDouble("LASERSOURCE", "CLEANPOSITION", 0, patternfile);
                laserValue.cleanDelta = (double)Util.GetPrivateProfileValueDouble("LASERSOURCE", "CLEANDELTA", 0, patternfile);

                laserValue.charClean = (int)Util.GetPrivateProfileValueUINT("LASERSOURCE", "CHARCLEAN", 0, patternfile);
                laserValue.combineFireClean = (int)Util.GetPrivateProfileValueUINT("LASERSOURCE", "COMBINEFIRECLEAN", 0, patternfile);
                laserValue.charFull = (char)Util.GetPrivateProfileValueByte("LASERSOURCE", "CHARFULL", (byte)':', patternfile);

                laserValue.useCleaning = (short)Util.GetPrivateProfileValueUINT("LASERSOURCE", "USECLEANING", 0, patternfile);

                Util.GetPrivateProfileValue("LASERSOURCE", "MARKPOWER", "10", ref laserValue.markPower, patternfile);
                Util.GetPrivateProfileValue("LASERSOURCE", "MARKWIDTH", "0.5", ref laserValue.markWidth, patternfile);
                Util.GetPrivateProfileValue("LASERSOURCE", "CLEANPOWER", "10", ref laserValue.cleanPower, patternfile);
                Util.GetPrivateProfileValue("LASERSOURCE", "CLEANWIDTH", "0.5", ref laserValue.cleanWidth, patternfile);
                Util.GetPrivateProfileValue("LASERSOURCE", "PLATEPOWER", "10", ref laserValue.platePower, patternfile);
                Util.GetPrivateProfileValue("LASERSOURCE", "PLATEWIDTH", "0.5", ref laserValue.plateWidth, patternfile);
                Util.GetPrivateProfileValue("LASERSOURCE", "SPOTPOWER", "10", ref laserValue.spotPower, patternfile);
                Util.GetPrivateProfileValue("LASERSOURCE", "SPOTWIDTH", "0.5", ref laserValue.spotWidth, patternfile);

                retval.execResult = 0;
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = retval.errorInfo.sErrorFunc + " = " + ex.Message;
                //retval.sErrorMessage = "LASER DATA EXCEPTION : " + ex.Message;
                return retval;
            }
        }

        public static ITNTResponseArgs GetPatternScanValue(string name, byte byheadType, ref ScanValue scanValue)
        {
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetPatternScanValue";
            ITNTResponseArgs retval = new ITNTResponseArgs(128);

            try
            {
                retval.errorInfo.sErrorFunc = "GET PATTERN SCAN VALUE";

                FileInfo fi = new FileInfo(patternfile);
                if (fi.Exists == false)
                {
                    retval.execResult = -1;
                    retval.errorInfo.sErrorMessage = "PATTERN FILE NOT FOUND - " + name;
                    //retval.sErrorMessage = "NO PATTERN FILE : " + patternfile;
                    return retval;
                }

                FileInfo fi2 = new FileInfo(Constants.SCANNER_INI_FILE);
                if (fi2.Exists == false)
                {
                    retval.execResult = -1;
                    retval.errorInfo.sErrorMessage = "PATTERN FILE NOT FOUND - " + Constants.SCANNER_INI_FILE;
                    //retval.sErrorMessage = "NO SCANNER SETTING FILE : " + Constants.SCANNER_INI_FILE;
                    return retval;
                }


                //scan value
                scanValue.stepLength_U = (short)Util.GetPrivateProfileValueUINT("CONFIG", "STEP_LENGTH", 100, Constants.SCANNER_INI_FILE);
                scanValue.max_U = (short)Util.GetPrivateProfileValueUINT("CONFIG", "MAX_U", 190, Constants.SCANNER_INI_FILE);
                scanValue.parkingU = (double)Util.GetPrivateProfileValueDouble("CONFIG", "PARKING", 90, Constants.SCANNER_INI_FILE);
                scanValue.home_U = (double)Util.GetPrivateProfileValueDouble("CONFIG", "HOME_U", 90, Constants.SCANNER_INI_FILE);
                scanValue.linkPos = (double)Util.GetPrivateProfileValueDouble("CONFIG", "LINKPOS", 140.5, Constants.SCANNER_INI_FILE);

                scanValue.initSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "INITIALSPEED", 10, patternfile);
                scanValue.targetSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "TARGETSPEED", 10, patternfile);
                scanValue.accelSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "ACCELERATION", 10, patternfile);
                scanValue.decelSpeed4Scan = (short)Util.GetPrivateProfileValueUINT("SCAN", "DECELERATION", 10, patternfile);

                scanValue.initSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "INITIALSPEED", 10, patternfile);
                scanValue.targetSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "TARGETSPEED", 10, patternfile);
                scanValue.accelSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "ACCELERATION", 10, patternfile);
                scanValue.decelSpeed4ScanFree = (short)Util.GetPrivateProfileValueUINT("SCANFREE", "DECELERATION", 10, patternfile);

                scanValue.reverseScan = (byte)Util.GetPrivateProfileValueByte("PROFILER", "REVERSESCAN", 0, patternfile);

                scanValue.startU = (double)Util.GetPrivateProfileValueDouble("PROFILER", "STARTPOS", 20, patternfile); // load Max U_Scan
                scanValue.scanLen = (double)Util.GetPrivateProfileValueDouble("PROFILER", "SCANLEN", 130, patternfile);

                retval.execResult = 0;
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorMessage = retval.errorInfo.sErrorFunc + " = " + ex.Message;
                //retval.sErrorMessage = "LASER DATA EXCEPTION : " + ex.Message;
                return retval;
            }
        }

        public static int SetPatternValue(string name, byte byheadType, PatternValueEx pattern, byte saveflag)
        {
            string className = "ImageProcessManager";
            string funcName = "SetPatternValue";
            int retval = 0;
            try
            {
                retval = SetPatternFontValue(name, byheadType, pattern.fontValue);
                if(retval != 0)
                    return retval;
                retval = SetPatternSpeedValue(name, byheadType, pattern.speedValue);
                if (retval != 0)
                    return retval;
                retval = SetPatternHeadValue(byheadType, pattern.headValue, saveflag);
                if (retval != 0)
                    return retval;
                retval = SetPatternPositionValue(name, byheadType, pattern.positionValue, saveflag);
                if (retval != 0)
                    return retval;
                retval = SetPatternLaserValue(name, byheadType, pattern.laserValue, saveflag);
                if (retval != 0)
                    return retval;
                retval = SetPatternScanValue(name, byheadType, pattern.scanValue, saveflag);
                if (retval != 0)
                    return retval;

                return 0;
            }
            catch (Exception ex)
            {
                //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "", "", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int GetOneCharacterFontData(char vinChar, string fontName, byte fontdirection, ref List<FontDataClass> fontData, out double fontsizeX, out double fontsizeY, out double shiftVal, out string ErrorCode)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = "";
            string lineFontValue = "";
            FileInfo fi;
            //string value = "";

            try
            {
                //Util.GetPrivateProfileValue("USEFONT", "TYPE", "0", ref value, "FONT.ini");
                //if (value != "0")
                //    filename = curDir + "FONTL\\S_" + fontName + ".FON";
                //else
                filename = curDir + "FONT\\S_" + fontName + ".FON";
                fi = new FileInfo(filename);

                if (fi.Exists == false)
                {
                    fontsizeX = 0.0d;
                    fontsizeY = 0.0d;
                    shiftVal = 0;
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);
                    return -3;
                }

                using (StreamReader sr = new StreamReader(filename))
                {
                    string[] pointList;
                    string[] point;

                    string[] fontsize;
                    shiftVal = 0;

                    lineFontValue = sr.ReadLine();
                    fontsize = lineFontValue.Split(',');
                    if (fontsize.Length < 2)
                    {
                        fontsizeX = 0.0d;
                        fontsizeY = 0.0d;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                        ErrorCode = "00HMF0000002";
                        return -4;
                    }

                    //fontsizeX = Math.Max((Convert.ToDouble(fontsize[0]) - 1.0d), 0.01d);
                    //fontsizeY = Math.Max((Convert.ToDouble(fontsize[1]) - 1.0d), 0.01d);

                    double.TryParse(fontsize[0], out fontsizeX);
                    double.TryParse(fontsize[1], out fontsizeY);

                    if ((fontsize.Length >= 3) && (fontsize[2].Length > 0))
                        double.TryParse(fontsize[2], out shiftVal);

                    if(shiftVal == 0)
                    {
                        if (fontName == "5X7")
                            shiftVal = 3.0d;
                        else if (fontName == "11X16" || fontName == "OCR")
                            shiftVal = 5.0d;
                    }

                    int charNum = (int)vinChar - 1;
                    lineFontValue = File.ReadLines(filename).Skip(charNum).Take(1).First();
                    FontDataClass fd = new FontDataClass();
                    if (lineFontValue.Length > 0)
                    {
                        pointList = lineFontValue.Split(';');
                        if (pointList.Count() > 0)
                        {
                            //bool find = false;
                            for (int i = 0; i < pointList.Count(); i++)
                            {
                                point = pointList[i].Split(',');
                                if (point.Count() >= 3)
                                {
                                    fd.vector3d.X = Convert.ToDouble(point[0]);
                                    if(fontdirection == 0)
                                    {

                                    }
                                    else
                                    {
                                        fd.vector3d.Y = (fontsizeY- 1) - Convert.ToDouble(point[1]);
                                    }
                                    if (fontName == "5X7")
                                        fd.vector3d.Y = (fontsizeY + 3.0d - Convert.ToDouble(point[1]));
                                    else if (fontName == "11X16" || fontName == "OCR")
                                        fd.vector3d.Y = (fontsizeY + 5.0d - Convert.ToDouble(point[1]));
                                    else
                                        fd.vector3d.Y = (fontsizeY - Convert.ToDouble(point[1]));
                                    fd.vector3d.Z = 0;
                                    fd.Flag = Convert.ToInt32(point[2]);
                                    fontData.Add(fd);
                                }
                            }
                        }
                    }
                }

                ErrorCode = "";
                ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return 0;
            }
            catch (Exception ex)
            {
                ErrorCode = string.Format("00HE{0:X8}", Math.Abs(ex.HResult));
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;
                shiftVal = 0.0d;
                //ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}", ErrorCode));
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int SetPatternFontValue(string name, byte byheadType, FontValue font)
        {
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetPatternFontValue";

            try
            {
                if (name.Length <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR NAME IS BLANK", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }

                Util.WritePrivateProfileValue("FONT", "NAME", font.fontName, patternfile); // load FONT
                Util.WritePrivateProfileValue("FONT", "WIDTH", font.width.ToString("F2"), patternfile);
                Util.WritePrivateProfileValue("FONT", "HEIGHT", font.height.ToString("F2"), patternfile);
                Util.WritePrivateProfileValue("FONT", "PITCH", font.pitch.ToString("F2"), patternfile);
                Util.WritePrivateProfileValue("FONT", "ROTATEANGLE", font.rotateAngle.ToString("F2"), patternfile);
                Util.WritePrivateProfileValue("FONT", "STRIKECOUNT", font.strikeCount.ToString(), patternfile);
                Util.WritePrivateProfileValue("FONT", "THICKNESS", font.thickness.ToString("F2"), patternfile);

                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int SetPatternSpeedValue(string name, byte byheadType, SpeedValue speedValue)
        {
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "SetPatternSpeedValue";

            try
            {
                if (name.Length <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR NAME IS BLANK", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }


                if (byheadType == 0)
                {
                    Util.WritePrivateProfileValue("LOAD", "INITIALSPEED", speedValue.initSpeed4MarkV.ToString(), patternfile);
                    Util.WritePrivateProfileValue("LOAD", "TARGETSPEED", speedValue.targetSpeed4MarkV.ToString(), patternfile);
                    Util.WritePrivateProfileValue("LOAD", "ACCELERATION", speedValue.accelSpeed4MarkV.ToString(), patternfile);
                    Util.WritePrivateProfileValue("LOAD", "DECELERATION", speedValue.decelSpeed4MarkV.ToString(), patternfile);

                    Util.WritePrivateProfileValue("NOLOAD", "INITIALSPEED", speedValue.initSpeed4Fast.ToString(), patternfile);
                    Util.WritePrivateProfileValue("NOLOAD", "TARGETSPEED", speedValue.targetSpeed4Fast.ToString(), patternfile);
                    Util.WritePrivateProfileValue("NOLOAD", "ACCELERATION", speedValue.accelSpeed4Fast.ToString(), patternfile);
                    Util.WritePrivateProfileValue("NOLOAD", "DECELERATION", speedValue.decelSpeed4Fast.ToString(), patternfile);
                }
                else
                {
                    Util.WritePrivateProfileValue("MARKVECTOR", "INITIALSPEED", speedValue.initSpeed4MarkV.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MARKVECTOR", "TARGETSPEED", speedValue.targetSpeed4MarkV.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MARKVECTOR", "ACCELERATION", speedValue.accelSpeed4MarkV.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MARKVECTOR", "DECELERATION", speedValue.decelSpeed4MarkV.ToString(), patternfile);

                    Util.WritePrivateProfileValue("MOVINGFAST", "INITIALSPEED", speedValue.initSpeed4Fast.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGFAST", "TARGETSPEED", speedValue.targetSpeed4Fast.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGFAST", "ACCELERATION", speedValue.accelSpeed4Fast.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGFAST", "DECELERATION", speedValue.decelSpeed4Fast.ToString(), patternfile);

                    Util.WritePrivateProfileValue("MOVINGHOME", "INITIALSPEED", speedValue.initSpeed4Home.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGHOME", "TARGETSPEED", speedValue.targetSpeed4Home.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGHOME", "ACCELERATION", speedValue.accelSpeed4Home.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGHOME", "DECELERATION", speedValue.decelSpeed4Home.ToString(), patternfile);

                    Util.WritePrivateProfileValue("MARKRASTER", "INITIALSPEED", speedValue.initSpeed4MarkR.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MARKRASTER", "TARGETSPEED", speedValue.targetSpeed4MarkR.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MARKRASTER", "ACCELERATION", speedValue.accelSpeed4MarkR.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MARKRASTER", "DECELERATION", speedValue.decelSpeed4MarkR.ToString(), patternfile);

                    Util.WritePrivateProfileValue("MOVINGMEASURE", "INITIALSPEED", speedValue.initSpeed4Measure.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGMEASURE", "TARGETSPEED", speedValue.targetSpeed4Measure.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGMEASURE", "ACCELERATION", speedValue.accelSpeed4Measure.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGMEASURE", "DECELERATION", speedValue.decelSpeed4Measure.ToString(), patternfile);

                    Util.WritePrivateProfileValue("MOVINGCLEAN", "INITIALSPEED", speedValue.initSpeed4Clean.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGCLEAN", "TARGETSPEED", speedValue.targetSpeed4Clean.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGCLEAN", "ACCELERATION", speedValue.accelSpeed4Clean.ToString(), patternfile);
                    Util.WritePrivateProfileValue("MOVINGCLEAN", "DECELERATION", speedValue.decelSpeed4Clean.ToString(), patternfile);
                }

                Util.WritePrivateProfileValue("SOLENOID", "SOLONTIME", speedValue.solOnTime.ToString(), patternfile);
                Util.WritePrivateProfileValue("SOLENOID", "SOLOFFTIME", speedValue.solOffTime.ToString(), patternfile);
                Util.WritePrivateProfileValue("SOLENOID", "DWELLTIME", speedValue.dwellTime.ToString(), patternfile);

                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int SetPatternHeadValue(/*string name, */byte byheadType, HeadValue headval, byte saveflag)
        {
            //string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "SetPatternHeadValue";

            try
            {
                //if (name.Length <= 0)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR NAME IS BLANK", Thread.CurrentThread.ManagedThreadId);
                //    return -1;
                //}

                if(saveflag != 0)
                {
                    Util.WritePrivateProfileValue("MARK", "STEP_LENGTH", headval.stepLength.ToString(), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("MARK", "MAX_X", headval.max_X.ToString(), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("MARK", "MAX_Y", headval.max_Y.ToString(), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("MARK", "MAX_Z", headval.max_Z.ToString(), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("SENSOR", "ANGLEDEGREE", headval.angleDegree.ToString("F2"), Constants.MARKING_INI_FILE);

                    Util.WritePrivateProfileValue("SENSOR", "POSITION", headval.sensorPosition.ToString(), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("SENSOR", "SPATTER", headval.spatterType.ToString(), Constants.MARKING_INI_FILE);

                    Util.WritePrivateProfileValue("PARKING", "X_POSITION", headval.park3DPos.X.ToString("F2"), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("PARKING", "Y_POSITION", headval.park3DPos.Y.ToString("F2"), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("PARKING", "Z_POSITION", headval.park3DPos.Z.ToString("F2"), Constants.MARKING_INI_FILE);

                    Util.WritePrivateProfileValue("POSITION", "HOME_X", headval.home3DPos.X.ToString("F2"), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("POSITION", "HOME_Y", headval.home3DPos.Y.ToString("F2"), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("POSITION", "HOME_Z", headval.home3DPos.Z.ToString("F2"), Constants.MARKING_INI_FILE);

                    Util.WritePrivateProfileValue("POSITION", "RASTERSP", headval.rasterSP.ToString("F2"), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("POSITION", "RASTEREP", headval.rasterEP.ToString("F2"), Constants.MARKING_INI_FILE);

                    Util.WritePrivateProfileValue("POSITION", "DISTANCE0POSITION", headval.distance0Position.ToString("F2"), Constants.MARKING_INI_FILE);

                    Util.WritePrivateProfileValue("MARKDELAYTIME", "1", headval.markDelayTime1.ToString(), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("MARKDELAYTIME", "2", headval.markDelayTime2.ToString(), Constants.MARKING_INI_FILE);

                    Util.WritePrivateProfileValue("MARK", "SKIPPLATE", headval.bySkipPlateCheck.ToString(), Constants.MARKING_INI_FILE);

                    Util.WritePrivateProfileValue("OPTION", "SLOPE", headval.slope.ToString("F2"), Constants.MARKING_INI_FILE);
                    Util.WritePrivateProfileValue("OPTION", "SLOPE4MANUAL", headval.slope4Manual.ToString("F2"), Constants.MARKING_INI_FILE);
                }

                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int SetPatternPositionValue(string name, byte byheadType, PositionValue positionValue, byte saveflag)
        {
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "SetPatternPositionValue";

            try
            {
                if (name.Length <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR NAME IS BLANK", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }

                //position value
                if (byheadType == 0)
                {
                    Util.WritePrivateProfileValue("FONT", "STARTPOSX", positionValue.center3DPos.X.ToString("F2"), patternfile);
                    Util.WritePrivateProfileValue("FONT", "STARTPOSY", positionValue.center3DPos.Y.ToString("F2"), patternfile);
                    Util.WritePrivateProfileValue("FONT", "STARTPOSZ", positionValue.center3DPos.Z.ToString("F2"), patternfile);
                }
                else
                {
                    Util.WritePrivateProfileValue("POSITION", "STARTPOSX", positionValue.center3DPos.X.ToString("F2"), patternfile);
                    Util.WritePrivateProfileValue("POSITION", "STARTPOSY", positionValue.center3DPos.Y.ToString("F2"), patternfile);
                    Util.WritePrivateProfileValue("POSITION", "STARTPOSZ", positionValue.center3DPos.Z.ToString("F2"), patternfile);
                }

                Util.WritePrivateProfileValue("POSITION", "CHECKDISTANCEHEIGHT", positionValue.checkDistanceHeight.ToString("F2"), patternfile);
                Util.WritePrivateProfileValue("POSITION", "TEACHING_Z_AXIS", positionValue.teachingZHeight.ToString("F2"), patternfile);
                //Util.WritePrivateProfileValue("LASERSOURCE", "CLEANPOSITION", positionValue.cleaningHeight.ToString("F2"), patternfile);

                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int SetPatternLaserValue(string name, byte byheadType, LaserValue laserValue, byte saveflag)
        {
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "SetPatternLaserValue";

            try
            {
                if (name.Length <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR NAME IS BLANK", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }

                // laser value
                Util.WritePrivateProfileValue("LASERSOURCE", "WAVEFORMPROFILE", laserValue.waveformNum.ToString(), patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "WAVEFORMCLEAN", laserValue.waveformClean.ToString(), patternfile);
                //Util.WritePrivateProfileValue("LASERSOURCE", "PHASECOMP", laserValue.phaseComp.ToString(), patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "PHASECOMP", laserValue.sPhaseComp, patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "DENSITY", laserValue.density.ToString(), patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "CLEANPOSITION", laserValue.cleanPosition.ToString("F2"), patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "CLEANDELTA", laserValue.cleanDelta.ToString("F2"), patternfile);

                Util.WritePrivateProfileValue("LASERSOURCE", "CHARCLEAN", laserValue.charClean.ToString(), patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "COMBINEFIRECLEAN", laserValue.combineFireClean.ToString(), patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "CHARFULL", ((int)laserValue.charFull).ToString(), patternfile);

                Util.WritePrivateProfileValue("LASERSOURCE", "USECLEANING", laserValue.useCleaning.ToString(), patternfile);

                Util.WritePrivateProfileValue("LASERSOURCE", "MARKPOWER", laserValue.markPower, patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "MARKWIDTH", laserValue.markWidth, patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "CLEANPOWER", laserValue.cleanPower, patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "CLEANWIDTH", laserValue.cleanWidth, patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "PLATEPOWER", laserValue.platePower, patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "PLATEWIDTH", laserValue.plateWidth, patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "SPOTPOWER", laserValue.spotPower, patternfile);
                Util.WritePrivateProfileValue("LASERSOURCE", "SPOTWIDTH", laserValue.spotWidth, patternfile);

                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int SetPatternScanValue(string name, byte byheadType, ScanValue scanValue, byte saveflag)
        {
            string patternfile = Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "SetPatternScanValue";

            try
            {
                if (name.Length <= 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR NAME IS BLANK", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }

                //scan value
                if (saveflag != 0)
                {
                    Util.WritePrivateProfileValue("CONFIG", "STEP_LENGTH", scanValue.stepLength_U.ToString(), Constants.SCANNER_INI_FILE);
                    Util.WritePrivateProfileValue("CONFIG", "MAX_U", scanValue.max_U.ToString(), Constants.SCANNER_INI_FILE);
                    Util.WritePrivateProfileValue("CONFIG", "PARKING", scanValue.parkingU.ToString("F2"), Constants.SCANNER_INI_FILE);
                    Util.WritePrivateProfileValue("CONFIG", "HOME_U", scanValue.home_U.ToString("F2"), Constants.SCANNER_INI_FILE);
                    Util.WritePrivateProfileValue("CONFIG", "LINKPOS", scanValue.linkPos.ToString("F2"), Constants.SCANNER_INI_FILE);
                }

                Util.WritePrivateProfileValue("SCAN", "INITIALSPEED", scanValue.initSpeed4Scan.ToString(), patternfile);
                Util.WritePrivateProfileValue("SCAN", "TARGETSPEED", scanValue.targetSpeed4Scan.ToString(), patternfile);
                Util.WritePrivateProfileValue("SCAN", "ACCELERATION", scanValue.accelSpeed4Scan.ToString(), patternfile);
                Util.WritePrivateProfileValue("SCAN", "DECELERATION", scanValue.decelSpeed4Scan.ToString(), patternfile);

                Util.WritePrivateProfileValue("SCANFREE", "INITIALSPEED", scanValue.initSpeed4ScanFree.ToString(), patternfile);
                Util.WritePrivateProfileValue("SCANFREE", "TARGETSPEED", scanValue.targetSpeed4ScanFree.ToString(), patternfile);
                Util.WritePrivateProfileValue("SCANFREE", "ACCELERATION", scanValue.accelSpeed4ScanFree.ToString(), patternfile);
                Util.WritePrivateProfileValue("SCANFREE", "DECELERATION", scanValue.decelSpeed4ScanFree.ToString(), patternfile);

                Util.WritePrivateProfileValue("PROFILER", "REVERSESCAN", scanValue.reverseScan.ToString(), patternfile);

                Util.WritePrivateProfileValue("PROFILER", "STARTPOS", scanValue.startU.ToString("F2"), patternfile); 
                Util.WritePrivateProfileValue("PROFILER", "SCANLEN", scanValue.scanLen.ToString("F2"), patternfile);
                return 0;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        //public static ITNTResponseArgs GetHeadHomeValue(ref Vector3D home3DPos)
        //{
        //    //string patternfile = Constants.PATTERN_PATH + name + ".ini";
        //    string className = "ImageProcessManager";
        //    string funcName = "GetHeadHomeValue";
        //    ITNTResponseArgs retval = new ITNTResponseArgs(128);

        //    try
        //    {
        //        FileInfo fi = new FileInfo(Constants.MARKING_INI_FILE);
        //        if (fi.Exists == false)
        //        {
        //            retval.execResult = -1;
        //            retval.sErrorMessage = "NO MARK SETTING FILE : " + Constants.MARKING_INI_FILE;
        //            return retval;
        //        }

        //        home3DPos.X = (double)Util.GetPrivateProfileValueDouble("POSITION", "HOME_X", 0, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
        //        home3DPos.Y = (double)Util.GetPrivateProfileValueDouble("POSITION", "HOME_Y", 40, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
        //        home3DPos.Z = (double)Util.GetPrivateProfileValueDouble("POSITION", "HOME_Z", 110, Constants.MARKING_INI_FILE);//"MarkHeader.ini");
        //        retval.execResult = 0;
        //        return retval;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval.execResult = ex.HResult;
        //        retval.sErrorMessage = "HEAD DATA EXCEPTION : " + ex.Message;
        //        return retval;
        //    }
        //}

    }
}
