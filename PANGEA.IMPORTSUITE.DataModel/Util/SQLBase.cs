using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PANGEA.IMPORTSUITE.DataModel.Util
{
    public class SQLBase
    {


        //Conexiones a la Base de Datos
        public SqlConnection Connection;
        public SqlCommand Command;

        /// <summary>
        /// Constuctor
        /// </summary>
        public SQLBase()
        {
            Connection = new SqlConnection(); //GetCnnString()
            Command = new SqlCommand();
            Command.CommandTimeout = 600;

            //try
            //{
            //    Connection.Open();
            //    Connection.Close();
            //}
            //catch (SqlException ex)
            //{ throw new Exception("Connection Failed: " + ex.Message); }

        }

        #region Connection



        public void OpenConnection()
        {
            if (Connection.State == System.Data.ConnectionState.Closed)
                Connection.Open();
        }


        public void CloseConnection()
        {
            if (Connection.State == System.Data.ConnectionState.Open)
                Connection.Close();
        }

        #endregion


        #region AddParameter methods
        public void AddParameter(string ParameterName, string ParameterValue)
        {
            Command.Parameters.AddWithValue(ParameterName, ParameterValue);
        }

        public void AddParameter(string ParameterName, int ParameterValue)
        {
            Command.Parameters.AddWithValue(ParameterName, ParameterValue);
        }

        public void AddParameter(string ParameterName, DateTime ParameterValue)
        {
            Command.Parameters.AddWithValue(ParameterName, ParameterValue);
        }

        public void AddParameter(string ParameterName, double ParameterValue)
        {
            Command.Parameters.AddWithValue(ParameterName, ParameterValue);
        }

        public void AddParameter(string ParameterName, float ParameterValue)
        {
            Command.Parameters.AddWithValue(ParameterName, ParameterValue);
        }

        public void AddParameter(string ParameterName, long ParameterValue)
        {
            Command.Parameters.AddWithValue(ParameterName, ParameterValue);
        }

        public void AddParameter(string ParameterName, bool ParameterValue)
        {
            Command.Parameters.AddWithValue(ParameterName, ParameterValue);
        }
        #endregion

        public static void ExecuteQuery(string Query, SqlConnection connection)
        {
            try
            {

                SqlCommand xCommand = new SqlCommand();
                xCommand.Connection = connection;
                xCommand.CommandText = Query;
                //xCommand.CommandType = CommandType.StoredProcedure;
               

                if (connection.State != ConnectionState.Open)
                    connection.Open();

                xCommand.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

        }


        public static DataTable ReturnDataTable(string Query, string tableName, SqlConnection connection)
        {
            try
            {

                if (connection.State != ConnectionState.Open)
                    connection.Open();

                DataTable ds = new DataTable(tableName);

                SqlDataAdapter objAdapter = new SqlDataAdapter(Query, connection);

                objAdapter.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                //ExceptionMngr.WriteEvent("ReturnDataTable: " + Query + sWhere, ListValues.EventType.Error, ex, null, ListValues.ErrorCategory.ErpConnection);
                return null;
            }
            finally { connection.Close(); }
        }


        public static DataSet ReturnDataSet(string Query, SqlConnection connection)
        {
            try
            {

                if (connection.State != ConnectionState.Open)
                    connection.Open();

                DataSet ds = new DataSet("ReturnDS");
                SqlDataAdapter objAdapter = new SqlDataAdapter(Query, connection);
                objAdapter.SelectCommand.CommandTimeout = 900;
                objAdapter.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally { connection.Close(); }
        }


        public static DataSet ReturnDataSet_WithException(string Query, SqlConnection connection)
        {
            try
            {

                if (connection.State != ConnectionState.Open)
                    connection.Open();

                DataSet ds = new DataSet("ReturnDS");
                SqlDataAdapter objAdapter = new SqlDataAdapter(Query, connection);
                objAdapter.Fill(ds);
                return ds;
            }
            catch 
            {
                throw;
            }
            finally { connection.Close(); }
        }


    }
}
