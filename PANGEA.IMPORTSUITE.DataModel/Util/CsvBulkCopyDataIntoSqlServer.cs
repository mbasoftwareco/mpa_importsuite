using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualBasic.FileIO;

namespace PANGEA.IMPORTSUITE.DataModel.Util
{
    public class BULK_COPY_PLAINTEXT_FILE
    {

        protected const string _truncateLiveTableCommandText = @"DELETE FROM T_TMP_UPLOAD_RECORDS WHERE GUID = ";

        protected const int _batchSize = 1000000;

        private ImportSuiteConnection curConnection;

        public BULK_COPY_PLAINTEXT_FILE(ImportSuiteConnection _curConnection)
        {
            this.curConnection = _curConnection;
        }

        public void LOAD_PLAINTEXT_INTO_SQL(int ROWID_TEMPLATE, string fileName, string GUID, string separator)
        {

            _WEB _web = new _WEB(curConnection);

            T_TEMPLATE T_TEMPLATE = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == ROWID_TEMPLATE).First();

            var createdCount = 0;

            using (var textFieldParser = new TextFieldParser(fileName))
            {

                textFieldParser.TextFieldType = FieldType.Delimited;
                textFieldParser.Delimiters = new[] { separator };
                textFieldParser.HasFieldsEnclosedInQuotes = true;


                var dataTable = new DataTable("T_TMP_UPLOAD_RECORDS");

                using (var sqlConnection = _web._sqlCnn)
                {
                    sqlConnection.Open();

                    // Truncate the live table
                    using (var sqlCommand = new SqlCommand(_truncateLiveTableCommandText + " '" + GUID + "'", sqlConnection))
                        sqlCommand.ExecuteNonQuery();


                    // Create the bulk copy object
                    var sqlBulkCopy = new SqlBulkCopy(sqlConnection)
                    {
                        DestinationTableName = "T_TMP_UPLOAD_RECORDS"
                    };



                    #region SETUP TEMPLATE COLUMN MAPPING 

                    // Setup the column mappings, anything ommitted is skipped
                    List<T_TEMPLATE_FIELD> TEMPLATE_FIELD = _web._dbx.T_TEMPLATE_FIELDs.Where(f => f.ROWID_TEMPLATE == ROWID_TEMPLATE).OrderBy(f => f.SEQUENCE).ToList();

                    foreach (var field in TEMPLATE_FIELD.ToList())
                    {

                        if (T_TEMPLATE.HEADER_FIRSTROW == true)
                        {
                            dataTable.Columns.Add(field.FIELD_SOURCE_NAME);
                            sqlBulkCopy.ColumnMappings.Add(field.FIELD_SOURCE_NAME, "C" + field.SEQUENCE);
                        }
                        else
                        {
                            dataTable.Columns.Add("C" + field.SEQUENCE);
                            sqlBulkCopy.ColumnMappings.Add((field.SEQUENCE - 1), (field.SEQUENCE - 1));
                        }
                    }

                    sqlBulkCopy.ColumnMappings.Add("ROWID_TEMPLATE", "ROWID_TEMPLATE");
                    dataTable.Columns.Add("ROWID_TEMPLATE");

                    sqlBulkCopy.ColumnMappings.Add("GUID", "GUID");
                    dataTable.Columns.Add("GUID");

                    sqlBulkCopy.ColumnMappings.Add("FILENAME", "FILENAME");
                    dataTable.Columns.Add("FILENAME");

                    #endregion


                    // Loop through the CSV and load each set of 100,000 records into a DataTable
                    // Then send it to the LiveTable
                    while (!textFieldParser.EndOfData)
                    {
                        createdCount++;

                        List<string> rows = rows = textFieldParser.ReadFields().ToList();

                        //Validate file header to skipthe first record.
                        if (T_TEMPLATE.HEADER_FIRSTROW == true && createdCount == 1)
                            continue;

                        rows.Add(ROWID_TEMPLATE.ToString());

                        rows.Add(GUID);

                        rows.Add(fileName);

                        string[] row = rows.ToArray();

                        dataTable.Rows.Add(row);

                        if (createdCount % _batchSize == 0)
                        {
                            InsertDataTable(sqlBulkCopy, sqlConnection, dataTable);

                            break;
                        }

                    }

                    // Don't forget to send the last batch under 100,000
                    InsertDataTable(sqlBulkCopy, sqlConnection, dataTable);

                    sqlConnection.Close();

                }

            }
        }


        public void LOAD_XLSX_INTO_SQL(int ROWID_TEMPLATE, string fileName, string GUID)
        {

            _WEB _web = new _WEB(curConnection);

            var createdCount = 0;

            using (var textFieldParser = new TextFieldParser(fileName))
            {

                T_TEMPLATE T_TEMPLATE = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == ROWID_TEMPLATE).First();

                textFieldParser.TextFieldType = FieldType.Delimited;
                //textFieldParser.Delimiters = new[] { separator }; // ","
                textFieldParser.HasFieldsEnclosedInQuotes = true;


                var dataTable = new DataTable("T_TMP_UPLOAD_RECORDS");

                using (var sqlConnection = _web._sqlCnn)
                {
                    sqlConnection.Open();

                    // Truncate the live table
                    using (var sqlCommand = new SqlCommand(_truncateLiveTableCommandText + " '" + GUID + "'", sqlConnection))
                        sqlCommand.ExecuteNonQuery();


                    // Create the bulk copy object
                    var sqlBulkCopy = new SqlBulkCopy(sqlConnection)
                    {
                        DestinationTableName = "T_TMP_UPLOAD_RECORDS"
                    };

                    #region SETUP TEMPLATE COLUMN MAPPING 

                    // Setup the column mappings, anything ommitted is skipped
                    List<T_TEMPLATE_FIELD> TEMPLATE_FIELD = _web._dbx.T_TEMPLATE_FIELDs.Where(f => f.ROWID_TEMPLATE == ROWID_TEMPLATE).OrderBy(f => f.SEQUENCE).ToList();

                    foreach (var field in TEMPLATE_FIELD.ToList())
                    {

                        if (T_TEMPLATE.HEADER_FIRSTROW == true)
                        {
                            dataTable.Columns.Add(field.FIELD_SOURCE_NAME);
                            sqlBulkCopy.ColumnMappings.Add(field.FIELD_SOURCE_NAME, "C" + field.SEQUENCE);
                        }
                        else
                        {
                            dataTable.Columns.Add("C" + field.SEQUENCE);
                            sqlBulkCopy.ColumnMappings.Add((field.SEQUENCE - 1), (field.SEQUENCE - 1));
                        }
                    }

                    sqlBulkCopy.ColumnMappings.Add("ROWID_TEMPLATE", "ROWID_TEMPLATE");
                    dataTable.Columns.Add("ROWID_TEMPLATE");

                    sqlBulkCopy.ColumnMappings.Add("GUID", "GUID");
                    dataTable.Columns.Add("GUID");

                    sqlBulkCopy.ColumnMappings.Add("FILENAME", "FILENAME");
                    dataTable.Columns.Add("FILENAME");

                    #endregion

                    /*
                    // Loop through the CSV and load each set of 100,000 records into a DataTable
                    // Then send it to the LiveTable
                    while (!textFieldParser.EndOfData)
                    {

                        createdCount++;

                        List<string> rows = textFieldParser.ReadFields().ToList();

                        rows.Add(ROWID_TEMPLATE.ToString());

                        rows.Add(GUID);

                        rows.Add(fileName);

                        string[] row = rows.ToArray();

                        dataTable.Rows.Add(row);

                        if (createdCount % _batchSize == 0)
                        {
                            InsertDataTable(sqlBulkCopy, sqlConnection, dataTable);

                            break;
                        }

                    }
                    */

                    // Don't forget to send the last batch under 100,000
                    InsertDataTable(sqlBulkCopy, sqlConnection, dataTable);

                    sqlConnection.Close();

                }

            }
        }


        public void LOAD_PLAINTEXT_INTO_AX_TABLE(T_TEMPLATE curTemplate, string fileName, string GUID, string separator)
        {

            _WEB _web = new _WEB(curConnection);

            T_TEMPLATE T_TEMPLATE = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == curTemplate.ROWID).First();

            var createdCount = 0;

            using (var textFieldParser = new TextFieldParser(fileName))
            {

                textFieldParser.TextFieldType = FieldType.Delimited;
                textFieldParser.Delimiters = new[] { separator };
                textFieldParser.HasFieldsEnclosedInQuotes = true;


                var dataTable = new DataTable("TMP_DATA");

                using (var sqlConnection = _web._sqlCnn)
                {
                    sqlConnection.Open();

                    // Create the bulk copy object
                    var sqlBulkCopy = new SqlBulkCopy(sqlConnection)
                    {
                        DestinationTableName = curTemplate.AX_TEMP_TABLE
                    };

                    //Select 
                    DataTable axTable = SQLBase.ReturnDataSet("SELECT TOP 0 * FROM " + curTemplate.AX_TEMP_TABLE, sqlConnection).Tables[0];

                    #region SETUP TEMPLATE COLUMN MAPPING 

                    //Column List
                    List<string> columnList = textFieldParser.ReadFields().ToList();


                    foreach (var field in columnList)
                    { 
                        if (!axTable.Columns.Contains(field))
                        {
                            try
                            {
                                string newfield  = _web._dbx.T_AX_COLUMN_EQUIVALENCEs.Where(f => f.FILECOLUMN == field).First().AXCOLUMN;

                                dataTable.Columns.Add(field.ToUpper());

                                sqlBulkCopy.ColumnMappings.Add(field.ToUpper(), newfield.ToUpper());

                            }
                            catch { }
                        }
                        else
                        {
                            T_AX_COLUMN_EQUIVALENCE eqColumn = _web._dbx.T_AX_COLUMN_EQUIVALENCEs
                                .Where(f => f.FILECOLUMN == field && f.TABLENAME == curTemplate.AX_TEMP_TABLE)
                                .FirstOrDefault();

                            if (eqColumn != null)
                            {

                                dataTable.Columns.Add(field.ToUpper());
                                sqlBulkCopy.ColumnMappings.Add(field.ToUpper(), eqColumn.AXCOLUMN.ToUpper());

                            }
                            else
                            {
                                dataTable.Columns.Add(field.ToUpper());
                                sqlBulkCopy.ColumnMappings.Add(field.ToUpper(), field.ToUpper());
                            }

                        }


                    }

                    sqlBulkCopy.ColumnMappings.Add("COUNTRY", "COUNTRY");
                    dataTable.Columns.Add("COUNTRY");

                    sqlBulkCopy.ColumnMappings.Add("GUID", "GUID");
                    dataTable.Columns.Add("GUID");

                    sqlBulkCopy.ColumnMappings.Add("MODULE", "MODULE");
                    dataTable.Columns.Add("MODULE");

                    sqlBulkCopy.ColumnMappings.Add("JOURNAL", "JOURNAL");
                    dataTable.Columns.Add("JOURNAL");



                    #endregion


                    // Loop through the CSV and load each set of 100,000 records into a DataTable
                    // Then send it to the LiveTable
                    while (!textFieldParser.EndOfData)
                    {
                        createdCount++;

                        List<string> rows = textFieldParser.ReadFields().ToList();

                        //Validate file header to skipthe first record.
                        if (T_TEMPLATE.HEADER_FIRSTROW == true && createdCount == 1)
                            continue;

                        rows.Add("COL");

                        rows.Add(GUID);

                        rows.Add(curTemplate.NAME);

                        rows.Add("APUL");

                        string[] row = rows.ToArray();

                        dataTable.Rows.Add(row);

                        if (createdCount % _batchSize == 0)
                        {
                            InsertDataTable(sqlBulkCopy, sqlConnection, dataTable);

                            break;
                        }

                    }

                    // Don't forget to send the last batch under 100,000
                    InsertDataTable(sqlBulkCopy, sqlConnection, dataTable);

                    sqlConnection.Close();

                }

            }
        }


        protected void InsertDataTable(SqlBulkCopy sqlBulkCopy, SqlConnection sqlConnection, DataTable dataTable)
        {
            if (sqlConnection.State != ConnectionState.Open)
                sqlConnection.Open();

            sqlBulkCopy.WriteToServer(dataTable);

            dataTable.Rows.Clear();
        }


    }
}