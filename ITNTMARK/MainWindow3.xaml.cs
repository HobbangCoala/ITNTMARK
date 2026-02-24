using ITNTCOMMON;
using ITNTUTIL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    partial class MainWindow
    {
        public byte byLaserStartFlag = 0;

        public List<int> iLaserPowerList = new List<int>();
        public List<int> iLPMPowerList = new List<int>();

        public async Task<CheckAreaData> Range_Test(string vin, PatternValueEx pattern, bool mark6 = false)   // Plating  by TM SHIN $$$
        {
            string className = "MainWindow";
            string funcName = "Range_Test";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            distanceSensorData snsdata = new distanceSensorData();
            CheckAreaData chkdata = new CheckAreaData();

            int vinLength = 17;
            double totWidth = 0;

            Vector3D SP0 = new Vector3D();
            Vector3D SP1 = new Vector3D();
            Vector3D CP = new Vector3D();

            Vector3D PointLU = new Vector3D();
            Vector3D PointLD = new Vector3D();
            Vector3D PointRU = new Vector3D();

            Vector3D[] vCheckPos = new Vector3D[7];
            double[] HeightVal = new double[7];

            Vector3D vector3 = new Vector3D();

            double HeightCT0 = 0;
            string value = "";
            string log = "";

            List<Vector3D> planePoints = new List<Vector3D>();

            short gMinX = 0;
            short gMaxX = 0;
            short gMinY = 0;
            short gMaxY = 0;
            short? CenterZ = null;
            double dSlope = 0;
            string sCurrentFunc = "PLATE CHECK";
            CheckAreaData chkdata2 = new CheckAreaData();
            byte SlopeErrorFlag = 0;

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                ShowLog(className, funcName, 0, "[PLATE CHECK] START");

                Util.GetPrivateProfileValue("CONFIG", "SKIPCHECKPLANE", "0", ref value, Constants.SCANNER_INI_FILE);
                if (value != "0")
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SKIP CHECK PLANE", Thread.CurrentThread.ManagedThreadId);
                    chkdata.NormalDir.X = 0;
                    chkdata.NormalDir.Y = 0;
                    chkdata.NormalDir.Z = 1;
                    chkdata.bReady = true;
                    chkdata.execResult = 0;
                    ShowLog(className, funcName, 0, "[PLATE CHECK] SKIP");
                    return chkdata;
                }

                vinLength = vin.Length;
                if (vinLength <= 0)
                {
                    log = "ERROR : VIN LENGTH <= 0 (" + vinLength.ToString() + ")";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = ErrorCodeConstant.ERROR_PARAM_INVALID;
                    chkdata.errorInfo.sErrorMessage = "VIN IS EMPTY";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_PARAM + Constants.ERROR_INVALID;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_DISTACE;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_DISTACE;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                    return chkdata;
                }

                totWidth = pattern.fontValue.pitch * (vinLength - 1) + pattern.fontValue.width;

                // SET Motor Speed
                m_currCMD = (byte)'L';
                retval = await MarkControll.LoadSpeed(m_currCMD, pattern.speedValue.initSpeed4Measure, pattern.speedValue.targetSpeed4Measure, pattern.speedValue.accelSpeed4Measure, pattern.speedValue.decelSpeed4Measure);
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - SET MOTOR SPEED(MEASURE) ERROR = " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = retval.execResult;
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + retval.execResult.ToString();
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    return chkdata;
                }


#if LASER_OFF
#else
                //-- Delete Aiming Beam ON During Plate Check 2025.09.20
                //// Laser Aiming Beam ON
                //retval = await laserSource.SetExternalAimingBeamControll(0);
                //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                //{
                //    retval = await laserSource.SetExternalAimingBeamControll(0);
                //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                //        retval = await laserSource.SetExternalAimingBeamControll(0);
                //}
                //if (retval.execResult != 0)
                //{
                //    log = "CHECK PLATE FAIL - SET BEAM CONTROLL ERROR : " + retval.execResult.ToString();
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //    chkdata.execResult = retval.execResult;
                //    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                //    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (SetExternalAimingBeamControll) ERROR = " + retval.execResult.ToString();
                //    chkdata.errorInfo.sErrorFunc = sCurrentFunc;

                //    return chkdata;
                //}

                //retval = await laserSource.AimingBeamON();
                //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                //{
                //    retval = await laserSource.AimingBeamON();
                //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                //        retval = await laserSource.AimingBeamON();
                //}
                //if (retval.execResult != 0)
                //{
                //    log = "CHECK PLATE FAIL - BEAM ON ERROR : " + retval.execResult.ToString();
                //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                //    chkdata.execResult = retval.execResult;
                //    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                //    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (AimingBeamON) ERROR = " + retval.execResult.ToString();
                //    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                //    return chkdata;
                //}

                //ShowRectangle(System.Windows.Media.Brushes.Black, AimingLamp);
#endif
                planePoints.Clear();
                currMarkInfo.senddata.Clear();

                // Absolute mm of Center Point
                CP = pattern.positionValue.center3DPos;
                SP0.X = totWidth / 2;
                SP0.Y = pattern.fontValue.height / 2;

                SP1 = CP - SP0;
                SP1.Z = CP.Z;

                // ABS mm
                double MinX = SP1.X;
                double MaxX = SP1.X + totWidth;
                double MinY = SP1.Y;
                double MaxY = SP1.Y + pattern.fontValue.height;

                // ABS BLU
                gMinX = (short)(MinX * pattern.headValue.stepLength + 0.5);
                gMaxX = (short)(MaxX * pattern.headValue.stepLength + 0.5);
                gMinY = (short)(MinY * pattern.headValue.stepLength + 0.5);
                gMaxY = (short)(MaxY * pattern.headValue.stepLength + 0.5);

                // ABS mm
                double CX = (MaxX + MinX) / 2.0;
                double CY = (MaxY + MinY) / 2.0;

                // ABS BLU
                short tCX = (short)(CX * pattern.headValue.stepLength + 0.5);
                short tCY = (short)(CY * pattern.headValue.stepLength + 0.5);
                //CenterX = tCX;
                //CenterY = tCY;
                //CenterZ = (short)(SP1.Z * pattern.headValue.stepLength + 0.5);
                CenterZ = (short)(CP.Z * pattern.headValue.stepLength + 0.5);

                //short Parking_Z = (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5);

                //vector3.X = tCX;
                //vector3.Y = tCY;
                //vector3.Z = Parking_Z;
                vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));

#if MY_DEBUGGING
                snsdata.sensoroffset = -28;
                snsdata.sensorshift = 0;
                snsdata.execResult = 0;
#else
                snsdata = await GetMeasureLength(vector3, 0, 1);
#endif
                if (snsdata.execResult != 0)
                {
                    //retval.execResult = snsdata.execResult;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = snsdata.execResult;
                    chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                    //chkdata.sErrorMessage = "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString();

                    //ITNTErrorCode();
                    //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength", snsdata.execResult.ToString());

                    return chkdata;
                }

                chkdata.checkdistance[0] = snsdata.rawdistance;
                ShowLabelData(snsdata.rawdistance.ToString("F3"), lblDispHeightValue);
                ShowLabelData(snsdata.sensoroffset.ToString("F3"), lblDispHeightCosine);

                double ShiftCT = snsdata.sensorshift;
                double HeightCT = snsdata.sensoroffset;

                if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                {
                    vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
#if MY_DEBUGGING
                    snsdata.sensoroffset = -90;
                    snsdata.sensorshift = 0;
                    snsdata.execResult = 0;
#else
                    snsdata = await GetMeasureLength(vector3, 0, 1);
#endif
                    if (snsdata.execResult != 0)
                    {
                        //retval.execResult = snsdata.execResult;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        chkdata.execResult = snsdata.execResult;
                        chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                        //chkdata.sErrorMessage = "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString();

                        //ITNTErrorCode();
                        //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength", snsdata.execResult.ToString());

                        return chkdata;
                    }

                    chkdata.checkdistance[0] = snsdata.rawdistance;
                    ShowLabelData(snsdata.rawdistance.ToString("F3"), lblDispHeightValue);
                    ShowLabelData(snsdata.sensoroffset.ToString("F3"), lblDispHeightCosine);

                    ShiftCT = snsdata.sensorshift;
                    HeightCT = snsdata.sensoroffset;

                    if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                    {

                        vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
#if MY_DEBUGGING
                        snsdata.sensoroffset = -90;
                        snsdata.sensorshift = 0;
                        snsdata.execResult = 0;
#else
                        snsdata = await GetMeasureLength(vector3, 0, 1);
#endif
                        if (snsdata.execResult != 0)
                        {
                            //retval.execResult = snsdata.execResult;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                            //chkdata.sErrorMessage = "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString();

                            //ITNTErrorCode();
                            //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength", snsdata.execResult.ToString());

                            return chkdata;
                        }

                        chkdata.checkdistance[0] = snsdata.rawdistance;
                        ShowLabelData(snsdata.rawdistance.ToString("F3"), lblDispHeightValue);
                        ShowLabelData(snsdata.sensoroffset.ToString("F3"), lblDispHeightCosine);

                        ShiftCT = snsdata.sensorshift;
                        HeightCT = snsdata.sensoroffset;

                        if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                        {
                            chkdata.ErrorDistanceSensor = true;

                            ShowLabelData(HeightCT.ToString("0.000"), lblDispCenXCenY, System.Windows.Media.Brushes.Red, null, null);
                            ShowLabelData("", lblDispMinXMaxY);
                            ShowLabelData("", lblDispMinXMinY);
                            ShowLabelData("", lblDispMaxXMaxY);
                            ShowLabelData("", lblDispMaxXMinY);
                            ShowLabelData("", lblDispCenXMaxY);
                            ShowLabelData("", lblDispCenXMinY);

                            //retval.execResult = -2;
                            log = "CENTER Z RANGE(" + HeightCT.ToString("F4") + ") IS OVER THAN STANDARD(" + pattern.positionValue.checkDistanceHeight.ToString("F4") + ")";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR;
                            chkdata.errorInfo.sErrorMessage = "DISTANCE FROM MARKING HEAD TO PLATE IS TOO FAR. (" + Math.Abs(HeightCT).ToString("F3") + " > " + pattern.positionValue.checkDistanceHeight.ToString("F3") + ")";
                            chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                            chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_Z_HEIGHT;

                            chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                            chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                            chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                            chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                            chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;
                            return chkdata;
                        }
                    }
                }

                if (pattern.headValue.sensorPosition == 0)  // RIGHT
                {
                    gMinX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
                        gMinX = pattern.headValue.stepLength;                                   // && 7

                    gMaxX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
                        gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
                }
                else                                       // LEFT
                {
                    gMinX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
                        gMinX = pattern.headValue.stepLength;
                    gMaxX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
                        gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
                }

                // Sensor Shift compensation
                ShowLabelData(HeightCT.ToString("0.000"), lblDispCenXCenY, System.Windows.Media.Brushes.Black, null, null);
                ShowLabelData("", lblDispMinXMaxY);
                ShowLabelData("", lblDispMinXMinY);
                ShowLabelData("", lblDispMaxXMaxY);
                ShowLabelData("", lblDispMaxXMinY);
                ShowLabelData("", lblDispCenXMaxY);
                ShowLabelData("", lblDispCenXMinY);

                tCX = (short)(((double)gMaxX + (double)gMinX) / 2.0 + 0.5);
                tCY = (short)(((double)gMaxY + (double)gMinY) / 2.0 + 0.5);

                double[] vCheckPosX = new double[] { gMinX, gMinX, tCX, gMaxX, gMaxX, tCX, tCX };
                double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMinY, gMaxY, gMaxY, tCY };

                Vector3D[] vAddPos = new Vector3D[7];

                double left = MinX - CP.X;
                double centerX = (MaxX + MinX) / 2.0 - CP.X;
                double right = MaxX - CP.X;
                double up = MaxY - CP.Y;
                double centerY = (MaxY + MinY) / 2.0 - CP.Y;
                double down = MinY - CP.Y;

                double[] vAddPosX = new double[] { left, left, centerX, right, right, centerX, centerX };
                double[] vAddPosY = new double[] { up, down, down, down, up, up, centerY };
                string[] sPosition = new string[] { "LU", "LD", "CD", "RD", "RU", "CU", "CC" };
                double[] sDebugVal = new double[] { 26.1, 26.9, 28.0, 29.7, 30.4, 28.1, 28.0 };

                Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispCenXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXMaxY, lblDispCenXCenY };
                for (int i = 0; i < 7; i++)
                {
                    vCheckPos[i].X = vCheckPosX[i];
                    vCheckPos[i].Y = vCheckPosY[i];
                    //vCheckPos[i].Z = Parking_Z;
                    vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

#if MY_DEBUGGING
                    snsdata.sensorshift = 0;
                    snsdata.execResult = 0;
                    snsdata.sensoroffset = sDebugVal[i];
#else
                    snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);   // 4 !!! TM SHIN
#endif

                    if (snsdata.execResult != 0)
                    {
                        //retval.execResult = snsdata.execResult;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(+" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        chkdata.execResult = snsdata.execResult;
                        chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                        //ITNTErrorCode();
                        //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                        return chkdata;
                    }

                    vAddPos[i].X = vAddPosX[i];
                    vAddPos[i].Y = vAddPosY[i];
                    vAddPos[i].Z = snsdata.sensoroffset;
                    HeightVal[i] = snsdata.sensoroffset;
                    chkdata.checkdistance[i + 1] = snsdata.rawdistance;
                    if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                    {
                        vCheckPos[i].X = vCheckPosX[i];
                        vCheckPos[i].Y = vCheckPosY[i];
                        //vCheckPos[i].Z = Parking_Z;
                        vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                        snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);   // 4 !!! TM SHIN
                        if (snsdata.execResult != 0)
                        {
                            //retval.execResult = snsdata.execResult;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(+" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                            //ITNTErrorCode();
                            //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                            return chkdata;
                        }

                        vAddPos[i].X = vAddPosX[i];
                        vAddPos[i].Y = vAddPosY[i];
                        vAddPos[i].Z = snsdata.sensoroffset;
                        HeightVal[i] = snsdata.sensoroffset;
                        chkdata.checkdistance[i + 1] = snsdata.rawdistance;
                        if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        {
                            vCheckPos[i].X = vCheckPosX[i];
                            vCheckPos[i].Y = vCheckPosY[i];
                            //vCheckPos[i].Z = Parking_Z;
                            vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                            snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);   // 4 !!! TM SHIN
                            if (snsdata.execResult != 0)
                            {
                                //retval.execResult = snsdata.execResult;
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(+" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                                chkdata.execResult = snsdata.execResult;
                                chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                                //ITNTErrorCode();
                                //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                                return chkdata;
                            }

                            vAddPos[i].X = vAddPosX[i];
                            vAddPos[i].Y = vAddPosY[i];
                            vAddPos[i].Z = snsdata.sensoroffset;
                            HeightVal[i] = snsdata.sensoroffset;
                            chkdata.checkdistance[i + 1] = snsdata.rawdistance;
                            if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                            {
                                chkdata.errorInfo.sErrorMessage = "DISTANCE FROM MARKING HEAD TO PLATE IS TOO FAR. (" + Math.Abs(HeightVal[i]).ToString("F3") + ")";
                                chkdata.ErrorDistanceSensor = true;
                                //retval.sErrorMessage = "";
                            }
                        }
                    }

                    planePoints.Add(vAddPos[i]);
                    ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
                    HeightCT = HeightVal[i];    // TM SHIN
                }

                //HeightCT = HeightVal[6];    // TM SHIN
#if LASER_OFF
#else

                retval = await laserSource.AimingBeamOFF();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.AimingBeamOFF();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.AimingBeamOFF();
                }
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = retval.execResult;

                    //ITNTErrorCode();
                    //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - AimingBeamOFF", chkdata.execResult.ToString());
                    //ShowLog(className, funcName, 2, log);
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();
                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (AimingBeamOFF) ERROR = " + retval.execResult.ToString(); ;
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;

                    return chkdata;
                }
#endif
                ShowRectangle(System.Windows.Media.Brushes.Black, AimingLamp);

                if (chkdata.ErrorDistanceSensor)
                {
                    Vector3D TmpPoint = new Vector3D();
                    planePoints.Clear();

                    HeightCT0 = HeightCT;
                    HeightCT = pattern.positionValue.checkDistanceHeight;

                    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = CY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over : " + HeightCT.ToString("0.000"), Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR;
                    //chkdata.errorInfo.sErrorMessage = "DISTANCE FROM MARKING HEAD TO PLATE IS TOO FAR.";
                    //chkdata.errorInfo.sErrorMessage = "CHECK POINT Z RANGE(" + HeightCT.ToString("F4") + ") IS OVER THAN STANDARD(" + pattern.positionValue.checkDistanceHeight.ToString("F4") + ")";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_Z_HEIGHT;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                    return chkdata;
                }

                HeightCT = HeightVal[6];    // TM SHIN

                //CenterZ += (short)(HeightCT * pattern.headValue.stepLength + 0.5);

                //// REL mm at 4 Corners, CD
                PointLU.X = SP1.X - CP.X;
                PointLU.Y = SP1.Y - CP.Y + pattern.fontValue.height;
                PointLD.Y = SP1.Y - CP.Y;
                PointRU.X = SP1.X - CP.X + totWidth;
                ////
                Vector3D Sum = new Vector3D();
                foreach (var mPoint in planePoints)
                {
                    Sum.X += mPoint.X; Sum.Y += mPoint.Y; Sum.Z += mPoint.Z;
                }
                Vector3D Centroid = new Vector3D();
                Centroid.X = Sum.X / planePoints.Count;
                Centroid.Y = Sum.Y / planePoints.Count;
                Centroid.Z = Sum.Z / planePoints.Count;
                double xx, xy, xz, yy, yz, zz;
                xx = xy = xz = yy = yz = zz = 0.0;
                foreach (var mPoint in planePoints)
                {
                    xx += (mPoint.X - Centroid.X) * (mPoint.X - Centroid.X);
                    xy += (mPoint.X - Centroid.X) * (mPoint.Y - Centroid.Y);
                    xz += (mPoint.X - Centroid.X) * (mPoint.Z - Centroid.Z);
                    yy += (mPoint.Y - Centroid.Y) * (mPoint.Y - Centroid.Y);
                    yz += (mPoint.Y - Centroid.Y) * (mPoint.Z - Centroid.Z);
                    zz += (mPoint.Z - Centroid.Z) * (mPoint.Z - Centroid.Z);
                }
                chkdata.NormalDir.X = xy * yz - xz * yy;
                chkdata.NormalDir.Y = xy * xz - yz * xx;
                chkdata.NormalDir.Z = xx * yy - xy * xy;

                double Ds = chkdata.NormalDir.X * Centroid.X + chkdata.NormalDir.Y * Centroid.Y + chkdata.NormalDir.Z * Centroid.Z;

                double PlaneLU = GetZfromPlane(PointLU.X, PointLU.Y);
                double PlaneLD = GetZfromPlane(PointLU.X, PointLD.Y);
                double PlaneRU = GetZfromPlane(PointRU.X, PointLU.Y);
                double PlaneRD = GetZfromPlane(PointRU.X, PointLD.Y);
                double PlaneCU = GetZfromPlane(0, PointLU.Y);
                double PlaneCD = GetZfromPlane(0, PointLD.Y);
                chkdata.PlaneCenterZ = GetZfromPlane(0, 0);

                CenterZ = (short)((CP.Z + pattern.headValue.distance0Position + chkdata.PlaneCenterZ) * (double)pattern.headValue.stepLength + 0.5);              // BLU // && 8

                double PdiffLU, PdiffLD, PdiffRD, PdiffRU, PdiffCU, PdiffCD;
                PdiffLU = PlaneLU - chkdata.PlaneCenterZ;
                PdiffLD = PlaneLD - chkdata.PlaneCenterZ;
                PdiffRU = PlaneRU - chkdata.PlaneCenterZ;
                PdiffRD = PlaneRD - chkdata.PlaneCenterZ;
                PdiffCU = PlaneCU - chkdata.PlaneCenterZ;
                PdiffCD = PlaneCD - chkdata.PlaneCenterZ;
                double PminDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
                double PmaxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
                double PdiffDiff = Math.Abs(PmaxDiff - PminDiff);

                Util.GetPrivateProfileValue("OPTION", "SLOPE", "1.0", ref value, Constants.MARKING_INI_FILE);
                double.TryParse(value, out dSlope);

                if (PdiffDiff > dSlope)
                {
                    log = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
                    // Error handling required!!
                    //chkdata.sErrorMessage = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
                    //log = "PLATE SLOPE(" + PdiffDiff.ToString("F3") + ") IS OVER THAN STANDARD(" + dSlope.ToString("F3") + ")";
                    //ShowLog(className, funcName, 2, log);


                    //chkdata.errorInfo.sErrorFunc = sCurrentFunc;


                    chkdata.execResult = ErrorCodeConstant.ERROR_SLOPE_ERROR;
                    chkdata.errorInfo.sErrorMessage = "PLATE SLOPE(" + PdiffDiff.ToString("F2") + ") > TOLERANCE(" + dSlope.ToString("F2") + ")";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_SLOPE_BIG;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PLATE SLOPE(" + PdiffDiff.ToString("F2") + ") > TOLERANCE(" + dSlope.ToString("F2") + ")", Thread.CurrentThread.ManagedThreadId);

                    //return chkdata;
                    SlopeErrorFlag = 1;
                    chkdata2 = (CheckAreaData)chkdata.Clone();
                    chkdata2.bReady = true;
                }

                ShowLabelData(chkdata.PlaneCenterZ.ToString("0.000;-0.000;0.000"), lblDispCenXCenY, (chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.DarkGreen);
                ShowLabelData(PdiffLU.ToString("+ 0.000;- 0.000;0.000"), lblDispMinXMaxY, (PdiffLU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffLD.ToString("+ 0.000;- 0.000;0.000"), lblDispMinXMinY, (PdiffLD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffRU.ToString("+ 0.000;- 0.000;0.000"), lblDispMaxXMaxY, (PdiffRU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffRD.ToString("+ 0.000;- 0.000;0.000"), lblDispMaxXMinY, (PdiffRD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffCU.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMaxY, (PdiffCU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffCD.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMinY, (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);

                //ShowLabelData(PdiffCD.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMinY, (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);

                //Dispatcher.Invoke(new Action(delegate
                //{
                //    lblDispCenXCenY.Foreground = (chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.DarkGreen;
                //    lblDispCenXCenY.Content = chkdata.PlaneCenterZ.ToString("0.000;-0.000;0.000");

                //    lblDispMinXMaxY.Foreground = (PdiffLU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMinXMaxY.Content = PdiffLU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMinXMinY.Foreground = (PdiffLD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMinXMinY.Content = PdiffLD.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMaxXMaxY.Foreground = (PdiffRU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMaxXMaxY.Content = PdiffRU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMaxXMinY.Foreground = (PdiffRD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispMaxXMinY.Content = PdiffRD.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispCenXMaxY.Foreground = (PdiffCU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispCenXMaxY.Content = PdiffCU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispCenXMinY.Foreground = (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispCenXMinY.Content = PdiffCD.ToString("+ 0.000;- 0.000;0.000");
                //}
                //));

                int jc = (int)((PointLU.Y - PointLD.Y) + 0.5);
                int ic = (int)((PointRU.X - PointLU.X) + 0.5);
                byte[,] HC = new byte[jc, ic];

                double Yy = PointLU.Y;
                double Xx = PointLU.X;
                double Zz = 0;
                for (int r = 0; r < jc; r++)
                {
                    Xx = PointLU.X;
                    for (int c = 0; c < ic; c++)
                    {
                        Zz = (GetZfromPlane(Xx, Yy) - GetZfromPlane(0, 0)) * 200.0;
                        if (Zz > 127.0) Zz = 127.0;
                        if (Zz < -127.0) Zz = -127.0;
                        HC[r, c] = (byte)(Zz + 127.0);
                        Xx += 1.0;  // +1 mm;
                    }
                    Yy -= 1.0;      // -1 mm;
                }

                Dispatcher.Invoke(new Action(delegate
                {
                    PlateColoring(HC);
                }));

                chkdata.bReady = true;

                double GetZfromPlane(double x, double y)
                {
                    double pz = 0;
                    if (chkdata.NormalDir.Z != 0)
                        pz = Ds / chkdata.NormalDir.Z - chkdata.NormalDir.X / chkdata.NormalDir.Z * x - chkdata.NormalDir.Y / chkdata.NormalDir.Z * y;
                    else
                        pz = 0;
                    return pz;
                }
                if(SlopeErrorFlag == 1)
                    chkdata = (CheckAreaData)chkdata2.Clone();
                return chkdata;
            }
            catch (Exception ex)
            {
                chkdata.execResult = ex.HResult;
                chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                chkdata.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-chkdata.execResult).ToString("X2");

                chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                return chkdata;
            }
        }

        public async Task<ITNTResponseArgs> Start_TEXT2(string vin, PatternValueEx pattern)    // Making Fire/Clean data by TM SHIN
        {
            string className = "MainWindow";
            string funcName = "Start_TEXT2";

            Vector3D SP0 = new Vector3D();
            Vector3D SP = new Vector3D();
            Vector3D CP = new Vector3D();
            Vector3D VectorNormal = new Vector3D();
            //Vector3D VectorLuRd = new Vector3D();
            //Vector3D VectorRuRd = new Vector3D();
            Vector3D VectorRot = new Vector3D();

            //Vector3D[] Rev_Point = new Vector3D[1];
            List<Vector3D> Rev_Point = new List<Vector3D>();
            //int XF, YF, SF;
            //string[] TEMPF;
            double Step_W;
            double Step_H;

            ITNTResponseArgs retval = new ITNTResponseArgs();
            int vinLength = 0;
            string value = "";
            byte headType = 0;
            VinNoInfo vininfo = new VinNoInfo();
            List<List<FontDataClass>> fontData = new List<List<FontDataClass>>();

            double fontsizeX = 0;
            double fontsizeY = 0;
            double shiftVal = 0;
            string errCode = "";

            double fontsizeX2 = 0;
            double fontsizeY2 = 0;
            double shiftVal2 = 0;

            double cleanPosition = 0;
            double totWidth = 0;
            double R11, R12, R13, R21, R22, R23, R31, R32, R33;

            int i, j;
            int idx = 0;
            List<FontDataClass> lineData = new List<FontDataClass>();
            List<FontDataClass> lineDataClean = new List<FontDataClass>();

            Vector3D M1 = new Vector3D();
            Vector3D M = new Vector3D();
            string log = "";
            string sCurrentFunc = "CALCULATTE MARKING POSITION";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                vinLength = vin.Length;
                if (vinLength <= 0)
                {
                    retval.execResult = -1;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "VIN LENGTH IS INVALID (" + vinLength.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    retval.errorInfo.sErrorMessage = "VIN LENGTH IS INVALID (" + vinLength.ToString() + ")";


                    log = "ERROR : VIN LENGTH <= 0 (" + vinLength.ToString() + ")";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    retval.execResult = ErrorCodeConstant.ERROR_PARAM_INVALID;
                    retval.errorInfo.sErrorMessage = "VIN IS EMPTY";
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_PARAM + Constants.ERROR_INVALID;

                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    return retval;
                }

                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out headType);

                vininfo.vinNo = vin;
                vininfo.fontName = pattern.fontValue.fontName;
                vininfo.width = pattern.fontValue.width;
                vininfo.height = pattern.fontValue.height;
                vininfo.pitch = pattern.fontValue.pitch;
                vininfo.thickness = pattern.fontValue.thickness;
                retval = ImageProcessManager.GetFontDataEx(vininfo, headType, pattern.laserValue.density, 0, ref fontData, ref fontsizeX, ref fontsizeY, ref shiftVal, ref errCode);
                if (retval.execResult != 0)
                {
                    //if (retval.sErrorMessage.Length > 0)
                    //    log = retval.sErrorMessage;
                    //else
                    //    log = "GetFontDataEx ERROR = " + retval.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);
                    return retval;
                }

                m_currCMD = (byte)'S';
                retval = await MarkControll.SolOnOffTime(pattern.speedValue.solOnTime, pattern.speedValue.solOffTime);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SolOnOffTime ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    //log = "COMMUNICATION TO CONTROLLER (SolOnOffTime) ERROR = " + retval.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (SolOnOffTime) ERROR = " + retval.execResult.ToString();
                    return retval;
                }

                m_currCMD = (byte)'d';
                retval = await MarkControll.dwellTimeSet(pattern.speedValue.dwellTime);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "dwellTimeSet ERROR (" + retval.execResult.ToString() + ")", Thread.CurrentThread.ManagedThreadId);
                    //log = "COMMUNICATION TO CONTROLLER (dwellTimeSet) ERROR = " + retval.execResult.ToString();
                    //ShowLog(className, funcName, 2, log);
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (dwellTimeSet) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    //retval.errorInfo.sErrorFunc = sErrorFunc;
                    return retval;
                }

                if (currMarkInfo.checkdata.bReady == false)
                {
                    CheckAreaData chkdata = new CheckAreaData();
                    //#if LASER_YLR
                    chkdata = await Range_Test(currMarkInfo.currMarkData.mesData.markvin, currMarkInfo.currMarkData.pattern);
                    if (chkdata.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "RangeTest ERROR = " + chkdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = chkdata.execResult;
                        retval.errorInfo = (ErrorInfo)chkdata.errorInfo.Clone();

                        retval.errorInfo.sErrorMessage = "RANGE TEST ERROR = " + chkdata.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }
                    //#else
                    //                    chkdata = await GetMarkPosition(currMarkInfo.currMarkData.pattern);
                    //                    if (chkdata.execResult != 0)
                    //                    {
                    //                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMarkPosition ERROR = " + chkdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    //                        retval.execResult = chkdata.execResult;
                    //                        return retval;
                    //                    }
                    //#endif


                    //chkdata = await Range_Test(vin, pattern);
                    //if (chkdata.execResult != 0)
                    //{
                    //    return retval;
                    //}
                    //else
                    currMarkInfo.checkdata = (CheckAreaData)chkdata.Clone();
                }

                totWidth = (vinLength - 1) * pattern.fontValue.pitch + pattern.fontValue.width;

                // ABS mm at Center Point
                CP = pattern.positionValue.center3DPos;
                CP.Z += pattern.headValue.distance0Position;
                cleanPosition = pattern.laserValue.cleanPosition;
                cleanPosition += CP.Z;         // Relative Cleanning postion

                // Relative half size mm
                SP0.X = totWidth / 2;
                SP0.Y = pattern.fontValue.height / 2;
                SP0.Z = 0;
                SP = CP - SP0;
                SP.Z = 0.0d;

                VectorNormal = currMarkInfo.checkdata.NormalDir;
                VectorRot.X = -VectorNormal.Y;
                VectorRot.Y = VectorNormal.X;
                VectorRot.Z = 0.0;

                double sqXY = Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y);
                VectorRot.X /= sqXY; VectorRot.Y /= sqXY;
                // Angle between VectorNormal to Z Axis ==> Rodrigues' Matrix
                bool skipRot = false;
                double cosValue = VectorNormal.Z / Math.Sqrt(VectorNormal.X * VectorNormal.X + VectorNormal.Y * VectorNormal.Y + VectorNormal.Z * VectorNormal.Z);
                double sinValue = Math.Sqrt(1.0 - cosValue * cosValue);

                if (cosValue > 0.9999986111)
                {      // 0.1 mm difference between 60mm
                    skipRot = true;
                    R11 = R12 = R13 = R21 = R22 = R23 = R31 = R32 = R33 = 0.0;
                    R11 = R22 = R33 = 1.0;
                }
                else
                {
                    R11 = cosValue + VectorRot.X * VectorRot.X * (1.0 - cosValue);
                    R12 = VectorRot.X * VectorRot.Y * (1.0 - cosValue) - VectorRot.Z * sinValue;
                    R13 = VectorRot.X * VectorRot.Z * (1.0 - cosValue) + VectorRot.Y * sinValue;
                    R21 = VectorRot.Y * VectorRot.X * (1.0 - cosValue) + VectorRot.Z * sinValue;
                    R22 = cosValue + VectorRot.Y * VectorRot.Y * (1.0 - cosValue);
                    R23 = VectorRot.Y * VectorRot.Z * (1.0 - cosValue) - VectorRot.X * sinValue;
                    R31 = VectorRot.Z * VectorRot.X * (1.0 - cosValue) - VectorRot.Y * sinValue;
                    R32 = VectorRot.Z * VectorRot.Y * (1.0 - cosValue) + VectorRot.X * sinValue;
                    R33 = cosValue + VectorRot.Z * VectorRot.Z * (1.0 - cosValue);
                }
                /////
                Step_W = pattern.fontValue.width / (fontsizeX - 1.0);
                Step_H = pattern.fontValue.height / (fontsizeY - 1.0);

                Int32[] RlowerBounds = { 0, -1, 0 };
                Int32[] Rlengths = { vinLength + 1, (int)(fontsizeY + 2), (int)(fontsizeX) };

                Int32[] ClowerBounds = { 0, -1, -1 };
                Int32[] Clengths = { vinLength, (int)(fontsizeY + 2), (int)(fontsizeX + 2) };

                FontData4Send[,,] RasterData = (FontData4Send[,,])Array.CreateInstance(typeof(FontData4Send), Rlengths, RlowerBounds);   // BLU
                FontData4Send[,,] AllClrData = (FontData4Send[,,])Array.CreateInstance(typeof(FontData4Send), Clengths, ClowerBounds);   // BLU

                currMarkInfo.senddata.sendDataFire.Clear();
                currMarkInfo.senddata.sendDataClean.Clear();

                //List<Vector3D> recvPoint = new List<Vector3D>();
                ImageProcessManager.GetStartPointLinear(vinLength, CP, SP, pattern.fontValue.pitch, pattern.fontValue.rotateAngle, ref Rev_Point);

                Vector3D[] LeftRightSP = new Vector3D[2];
                LeftRightSP[0] = ImageProcessManager.Rotate_Point2(SP.X - pattern.headValue.rasterSP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);
                LeftRightSP[1] = ImageProcessManager.Rotate_Point2(SP.X + totWidth + pattern.headValue.rasterEP, SP.Y, CP.X, CP.Y, pattern.fontValue.rotateAngle);

                ImageProcessManager.GetFontDataOneEx(pattern.laserValue.charFull, pattern.fontValue.fontName, headType, pattern.laserValue.density, 0, ref lineDataClean, out fontsizeX2, out fontsizeY2, out shiftVal2, out errCode);

                for (i = 0; i < Rev_Point.Count; i++)
                {
                    if (vin.Substring(i, 1) != " ")      //Space Skip
                    {
                        lineData = fontData[i];
                        FontDataClass fd = new FontDataClass();
                        for (j = 0; j < lineData.Count; j++)
                        {
                            fd = (FontDataClass)lineData[j].Clone();
                            // ABS mm
                            M1.X = Rev_Point[i].X + fd.vector3d.X * Step_W;
                            // Font offset compensation
                            M1.Y = Rev_Point[i].Y + (fd.vector3d.Y - shiftVal) * Step_H;
                            M1.Z = SP.Z;

                            M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
                            M1.Z = SP.Z;

                            // TM SHIN
                            M1.X -= CP.X; M1.Y -= CP.Y;

                            M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

                            M.X += CP.X; M.Y += CP.Y;
                            double Mt = M.Z;
                            M.Z = (currMarkInfo.currMarkData.pattern.headValue.bySkipPlateCheck == 0) ? Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ : currMarkInfo.currMarkData.pattern.positionValue.teachingZHeight;
                            double Cz = 0.0;
                            Cz = (currMarkInfo.currMarkData.pattern.headValue.bySkipPlateCheck == 0) ? Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ : currMarkInfo.currMarkData.pattern.positionValue.teachingZHeight + cleanPosition;         // Clean Axis

                            // Change mm to BLU(Unit 0.01mm)
                            M.X *= pattern.headValue.stepLength; M.Y *= pattern.headValue.stepLength; M.Z *= pattern.headValue.stepLength;
                            Cz *= pattern.headValue.stepLength;

                            FontData4Send font4Send = new FontData4Send();
                            font4Send.cN = (byte)i; font4Send.fN = (byte)j;
                            font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); font4Send.mF = (byte)fd.Flag;
                            font4Send.mC = (UInt16)(Cz + 0.5);
                            font4Send.mI = (UInt16)(fd.vector3d.X + 0.5);

                            if (pattern.laserValue.density == 1)
                            {
                                font4Send.mF = 0;
                                RasterData[i, (int)(fd.vector3d.Y - shiftVal), (int)Math.Round(fd.vector3d.X)] = (FontData4Send)font4Send.Clone();
                            }
                            else
                            {
                                var m_font = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mZ.ToString("X4") + font4Send.mF.ToString("X4");
                                currMarkInfo.senddata.sendDataFire.Add(m_font);
                            }

                            idx++;
                        }

                        idx = 0;
                        FontDataClass fdc = new FontDataClass();

                        //  make all clear data : 0, char dot clear data : 1
                        if (pattern.laserValue.charClean == 0 || pattern.laserValue.charClean == 1)
                        {
                            for (j = 0; j < lineDataClean.Count - 1; j++)
                            {
                                fdc = (FontDataClass)lineDataClean[j].Clone();

                                // ABS mm
                                M1.X = Rev_Point[i].X + fdc.vector3d.X * Step_W;
                                // Font offset compensation
                                //M1.Y = Rev_Point[i].Y + ((double.Parse(xy_[1])) - SF) * Step_H;
                                M1.Y = Rev_Point[i].Y + (fdc.vector3d.Y - shiftVal2) * Step_H;
                                M1.Z = SP.Z;

                                //M1 = Mode_File.Rotate_Point(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, double.Parse(Angle));
                                M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, Rev_Point[i].X, Rev_Point[i].Y, pattern.fontValue.rotateAngle);
                                M1.Z = SP.Z;

                                // TM SHIN
                                M1.X -= CP.X; M1.Y -= CP.Y;

                                M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

                                M.X += CP.X; M.Y += CP.Y;
                                double Mt = M.Z;
                                M.Z = (currMarkInfo.currMarkData.pattern.headValue.bySkipPlateCheck == 0) ? Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ : currMarkInfo.currMarkData.pattern.positionValue.teachingZHeight;
                                double Cz = 0.0;
                                Cz = (currMarkInfo.currMarkData.pattern.headValue.bySkipPlateCheck == 0) ? Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ : currMarkInfo.currMarkData.pattern.positionValue.teachingZHeight + cleanPosition;         // Clean Axis

                                //M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;
                                //double Cz = 0.0;
                                //Cz = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Axis mm

                                // Change mm to BLU(Unit 0.01mm)
                                M.X *= pattern.headValue.stepLength; M.Y *= pattern.headValue.stepLength; M.Z *= pattern.headValue.stepLength;
                                Cz *= pattern.headValue.stepLength;

                                FontData4Send font4Send = new FontData4Send();
                                font4Send.cN = (byte)i; font4Send.fN = (byte)j;
                                font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); font4Send.mF = (byte)fdc.Flag;
                                font4Send.mC = (UInt16)(Cz + 0.5);
                                font4Send.mI = (UInt16)(fdc.vector3d.X + 0.5);

                                if (pattern.laserValue.density == 1)
                                {
                                    font4Send.mF = 0;
                                    AllClrData[i, (int)(fdc.vector3d.Y - shiftVal2), (int)Math.Round(fdc.vector3d.X)] = (FontData4Send)font4Send.Clone();
                                }
                                else
                                {
                                    var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mZ.ToString("X4") + font4Send.mF.ToString("X4");
                                    currMarkInfo.senddata.sendDataClean.Add(m_clean);
                                }
                                idx++;
                            }
                        }

                        FontDataClass fd2 = new FontDataClass();

                        if (pattern.laserValue.charClean == 1)  // dot by dot clear only
                        {
                            for (j = 0; j < lineData.Count - 1; j++)
                            {
                                fd2 = lineData[j];

                                FontData4Send font4Send = new FontData4Send();

                                font4Send = RasterData[i, (int)(fd2.vector3d.Y - shiftVal), (int)Math.Round(fd2.vector3d.X)];
                                if (pattern.laserValue.density == 1)
                                {
                                    font4Send.mF = 0;
                                    AllClrData[i, (int)(fd2.vector3d.Y - shiftVal), (int)Math.Round(fd2.vector3d.X)] = font4Send;
                                }
                                else
                                {
                                    var m_clean = font4Send.cN.ToString("X2") + font4Send.fN.ToString("X2") + font4Send.mX.ToString("X4") + font4Send.mY.ToString("X4") + font4Send.mC.ToString("X4") + font4Send.mF.ToString("X4");
                                    currMarkInfo.senddata.sendDataClean.Add(m_clean);
                                }

                                idx++;
                            }
                        }
                    }
                }

                //
                // Make firing/cleanning data
                //
                idx = 0;
                int ifontsizeX = (int)(fontsizeX + 0.5);
                int ifontsizeY = (int)(fontsizeY + 0.5);

                if (pattern.laserValue.density == 1)       // Dot Firing
                {
                    // Calculate No of Dot point
                    ushort[,] NoPoints = (ushort[,])Array.CreateInstance(typeof(ushort), new int[2] { ifontsizeY + 2, 1 }, new int[2] { -1, 0 });

                    for (int y = -1; y < ifontsizeY + 1; y++)
                    {
                        for (i = 0; i < vinLength; i++)
                        {
                            for (int x = 0; x < ifontsizeX; x++)
                            {
                                if (RasterData[i, y, x] != null) NoPoints[y, 0]++;         // Data Number of fire data
                            }
                        }
                    }


                    ////
                    Debug.WriteLine("");

                    //
                    // Make Jump/Start data
                    for (i = -1; i < ifontsizeY + 1; i++)
                    {
                        for (j = 0; j < 2; j++)         // Jump/Start XXXXX
                        {
                            M1.X = LeftRightSP[j].X;
                            M1.Y = LeftRightSP[j].Y + (double)i * Step_H;
                            M1.Z = SP.Z;

                            //M1 = Mode_File.Rotate_Point(M1.X, M1.Y, LeftRightSP[j].X, LeftRightSP[j].Y, double.Parse(Angle));
                            M1 = ImageProcessManager.Rotate_Point2(M1.X, M1.Y, LeftRightSP[j].X, LeftRightSP[j].Y, pattern.fontValue.rotateAngle);
                            M1.Z = SP.Z;

                            M1.X -= CP.X; M1.Y -= CP.Y;

                            M = (skipRot == true) ? M1 : getRodrigueRotation(M1);

                            M.X += CP.X; M.Y += CP.Y;
                            double Mt = M.Z;
                            M.Z = (currMarkInfo.currMarkData.pattern.headValue.bySkipPlateCheck == 0) ? Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ : currMarkInfo.currMarkData.pattern.positionValue.teachingZHeight;
                            double Cz = 0.0;
                            Cz = (currMarkInfo.currMarkData.pattern.headValue.bySkipPlateCheck == 0) ? Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ : currMarkInfo.currMarkData.pattern.positionValue.teachingZHeight + cleanPosition;         // Clean Axis

                            //M.Z = Mt + CP.Z + currMarkInfo.checkdata.PlaneCenterZ;       // Fire Z Axis
                            //double Cz = 0.0;
                            //Cz = Mt + cleanPosition + currMarkInfo.checkdata.PlaneCenterZ;         // Clean Z Axis
                            Debug.WriteLine(string.Format("J{0:D3}=>{1,7:F3},{2,7:F3},{3,7:F3}/{4,7:F3}", idx, M.X, M.Y, M.Z, Cz));

                            // Change to BLU(Unit 0.01mm)
                            M.X *= pattern.headValue.stepLength; M.Y *= pattern.headValue.stepLength; M.Z *= pattern.headValue.stepLength;
                            Cz *= pattern.headValue.stepLength;

                            FontData4Send font4Send = new FontData4Send();
                            font4Send.cN = (byte)i; font4Send.fN = (byte)j;
                            font4Send.mX = (UInt16)(M.X + 0.5); font4Send.mY = (UInt16)(M.Y + 0.5); font4Send.mZ = (UInt16)(M.Z + 0.5); //font4Send.mF = (byte)fd.Flag;
                            font4Send.mC = (UInt16)(Cz + 0.5);

                            RasterData[vinLength, i, j] = font4Send;
                            idx++;
                        }
                    }

                    idx = 0;
                    ushort[,] NoPointsC = (ushort[,])Array.CreateInstance(typeof(ushort), new int[2] { ifontsizeY + 2, 1 }, new int[2] { -1, 0 });
                    //
                    // Check Dot Pitching 0.6
                    if (pattern.laserValue.charClean == 1)
                    {
                        for (int y = 0; y < ifontsizeY; y++)
                            for (i = 0; i < vinLength; i++)
                                for (int x = 0; x < ifontsizeX; x++)
                                {
                                    if (checkDistance(AllClrData, i, y, x) == false)
                                    {
                                        if (AllClrData[i, y, x].mI == (double)x)
                                        {
                                            AllClrData[i, y, x] = null;
                                            Debug.WriteLine(string.Format("NULL=>V{0:D3},X{1:D3},Y{2:D3}", i, x, y));
                                        }
                                    }
                                }
                    }

                    for (int y = -1; y < ifontsizeY + 1; y++)
                        for (i = 0; i < vinLength; i++)
                            for (int x = -1; x < ifontsizeX + 1; x++)
                                if (AllClrData[i, y, x] != null) NoPointsC[y, 0]++;    // Data Number of clean data

                    //
                    //
                    //
                    if (pattern.headValue.spatterType == 0)  // 0 : Back
                    {
                        //
                        // Make Firing & cleanning Data for back spatter at 0 degree
                        if (pattern.fontValue.rotateAngle == 0.0)
                        {
                            // Fire & then Clean : Normal case
                            {
                                //
                                // Make firing Data for 0 degree, back spatter [B0D]
                                StringBuilder sb = new StringBuilder();
                                bool DirRight = true;
                                for (int y = 0; y < ifontsizeY; y++)
                                {
                                    if (DirRight == true)
                                    {   // Fire Data string
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
                                        for (i = 0; i < vinLength; i++)
                                            for (int x = 0; x < ifontsizeX; x++)
                                                if (RasterData[i, y, x] != null)
                                                    sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                        //Mode_File.SendData.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    else
                                    {   // Fire Data string
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
                                        for (i = vinLength - 1; i >= 0; i--)
                                            for (int x = ifontsizeX - 1; x >= 0; x--)
                                                if (RasterData[i, y, x] != null)
                                                    sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                        //Mode_File.SendData.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    DirRight = !DirRight;
                                }

                                //
                                //  Make Cleaning Data for 0 degree, back spatter [B0C]
                                sb = new StringBuilder();
                                ushort x2 = 0, y2 = 0, c2 = 0;

                                //Mode_File.TwoLineDisplay = false;
                                currMarkInfo.checkdata.TwoLineDisplay = false;
                                for (int y = ifontsizeY; y >= -1; y--)
                                {
                                    if (NoPointsC[y, 0] > 0)
                                    {
                                        if (DirRight == false)
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                            for (i = vinLength - 1; i >= 0; i--)
                                                for (int x = ifontsizeX; x >= -1; x--)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                    }
                                                }
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        else
                                        {
                                            x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                            c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                            x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                            y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                            c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                            sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                            for (i = 0; i < vinLength; i++)
                                                for (int x = -1; x < ifontsizeX + 1; x++)
                                                {
                                                    if (AllClrData[i, y, x] != null)
                                                    {
                                                        sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                    }
                                                }
                                            //Mode_File.SendClean.Add(sb.ToString());
                                            currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                            Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                            sb.Length = 0;
                                        }
                                        DirRight = !DirRight;
                                    }   // NoPointsC
                                    else
                                    {
                                        //Mode_File.TwoLineDisplay = true;
                                        currMarkInfo.checkdata.TwoLineDisplay = true;
                                    }
                                }

                            } // CombineFireClean if-else
                        }
                        else
                        {
                            // 
                            // Make firing data at 180 degree for back spatter [B180D]
                            StringBuilder sb = new StringBuilder();
                            bool DirRight = true;
                            for (int y = ifontsizeY - 1; y >= 0; y--)
                            {
                                if (DirRight == false)
                                {   // Fire Data string
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
                                    for (i = vinLength - 1; i >= 0; i--)
                                        for (int x = ifontsizeX - 1; x >= 0; x--)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                else
                                {   // Fire Data string
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
                                    for (i = 0; i < vinLength; i++)
                                        for (int x = 0; x < ifontsizeX; x++)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                DirRight = !DirRight;
                            }

                            //
                            //  Make Cleaning Data for 180 degree, back spatter [B180C]
                            sb = new StringBuilder();
                            ushort x2 = 0, y2 = 0, c2 = 0;
                            currMarkInfo.checkdata.TwoLineDisplay = false;
                            for (int y = -1; y < ifontsizeY + 1; y++)
                            {
                                if (NoPointsC[y, 0] > 0)
                                {
                                    if (DirRight == false)
                                    {
                                        x2 = (ushort)RasterData[vinLength, y, 1].mX;
                                        y2 = (ushort)RasterData[vinLength, y, 1].mY;
                                        c2 = (ushort)RasterData[vinLength, y, 1].mC;
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)RasterData[vinLength, y, 0].mX;
                                        y2 = (ushort)RasterData[vinLength, y, 0].mY;
                                        c2 = (ushort)RasterData[vinLength, y, 0].mC;
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = vinLength - 1; i >= 0; i--)
                                            for (int x = ifontsizeX; x >= -1; x--)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    else
                                    {
                                        x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = 0; i < vinLength; i++)
                                            for (int x = -1; x < ifontsizeX + 1; x++)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    DirRight = !DirRight;
                                } //
                                else
                                {
                                    currMarkInfo.checkdata.TwoLineDisplay = true;
                                }
                            }

                        }   // Back spatter
                    }
                    else    // Front spatter
                    {
                        //
                        // Make Firing & cleanning Data for front spatter at 0 degree
                        if (pattern.fontValue.rotateAngle == 0.0)
                        {
                            //
                            // Make firing Data for 0 degree, front spatter [F0D]
                            StringBuilder sb = new StringBuilder();
                            bool DirRight = true;
                            for (int y = ifontsizeY - 1; y >= 0; y--)
                            {
                                if (DirRight == true)
                                {   // Fire Data string for even line ->
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
                                    for (i = 0; i < vinLength; i++)
                                        for (int x = 0; x < ifontsizeX; x++)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                else
                                {   // Fire Data string for odd line <-
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
                                    for (i = vinLength - 1; i >= 0; i--)
                                        for (int x = ifontsizeX - 1; x >= 0; x--)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                DirRight = !DirRight;
                            }

                            //
                            //  Make Cleaning Data for 0 degree, front spatter [F0C]
                            sb = new StringBuilder();
                            ushort x2 = 0, y2 = 0, c2 = 0;

                            currMarkInfo.checkdata.TwoLineDisplay = false;
                            for (int y = -1; y < ifontsizeY + 1; y++)
                            {
                                if (NoPointsC[y, 0] > 0)
                                {
                                    if (DirRight == false)
                                    {
                                        x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = vinLength - 1; i >= 0; i--)
                                            for (int x = ifontsizeX; x >= -1; x--)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    else
                                    {
                                        x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = 0; i < vinLength; i++)
                                            for (int x = -1; x < ifontsizeX + 1; x++)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    DirRight = !DirRight;
                                }
                                else
                                {
                                    currMarkInfo.checkdata.TwoLineDisplay = true;
                                }
                            }
                        }
                        else
                        {
                            // 
                            // Make firing data at 180 degree for front spatter [F180D]
                            StringBuilder sb = new StringBuilder();
                            bool DirRight = true;
                            for (int y = 0; y < ifontsizeY; y++)
                            {
                                if (DirRight == false) // odd line <-
                                {   // Fire Data string
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, NoPoints[y, 0]);
                                    for (i = vinLength - 1; i >= 0; i--)
                                        for (int x = ifontsizeX - 1; x >= 0; x--)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                else
                                {   // Fire Data string even line ->
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', RasterData[vinLength, y, 0].mX, RasterData[vinLength, y, 0].mY, RasterData[vinLength, y, 0].mZ, (ushort)y);
                                    sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', RasterData[vinLength, y, 1].mX, RasterData[vinLength, y, 1].mY, RasterData[vinLength, y, 1].mZ, NoPoints[y, 0]);
                                    for (i = 0; i < vinLength; i++)
                                        for (int x = 0; x < ifontsizeX; x++)
                                            if (RasterData[i, y, x] != null)
                                                sb.AppendFormat("{0:X4}", RasterData[i, y, x].mX);
                                    //Mode_File.SendData.Add(sb.ToString());
                                    currMarkInfo.senddata.sendDataFire.Add(sb.ToString());
                                    Debug.WriteLine(y.ToString("00") + "F" + NoPoints[y, 0].ToString("000") + ":" + sb.ToString());
                                    sb.Length = 0;
                                }
                                DirRight = !DirRight;
                            }

                            //
                            //  Make Cleaning Data for 180 degree, front spatter [F180C]
                            sb = new StringBuilder();
                            ushort x2 = 0, y2 = 0, c2 = 0;

                            currMarkInfo.checkdata.TwoLineDisplay = false;
                            for (int y = ifontsizeY; y >= -1; y--)
                            {
                                if (NoPointsC[y, 0] > 0)
                                {
                                    if (DirRight == true)
                                    {
                                        x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = 0; i < vinLength; i++)
                                            for (int x = -1; x < ifontsizeX + 1; x++)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    else
                                    {
                                        x2 = (ushort)(RasterData[vinLength, y, 1].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 1].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 1].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'J', x2, y2, c2, (ushort)y);
                                        x2 = (ushort)(RasterData[vinLength, y, 0].mX);
                                        y2 = (ushort)(RasterData[vinLength, y, 0].mY);
                                        c2 = (ushort)(RasterData[vinLength, y, 0].mC);
                                        sb.AppendFormat("{0:X4}{1:X4}{2:X4}{3:X4}{4:X4}", (ushort)'S', x2, y2, c2, NoPointsC[y, 0]);

                                        for (i = vinLength - 1; i >= 0; i--)
                                            for (int x = ifontsizeX; x >= -1; x--)
                                            {
                                                if (AllClrData[i, y, x] != null)
                                                {
                                                    sb.AppendFormat("{0:X4}", AllClrData[i, y, x].mX);
                                                }
                                            }
                                        //Mode_File.SendClean.Add(sb.ToString());
                                        currMarkInfo.senddata.sendDataClean.Add(sb.ToString());
                                        Debug.WriteLine(y.ToString("00") + "C" + NoPointsC[y, 0].ToString("000") + ":" + sb.ToString());
                                        sb.Length = 0;
                                    }
                                    DirRight = !DirRight;
                                }
                                else
                                {
                                    currMarkInfo.checkdata.TwoLineDisplay = true;
                                }
                            }

                        }   // front spatter
                        if (pattern.laserValue.charClean != 0)
                        {
                            currMarkInfo.checkdata.TwoLineDisplay = false;
                        }
                    }   // Back/front spatter
                }   // Dot firing

                //M_Count = idx;
                //Mark_Counter++;

                //Mode_File.Download_Data = true;
            }
            catch (Exception ex)
            {
                //retval.execResult = ex.HResult;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //retval.errorInfo.sErrorMessage = "EXCEPTION = " + ex.Message;

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                return retval;
            }

            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;

            // Local function

            Vector3D getRodrigueRotation(Vector3D XY0)
            {
                Vector3D Tmp = new Vector3D();
                Tmp.X = XY0.X * R11 + XY0.Y * R12 + XY0.Z * R13;
                Tmp.Y = XY0.X * R21 + XY0.Y * R22 + XY0.Z * R23;
                Tmp.Z = XY0.X * R31 + XY0.Y * R32 + XY0.Z * R33;

                return Tmp;
            }

            bool checkDistance(FontData4Send[,,] AllClrData, int ii, int yy, int xx)
            {
                if (AllClrData[ii, yy, xx - 1] != null)
                {
                    if (Math.Abs(AllClrData[ii, yy, xx].mI - AllClrData[ii, yy, xx - 1].mI) < 0.599)
                        return false;
                }
                if (AllClrData[ii, yy, xx + 1] != null)
                {
                    if (Math.Abs(AllClrData[ii, yy, xx].mI - AllClrData[ii, yy, xx + 1].mI) < 0.599)
                        return false;
                }
                return true;
            }
        }

        // 7 Point Checking And Error Popup
        public async Task<CheckAreaData> Range_Test3(string vin, PatternValueEx pattern, bool mark6 = false)   // Plating  by TM SHIN $$$
        {
            string className = "MainWindow";
            string funcName = "Range_Test";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            distanceSensorData snsdata = new distanceSensorData();
            CheckAreaData chkdata = new CheckAreaData();

            int vinLength = 17;
            double totWidth = 0;

            Vector3D SP0 = new Vector3D();
            Vector3D SP1 = new Vector3D();
            Vector3D CP = new Vector3D();

            Vector3D PointLU = new Vector3D();
            Vector3D PointLD = new Vector3D();
            Vector3D PointRU = new Vector3D();

            Vector3D[] vCheckPos = new Vector3D[7];
            double[] HeightVal = new double[7];

            Vector3D vector3 = new Vector3D();

            double HeightCT0 = 0;
            string value = "";
            string log = "";

            List<Vector3D> planePoints = new List<Vector3D>();

            short gMinX = 0;
            short gMaxX = 0;
            short gMinY = 0;
            short gMaxY = 0;
            short? CenterZ = null;
            double dSlope = 0;
            string sCurrentFunc = "PLATE CHECK";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                ShowLog(className, funcName, 0, "[PLATE CHECK] START");

                Util.GetPrivateProfileValue("CONFIG", "SKIPCHECKPLANE", "0", ref value, Constants.SCANNER_INI_FILE);
                if (value != "0")
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SKIP CHECK PLANE", Thread.CurrentThread.ManagedThreadId);
                    chkdata.NormalDir.X = 0;
                    chkdata.NormalDir.Y = 0;
                    chkdata.NormalDir.Z = 1;
                    chkdata.bReady = true;
                    chkdata.execResult = 0;
                    ShowLog(className, funcName, 0, "[PLATE CHECK] SKIP");
                    return chkdata;
                }

                vinLength = vin.Length;
                if (vinLength <= 0)
                {
                    log = "ERROR : VIN LENGTH <= 0 (" + vinLength.ToString() + ")";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = ErrorCodeConstant.ERROR_PARAM_INVALID;
                    chkdata.errorInfo.sErrorMessage = "VIN IS EMPTY";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_PARAM + Constants.ERROR_INVALID;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_DISTACE;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_DISTACE;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                    return chkdata;
                }

                totWidth = pattern.fontValue.pitch * (vinLength - 1) + pattern.fontValue.width;

                // SET Motor Speed
                m_currCMD = (byte)'L';
                retval = await MarkControll.LoadSpeed(m_currCMD, pattern.speedValue.initSpeed4Measure, pattern.speedValue.targetSpeed4Measure, pattern.speedValue.accelSpeed4Measure, pattern.speedValue.decelSpeed4Measure);
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - SET MOTOR SPEED(MEASURE) ERROR = " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = retval.execResult;
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + retval.execResult.ToString();
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    return chkdata;
                }

                // Laser Aiming Beam ON
                retval = await laserSource.SetExternalAimingBeamControll(0);
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.SetExternalAimingBeamControll(0);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.SetExternalAimingBeamControll(0);
                }
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - SET BEAM CONTROLL ERROR : " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = retval.execResult;
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (SetExternalAimingBeamControll) ERROR = " + retval.execResult.ToString();
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;

                    return chkdata;
                }

                retval = await laserSource.AimingBeamON();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.AimingBeamON();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.AimingBeamON();
                }
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM ON ERROR : " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = retval.execResult;
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (AimingBeamON) ERROR = " + retval.execResult.ToString();
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    return chkdata;
                }

                ShowRectangle(System.Windows.Media.Brushes.Black, AimingLamp);

                ShowLabelData("", lblDispCenXCenY);
                ShowLabelData("", lblDispMinXMaxY);
                ShowLabelData("", lblDispMinXMinY);
                ShowLabelData("", lblDispMaxXMaxY);
                ShowLabelData("", lblDispMaxXMinY);
                ShowLabelData("", lblDispCenXMaxY);
                ShowLabelData("", lblDispCenXMinY);

                planePoints.Clear();
                currMarkInfo.senddata.Clear();

                // Absolute mm of Center Point
                CP = pattern.positionValue.center3DPos;
                SP0.X = totWidth / 2;
                SP0.Y = pattern.fontValue.height / 2;

                SP1 = CP - SP0;
                SP1.Z = CP.Z;

                // ABS mm
                double MinX = SP1.X;
                double MaxX = SP1.X + totWidth;
                double MinY = SP1.Y;
                double MaxY = SP1.Y + pattern.fontValue.height;

                // ABS BLU
                gMinX = (short)(MinX * pattern.headValue.stepLength + 0.5);
                gMaxX = (short)(MaxX * pattern.headValue.stepLength + 0.5);
                gMinY = (short)(MinY * pattern.headValue.stepLength + 0.5);
                gMaxY = (short)(MaxY * pattern.headValue.stepLength + 0.5);

                // ABS mm
                double CX = (MaxX + MinX) / 2.0;
                double CY = (MaxY + MinY) / 2.0;

                // ABS BLU
                short tCX = (short)(CX * pattern.headValue.stepLength + 0.5);
                short tCY = (short)(CY * pattern.headValue.stepLength + 0.5);
                //CenterX = tCX;
                //CenterY = tCY;
                //CenterZ = (short)(SP1.Z * pattern.headValue.stepLength + 0.5);
                CenterZ = (short)(CP.Z * pattern.headValue.stepLength + 0.5);

                //short Parking_Z = (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5);

                //vector3.X = tCX;
                //vector3.Y = tCY;
                //vector3.Z = Parking_Z;
                vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));

                snsdata = await GetMeasureLength(vector3, 0, 1);
                if (snsdata.execResult != 0)
                {
                    //retval.execResult = snsdata.execResult;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = snsdata.execResult;
                    chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                    //chkdata.sErrorMessage = "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString();

                    //ITNTErrorCode();
                    //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength", snsdata.execResult.ToString());

                    return chkdata;
                }

                double MeasureHeight = snsdata.sensoroffset;
                if (Math.Abs(MeasureHeight) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                {
                    vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
                    snsdata = await GetMeasureLength(vector3, 0, 1);
                    if (snsdata.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        chkdata.execResult = snsdata.execResult;
                        chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                        return chkdata;
                    }

                    MeasureHeight = snsdata.sensoroffset;
                    if (Math.Abs(MeasureHeight) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                    {
                        vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
                        snsdata = await GetMeasureLength(vector3, 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                            return chkdata;
                        }

                        MeasureHeight = snsdata.sensoroffset;
                        if (Math.Abs(MeasureHeight) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                        {
                            chkdata.ErrorDistanceSensor = true;

                            ShowLabelData(MeasureHeight.ToString("0.000"), lblDispCenXCenY, System.Windows.Media.Brushes.Red, null, null);

                            //retval.execResult = -2;
                            log = "CENTER Z RANGE(" + MeasureHeight.ToString("F4") + ") IS OVER THAN STANDARD(" + pattern.positionValue.checkDistanceHeight.ToString("F4") + ")";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                            chkdata.execResult = ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR;
                            chkdata.errorInfo.sErrorMessage = "DISTANCE FROM MARKING HEAD TO PLATE IS TOO FAR.";
                            chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                            chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_Z_HEIGHT;

                            chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                            chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                            chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                            chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                            chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;
                            return chkdata;
                        }
                    }
                }

                double ShiftCT = snsdata.sensorshift;
                double HeightCT = snsdata.sensoroffset;

                chkdata.checkdistance[0] = snsdata.rawdistance;
                ShowLabelData(snsdata.rawdistance.ToString("F3"), lblDispHeightValue);
                ShowLabelData(snsdata.sensoroffset.ToString("F3"), lblDispHeightCosine);
                // Sensor Shift compensation
                ShowLabelData(HeightCT.ToString("0.000"), lblDispCenXCenY, System.Windows.Media.Brushes.Black, null, null);

                if (pattern.headValue.sensorPosition == 0)  // RIGHT
                {
                    gMinX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
                        gMinX = pattern.headValue.stepLength;                                   // && 7

                    gMaxX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
                        gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
                }
                else                                       // LEFT
                {
                    gMinX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
                        gMinX = pattern.headValue.stepLength;
                    gMaxX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
                        gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
                }

                tCX = (short)(((double)gMaxX + (double)gMinX) / 2.0 + 0.5);
                tCY = (short)(((double)gMaxY + (double)gMinY) / 2.0 + 0.5);


                double[] vCheckPosX = new double[] { gMinX, gMinX, tCX, gMaxX, gMaxX, tCX, tCX };
                double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMinY, gMaxY, gMaxY, tCY };

                Vector3D[] vAddPos = new Vector3D[7];

                double left = MinX - CP.X;
                double centerX = (MaxX + MinX) / 2.0 - CP.X;
                double right = MaxX - CP.X;
                double up = MaxY - CP.Y;
                double centerY = (MaxY + MinY) / 2.0 - CP.Y;
                double down = MinY - CP.Y;

                double[] vAddPosX = new double[] { left, left, centerX, right, right, centerX, centerX };
                double[] vAddPosY = new double[] { up, down, down, down, up, up, centerY };
                string[] sPosition = new string[] { "LU", "LD", "CD", "RD", "RU", "CU", "CC" };

                Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispCenXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXMaxY, lblDispCenXCenY };
                for (int i = 0; i < 7; i++)
                {
                    vCheckPos[i].X = vCheckPosX[i];
                    vCheckPos[i].Y = vCheckPosY[i];
                    //vCheckPos[i].Z = Parking_Z;
                    vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                    snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);   // 4 !!! TM SHIN
                    if (snsdata.execResult != 0)
                    {
                        //retval.execResult = snsdata.execResult;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(+" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        chkdata.execResult = snsdata.execResult;
                        chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                        //ITNTErrorCode();
                        //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                        return chkdata;
                    }

                    vAddPos[i].X = vAddPosX[i];
                    vAddPos[i].Y = vAddPosY[i];
                    vAddPos[i].Z = snsdata.sensoroffset;
                    HeightVal[i] = snsdata.sensoroffset;
                    chkdata.checkdistance[i + 1] = snsdata.rawdistance;
                    if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                    {
                        chkdata.ErrorDistanceSensor = true;
                        //retval.sErrorMessage = "";
                    }

                    planePoints.Add(vAddPos[i]);
                    ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
                    HeightCT = HeightVal[i];    // TM SHIN
                }

                //HeightCT = HeightVal[6];    // TM SHIN

                retval = await laserSource.AimingBeamOFF();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.AimingBeamOFF();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.AimingBeamOFF();
                }
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = retval.execResult;

                    //ITNTErrorCode();
                    //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - AimingBeamOFF", chkdata.execResult.ToString());
                    //ShowLog(className, funcName, 2, log);
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();
                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (AimingBeamOFF) ERROR = " + retval.execResult.ToString(); ;
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;

                    return chkdata;
                }

                ShowRectangle(System.Windows.Media.Brushes.Black, AimingLamp);

                if (chkdata.ErrorDistanceSensor)
                {
                    Vector3D TmpPoint = new Vector3D();
                    planePoints.Clear();

                    HeightCT0 = HeightCT;
                    HeightCT = pattern.positionValue.checkDistanceHeight;

                    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = CY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over : " + HeightCT.ToString("0.000"), Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR;
                    //chkdata.errorInfo.sErrorMessage = "DISTANCE FROM MARKING HEAD TO PLATE IS TOO FAR.";
                    chkdata.errorInfo.sErrorMessage = "CHECK POINT Z RANGE(" + HeightCT.ToString("F4") + ") IS OVER THAN STANDARD(" + pattern.positionValue.checkDistanceHeight.ToString("F4") + ")";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_Z_HEIGHT;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                    return chkdata;
                }

                HeightCT = HeightVal[6];    // TM SHIN

                //CenterZ += (short)(HeightCT * pattern.headValue.stepLength + 0.5);

                //// REL mm at 4 Corners, CD
                PointLU.X = SP1.X - CP.X;
                PointLU.Y = SP1.Y - CP.Y + pattern.fontValue.height;
                PointLD.Y = SP1.Y - CP.Y;
                PointRU.X = SP1.X - CP.X + totWidth;
                ////
                Vector3D Sum = new Vector3D();
                foreach (var mPoint in planePoints)
                {
                    Sum.X += mPoint.X; Sum.Y += mPoint.Y; Sum.Z += mPoint.Z;
                }
                Vector3D Centroid = new Vector3D();
                Centroid.X = Sum.X / planePoints.Count;
                Centroid.Y = Sum.Y / planePoints.Count;
                Centroid.Z = Sum.Z / planePoints.Count;
                double xx, xy, xz, yy, yz, zz;
                xx = xy = xz = yy = yz = zz = 0.0;
                foreach (var mPoint in planePoints)
                {
                    xx += (mPoint.X - Centroid.X) * (mPoint.X - Centroid.X);
                    xy += (mPoint.X - Centroid.X) * (mPoint.Y - Centroid.Y);
                    xz += (mPoint.X - Centroid.X) * (mPoint.Z - Centroid.Z);
                    yy += (mPoint.Y - Centroid.Y) * (mPoint.Y - Centroid.Y);
                    yz += (mPoint.Y - Centroid.Y) * (mPoint.Z - Centroid.Z);
                    zz += (mPoint.Z - Centroid.Z) * (mPoint.Z - Centroid.Z);
                }
                chkdata.NormalDir.X = xy * yz - xz * yy;
                chkdata.NormalDir.Y = xy * xz - yz * xx;
                chkdata.NormalDir.Z = xx * yy - xy * xy;

                double Ds = chkdata.NormalDir.X * Centroid.X + chkdata.NormalDir.Y * Centroid.Y + chkdata.NormalDir.Z * Centroid.Z;

                double PlaneLU = GetZfromPlane(PointLU.X, PointLU.Y);
                double PlaneLD = GetZfromPlane(PointLU.X, PointLD.Y);
                double PlaneRU = GetZfromPlane(PointRU.X, PointLU.Y);
                double PlaneRD = GetZfromPlane(PointRU.X, PointLD.Y);
                double PlaneCU = GetZfromPlane(0, PointLU.Y);
                double PlaneCD = GetZfromPlane(0, PointLD.Y);
                chkdata.PlaneCenterZ = GetZfromPlane(0, 0);

                CenterZ = (short)((CP.Z + pattern.headValue.distance0Position + chkdata.PlaneCenterZ) * (double)pattern.headValue.stepLength + 0.5);              // BLU // && 8

                double PdiffLU, PdiffLD, PdiffRD, PdiffRU, PdiffCU, PdiffCD;
                PdiffLU = PlaneLU - chkdata.PlaneCenterZ;
                PdiffLD = PlaneLD - chkdata.PlaneCenterZ;
                PdiffRU = PlaneRU - chkdata.PlaneCenterZ;
                PdiffRD = PlaneRD - chkdata.PlaneCenterZ;
                PdiffCU = PlaneCU - chkdata.PlaneCenterZ;
                PdiffCD = PlaneCD - chkdata.PlaneCenterZ;
                double PminDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
                double PmaxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
                double PdiffDiff = Math.Abs(PmaxDiff - PminDiff);

                Util.GetPrivateProfileValue("OPTION", "SLOPE", "1.0", ref value, Constants.MARKING_INI_FILE);
                double.TryParse(value, out dSlope);

                if (PdiffDiff > dSlope)
                {
                    log = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
                    chkdata.execResult = -5;

                    // Error handling required!!
                    //chkdata.sErrorMessage = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
                    //log = "PLATE SLOPE(" + PdiffDiff.ToString("F3") + ") IS OVER THAN STANDARD(" + dSlope.ToString("F3") + ")";
                    //ShowLog(className, funcName, 2, log);


                    //chkdata.errorInfo.sErrorFunc = sCurrentFunc;


                    chkdata.execResult = ErrorCodeConstant.ERROR_SLOPE_ERROR;
                    chkdata.errorInfo.sErrorMessage = "PLATE SLOPE(" + PdiffDiff.ToString("F3") + ") IS OVER THAN TOLERANCE(" + dSlope.ToString("F3") + ")";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_SLOPE_BIG;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over : " + HeightCT.ToString("0.000"), Thread.CurrentThread.ManagedThreadId);

                    return chkdata;
                }

                ShowLabelData(chkdata.PlaneCenterZ.ToString("0.000;-0.000;0.000"), lblDispCenXCenY, (chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.DarkGreen);
                ShowLabelData(PdiffLU.ToString("+ 0.000;- 0.000;0.000"), lblDispMinXMaxY, (PdiffLU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffLD.ToString("+ 0.000;- 0.000;0.000"), lblDispMinXMinY, (PdiffLD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffRU.ToString("+ 0.000;- 0.000;0.000"), lblDispMaxXMaxY, (PdiffRU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffRD.ToString("+ 0.000;- 0.000;0.000"), lblDispMaxXMinY, (PdiffRD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffCU.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMaxY, (PdiffCU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffCD.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMinY, (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);

                //ShowLabelData(PdiffCD.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMinY, (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);

                //Dispatcher.Invoke(new Action(delegate
                //{
                //    lblDispCenXCenY.Foreground = (chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.DarkGreen;
                //    lblDispCenXCenY.Content = chkdata.PlaneCenterZ.ToString("0.000;-0.000;0.000");

                //    lblDispMinXMaxY.Foreground = (PdiffLU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMinXMaxY.Content = PdiffLU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMinXMinY.Foreground = (PdiffLD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMinXMinY.Content = PdiffLD.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMaxXMaxY.Foreground = (PdiffRU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMaxXMaxY.Content = PdiffRU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMaxXMinY.Foreground = (PdiffRD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispMaxXMinY.Content = PdiffRD.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispCenXMaxY.Foreground = (PdiffCU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispCenXMaxY.Content = PdiffCU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispCenXMinY.Foreground = (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispCenXMinY.Content = PdiffCD.ToString("+ 0.000;- 0.000;0.000");
                //}
                //));

                int jc = (int)((PointLU.Y - PointLD.Y) + 0.5);
                int ic = (int)((PointRU.X - PointLU.X) + 0.5);
                byte[,] HC = new byte[jc, ic];

                double Yy = PointLU.Y;
                double Xx = PointLU.X;
                double Zz = 0;
                for (int r = 0; r < jc; r++)
                {
                    Xx = PointLU.X;
                    for (int c = 0; c < ic; c++)
                    {
                        Zz = (GetZfromPlane(Xx, Yy) - GetZfromPlane(0, 0)) * 200.0;
                        if (Zz > 127.0) Zz = 127.0;
                        if (Zz < -127.0) Zz = -127.0;
                        HC[r, c] = (byte)(Zz + 127.0);
                        Xx += 1.0;  // +1 mm;
                    }
                    Yy -= 1.0;      // -1 mm;
                }

                Dispatcher.Invoke(new Action(delegate
                {
                    PlateColoring(HC);
                }));

                chkdata.bReady = true;

                double GetZfromPlane(double x, double y)
                {
                    double pz = 0;
                    if (chkdata.NormalDir.Z != 0)
                        pz = Ds / chkdata.NormalDir.Z - chkdata.NormalDir.X / chkdata.NormalDir.Z * x - chkdata.NormalDir.Y / chkdata.NormalDir.Z * y;
                    else
                        pz = 0;
                    return pz;
                }
                return chkdata;
            }
            catch (Exception ex)
            {
                chkdata.execResult = ex.HResult;
                chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                chkdata.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-chkdata.execResult).ToString("X2");

                chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                return chkdata;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vin"></param>
        /// <param name="pattern"></param>
        /// <param name="mark6"></param>
        /// <returns></returns>
        public async Task<CheckAreaData> Range_Test4(string vin, PatternValueEx pattern, bool mark6 = false)   // Plating  by TM SHIN $$$
        {
            string className = "MainWindow";
            string funcName = "Range_Test";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            distanceSensorData snsdata = new distanceSensorData();
            CheckAreaData chkdata = new CheckAreaData();

            int vinLength = 17;
            double totWidth = 0;

            Vector3D SP0 = new Vector3D();
            Vector3D SP1 = new Vector3D();
            Vector3D CP = new Vector3D();

            Vector3D PointLU = new Vector3D();
            Vector3D PointLD = new Vector3D();
            Vector3D PointRU = new Vector3D();

            Vector3D[] vCheckPos = new Vector3D[7];
            double[] HeightVal = new double[7];

            Vector3D vector3 = new Vector3D();

            double HeightCT0 = 0;
            string value = "";
            string log = "";

            List<Vector3D> planePoints = new List<Vector3D>();

            short gMinX = 0;
            short gMaxX = 0;
            short gMinY = 0;
            short gMaxY = 0;
            short? CenterZ = null;
            double dSlope = 0;
            string sCurrentFunc = "PLATE CHECK";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                ShowLog(className, funcName, 0, "[PLATE CHECK] START");

                Util.GetPrivateProfileValue("CONFIG", "SKIPCHECKPLANE", "0", ref value, Constants.SCANNER_INI_FILE);
                if (value != "0")
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SKIP CHECK PLANE", Thread.CurrentThread.ManagedThreadId);
                    chkdata.NormalDir.X = 0;
                    chkdata.NormalDir.Y = 0;
                    chkdata.NormalDir.Z = 1;
                    chkdata.bReady = true;
                    chkdata.execResult = 0;
                    ShowLog(className, funcName, 0, "[PLATE CHECK] SKIP");
                    return chkdata;
                }

                vinLength = vin.Length;
                if (vinLength <= 0)
                {
                    log = "ERROR : VIN LENGTH <= 0 (" + vinLength.ToString() + ")";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = ErrorCodeConstant.ERROR_PARAM_INVALID;
                    chkdata.errorInfo.sErrorMessage = "VIN IS EMPTY";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_PARAM + Constants.ERROR_INVALID;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_DISTACE;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_DISTACE;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                    return chkdata;
                }

                totWidth = pattern.fontValue.pitch * (vinLength - 1) + pattern.fontValue.width;

                // SET Motor Speed
                m_currCMD = (byte)'L';
                retval = await MarkControll.LoadSpeed(m_currCMD, pattern.speedValue.initSpeed4Measure, pattern.speedValue.targetSpeed4Measure, pattern.speedValue.accelSpeed4Measure, pattern.speedValue.decelSpeed4Measure);
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - SET MOTOR SPEED(MEASURE) ERROR = " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = retval.execResult;
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + retval.execResult.ToString();
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    return chkdata;
                }

                // Laser Aiming Beam ON
                retval = await laserSource.SetExternalAimingBeamControll(0);
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.SetExternalAimingBeamControll(0);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.SetExternalAimingBeamControll(0);
                }
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - SET BEAM CONTROLL ERROR : " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = retval.execResult;
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (SetExternalAimingBeamControll) ERROR = " + retval.execResult.ToString();
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;

                    return chkdata;
                }

                retval = await laserSource.AimingBeamON();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.AimingBeamON();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.AimingBeamON();
                }
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM ON ERROR : " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = retval.execResult;
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (AimingBeamON) ERROR = " + retval.execResult.ToString();
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    return chkdata;
                }

                ShowRectangle(System.Windows.Media.Brushes.Black, AimingLamp);

                ShowLabelData("", lblDispCenXCenY);
                ShowLabelData("", lblDispMinXMaxY);
                ShowLabelData("", lblDispMinXMinY);
                ShowLabelData("", lblDispMaxXMaxY);
                ShowLabelData("", lblDispMaxXMinY);
                ShowLabelData("", lblDispCenXMaxY);
                ShowLabelData("", lblDispCenXMinY);

                planePoints.Clear();
                currMarkInfo.senddata.Clear();

                // Absolute mm of Center Point
                CP = pattern.positionValue.center3DPos;
                SP0.X = totWidth / 2;
                SP0.Y = pattern.fontValue.height / 2;

                SP1 = CP - SP0;
                SP1.Z = CP.Z;

                // ABS mm
                double MinX = SP1.X;
                double MaxX = SP1.X + totWidth;
                double MinY = SP1.Y;
                double MaxY = SP1.Y + pattern.fontValue.height;

                // ABS BLU
                gMinX = (short)(MinX * pattern.headValue.stepLength + 0.5);
                gMaxX = (short)(MaxX * pattern.headValue.stepLength + 0.5);
                gMinY = (short)(MinY * pattern.headValue.stepLength + 0.5);
                gMaxY = (short)(MaxY * pattern.headValue.stepLength + 0.5);

                // ABS mm
                double CX = (MaxX + MinX) / 2.0;
                double CY = (MaxY + MinY) / 2.0;

                // ABS BLU
                short tCX = (short)(CX * pattern.headValue.stepLength + 0.5);
                short tCY = (short)(CY * pattern.headValue.stepLength + 0.5);
                //CenterX = tCX;
                //CenterY = tCY;
                //CenterZ = (short)(SP1.Z * pattern.headValue.stepLength + 0.5);
                CenterZ = (short)(CP.Z * pattern.headValue.stepLength + 0.5);

                //short Parking_Z = (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5);

                //vector3.X = tCX;
                //vector3.Y = tCY;
                //vector3.Z = Parking_Z;
                vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
                snsdata = await GetMeasureLength(vector3, 0, 1);
                if (snsdata.execResult != 0)
                {
                    //retval.execResult = snsdata.execResult;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = snsdata.execResult;
                    chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                    //chkdata.sErrorMessage = "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString();

                    //ITNTErrorCode();
                    //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength", snsdata.execResult.ToString());

                    return chkdata;
                }

                double MeasureHeight = snsdata.sensoroffset;
                if (Math.Abs(MeasureHeight) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                {
                    await Task.Delay(200);
                    vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
                    snsdata = await GetMeasureLength(vector3, 0, 1);
                    if (snsdata.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        chkdata.execResult = snsdata.execResult;
                        chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                        return chkdata;
                    }

                    MeasureHeight = snsdata.sensoroffset;
                    if (Math.Abs(MeasureHeight) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                    {
                        await Task.Delay(200);
                        vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
                        snsdata = await GetMeasureLength(vector3, 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                            return chkdata;
                        }

                        MeasureHeight = snsdata.sensoroffset;
                        if (Math.Abs(MeasureHeight) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                        {
                            chkdata.ErrorDistanceSensor = true;

                            ShowLabelData(MeasureHeight.ToString("0.000"), lblDispCenXCenY, System.Windows.Media.Brushes.Red, null, null);

                            //retval.execResult = -2;
                            log = "CENTER Z RANGE(" + MeasureHeight.ToString("F4") + ") IS OVER THAN STANDARD(" + pattern.positionValue.checkDistanceHeight.ToString("F4") + ")";
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                            chkdata.execResult = ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR;
                            chkdata.errorInfo.sErrorMessage = "DISTANCE FROM MARKING HEAD TO PLATE IS TOO FAR.";
                            chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                            chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_Z_HEIGHT;

                            chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                            chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                            chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                            chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                            chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;
                            return chkdata;
                        }
                    }
                }

                double ShiftCT = snsdata.sensorshift;
                double HeightCT = snsdata.sensoroffset;

                chkdata.checkdistance[0] = snsdata.rawdistance;
                ShowLabelData(snsdata.rawdistance.ToString("F3"), lblDispHeightValue);
                ShowLabelData(snsdata.sensoroffset.ToString("F3"), lblDispHeightCosine);
                // Sensor Shift compensation
                ShowLabelData(HeightCT.ToString("0.000"), lblDispCenXCenY, System.Windows.Media.Brushes.Black, null, null);

                if (pattern.headValue.sensorPosition == 0)  // RIGHT
                {
                    gMinX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
                        gMinX = pattern.headValue.stepLength;                                   // && 7

                    gMaxX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
                        gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
                }
                else                                       // LEFT
                {
                    gMinX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
                        gMinX = pattern.headValue.stepLength;
                    gMaxX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
                        gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
                }

                tCX = (short)(((double)gMaxX + (double)gMinX) / 2.0 + 0.5);
                tCY = (short)(((double)gMaxY + (double)gMinY) / 2.0 + 0.5);


























                //double[] vCheckPosX = new double[] { gMinX, gMinX, tCX, gMaxX, gMaxX, tCX, tCX };
                //double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMinY, gMaxY, gMaxY, tCY };



                Vector3D[] vAddPos = new Vector3D[7];
                if (pattern.positionValue.plateMode == 0)
                {
                    HeightCT = 0.0;              // && 7

                    vAddPos[0].X = MinX - CP.X; vAddPos[0].Y = MaxY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]); // && 7
                    vAddPos[0].X = MinX - CP.X; vAddPos[0].Y = MinY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]);
                    vAddPos[0].X = CX - CP.X; vAddPos[0].Y = MinY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]);
                    vAddPos[0].X = MaxX - CP.X; vAddPos[0].Y = MinY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]);
                    vAddPos[0].X = MaxX - CP.X; vAddPos[0].Y = MaxY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]);
                    vAddPos[0].X = CX - CP.X; vAddPos[0].Y = MaxY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]);
                    vAddPos[0].X = CX - CP.X; vAddPos[0].Y = CY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]); // && 7
                }
                else if (pattern.positionValue.plateMode == 1)
                {
                    Vector3D v3d = new Vector3D();
                    v3d.X = tCX; v3d.Y = tCY; v3d.Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                    snsdata = await GetMeasureLength(v3d, 0, 1);
                    if (snsdata.execResult != 0)
                    {
                        //retval.execResult = snsdata.execResult;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CC) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        chkdata.execResult = snsdata.execResult;
                        chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                        //ITNTErrorCode();
                        //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                        return chkdata;
                    }

                    HeightCT = snsdata.sensoroffset;
                    if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)
                    {
                        await Task.Delay(200);
                        v3d.X = tCX; v3d.Y = tCY; v3d.Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;
                        snsdata = await GetMeasureLength(v3d, 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CC) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                            return chkdata;
                        }

                        HeightCT = snsdata.sensoroffset;
                        if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)
                        {
                            await Task.Delay(200);
                            v3d.X = tCX; v3d.Y = tCY; v3d.Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                            snsdata = await GetMeasureLength(v3d, 0, 1);
                            if (snsdata.execResult != 0)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CC) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                                chkdata.execResult = snsdata.execResult;
                                chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                                return chkdata;
                            }

                            HeightCT = snsdata.sensoroffset;
                            if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)
                            {
                                chkdata.ErrorDistanceSensor = true;
                                //retval.sErrorMessage = "";
                            }
                        }
                    }

                    vAddPos[0].X = 0; vAddPos[0].Y = 0; vAddPos[0].Z = Height; planePoints.Add(vAddPos[0]);

                    ShowLabelData(HeightCT.ToString("F3"), lblDispCenXCenY, Brushes.Blue);

                    vAddPos[1].X = MinX - CP.X; vAddPos[1].Y = MaxY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]); // && 7
                    vAddPos[2].X = MinX - CP.X; vAddPos[1].Y = MinY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]);
                    vAddPos[3].X = CX - CP.X; vAddPos[1].Y = MinY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]);
                    vAddPos[4].X = MaxX - CP.X; vAddPos[1].Y = MinY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]);
                    vAddPos[5].X = MaxX - CP.X; vAddPos[1].Y = MaxY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]);
                    vAddPos[6].X = CX - CP.X; vAddPos[1].Y = MaxY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]);
                    vAddPos[7].X = CX - CP.X; vAddPos[1].Y = CY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]); // && 7
                }
                else if (pattern.positionValue.plateMode == 3)
                {
                    double[] vCheckPosX = new double[] { tCX, gMinX, gMaxX };
                    double[] vCheckPosY = new double[] { gMinY, gMaxY, gMaxY };

                    HeightCT = 0;
                    double[] vAddPosX = new double[] { CX - CP.X, MinX - CP.X, MaxX - CP.X };
                    double[] vAddPosY = new double[] { MinY - CP.Y, MaxY - CP.Y, MaxY - CP.Y };
                    string[] sPosition = new string[] { "CD", "LU", "RU" };
                    Label[] lblValue = new Label[] { lblDispCenXMinY, lblDispMinXMaxY, lblDispMaxXMaxY };

                    for (int i = 0; i < 3; i++)
                    {
                        vCheckPos[i].X = vCheckPosX[i];
                        vCheckPos[i].Y = vCheckPosY[i];
                        vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                        snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                            return chkdata;
                        }

                        HeightVal[i] = snsdata.sensoroffset;
                        if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        {
                            await Task.Delay(200);

                            vCheckPos[i].X = vCheckPosX[i];
                            vCheckPos[i].Y = vCheckPosY[i];
                            vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                            snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                            if (snsdata.execResult != 0)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                                chkdata.execResult = snsdata.execResult;
                                chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                                return chkdata;
                            }

                            HeightVal[i] = snsdata.sensoroffset;
                            if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                            {
                                await Task.Delay(200);
                                vCheckPos[i].X = vCheckPosX[i];
                                vCheckPos[i].Y = vCheckPosY[i];
                                vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                                snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                                if (snsdata.execResult != 0)
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                                    chkdata.execResult = snsdata.execResult;
                                    chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                                    return chkdata;
                                }

                                HeightVal[i] = snsdata.sensoroffset;
                                if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                                {
                                    chkdata.ErrorDistanceSensor = true;
                                    //retval.sErrorMessage = "";
                                }
                            }
                        }

                        vAddPos[i].X = vAddPosX[i]; vAddPos[i].Y = vAddPosY[i]; vAddPos[i].Z = HeightVal[i]; planePoints.Add(vAddPos[i]);
                        ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
                        //HeightCT = HeightVal[i];    // TM SHIN
                    }
                }
                else if (pattern.positionValue.plateMode == 5)
                {
                    double[] vCheckPosX = new double[] { gMinX, gMinX, gMaxX, gMaxX, tCX };
                    double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMaxY, tCY };


                    double[] vAddPosX = new double[] { MinX - CP.X, MinX - CP.X, MaxX - CP.X, MaxX - CP.X, CX - CP.X };
                    double[] vAddPosY = new double[] { MaxY - CP.Y, MinY - CP.Y, MinY - CP.Y, MaxY - CP.Y, CY - CP.Y };
                    string[] sPosition = new string[] { "LU", "LD", "RD", "RU", "CC" };
                    Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXCenY };

                    for (int i = 0; i < 5; i++)
                    {
                        vCheckPos[i].X = vCheckPosX[i];
                        vCheckPos[i].Y = vCheckPosY[i];
                        vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                        snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                            return chkdata;
                        }

                        HeightVal[i] = snsdata.sensoroffset;
                        if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        {
                            await Task.Delay(200);

                            vCheckPos[i].X = vCheckPosX[i];
                            vCheckPos[i].Y = vCheckPosY[i];
                            vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                            snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                            if (snsdata.execResult != 0)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                                chkdata.execResult = snsdata.execResult;
                                chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                                return chkdata;
                            }

                            HeightVal[i] = snsdata.sensoroffset;
                            if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                            {
                                await Task.Delay(200);

                                vCheckPos[i].X = vCheckPosX[i];
                                vCheckPos[i].Y = vCheckPosY[i];
                                vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                                snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                                if (snsdata.execResult != 0)
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                                    chkdata.execResult = snsdata.execResult;
                                    chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                                    return chkdata;
                                }

                                HeightVal[i] = snsdata.sensoroffset;
                                if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                                {
                                    chkdata.ErrorDistanceSensor = true;
                                    //retval.sErrorMessage = "";
                                }
                            }
                        }

                        vAddPos[i].X = vAddPosX[i]; vAddPos[i].Y = vAddPosY[i]; vAddPos[i].Z = snsdata.sensoroffset; planePoints.Add(vAddPos[i]);
                        ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
                        Height = HeightVal[i];    // TM SHIN
                    }
                    HeightCT = HeightVal[4];
                }
                else if (pattern.positionValue.plateMode == 7)
                {
                    Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispCenXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXMaxY, lblDispCenXCenY };

                    double[] vCheckPosX = new double[] { gMinX, gMinX, tCX, gMaxX, gMaxX, tCX, tCX };
                    double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMinY, gMaxY, gMaxY, tCY };

                    double[] vAddPosX = new double[] { MinX - CP.X, MinX - CP.X, CX - CP.X, MaxX - CP.X, MaxX - CP.X, CX - CP.X, CX - CP.X };
                    double[] vAddPosY = new double[] { MaxY - CP.Y, MinY - CP.Y, MinY - CP.Y, MinY - CP.Y, MaxY - CP.Y, MaxY - CP.Y, CY - CP.Y };
                    string[] sPosition = new string[] { "LU", "LD", "CD", "RD", "RU", "CU", "CC" };

                    for (int i = 0; i < 7; i++)
                    {
                        vCheckPos[i].X = vCheckPosX[i];
                        vCheckPos[i].Y = vCheckPosY[i];
                        vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                        snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                            return chkdata;
                        }

                        vAddPos[i].X = vAddPosX[i];
                        vAddPos[i].Y = vAddPosY[i];
                        vAddPos[i].Z = snsdata.sensoroffset;
                        HeightVal[i] = snsdata.sensoroffset;
                        if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        {
                            await Task.Delay(200);
                            vCheckPos[i].X = vCheckPosX[i];
                            vCheckPos[i].Y = vCheckPosY[i];
                            vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                            snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                            if (snsdata.execResult != 0)
                            {
                                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                                chkdata.execResult = snsdata.execResult;
                                chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                                return chkdata;
                            }

                            vAddPos[i].X = vAddPosX[i];
                            vAddPos[i].Y = vAddPosY[i];
                            vAddPos[i].Z = snsdata.sensoroffset;
                            HeightVal[i] = snsdata.sensoroffset;
                            if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                            {
                                await Task.Delay(200);
                                vCheckPos[i].X = vCheckPosX[i];
                                vCheckPos[i].Y = vCheckPosY[i];
                                vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                                snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                                if (snsdata.execResult != 0)
                                {
                                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                                    chkdata.execResult = snsdata.execResult;
                                    chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                                    return chkdata;
                                }

                                vAddPos[i].X = vAddPosX[i];
                                vAddPos[i].Y = vAddPosY[i];
                                vAddPos[i].Z = snsdata.sensoroffset;
                                HeightVal[i] = snsdata.sensoroffset;
                                if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                                {
                                    chkdata.ErrorDistanceSensor = true;
                                    //retval.sErrorMessage = "";
                                }
                            }
                        }

                        planePoints.Add(vAddPos[i]);
                        ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
                        HeightCT = HeightVal[i];    // TM SHIN
                    }
                    HeightCT = HeightVal[6];
                }
                else
                {
                    Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispCenXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXMaxY, lblDispCenXCenY };

                    double[] vCheckPosX = new double[] { gMinX, gMinX, tCX, gMaxX, gMaxX, tCX, tCX };
                    double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMinY, gMaxY, gMaxY, tCY };

                    double[] vAddPosX = new double[] { MinX - CP.X, MinX - CP.X, CX - CP.X, MaxX - CP.X, MaxX - CP.X, CX - CP.X, CX - CP.X };
                    double[] vAddPosY = new double[] { MaxY - CP.Y, MinY - CP.Y, MinY - CP.Y, MinY - CP.Y, MaxY - CP.Y, MaxY - CP.Y, CY - CP.Y };
                    string[] sPosition = new string[] { "LU", "LD", "CD", "RD", "RU", "CU", "CC" };

                    for (int i = 0; i < 7; i++)
                    {
                        vCheckPos[i].X = vCheckPosX[i];
                        vCheckPos[i].Y = vCheckPosY[i];
                        vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                        snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            //retval.execResult = snsdata.execResult;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(+" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                            //ITNTErrorCode();
                            //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                            return chkdata;
                        }

                        vAddPos[i].X = vAddPosX[i];
                        vAddPos[i].Y = vAddPosY[i];
                        vAddPos[i].Z = snsdata.sensoroffset;
                        HeightVal[i] = snsdata.sensoroffset;
                        if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        {
                            chkdata.ErrorDistanceSensor = true;
                            //retval.sErrorMessage = "";
                        }

                        planePoints.Add(vAddPos[i]);
                        ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
                        HeightCT = HeightVal[i];    // TM SHIN
                    }
                    HeightCT = HeightVal[6];    // TM SHIN
                }

                ////HeightCT = HeightVal[6];    // TM SHIN

                retval = await laserSource.AimingBeamOFF();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.AimingBeamOFF();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.AimingBeamOFF();
                }
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = retval.execResult;

                    //ITNTErrorCode();
                    //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - AimingBeamOFF", chkdata.execResult.ToString());
                    //ShowLog(className, funcName, 2, log);
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();
                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (AimingBeamOFF) ERROR = " + retval.execResult.ToString(); ;
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;

                    return chkdata;
                }

                ShowRectangle(System.Windows.Media.Brushes.Black, AimingLamp);

                if (chkdata.ErrorDistanceSensor)
                {
                    Vector3D TmpPoint = new Vector3D();
                    planePoints.Clear();

                    HeightCT0 = HeightCT;
                    HeightCT = pattern.positionValue.checkDistanceHeight;

                    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = CY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over : " + HeightCT.ToString("0.000"), Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR;
                    //chkdata.errorInfo.sErrorMessage = "DISTANCE FROM MARKING HEAD TO PLATE IS TOO FAR.";
                    chkdata.errorInfo.sErrorMessage = "CHECK POINT Z RANGE(" + HeightCT.ToString("F4") + ") IS OVER THAN STANDARD(" + pattern.positionValue.checkDistanceHeight.ToString("F4") + ")";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_Z_HEIGHT;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                    return chkdata;
                }

                HeightCT = HeightVal[6];    // TM SHIN

                //CenterZ += (short)(HeightCT * pattern.headValue.stepLength + 0.5);

                //// REL mm at 4 Corners, CD
                PointLU.X = SP1.X - CP.X;
                PointLU.Y = SP1.Y - CP.Y + pattern.fontValue.height;
                PointLD.Y = SP1.Y - CP.Y;
                PointRU.X = SP1.X - CP.X + totWidth;
                ////
                Vector3D Sum = new Vector3D();
                foreach (var mPoint in planePoints)
                {
                    Sum.X += mPoint.X; Sum.Y += mPoint.Y; Sum.Z += mPoint.Z;
                }
                Vector3D Centroid = new Vector3D();
                Centroid.X = Sum.X / planePoints.Count;
                Centroid.Y = Sum.Y / planePoints.Count;
                Centroid.Z = Sum.Z / planePoints.Count;
                double xx, xy, xz, yy, yz, zz;
                xx = xy = xz = yy = yz = zz = 0.0;
                foreach (var mPoint in planePoints)
                {
                    xx += (mPoint.X - Centroid.X) * (mPoint.X - Centroid.X);
                    xy += (mPoint.X - Centroid.X) * (mPoint.Y - Centroid.Y);
                    xz += (mPoint.X - Centroid.X) * (mPoint.Z - Centroid.Z);
                    yy += (mPoint.Y - Centroid.Y) * (mPoint.Y - Centroid.Y);
                    yz += (mPoint.Y - Centroid.Y) * (mPoint.Z - Centroid.Z);
                    zz += (mPoint.Z - Centroid.Z) * (mPoint.Z - Centroid.Z);
                }
                chkdata.NormalDir.X = xy * yz - xz * yy;
                chkdata.NormalDir.Y = xy * xz - yz * xx;
                chkdata.NormalDir.Z = xx * yy - xy * xy;

                double Ds = chkdata.NormalDir.X * Centroid.X + chkdata.NormalDir.Y * Centroid.Y + chkdata.NormalDir.Z * Centroid.Z;

                double PlaneLU = GetZfromPlane(PointLU.X, PointLU.Y);
                double PlaneLD = GetZfromPlane(PointLU.X, PointLD.Y);
                double PlaneRU = GetZfromPlane(PointRU.X, PointLU.Y);
                double PlaneRD = GetZfromPlane(PointRU.X, PointLD.Y);
                double PlaneCU = GetZfromPlane(0, PointLU.Y);
                double PlaneCD = GetZfromPlane(0, PointLD.Y);
                chkdata.PlaneCenterZ = GetZfromPlane(0, 0);

                CenterZ = (short)((CP.Z + pattern.headValue.distance0Position + chkdata.PlaneCenterZ) * (double)pattern.headValue.stepLength + 0.5);              // BLU // && 8

                double PdiffLU, PdiffLD, PdiffRD, PdiffRU, PdiffCU, PdiffCD;
                PdiffLU = PlaneLU - chkdata.PlaneCenterZ;
                PdiffLD = PlaneLD - chkdata.PlaneCenterZ;
                PdiffRU = PlaneRU - chkdata.PlaneCenterZ;
                PdiffRD = PlaneRD - chkdata.PlaneCenterZ;
                PdiffCU = PlaneCU - chkdata.PlaneCenterZ;
                PdiffCD = PlaneCD - chkdata.PlaneCenterZ;
                double PminDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
                double PmaxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
                double PdiffDiff = Math.Abs(PmaxDiff - PminDiff);

                Util.GetPrivateProfileValue("OPTION", "SLOPE", "1.0", ref value, Constants.MARKING_INI_FILE);
                double.TryParse(value, out dSlope);

                if (PdiffDiff > dSlope)
                {
                    log = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
                    chkdata.execResult = -5;

                    // Error handling required!!
                    //chkdata.sErrorMessage = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
                    //log = "PLATE SLOPE(" + PdiffDiff.ToString("F3") + ") IS OVER THAN STANDARD(" + dSlope.ToString("F3") + ")";
                    //ShowLog(className, funcName, 2, log);


                    //chkdata.errorInfo.sErrorFunc = sCurrentFunc;


                    chkdata.execResult = ErrorCodeConstant.ERROR_SLOPE_ERROR;
                    chkdata.errorInfo.sErrorMessage = "PLATE SLOPE(" + PdiffDiff.ToString("F3") + ") IS OVER THAN TOLERANCE(" + dSlope.ToString("F3") + ")";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_SLOPE_BIG;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over : " + HeightCT.ToString("0.000"), Thread.CurrentThread.ManagedThreadId);

                    return chkdata;
                }

                ShowLabelData(chkdata.PlaneCenterZ.ToString("0.000;-0.000;0.000"), lblDispCenXCenY, (chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.DarkGreen);
                ShowLabelData(PdiffLU.ToString("+ 0.000;- 0.000;0.000"), lblDispMinXMaxY, (PdiffLU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffLD.ToString("+ 0.000;- 0.000;0.000"), lblDispMinXMinY, (PdiffLD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffRU.ToString("+ 0.000;- 0.000;0.000"), lblDispMaxXMaxY, (PdiffRU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffRD.ToString("+ 0.000;- 0.000;0.000"), lblDispMaxXMinY, (PdiffRD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffCU.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMaxY, (PdiffCU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffCD.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMinY, (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);

                //ShowLabelData(PdiffCD.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMinY, (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);

                //Dispatcher.Invoke(new Action(delegate
                //{
                //    lblDispCenXCenY.Foreground = (chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.DarkGreen;
                //    lblDispCenXCenY.Content = chkdata.PlaneCenterZ.ToString("0.000;-0.000;0.000");

                //    lblDispMinXMaxY.Foreground = (PdiffLU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMinXMaxY.Content = PdiffLU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMinXMinY.Foreground = (PdiffLD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMinXMinY.Content = PdiffLD.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMaxXMaxY.Foreground = (PdiffRU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMaxXMaxY.Content = PdiffRU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMaxXMinY.Foreground = (PdiffRD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispMaxXMinY.Content = PdiffRD.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispCenXMaxY.Foreground = (PdiffCU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispCenXMaxY.Content = PdiffCU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispCenXMinY.Foreground = (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispCenXMinY.Content = PdiffCD.ToString("+ 0.000;- 0.000;0.000");
                //}
                //));

                int jc = (int)((PointLU.Y - PointLD.Y) + 0.5);
                int ic = (int)((PointRU.X - PointLU.X) + 0.5);
                byte[,] HC = new byte[jc, ic];

                double Yy = PointLU.Y;
                double Xx = PointLU.X;
                double Zz = 0;
                for (int r = 0; r < jc; r++)
                {
                    Xx = PointLU.X;
                    for (int c = 0; c < ic; c++)
                    {
                        Zz = (GetZfromPlane(Xx, Yy) - GetZfromPlane(0, 0)) * 200.0;
                        if (Zz > 127.0) Zz = 127.0;
                        if (Zz < -127.0) Zz = -127.0;
                        HC[r, c] = (byte)(Zz + 127.0);
                        Xx += 1.0;  // +1 mm;
                    }
                    Yy -= 1.0;      // -1 mm;
                }

                Dispatcher.Invoke(new Action(delegate
                {
                    PlateColoring(HC);
                }));

                chkdata.bReady = true;

                double GetZfromPlane(double x, double y)
                {
                    double pz = 0;
                    if (chkdata.NormalDir.Z != 0)
                        pz = Ds / chkdata.NormalDir.Z - chkdata.NormalDir.X / chkdata.NormalDir.Z * x - chkdata.NormalDir.Y / chkdata.NormalDir.Z * y;
                    else
                        pz = 0;
                    return pz;
                }
                return chkdata;
            }
            catch (Exception ex)
            {
                chkdata.execResult = ex.HResult;
                chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                chkdata.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-chkdata.execResult).ToString("X2");

                chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                return chkdata;
            }
        }

        public async Task<ITNTResponseArgs> MarkingProcess(byte order)
        {
            string className = "MainWindow";
            string funcName = "MarkingProcess";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string log = "";
            string value = "";
            LASERSTATUS Status = 0;
            short initSpeed = 0;
            short targetSpeed = 0;
            short accelSpeed = 0;
            short decelSpeed = 0;
            string orderstring = "";
            string[] st;
            bool bEmissionOK = false;
            Stopwatch chkEmission = new Stopwatch();
            string sCurrentFunc = "MARKING PROCESS";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                if (order == 2)
                    orderstring = "[4-3]";
                else
                    orderstring = "[2-3]";
#if LASER_OFF
#else

                retval = await PrepareLaserSource(orderstring);
                if (retval.execResult != 0)
                {
                    log = "MARKING ERROR - PrepareLaserSource. (" + retval.execResult.ToString() + ")";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    //ShowLog(className, funcName, 2, "LASER MARKING PREPARATION FAILURE", retval.execResult.ToString());
                    //ITNTErrorCode();

                    return retval;
                }
#endif
                if (currMarkInfo.currMarkData.pattern.laserValue.density != 1)
                {
                    initSpeed = currMarkInfo.currMarkData.pattern.speedValue.initSpeed4MarkV;
                    targetSpeed = currMarkInfo.currMarkData.pattern.speedValue.targetSpeed4MarkV;
                    accelSpeed = currMarkInfo.currMarkData.pattern.speedValue.accelSpeed4MarkV;
                    decelSpeed = currMarkInfo.currMarkData.pattern.speedValue.decelSpeed4MarkV;
                }
                else
                {
                    initSpeed = currMarkInfo.currMarkData.pattern.speedValue.initSpeed4MarkR;
                    targetSpeed = currMarkInfo.currMarkData.pattern.speedValue.targetSpeed4MarkR;
                    accelSpeed = currMarkInfo.currMarkData.pattern.speedValue.accelSpeed4MarkR;
                    decelSpeed = currMarkInfo.currMarkData.pattern.speedValue.decelSpeed4MarkR;
                }

                m_currCMD = (byte)'L';
                retval = await MarkControll.LoadSpeed(m_currCMD, initSpeed, targetSpeed, accelSpeed, decelSpeed);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO CONTROLLER ERROR. (LoadSpeed) : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (LoadSpeed) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    return retval;
                }

                m_currCMD = (byte)'N';
                retval = await MarkControll.SetDensity((short)currMarkInfo.currMarkData.pattern.laserValue.density);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO CONTROLLER ERROR. (SetDensity) : " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (SetDensity) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    return retval;
                }

                // Run Marking
                Stopwatch sw = Stopwatch.StartNew();
                currMarkInfo.senddata.CleanFireFlag = false;    // Clean sending
                ////////////////////////////////////////////////////////////////////////////////////

                //currMarkInfo.currMarkData.pattern.laserValue.
                Util.GetPrivateProfileValue("LASER", "EMISSION", "0", ref value, Constants.PARAMS_INI_FILE);
                if (value != "0")
                {
                    dLaserAveragePower = 0.0d;
                    iLaserPeakPower = 0;
#if LASER_OFF
                    iLaserPowerList = null;
                    iLaserPowerList = new List<int>();
                    //iLaserAvePower = 0;
                    //iLaserMaxPower = 0;
                    //iLaserMinPower = 0;
                    //iLaserCount = 0;
                    byLaserStartFlag = 1;
#else
                    iLaserPowerList = null;
                    iLaserPowerList = new List<int>();

                    iLPMPowerList = null;
                    iLPMPowerList = new List<int>();

                    byLaserStartFlag = 1;

                    retval = await EmissionON();
                    if (retval.execResult != 0)
                    {
                        log = "MARKING ERROR - START EMISSION. (" + retval.execResult.ToString() + ")";
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (StartEmission) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        byLaserStartFlag = 0;
                        return retval;
                    }
#endif
                    ShowRectangle(System.Windows.Media.Brushes.Red, EmissionLamp);
                    StartLabelBlinking();
                }
                ////////////////////////////////////////////////////////////////////////////////////
                ///

                //Thread.Sleep(500);
                //Thread.Sleep(500);

                //Marking Start
                //ShowLog("MARKING - START MARKING");
#if LASER_OFF
                Stopwatch testsw = new Stopwatch();
                testsw.Start();
                while (testsw.ElapsedMilliseconds < 5000)
                {
                    await Task.Delay(50);
                }

#else
                Util.GetPrivateProfileValue("OPTION", "MARKINGLOGLEVEL", "0", ref value, Constants.PARAMS_INI_FILE);
                byte logLevel = 0;
                byte.TryParse(value, out logLevel);
                m_currCMD = (byte)'@';
                retval = await MarkControll.RunStart_S(currMarkInfo, false, logLevel);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO CONTROLLER (RunStart_S) ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (RunStart_S) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    ShowRectangle(System.Windows.Media.Brushes.Black, EmissionLamp);
                    StopLabelBlinking();
                    byLaserStartFlag = 0;
                    return retval;
                }
#endif
                sw.Stop();
                //log = "MARKING TIME : " + sw.ElapsedMilliseconds.ToString();
                //ShowLabelData(log, lblMarkingTime);
                //Thread.Sleep(300);
                //ShowLog(log);
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if LASER_OFF
#else
                retval = await EmissionOFF();
                if (retval.execResult != 0)
                {
                    log = "MARKING ERROR - EMISSION OFF ERROR. (" + retval.execResult.ToString() + ")";
                    //ShowLog(log);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                }
#endif
                ShowRectangle(System.Windows.Media.Brushes.Black, EmissionLamp);
                StopLabelBlinking();
                byLaserStartFlag = 0;

                //if ((bool)CleaningBox.IsChecked && !currMarkInfo.checkdata.ErrorDistanceSensor)
                if ((currMarkInfo.currMarkData.pattern.laserValue.useCleaning != 0) && !currMarkInfo.checkdata.ErrorDistanceSensor)
                {
                    ShowLog(className, funcName, 0, "SATRT CLEANING");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SATRT CLEANING", Thread.CurrentThread.ManagedThreadId);
#if LASER_OFF
#else
#if LASER_YLR_PULSEMODE
                    value = currMarkInfo.currMarkData.pattern.laserValue.waveformClean.ToString();
                    retval = await laserSource.SelectProfile(value);
                    string[] prsel = retval.recvString.Split('[', ']');
                    if (prsel[0] == "PRSEL: ")
                    {
                        string[] sel = prsel[1].Split(':');
                        if (value != sel[0])
                        {
                            log = "MARKING ERROR - Profile setting Error!. (" + sel[0] + ")";
                            //ShowLog(log);
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                            retval.execResult = -6;

                            ShowLog(className, funcName, 2, "CLEANING FAILURE - SelectProfile DATA ERROR", prsel[0]);
                            //ITNTErrorCode();


                            //ShowLog(className, funcName, 2, "CLEANING 실패 - SelectProfile DATA ERROR", retval.execResult.ToString());
                            ////ITNTErrorCode();

                            return retval;
                        }
                    }
                    else
                    {
                        log = "MARKING ERROR - Profile setting Error2!. (" + prsel[0] + ")";
                        //ShowLog(log);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                        retval.execResult = -7;

                        ShowLog(className, funcName, 2, "CLEANING FAILURE - SelectProfile", retval.execResult.ToString());
                        //ITNTErrorCode();

                        return retval;
                    }
#else
                    retval = await laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.cleanPower);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.cleanPower);
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            retval = await laserSource.SetDiodeCurrent(currMarkInfo.currMarkData.pattern.laserValue.cleanPower);
                        }
                    }
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO LASER (SetDiodeCurrent) ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);

                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (SetDiodeCurrent) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }

                    retval = await laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.cleanWidth);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.cleanWidth);
                        if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        {
                            retval = await laserSource.SetPulseWidth(currMarkInfo.currMarkData.pattern.laserValue.cleanWidth);
                        }
                    }
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO LASER (SetPulseWidth) ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (SetPulseWidth) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        return retval;
                    }
#endif
#endif

                    //initSpeed = currMarkInfo.currMarkData.pattern.speedValue.initSpeed4MarkR;
                    //targetSpeed = currMarkInfo.currMarkData.pattern.speedValue.targetSpeed4MarkR;
                    //accelSpeed = currMarkInfo.currMarkData.pattern.speedValue.accelSpeed4MarkR;
                    //decelSpeed = currMarkInfo.currMarkData.pattern.speedValue.decelSpeed4MarkR;

                    Util.GetPrivateProfileValue("LASER", "EMISSION", "0", ref value, Constants.PARAMS_INI_FILE);
                    if (value != "0")
                    {
                        dLaserAveragePower = 0.0d;
                        iLaserPeakPower = 0;

                        m_currCMD = (byte)'L';
                        retval = await MarkControll.LoadSpeed(m_currCMD, currMarkInfo.currMarkData.pattern.speedValue.initSpeed4Clean, currMarkInfo.currMarkData.pattern.speedValue.targetSpeed4Clean, currMarkInfo.currMarkData.pattern.speedValue.accelSpeed4Clean, currMarkInfo.currMarkData.pattern.speedValue.decelSpeed4Clean);
                        if (retval.execResult != 0)
                        {
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            return retval;
                        }

#if LASER_OFF
#else
                        retval = await EmissionON();
                        if (retval.execResult != 0)
                        {
                            retval.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (StartEmission) ERROR = " + retval.execResult.ToString();
                            retval.errorInfo.sErrorFunc = sCurrentFunc;

                            return retval;
                        }
#endif
                        StartLabelBlinking();
                        ShowRectangle(System.Windows.Media.Brushes.Red, EmissionLamp);
                    }

                    //currMarkInfo.senddata.SendDataIndex = 0;
                    Util.GetPrivateProfileValue("OPTION", "MARKINGLOGLEVEL", "0", ref value, Constants.PARAMS_INI_FILE);
                    byte.TryParse(value, out logLevel);
                    currMarkInfo.senddata.CleanFireFlag = true;    // Clean sending

                    m_currCMD = (byte)'@';
                    retval = await MarkControll.RunStart_S(currMarkInfo, true, logLevel);
                    if (retval.execResult != 0)
                    {
                        //if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                        {
                            //retval = await laserSource.StopEmission();
                            //if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            //{
                            //    retval = await laserSource.StopEmission();
                            //    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                            //        retval = await laserSource.StopEmission();
                            //}
                            retval = await EmissionOFF();
                            ShowRectangle(System.Windows.Media.Brushes.Black, EmissionLamp);
                            StopLabelBlinking();
                        }

                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO CONTROLLER (RunStart_S) ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (RunStart_S) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        return retval;
                    }

                    retval = await EmissionOFF();
                    if (retval.execResult != 0)
                    {
                        log = "CLEANIG ERROR - EMISSION OFF ERROR. (" + retval.execResult.ToString() + ")";
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    }
                    ShowRectangle(System.Windows.Media.Brushes.Black, EmissionLamp);
                    StopLabelBlinking();

                    log = "CLEAN TIME : " + sw.ElapsedMilliseconds.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                }

#if LASER_OFF
#else
                //Emission Status CHECK One More Time
                retval = await laserSource.ReadDeviceStatus();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.ReadDeviceStatus();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.ReadDeviceStatus();
                }
                if (retval.execResult == 0)
                {
                    st = retval.recvString.Split(':');
                    Status = (LASERSTATUS)UInt32.Parse(st[1]);
                    if ((Status & LASERSTATUS.EmissionOnOff) != 0)
                    {
                        retval = await EmissionOFF();
                        ShowRectangle(System.Windows.Media.Brushes.Black, EmissionLamp);
                        StopLabelBlinking();
                    }
                }
                else
                {
                    retval = await EmissionOFF();
                    ShowRectangle(System.Windows.Media.Brushes.Black, EmissionLamp);
                    StopLabelBlinking();
                }
#endif
                m_currCMD = (byte)'L';
                retval = await MarkControll.LoadSpeed((byte)'L', currMarkInfo.currMarkData.pattern.speedValue.initSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.targetSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.accelSpeed4Fast, currMarkInfo.currMarkData.pattern.speedValue.decelSpeed4Fast);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    return retval;
                }

                short stepLeng = currMarkInfo.currMarkData.pattern.headValue.stepLength;
                if (stepLeng <= 0) stepLeng = 100;

                m_currCMD = (byte)'M';
                retval = await MarkControll.GoPoint((short)(currMarkInfo.currMarkData.pattern.scanValue.linkPos * stepLeng),           // // ?? TM SHIN
                                                    (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Y * stepLeng + 0.5),
                                                    (short)(currMarkInfo.currMarkData.pattern.headValue.park3DPos.Z * stepLeng + 0.5), 0);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO CONTROLLER (MarkControll) ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (MarkControll) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    return retval;
                }

                sw.Stop();
                log = "TOTAL MARKING TIME : " + sw.ElapsedMilliseconds.ToString();
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Running Time : " + sw.Elapsed, Thread.CurrentThread.ManagedThreadId);

#if MANUAL_MARK
                //ShowCurrentStateLabel(5);
                ShowCurrentStateLabelManual(4);
                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPYELLOW, 0);
                DIOControl.DIOWriteOutportBit(DIO_OUT_LAMPGREEN, 1);
#else
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_FINISH_MARKING);
#endif
                ShowLabelData("[2-4] : MARKING COMPLETE", lblPLCData);
                ShowLabelData("MARKING COMPLETE", lblCheckResult, Brushes.Blue);

                //
                currMarkInfo.currMarkData.mesData.markdate = DateTime.Now.ToString("yyyy-MM-dd");
                currMarkInfo.currMarkData.mesData.marktime = DateTime.Now.ToString("HH:mm:ss");

                //DeletePlanData();
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_SEND_MARKCOMPLETE);
                retval = await plcComm.SendMarkingStatus(PLCMELSEQSerial.PLC_MARK_STATUS_COMPLETE, order);
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "COMMUNICATION TO PLC (SendMarkingStatus) ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (SendMarkingStatus) ERROR = " + retval.execResult.ToString();
                    retval.errorInfo.sErrorFunc = sCurrentFunc;

                    return retval;
                }

                //ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_SEND_MARKCOMPLETE);
                //SaveMarkResultData(currMarkInfo.currMarkData.mesData, 0, 0, currMarkInfo.currMarkData.multiMarkFlag, currMarkInfo.currMarkData.markorderFlag);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Calulate Average Power Start", Thread.CurrentThread.ManagedThreadId);
                int dirty = 0;
                if (iLPMPowerList.Count > 0)
                    dirty = (int)iLPMPowerList.Average();

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Average Power = " + dirty.ToString(), Thread.CurrentThread.ManagedThreadId);
                //try
                //{
                //    SaveLPMData2DB(currMarkInfo.currMarkData.mesData, iLPMPowerList);
                //}
                //catch (Exception ex) { }

                SaveMarkResultData(currMarkInfo.currMarkData.mesData, 0, 0, currMarkInfo.currMarkData.multiMarkFlag, currMarkInfo.currMarkData.markorderFlag, currMarkInfo.checkdata, dirty);
                await WriteCompleteData(currMarkInfo.currMarkData.mesData, 0);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "After WriteCompleteData", Thread.CurrentThread.ManagedThreadId);
#if AGING_TEST_DATA
#else
                UpdateCompleteDatabaseThread(dgdPlanData, true, 0);
#endif
                //await ShowCurrentSequenceVIN(0, currMarkInfo.currMarkData, (byte)DISPLAY_INFO_COLOR_TYPE.DISP_COLOR_COMPLETE, 1);

                //await ShowCurrentMarkingInformation2(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 1, 1);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "After UpdateCompleteDatabaseThread", Thread.CurrentThread.ManagedThreadId);

                m_bDoingMarkingFlag = false;
                currMarkInfo.checkdata.bReady = false;
                currMarkInfo.senddata.bReady = false;

                //Util.WritePrivateProfileValue("CURRENT", "VIN", currMarkInfo.currMarkData.mesData.vin.Trim(), Constants.DATA_CUR_COMPLETE_FILE);
                Util.WritePrivateProfileValue("CURRENT", "SEQVIN", currMarkInfo.currMarkData.mesData.sequence.Trim() + "|" + currMarkInfo.currMarkData.mesData.rawvin.Trim(), Constants.DATA_CUR_COMPLETE_FILE);

                //currMarkInfo.Initialize();
                //ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[4] : MARKING COMPLETE");
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[4] : MARKING COMPLETE", Thread.CurrentThread.ManagedThreadId);
                //ITNTJobLog.Instance.Trace(0, "[4] : MARKING COMPLETE");
                markRunTimer.Stop();
                //ShowCurrentStateLabel(7);
                m_currCMD = 0;

                //if (mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INST_COMPLETE)
                //{
                //    await ChangeDBProcess4Thread();
                //}

                //if(mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_RECV_COMPLETE)
                //{
                //    Thread ccr2workThread = new Thread(new ParameterizedThreadStart(CCR2WORK2));
                //    ccr2workThread.Start(dgdPlanData);
                //}
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Before Mark Count", Thread.CurrentThread.ManagedThreadId);

                int ivalue = 0;
                ivalue = (int)Util.GetPrivateProfileValueUINT("OPTION", "MarkingCount", 0, Constants.PARAMS_INI_FILE);
                ivalue++;
                value = ivalue.ToString();
                ShowLabelData(value, lblMarkingCount);
                Util.WritePrivateProfileValue("OPTION", "MarkingCount", value, Constants.PARAMS_INI_FILE);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "After Mark Count", Thread.CurrentThread.ManagedThreadId);

                ShowLog(className, funcName, (byte)LOGTYPE.LOG_NORMAL, "[2-4] MARKING COMPLETE", "");
                ShowCurrentStateLabel((byte)STATUS_LABEL_NUM.STATUS_LABEL_WAIT_VISION);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                return retval;
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        public async Task<CheckAreaData> Range_Test2(string vin, PatternValueEx pattern, bool mark6 = false)   // Plating  by TM SHIN $$$
        {
            string className = "MainWindow";
            string funcName = "Range_Test";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            distanceSensorData snsdata = new distanceSensorData();
            CheckAreaData chkdata = new CheckAreaData();

            int vinLength = 17;
            double totWidth = 0;

            Vector3D SP0 = new Vector3D();
            Vector3D SP1 = new Vector3D();
            Vector3D CP = new Vector3D();

            Vector3D PointLU = new Vector3D();
            Vector3D PointLD = new Vector3D();
            Vector3D PointRU = new Vector3D();

            Vector3D[] vCheckPos = new Vector3D[7];
            double[] HeightVal = new double[7];

            Vector3D vector3 = new Vector3D();

            double HeightCT0 = 0;
            string value = "";
            string log = "";
            //byte bHeadType = 0;

            List<Vector3D> planePoints = new List<Vector3D>();

            short gMinX = 0;
            short gMaxX = 0;
            short gMinY = 0;
            short gMaxY = 0;

            //short? CenterX = null;
            //short? CenterY = null;
            short? CenterZ = null;
            double dSlope = 0;
            string sCurrentFunc = "PLATE CHECK";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                ShowLog(className, funcName, 0, "[PLATE CHECK] START");

                Util.GetPrivateProfileValue("CONFIG", "SKIPCHECKPLANE", "0", ref value, Constants.SCANNER_INI_FILE);
                if (value != "0")
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SKIP CHECK PLANE", Thread.CurrentThread.ManagedThreadId);
                    chkdata.NormalDir.X = 0;
                    chkdata.NormalDir.Y = 0;
                    chkdata.NormalDir.Z = 1;
                    chkdata.bReady = true;
                    chkdata.execResult = 0;
                    ShowLog(className, funcName, 0, "[PLATE CHECK] SKIP");
                    return chkdata;
                }

                vinLength = vin.Length;
                if (vinLength <= 0)
                {
                    log = "ERROR : VIN LENGTH <= 0 (" + vinLength.ToString() + ")";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = ErrorCodeConstant.ERROR_PARAM_INVALID;
                    chkdata.errorInfo.sErrorMessage = "VIN IS EMPTY";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_DISTACE + Constants.ERROR_PARAM + Constants.ERROR_INVALID;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_DISTACE;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_DISTACE;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                    return chkdata;
                }

                totWidth = pattern.fontValue.pitch * (vinLength - 1) + pattern.fontValue.width;

                // SET Motor Speed
                m_currCMD = (byte)'L';
                retval = await MarkControll.LoadSpeed(m_currCMD, pattern.speedValue.initSpeed4Measure, pattern.speedValue.targetSpeed4Measure, pattern.speedValue.accelSpeed4Measure, pattern.speedValue.decelSpeed4Measure);
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - SET MOTOR SPEED(MEASURE) ERROR = " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = retval.execResult;
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO CONTROLLER (LoadSpeed) ERROR = " + retval.execResult.ToString();
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    return chkdata;
                }

                // Laser Aiming Beam ON
                retval = await laserSource.SetExternalAimingBeamControll(0);
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.SetExternalAimingBeamControll(0);
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.SetExternalAimingBeamControll(0);
                }
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - SET BEAM CONTROLL ERROR : " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = retval.execResult;
                    chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                    chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (SetExternalAimingBeamControll) ERROR = " + retval.execResult.ToString();
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;

                    return chkdata;
                }

                retval = await laserSource.AimingBeamON();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.AimingBeamON();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                        retval = await laserSource.AimingBeamON();
                }
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM ON ERROR : " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //chkdata.execResult = retval.execResult;
                    //chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();

                    //chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (AimingBeamON) ERROR = " + retval.execResult.ToString();
                    //chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    //return chkdata;
                }

                ShowRectangle(System.Windows.Media.Brushes.Black, AimingLamp);

                planePoints.Clear();
                currMarkInfo.senddata.Clear();

                // Absolute mm of Center Point
                CP = pattern.positionValue.center3DPos;
                SP0.X = totWidth / 2;
                SP0.Y = pattern.fontValue.height / 2;

                SP1 = CP - SP0;
                SP1.Z = CP.Z;

                // ABS mm
                double MinX = SP1.X;
                double MaxX = SP1.X + totWidth;
                double MinY = SP1.Y;
                double MaxY = SP1.Y + pattern.fontValue.height;

                // ABS BLU
                gMinX = (short)(MinX * pattern.headValue.stepLength + 0.5);
                gMaxX = (short)(MaxX * pattern.headValue.stepLength + 0.5);
                gMinY = (short)(MinY * pattern.headValue.stepLength + 0.5);
                gMaxY = (short)(MaxY * pattern.headValue.stepLength + 0.5);

                // ABS mm
                double CX = (MaxX + MinX) / 2.0;
                double CY = (MaxY + MinY) / 2.0;

                // ABS BLU
                short tCX = (short)(CX * pattern.headValue.stepLength + 0.5);
                short tCY = (short)(CY * pattern.headValue.stepLength + 0.5);
                CenterZ = (short)(CP.Z * pattern.headValue.stepLength + 0.5);
                vector3 = new Vector3D(tCX, tCY, (short)(pattern.headValue.park3DPos.Z * pattern.headValue.stepLength + 0.5));
                snsdata = await GetMeasureLength(vector3, 0, 1);
                if (snsdata.execResult != 0)
                {
                    //retval.execResult = snsdata.execResult;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    chkdata.execResult = snsdata.execResult;
                    chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();
                    //chkdata.sErrorMessage = "GetMeasureLength(CT) : ERROR = " + snsdata.execResult.ToString();

                    //ITNTErrorCode();
                    //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength", snsdata.execResult.ToString());

                    return chkdata;
                }

                chkdata.checkdistance[0] = snsdata.rawdistance;
                ShowLabelData(snsdata.rawdistance.ToString("F3"), lblDispHeightValue);
                ShowLabelData(snsdata.sensoroffset.ToString("F3"), lblDispHeightCosine);

                double ShiftCT = snsdata.sensorshift;
                double HeightCT = snsdata.sensoroffset;

                if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)      // Z Diff Max. 60mm
                {
                    chkdata.ErrorDistanceSensor = true;

                    ShowLabelData(HeightCT.ToString("0.000"), lblDispCenXCenY, System.Windows.Media.Brushes.Red, null, null);
                    ShowLabelData("", lblDispMinXMaxY);
                    ShowLabelData("", lblDispMinXMinY);
                    ShowLabelData("", lblDispMaxXMaxY);
                    ShowLabelData("", lblDispMaxXMinY);
                    ShowLabelData("", lblDispCenXMaxY);
                    ShowLabelData("", lblDispCenXMinY);

                    //retval.execResult = -2;
                    log = "CENTER Z RANGE(" + HeightCT.ToString("F4") + ") IS OVER THAN STANDARD(" + pattern.positionValue.checkDistanceHeight.ToString("F4") + ")";
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR;
                    chkdata.errorInfo.sErrorMessage = "DISTANCE FROM MARKING HEAD TO PLATE IS TOO FAR.";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_Z_HEIGHT;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;
                    return chkdata;
                }

                if (pattern.headValue.sensorPosition == 0)  // RIGHT
                {
                    gMinX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
                        gMinX = pattern.headValue.stepLength;                                   // && 7

                    gMaxX -= (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
                        gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
                }
                else                                       // LEFT
                {
                    gMinX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMinX < pattern.headValue.stepLength) // 1mm                            // && 7
                        gMinX = pattern.headValue.stepLength;
                    gMaxX += (short)(ShiftCT * pattern.headValue.stepLength + 0.5);
                    if (gMaxX > (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength))    // MAX_X - 1mm   // && 7
                        gMaxX = (short)(pattern.headValue.max_X * pattern.headValue.stepLength - pattern.headValue.stepLength);                     // && 7
                }

                // Sensor Shift compensation
                ShowLabelData(HeightCT.ToString("0.000"), lblDispCenXCenY, System.Windows.Media.Brushes.Black, null, null);
                ShowLabelData("", lblDispMinXMaxY);
                ShowLabelData("", lblDispMinXMinY);
                ShowLabelData("", lblDispMaxXMaxY);
                ShowLabelData("", lblDispMaxXMinY);
                ShowLabelData("", lblDispCenXMaxY);
                ShowLabelData("", lblDispCenXMinY);

                tCX = (short)(((double)gMaxX + (double)gMinX) / 2.0 + 0.5);
                tCY = (short)(((double)gMaxY + (double)gMinY) / 2.0 + 0.5);

                Vector3D[] vAddPos = new Vector3D[7];
                if (pattern.positionValue.plateMode == 0)
                {
                    HeightCT = 0.0;              // && 7

                    vAddPos[0].X = MinX - CP.X; vAddPos[0].Y = MaxY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]); // && 7
                    vAddPos[0].X = MinX - CP.X; vAddPos[0].Y = MinY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]);
                    vAddPos[0].X = CX - CP.X; vAddPos[0].Y = MinY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]);
                    vAddPos[0].X = MaxX - CP.X; vAddPos[0].Y = MinY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]);
                    vAddPos[0].X = MaxX - CP.X; vAddPos[0].Y = MaxY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]);
                    vAddPos[0].X = CX - CP.X; vAddPos[0].Y = MaxY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]);
                    vAddPos[0].X = CX - CP.X; vAddPos[0].Y = CY - CP.Y; vAddPos[0].Z = 0.0; planePoints.Add(vAddPos[0]); // && 7
                }
                else if (pattern.positionValue.plateMode == 1)
                {
                    Vector3D v3d = new Vector3D();
                    v3d.X = tCX; v3d.Y = tCY; v3d.Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                    snsdata = await GetMeasureLength(v3d, 0, 1);
                    if (snsdata.execResult != 0)
                    {
                        //retval.execResult = snsdata.execResult;
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(CC) : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        chkdata.execResult = snsdata.execResult;
                        chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                        //ITNTErrorCode();
                        //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                        return chkdata;
                    }
                    //if (snsdata.execResult != 0)
                    //{
                    //    retval.execResult = snsdata.execResult;
                    //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(LU) : ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                    //    return retval;
                    //}

                    HeightCT = snsdata.sensoroffset;
                    if (Math.Abs(HeightCT) > pattern.positionValue.checkDistanceHeight)
                    {
                        chkdata.ErrorDistanceSensor = true;
                        //retval.sErrorMessage = "";
                    }
                    //if (Math.Abs(Height) > pattern.positionValue.checkDistanceHeight)
                    //{
                    //    retval.ErrorDistanceSensor = true;
                    //    retval.errorInfo.sErrorMessage = "";
                    //}

                    vAddPos[0].X = 0; vAddPos[0].Y = 0; vAddPos[0].Z = Height; planePoints.Add(vAddPos[0]);

                    ShowLabelData(HeightCT.ToString("F3"), lblDispCenXCenY, Brushes.Blue);

                    vAddPos[1].X = MinX - CP.X; vAddPos[1].Y = MaxY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]); // && 7
                    vAddPos[2].X = MinX - CP.X; vAddPos[1].Y = MinY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]);
                    vAddPos[3].X = CX - CP.X; vAddPos[1].Y = MinY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]);
                    vAddPos[4].X = MaxX - CP.X; vAddPos[1].Y = MinY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]);
                    vAddPos[5].X = MaxX - CP.X; vAddPos[1].Y = MaxY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]);
                    vAddPos[6].X = CX - CP.X; vAddPos[1].Y = MaxY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]);
                    vAddPos[7].X = CX - CP.X; vAddPos[1].Y = CY - CP.Y; vAddPos[1].Z = 0.0; planePoints.Add(vAddPos[1]); // && 7

                }
                else if (pattern.positionValue.plateMode == 3)
                {
                    double[] vCheckPosX = new double[] { tCX, gMinX, gMaxX };
                    double[] vCheckPosY = new double[] { gMinY, gMaxY, gMaxY };

                    HeightCT = 0;
                    double[] vAddPosX = new double[] { CX - CP.X, MinX - CP.X, MaxX - CP.X };
                    double[] vAddPosY = new double[] { MinY - CP.Y, MaxY - CP.Y, MaxY - CP.Y };
                    string[] sPosition = new string[] { "CD", "LU", "RU" };
                    Label[] lblValue = new Label[] { lblDispCenXMinY, lblDispMinXMaxY, lblDispMaxXMaxY };

                    for (int i = 0; i < 3; i++)
                    {
                        vCheckPos[i].X = vCheckPosX[i];
                        vCheckPos[i].Y = vCheckPosY[i];
                        vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                        snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            //retval.execResult = snsdata.execResult;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                            //ITNTErrorCode();
                            //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                            return chkdata;
                        }

                        HeightVal[i] = snsdata.sensoroffset;
                        if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        {
                            chkdata.ErrorDistanceSensor = true;
                            //retval.sErrorMessage = "";
                        }

                        vAddPos[i].X = vAddPosX[i]; vAddPos[i].Y = vAddPosY[i]; vAddPos[i].Z = HeightVal[i]; planePoints.Add(vAddPos[i]);
                        ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
                        //HeightCT = HeightVal[i];    // TM SHIN
                    }
                }
                else if (pattern.positionValue.plateMode == 5)
                {
                    double[] vCheckPosX = new double[] { gMinX, gMinX, gMaxX, gMaxX, tCX };
                    double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMaxY, tCY };


                    double[] vAddPosX = new double[] { MinX - CP.X, MinX - CP.X, MaxX - CP.X, MaxX - CP.X, CX - CP.X };
                    double[] vAddPosY = new double[] { MaxY - CP.Y, MinY - CP.Y, MinY - CP.Y, MaxY - CP.Y, CY - CP.Y };
                    string[] sPosition = new string[] { "LU", "LD", "RD", "RU", "CC" };
                    Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXCenY };

                    for (int i = 0; i < 5; i++)
                    {
                        vCheckPos[i].X = vCheckPosX[i];
                        vCheckPos[i].Y = vCheckPosY[i];
                        vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                        snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            //retval.execResult = snsdata.execResult;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                            //ITNTErrorCode();
                            //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                            return chkdata;
                        }

                        HeightVal[i] = snsdata.sensoroffset;
                        if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        {
                            chkdata.ErrorDistanceSensor = true;
                            //retval.sErrorMessage = "";
                        }

                        vAddPos[i].X = vAddPosX[i]; vAddPos[i].Y = vAddPosY[i]; vAddPos[i].Z = snsdata.sensoroffset; planePoints.Add(vAddPos[i]);
                        ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
                        Height = HeightVal[i];    // TM SHIN
                    }
                    HeightCT = HeightVal[4];
                }
                else if (pattern.positionValue.plateMode == 7)
                {
                    Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispCenXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXMaxY, lblDispCenXCenY };

                    double[] vCheckPosX = new double[] { gMinX, gMinX, tCX, gMaxX, gMaxX, tCX, tCX };
                    double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMinY, gMaxY, gMaxY, tCY };

                    double[] vAddPosX = new double[] { MinX - CP.X, MinX - CP.X, CX - CP.X, MaxX - CP.X, MaxX - CP.X, CX - CP.X, CX - CP.X };
                    double[] vAddPosY = new double[] { MaxY - CP.Y, MinY - CP.Y, MinY - CP.Y, MinY - CP.Y, MaxY - CP.Y, MaxY - CP.Y, CY - CP.Y };
                    string[] sPosition = new string[] { "LU", "LD", "CD", "RD", "RU", "CU", "CC" };

                    for (int i = 0; i < 7; i++)
                    {
                        vCheckPos[i].X = vCheckPosX[i];
                        vCheckPos[i].Y = vCheckPosY[i];
                        vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                        snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            //retval.execResult = snsdata.execResult;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                            //ITNTErrorCode();
                            //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                            return chkdata;
                        }
                        //if (snsdata.execResult != 0)
                        //{
                        //    retval.execResult = snsdata.execResult;
                        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(LU) : ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        //    return retval;
                        //}

                        vAddPos[i].X = vAddPosX[i];
                        vAddPos[i].Y = vAddPosY[i];
                        vAddPos[i].Z = snsdata.sensoroffset;
                        HeightVal[i] = snsdata.sensoroffset;
                        if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        {
                            chkdata.ErrorDistanceSensor = true;
                            //retval.sErrorMessage = "";
                        }

                        //if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        //{
                        //    retval.ErrorDistanceSensor = true;
                        //    retval.errorInfo.sErrorMessage = "";
                        //}

                        planePoints.Add(vAddPos[i]);
                        ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
                        HeightCT = HeightVal[i];    // TM SHIN
                    }
                    HeightCT = HeightVal[6];
                }
                else
                {
                    Label[] lblValue = new Label[] { lblDispMinXMaxY, lblDispMinXMinY, lblDispCenXMinY, lblDispMaxXMinY, lblDispMaxXMaxY, lblDispCenXMaxY, lblDispCenXCenY };

                    double[] vCheckPosX = new double[] { gMinX, gMinX, tCX, gMaxX, gMaxX, tCX, tCX };
                    double[] vCheckPosY = new double[] { gMaxY, gMinY, gMinY, gMinY, gMaxY, gMaxY, tCY };

                    double[] vAddPosX = new double[] { MinX - CP.X, MinX - CP.X, CX - CP.X, MaxX - CP.X, MaxX - CP.X, CX - CP.X, CX - CP.X };
                    double[] vAddPosY = new double[] { MaxY - CP.Y, MinY - CP.Y, MinY - CP.Y, MinY - CP.Y, MaxY - CP.Y, MaxY - CP.Y, CY - CP.Y };
                    string[] sPosition = new string[] { "LU", "LD", "CD", "RD", "RU", "CU", "CC" };

                    for (int i = 0; i < 7; i++)
                    {
                        vCheckPos[i].X = vCheckPosX[i];
                        vCheckPos[i].Y = vCheckPosY[i];
                        vCheckPos[i].Z = pattern.headValue.park3DPos.Z * pattern.headValue.stepLength;

                        snsdata = await GetMeasureLength(vCheckPos[i], 0, 1);
                        if (snsdata.execResult != 0)
                        {
                            //retval.execResult = snsdata.execResult;
                            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(+" + sPosition[i] + ") : ERROR = " + snsdata.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                            chkdata.execResult = snsdata.execResult;
                            chkdata.errorInfo = (ErrorInfo)snsdata.errorInfo.Clone();

                            //ITNTErrorCode();
                            //ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - GetMeasureLength " + i.ToString(), snsdata.execResult.ToString());

                            return chkdata;
                        }
                        //if (snsdata.execResult != 0)
                        //{
                        //    retval.execResult = snsdata.execResult;
                        //    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "GetMeasureLength(LU) : ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        //    return retval;
                        //}

                        vAddPos[i].X = vAddPosX[i];
                        vAddPos[i].Y = vAddPosY[i];
                        vAddPos[i].Z = snsdata.sensoroffset;
                        HeightVal[i] = snsdata.sensoroffset;
                        if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        {
                            chkdata.ErrorDistanceSensor = true;
                            //retval.sErrorMessage = "";
                        }

                        //if (Math.Abs(HeightVal[i]) > pattern.positionValue.checkDistanceHeight)
                        //{
                        //    retval.ErrorDistanceSensor = true;
                        //    retval.errorInfo.sErrorMessage = "";
                        //}

                        planePoints.Add(vAddPos[i]);
                        ShowLabelData(HeightVal[i].ToString("0.000"), lblValue[i], Brushes.Blue);
                        HeightCT = HeightVal[i];    // TM SHIN
                    }
                    HeightCT = HeightVal[6];    // TM SHIN
                }

                retval = await laserSource.AimingBeamOFF();
                if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                {
                    retval = await laserSource.AimingBeamOFF();
                    if ((retval.execResult == (int)COMMUNICATIONERROR.ERR_COMMAND_BUSY) || (retval.execResult == (int)COMMUNICATIONERROR.ERR_CMD_MISSMATCH))
                    {
                        retval = await laserSource.AimingBeamOFF();
                    }
                }
                if (retval.execResult != 0)
                {
                    log = "CHECK PLATE FAIL - BEAM OFF ERROR : " + retval.execResult.ToString();
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, log, Thread.CurrentThread.ManagedThreadId);
                    //chkdata.execResult = retval.execResult;

                    ////ITNTErrorCode();
                    ////ShowLog(className, funcName, 2, "[PLATE CHECK] ERROR - AimingBeamOFF", chkdata.execResult.ToString());
                    ////ShowLog(className, funcName, 2, log);
                    //chkdata.errorInfo = (ErrorInfo)retval.errorInfo.Clone();
                    //chkdata.errorInfo.sErrorMessage = "COMMUNICATION TO LASER (AimingBeamOFF) ERROR = " + retval.execResult.ToString(); ;
                    //chkdata.errorInfo.sErrorFunc = sCurrentFunc;

                    //return chkdata;
                }

                ShowRectangle(System.Windows.Media.Brushes.Black, AimingLamp);

                if (chkdata.ErrorDistanceSensor)
                {
                    Vector3D TmpPoint = new Vector3D();
                    planePoints.Clear();

                    HeightCT0 = HeightCT;
                    HeightCT = pattern.positionValue.checkDistanceHeight;

                    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MinX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MinY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = MaxX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = MaxY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);
                    TmpPoint.X = CX - CP.X; TmpPoint.Y = CY - CP.Y; TmpPoint.Z = pattern.positionValue.checkDistanceHeight; planePoints.Add(TmpPoint);

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over : " + HeightCT.ToString("0.000"), Thread.CurrentThread.ManagedThreadId);

                    chkdata.execResult = ErrorCodeConstant.ERROR_Z_HEIGHT_ERROR;
                    //chkdata.errorInfo.sErrorMessage = "DISTANCE FROM MARKING HEAD TO PLATE IS TOO FAR.";
                    chkdata.errorInfo.sErrorMessage = "CHECK POINT Z RANGE(" + HeightCT.ToString("F4") + ") IS OVER THAN STANDARD(" + pattern.positionValue.checkDistanceHeight.ToString("F4") + ")";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_Z_HEIGHT;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                    return chkdata;
                }

                //HeightCT = HeightVal[6];    // TM SHIN

                //CenterZ += (short)(HeightCT * pattern.headValue.stepLength + 0.5);

                //// REL mm at 4 Corners, CD
                PointLU.X = SP1.X - CP.X;
                PointLU.Y = SP1.Y - CP.Y + pattern.fontValue.height;
                PointLD.Y = SP1.Y - CP.Y;
                PointRU.X = SP1.X - CP.X + totWidth;
                ////
                Vector3D Sum = new Vector3D();
                foreach (var mPoint in planePoints)
                {
                    Sum.X += mPoint.X; Sum.Y += mPoint.Y; Sum.Z += mPoint.Z;
                }
                Vector3D Centroid = new Vector3D();
                Centroid.X = Sum.X / planePoints.Count;
                Centroid.Y = Sum.Y / planePoints.Count;
                Centroid.Z = Sum.Z / planePoints.Count;
                double xx, xy, xz, yy, yz, zz;
                xx = xy = xz = yy = yz = zz = 0.0;
                foreach (var mPoint in planePoints)
                {
                    xx += (mPoint.X - Centroid.X) * (mPoint.X - Centroid.X);
                    xy += (mPoint.X - Centroid.X) * (mPoint.Y - Centroid.Y);
                    xz += (mPoint.X - Centroid.X) * (mPoint.Z - Centroid.Z);
                    yy += (mPoint.Y - Centroid.Y) * (mPoint.Y - Centroid.Y);
                    yz += (mPoint.Y - Centroid.Y) * (mPoint.Z - Centroid.Z);
                    zz += (mPoint.Z - Centroid.Z) * (mPoint.Z - Centroid.Z);
                }
                chkdata.NormalDir.X = xy * yz - xz * yy;
                chkdata.NormalDir.Y = xy * xz - yz * xx;
                chkdata.NormalDir.Z = xx * yy - xy * xy;

                double Ds = chkdata.NormalDir.X * Centroid.X + chkdata.NormalDir.Y * Centroid.Y + chkdata.NormalDir.Z * Centroid.Z;

                double PlaneLU = GetZfromPlane(PointLU.X, PointLU.Y);
                double PlaneLD = GetZfromPlane(PointLU.X, PointLD.Y);
                double PlaneRU = GetZfromPlane(PointRU.X, PointLU.Y);
                double PlaneRD = GetZfromPlane(PointRU.X, PointLD.Y);
                double PlaneCU = GetZfromPlane(0, PointLU.Y);
                double PlaneCD = GetZfromPlane(0, PointLD.Y);
                chkdata.PlaneCenterZ = GetZfromPlane(0, 0);

                CenterZ = (short)((CP.Z + pattern.headValue.distance0Position + chkdata.PlaneCenterZ) * (double)pattern.headValue.stepLength + 0.5);              // BLU // && 8

                double PdiffLU, PdiffLD, PdiffRD, PdiffRU, PdiffCU, PdiffCD;
                PdiffLU = PlaneLU - chkdata.PlaneCenterZ;
                PdiffLD = PlaneLD - chkdata.PlaneCenterZ;
                PdiffRU = PlaneRU - chkdata.PlaneCenterZ;
                PdiffRD = PlaneRD - chkdata.PlaneCenterZ;
                PdiffCU = PlaneCU - chkdata.PlaneCenterZ;
                PdiffCD = PlaneCD - chkdata.PlaneCenterZ;
                double PminDiff = Math.Min(Math.Min(Math.Min(Math.Min(Math.Min(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
                double PmaxDiff = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(PdiffLU, PdiffLD), PdiffRU), PdiffRD), PdiffCU), PdiffCD);
                double PdiffDiff = Math.Abs(PmaxDiff - PminDiff);

                Util.GetPrivateProfileValue("OPTION", "SLOPE", "1.0", ref value, Constants.MARKING_INI_FILE);
                double.TryParse(value, out dSlope);

                if (PdiffDiff > dSlope)
                {
                    log = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
                    chkdata.execResult = -5;

                    // Error handling required!!
                    //chkdata.sErrorMessage = string.Format("ERROR => Too much inclined Plate : {0:F3} mm", PdiffDiff);
                    //log = "PLATE SLOPE(" + PdiffDiff.ToString("F3") + ") IS OVER THAN STANDARD(" + dSlope.ToString("F3") + ")";
                    //ShowLog(className, funcName, 2, log);


                    //chkdata.errorInfo.sErrorFunc = sCurrentFunc;


                    chkdata.execResult = ErrorCodeConstant.ERROR_SLOPE_ERROR;
                    chkdata.errorInfo.sErrorMessage = "PLATE SLOPE(" + PdiffDiff.ToString("F3") + ") IS OVER THAN TOLERANCE(" + dSlope.ToString("F3") + ")";
                    chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_XXXX + Constants.ERROR_SLOPE_BIG;

                    chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                    chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "CENTER Z Range over : " + HeightCT.ToString("0.000"), Thread.CurrentThread.ManagedThreadId);

                    return chkdata;
                }

                ShowLabelData(chkdata.PlaneCenterZ.ToString("0.000;-0.000;0.000"), lblDispCenXCenY, (chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.DarkGreen);
                ShowLabelData(PdiffLU.ToString("+ 0.000;- 0.000;0.000"), lblDispMinXMaxY, (PdiffLU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffLD.ToString("+ 0.000;- 0.000;0.000"), lblDispMinXMinY, (PdiffLD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffRU.ToString("+ 0.000;- 0.000;0.000"), lblDispMaxXMaxY, (PdiffRU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffRD.ToString("+ 0.000;- 0.000;0.000"), lblDispMaxXMinY, (PdiffRD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffCU.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMaxY, (PdiffCU > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);
                ShowLabelData(PdiffCD.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMinY, (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.Blue);

                //ShowLabelData(PdiffCD.ToString("+ 0.000;- 0.000;0.000"), lblDispCenXMinY, (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? Brushes.Red : Brushes.LightSkyBlue);

                //Dispatcher.Invoke(new Action(delegate
                //{
                //    lblDispCenXCenY.Foreground = (chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.DarkGreen;
                //    lblDispCenXCenY.Content = chkdata.PlaneCenterZ.ToString("0.000;-0.000;0.000");

                //    lblDispMinXMaxY.Foreground = (PdiffLU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMinXMaxY.Content = PdiffLU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMinXMinY.Foreground = (PdiffLD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMinXMinY.Content = PdiffLD.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMaxXMaxY.Foreground = (PdiffRU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue;
                //    lblDispMaxXMaxY.Content = PdiffRU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispMaxXMinY.Foreground = (PdiffRD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispMaxXMinY.Content = PdiffRD.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispCenXMaxY.Foreground = (PdiffCU > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispCenXMaxY.Content = PdiffCU.ToString("+ 0.000;- 0.000;0.000");

                //    lblDispCenXMinY.Foreground = (PdiffCD > 0 || chkdata.ErrorDistanceSensor) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LightSkyBlue; ;
                //    lblDispCenXMinY.Content = PdiffCD.ToString("+ 0.000;- 0.000;0.000");
                //}
                //));

                int jc = (int)((PointLU.Y - PointLD.Y) + 0.5);
                int ic = (int)((PointRU.X - PointLU.X) + 0.5);
                byte[,] HC = new byte[jc, ic];

                double Yy = PointLU.Y;
                double Xx = PointLU.X;
                double Zz = 0;
                for (int r = 0; r < jc; r++)
                {
                    Xx = PointLU.X;
                    for (int c = 0; c < ic; c++)
                    {
                        Zz = (GetZfromPlane(Xx, Yy) - GetZfromPlane(0, 0)) * 200.0;
                        if (Zz > 127.0) Zz = 127.0;
                        if (Zz < -127.0) Zz = -127.0;
                        HC[r, c] = (byte)(Zz + 127.0);
                        Xx += 1.0;  // +1 mm;
                    }
                    Yy -= 1.0;      // -1 mm;
                }

                Dispatcher.Invoke(new Action(delegate
                {
                    PlateColoring(HC);
                }));

                chkdata.bReady = true;

                double GetZfromPlane(double x, double y)
                {
                    double pz = 0;
                    if (chkdata.NormalDir.Z != 0)
                        pz = Ds / chkdata.NormalDir.Z - chkdata.NormalDir.X / chkdata.NormalDir.Z * x - chkdata.NormalDir.Y / chkdata.NormalDir.Z * y;
                    else
                        pz = 0;
                    return pz;
                }
                return chkdata;
            }
            catch (Exception ex)
            {
                chkdata.execResult = ex.HResult;
                chkdata.errorInfo.sErrorFunc = sCurrentFunc;
                chkdata.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                chkdata.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-chkdata.execResult).ToString("X2");

                chkdata.errorInfo.devErrorInfo.execResult = chkdata.execResult;
                chkdata.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                chkdata.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                chkdata.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                chkdata.errorInfo.devErrorInfo.sErrorMessage = chkdata.errorInfo.sErrorMessage;

                return chkdata;
            }
        }

        private async Task SaveLPMData2DB(MESReceivedData mesData, List<int> dirty)
        {
            string className = "MainWindow";
            string funcName = "SaveLPMData2DB";

            int avg = 0;
            try
            {
                if(dirty.Count > 0)
                    avg = (int)dirty.Average();
                //_currentEpoch = await _db.GetResetEpochAsync();
                //await _db.InsertAverageAsync(DateTime.Now, mesData.cartype, mesData.rawvin, avg, _currentEpoch, dirty);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }

        }

        private async Task<ITNTResponseArgs> CheckCarTypeProcess2(MESReceivedData mesdata, string sProcess)
        {
            string className = "MainWindow";
            string funcName = "CheckCarTypeProcess2";

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            int cartypeOption = 0;
            string plcFrameType = "";
            string plcCaption = "";
            string mesFrameType = "";
            string mesCaption = "";
            string scartyperead = "";
            string value = "";
            string sCurrentFunc = "CHECK CAR TYPE";
            string sProcedure = "1";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (mesdata.userDataType != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SKIP FLAG SET", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = 0;
                    return retval;
                }

                cartypeOption = (int)Util.GetPrivateProfileValueUINT("OPTION", "CARTYPECOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                if (cartypeOption == 5) //HMI 3
                {
                    retval = await plcComm.ReadBodyNum();
                    if (retval.execResult != 0)
                    {
                        //retval = await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadBodyNum ERROR = " + retval.execResult, Thread.CurrentThread.ManagedThreadId);
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadBodyNum) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                        return retval;
                    }

                    //recvArg.recvString = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadBodyNum {0}", retval.recvString), Thread.CurrentThread.ManagedThreadId);

                    mesCaption = mesdata.bodyno;
                    plcCaption = retval.recvString;

                    mesFrameType = mesCaption;
                    plcFrameType = plcCaption;
                }
                else
                {
#if AGING_TEST_PLC
#else
                    retval = await plcComm.ReadPLCCarType();
                    if (retval.execResult != 0)
                    {
                        //retval = await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadPLCCarType ERROR = " + retval.execResult, Thread.CurrentThread.ManagedThreadId);
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadPLCCarType) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                        return retval;
                    }

                    //recvArg.recvString = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadPLCCarType {0}", retval.recvString), Thread.CurrentThread.ManagedThreadId);
                    if (retval.recvString.Length < 8)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("COMMUNICATION ERROR TO PLC - SHORT LENGTH {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);

                        retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                        retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + retval.recvString.Length + " - " + retval.recvString;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                        retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                        retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                        retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                        retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                        ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                        return retval;
                    }

                    scartyperead = retval.recvString.Substring(4, 4);
                    plcFrameType = scartyperead.Trim();
#endif

                    //cartypeOption = (int)Util.GetPrivateProfileValueUINT("OPTION", "CARTYPECOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                    if (cartypeOption == 0)     //HMC U42
                    {
                        //BODY TYPE(MES)
                        mesFrameType = GetFrameType4MES(mesdata.rawcartype.Trim(), mesdata.rawbodytype.Trim(), mesdata.rawtrim.Trim(), "");
                        mesCaption = GetFrameTypeDescription(mesFrameType, 1);
                        plcFrameType = plcFrameType.Replace("0", "");
                        plcCaption = GetFrameTypeDescription(plcFrameType);

#if AGING_TEST_PLC
                        plcFrameType = mesFrameType;
#endif
                    }
                    else if (cartypeOption == 1)    //KIA
                    {
                        //plcCaption = GetCarTypeDescription(plcFrameType);
                        //ShowLabelData(plcCaption, lblPLCCARTYPEValue);

                        //BODY TYPE(MES)
                        mesFrameType = mesdata.rawcartype.Trim();
                        //mesCaption = GetCarTypeDescription(plcFrameType);
                        //ShowLabelData(mesCaption, lblMESCARTYPEValue);

                        mesCaption = mesFrameType;
                        plcCaption = plcFrameType;

#if AGING_TEST_PLC
                        plcFrameType = mesFrameType;
#endif
                    }
                    else if (cartypeOption == 2)         //HMC U51
                    {
                        mesFrameType = GetCarTypeFromCarName(mesdata.rawcartype.Trim(), "");

                        //plcCaption = plcFrameType.Substring(2,2);
                        //plcFrameType = GetCarTypeFromPLC(plcCaption);
#if AGING_TEST_PLC
                        plcFrameType = GetCarTypeFromPLC(mesFrameType);
                        plcFrameType = "00" + plcFrameType;
#endif

                        plcCaption = plcFrameType.Substring(2, 2);
                        plcFrameType = GetCarTypeFromCarName(plcCaption, plcCaption);
                        //plcFrameType = GetCarTypeFromPLC(plcCaption);

                        mesCaption = mesFrameType;
                        plcCaption = plcFrameType;

#if AGING_TEST_PLC
                        plcFrameType = mesFrameType;
#endif
                    }
                    else
                    {
                        //recvArg.recvString = recvArg.recvString.Substring(4, 4);
                        //plcFrameType = recvArg.recvString.Trim();
                        plcCaption = GetFrameTypeDescription(plcFrameType, 1);

                        plcFrameType = scartyperead.Trim();

                        mesFrameType = mesdata.rawcartype.Trim();
                        mesCaption = mesdata.rawcartype.Trim();

#if AGING_TEST_PLC
                        plcCaption = mesCaption;
#endif
                        plcFrameType = plcCaption;
                    }
                }
                ShowLabelData(plcCaption, lblPLCCARTYPEValue);
                ShowLabelData(mesCaption, lblMESCARTYPEValue);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[2] : RECEIVE CAR TYPE FROM MES - " + mesCaption, Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[2] : RECEIVE CAR TYPE FROM PLC - " + plcCaption, Thread.CurrentThread.ManagedThreadId);

                if (plcFrameType != mesFrameType)
                {
                    string log = "";
                    log = "PLC : " + plcCaption + ", MES : " + mesCaption;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("MATCHING ERROR - " + log), Thread.CurrentThread.ManagedThreadId);

                    ShowLabelData("MATCHING NG", lblCheckResult, Brushes.Red);
                    retval.execResult = ErrorCodeConstant.ERROR_MATCHING_NG;
                    //recvArg.sErrorMessage = "MATCHING NG - " + log;

                    retval.errorInfo.sErrorMessage = "MATCHING NG - " + log;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_TYPE_NG;
                    await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo, 0);

                    ShowMatchingErrorMessage(plcFrameType, mesFrameType);
                    //SaveErrorDB(retval.errorInfo, sCurrentFunc);

                    retval.recvString = plcCaption;
                    return retval;
                }
                ShowLabelData("MATCHING OK", lblCheckResult, Brushes.Blue);

                if (cartypeOption == 0)     //HMC U42
                {
                    retval = await plcComm.SendFrameType2PLC(plcFrameType);
                    if (retval.execResult != 0)
                    {
                        //ShowErrorMessage("SEND CAR TYPE TO PLC ERROR", false);
                        ////ITNTErrorLog.Instance.Trace(0, "PLC로 차종 정보 신호 전송 ERROR 발생");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CheckCarType", "SendFrameType2PLC ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        //retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (SendFrameType2PLC) ERROR = " + retval.execResult.ToString();
                        //recvArg.errorInfo.sErrorFunc = sErrorFunc;
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private async Task<ITNTResponseArgs> CheckNextDataCount(string sCurrentFunc)
        {
            string className = "MainWindow";
            string funcName = "CheckCarTypeProcess2";

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            int count = 0;
            int totcount = 0;
            Stopwatch sw = new Stopwatch();
            try
            {
                (count, totcount) = GetCount4MarkPlanData(dgdPlanData);
                if (count <= 0)
                {
                    //ShowLog(className, funcName, 0, "[1-2] THERE IS NO MES DATA");
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("MES DATA COUNT < 0"), Thread.CurrentThread.ManagedThreadId);

                    //4초간 MES DATA 대기
                    sw.Start();
                    while (sw.Elapsed < TimeSpan.FromSeconds(4))
                    {
                        if (!changeDBProcessFlag)
                            break;

                        await Task.Delay(50);
                    }
                    sw.Stop();

                    if (mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INST_COMPLETE)
                    {
                        await ChangeDBProcess4Thread();
                    }

                    (count, totcount) = GetCount4MarkPlanData(dgdPlanData);
                    if (count <= 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("CANNOT FIND FA DATA 1"), Thread.CurrentThread.ManagedThreadId);

                        retval.execResult = ErrorCodeConstant.ERROR_DATA_NOT_FOUND;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.sErrorMessage = "NO MARKING DATA FOUND";
                        retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_NOT_FOUND;

                        retval.errorInfo.devErrorInfo.execResult = ErrorCodeConstant.ERROR_DATA_NOT_FOUND;
                        retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                        retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                        retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.devErrorInfo.sErrorMessage = "NO MARKING DATA FOUND";

                        //ITNTErrorCode(className, funcName, sCurrentFunc, retval.errorInfo);

                        return retval;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return retval;
        }


        private async Task<ITNTResponseArgs> CheckCartTypeNSequeneProcess(MESReceivedData mesdata, string sProcess)
        {
            string className = "MainWindow";
            string funcName = "CheckCartTypeNSequeneProcess";

            ITNTResponseArgs retval = new ITNTResponseArgs(128);
            int cartypeOption = 0;
            //string plcFrameType = "";
            string plcCaption = "";
            //string mesFrameType = "";
            string mesCaption = "";
            string sCarTypeRead = "";
            string value = "";
            string sCurrentFunc = "CHECK CAR TYPE";
            string sProcedure = "1";
            string tmpString = "";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (mesdata.userDataType != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "SKIP FLAG SET", Thread.CurrentThread.ManagedThreadId);
                    retval.execResult = 0;
                    return retval;
                }

                cartypeOption = (int)Util.GetPrivateProfileValueUINT("OPTION", "CARTYPECOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                if (cartypeOption == 5) //HMI 3
                {
                    retval = await plcComm.ReadBodyNum();
                    if (retval.execResult != 0)
                    {
                        //retval = await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadBodyNum ERROR = " + retval.execResult, Thread.CurrentThread.ManagedThreadId);
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadBodyNum) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                        ShowLabelData(plcCaption, lblPLCCARTYPEValue);
                        ShowLabelData(mesCaption, lblMESCARTYPEValue);

                        return retval;
                    }

                    //recvArg.recvString = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadBodyNum {0}", retval.recvString), Thread.CurrentThread.ManagedThreadId);

                    mesCaption = mesdata.bodyno;
                    plcCaption = retval.recvString;

                    //mesFrameType = mesdata.bodyno;
                    //plcFrameType = retval.recvString;
                }
                else
                {
#if AGING_TEST_PLC
#else
                    retval = await plcComm.ReadPLCCarType();
                    if (retval.execResult != 0)
                    {
                        //retval = await SendErrorInfo2PLC((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_PC_ERROR, PLCMELSEQSerial.PLC_ADDRESS_D200);
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ReadPLCCarType ERROR = " + retval.execResult, Thread.CurrentThread.ManagedThreadId);
                        retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (ReadPLCCarType) ERROR = " + retval.execResult.ToString();
                        retval.errorInfo.sErrorFunc = sCurrentFunc;

                        ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                        return retval;
                    }

                    //recvArg.recvString = Encoding.UTF8.GetString(recvArg.recvBuffer, 0, recvArg.recvSize);
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("ReadPLCCarType {0}", retval.recvString), Thread.CurrentThread.ManagedThreadId);
                    if (retval.recvString.Length < 8)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("COMMUNICATION ERROR TO PLC - SHORT LENGTH {0}", retval.recvString.Length), Thread.CurrentThread.ManagedThreadId);

                        retval.execResult = ErrorCodeConstant.ERROR_RECV_DATA_INVALID;
                        retval.errorInfo.sErrorMessage = "PLC DATA LENGTH INVALID (ReadPLCCarType) : " + retval.recvString.Length + " - " + retval.recvString;
                        retval.errorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.sErrorCode = DeviceCode.Device_PLC + Constants.ERROR_DATA + Constants.ERROR_INVALID;

                        retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                        retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_PLC;
                        retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_PLC;
                        retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                        retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                        ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);

                        return retval;
                    }

                    sCarTypeRead = retval.recvString.Substring(4, 4).Trim();
                    //plcFrameType = scartyperead.Trim();
#endif

                    //cartypeOption = (int)Util.GetPrivateProfileValueUINT("OPTION", "CARTYPECOMPTYPE", 0, Constants.PARAMS_INI_FILE);
                    if (cartypeOption == 0)     //HMC U42
                    {
                        //BODY TYPE(MES)
                        tmpString = GetFrameType4MES(mesdata.rawcartype.Trim(), mesdata.rawbodytype.Trim(), mesdata.rawtrim.Trim(), "");
                        mesCaption = GetFrameTypeDescription(tmpString, 1);
                        sCarTypeRead = sCarTypeRead.Replace("0", "");
                        plcCaption = GetFrameTypeDescription(sCarTypeRead);

#if AGING_TEST_PLC
                        plcFrameType = mesFrameType;
#endif
                    }
                    else if (cartypeOption == 1)    //KIA
                    {
                        //plcCaption = GetCarTypeDescription(plcFrameType);
                        //ShowLabelData(plcCaption, lblPLCCARTYPEValue);

                        //BODY TYPE(MES)
                        tmpString = mesdata.rawcartype.Trim();
                        //mesCaption = GetCarTypeDescription(plcFrameType);
                        //ShowLabelData(mesCaption, lblMESCARTYPEValue);

                        mesCaption = tmpString;
                        plcCaption = sCarTypeRead;

#if AGING_TEST_PLC
                        plcFrameType = mesFrameType;
#endif
                    }
                    else if (cartypeOption == 2)         //HMC U51
                    {
                        mesCaption = GetCarTypeFromCarName(mesdata.rawcartype.Trim(), "");

                        //plcCaption = plcFrameType.Substring(2,2);
                        //plcFrameType = GetCarTypeFromPLC(plcCaption);
#if AGING_TEST_PLC
                        plcFrameType = GetCarTypeFromPLC(mesFrameType);
                        plcFrameType = "00" + plcFrameType;
#endif

                        tmpString = sCarTypeRead.Substring(2, 2);
                        plcCaption = GetCarTypeFromCarName(tmpString, tmpString);
                        //plcFrameType = GetCarTypeFromPLC(plcCaption);

#if AGING_TEST_PLC
                        plcFrameType = mesFrameType;
#endif
                    }
                    else
                    {
                        //recvArg.recvString = recvArg.recvString.Substring(4, 4);
                        //plcFrameType = recvArg.recvString.Trim();
                        plcCaption = GetFrameTypeDescription(sCarTypeRead, 1);

                        //plcFrameType = scartyperead.Trim();

                        //mesFrameType = mesdata.rawcartype.Trim();
                        mesCaption = mesdata.rawcartype.Trim();

#if AGING_TEST_PLC
                        plcCaption = mesCaption;
#endif
                        //plcFrameType = plcCaption;
                    }
                }
                ShowLabelData(plcCaption, lblPLCCARTYPEValue);
                ShowLabelData(mesCaption, lblMESCARTYPEValue);

                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[2] : RECEIVE CAR TYPE FROM MES - " + mesCaption, Thread.CurrentThread.ManagedThreadId);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "[2] : RECEIVE CAR TYPE FROM PLC - " + plcCaption, Thread.CurrentThread.ManagedThreadId);

                if (plcCaption != mesCaption)
                {
                    string log = "";
                    log = "PLC : " + plcCaption + ", MES : " + mesCaption;
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("MATCHING ERROR - " + log), Thread.CurrentThread.ManagedThreadId);

                    ShowLabelData("CAR TYPE MATCHING NG", lblCheckResult, Brushes.Red);
                    retval.execResult = ErrorCodeConstant.ERROR_MATCHING_NG;
                    //recvArg.sErrorMessage = "MATCHING NG - " + log;

                    retval.errorInfo.sErrorMessage = "MATCHING NG - " + log;
                    retval.errorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_DATA + Constants.ERROR_TYPE_NG;
                    await plcComm.SendMatchingResult((byte)PLCMELSEQSerial.SIGNAL_PC2PLC_MATCHING_NG, 0);

                    retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                    retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                    retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                    retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                    retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo, 0);

                    ShowMatchingErrorMessage(plcCaption, mesCaption);
                    //SaveErrorDB(retval.errorInfo, sCurrentFunc);

                    retval.recvString = plcCaption;
                    return retval;
                }
                ShowLabelData("CAR TYPE MATCHING OK", lblCheckResult, Brushes.Blue);

                if (cartypeOption == 0)     //HMC U42
                {
                    retval = await plcComm.SendFrameType2PLC(plcCaption);
                    if (retval.execResult != 0)
                    {
                        //ShowErrorMessage("SEND CAR TYPE TO PLC ERROR", false);
                        ////ITNTErrorLog.Instance.Trace(0, "PLC로 차종 정보 신호 전송 ERROR 발생");
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MainWindow", "CheckCarType", "SendFrameType2PLC ERROR = " + retval.execResult.ToString(), Thread.CurrentThread.ManagedThreadId);
                        //retval.errorInfo.sErrorMessage = "COMMUNICATION TO PLC (SendFrameType2PLC) ERROR = " + retval.execResult.ToString();
                        //recvArg.errorInfo.sErrorFunc = sErrorFunc;
                    }
                }
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);

                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.sErrorMessage = sCurrentFunc + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");

                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sCurrentFunc;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage;

                ITNTErrorCode(className, funcName, sProcess, retval.errorInfo);
            }
            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            return retval;
        }

        private async Task<ITNTResponseArgs> LoadMarkDataAgain(string sProcedure)
        {
            string className = "MainWindow";
            string funcName = "LoadMarkDataAgain";

            ITNTResponseArgs retval = new ITNTResponseArgs(128);

            try
            {
                if (byMainScreenType == 0)
                {
                    retval = await CheckNextDataCount(sProcedure);
                    if (retval.execResult != 0)
                    {
                        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("CANNOT FIND FA DATA 1"), Thread.CurrentThread.ManagedThreadId);
                        ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
                        return retval;
                    }

                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("COMPLETE CHECKING MES DATA"), Thread.CurrentThread.ManagedThreadId);

                    retval = await MakeCurrentMarkData(dgdPlanData, 1);
                }
                else
                {
                    retval = await MakeCurrentMarkData2();
                }
                if (retval.execResult != 0)
                {
                    ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, retval.errorInfo.sErrorMessage, Thread.CurrentThread.ManagedThreadId);
                    ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);

                    return retval;
                }

                string logstring = "";
                logstring = "MARKING DATA\r\n";
                logstring += "##########################################################\r\n";
                logstring += "    --- DATA INFORMATION ---\r\n";
                logstring += "    TYPE     : " + currMarkInfo.currMarkData.mesData.cartype + "\r\n";
                logstring += "  MES VIN    : " + currMarkInfo.currMarkData.mesData.rawvin + "\r\n";
                logstring += "  MARK VIN   : " + currMarkInfo.currMarkData.mesData.markvin + "\r\n";
                logstring += "    SEQ NO   : " + currMarkInfo.currMarkData.mesData.sequence + "\r\n";

                logstring += "    --- FONT INFORMATION ---\r\n";
                logstring += "    NAME     : " + currMarkInfo.currMarkData.pattern.fontValue.fontName + "\r\n";
                logstring += "    HEIGTH   : " + currMarkInfo.currMarkData.pattern.fontValue.height.ToString() + "\r\n";
                logstring += "    WIDTH    : " + currMarkInfo.currMarkData.pattern.fontValue.width.ToString() + "\r\n";
                logstring += "    PITCH    : " + currMarkInfo.currMarkData.pattern.fontValue.pitch + "\r\n";
                logstring += "    PATTERN  : " + currMarkInfo.currMarkData.pattern.name + "\r\n";
                logstring += "##########################################################";
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, logstring, Thread.CurrentThread.ManagedThreadId);
                logstring = "SEQ = " + currMarkInfo.currMarkData.mesData.sequence.ToString() + ", VIN = " + currMarkInfo.currMarkData.mesData.markvin;

                ////Change Check Flag
                //if (byMainScreenType == 0)
                //{
                //    UpdateNextMarkData(currMarkInfo.currMarkData.mesData);
                //    ShowMarkingDataList(true, false);
                //    ScrollViewToPoint(dgdPlanData);
                //}

                //Show Current data on Top
                //await ShowCurrentMarkingInformation2(0, currMarkInfo.currMarkData.mesData, currMarkInfo.currMarkData.pattern, currMarkInfo.currMarkData.fontData, currMarkInfo.currMarkData.fontSizeX, currMarkInfo.currMarkData.fontSizeY, currMarkInfo.currMarkData.shiftValue, 2, 1);
                await ShowCurrentSequenceVIN(0, currMarkInfo.currMarkData, (byte)DISPLAY_INFO_COLOR_TYPE.DISP_COLOR_NEXTVIN, 1);
            }
            catch (Exception ex)
            {
                retval.execResult = ex.HResult;
                retval.errorInfo.sErrorFunc = sProcedure;
                retval.errorInfo.sErrorMessage = sProcedure + " EXCEPTION ERROR = " + ex.Message;
                retval.errorInfo.sErrorCode = DeviceCode.Device_APP + Constants.ERROR_EXCEPT + (-retval.execResult).ToString("X2");
                retval.errorInfo.devErrorInfo.execResult = retval.execResult;
                retval.errorInfo.devErrorInfo.sDeviceCode = DeviceCode.Device_APP;
                retval.errorInfo.devErrorInfo.sDeviceName = DeviceName.Device_APP;
                retval.errorInfo.devErrorInfo.sErrorFunc = sProcedure;
                retval.errorInfo.devErrorInfo.sErrorMessage = retval.errorInfo.sErrorMessage; ITNTErrorCode(className, funcName, sProcedure, retval.errorInfo);
            }

            return retval;
        }

    }
}
