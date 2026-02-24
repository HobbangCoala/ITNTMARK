using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ITNTUTIL
{
    public class ITNTDBManager
    {
        bool bDBOpened = false;
        private MySqlConnection m_Conn = null;

        public ITNTDBManager(string connectionstring)
        {

        }

        public async Task<int> OpenAsync(string ConnectionString)
        {
            int retval = 0;

            try
            {
                if (bDBOpened)
                    return 0;

                using (m_Conn = new MySqlConnection(ConnectionString))
                {
                    await m_Conn.OpenAsync();
                    //using (var command = conn.CreateCommand())
                    //{
                    //    command.CommandText = "DROP TABLE IF EXISTS inventory;";
                    //    await command.ExecuteNonQueryAsync();
                    //    Console.WriteLine("Finished dropping table (if existed)");

                    //    command.CommandText = "CREATE TABLE inventory (id serial PRIMARY KEY, name VARCHAR(50), quantity INTEGER);";
                    //    await command.ExecuteNonQueryAsync();
                    //    Console.WriteLine("Finished creating table");

                    //    command.CommandText = @"INSERT INTO inventory (name, quantity) VALUES (@name1, @quantity1),
                    //    (@name2, @quantity2), (@name3, @quantity3);";
                    //    command.Parameters.AddWithValue("@name1", "banana");
                    //    command.Parameters.AddWithValue("@quantity1", 150);
                    //    command.Parameters.AddWithValue("@name2", "orange");
                    //    command.Parameters.AddWithValue("@quantity2", 154);
                    //    command.Parameters.AddWithValue("@name3", "apple");
                    //    command.Parameters.AddWithValue("@quantity3", 100);

                    //    int rowCount = await command.ExecuteNonQueryAsync();
                    //    Console.WriteLine(String.Format("Number of rows inserted={0}", rowCount));
                    //}

                    //// connection will be closed by the 'using' block
                    //Console.WriteLine("Closing connection");
                    bDBOpened = true;
                }
            }
            catch(Exception ex)
            {
                bDBOpened = false;
                retval = ex.HResult;
            }

            return retval;
        }

        public class DBExecuteReaderResult
        {
            public int executeResult;
            public System.Data.Common.DbDataReader dataReader;

            public DBExecuteReaderResult()
            {
                executeResult = 0;
                dataReader = null;
            }
        }

        public async Task<DBExecuteReaderResult> ExecuteReaderAsync(string commandString)
        {
            DBExecuteReaderResult retval = new DBExecuteReaderResult();
            bool exeret = false;
            try
            {
                if (!bDBOpened || (m_Conn == null))
                {
                    retval.executeResult = -1;
                    return retval;
                }

                using (var command = m_Conn.CreateCommand())
                {
                    command.CommandText = commandString;

                    using (retval.dataReader = await command.ExecuteReaderAsync())
                    {
                        await retval.dataReader.ReadAsync();

                        //while (await reader.ReadAsync())
                        //{
                        //    Console.WriteLine(string.Format(
                        //        "Reading from table=({0}, {1}, {2})",
                        //        reader.GetInt32(0),
                        //        reader.GetString(1),
                        //        reader.GetInt32(2)));
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                retval.executeResult = ex.HResult;
            }

            return retval;
        }
    }
}
