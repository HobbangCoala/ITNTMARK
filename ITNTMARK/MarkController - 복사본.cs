using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ITNTCOMMM;
using ITNTUTIL;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using ITNTCOMMON;
using System.Windows;

namespace ITNTMARK
{
    public class MarkController : MarkComm
    {
        const byte CMD_MOVE_ABS =       0x01;
        const byte CMD_MOVE_INC =       0x02;
        const byte CMD_SOL_TEST =       0x04;
        const byte CMD_SOL_ON_OFF =     0x0A;


        //Thread _readThread;
        //bool DoReading = false;


        public MarkController()
        {
            //SendFlag = SENDFLAG_IDLE;
        }

        public int OpenMarkController()
        {
            int retval = 0;
            string value = "";
            string portnum = "COM";
            int baud = 19200;

            try
            {
                if ((Port != null) && (Port.IsOpen))
                    return 0;

                Util.GetPrivateProfileValue("MARK", "PORT", "COM1", ref portnum, Constants.PARAMS_INI_FILE);
                //portnum += value;
                Util.GetPrivateProfileValue("MARK", "BAUDRATE", "COM1", ref value, Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out baud);
                retval = OpenDevice(portnum, baud, 8, Parity.None, StopBits.One);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "OpenMarkController", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                retval = ex.HResult;
            }
            return retval;
        }

        public async Task<int> OpenMarkControllerAsync()
        {
            int retval = 0;
            string value = "";
            string portnum = "";
            int baud = 19200;

            try
            {
                if ((Port != null) && (Port.IsOpen))
                    return 0;

                Util.GetPrivateProfileValue("MARK", "PORT", "COM1", ref portnum, Constants.PARAMS_INI_FILE);
                //portnum += value;
                Util.GetPrivateProfileValue("MARK", "BAUDRATE", "19200", ref value, Constants.PARAMS_INI_FILE);
                Int32.TryParse(value, out baud);
                retval = await OpenDeviceAsync(portnum, baud, 8, Parity.None, StopBits.One);
            }
            catch(Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "OpenMarkControllerAsync", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                retval = ex.HResult;
            }
            return retval;
        }

        public void CloseMarkController()
        {
            if (Port == null)
                return;
            if(!Port.IsOpen)
                return;

            CloseDevice();
        }

        public int Controller_Online()
        {
            int retval = 0;

            return retval;
        }

        //public async Task<int> SendPattern2MarkController(string pattern)
        //{
        //    int retval = 0;

        //    return retval;
        //}

        public async Task<int> StartMark2MarkController(short count)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            byte[] value = Encoding.UTF8.GetBytes(count.ToString("X4"));
            retval = await RunStart(value, value.Length);
            return retval.execResult;
        }

        //public async Task<int> SendFontData2MarkController(string vin, List<List<FontData>> fontData)
        //{
        //    int retval = 0;

        //    return retval;
        //}

        public async Task<int> SendFontData2MarkController(string vin, string name)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            retval = await SendFontData(vin, name);
            return retval.execResult;
        }
        
        /////////////////////////////////////////////////
        /////////////////////////////////////////////////
        ///
        //public async Task<ITNTResponseArgs> SendFontData(MarkVINInform2 markData)// string DATA_, string Pattern) // Start Text
        public async Task<ITNTResponseArgs> SendFontData(string vin, string patternName)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            double stepLength;
            string value = "";
            Point SP = new Point();
            double Step_W;
            double Step_H;
            string SetSpeed = "";
            string SetSol_OnOff = "";
            string Strikes = "";
            short i;
            short j;
            int idx = 0;
            string sendstring = "";
            byte[] sbuff;
            Pattern pattern = new Pattern();
            double fontSizeX = 0.0d;
            double fontSizeY = 0.0d;
            Stopwatch sw = new Stopwatch();

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "SendFontData", "START - " + vin);
                sw.Start();
                Util.GetPrivateProfileValue("MARK", "STEP_LENGTH", "50", ref value, Constants.PARAMS_INI_FILE);
                double.TryParse(value, out stepLength);

                ImageProcessManager.GetPatternData(patternName, ref pattern);

                SetSpeed = pattern.initSpeed4Load.ToString("X4") + pattern.targetSpeed4Load.ToString("X4") + pattern.accelSpeed4Load.ToString("X4") + pattern.decelSpeed4Load.ToString("X4");// set speed
                SetSol_OnOff = pattern.solOnTime.ToString("X4") + pattern.solOffTime.ToString("X4"); // set Sol OnOff time
                Strikes = pattern.strikeCount.ToString("X4"); //set Strike 

                byte[] speed = Encoding.UTF8.GetBytes(SetSpeed);
                byte[] solOnOff = Encoding.UTF8.GetBytes(SetSpeed);
                byte[] strikeCount = Encoding.UTF8.GetBytes(SetSpeed);

                retval = await LoadSpeed(speed, speed.Length).ConfigureAwait(false); //send speed to MCU
                if(retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "SendFontData", string.Format("LoadSpeed ERROR = {0}", retval.execResult));
                    return retval;
                }
                retval = await SolOnOffTime(solOnOff, solOnOff.Length).ConfigureAwait(false);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "SendFontData", string.Format("SolOnOffTime ERROR = {0}", retval.execResult));
                    return retval;
                }
                retval = await StrikeNo(strikeCount, strikeCount.Length).ConfigureAwait(false); // Marking couter
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "SendFontData", string.Format("StrikeNo ERROR = {0}", retval.execResult));
                    return retval;
                }

                switch (pattern.rotateAngle)
                {
                    case 0:
                        SP.X = pattern.startX * stepLength;
                        break;

                    case 180:
                        SP.X = pattern.startX - pattern.pitch * (vin.Length - 1) + pattern.width * stepLength;
                        break;
                    default:
                        SP.X = pattern.startX * stepLength;
                        break;
                }
                SP.Y = pattern.startY * stepLength;

                Point TP = new Point();
                TP.X = pattern.startX * stepLength;
                TP.Y = pattern.startY * stepLength;

                List<Point> changedPoint = new List<Point>();
                FontData movedData = new FontData();

                ImageProcessManager.GetStartPointLinear(vin.Length, TP, SP, pattern.pitch * stepLength, pattern.rotateAngle, ref changedPoint);
                for (i = 0; i < changedPoint.Count; i++)
                {
                    List<FontData> fdatas = new List<FontData>();
                    string error = "";
                    ImageProcessManager.GetOneCharacterFontData((char)vin[i], pattern.fontName, ref fdatas, out fontSizeX, out fontSizeY, out error);
                    Step_W = pattern.width / (fontSizeX - 1) * stepLength;
                    Step_H = pattern.height / (fontSizeY - 1) * stepLength;
                    //fdatas = markData.fontData[i];
                    //for (j = 0; j < fdatas.Count - 1; j++) //From big Arrays GET SMALL Arrays in Font
                    for (j = 0; j < fdatas.Count; j++) //From big Arrays GET SMALL Arrays in Font
                    {
                        Point RP = new Point();
                        FontData fontValue = new FontData();
                        fontValue = fdatas[j];
                        movedData.X = changedPoint[i].X + fontValue.X * Step_W;
                        movedData.Y = changedPoint[i].Y + fontValue.Y * Step_H;

                        if (pattern.fontName == "5X7")
                            movedData.Y = changedPoint[i].Y + (fontValue.Y - 3) * Step_H;
                        else if (pattern.fontName == "11X16")
                            movedData.Y = changedPoint[i].Y + (fontValue.Y - 5) * Step_H;

                        RP = ImageProcessManager.Rotate_Point(movedData.X, movedData.Y, changedPoint[i].X, changedPoint[i].Y, pattern.rotateAngle);

                        short moveX = (short)(movedData.X + 0.5);
                        short moveY = (short)(movedData.Y + 0.5);
                        //sendstring = string.Format("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", i, j, movedData.X, movedData.Y, movedData.Flag);
                        sendstring = i.ToString("X4") + j.ToString("X4") + moveX.ToString("X4") + moveY.ToString("X4") + movedData.Flag.ToString("X4");
                        sbuff = Encoding.UTF8.GetBytes(sendstring);
                        retval = await LoadFontData(sbuff, sbuff.Length).ConfigureAwait(false); // send Point to MCU
                        ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "SendFontData", string.Format("MARK {0}, {1}", i, j));
                        if (retval.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "SendFontData", string.Format("LoadFontData({0}, {1}) ERROR = {2}", i, j, retval.execResult));
                            return retval;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "SendFontData", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                retval.execResult = ex.HResult;
            }
            sw.Stop();
            ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "SendFontData", string.Format("END - {0}", sw.ElapsedMilliseconds));
            return retval;
        }

        public async Task<ITNTResponseArgs> CHECK_Area(MarkVINInform2 markData)
        { // check box 4

            ITNTResponseArgs retval = new ITNTResponseArgs();
            double stepLength;
            string value = "";
            Point SP = new Point();
            double Step_W;
            double Step_H;
            string SetSpeed = "";
            string SetSol_OnOff = "";
            string Strikes = "";
            int i;
            int j;
            int idx = 0;
            string sendstring = "";
            byte[] sbuff;

            try
            {
                Util.GetPrivateProfileValue("MARK", "STEP_LENGTH", "50", ref value, Constants.PARAMS_INI_FILE);
                double.TryParse(value, out stepLength);

                SetSpeed = markData.pattern.initSpeed4Load.ToString("X4") + markData.pattern.targetSpeed4Load.ToString("X4") + markData.pattern.accelSpeed4Load.ToString("X8") + markData.pattern.decelSpeed4Load.ToString("X8");// set speed
                SetSol_OnOff = markData.pattern.solOnTime.ToString("X4") + markData.pattern.solOffTime.ToString("X4"); // set Sol OnOff time
                Strikes = markData.pattern.strikeCount.ToString("X4"); //set Strike 

                byte[] speed = Encoding.UTF8.GetBytes(SetSpeed);
                byte[] solOnOff = Encoding.UTF8.GetBytes(SetSpeed);
                byte[] strikeCount = Encoding.UTF8.GetBytes(SetSpeed);

                retval = await LoadSpeed(speed, speed.Length); //send speed to MCU
                retval = await SolOnOffTime(solOnOff, solOnOff.Length);
                retval = await StrikeNo(strikeCount, strikeCount.Length); // Marking couter

                Step_W = markData.pattern.width / (markData.fontSizeX - 1) * stepLength;
                Step_H = markData.pattern.height / (markData.fontSizeY - 1) * stepLength;

                switch (markData.pattern.rotateAngle)
                {
                    case 0:
                        SP.X = markData.pattern.startX * stepLength;
                        break;

                    case 180:
                        SP.X = markData.pattern.startX - markData.pattern.pitch * (markData.mesData.vin.Length - 1) + markData.pattern.width * stepLength;
                        break;
                    default:
                        SP.X = markData.pattern.startX * stepLength;
                        break;
                }
                SP.Y = markData.pattern.startY * stepLength;

                Point TP = new Point();
                TP.X = markData.pattern.startX * stepLength;
                TP.Y = markData.pattern.startY * stepLength;

                List<Point> changedPoint = new List<Point>();
                FontData movedData = new FontData();
                Point RP = new Point();

                ImageProcessManager.GetStartPointLinear(markData.mesData.vin.Length, TP, SP, markData.pattern.pitch * stepLength, markData.pattern.rotateAngle, ref changedPoint);
                for (i = 0; i < changedPoint.Count; i++)
                {
                    List<FontData> fdatas = new List<FontData>();
                    fdatas = markData.fontData[i];
                    for (j = 0; j < fdatas.Count - 1; j++) //From big Arrays GET SMALL Arrays in Font
                    {
                        FontData fontValue = new FontData();
                        fontValue = fdatas[j];
                        movedData.X = changedPoint[i].X + fontValue.X * Step_W;
                        movedData.Y = changedPoint[i].Y + fontValue.Y * Step_H;

                        if (markData.pattern.fontName == "5X7")
                            movedData.Y = changedPoint[i].Y + (fontValue.Y - 3) * Step_H;
                        else if (markData.pattern.fontName == "11X16")
                            movedData.Y = changedPoint[i].Y + (fontValue.Y - 5) * Step_H;

                        RP = ImageProcessManager.Rotate_Point(movedData.X, movedData.Y, changedPoint[i].X, changedPoint[i].Y, markData.pattern.rotateAngle);

                        sendstring = string.Format("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", i, j, movedData.X, movedData.Y, movedData.Flag);
                        sbuff = Encoding.UTF8.GetBytes(sendstring);
                        retval = await LoadFontData(sbuff, sbuff.Length) ; // send Point to MCU
                        if (retval.execResult != 0)
                            return retval;
                    }
                }
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> Range_Test()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string HomeXY = "";
            string Range_MaxMinXY = "";
            //string value = "";
            short maxX = 0;
            short maxY = 0;
            byte[] byHomeXY;
            byte[] byHome;
            byte[] testbox = new byte[2];
            short zero = 0;
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Range_Test", "START");

                maxX = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_X", 9650, Constants.PARAMS_INI_FILE);
                maxY = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_Y", 3150, Constants.PARAMS_INI_FILE);

                HomeXY = zero.ToString("X4") + maxX.ToString("X4");

                //var HomeXY = string.Concat(0.ToString("X4"), Mode_File.MAX_Y.ToString("X4"));
                //var Range_MaxMinXY = string.Concat(Mode_File.gMinX.ToString("X4"), Mode_File.gMaxX.ToString("X4"), Mode_File.gMinY.ToString("X4"), Mode_File.gMaxY.ToString("X4"));
                byHome = Encoding.UTF8.GetBytes(HomeXY);
                retval = await GoHome(byHome, byHome.Length); //GoHome
                if(retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Range_Test", string.Format("GoHome return : {0}", retval.execResult));
                    return retval;
                }

                //retval = SetMaxMinXY("X", Range_MaxMinXY).Result;
                //if (retval.execResult != 0)
                //{
                //    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Range_Test", string.Format("GoHome return : {0}", retval.execResult));
                //    return retval;
                //}

                retval = await TestBox4(testbox, 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Range_Test", string.Format("GoHome return : {0}", retval.execResult));
                    return retval;
                }

                retval = await GoHome(byHome, byHome.Length); //GoHome
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Range_Test", string.Format("GoHome return : {0}", retval.execResult));
                    return retval;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Range_Test", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> Head_Range_Test()
        {
            short maxX = 0;
            short maxY = 0;
            byte[] HomeXY;
            byte[] MinXMinY;
            byte[] MinXMaxY;
            byte[] MaxXMaxY;
            byte[] MaxXMinY;
            short zero = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Head_Range_Test", "START");

                maxX = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_X", 9650, Constants.PARAMS_INI_FILE);
                maxY = (short)Util.GetPrivateProfileValueInt("MARK", "MAX_Y", 3150, Constants.PARAMS_INI_FILE);

                HomeXY = Encoding.UTF8.GetBytes(zero.ToString("X4") + maxY.ToString("X4"));
                MinXMinY = Encoding.UTF8.GetBytes(zero.ToString("X4") + zero.ToString("X4"));
                MinXMaxY = Encoding.UTF8.GetBytes(zero.ToString("X4") + maxY.ToString("X4"));
                MaxXMinY = Encoding.UTF8.GetBytes(maxX.ToString("X4") + zero.ToString("X4"));
                MaxXMaxY = Encoding.UTF8.GetBytes(maxX.ToString("X4") + maxY.ToString("X4"));

                retval = await GoHome(HomeXY, HomeXY.Length);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(HomeXY) return : {0}", retval.execResult));
                    return retval;
                }

                retval = await GoHome(MaxXMaxY, MaxXMaxY.Length);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MaxXMaxY) return : {0}", retval.execResult));
                    return retval;
                }

                retval = await GoHome(MaxXMinY, MaxXMinY.Length);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MaxXMinY) return : {0}", retval.execResult));
                    return retval;
                }

                retval = await GoHome(MinXMinY, MinXMinY.Length);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MinXMinY) return : {0}", retval.execResult));
                    return retval;
                }

                retval = await GoHome(MinXMaxY, MinXMaxY.Length);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MinXMaxY) return : {0}", retval.execResult));
                    return retval;
                }

                retval = await GoHome(MaxXMinY, MaxXMinY.Length) ;
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MaxXMinY) return : {0}", retval.execResult));
                    return retval;
                }

                retval = await GoHome(MinXMaxY, MinXMaxY.Length);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MinXMaxY) return : {0}", retval.execResult));
                    return retval;
                }
            }

            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> READY_IO(bool OnOff)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sendstring = "";
            byte[] sbuff;
            short zero = 0;
            short one = 1;
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "READY_IO", "START");

                if (OnOff == true)
                    sendstring = zero.ToString("X4") + one.ToString("X4");// "00000001";
                else
                    sendstring = zero.ToString("X4") + zero.ToString("X4");//"00000000";
                sbuff = Encoding.UTF8.GetBytes(sendstring);
                retval = await TestSolFet(sbuff, sbuff.Length);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "READY_IO", string.Format("GoHome(MinXMaxY) return : {0}", retval.execResult));
                    return retval;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "READY_IO", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> DONE_IO(bool OnOff)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string sendstring = "";
            byte[] sbuff;
            short zero = 0;
            short one = 1;
            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "READY_IO", "START");

                if (OnOff == true)
                    sendstring = one.ToString("X4") + one.ToString("X4");//"00010001";
                else
                    sendstring = one.ToString("X4") + zero.ToString("X4");//"00010000";
                sbuff = Encoding.UTF8.GetBytes(sendstring);
                retval = await TestSolFet (sbuff, sbuff.Length);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "READY_IO", string.Format("GoHome(MinXMaxY) return : {0}", retval.execResult));
                    return retval;
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}::{1}()  {2}", "MarkController", "READY_IO", string.Format("EXCEPTION - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message));
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        //public byte GetBCC(byte[] inputStream)
        //{
        //    byte bcc = 0;

        //    if (inputStream != null && inputStream.Length > 0)
        //    {

        //        for (int i = 0; i < inputStream.Length; i++)
        //        {
        //            bcc ^= inputStream[i];
        //        }
        //    }

        //    return bcc;
        //}
        //private void GetPatternData(string name, ref Pattern pattern)
        //{
        //    string patternfile = "Parameter\\Pattern_" + name + ".ini";
        //    string value = "";
        //    try
        //    {
        //        Util.GetPrivateProfileValue("FONT", "NANE", "5X7", ref pattern.fontName, patternfile);

        //        pattern.strikeCount = (short)Util.GetPrivateProfileValueInt("FONT", "strikecount", 1, patternfile);
        //        pattern.initSpeed4NoLoad = (short)Util.GetPrivateProfileValueInt("NOLOAD", "INITIALSPEED", 1, patternfile);
        //        pattern.targetSpeed4NoLoad = (short)Util.GetPrivateProfileValueInt("NOLOAD", "TARGETSPEED", 1, patternfile);
        //        pattern.accelSpeed4NoLoad = (short)Util.GetPrivateProfileValueInt("NOLOAD", "ACCELERATION", 1, patternfile);
        //        pattern.decelSpeed4NoLoad = (short)Util.GetPrivateProfileValueInt("NOLOAD", "DECELERATION", 1, patternfile);
        //        pattern.initSpeed4Load = (short)Util.GetPrivateProfileValueInt("LOAD", "INITIALSPEED", 1, patternfile);
        //        pattern.targetSpeed4Load = (short)Util.GetPrivateProfileValueInt("LOAD", "TARGETSPEED", 1, patternfile);
        //        pattern.initSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "INITIALSPEED", 1, patternfile);
        //        pattern.targetSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "TARGETSPEED", 1, patternfile);
        //        pattern.accelSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "ACCELERATION", 1, patternfile);
        //        pattern.decelSpeed4Scan = (short)Util.GetPrivateProfileValueInt("SCAN", "DECELERATION", 1, patternfile);
        //        pattern.solOnTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "SOLONTIME", 1, patternfile);
        //        pattern.solOffTime = (short)Util.GetPrivateProfileValueInt("SOLENOID", "SOLOFFTIME", 1, patternfile);

        //        Util.GetPrivateProfileValue("FONT", "STARTPOSX", "", ref value, patternfile);
        //        double.TryParse(value, out pattern.startX);
        //        Util.GetPrivateProfileValue("FONT", "STARTPOSY", "", ref value, patternfile);
        //        double.TryParse(value, out pattern.startY);
        //        Util.GetPrivateProfileValue("FONT", "WIDTH", "", ref value, patternfile);
        //        double.TryParse(value, out pattern.width);
        //        Util.GetPrivateProfileValue("FONT", "HEIGHT", "", ref value, patternfile);
        //        double.TryParse(value, out pattern.height);
        //        Util.GetPrivateProfileValue("FONT", "PITCH", "", ref value, patternfile);
        //        double.TryParse(value, out pattern.pitch);
        //        Util.GetPrivateProfileValue("FONT", "THICKNESS", "", ref value, patternfile);
        //        double.TryParse(value, out pattern.thickness);
        //        Util.GetPrivateProfileValue("FONT", "ROTATEANGLE", "", ref value, patternfile);
        //        double.TryParse(value, out pattern.rotateAngle);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

    }

    //public class MARK_COMMAND_RESULT : ICloneable
    //{
    //    public int result;
    //    public string response;

    //    public MARK_COMMAND_RESULT()
    //    {
    //        result = 0;
    //        response = "";
    //    }

    //    public object Clone()
    //    {
    //        MARK_COMMAND_RESULT ret = new MARK_COMMAND_RESULT();
    //        ret.result = this.result;
    //        ret.response = response;
    //        return ret;
    //    }
    //}


    //public class MarkResponseData : ICloneable
    //{
    //    public int result;

    //    public string response;

    //    public MARK_COMMAND_RESULT()
    //    {
    //        result = 0;
    //        response = "";
    //    }

    //    public object Clone()
    //    {
    //        MARK_COMMAND_RESULT ret = new MARK_COMMAND_RESULT();
    //        ret.result = this.result;
    //        ret.response = response;
    //        return ret;
    //    }
    //}

}
