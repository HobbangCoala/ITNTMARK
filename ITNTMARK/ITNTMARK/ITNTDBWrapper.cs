using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITNTUTIL;
using MySql.Data.MySqlClient;
using System.Data;
using System.Net;
using System.Threading;

namespace ITNTMARK
{
    class ITNTDBWrapper
    {
        Mutex dbLockMutex = null;// = new Mutex();
        public ITNTDBWrapper()
        {
            dbLockMutex = new Mutex(true);
        }

        public int ExecuteCommand(string connstring, string commandstring, CommandMode cmdtype, CommandTypeEnum type, ref DataTable retTable, ref object scolar)
        {
            int retval = 0;
            dbLockMutex.WaitOne(10000);
            ITNTDBManage dbManage = new ITNTDBManage(connstring);
            dbManage.Open(connstring);

            dbManage.CommandText = commandstring;
            if (cmdtype == CommandMode.NonQuery)
            {
                retval = dbManage.ExecuteCommandNonQuery(type);
            }
            else if(cmdtype == CommandMode.Scalar)
            {
                retval = dbManage.ExecuteCommandScalar(type, ref scolar);
            }
            else if(cmdtype == CommandMode.Reader)
            {
                retval = dbManage.ExecuteCommandReader(type, ref retTable);
            }

            dbLockMutex.ReleaseMutex();
            return retval;
        }
    }
}
