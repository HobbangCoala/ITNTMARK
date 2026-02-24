//#define _LAMP_CONTROL_PNP_

using System;
using System.Windows;
using System.Windows.Interop;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using ITNTCOMMON;
using ITNTUTIL;

#pragma warning disable 0219
#pragma warning disable 4014


namespace ITNTMARK
{
	//public delegate void DIODataArrivalHandler(ref Byte[] data, int length, uint uFlag);

	public static class DIOControl
    {
        #region sample
        //	public bool IsOpened { get; set; } = false;
        //	public bool OpenDevice(System.Windows.Window window)
        //	{
        //		IntPtr windowHandle = new WindowInteropHelper(window).EnsureHandle();
        //		//++
        //		// 라이브러리 초기화
        //		if (CAxtLib.AxtIsInitialized() == 0)                // 통합 라이브러리가 사용 가능하지 (초기화가 되었는지)를 확인한다
        //		{
        //			if (CAxtLib.AxtInitialize(windowHandle, 0) == 0)     // 통합 라이브러리를 초기화 한다
        //			{
        //				System.Windows.MessageBox.Show("라이브러리 초기화 실패 입니다. 프로그램을 다시 실행 시켜 주세요.");

        //				return false;
        //			}
        //		}

        //		// 사용하시는 베이스보드에 맞추어 Device를 Open하면 됩니다.
        //		// BUSTYPE_ISA					:	0
        //		// BUSTYPE_PCI					:	1
        //		// BUSTYPE_VME					:	2
        //		// BUSTYPE_CPCI(Compact PCI)	:	3

        //		if (CAxtLib.AxtIsInitializedBus(1) == 0)            // 지정한 버스(PCI)가 초기화 되었는지를 확인한다
        //		{
        //			if (CAxtLib.AxtOpenDeviceAuto(1) == 0)          // 새로운 베이스보드를 자동으로 통합라이브러리에 추가한다
        //			{
        //				System.Windows.MessageBox.Show("보드 초기화 실패 입니다. 확인 후 다시 실행 시켜 주세요");

        //				return false;
        //			}
        //		}

        //		if (CAxtDIO.DIOIsInitialized() == 0)                // DIO모듈을 사용할 수 있도록 라이브러리가 초기화되었는지 확인한다
        //		{
        //			if (CAxtDIO.InitializeDIO() == 0)               // DIO모듈을 초기화한다. 열려있는 모든베이스보드에서 DIO모듈을 검색하여 초기화한다
        //			{
        //				System.Windows.MessageBox.Show("DIO모듈 초기화 실패 입니다. 확인 후 다시 실행 시켜 주세요");

        //				return false;
        //			}
        //		}
        //		IsOpened = true;
        //		return true;
        //	}

        //	public void D_Out(ushort ch, int state)
        //	{
        //		CAxtDIO.DIOwrite_outport_bit(1, ch, state);
        //	}
        //	public void D_In(ushort ch)
        //	{
        //		CAxtDIO.DIOread_inport_bit(0, ch);
        //	}

        //	public void CloseWindow()
        //	{
        //		if (CAxtLib.AxtIsInitialized() == 1)
        //		{
        //			CAxtLib.AxtCloseDeviceAll();
        //			CAxtLib.AxtClose();
        //		}
        //		IsOpened = false;
        //	}
        #endregion

        //static System.Windows.Window main = null;
		//private static Thread EventThread = null;
		//private static bool bThread = false;
		//private static uint hInterrupt = 0;

		public readonly static uint INFINITE = 0xFFFFFFFF;
		public readonly static uint STATUS_WAIT_0 = 0x00000000;
		public readonly static uint WAIT_OBJECT_0 = ((STATUS_WAIT_0) + 0);

		[DllImport("kernel32", EntryPoint = "WaitForSingleObject", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern uint WaitForSingleObject(uint hHandle, uint dwMilliseconds);

		[DllImport("KERNEL32", EntryPoint = "SetEvent", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern bool SetEvent(long hEvent);

		//private static Byte[] INData = new byte[Constants.MAX_PLC_PORT_SIZE];
		//private static Byte[] OUTData = new byte[Constants.MAX_PLC_PORT_SIZE];
		private static int PINModuleNum = -1;
		private static int POUTModuleNum = -1;

		private static object lockInObject = new object();
		private static object lockOutObject = new object();

		//private static DIODataArrivalHandler DataArrivalCallback;
		//public static uint[] DIOINValue = new uint[4];
		//public static uint[] DIOOUTValue = new uint[4];
		//public static uint[] DIOINValueB = new uint[4];
		//public static uint[] DIOOUTValueB = new uint[4];

		const int LED_PULSE = 5;
		private static int lampLow = 0;

		static DIOControl()
        {
			//for (int i = 0; i < Constants.MAX_PLC_PORT_SIZE; i++)
			//{
			//	INData[i] = 0;
			//	OUTData[i] = 0;
			//}
			string className = "DIOControl";
			string funcName = "DIOControl";
			ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

			string value = "";
            try
            {
				Util.GetPrivateProfileValue("CONFIG", "LEDLOWCONTROL", "0", ref value, "CameraConfig.ini");
				lampLow = Convert.ToInt32(value);
			}
			catch(Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
				lampLow = 0;
			}
		}

		//public static void SetCallBackFunction(DIODataArrivalHandler callback)
        //{
		//	DataArrivalCallback = callback;
		//}

		public static int OpenDevice(System.Windows.Window main, out string ErrorCode)
		{
			int retval = 0;
			string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
			string funcName = MethodBase.GetCurrentMethod().Name;
			ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
			try
            {
				if (CAXL.AxlIsOpened() != 0)
				{
					ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Already opened., Thread.CurrentThread.ManagedThreadId");
					ErrorCode = "";
					return 0;
				}

				retval = (int)CAXL.AxlOpen(7);
				if (retval == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
				{
					uint uStatus = 0;
					retval = (int)CAXD.AxdInfoIsDIOModule(ref uStatus);
					if (retval == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
					{
						if ((AXT_EXISTENCE)uStatus == AXT_EXISTENCE.STATUS_EXIST)
						{
							int nModuleCount = 0;
							retval = (int)CAXD.AxdInfoGetModuleCount(ref nModuleCount);
							if (retval == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
							{
								short i = 0;
								int SIO_Num = 0;
								//int SIO_DO_Num = 0;
								int nBoardNo = 0;
								int nModulePos = 0;
								uint uModuleID = 0;
								//string strData = "";

								for (i = 0; i < nModuleCount; i++)
								{
									if (CAXD.AxdInfoGetModule(i, ref nBoardNo, ref nModulePos, ref uModuleID) == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
									{
										switch ((AXT_MODULE)uModuleID)
										{
											case AXT_MODULE.AXT_SIO_DI32:
												PINModuleNum = SIO_Num++;
												CAXD.AxdiLevelSetInportDword(PINModuleNum, 0, 0xffffffff);
												ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("PINModuleNum = {0}", PINModuleNum), Thread.CurrentThread.ManagedThreadId);
												break;
											//strData = String.Format("[{0:D2}:{1:D2}] SIO-DI32", nBoardNo, i); break;
											case AXT_MODULE.AXT_SIO_DO32P:
												POUTModuleNum = SIO_Num++;
												CAXD.AxdoLevelSetOutportDword(POUTModuleNum, 0, 0xffffffff);
												ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("POUTModuleNum = {0}", POUTModuleNum), Thread.CurrentThread.ManagedThreadId);
												break;
											case AXT_MODULE.AXT_SIO_DB32P:
											case AXT_MODULE.AXT_SIO_DO32T:
											case AXT_MODULE.AXT_SIO_DB32T:
											case AXT_MODULE.AXT_SIO_RDI32:
											case AXT_MODULE.AXT_SIO_RDO32:
											case AXT_MODULE.AXT_SIO_RDB128MLII:
											case AXT_MODULE.AXT_SIO_RSIMPLEIOMLII:
											case AXT_MODULE.AXT_SIO_RDO16AMLII:
											case AXT_MODULE.AXT_SIO_RDO16BMLII:
											case AXT_MODULE.AXT_SIO_RDB96MLII:
											case AXT_MODULE.AXT_SIO_RDO32RTEX:
											case AXT_MODULE.AXT_SIO_RDI32RTEX:
											case AXT_MODULE.AXT_SIO_RDB32RTEX:
											case AXT_MODULE.AXT_SIO_DI32_P:
											case AXT_MODULE.AXT_SIO_DO32T_P:
											case AXT_MODULE.AXT_SIO_RDB32T:
												SIO_Num++;
												break;
												//strData = String.Format("[{0:D2}:{1:D2}] SIO-DO32P", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_DB32P: strData = String.Format("[{0:D2}:{1:D2}] SIO-DB32P", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_DO32T: strData = String.Format("[{0:D2}:{1:D2}] SIO-DO32T", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_DB32T: strData = String.Format("[{0:D2}:{1:D2}] SIO-DB32T", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RDI32: strData = String.Format("[{0:D2}:{1:D2}] SIO_RDI32", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RDO32: strData = String.Format("[{0:D2}:{1:D2}] SIO_RDO32", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RDB128MLII: strData = String.Format("[{0:D2}:{1:D2}] SIO-RDB128MLII", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RSIMPLEIOMLII: strData = String.Format("[{0:D2}:{1:D2}] SIO-RSIMPLEIOMLII", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RDO16AMLII: strData = String.Format("[{0:D2}:{1:D2}] SIO-RDO16AMLII", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RDO16BMLII: strData = String.Format("[{0:D2}:{1:D2}] SIO-RDO16BMLII", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RDB96MLII: strData = String.Format("[{0:D2}:{1:D2}] SIO-RDB96MLII", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RDO32RTEX: strData = String.Format("[{0:D2}:{1:D2}] SIO-RDO32RTEX", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RDI32RTEX: strData = String.Format("[{0:D2}:{1:D2}] SIO-RDI32RTEX", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RDB32RTEX: strData = String.Format("[{0:D2}:{1:D2}] SIO-RDB32RTEX", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_DI32_P: strData = String.Format("[{0:D2}:{1:D2}] SIO-DI32_P", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_DO32T_P: strData = String.Format("[{0:D2}:{1:D2}] SIO-DO32T_P", nBoardNo, i); break;
												//case AXT_MODULE.AXT_SIO_RDB32T: strData = String.Format("[{0:D2}:{1:D2}] SIO-RDB32T", nBoardNo, i); break;
										}

										//comboModule.Items.Add(strData);
									}
								}

								if ((PINModuleNum < 0) || (POUTModuleNum < 0))
								{
									ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "PINModuleNum or POUTModuleNum is invalid", Thread.CurrentThread.ManagedThreadId);
									ErrorCode = "00PMU0000001";
									return -1;
								}
							}
							else
							{
								ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("AxdInfoGetModuleCount() returns {0}", retval), Thread.CurrentThread.ManagedThreadId);
								ErrorCode = string.Format("00PM{0:X8}", retval);
								return retval;
							}
						}
						else
						{
							ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Module not exist.", Thread.CurrentThread.ManagedThreadId);
							//MessageBox.Show("Module not exist.");
							ErrorCode = "00PMU0000002";
							return -2;
						}
					}
					else
					{
						ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("AxdInfoIsDIOModule() returns {0}", retval), Thread.CurrentThread.ManagedThreadId);
						ErrorCode = string.Format("00PM{0:X8}", retval);
						return retval;
					}
				}
				else
				{
					ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("Open error({0})", retval), Thread.CurrentThread.ManagedThreadId);
					//MessageBox.Show("Open Error!");
					ErrorCode = string.Format("00PM{0:X8}", retval);
					//retval = 0;
					return retval;
				}

				//retval = SelectEvent(out ErrorCode);
			}
			catch (Exception ex)
            {
				ErrorCode = string.Format("00PE{0:X8}", Math.Abs(ex.HResult));
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
				return ex.HResult;
			}

			SendInitialSignal();

			ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
			ErrorCode = "";
			return retval;
			//return true;
		}

		private static void SendInitialSignal()
        {
			string className = "DIOControl";
			string funcName = "SendInitialSignal";
			try
			{
				if (lampLow != 0)
				{
					DIOSendData(0, 1);
					DIOSendData(1, 1);
				}
				else
				{
					DIOSendData(0, 0);
					DIOSendData(1, 0);
				}
				DIOSendData(2, 0);
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		//private static int InterruptProc(int nModuleNo, uint uFlag)
		//{
		//	//int i = 0;
		//	//int j = 0;
		//	uint uValue = 0;
		//	//string strClass = "";
		//	string strInt = "";
		//	uint uFlagTemp = uFlag;
		//	byte[] INData = new byte[Constants.MAX_PLC_PORT_SIZE];

		//	//int InputCounts = ; //전체 입력 접점 의 수
		//	for (int i = 0; i < Constants.MAX_PLC_PORT_SIZE; i++)
		//	{
		//		if (((uFlag >> i) & 0x01) != 0)
		//		{
		//			CAXD.AxdiReadInportBit(nModuleNo, i, ref uValue);
		//			if (uValue != 0)
		//                  {
		//				INData[i] = 1;
		//				strInt = string.Format("{0}'s modeul {1}th input bit Rising interrupt", nModuleNo, i);
		//			}
		//			else
		//			{
		//				INData[i] = 0;
		//				strInt = string.Format("{0}'s modeul {1}th input bit Falling interrupt", nModuleNo, i);
		//			}
		//		}
		//	}
		//	DataArrivalCallback(ref INData, Constants.MAX_PLC_PORT_SIZE, uFlagTemp);

		//	//switch (uClass)
		//	//{
		//	//	case AXT_INTERRUPT_CLASS.KIND_MESSAGE:
		//	//		strClass = "Message";
		//	//		break;

		//	//	case AXT_INTERRUPT_CLASS.KIND_CALLBACK:
		//	//		strClass = "Callback";
		//	//		break;

		//	//	case AXT_INTERRUPT_CLASS.KIND_EVENT:
		//	//		strClass = "Event";
		//	//		break;
		//	//}

		//	//for (i = 0; i < 4; i++)
		//	//{
		//	//	for (j = 0; j < 8; j++)
		//	//	{
		//	//		if ((((uFlag >> (i * 8)) >> j) & 0x01) == 0x01)
		//	//		{
		//	//			CAXD.AxdiReadInportBit(nModuleNo, ((i * 8) + j), ref uValue);

		//	//			if (uValue == 0x01)
		//	//				strInt = String.Format("{0:s} : Rising Int Set Bit {1:X2}", strClass, (i * 8) + j);
		//	//			else
		//	//				strInt = String.Format("{0:s} : Falling Int Set Bit {1:X2}", strClass, (i * 8) + j);

		//	//			//if (textInterrupt.TextLength == 0)
		//	//			//	textInterrupt.Text += strInt;
		//	//			//else
		//	//			//	textInterrupt.Text += "\r\n" + strInt;

		//	//			//textInterrupt.SelectionStart = textInterrupt.TextLength;
		//	//			//textInterrupt.ScrollToCaret();
		//	//			Debug.WriteLine(strInt);
		//	//		}
		//	//	}
		//	//}

		//	return 0;
		//}


		public static void DIOOutputOnSendData(int ch)
		{
			string className = "DIOControl";
			string funcName = "DIOOutputOnSendData";
			try
			{
				lock (lockOutObject)
				{
					CAXD.AxdoWriteOutportBit(POUTModuleNum, ch, 1);
					for (int i = 0; i < 100; i++)
						Thread.Sleep(10);
					CAXD.AxdoWriteOutportBit(POUTModuleNum, ch, 0);
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		public static void DIOSendData(int ch, uint state, int level=0)
        {
			string className = "DIOControl";
			string funcName = "DIOSendData";
			try
			{
				lock (lockOutObject)
				{
					CAXD.AxdoWriteOutportBit(POUTModuleNum, ch, state);
					//ITNTTraceLog.Instance.Trace(level, "{0}::{1}()  {2}", "DIOControl", "DIOSendData", string.Format("SEND to {0} {1}", ch, state));
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		public static void DIOReadInportBit(int offset, ref uint ch)
        {
			string className = "DIOControl";
			string funcName = "DIOReadInportBit";
			try
			{
				lock (lockInObject)
				{
					CAXD.AxdiReadInportBit(PINModuleNum, offset, ref ch);
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		public static void DIOReadOutportBit(int offset, ref uint ch)
		{
		}

		public static void DIOReadInportDWORD(int offset, ref uint ch)
		{
			string className = "DIOControl";
			string funcName = "DIOReadInportDWORD";
			try
			{
				lock (lockInObject)
				{
					CAXD.AxdiReadInportDword(PINModuleNum, offset, ref ch);
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		public static void DIOWriteOutportDWORD(int offset, uint ch)
		{
			string className = "DIOControl";
			string funcName = "DIOWriteOutportDWORD";
			try
			{
				lock (lockInObject)
				{
					CAXD.AxdoWriteOutportDword(POUTModuleNum, offset, ch);
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		public static void DIOWriteOutportWORD(int offset, uint ch)
		{
			string className = "DIOControl";
			string funcName = "DIOWriteOutportWORD";
			try
			{
				lock (lockInObject)
				{
					CAXD.AxdoWriteOutportWord(POUTModuleNum, offset, ch);
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		public static void DIOWriteOutportByte(int offset, uint ch)
		{
			string className = "DIOControl";
			string funcName = "DIOWriteOutportByte";
			try
			{
				lock (lockInObject)
				{
					CAXD.AxdoWriteOutportByte(POUTModuleNum, offset, ch);
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		public static void DIOWriteOutportBit(int offset, uint ch)
		{
			string className = "DIOControl";
			string funcName = "DIOWriteOutportBit";
			try
			{
				lock (lockInObject)
				{
					CAXD.AxdoWriteOutportBit(POUTModuleNum, offset, ch);
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		//Get 1 Bit Status on DWORD
		public static int GetBitStatus(uint uData, uint pos)
		{
			string className = "DIOControl";
			string funcName = "GetBitStatus";
			try
			{
				return ((int)uData & (1 << (int)pos)) >> (int)pos;
			}
			catch (Exception ex)
            {
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
				return -1;
			}
		}
		//public static void DIOOutputOnSendData(int ch)
		//{
		//	DIOControl.DIOSendData(ch, 1);
		//	delay_ms(1000);
		//	DIOControl.DIOSendData(ch, 0);
		//}

		public static void First_Light_ON()
		{
			try
			{
				if (lampLow != 0)
				{
					lock (lockOutObject)
					{
						DIOSendData(0, 0, 3);
						//CAXD.AxdoWriteOutportBit(POUTModuleNum, 0, 0);
						Thread.Sleep(LED_PULSE);
						DIOSendData(0, 1, 3);
						//CAXD.AxdoWriteOutportBit(POUTModuleNum, 0, 1);
					}
				}
				else
				{
					lock (lockOutObject)
					{
						DIOSendData(0, 1, 3);
						//CAXD.AxdoWriteOutportBit(POUTModuleNum, 0, 1);
						Thread.Sleep(LED_PULSE);
						DIOSendData(0, 0, 3);
						//CAXD.AxdoWriteOutportBit(POUTModuleNum, 0, 0);
					}
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", "DIOControl", "First_Light_ON", string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		public static void Next_Light_ON()
		{
			string className = "DIOControl";
			string funcName = "Next_Light_ON";
			try
			{
				if (lampLow != 0)
				{
					lock (lockOutObject)
					{
						DIOSendData(1, 0, 3);
						//CAXD.AxdoWriteOutportBit(POUTModuleNum, 1, 0);
						Thread.Sleep(LED_PULSE);
						DIOSendData(1, 1, 3);
						//CAXD.AxdoWriteOutportBit(POUTModuleNum, 1, 1);
					}
				}
				else
				{
					lock (lockOutObject)
					{
						DIOSendData(1, 1, 3);
						//CAXD.AxdoWriteOutportBit(POUTModuleNum, 1, 1);
						Thread.Sleep(LED_PULSE);
						DIOSendData(1, 0, 3);
						//CAXD.AxdoWriteOutportBit(POUTModuleNum, 1, 0);
					}
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		public static void Center_Light_ON()
		{
			string className = "DIOControl";
			string funcName = "Center_Light_ON";

			try
			{
				lock (lockOutObject)
				{
					DIOSendData(2, 1);
					//CAXD.AxdoWriteOutportBit(POUTModuleNum, 2, 1);
					//Thread.Sleep(LED_PULSE);
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		public static void Center_Light_OFF()
		{
			string className = "DIOControl";
			string funcName = "Center_Light_OFF";
			try
			{
				lock (lockOutObject)
				{
					DIOSendData(2, 0);
					//CAXD.AxdoWriteOutportBit(POUTModuleNum, 2, 0);
					//Thread.Sleep(LED_PULSE);
				}
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}

		//public static void DIOReadOutportDWORD(ref uint ch)
		//{
		//	lock (lockOutObject)
		//	{
		//		CAXD.AxdoReadOutport(0, ref ch);
		//	}
		//}

		//private static void ThreadProc()
		//{
		//	lock (lockObject)
		//	{
		//		uint uret = CAXD.AxdiInterruptSetModule(PINModuleNum, (IntPtr)null, 0, null, ref hInterrupt);
		//	}
		//	while (bThread)
		//	{
		//		if (WaitForSingleObject(hInterrupt, 1000) == WAIT_OBJECT_0)
		//		{
		//			int nModuleNo = 0;
		//			uint uFlag = 0;
		//			CAXD.AxdiInterruptRead(ref nModuleNo, ref uFlag);
		//			InterruptProc(nModuleNo, uFlag);
		//		}
		//		Thread.Sleep(5);
		//	}
		//	EventThread = null;
		//}

		//private static void ThreadGet()
		//{
		//	uint uret = CAXD.AxdiInterruptSetModule(PINModuleNum, (IntPtr)null, 0, null, ref hInterrupt);
		//	uret = CAXL.AxlInterruptEnable();
		//	uret = CAXD.AxdiInterruptSetModuleEnable(PINModuleNum, (uint)AXT_USE.ENABLE);

		//	while (bThread)
		//	{
		//		if (WaitForSingleObject(hInterrupt, INFINITE) == WAIT_OBJECT_0)
		//		{
		//			int nModuleNo = 0;
		//			uint uFlag = 0;
		//			CAXD.AxdiInterruptRead(ref nModuleNo, ref uFlag);
		//			InterruptProc(nModuleNo, uFlag);
		//		}
		//	}
		//	EventThread = null;
		//}


		//private static int SelectEvent(out string ErrorCode)
		//{
		//	string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
		//	string funcName = MethodBase.GetCurrentMethod().Name;

		//	ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

		//	try
		//	{
		//		if (EventThread != null)
		//		{
		//			bThread = false;
		//			SetEvent(hInterrupt);
		//			EventThread.Abort();
		//			EventThread = null;
		//		}

		//		if (!bThread)
		//		{
		//			bThread = true;
		//			EventThread = new Thread(new ThreadStart(ThreadProc));
		//			EventThread.Start();

		//			CAXL.AxlInterruptEnable();
		//			CAXD.AxdiInterruptSetModuleEnable(PINModuleNum, (uint)AXT_USE.ENABLE);
		//		}
		//	}
		//	catch (Exception ex)
		//          {
		//		ErrorCode = string.Format("00PE{0:X8}", Math.Abs(ex.HResult));
		//		ITNTTraceLog.Instance.Trace(0, "CutImage() Error - Exception : {0}", ex.HResult);
		//		return ex.HResult;
		//	}

		//	ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
		//	ErrorCode = "";
		//	return 0;
		//}

		//private static bool SelectHighIndex(int nIndex, uint uValue)
		//{
		//	int nModuleCount = 0;

		//	CAXD.AxdInfoGetModuleCount(ref nModuleCount);

		//	if (nModuleCount > 0)
		//	{
		//		int nBoardNo = 0;
		//		int nModulePos = 0;
		//		uint uModuleID = 0;

		//		CAXD.AxdInfoGetModule(PINModuleNum, ref nBoardNo, ref nModulePos, ref uModuleID);

		//		switch ((AXT_MODULE)uModuleID)
		//		{
		//			case AXT_MODULE.AXT_SIO_DO32P:
		//			case AXT_MODULE.AXT_SIO_DO32T:
		//			case AXT_MODULE.AXT_SIO_RDO32:
		//				CAXD.AxdoWriteOutportBit(PINModuleNum, nIndex, uValue);
		//				break;

		//			default:
		//				return false;
		//		}
		//	}

		//	return true;
		//}

		//private static bool SelectLowIndex(int nIndex, uint uValue)
		//{
		//	int nModuleCount = 0;

		//	CAXD.AxdInfoGetModuleCount(ref nModuleCount);

		//	if (nModuleCount > 0)
		//	{
		//		int nBoardNo = 0;
		//		int nModulePos = 0;
		//		uint uModuleID = 0;

		//		CAXD.AxdInfoGetModule(PINModuleNum, ref nBoardNo, ref nModulePos, ref uModuleID);

		//		//switch ((AXT_MODULE)uModuleID)
		//		//{
		//		//	case AXT_MODULE.AXT_SIO_DO32P:
		//		//	case AXT_MODULE.AXT_SIO_DO32T:
		//		//	case AXT_MODULE.AXT_SIO_RDO32:
		//		//		CAXD.AxdoWriteOutportBit(comboModule.SelectedIndex, nIndex + 16, uValue);
		//		//		break;
		//		//	case AXT_MODULE.AXT_SIO_DB32P:
		//		//	case AXT_MODULE.AXT_SIO_DB32T:
		//		//	case AXT_MODULE.AXT_SIO_RDB128MLII:
		//		//		CAXD.AxdoWriteOutportBit(comboModule.SelectedIndex, nIndex, uValue);
		//		//		break;

		//		//	default:
		//		//		return false;
		//		//}
		//	}

		//	return true;
		//}

		public static void CloseDevice()
		{
			string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
			string funcName = MethodBase.GetCurrentMethod().Name;
			ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

			try
			{
				SendInitialSignal();

				if (CAXL.AxlIsOpened() == 0)
				{
					ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Already closed.", Thread.CurrentThread.ManagedThreadId);
					return;
				}

				CAXL.AxlInterruptDisable();

				//if (EventThread != null)
				//{
				//	bThread = false;
				//	SetEvent(hInterrupt);
				//	EventThread.Abort();
				//	EventThread = null;
				//}

				CAXL.AxlClose();
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
			}
			catch (Exception ex)
			{
				ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
			}
		}
	}
}
