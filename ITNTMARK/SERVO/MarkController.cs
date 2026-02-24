using ITNTCOMMON;
using ITNTUTIL;
using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    //internal class MarkController
    //{
    //}


    public class MarkController// : MarkComm
    {
        //const byte CMD_MOVE_ABS = 0x01;
        //const byte CMD_MOVE_INC = 0x02;
        //const byte CMD_SOL_TEST = 0x04;
        //const byte CMD_SOL_ON_OFF = 0x0A;

        public MarkComm markComm = new MarkComm();
        public MarkCommLaser markCommLaser = new MarkCommLaser();
        public bool IsOpen = false;
        byte bHeadType = 0;
        //public bool ErrorLaserSource = false;

        public MarkController()
        {
            //SendFlag = SENDFLAG_IDLE;
            string value = "";
            //byte bHeadType = 0;

            try
            {
                Util.GetPrivateProfileValue("MARK", "HEADTYPE", "0", ref value, Constants.PARAMS_INI_FILE);
                byte.TryParse(value, out bHeadType);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "MarkController", "HEAD TYPE = " + bHeadType.ToString(), Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "MarkController", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }

        public int OpenMarkController()
        {
            int retval = 0;

            try
            {
                if(bHeadType == 0)
                    retval = markComm.OpenMarkDevice();
                else
                    retval = markCommLaser.OpenMarkDevice();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "OpenMarkController", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
            }
            return retval;
        }

        public async Task<int> OpenMarkControllerAsync()
        {
            int retval = 0;

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.OpenMarkDeviceAsync();
                else
                    retval = await markCommLaser.OpenMarkDeviceAsync();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "OpenMarkControllerAsync", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
            }
            return retval;
        }

        public void CloseMarkController()
        {
            int retval = 0;

            try
            {
                if (bHeadType == 0)
                    retval = markComm.CloseDevice();
                else
                    retval = markCommLaser.CloseDevice();
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "CloseDevice", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
            }
            return;
        }

        public bool GetOpenStatus()
        {
            bool retval = false;

            try
            {
                if (bHeadType == 0)
                    retval = markComm.IsOpen;
                else
                    retval = markCommLaser.IsOpen;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "CloseDevice", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = false;
            }
            return retval;

        }

        public async Task<ITNTResponseArgs> GetCurrentSetting()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
#if TEST_DEBUG_MARK
            //GetMarkStatus();
#else
            //retval = await ExecuteCommandMsg(0, 5, (byte)'0', sdata, sleng);
#endif
            return retval;
        }

        //public int Controller_Online()
        //{
        //    int retval = 0;

        //    return retval;
        //}

        //public void StartThread()
        //{
        //    markComm.StartThread();
        //}

        //public void StopThread()
        //{
        //    markComm.StopThread();
        //}

        //public async Task<int> InitializeController()
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();

        //    //short steplength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
        //    //short maxY = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 60, Constants.MARKING_INI_FILE);
        //    //steplength = (short)(steplength * maxY);

        //    //short scansteplength = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 50, Constants.MARKING_INI_FILE);
        //    //short scanmaxY = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 60, Constants.MARKING_INI_FILE);

        //    //retval = await markComm.InitializeController(steplength);
        //    retval = await markComm.InitializeController();
        //    return retval.execResult;
        //}

        //public void SetReadyErrorStatus(bool ready)
        //{
        //    markComm.SetReadyErrorStatus(ready);
        //}

        //public async Task<int> StartMark2MarkController(short runmode)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    //byte[] value = Encoding.UTF8.GetBytes(count.ToString("X4"));
        //    //retval = await markComm.RunStart(value, value.Length);
        //    retval = await markComm.RunStart(runmode);
        //    return retval.execResult;
        //}

        public async Task<int> StartMark2MarkController(short runmode)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            //byte[] value = Encoding.UTF8.GetBytes(count.ToString("X4"));
            //retval = await markComm.RunStart(value, value.Length);
            retval = await markComm.RunStart(runmode);
            return retval.execResult;
        }


        public async Task<ITNTResponseArgs> FontFlush()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.FontFlush();
                else
                    retval = await markCommLaser.FontFlush();
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> Inport()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.Inport();
                else
                    retval = await markCommLaser.Inport();
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ScanJog(byte direction, short resolution, double scanstpelength)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.ScanJog(direction, resolution, scanstpelength);
                else
                    retval = await markCommLaser.ScanJog(direction, resolution, scanstpelength);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        //public async Task<ITNTResponseArgs> Profile_Speed(byte flag, short ispeed, short tspeed, short acspeed, short despeed)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    string className = "MarkController";
        //    string funcName = "StrikeNo";

        //    try
        //    {
        //        if (bHeadType == 0)
        //            retval = await markComm.Profile_Speed(flag, ispeed, tspeed, acspeed, despeed);
        //        else
        //            retval = await markCommLaser.Profile_Speed(flag, ispeed, tspeed, acspeed, despeed);
        //        return retval;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval.execResult = ex.HResult;
        //    }
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> MoveScan2Home(short homeposition)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.MoveScan2Home(homeposition);
                else
                    retval = await markCommLaser.MoveScan2Home(homeposition);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> ScanProfile(short length, int timeout)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.ScanProfile(length);
                else
                    retval = await markCommLaser.ScanProfile(length, timeout);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        //public async Task<ITNTResponseArgs> MoveScanProfile(short position, short steplength)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    retval = await markComm.MoveScanProfile(position, steplength);
        //    return retval;
        //}
        public async Task<ITNTResponseArgs> MoveScanProfile(short position, int timeout)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.MoveScanProfile(position);
                else
                    retval = await markCommLaser.MoveScanProfile(position, timeout);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> RunStart(short count)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.RunStart(count);
                else
                    retval.execResult = -1;
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }

            //retval = await markComm.RunStart(count);
            return retval;
        }

        //public async Task<ITNTResponseArgs> RunStart_S(string markpoint)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    retval = await markComm.RunStart_S(markpoint);
        //    return retval;
        //}

        public async Task<ITNTResponseArgs> RunStart_S(string markdata, bool cleanFireFlag, byte loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval.execResult = -1;
                else
                    retval = await markCommLaser.RunStart_S(markdata, cleanFireFlag, loglevel);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            //if (DeviceType == 0)
            //    retval = await markComm.RunStart_S(markinfo, cleanFireFlag);
            //else
            return retval;
        }


        public async Task<ITNTResponseArgs> RunStart_S(MarkVINInformEx markinfo, bool cleanFireFlag, byte loglevel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval.execResult = -1;
                else
                    retval = await markCommLaser.RunStart_S(markinfo, cleanFireFlag, loglevel);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            //if (DeviceType == 0)
            //    retval = await markComm.RunStart_S(markinfo, cleanFireFlag);
            //else
            return retval;
        }

        public async Task<ITNTResponseArgs> SetWorkArea(int posX, int posY, int posZ)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval.execResult = 0;
                else
                    retval = await markCommLaser.SetWorkArea(posX, posY, posZ);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;

        }

        public async Task<ITNTResponseArgs> Move2LimitXY(short maxX, short maxY)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.Move2LimitXY(maxX, maxY);
                else
                    retval = await markCommLaser.Move2LimitXY(maxX, maxY);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> Move2LimitU(short maxu)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.Move2LimitU(maxu);
                else
                    retval = await markCommLaser.Move2LimitU(maxu);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> MoveHead(short xval, short yval, short zval)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.MoveHead(xval, yval);
                else
                    retval = await markCommLaser.MoveHead(xval, yval, zval);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> GoPoint(short posX, short posY, short pos)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.GoPoint(posX, posY, pos);
                else
                    retval.execResult = -1;// await markCommLaser.GoPoint(posX, posY, posZ, pos);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            //retval = await markComm.GoPoint(posX, posY, posZ);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoPoint(int posX, int posY, int posZ, int pos)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.GoPoint(posX, posY, pos);
                else
                    retval = await markCommLaser.GoPoint(posX, posY, posZ, pos);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> TestSolFet(short Fet, bool Sol, bool Igno = false)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.TestSolFet(Fet, Sol);
                else
                    retval = await markCommLaser.TestSolFet(Fet, Sol);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            //retval = await markComm.TestSolFet(Fet, Sol);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoHome(short posX, short posY)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.GoHome(posX, posY);
                else
                    retval = await markCommLaser.GoHome(posX, posY);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            //retval = await markComm.GoHome(posX, posY);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoHome(int posX, int posY, int posZ)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.GoHome(posX, posY);
                else
                    retval = await markCommLaser.GoHome(posX, posY, posZ);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            //retval = await markComm.GoHome(posX, posY, posZ);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoHomeAll(short posX, short posY, short posU)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.GoHomeAll(posX, posY, posU);
                else
                    retval = await markCommLaser.GoHomeAll(posX, posY, posU);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> GoParking(short posX, short posY)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.GoParking(posX, posY);
                else
                    retval = await markCommLaser.GoParking(posX, posY);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            //retval = await markComm.GoParking(posX, posY);
            return retval;
        }

        public async Task<ITNTResponseArgs> GoParking(int posX, int posY, int posZ)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.GoParking(posX, posY);
                else
                    retval = await markCommLaser.GoParking(posX, posY, posZ);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> GoParkingAsync(int posX, int posY, int posZ)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.GoParking(posX, posY);
                else
                    retval = await markCommLaser.GoParkingAsync(posX, posY, posZ);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public async Task<ITNTResponseArgs> LoadSpeed(byte cmd, short initSpeed, short targetSpeed, short accel, short decel)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.LoadSpeed(cmd, initSpeed, targetSpeed, accel, decel);
                else
                    retval = await markCommLaser.LoadSpeed(cmd, initSpeed, targetSpeed, accel, decel);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> LoadFontData(short chidx, short ptidx, short posX, short poxY, short flag)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.LoadFontData(chidx, ptidx, posX, poxY, flag);
                else
                    retval = await markCommLaser.LoadFontData(chidx, ptidx, posX, poxY, flag);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            //retval = await markComm.LoadFontData(chidx, ptidx, posX, poxY, flag);
            return retval;
        }

        public async Task<ITNTResponseArgs> LoadFontData(short chidx, short ptidx, int posX, int poxY, int posZ, short flag)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.LoadFontData(chidx, ptidx, posX, poxY, flag);
                else
                    retval = await markCommLaser.LoadFontData(chidx, ptidx, posX, poxY, posZ, flag);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            //retval = await markComm.SolOnOffTime(solontime, solofftime);
            return retval;
        }

        public async Task<ITNTResponseArgs> SolOnOffTime(short solontime, short solofftime)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.SolOnOffTime(solontime, solofftime);
                else
                    retval = await markCommLaser.SolOnOffTime(solontime, solofftime);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            //retval = await markComm.SolOnOffTime(solontime, solofftime);
            return retval;
        }

        public async Task<ITNTResponseArgs> StrikeNo(short count)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.StrikeNo(count);
                else
                    retval = await markCommLaser.StrikeNo(count);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }

            return retval;
        }

        public async Task<ITNTResponseArgs> SetDensity(short density)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "SetDensity";

            try
            {
                if (bHeadType != 0)
                    retval = await markCommLaser.SetDensity(density);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> dwellTimeSet(short time)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "dwellTimeSet";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.dwellTimeSet(time);
                else
                    retval = await markCommLaser.dwellTimeSet(time);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }

            //retval = await markComm.dwellTimeSet(time);
            return retval;
        }

        public async Task<string> GetFWVersion()
        {
            string retval = "";
            string className = "MarkController";
            string funcName = "GetFWVersion";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.GetFWVersion();
                else
                    retval = await markCommLaser.GetFWVersion();
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = "";
            }

            //retval = await markComm.GetFWVersion();
            return retval;
        }

        public async Task<ITNTResponseArgs> GetFWVersion2()
        {
            string className = "MarkController";
            string funcName = "GetFWVersion2";

            ITNTResponseArgs retval = new ITNTResponseArgs();
            string version = "";
            int iversion = 0;
            byte[] buff = new byte[32];

            try
            {
                //if (bHeadType == 0)
                //    retval = await markComm.GetFWVersion();
                //else
                    retval = await markCommLaser.GetFWVersion2();

                version = Encoding.Default.GetString(retval.recvBuffer, 0, retval.recvSize);
                iversion = Convert.ToInt32(version, 16);
                retval.recvString = (iversion / 100).ToString("D") + "." + (iversion % 100).ToString("D2");
                Encoding.UTF8.GetBytes(retval.recvString, 0, retval.recvString.Length, buff, 0);
                retval.recvSize = retval.recvString.Length;

                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }

            //retval = await markComm.GetFWVersion();
            return retval;
        }

        public async Task<ITNTResponseArgs> Test4RealMarkArea()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "GetFWVersion";

            try
            {
                if (bHeadType == 0)
                    retval = await markComm.Test4RealMarkArea();
                else
                    retval = await markCommLaser.Test4RealMarkArea();
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> SetPhaseComp(Single p_value)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "GetFWVersion";

            try
            {
                if (bHeadType == 0)
                    retval.execResult = 0;
                else
                    retval = await markCommLaser.SetPhaseComp(p_value);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> GoStartPoint(int posX, int posY, int posZ)
        {
            short stepLeng = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "StrikeNo";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                stepLeng = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 100, Constants.MARKING_INI_FILE);
                if(bHeadType == 0)
                    retval = await markComm.GoPoint(posX * stepLeng, posY * stepLeng, 0);
                else
                    retval = await markCommLaser.GoPoint(posX * stepLeng, posY * stepLeng, posZ * stepLeng, 0);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("END : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public async Task<ITNTResponseArgs> GoStartPoint2(int posX, int posY, int posZ)
        {
            //short stepLeng = 0;
            ITNTResponseArgs retval = new ITNTResponseArgs();
            string className = "MarkController";
            string funcName = "GoStartPoint2";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                //stepLeng = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 100, Constants.MARKING_INI_FILE);
                if(bHeadType == 0)
                    retval = await markComm.GoPoint(posX, posY, 0);
                else
                    retval = await markCommLaser.GoPoint(posX, posY, posZ, 0);
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoHome(HomeXY) END : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public ITNTResponseArgs GetLaserErrorStatus()
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(8);
            string className = "MarkController";
            string funcName = "GetLaserErrorStatus";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (bHeadType == 0)
                    retval.recvBuffer[0] = 0;
                else
                {
                    if (markCommLaser.GetLaserErrorStatus() == false)
                        retval.recvBuffer[0] = 0;
                    else
                        retval.recvBuffer[1] = 1;
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoHome(HomeXY) END : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.recvBuffer[0] = 0;
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public ITNTResponseArgs SetLaserErrorStatus(bool bError)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(8);
            string className = "MarkController";
            string funcName = "SetLaserErrorStatus";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START " + bError.ToString(), Thread.CurrentThread.ManagedThreadId);
                if (bHeadType != 0)
                {
                    markCommLaser.SetLaserErrorStatus(bError);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }


        public ITNTResponseArgs SetMotorErrorStatus(bool bError)
        {
            ITNTResponseArgs retval = new ITNTResponseArgs(8);
            string className = "MarkController";
            string funcName = "SetLaserErrorStatus";

            try
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                if (bHeadType != 0)
                {
                    markCommLaser.SetMotorErrorStatus(bError);
                }
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                return retval;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval.execResult = ex.HResult;
            }
            return retval;
        }

        public string[] GetSerialPorts()
        {
            string[] portNames = null;
            string className = "MarkController";
            string funcName = "GoStartPoint2";

            try
            {
                portNames = SerialPort.GetPortNames();
                return portNames;
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
            return portNames;
        }

        //public async Task<ITNTResponseArgs> Head_Range_Test()
        //{
        //    short maxX = 0;
        //    short maxY = 0;
        //    short maxZ = 0;
        //    short stepLeng = 0;
        //    short zero = 0;
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "Head_Range_Test", "START", Thread.CurrentThread.ManagedThreadId);

        //        maxX = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_X", 190, Constants.MARKING_INI_FILE);
        //        maxY = (short)Util.GetPrivateProfileValueUINT("MARK", "MAX_Y", 60, Constants.MARKING_INI_FILE);
        //        maxZ = (short)(Util.GetPrivateProfileValueUINT("MARK", "MAX_Z", 10, Constants.MARKING_INI_FILE) / 2);
        //        stepLeng = (short)Util.GetPrivateProfileValueUINT("MARK", "STEP_LENGTH", 100, Constants.MARKING_INI_FILE);

        //        //Range_MinXMaxY
        //        retval = await markComm.GoPoint(0, maxY * stepLeng, maxZ * stepLeng / 2, 0);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(HomeXY) return : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }
        //        //Range_MaxXMaxY
        //        retval = await markComm.GoPoint(maxX * stepLeng, maxY * stepLeng, maxZ * stepLeng / 2, 0);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MaxXMaxY) return : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }
        //        //Range_MaxXMinY
        //        retval = await markComm.GoPoint(maxX * stepLeng, 0, maxZ * stepLeng / 2, 0);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MaxXMinY) return : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }
        //        //Range_MinXY
        //        retval = await markComm.GoPoint(0, 0, maxZ * stepLeng / 2, 0);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MinXMinY) return : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }
        //        //Range_MinXMaxY
        //        retval = await markComm.GoPoint(0, maxY * stepLeng, maxZ * stepLeng / 2, 0);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MinXMaxY) return : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }
        //        //Range_MaxXMinY
        //        retval = await markComm.GoPoint(maxX * stepLeng, 0, maxZ * stepLeng / 2, 0);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MaxXMinY) return : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }
        //        //Range_MinXMaxY
        //        retval = await markComm.GoPoint(0, maxY * stepLeng, maxZ * stepLeng / 2, 0);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("GoHome(MinXMaxY) return : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }
        //    }

        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "Head_Range_Test", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval.execResult = ex.HResult;
        //    }
        //    return retval;
        //}


        //public async Task<ITNTResponseArgs> READY_IO(bool OnOff)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    string sendstring = "";
        //    byte[] sbuff;
        //    short zero = 0;
        //    short one = 1;
        //    string className = "MarkController";
        //    string funcName = "READY_IO";

        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

        //        if (OnOff == true)
        //            sendstring = zero.ToString("X4") + one.ToString("X4");// "00000001";
        //        else
        //            sendstring = zero.ToString("X4") + zero.ToString("X4");//"00000000";
        //        sbuff = Encoding.UTF8.GetBytes(sendstring);

        //        if(bHeadType == 0)
        //            retval = await markComm.TestSolFet(sbuff, sbuff.Length);
        //        else
        //            retval = await markCommLaser.TestSolFet(sbuff, sbuff.Length);
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("GoHome(MinXMaxY) return : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //        return retval;
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval.execResult = ex.HResult;
        //    }
        //    return retval;
        //}

        //public async Task<ITNTResponseArgs> DONE_IO(bool OnOff)
        //{
        //    ITNTResponseArgs retval = new ITNTResponseArgs();
        //    string sendstring = "";
        //    byte[] sbuff;
        //    short zero = 0;
        //    short one = 1;
        //    try
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "READY_IO", "START", Thread.CurrentThread.ManagedThreadId);

        //        if (OnOff == true)
        //            sendstring = one.ToString("X4") + one.ToString("X4");//"00010001";
        //        else
        //            sendstring = one.ToString("X4") + zero.ToString("X4");//"00010000";
        //        sbuff = Encoding.UTF8.GetBytes(sendstring);
        //        retval = await markComm.TestSolFet(sbuff, sbuff.Length);
        //        if (retval.execResult != 0)
        //        {
        //            ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "READY_IO", string.Format("GoHome(MinXMaxY) return : {0}", retval.execResult), Thread.CurrentThread.ManagedThreadId);
        //            return retval;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "MarkController", "READY_IO", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
        //        retval.execResult = ex.HResult;
        //    }
        //    return retval;
        //}

    }

}
