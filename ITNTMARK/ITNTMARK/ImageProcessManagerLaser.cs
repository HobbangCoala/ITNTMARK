using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Media = System.Windows.Media;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Diagnostics;
using ITNTUTIL;
using ITNTCOMMON;
using System.Threading;
using System.Windows.Media.Media3D;

namespace ITNTMARK
{
    public class ImageProcessManagerLaser
    {
        private Canvas canvas;
        public ImageProcessManagerLaser()
        {
            canvas = new Canvas();
        }


        public static int GetFontSize(string fontName, out double fontsizeX, out double fontsizeY, out double fontsizeZ, out string ErrorCode)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = "";
            FileInfo fi;
            //string value = "";

            try
            {
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;
                fontsizeZ = 0.0d;

                filename = curDir + "FONTL\\S_" + fontName + ".FON";

                fi = new FileInfo(filename);
                if (fi.Exists == false)
                {
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);
                    return -3;
                }
                string fileline;
                List<string> FontDataLaser = new List<string>();
                using (StreamReader sr = new StreamReader(filename))
                {
                    string[] fontsize;
                    fileline = sr.ReadLine();
                    fontsize = fileline.Split(',');
                    if (fontsize.Length < 2)
                    {
                        //System.Windows.MessageBox.Show("Font File is not valid");
                        //imgsource = null;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                        ErrorCode = "00HMF0000002";
                        return -4;
                    }
                    double.TryParse(fontsize[0], out fontsizeX);
                    double.TryParse(fontsize[1], out fontsizeY);
                    if (fontsize.Length >= 3)
                        double.TryParse(fontsize[2], out fontsizeZ);
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
                fontsizeZ = 0.0d;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int GetFontData(VinNoInfo vin, ref Dictionary<int, List<FontDataClass>> MyData, out double fontsizeX, out double fontsizeY, out double shiftVal, out string ErrorCode)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = "";
            FileInfo fi;
            //string value = "";

            try
            {
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;
                shiftVal = 0.0d;

                filename = curDir + "FONTL\\S_" + vin.fontName + ".FON";

                fi = new FileInfo(filename);
                if (fi.Exists == false)
                {
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);
                    return -3;
                }
                string fileline;
                List<string> FontDataLaser = new List<string>();
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
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                        ErrorCode = "00HMF0000002";
                        return -4;
                    }
                    fontsizeX = Math.Max(Convert.ToDouble(fontsize[0]), 0.01d);
                    fontsizeY = Math.Max(Convert.ToDouble(fontsize[1]), 0.01d);
                    if (fontsize.Length >= 3)
                    {
                        shiftVal = Math.Max(Convert.ToDouble(fontsize[2]), 0.01d);
                    }
                    FontDataClass tmpfd = new FontDataClass();
                    List<FontDataClass> tmplist = new List<FontDataClass>();
                    tmplist.Add(tmpfd);
                    MyData.Add(0, tmplist);

                    int idx = 1;

                    while ((fileline = sr.ReadLine()) != null)
                    {
                        List<FontDataClass> FontList = new List<FontDataClass>();
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
                                        //if ((vin.fontName == "HMC5") || (vin.fontName == "OCR"))
                                        //    fd.Y = (fontsizeY - Convert.ToDouble(point[1]));
                                        //else if (vin.fontName == "5X7")
                                        //    fd.Y = (fontsizeY + 3.0d - Convert.ToDouble(point[1]));
                                        //else
                                        //    fd.Y = (fontsizeY + 5.0d - Convert.ToDouble(point[1]));

                                        //Read Font List Fix 210801 James Cho
                                        //if (vin.fontName == "5X7")
                                        //    fd.Y = (fontsizeY + 3.0d - Convert.ToDouble(point[1]));
                                        //else if (vin.fontName == "11X16" || vin.fontName == "OCR")
                                        //    fd.Y = (fontsizeY + 5.0d - Convert.ToDouble(point[1]));
                                        //else
                                        //    fd.Y = (fontsizeY - Convert.ToDouble(point[1]));

                                        fd.vector3d.Y = (fontsizeY + shiftVal - Convert.ToDouble(point[1]));

                                        fd.vector3d.Z = 0.0d;
                                        fd.Flag = Convert.ToInt32(point[2]);
                                        FontList.Add(fd);
                                        find = true;
                                    }
                                    find = true;
                                }
                                if (find == false)
                                {
                                    fd.vector3d.X = -1.0d;
                                    fd.vector3d.Y = -1.0d;
                                    fd.vector3d.Z = -1.0d;
                                    fd.Flag = -1;
                                    FontList.Add(fd);
                                }
                            }
                            else
                            {
                                fd.vector3d.X = -1.0d;
                                fd.vector3d.Y = -1.0d;
                                fd.vector3d.Z = -1.0d;
                                fd.Flag = -1;
                                FontList.Add(fd);
                            }
                        }
                        else
                        {
                            fd.vector3d.X = -1.0d;
                            fd.vector3d.Y = -1.0d;
                            fd.vector3d.Z = -1.0d;
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
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font file data  is invalid (count = {0}", MyData.Count), Thread.CurrentThread.ManagedThreadId);
                    //                ErrorCode = "VH";
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
                shiftVal = 0.0d;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int GetFontData(string vin, string filename, ref List<List<FontDataClass>> MyData, out double fontsizeX, out double fontsizeY, out double shiftVal, out string ErrorCode)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            FileInfo fi;
            string lineFontValue = "";
            //string fileline;
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;
                shiftVal = 0;

                fi = new FileInfo(filename);
                if (fi.Exists == false)
                {
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);
                    return -3;
                }

                //List<string> FontDataClass = new List<string>();
                using (StreamReader sr = new StreamReader(filename))
                {
                    string[] fontsize;
                    lineFontValue = sr.ReadLine();
                    fontsize = lineFontValue.Split(',');
                    if (fontsize.Length < 2)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                        ErrorCode = "00HMF0000002";
                        return -4;
                    }
                    fontsizeX = Math.Max(Convert.ToDouble(fontsize[0]), 0.01d);
                    fontsizeY = Math.Max(Convert.ToDouble(fontsize[1]), 0.01d);
                    if (fontsize.Length >= 3)
                        shiftVal = Math.Max(Convert.ToDouble(fontsize[2]), 0.01d);

                    for (int i = 0; i < vin.Length; i++)
                    {
                        List<FontDataClass> fontlist = new List<FontDataClass>();
                        string[] pointList;
                        string[] point;

                        int charNum = (int)vin[i] - 1;
                        lineFontValue = File.ReadLines(filename).Skip(charNum).Take(1).First();
                        //FontDataClass fd;
                        FontDataClass fd = new FontDataClass();
                        if (lineFontValue.Length > 0)
                        {
                            pointList = lineFontValue.Split(';');
                            if (pointList.Count() > 2)
                            {
                                //bool find = false;
                                for (int j = 0; j < pointList.Count(); j++)
                                {
                                    point = pointList[j].Split(',');
                                    if (point.Count() >= 3)
                                    {
                                        fd.vector3d.X = Convert.ToDouble(point[0]);
                                        fd.vector3d.Y = Convert.ToDouble(point[1]);
                                        //fd.Y = (fontsizeY + (double)shiftVal - Convert.ToDouble(point[1]));
                                        fd.Flag = Convert.ToInt32(point[2]);
                                        fontlist.Add(fd);
                                    }
                                }
                            }
                            else
                            {
                                fd.vector3d.X = 0;
                                fd.vector3d.Y = 0;
                                fd.Flag = -1;
                                fontlist.Add(fd);
                            }
                        }
                        MyData.Add(fontlist);
                    }
                }

                //using (StreamReader sr = new StreamReader(filename))
                //{
                //    string[] pointList;
                //    string[] point;

                //    string[] fontsize;
                //    fileline = sr.ReadLine();
                //    fontsize = fileline.Split(',');
                //    if (fontsize.Length < 1)
                //    {
                //        //System.Windows.MessageBox.Show("Font File is not valid");
                //        //imgsource = null;
                //        fontsizeX = 0.0d;
                //        fontsizeY = 0.0d;
                //        shiftVal = 0;
                //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                //        ErrorCode = "00HMF0000002";
                //        return -4;
                //    }
                //    fontsizeX = Math.Max((Convert.ToDouble(fontsize[0]) - 1.0d), 0.01d);
                //    fontsizeY = Math.Max((Convert.ToDouble(fontsize[1]) - 1.0d), 0.01d);
                //    if (fontsize.Length < 3)
                //        shiftVal = 0;
                //    else
                //        double.TryParse(fontsize[2], out shiftVal);

                //    int fontsizeYoffset = (fontsize.Length > 2) ? int.Parse(fontsize[2]) : 0;
                //    FontDataClass tmpfd = new FontDataClass() { X = -1.0d, Y = -1.0d, Flag = -1 };
                //    List<FontDataClass> tmplist = new List<FontDataClass>();
                //    tmplist.Add(tmpfd);
                //    MyData.Add(0, tmplist);

                //    int idx = 1;

                //    while ((fileline = sr.ReadLine()) != null)
                //    {
                //        List<FontDataClass> FontList = new List<FontDataClass>();
                //        FontDataClass fd;
                //        if (fileline.Length > 0)
                //        {
                //            pointList = fileline.Split(';');
                //            if (pointList.Count() > 0)
                //            {
                //                bool find = false;
                //                for (int i = 0; i < pointList.Count(); i++)
                //                {
                //                    point = pointList[i].Split(',');

                //                    if (point.Count() >= 3)
                //                    {
                //                        fd.X = Convert.ToDouble(point[0]);
                //                        fd.Y = (fontsizeY + (double)fontsizeYoffset - Convert.ToDouble(point[1]));
                //                        fd.Flag = Convert.ToInt32(point[2]);
                //                        FontList.Add(fd);
                //                        find = true;
                //                    }
                //                    find = true;
                //                }
                //                if (find == false)
                //                {
                //                    fd.X = -1.0d;
                //                    fd.Y = -1.0d;
                //                    fd.Flag = -1;
                //                    FontList.Add(fd);
                //                }
                //            }
                //            else
                //            {
                //                fd.X = -1.0d;
                //                fd.Y = -1.0d;
                //                fd.Flag = -1;
                //                FontList.Add(fd);
                //            }
                //        }
                //        else
                //        {
                //            fd.X = -1.0d;
                //            fd.Y = -1.0d;
                //            fd.Flag = -1;
                //            FontList.Add(fd);
                //        }
                //        MyData.Add(idx, FontList);
                //        idx++;
                //    }
                //}

                if (MyData.Count <= 32)
                {
                    //System.Windows.MessageBox.Show("Invalid font file.");
                    //imgsource = null;
                    ErrorCode = "00HMF0000001";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font file data  is invalid (count = {0}", MyData.Count), Thread.CurrentThread.ManagedThreadId);
                    //                ErrorCode = "VH";
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
                shiftVal = 0;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        //public static int LoadFontData(string filename, ref Dictionary<int, List<FontDataClass>> MyData, out double fontsizeX, out double fontsizeY, out double shiftVal, out string ErrorCode)
        //{
        //    string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = MethodBase.GetCurrentMethod().Name;
        //    FileInfo fi;
        //    string fileline;
        //    ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //    try
        //    {
        //        fi = new FileInfo(filename);
        //        if (fi.Exists == false)
        //        {
        //            fontsizeX = 0.0d;
        //            fontsizeY = 0.0d;
        //            shiftVal = 0;
        //            ErrorCode = "00HMF0000003";
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);
        //            return -3;
        //        }
        //        List<string> FontDataClass = new List<string>();
        //        using (StreamReader sr = new StreamReader(filename))
        //        {
        //            string[] pointList;
        //            string[] point;

        //            string[] fontsize;
        //            fileline = sr.ReadLine();
        //            fontsize = fileline.Split(',');
        //            if (fontsize.Length < 1)
        //            {
        //                //System.Windows.MessageBox.Show("Font File is not valid");
        //                //imgsource = null;
        //                fontsizeX = 0.0d;
        //                fontsizeY = 0.0d;
        //                shiftVal = 0;
        //                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
        //                ErrorCode = "00HMF0000002";
        //                return -4;
        //            }
        //            fontsizeX = Math.Max((Convert.ToDouble(fontsize[0]) - 1.0d), 0.01d);
        //            fontsizeY = Math.Max((Convert.ToDouble(fontsize[1]) - 1.0d), 0.01d);
        //            if (fontsize.Length < 3)
        //                shiftVal = 0;
        //            else
        //                double.TryParse(fontsize[2], out shiftVal);

        //            int fontsizeYoffset = (fontsize.Length > 2) ? int.Parse(fontsize[2]) : 0;
        //            FontDataClass tmpfd = new FontDataClass() { X = -1.0d, Y = -1.0d, Flag = -1 };
        //            List<FontDataClass> tmplist = new List<FontDataClass>();
        //            tmplist.Add(tmpfd);
        //            MyData.Add(0, tmplist);

        //            int idx = 1;

        //            while ((fileline = sr.ReadLine()) != null)
        //            {
        //                List<FontDataClass> FontList = new List<FontDataClass>();
        //                FontDataClass fd;
        //                if (fileline.Length > 0)
        //                {
        //                    pointList = fileline.Split(';');
        //                    if (pointList.Count() > 0)
        //                    {
        //                        bool find = false;
        //                        for (int i = 0; i < pointList.Count(); i++)
        //                        {
        //                            point = pointList[i].Split(',');

        //                            if (point.Count() >= 3)
        //                            {
        //                                fd.X = Convert.ToDouble(point[0]);
        //                                fd.Y = (fontsizeY + (double)fontsizeYoffset - Convert.ToDouble(point[1]));
        //                                fd.Flag = Convert.ToInt32(point[2]);
        //                                FontList.Add(fd);
        //                                find = true;
        //                            }
        //                            find = true;
        //                        }
        //                        if (find == false)
        //                        {
        //                            fd.X = -1.0d;
        //                            fd.Y = -1.0d;
        //                            fd.Flag = -1;
        //                            FontList.Add(fd);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        fd.X = -1.0d;
        //                        fd.Y = -1.0d;
        //                        fd.Flag = -1;
        //                        FontList.Add(fd);
        //                    }
        //                }
        //                else
        //                {
        //                    fd.X = -1.0d;
        //                    fd.Y = -1.0d;
        //                    fd.Flag = -1;
        //                    FontList.Add(fd);
        //                }
        //                MyData.Add(idx, FontList);
        //                idx++;
        //            }
        //        }

        //        if (MyData.Count <= 32)
        //        {
        //            //System.Windows.MessageBox.Show("Invalid font file.");
        //            //imgsource = null;
        //            ErrorCode = "00HMF0000001";
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font file data  is invalid (count = {0}", MyData.Count), Thread.CurrentThread.ManagedThreadId);
        //            //                ErrorCode = "VH";
        //            return -5;
        //        }
        //        ErrorCode = "";
        //        ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        //        return 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorCode = string.Format("00HE{0:X8}", Math.Abs(ex.HResult));
        //        fontsizeX = 0.0d;
        //        fontsizeY = 0.0d;
        //        shiftVal = 0;
        //        //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Error - Exception : {0}", ErrorCode), Thread.CurrentThread.ManagedThreadId);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        return ex.HResult;
        //    }
        //}


        public static int GetOneCharacterFontData(char vin, string fontName, ref List<FontDataClass> FontDataLaser, out double fontsizeX, out double fontsizeY, out string ErrorCode)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = "";
            string lineFontValue = "";
            FileInfo fi;
            try
            {
                //string value = "";
                //Util.GetPrivateProfileValue("USEFONT", "TYPE", "0", ref value, "Parameter/FONT.ini");
                //if (value != "0")
                    filename = curDir + "FONTL\\S_" + fontName + ".FON";
                //else
                //    filename = curDir + "FONT\\S_" + fontName + ".FON";
                fi = new FileInfo(filename);

                if (fi.Exists == false)
                {
                    fontsizeX = 0.0d;
                    fontsizeY = 0.0d;
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);
                    return -3;
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
                        fontsizeX = 0.0d;
                        fontsizeY = 0.0d;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                        ErrorCode = "00HMF0000002";
                        return -4;
                    }
                    fontsizeX = Math.Max((Convert.ToDouble(fontsize[0]) - 1.0d), 0.01d);
                    fontsizeY = Math.Max((Convert.ToDouble(fontsize[1]) - 1.0d), 0.01d);

                    int SF = (fontsize.Length >= 3) ? int.Parse(fontsize[2]) : 0;

                    int charNum = (int)vin - 1;
                    lineFontValue = File.ReadLines(filename).Skip(charNum).Take(1).First();
                    //FontDataClass fd;
                    FontDataClass fd = new FontDataClass();
                    if (lineFontValue.Length > 0)
                    {
                        pointList = lineFontValue.Split(';');
                        if (pointList.Count() > 0)
                        {
                            for (int i = 0; i < pointList.Count(); i++)
                            {
                                point = pointList[i].Split(',');
                                if (point.Count() >= 3)
                                {
                                    fd.vector3d.X = Convert.ToDouble(point[0]);
                                    //fd.Y = (fontsizeY + (double)SF - Convert.ToDouble(point[1]));
                                    fd.vector3d.Y = (Convert.ToDouble(point[1]) - fontsizeY - (double)SF);
                                    fd.Flag = Convert.ToInt32(point[2]);
                                    fd.vector3d.Z = 0.0d;
                                    FontDataLaser.Add(fd);
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int GetOneCharacterPrintData(char vin, string fontName, ref List<FontDataClass> FontDataLaser, out double fontsizeX, out double fontsizeY, out double fontsizeZ, out string ErrorCode)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = "";
            string lineFontValue = "";
            FileInfo fi;
            try
            {
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;
                fontsizeZ = 0.0d;

                filename = curDir + "FONTL\\S_" + fontName + ".FON";
                fi = new FileInfo(filename);

                if (fi.Exists == false)
                {
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);
                    return -3;
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
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                        ErrorCode = "00HMF0000002";
                        return -4;
                    }
                    fontsizeX = Math.Max(Convert.ToDouble(fontsize[0]), 0.01d);
                    fontsizeY = Math.Max(Convert.ToDouble(fontsize[1]), 0.01d);
                    if (fontsize.Length >= 3)
                        fontsizeZ = Math.Max(Convert.ToDouble(fontsize[2]), 0.01d);

                    int charNum = (int)vin - 1;
                    lineFontValue = File.ReadLines(filename).Skip(charNum).Take(1).First();
                    //FontDataLaser fd;
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
                                    fd.vector3d.Y = Convert.ToDouble(point[1]);
                                    fd.vector3d.Z = 0.0d;
                                    fd.Flag = Convert.ToInt32(point[2]);
                                    FontDataLaser.Add(fd);
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
                fontsizeZ = 0.0d;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        public static int GetOneCharacterPrintData(char vin, string fontName, ref List<FontDataClass> fontData, out string ErrorCode)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(2, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = "";
            string lineFontValue = "";
            FileInfo fi;
            try
            {
                filename = curDir + "FONTL\\S_" + fontName + ".FON";
                fi = new FileInfo(filename);

                if (fi.Exists == false)
                {
                    ErrorCode = "00HMF0000003";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("File does not exist : {0}", fi.FullName), Thread.CurrentThread.ManagedThreadId);
                    return -3;
                }

                using (StreamReader sr = new StreamReader(filename))
                {
                    string[] pointList;
                    string[] point;

                    //string[] fontsize;
                    //lineFontValue = sr.ReadLine();
                    //fontsize = lineFontValue.Split(',');
                    //if (fontsize.Length < 2)
                    //{
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Font Size is invalid"), Thread.CurrentThread.ManagedThreadId);
                    //    ErrorCode = "00HMF0000002";
                    //    return -4;
                    //}
                    //fontsizeX = Math.Max(Convert.ToDouble(fontsize[0]), 0.01d);
                    //fontsizeY = Math.Max(Convert.ToDouble(fontsize[1]), 0.01d);
                    //if (fontsize.Length >= 3)
                    //    fontsizeZ = Math.Max(Convert.ToDouble(fontsize[2]), 0.01d);

                    int charNum = (int)vin - 1;
                    lineFontValue = File.ReadLines(filename).Skip(charNum).Take(1).First();
                    FontDataClass fd = new FontDataClass();
                    double tmp = 0;
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
                                    double.TryParse(point[0], out tmp);
                                    fd.vector3d.X = tmp;
                                    double.TryParse(point[1], out tmp);
                                    fd.vector3d.Y = tmp;
                                    int.TryParse(point[2], out fd.Flag);
                                    fd.vector3d.Z = 0.0d;
                                    //fd.X = Convert.ToDouble(point[0]);
                                    //fd.Y = Convert.ToDouble(point[1]);
                                    //fd.Flag = Convert.ToInt32(point[2]);
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }


        public int MakeImageFromFont(VinNoInfo vin, Thickness margin, byte saveFlag, out string ErrorCode, string saveFile = "ImageFromFont.jpg")
        {
            //DateTime beginTime = DateTime.Now;

            double pitch = vin.pitch;
            double width = vin.width;
            double height = vin.height;
            double fontsizeX = 4;
            double fontsizeY = 7;
            double fontsizeZ = 7;
            Dictionary<int, List<FontDataClass>> MyData = new Dictionary<int, List<FontDataClass>>();

            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                if (vin.vinNo.Length <= 0)
                {
                    ErrorCode = "00HMV0000001";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ERROR : INVALID VINNUMBER", Thread.CurrentThread.ManagedThreadId);
                    return -1;
                }

                int retval = GetFontData(vin, ref MyData, out fontsizeX, out fontsizeY, out fontsizeZ, out ErrorCode);
                if (retval != 0)
                {
                    return retval;
                }

                if (canvas.CheckAccess())
                {
                    MakeNSaveFile(vin, width, height, pitch, margin, fontsizeX, fontsizeY, MyData, saveFlag, saveFile);
                }
                else
                {
                    canvas.Dispatcher.Invoke(new Action(delegate
                    {
                        try
                        {
                            MakeNSaveFile(vin, width, height, pitch, margin, fontsizeX, fontsizeY, MyData, saveFlag, saveFile);
                        }
                        catch (Exception ex)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("MakeNSaveFile EXCEPTION2 - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                ErrorCode = string.Format("00HE{0:X8}", Math.Abs(ex.HResult));
                fontsizeX = 0.0d;
                fontsizeY = 0.0d;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION2 - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }

            ErrorCode = "";
            sw.Stop();
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("END (TIMESPAN  = {0})", sw.ElapsedMilliseconds), Thread.CurrentThread.ManagedThreadId);
            return 0;
        }

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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Exception - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Exception - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
        }

        private void MakeNSaveFile(VinNoInfo vin, double width, double height, double pitch, Thickness margin, double fontsizeX, double fontsizeY, Dictionary<int, List<FontDataClass>> MyData, byte saveFlag, string saveFile = "ImageFromFont.jpg")
        {
            double TextWidth = ((vin.vinNo.Length - 1) * pitch + width) * Util.PXPERMM;
            double TextHeight = Util.PXPERMM * height;

            Canvas canvas = new Canvas();
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
                brush = new Media.SolidColorBrush(Media.Color.FromArgb(255, (byte)200, (byte)200, (byte)200));
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
                        if ((MyData[charNum][j].Flag == 1) || (MyData[charNum][j].Flag == 3))
                        {
                            line[index] = new Line();
                            line[index].Stroke = brush;
                            line[index].StrokeThickness = vin.thickness * Util.PXPERMM;
                            line[index].StrokeStartLineCap = Media.PenLineCap.Round;
                            line[index].StrokeEndLineCap = Media.PenLineCap.Round;
                            line[index].StrokeLineJoin = Media.PenLineJoin.Round;

                            line[index].X1 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / fontsizeX + 0 + pitch_px * (double)i;
                            line[index].Y1 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / fontsizeY;
                        }
                        else if ((MyData[charNum][j].Flag == 2) || (MyData[charNum][j].Flag == 4))
                        {
                            if (line[index] != null)
                            {
                                line[index].X2 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / fontsizeX + 0 + pitch_px * (double)(i);
                                line[index].Y2 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / fontsizeY;

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

                            line[index].X1 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / fontsizeX + 0 + pitch_px * (double)(i);
                            line[index].Y1 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / fontsizeY;
                        }
                        //else if (MyData[charNum][j].Flag == 3)
                        //{
                        //    line[index] = new Line();
                        //    line[index].Stroke = brush;
                        //    line[index].StrokeThickness = vin.thickness * Util.PXPERMM;
                        //    line[index].StrokeStartLineCap = Media.PenLineCap.Round;
                        //    line[index].StrokeEndLineCap = Media.PenLineCap.Round;
                        //    line[index].StrokeLineJoin = Media.PenLineJoin.Round;

                        //    line[index].X1 = startX + (MyData[charNum][j].X * CharWidth) / fontsizeX + 0 + pitch_px * (double)i;
                        //    line[index].Y1 = startY + (MyData[charNum][j].Y * CharHeight) / fontsizeY;
                        //}
                        else if (MyData[charNum][j].Flag == 5)
                        {
                            if (line[index] != null)
                            {
                                line[index].X2 = startX + (MyData[charNum][j].vector3d.X * CharWidth) / fontsizeX + 0 + pitch_px * (double)(i);
                                line[index].Y2 = startY + (MyData[charNum][j].vector3d.Y * CharHeight) / fontsizeY;
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

        //public static void GetPatternData(string name, ref PatternValueLaser pattern)
        //{
        //    string patternName = ITNTCOMMON.Constants.PATTERN_PATH + name + ".ini";
        //    string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        //    string funcName = MethodBase.GetCurrentMethod().Name;
        //    //string value = "";
        //    try
        //    {
        //        pattern = new PatternValueLaser();
        //        //string patternName = ITNTCOMMON.Constants.PATTERN_PATH + patternfile + ".ini";

        //        //Font Value
        //        Util.GetPrivateProfileValue("FONT", "NAME", "11X16", ref pattern.fontValue.fontName, patternName); // load FONT
        //        pattern.fontValue.width = (double)Util.GetPrivateProfileValueDouble("FONT", "WIDTH", 4, patternName);
        //        pattern.fontValue.height = (double)Util.GetPrivateProfileValueDouble("FONT", "HEIGHT", 7, patternName);
        //        pattern.fontValue.pitch = (double)Util.GetPrivateProfileValueDouble("FONT", "PITCH", 6, patternName);
        //        pattern.fontValue.rotateAngle = (double)Util.GetPrivateProfileValueDouble("FONT", "ROTATEANGLE", 0, patternName);
        //        //pattern.fontValue.strikeCount = (short)Util.GetPrivateProfileValueInt("FONT", "STRIKECOUNT", 0, patternName);
        //        pattern.fontValue.thickness = (short)Util.GetPrivateProfileValueInt("FONT", "THICKNESS", 0, patternName);
        //        pattern.headValue.stepLength = (short)Util.GetPrivateProfileValueInt("MARK", "STEP_LENGTH", 0, patternName);


        //        pattern.speedValue.initSpeed4MarkR = (short)Util.GetPrivateProfileValueInt("MARKRASTER", "INITIALSPEED", 10, patternName);
        //        pattern.speedValue.targetSpeed4MarkR = (short)Util.GetPrivateProfileValueInt("MARKRASTER", "TARGETSPEED", 10, patternName);
        //        pattern.speedValue.accelSpeed4MarkR = (short)Util.GetPrivateProfileValueInt("MARKRASTER", "ACCELERATION", 15, patternName);
        //        pattern.speedValue.decelSpeed4MarkR = (short)Util.GetPrivateProfileValueInt("MARKRASTER", "DECELERATION", 15, patternName);

        //        pattern.speedValue.initSpeed4MarkV = (short)Util.GetPrivateProfileValueInt("MARKVECTOR", "INITIALSPEED", 10, patternName);
        //        pattern.speedValue.targetSpeed4MarkV = (short)Util.GetPrivateProfileValueInt("MARKVECTOR", "TARGETSPEED", 10, patternName);
        //        pattern.speedValue.accelSpeed4MarkV = (short)Util.GetPrivateProfileValueInt("MARKVECTOR", "ACCELERATION", 15, patternName);
        //        pattern.speedValue.decelSpeed4MarkV = (short)Util.GetPrivateProfileValueInt("MARKVECTOR", "DECELERATION", 15, patternName);

        //        pattern.speedValue.initSpeed4Home = (short)Util.GetPrivateProfileValueInt("MOVINGHOME", "INITIALSPEED", 10, patternName);
        //        pattern.speedValue.targetSpeed4Home = (short)Util.GetPrivateProfileValueInt("MOVINGHOME", "TARGETSPEED", 10, patternName);
        //        pattern.speedValue.accelSpeed4Home = (short)Util.GetPrivateProfileValueInt("MOVINGHOME", "ACCELERATION", 15, patternName);
        //        pattern.speedValue.decelSpeed4Home = (short)Util.GetPrivateProfileValueInt("MOVINGHOME", "DECELERATION", 15, patternName);

        //        pattern.speedValue.solOnTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "SOLONTIME", 10, patternName);
        //        pattern.speedValue.solOffTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "SOLOFFTIME", 10, patternName);
        //        pattern.speedValue.dwellTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "DWELLTIME", 10, patternName);

        //        pattern.speedValue.initSpeed4Free = (short)Util.GetPrivateProfileValueInt("FREEMOVING", "INITIALSPEED", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.speedValue.targetSpeed4Free = (short)Util.GetPrivateProfileValueInt("FREEMOVING", "TARGETSPEED", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.speedValue.accelSpeed4Free = (short)Util.GetPrivateProfileValueInt("FREEMOVING", "ACCELERATION", 10, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.speedValue.decelSpeed4Free = (short)Util.GetPrivateProfileValueInt("FREEMOVING", "DECELERATION", 10, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //        pattern.positionValue.start_X = (double)Util.GetPrivateProfileValueDouble("POSITION", "STARTPOSX", 20, patternName);
        //        pattern.positionValue.start_Y = (double)Util.GetPrivateProfileValueDouble("POSITION", "STARTPOSY", 50, patternName);
        //        pattern.positionValue.start_Z = (double)Util.GetPrivateProfileValueDouble("POSITION", "STARTPOSZ", 50, patternName);

        //        pattern.positionValue.park_X = (double)Util.GetPrivateProfileValueDouble("POSITION", "PARKING_X", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.positionValue.park_Y = (double)Util.GetPrivateProfileValueDouble("POSITION", "PARKING_Y", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.positionValue.park_Z = (double)Util.GetPrivateProfileValueDouble("POSITION", "PARKING_Z", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //        pattern.positionValue.offset_X = (double)Util.GetPrivateProfileValueDouble("POSITION", "OFFSET_X", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.positionValue.offset_Y = (double)Util.GetPrivateProfileValueDouble("POSITION", "OFFSET_Y", 40, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.positionValue.offset_Z = (double)Util.GetPrivateProfileValueDouble("POSITION", "OFFSET_Z", 110, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //        pattern.positionValue.rasterSP = (double)Util.GetPrivateProfileValueDouble("POSITION", "RASTERSP", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.positionValue.rasterEP = (double)Util.GetPrivateProfileValueDouble("POSITION", "RASTEREP", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //        pattern.headValue.stepLength = (short)Util.GetPrivateProfileValueInt("MARK", "STEP_LENGTH", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.headValue.max_x = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_X", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.headValue.max_y = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_Y", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.headValue.max_z = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_Z", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);



        //        pattern.laserValue.waveformNum = (short)Util.GetPrivateProfileValueInt("LASERSOURCE", "WAVEFORMPROFILE", 0, patternName);
        //        pattern.laserValue.waveformClean = (short)Util.GetPrivateProfileValueInt("LASERSOURCE", "WAVEFORMCLEAN", 0, patternName);
        //        pattern.laserValue.phaseComp = (short)Util.GetPrivateProfileValueInt("LASERSOURCE", "PHASECOMP", 0, patternName);
        //        pattern.laserValue.density = (short)Util.GetPrivateProfileValueInt("LASERSOURCE", "DENSITY", 0, patternName);
        //        pattern.laserValue.cleanPosition = (double)Util.GetPrivateProfileValueDouble("LASERSOURCE", "CLEANPOSITION", 0, patternName);
        //        pattern.laserValue.angleDegree = (short)Util.GetPrivateProfileValueInt("SENSOR", "ANGLEDEGREE", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

        //    }





        //    //try
        //    //{
        //    //    Util.GetPrivateProfileValue("FONT", "NAME", "OCR", ref pattern.fontName, patternfile); // load FONT

        //    //    pattern.startX = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSX", 20, patternfile);
        //    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("STARTPOSX = " + pattern.startX.ToString()), Thread.CurrentThread.ManagedThreadId);


        //    //    pattern.startY = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSY", 50, patternfile);
        //    //    pattern.startZ = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSZ", 10, patternfile);

        //    //    pattern.width = (double)Util.GetPrivateProfileValueDouble("FONT", "WIDTH", 4.2, patternfile);
        //    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("STARTPOSY = " + pattern.startY.ToString()), Thread.CurrentThread.ManagedThreadId);

        //    //    pattern.height = (double)Util.GetPrivateProfileValueDouble("FONT", "HEIGHT", 7.5, patternfile);
        //    //    pattern.pitch = (double)Util.GetPrivateProfileValueDouble("FONT", "PITCH", 6.2, patternfile);

        //    //    pattern.thickness = (double)Util.GetPrivateProfileValueDouble("FONT", "THICKNESS", 0.5, patternfile);
        //    //    //pattern.width = (short)Util.GetPrivateProfileValueInt("FONT", "WIDTH", 4, patternfile);
        //    //    //pattern.height = (short)Util.GetPrivateProfileValueInt("FONT", "HEIGHT", 7, patternfile);
        //    //    //pattern.pitch = (short)Util.GetPrivateProfileValueInt("FONT", "PITCH", 6, patternfile);
        //    //    pattern.rotateAngle = (double)Util.GetPrivateProfileValueDouble("FONT", "ROTATEANGLE", 0, patternfile);
        //    //    pattern.strikeCount = (short)Util.GetPrivateProfileValueInt("FONT", "STRIKECOUNT", 0, patternfile);

        //    //    pattern.initSpeed4Load = (short)Util.GetPrivateProfileValueInt("LOAD", "INITIALSPEED", 10, patternfile);
        //    //    pattern.targetSpeed4Load = (short)Util.GetPrivateProfileValueInt("LOAD", "TARGETSPEED", 10, patternfile);
        //    //    pattern.accelSpeed4Load = (short)Util.GetPrivateProfileValueInt("LOAD", "ACCELERATION", 15, patternfile);
        //    //    pattern.decelSpeed4Load = (short)Util.GetPrivateProfileValueInt("LOAD", "DECELERATION", 15, patternfile);
        //    //    pattern.solOnTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "SOLONTIME", 10, patternfile);
        //    //    pattern.solOffTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "SOLOFFTIME", 10, patternfile);
        //    //    pattern.dwellTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "DWELLTIME", 10, patternfile);
        //    //    pattern.stepLength = (short)Util.GetPrivateProfileValueInt("MARK", "STEP_LENGTH", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    //    pattern.max_x = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_X", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    //    pattern.max_y = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_Y", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    //    pattern.max_z = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_Z", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //    //    pattern.park_x = (double)Util.GetPrivateProfileValueDouble("PARKING", "X_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    //    pattern.park_y = (double)Util.GetPrivateProfileValueDouble("PARKING", "Y_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    //    pattern.park_z = (double)Util.GetPrivateProfileValueDouble("PARKING", "Z_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //    //    pattern.offset_x = (double)Util.GetPrivateProfileValueDouble("OFFSET", "X_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    //    pattern.offset_y = (double)Util.GetPrivateProfileValueDouble("OFFSET", "Y_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    //    pattern.offset_z = (double)Util.GetPrivateProfileValueDouble("OFFSET", "Z_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //    //    pattern.initSpeed4NoLoad = (short)Util.GetPrivateProfileValueInt("NOLOAD", "INITIALSPEED", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    //    pattern.targetSpeed4NoLoad = (short)Util.GetPrivateProfileValueInt("NOLOAD", "TARGETSPEED", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    //    pattern.accelSpeed4NoLoad = (short)Util.GetPrivateProfileValueInt("NOLOAD", "ACCELERATION", 10, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //    //    pattern.decelSpeed4NoLoad = (short)Util.GetPrivateProfileValueInt("NOLOAD", "DECELERATION", 10, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //    //    pattern.opmode = (short)Util.GetPrivateProfileValueInt("CONFIG", "OFFSET", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //    //    //pattern.stepLength_U = (short)Util.GetPrivateProfileValueInt("CONFIG", "STEP_LENGTH", 100, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.max_u = (short)Util.GetPrivateProfileValueInt("CONFIG", "MAX_U", 190, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.parkingU = (double)Util.GetPrivateProfileValueDouble("CONFIG", "PARKING", 90, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.home_u = (double)Util.GetPrivateProfileValueDouble("CONFIG", "HOME_U", 90, ITNTCOMMON.Constants.SCANNER_INI_FILE);

        //    //    pattern.initSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "INITIALSPEED", 10, patternfile);
        //    //    pattern.targetSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "TARGETSPEED", 10, patternfile);
        //    //    pattern.accelSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "ACCELERATION", 10, patternfile);
        //    //    pattern.decelSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "DECELERATION", 10, patternfile);

        //    //    pattern.initSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "INITIALSPEED", 10, patternfile);
        //    //    pattern.targetSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "TARGETSPEED", 10, patternfile);
        //    //    pattern.accelSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "ACCELERATION", 10, patternfile);
        //    //    pattern.decelSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "DECELERATION", 10, patternfile);

        //    //    //pattern.startU = (double)Util.GetPrivateProfileValueDouble("PROFILER", "STARTPOS", 20, patternfile); // load Max U_Scan
        //    //    //pattern.scanLen = (double)Util.GetPrivateProfileValueDouble("PROFILER", "SCANLEN", 130, patternfile);

        //    //    //pattern.stepLength_U = (short)Util.GetPrivateProfileValueInt("CONFIG", "STEP_LENGTH", 100, ITNTCOMMON.Constants.SCANNER_INI_FILE);

        //    //    //pattern.initSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "INITIALSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.targetSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "TARGETSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.accelSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "ACCELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.decelSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "DECELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);

        //    //    //pattern.initSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "INITIALSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.targetSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "TARGETSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.accelSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "ACCELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.decelSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "DECELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);

        //    //    //pattern.startU = (double)Util.GetPrivateProfileValueDouble("CONFIG", "STARTPOS", 20, ITNTCOMMON.Constants.SCANNER_INI_FILE); // load Max U_Scan
        //    //    //pattern.scanLen = (double)Util.GetPrivateProfileValueDouble("CONFIG", "SCANLEN", 120, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.max_u = (short)Util.GetPrivateProfileValueInt("CONFIG", "MAX_U", 190, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.parkingU = (double)Util.GetPrivateProfileValueDouble("CONFIG", "PARKING", 90, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //    //pattern.home_u = (double)Util.GetPrivateProfileValueDouble("CONFIG", "HOME_U", 90, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //    //}
        //}

        public static void GetPatternDataEx(string name, ref PatternValueEx pattern)
        {
            string patternfile = ITNTCOMMON.Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetPatternDataEx";

            try
            {
                pattern.name = name;
                GetPatternFontValue(name, ref pattern.fontValue);
                GetPatternSpeedValue(name, ref pattern.speedValue);
                GetPatternHeadValue(name, ref pattern.headValue);
                GetPatternPositionValue(name, ref pattern.positionValue);
                GetPatternLaserValue(name, ref pattern.laserValue);
                GetPatternScanValue(name, ref pattern.scanValue);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
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

        //public static void GetPatternDataManual(string name, string cartype, ref PatternValueLaser pattern)
        //{
        //    string patternfile = ITNTCOMMON.Constants.PATTERN_PATH + name + ".ini";
        //    //string value = "";
        //    string section = "FONT_" + cartype.Trim();
        //    if (SectionNameExist(patternfile, section) == false)
        //        section = "FONT";
        //    try
        //    {
        //        Util.GetPrivateProfileValue(section, "NAME", "OCR", ref pattern.fontName, patternfile); // load FONT

        //        pattern.startX                  = (double)Util.GetPrivateProfileValueDouble(section, "STARTPOSX", 20, patternfile);
        //        pattern.startY                  = (double)Util.GetPrivateProfileValueDouble(section, "STARTPOSY", 50, patternfile);
        //        pattern.startZ                  = (double)Util.GetPrivateProfileValueDouble(section, "STARTPOSZ", 50, patternfile);
        //        pattern.width                   = (double)Util.GetPrivateProfileValueDouble(section, "WIDTH", 4.2, patternfile);
        //        pattern.height                  = (double)Util.GetPrivateProfileValueDouble(section, "HEIGHT", 7.5, patternfile);
        //        pattern.pitch                   = (double)Util.GetPrivateProfileValueDouble(section, "PITCH", 6.2, patternfile);
        //        pattern.thickness               = (double)Util.GetPrivateProfileValueDouble(section, "THICKNESS", 0.5, patternfile);
        //        pattern.rotateAngle             = (double)Util.GetPrivateProfileValueDouble(section, "ROTATEANGLE", 0, patternfile);
        //        pattern.strikeCount             = (short)Util.GetPrivateProfileValueInt(section, "STRIKECOUNT", 0, patternfile);

        //        pattern.initSpeed4Load          = (short)Util.GetPrivateProfileValueInt("LOAD", "INITIALSPEED", 10, patternfile);
        //        pattern.targetSpeed4Load        = (short)Util.GetPrivateProfileValueInt("LOAD", "TARGETSPEED", 10, patternfile);
        //        pattern.accelSpeed4Load         = (short)Util.GetPrivateProfileValueInt("LOAD", "ACCELERATION", 15, patternfile);
        //        pattern.decelSpeed4Load         = (short)Util.GetPrivateProfileValueInt("LOAD", "DECELERATION", 15, patternfile);
        //        pattern.solOnTime               = (short)Util.GetPrivateProfileValueInt("SOLENOID", "SOLONTIME", 10, patternfile);
        //        pattern.solOffTime              = (short)Util.GetPrivateProfileValueInt("SOLENOID", "SOLOFFTIME", 10, patternfile);

        //        pattern.stepLength              = (short)Util.GetPrivateProfileValueInt("MARK", "STEP_LENGTH", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.max_x                   = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_X", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.max_y                   = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_Y", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.max_z                   = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_Z", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //        pattern.park_x                  = (double)Util.GetPrivateProfileValueDouble("PARKING", "X_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.park_y                  = (double)Util.GetPrivateProfileValueDouble("PARKING", "Y_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.park_z                  = (double)Util.GetPrivateProfileValueDouble("PARKING", "Z_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //        pattern.offset_x                = (double)Util.GetPrivateProfileValueDouble("OFFSET", "X_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.offset_y                = (double)Util.GetPrivateProfileValueDouble("OFFSET", "Y_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.offset_z                = (double)Util.GetPrivateProfileValueDouble("OFFSET", "Z_POSITION", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //        pattern.initSpeed4NoLoad        = (short)Util.GetPrivateProfileValueInt("NOLOAD", "INITIALSPEED", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.targetSpeed4NoLoad      = (short)Util.GetPrivateProfileValueInt("NOLOAD", "TARGETSPEED", 50, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.accelSpeed4NoLoad       = (short)Util.GetPrivateProfileValueInt("NOLOAD", "ACCELERATION", 10, ITNTCOMMON.Constants.MARKING_INI_FILE);
        //        pattern.decelSpeed4NoLoad       = (short)Util.GetPrivateProfileValueInt("NOLOAD", "DECELERATION", 10, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //        pattern.opmode                  = (short)Util.GetPrivateProfileValueInt("CONFIG", "OFFSET", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);

        //        //pattern.stepLength_U            = (short)Util.GetPrivateProfileValueInt("CONFIG", "STEP_LENGTH", 100, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.max_u                   = (short)Util.GetPrivateProfileValueInt("CONFIG", "MAX_U", 190, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.parkingU                = (double)Util.GetPrivateProfileValueDouble("CONFIG", "PARKING", 90, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.home_u                  = (double)Util.GetPrivateProfileValueDouble("CONFIG", "HOME_U", 90, ITNTCOMMON.Constants.SCANNER_INI_FILE);

        //        pattern.initSpeed4Scan          = (short)Util.GetPrivateProfileValueInt("SCAN", "INITIALSPEED", 10, patternfile);
        //        pattern.targetSpeed4Scan        = (short)Util.GetPrivateProfileValueInt("SCAN", "TARGETSPEED", 10, patternfile);
        //        pattern.accelSpeed4Scan         = (short)Util.GetPrivateProfileValueInt("SCAN", "ACCELERATION", 10, patternfile);
        //        pattern.decelSpeed4Scan         = (short)Util.GetPrivateProfileValueInt("SCAN", "DECELERATION", 10, patternfile);

        //        pattern.initSpeed4ScanFree      = (short)Util.GetPrivateProfileValueInt("SCANFREE", "INITIALSPEED", 10, patternfile);
        //        pattern.targetSpeed4ScanFree    = (short)Util.GetPrivateProfileValueInt("SCANFREE", "TARGETSPEED", 10, patternfile);
        //        pattern.accelSpeed4ScanFree     = (short)Util.GetPrivateProfileValueInt("SCANFREE", "ACCELERATION", 10, patternfile);
        //        pattern.decelSpeed4ScanFree     = (short)Util.GetPrivateProfileValueInt("SCANFREE", "DECELERATION", 10, patternfile);

        //        //pattern.startU                  = (double)Util.GetPrivateProfileValueDouble("PROFILER", "STARTPOS", 20, patternfile); // load Max U_Scan
        //        //pattern.scanLen                 = (double)Util.GetPrivateProfileValueDouble("PROFILER", "SCANLEN", 130, patternfile);

        //        //pattern.stepLength_U = (short)Util.GetPrivateProfileValueInt("CONFIG", "STEP_LENGTH", 100, ITNTCOMMON.Constants.SCANNER_INI_FILE);

        //        //pattern.initSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "INITIALSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.targetSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "TARGETSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.accelSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "ACCELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.decelSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "DECELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);

        //        //pattern.initSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "INITIALSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.targetSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "TARGETSPEED", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.accelSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "ACCELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.decelSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "DECELERATION", 10, ITNTCOMMON.Constants.SCANNER_INI_FILE);

        //        //pattern.startU = (double)Util.GetPrivateProfileValueDouble("CONFIG", "STARTPOS", 20, ITNTCOMMON.Constants.SCANNER_INI_FILE); // load Max U_Scan
        //        //pattern.scanLen = (double)Util.GetPrivateProfileValueDouble("CONFIG", "SCANLEN", 120, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.max_u = (short)Util.GetPrivateProfileValueInt("CONFIG", "MAX_U", 190, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.parkingU = (double)Util.GetPrivateProfileValueDouble("CONFIG", "PARKING", 90, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //        //pattern.home_u = (double)Util.GetPrivateProfileValueDouble("CONFIG", "HOME_U", 90, ITNTCOMMON.Constants.SCANNER_INI_FILE);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        //public static void GetStartPointLinear(int count, MPOINT CP, MPOINT START_XY, double PITCH, double ANG, ref List<Point> POS)
        public static void GetStartPointLinear(int count, Vector3D CP, Vector3D START_XY, double PITCH, double ANG, ref List<Point> POS)
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

        public static void GetStartPointLinear2(int count, Vector3D CP, Vector3D START_XY, double PITCH, double ANG, ref List<Vector3D> POS)
        {
            int i;
            Vector3D pt = new Vector3D();
            try
            {
                for (i = 0; i <= count - 1; i++)
                {
                    Point p1 = new Point();
                    p1 = Rotate_Point(START_XY.X + (i * PITCH), START_XY.Y, CP.X, CP.Y, ANG);
                    pt.X = p1.X;
                    pt.Y = p1.Y;
                    pt.Z = 0;
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
            Vector3D returnValue = new Vector3D();
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
                returnValue.Z = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Rotate_Point Exception - {0}, {1}", ex.HResult, ex.Message);
            }
            return returnValue;
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
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            ITNTTraceLog.Instance.Trace(2, "{0}::{1}()  {2}", className, funcName, "END - " + retval);
            return retval;
        }

        public static void GetPatternFontValue(string name, ref FontValue font)
        {
            string patternfile = ITNTCOMMON.Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetPatternFontValue";

            try
            {
                Util.GetPrivateProfileValue("FONT", "NAME", "OCR", ref font.fontName, patternfile); // load FONT
                font.width = (double)Util.GetPrivateProfileValueDouble("FONT", "WIDTH", 4, patternfile);
                font.height = (double)Util.GetPrivateProfileValueDouble("FONT", "HEIGHT", 7, patternfile);
                font.pitch = (double)Util.GetPrivateProfileValueDouble("FONT", "PITCH", 6, patternfile);
                font.rotateAngle = (double)Util.GetPrivateProfileValueDouble("FONT", "ROTATEANGLE", 0, patternfile);
                font.strikeCount = (short)Util.GetPrivateProfileValueInt("FONT", "STRIKECOUNT", 0, patternfile);
                font.thickness = (double)Util.GetPrivateProfileValueDouble("FONT", "THICKNESS", 0.5, patternfile);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }


        public static void GetPatternSpeedValue(string name, ref SpeedValue speedValue)
        {
            string patternfile = ITNTCOMMON.Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetSpeedValue";

            try
            {
                //speed value
                speedValue.initSpeed4MarkV = (short)Util.GetPrivateProfileValueInt("LOAD", "INITIALSPEED", 20, patternfile);
                speedValue.targetSpeed4MarkV = (short)Util.GetPrivateProfileValueInt("LOAD", "TARGETSPEED", 40, patternfile);
                speedValue.accelSpeed4MarkV = (short)Util.GetPrivateProfileValueInt("LOAD", "ACCELERATION", 15, patternfile);
                speedValue.decelSpeed4MarkV = (short)Util.GetPrivateProfileValueInt("LOAD", "DECELERATION", 15, patternfile);

                speedValue.initSpeed4MarkR = (short)Util.GetPrivateProfileValueInt("MARKRASTER", "INITIALSPEED", 10, patternfile);
                speedValue.targetSpeed4MarkR = (short)Util.GetPrivateProfileValueInt("MARKRASTER", "TARGETSPEED", 10, patternfile);
                speedValue.accelSpeed4MarkR = (short)Util.GetPrivateProfileValueInt("MARKRASTER", "ACCELERATION", 15, patternfile);
                speedValue.decelSpeed4MarkR = (short)Util.GetPrivateProfileValueInt("MARKRASTER", "DECELERATION", 15, patternfile);

                speedValue.initSpeed4Home = (short)Util.GetPrivateProfileValueInt("MOVINGHOME", "INITIALSPEED", 10, patternfile);
                speedValue.targetSpeed4Home = (short)Util.GetPrivateProfileValueInt("MOVINGHOME", "TARGETSPEED", 10, patternfile);
                speedValue.accelSpeed4Home = (short)Util.GetPrivateProfileValueInt("MOVINGHOME", "ACCELERATION", 15, patternfile);
                speedValue.decelSpeed4Home = (short)Util.GetPrivateProfileValueInt("MOVINGHOME", "DECELERATION", 15, patternfile);

                speedValue.initSpeed4Measure = (short)Util.GetPrivateProfileValueInt("MOVINGMEASURE", "INITIALSPEED", 10, patternfile);
                speedValue.targetSpeed4Measure = (short)Util.GetPrivateProfileValueInt("MOVINGMEASURE", "TARGETSPEED", 10, patternfile);
                speedValue.accelSpeed4Measure = (short)Util.GetPrivateProfileValueInt("MOVINGMEASURE", "ACCELERATION", 15, patternfile);
                speedValue.decelSpeed4Measure = (short)Util.GetPrivateProfileValueInt("MOVINGMEASURE", "DECELERATION", 15, patternfile);

                //speedValue.initSpeed4Free = (short)Util.GetPrivateProfileValueInt("NOLOAD", "INITIALSPEED", 50, "MarkHeader.ini");
                //speedValue.targetSpeed4Free = (short)Util.GetPrivateProfileValueInt("NOLOAD", "TARGETSPEED", 50, "MarkHeader.ini");
                //speedValue.accelSpeed4Free = (short)Util.GetPrivateProfileValueInt("NOLOAD", "ACCELERATION", 10, "MarkHeader.ini");
                //speedValue.decelSpeed4Free = (short)Util.GetPrivateProfileValueInt("NOLOAD", "DECELERATION", 10, "MarkHeader.ini");

                speedValue.solOnTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "SOLONTIME", 10, patternfile);
                speedValue.solOffTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "SOLOFFTIME", 10, patternfile);
                speedValue.dwellTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "DWELLTIME", 10, patternfile);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public static void GetPatternHeadValue(string name, ref HeadValue headval)
        {
            //string patternfile = ITNTCOMMON.Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetHeadValue";

            try
            {
                headval.stepLength = (short)Util.GetPrivateProfileValueInt("MARK", "STEP_LENGTH", 50, "MarkHeader.ini");
                headval.max_X = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_X", 0, "MarkHeader.ini");
                headval.max_Y = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_Y", 0, "MarkHeader.ini");
                headval.max_Z = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_Z", 0, "MarkHeader.ini");
                headval.angleDegree = (short)Util.GetPrivateProfileValueInt("SENSOR", "ANGLEDEGREE", 0, ITNTCOMMON.Constants.MARKING_INI_FILE);// "MarkHeader.ini");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public static void GetPatternPositionValue(string name, ref PositionValue positionValue)
        {
            string patternfile = ITNTCOMMON.Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetPositionValue";

            try
            {
                if (name.Length <= 0)
                    patternfile = ITNTCOMMON.Constants.PATTERN_PATH + "Pattern_DEFAULT.ini";

                FileInfo fi = new FileInfo(patternfile);
                if (fi.Exists == false)
                {
                    positionValue.center3DPos.X = 20;
                    positionValue.center3DPos.Y = 50;
                    positionValue.center3DPos.Z = 20;

                    positionValue.park3DPos.X = 0;
                    positionValue.park3DPos.Y = 0;
                    positionValue.park3DPos.Z = 0;

                    positionValue.offset3DPos.X = 0;
                    positionValue.offset3DPos.Y = 0;
                    positionValue.offset3DPos.Z = 0;

                    positionValue.rasterSP = 0;
                    positionValue.rasterEP = 0;
                    return;
                }

                //position value
                //positionValue.start_X = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSX", -1, patternfile);
                //if (positionValue.start_X < 0)
                //    positionValue.start_X = (double)Util.GetPrivateProfileValueDouble("POSITION", "STARTPOSX", 20, patternfile);

                //positionValue.start_Y = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSY", -1, patternfile);
                //if (positionValue.start_Y < 0)
                //    positionValue.start_Y = (double)Util.GetPrivateProfileValueDouble("POSITION", "STARTPOSY", 50, patternfile);

                //positionValue.start_Z = (double)Util.GetPrivateProfileValueDouble("FONT", "STARTPOSZ", -1, patternfile);
                //if (positionValue.start_Z < 0)
                //    positionValue.start_Z = (double)Util.GetPrivateProfileValueDouble("POSITION", "STARTPOSZ", 20, patternfile);

                positionValue.park3DPos.X = (double)Util.GetPrivateProfileValueDouble("PARKING", "X_POSITION", 0, "MarkHeader.ini");
                positionValue.park3DPos.Y = (double)Util.GetPrivateProfileValueDouble("PARKING", "Y_POSITION", 0, "MarkHeader.ini");
                positionValue.park3DPos.Z = (double)Util.GetPrivateProfileValueDouble("PARKING", "Z_POSITION", 0, "MarkHeader.ini");

                positionValue.offset3DPos.X = (double)Util.GetPrivateProfileValueDouble("POSITION", "OFFSET_X", 0, "MarkHeader.ini");
                positionValue.offset3DPos.Y = (double)Util.GetPrivateProfileValueDouble("POSITION", "OFFSET_Y", 40, "MarkHeader.ini");
                positionValue.offset3DPos.Z = (double)Util.GetPrivateProfileValueDouble("POSITION", "OFFSET_Z", 110, "MarkHeader.ini");

                positionValue.rasterSP = (double)Util.GetPrivateProfileValueDouble("POSITION", "RASTERSP", 0, "MarkHeader.ini");
                positionValue.rasterEP = (double)Util.GetPrivateProfileValueDouble("POSITION", "RASTEREP", 0, "MarkHeader.ini");
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public static void GetPatternLaserValue(string name, ref LaserValue laserValue)
        {
            string patternfile = ITNTCOMMON.Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetLaserValue";

            try
            {
                if (name.Length <= 0)
                    patternfile = ITNTCOMMON.Constants.PATTERN_PATH + "Pattern_DEFAULT.ini";

                FileInfo fi = new FileInfo(patternfile);
                if (fi.Exists == false)
                {
                    laserValue.waveformNum = 0;
                    laserValue.waveformClean = 0;
                    laserValue.phaseComp = 0;
                    laserValue.density = 0;
                    laserValue.cleanPosition = 0;
                    return;
                }
                // laser value
                laserValue.waveformNum = (short)Util.GetPrivateProfileValueInt("LASERSOURCE", "WAVEFORMPROFILE", 0, patternfile);
                laserValue.waveformClean = (short)Util.GetPrivateProfileValueInt("LASERSOURCE", "WAVEFORMCLEAN", 0, patternfile);
                laserValue.phaseComp = (short)Util.GetPrivateProfileValueInt("LASERSOURCE", "PHASECOMP", 0, patternfile);
                laserValue.density = (short)Util.GetPrivateProfileValueInt("LASERSOURCE", "DENSITY", 0, patternfile);
                laserValue.cleanPosition = (double)Util.GetPrivateProfileValueDouble("LASERSOURCE", "CLEANPOSITION", 0, patternfile);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public static void GetPatternScanValue(string name, ref ScanValue scanValue)
        {
            string patternfile = ITNTCOMMON.Constants.PATTERN_PATH + name + ".ini";
            string className = "ImageProcessManager";
            string funcName = "GetScanValue";

            try
            {
                if (name.Length <= 0)
                    patternfile = ITNTCOMMON.Constants.PATTERN_PATH + "Pattern_DEFAULT.ini";

                FileInfo fi = new FileInfo(patternfile);

                //scan value
                scanValue.stepLength_U = (short)Util.GetPrivateProfileValueInt("CONFIG", "STEP_LENGTH", 100, "ProfileScanner.ini");
                scanValue.max_U = (short)Util.GetPrivateProfileValueInt("CONFIG", "MAX_U", 190, "ProfileScanner.ini");
                scanValue.parkingU = (Single)Util.GetPrivateProfileValueDouble("CONFIG", "PARKING", 90, "ProfileScanner.ini");
                scanValue.home_U = (double)Util.GetPrivateProfileValueDouble("CONFIG", "HOME_U", 90, "ProfileScanner.ini");

                scanValue.initSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "INITIALSPEED", 10, patternfile);
                scanValue.targetSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "TARGETSPEED", 10, patternfile);
                scanValue.accelSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "ACCELERATION", 10, patternfile);
                scanValue.decelSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "DECELERATION", 10, patternfile);

                scanValue.initSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "INITIALSPEED", 10, patternfile);
                scanValue.targetSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "TARGETSPEED", 10, patternfile);
                scanValue.accelSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "ACCELERATION", 10, patternfile);
                scanValue.decelSpeed4ScanFree = (short)Util.GetPrivateProfileValueInt("SCANFREE", "DECELERATION", 10, patternfile);

                scanValue.startU = (double)Util.GetPrivateProfileValueDouble("PROFILER", "STARTPOS", 20, patternfile); // load Max U_Scan
                scanValue.scanLen = (double)Util.GetPrivateProfileValueDouble("PROFILER", "SCANLEN", 130, patternfile);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE : {0}, MSG : {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }
    }
}
