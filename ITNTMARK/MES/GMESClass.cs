using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using ITNTCOMMON;
using ITNTUTIL;
using System.Threading;

#pragma warning disable 1998
#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTMARK
{
    public class GMESClass
    {
        public event MESClientReceivedEventHandler receivedEvent = null;
        public event MESClientStatusChangedEventHandler statusChangedEvent = null;

        DispatcherTimer mesRunningTimer = new DispatcherTimer();
        csConnStatus connstatus = csConnStatus.Closed;
        csConnStatus connbefore = csConnStatus.Closed;

        public GMESClass()
        {
            connstatus = csConnStatus.Closed;
            connbefore = csConnStatus.Closed;

        }

        public async Task<int> StartGMES()
        {
            int retval = 0;
            string className = "GMESClass";
            string funcName = "StartGMES";
            ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();

            try
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                mesRunningTimer.Tick += mesTimerHandler;
                mesRunningTimer.Interval = TimeSpan.FromSeconds(10);
                //mesRunningTimer.IsEnabled = false;
                mesRunningTimer.Start();


                connbefore = connstatus;
                connstatus = csConnStatus.Connected;

                if (connstatus != connbefore)
                {
                    stsarg.oldstatus = connbefore;
                    stsarg.newstatus = connstatus;
                    statusChangedEvent?.Invoke(this, stsarg);
                }

                //if (connstatus != csConnStatus.Connected)
                //{
                //    connbefore = connstatus;
                //    connstatus = csConnStatus.Connected;

                //    arg.oldstatus = connbefore;
                //    arg.newstatus = connstatus;
                //    statusChangedEvent?.Invoke(this, arg);
                //}

                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                retval = ex.HResult;
            }
            return retval;
        }


        public async Task<int> CloseMES()
        {
            string className = "MESClient";
            string funcName = "CloseMES";
            ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            ServerStatusChangedEventArgs arg = new ServerStatusChangedEventArgs();

            try
            {
                mesRunningTimer.Stop();

                if (connstatus != csConnStatus.Closed)
                {
                    connbefore = connstatus;
                    connstatus = csConnStatus.Closed;

                    arg.oldstatus = connbefore;
                    arg.newstatus = connstatus;
                    statusChangedEvent?.Invoke(this, arg);
                }
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }

            return 0;
        }


        private async void mesTimerHandler(object sender, EventArgs e)
        {
            string className = "MESClient";
            string funcName = "mesTimerHandler";
            int retval = 0;
            //TimeSpan ts = TimeSpan.FromSeconds(10);
            Stopwatch sw = new Stopwatch();
            MESClientReceivedEventArgs arg = new MESClientReceivedEventArgs();
            string value = "";

            try
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
                //mesRunningTimer.Interval = TimeSpan.FromSeconds(300);
                mesRunningTimer.Stop();

                connbefore = connstatus;
                connstatus = csConnStatus.Connected;

                if (connstatus != connbefore)
                {
                    ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                    stsarg.oldstatus = connbefore;
                    stsarg.newstatus = connstatus;
                    statusChangedEvent?.Invoke(this, stsarg);
                }

                Util.GetPrivateProfileValue("FILENAME", "DATAFILE", "C:\\GMES\\EXTAGENTCLIENT\\DATA\\PRODORDER.DAT", ref value, Constants.MESCONF_INI_FILE);

                FileInfo datafile = new FileInfo(value);

                if(datafile.Exists == true)
                {
                   WriteCCRFile3(datafile.FullName);

                    value = DateTime.Now.ToString("yyyyMMdd");
                    Util.WritePrivateProfileValue("MES", "ORDERDATETIME", value, Constants.PARAMS_INI_FILE);

                    ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_RECV_COMPLETE;
                    Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);

                    receivedEvent?.Invoke(this, arg);
                }

                //arg.blockCount = mesDataInfo.iblockCount;
                //arg.itotalCount = mesDataInfo.itotalCount;
                //arg.recordSize = mesHeader.irecordSize;
                ////arg.recvMsg = recorddata;

                //doingCmdFlag = false;
                connbefore = connstatus;
                connstatus = csConnStatus.Disconnected;

                if (connstatus != connbefore)
                {
                    ServerStatusChangedEventArgs stsarg = new ServerStatusChangedEventArgs();
                    stsarg.oldstatus = connbefore;
                    stsarg.newstatus = connstatus;
                    statusChangedEvent?.Invoke(this, stsarg);
                }

                mesRunningTimer.Start();
            }
            catch (Exception ex)
            {
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                //doingCmdFlag = false;
                //await DisconnectServer();
                mesRunningTimer.Start();
                //ErrorCount++;
                //((DispatcherTimer)sender).Start();
                return;
            }
            //doingCmdFlag = false;
            //mesRunningTimer.Start();
            //((DispatcherTimer)sender).Start();
        }

        private int WriteCCRFile3(string datafile)
        {
            //StreamWriter writer = null;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string className = "MESClient";
            string funcName = "WriteCCRFile3";
            string fname = "";
            string value = "";
            FileInfo fi;
            FileInfo srcfi;
            Stopwatch sw = new Stopwatch();

            try
            {
                if (((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "mesDBUpdateFlag = MES_UPDATE_STATUS_INSERTING", Thread.CurrentThread.ManagedThreadId);

                    sw.Start();
                    while (sw.Elapsed < TimeSpan.FromSeconds(10))
                    {
                        if (((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag != (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING)
                            break;
                        Thread.Sleep(10);
                        //await Task.Delay(50);
                    }
                    sw.Stop();
                }
                srcfi = new FileInfo(datafile);

                ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_SAVING_FILE;
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref value, Constants.PARAMS_INI_FILE);
                curDir = curDir + "DATA\\";// CCR.dat";
                if (System.IO.Directory.Exists(curDir) == false)
                    System.IO.Directory.CreateDirectory(curDir);

                //fname = curDir + "\\CCR" + index.ToString("D2") + ".dat";
                if (value == "plantable")
                    fname = curDir + "CCR2.DAT";
                else
                    fname = curDir + "CCR.DAT";

                fi = new FileInfo(fname);
                if (fi.Exists == true)
                    fi.Delete();

                srcfi.CopyTo(fname, true);

                srcfi.Delete();

                return 0;
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("WriteFile() exception = " + ex.HResult.ToString() + "||||" + data);
                ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                return ex.HResult;
            }
        }

        async Task WriteCCRFile3Async(string source)
        {
            StreamWriter writer;
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            string fname = "";
            string value = "";
            FileInfo fi;
            string className = "MESClient";
            string funcName = "WriteCCRFile3";

            try
            {
                if (((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag == (int)mesUpdateStatus.MES_UPDATE_STATUS_INSERTING)
                {
                    ITNTMESLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "mesDBUpdateFlag = MES_UPDATE_STATUS_INSERTING", Thread.CurrentThread.ManagedThreadId);
                    return;
                }

                ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag = (int)mesUpdateStatus.MES_UPDATE_STATUS_SAVING_FILE;
                Util.WritePrivateProfileValue("OPTION", "MESUPDATEFLAG", ((MainWindow)System.Windows.Application.Current.MainWindow).mesDBUpdateFlag.ToString(), Constants.PARAMS_INI_FILE);
                Util.GetPrivateProfileValue("OPTION", "TABLENAME", "plantable", ref value, Constants.PARAMS_INI_FILE);
                curDir = curDir + "DATA\\";// CCR.dat";
                if (System.IO.Directory.Exists(curDir) == false)
                    System.IO.Directory.CreateDirectory(curDir);

                //fname = curDir + "\\CCR" + index.ToString("D2") + ".dat";
                if (value == "plantable")
                    fname = curDir + "CCR2.DAT";
                else
                    fname = curDir + "CCR.DAT";
                fi = new FileInfo(fname);

                if (fi.Exists == true)
                    fi.Delete();

                fi.CopyTo(fname, true);
                //await File.CopyAsync(source, fname);

                //if (mode == FileMode.CreateNew)
                //{
                //    //writer = new StreamWriter(fname, false);
                //    writer = File.CreateText(fname);
                //    await writer.WriteLineAsync(data);
                //    //await writer.WriteAsync(string.Empty);
                //    //await writer.WriteAsync(data);
                //    writer.Close();
                //}
                //else if (mode == FileMode.Append)
                //{
                //    //writer = new StreamWriter(fname, true);
                //    writer = File.AppendText(fname);
                //    await writer.WriteLineAsync(data);
                //    //await writer.WriteAsync(data);
                //    writer.Close();
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WriteFile() exception = " + ex.HResult.ToString());
            }
        }
    }
}
