using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ITNTUTIL;
using ITNTCOMMON;
using System.Diagnostics;
using System.IO;
using System.Threading;


#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{

    //    Public g_sMESDataFile                           As String  '서버 작업예정데이터 파일 경로 설정 저장변수
    //Public g_sMESFlagFile                           As String  '서버 데이터 플래그 파일 경로 설정 저장변수
    //Public g_sMESCompleteFile                       As String  '서버 작업완료데이터 파일 경로 설정 저장변수
    //Public g_sMESReqFile                            As String  '서버 데이터 재요구 플래그 파일 경로 설정 저장변수
    //Public g_nMESPollingTime                        As Long    '서버 작업예정 데이터 스캔 주기 설정 저장변수
    //Public g_sDownloadTime                          As String  '마지막 서버데이터 다운로드타임 저장변수
    //Public g_nSearchDataCnt                         As Long    '서버타이머에서 스캔카운트를 저장하기 위한 변수

    class MESFTB
    {
        public event MESClientReceivedEventHandler receivedEvent = null;
        public event MESClientStatusChangedEventHandler statusChangedEvent = null;

        const int MES_MSG_DATA = 100;
        public string serverIP = "";
        public int serverPort = 0;
        RingBuffer rb = null;
        //StateObject asyncstate = null;
        //bool isConnected = false;
        bool doingCmdFlag = false;
        //byte sendFlag = (byte)MESSENDFLAG.SENDFLAG_SEND_IDLE;

        DispatcherTimer mesRunningTimer = new DispatcherTimer();

        private readonly object lockbuff = new object();

        //Socket workSocket = null;
        //byte[] ReceivedFrame;
        //byte[] socketBuffer = new byte[512 * 1024];
        //byte[] socketBuffer = new byte[32 * 1024];

        int iBlockSize = 0;
        bool bRequestFlag = false;
        csConnStatus connstatus = csConnStatus.Closed;
        csConnStatus connbefore = csConnStatus.Closed;


        public MESFTB()
        {
            mesRunningTimer.Tick += mesTimerHandler;
            mesRunningTimer.IsEnabled = false;
            mesRunningTimer.Stop();
        }


        public int StartTimer()
        {
            mesRunningTimer.Start();
            return 0;
        }

        public int RequestData()
        {
            int retval = 0;
            FileInfo FI_REQ;// = new FileInfo();
            string requestFileName = "";

            try
            {
                Util.GetPrivateProfileValue("FTB", "DATAREQ", "D:\\VIN\\재송신요구_VIN1.flg", ref requestFileName, Constants.PARAMS_INI_FILE);

                //data save file
                FI_REQ = new FileInfo(requestFileName);
                if (FI_REQ.Exists == false)
                {
                    using (FileStream completeStream = File.Create(requestFileName))
                    {
                    }
                    mesRunningTimer.Start();
                }
            }
            catch (Exception ex)
            {

            }
            return retval;
        }



        private async void mesTimerHandler(object sender, EventArgs e)
        {
            string className = "MESFTB";
            string funcName = "mesTimerHandler";

            int pollingTime = 30;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;

            string flagFileName = "";
            string dataFileName = "";
            string compFileName = "";
            string requestFileName = "";
            string ccrFileName = "";
            FileInfo FI_DATA;
            FileInfo FI_FLAG;
            FileInfo FI_COMP;
            FileInfo FI_REQ;
            FileInfo FI_CCR;

            string value = "";
            MESClientReceivedEventArgs arg = new MESClientReceivedEventArgs();


            try
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

                Util.GetPrivateProfileValue("FTB", "POLLINGTIME", "300", ref value, Constants.PARAMS_INI_FILE);
                int.TryParse(value, out pollingTime);

                Util.GetPrivateProfileValue("FTB", "DATAFILE", "D:\\VIN\\vin_main.dat", ref flagFileName, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("FTB", "FLAGFILE", "D:\\VIN\\데이터갱신_VIN.flg", ref dataFileName, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("FTB", "COMPLETEFILE", "D:\\VIN\\완료_VIN.flg", ref compFileName, Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("FTB", "DATAREQ", "D:\\VIN\\재송신요구_VIN1.flg", ref requestFileName, Constants.PARAMS_INI_FILE);


                mesRunningTimer.Interval = TimeSpan.FromSeconds(pollingTime);
                doingCmdFlag = true;
                mesRunningTimer.Stop();

                if (((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING) //(int)mesUpdateStatus.MES_UPDATE_STATUS_RECV_COMPLETE;
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "MES DATA is UPDATING", Thread.CurrentThread.ManagedThreadId);
                    mesRunningTimer.Start();
                    return;
                }


                FI_FLAG = new FileInfo(flagFileName);
                if(FI_FLAG.Exists == false)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "FLAG FILE NONE", Thread.CurrentThread.ManagedThreadId);
                    mesRunningTimer.Start();
                    return;
                }

                //data save file
                FI_DATA = new FileInfo(dataFileName);
                if(FI_DATA.Exists == false)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "DATA FILE NONE", Thread.CurrentThread.ManagedThreadId);
                    mesRunningTimer.Start();
                    return;
                }

                ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_SAVING_FILE;

                //ccr file
                ccrFileName = curDir + "DATA\\CCR.DATA";
                FI_CCR = new FileInfo(ccrFileName);
                if (FI_CCR.Exists == true)
                    FI_CCR.Delete();

                using (FileStream sourceStream = File.Open(dataFileName, FileMode.Open))
                {
                    using (FileStream destinationStream = File.Create(ccrFileName))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }
                }

                // create complete flag file
                using (FileStream completeStream = File.Create(compFileName))
                {
                }

                //delete flag file
                FI_FLAG.Delete();

                //set flag to recv complete
                ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_RECV_COMPLETE;

                arg.blockCount = 0;
                arg.itotalCount = 0;
                arg.recordSize = 0;
                //arg.recvMsg = recorddata;
                receivedEvent?.Invoke(this, arg);

                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                mesRunningTimer.Start();
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                mesRunningTimer.Start();
                return;
            }
        }
    }

}
