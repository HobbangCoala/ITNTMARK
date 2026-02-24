using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using ITNTCOMMON;


#pragma warning disable 0219
#pragma warning disable 4014

namespace ITNTUTIL
{
    #region All Enum
    public enum CommandTypeEnum
    {
        Text,
        StoredProcedure
    }

    public enum CommandMode
    {
        NonQuery,
        Scalar,
        Reader
    }
    #endregion


    class ITNTDBManage : IDisposable
    {
        private string m_sConnectionString;
        private string m_strSqlCommandText;
        private MySqlConnection m_Conn = null;
        //private MySqlTransaction m_oTrans = null;
        private object m_oScalar;

        //Mutex mutex = null;
        protected internal string CommandText
        {
            set
            {
                m_strSqlCommandText = value;
            }
        }


        protected internal ITNTDBManage(string sConnectionString)
        {
            m_sConnectionString = sConnectionString;
            //if (mutex == null)
            //    mutex = new Mutex(true, "DBMUTEX");
        }

        //public ITNTDBManage()
        //{
        //    //if (mutex == null)
        //    //    mutex = new Mutex(true, "DBMUTEX");
        //}


        /// <summary>
        /// Call or Execute all Reader 
        /// </summary>
        protected internal int ExecuteCommandReader(CommandTypeEnum sqlType, ref DataTable oTable)
        {
            int rowAffected;
            return ExecuteCommand(CommandMode.Reader, sqlType, ref m_oScalar, out rowAffected, ref oTable);
        }

        //protected internal int ExecuteCommandReader(CommandTypeEnum sqlType, out int rowAffected, ref DataTable oTable)
        //{
        //    return ExecuteCommand(CommandMode.Reader, sqlType, ref m_oScalar, out rowAffected, ref oTable);
        //}

        protected internal int ExecuteCommandReader(ref DataTable oTable)
        {
            int rowAffected;
            object oScalar = null;
            return ExecuteCommand(CommandMode.Reader, CommandTypeEnum.StoredProcedure, ref oScalar, out rowAffected, ref oTable);
        }

//        protected internal int ExecuteCommandReader(ref MySqlDataReader oReader)
//        {
//            int rowAffected;
//            object oScalar = null;
//            return 0;
////            return ExecuteCommand(CommandMode.Reader, CommandTypeEnum.StoredProcedure, ref oScalar, out rowAffected, ref oTable);
//        }

        /// <summary>
        /// Call or Execute all Scalar 
        /// </summary>
        protected internal int ExecuteCommandScalar()
        {
            int rowAffected;
            DataTable oTable = new DataTable();
            return ExecuteCommand(CommandMode.Scalar, CommandTypeEnum.StoredProcedure, ref m_oScalar, out rowAffected, ref oTable);
        }

        protected internal int ExecuteCommandScalar(CommandTypeEnum sqlType, ref object eScalar)
        {
            int rowAffected;
            DataTable oTable = new DataTable();
            return ExecuteCommand(CommandMode.Scalar, sqlType, ref eScalar, out rowAffected, ref oTable);
        }

        protected internal object ExecuteCommandScalar(ref object eScalar)
        {
            int rowAffected;
            DataTable oTable = new DataTable();
            ExecuteCommand(CommandMode.Scalar, CommandTypeEnum.StoredProcedure, ref eScalar, out rowAffected, ref oTable);
            return m_oScalar;
        }



        /// <summary>
        /// Call or Execute all Non Query 
        /// </summary>
        /// 
        protected internal int ExecuteCommandNonQuery(CommandTypeEnum sqlType)
        {
            int nRowsAffected;
            DataTable oDataTable = new DataTable();
            return ExecuteCommand(CommandMode.NonQuery, sqlType, ref m_oScalar, out nRowsAffected, ref oDataTable);
        }

        protected internal int ExecuteCommandNonQuery(CommandTypeEnum sqlType, out int nRowsAffected)
        {
            DataTable oDataTable = new DataTable();
            return ExecuteCommand(CommandMode.NonQuery, sqlType, ref m_oScalar, out nRowsAffected, ref oDataTable);
        }

        protected internal int ExecuteCommandNonQuery(out int nRowsAffected)
        {
            DataTable oDataTable = new DataTable();
            return ExecuteCommand(CommandMode.NonQuery, CommandTypeEnum.StoredProcedure, ref m_oScalar, out nRowsAffected, ref oDataTable);
        }

        protected internal int ExecuteCommandNonQuery()
        {
            int nRowsAffected;
            DataTable oDataTable = new DataTable();
            return ExecuteCommand(CommandMode.NonQuery, CommandTypeEnum.StoredProcedure, ref m_oScalar, out nRowsAffected, ref oDataTable);
        }


        #region ExecuteCommands
        private int ExecuteCommand(CommandMode eMode, CommandTypeEnum sqlType, ref object ScalarOutput, out int nRowsAffected, ref DataTable oTable)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            if ((m_Conn != null) && (m_Conn.State == ConnectionState.Open))
            {
                nRowsAffected = 0;
                using (MySqlCommand oCmd = new MySqlCommand(m_strSqlCommandText, m_Conn))
                {
                    oCmd.CommandText = m_strSqlCommandText;
                    switch (sqlType)
                    {
                        case CommandTypeEnum.StoredProcedure:
                            oCmd.CommandType = CommandType.StoredProcedure;
                            break;
                        case CommandTypeEnum.Text:
                            oCmd.CommandType = CommandType.Text;
                            break;
                    }

                    //foreach (Parameters List in ParameterList)
                    //{
                    //    MySqlParameter oParam = new MySqlParameter(List.ParameterName, List.ParameterValues);
                    //    oParam.DbType = List.ParameterType;
                    //    oCmd.Parameters.Add(oParam);
                    //}
                    try
                    {
                        MySqlParameter oRetParam = new MySqlParameter("RETURN_VALUE", DBNull.Value);
                        oRetParam.Direction = ParameterDirection.ReturnValue;
                        oCmd.Parameters.Add(oRetParam);
                        oCmd.Connection = m_Conn;
                        nRowsAffected = 0;
                    }
                    catch (Exception ex)
                    {
                        nRowsAffected = 0;
                        Debug.WriteLine(ex.Message + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                        ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION 1 - CODE = {0:X}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                        throw new DataException(ex.Message);
                    }

#if ActivateTransaction
                            if (oTrans != null)
                            {
                                oCmd.Transaction = oTrans;
                            }
#endif
                    switch (eMode)
                    {
                        case CommandMode.NonQuery:
                            try
                            {
                                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ExecuteNonQuery START", Thread.CurrentThread.ManagedThreadId);
                                nRowsAffected = oCmd.ExecuteNonQuery();
                                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ExecuteNonQuery END", Thread.CurrentThread.ManagedThreadId);
                            }
                            catch (Exception ex)
                            {
                                nRowsAffected = 0;
                                Debug.WriteLine(ex.Message + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                                throw new DataException(ex.Message);
                            }
                            break;
                        case CommandMode.Scalar:
                            try
                            {
                                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ExecuteScalar START", Thread.CurrentThread.ManagedThreadId);
                                ScalarOutput = oCmd.ExecuteScalar();
                                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "ExecuteScalar END", Thread.CurrentThread.ManagedThreadId);
                            }
                            catch (Exception ex)
                            {
                                nRowsAffected = 0;
                                Debug.WriteLine(ex.Message + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                                throw new DataException(ex.Message);
                            }
                            break;
                        case CommandMode.Reader:
                            try
                            {
                                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Fill START", Thread.CurrentThread.ManagedThreadId);
                                MySqlDataAdapter oAdapter = new MySqlDataAdapter(oCmd);
                                if (oTable != null)
                                {
                                    oAdapter.Fill(oTable);
                                }
                                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "Fill END", Thread.CurrentThread.ManagedThreadId);
                            }
                            catch (Exception ex)
                            {
                                nRowsAffected = 0;
                                Debug.WriteLine(ex.Message + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                                return -2;
                                //                                throw new Exception(ex.Message);
                            }
                            break;
                    }
                    ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
                    return 0;
                    //                    return Convert.ToInt32(oCmd.Parameters["RETURN_VALUE"].Value);
                }
            }
            else
            {
                nRowsAffected = 0;
                ITNTTraceLog.Instance.Trace(0, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END - 3", Thread.CurrentThread.ManagedThreadId);
                return -3;
//                throw new DataException("Connection not open");
            }
        }
        #endregion

        #region Connection Dispose
        public void Dispose()
        {
            if (m_Conn != null)
            {
                m_Conn.Dispose();
            }
            //mutex.ReleaseMutex();
        }
        #endregion


        //protected internal void Open()
        //{
        //    try
        //    {
        //        //mutex.WaitOne();
        //        if((m_Conn == null) ||(m_Conn.State == ConnectionState.Closed))
        //        {
        //            m_Conn = new MySqlConnection();
        //            m_Conn.ConnectionString = m_sConnectionString;
        //            m_Conn.Open();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //mutex.ReleaseMutex();
        //        throw ex;
        //    }
        //}

        protected internal void Open(string ConnectionString)
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);

            try
            {
                //mutex.WaitOne();
                if ((m_Conn == null) || (m_Conn.State == ConnectionState.Closed))
                {
                    m_Conn = new MySqlConnection(ConnectionString);
                    m_sConnectionString = ConnectionString;
                    m_Conn.ConnectionString = m_sConnectionString;
                    ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "OPEN START", Thread.CurrentThread.ManagedThreadId);
                    m_Conn.Open();
                    ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END OPEN", Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                //mutex.ReleaseMutex();
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
                throw ex;
            }
        }


        /// <summary>
        /// Close the connection to the Sql db
        /// </summary>
        protected internal void Close()
        {
            string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            string funcName = MethodBase.GetCurrentMethod().Name;
            ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "START", Thread.CurrentThread.ManagedThreadId);
            try
            {
                if (m_Conn != null)
                {
                    m_Conn.Close();
                    m_Conn = null;
                }
                //mutex.ReleaseMutex();
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, "END", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                ITNTTraceLog.Instance.Trace(1, "{0}:{3:D4}:{1}()  {2}", className, funcName, string.Format("EXCEPTION - CODE = {0}, MSG = {1}", ex.HResult, ex.Message), Thread.CurrentThread.ManagedThreadId);
            }
        }
    }
}
