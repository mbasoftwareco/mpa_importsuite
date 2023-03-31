using Microsoft.VisualBasic;
using PANGEA.IMPORTSUITE.DataModel;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PANGEA.IMPORTSUITE.Runtime
{
    class Program
    {
        static void Main(string[] args)
        {
            InvoiceDelivery_Helper devHelper = new InvoiceDelivery_Helper();


            try
            {
                Console.WriteLine("START-Print_Pending_Invoices");
                devHelper.Print_Pending_Invoices();
                Console.WriteLine("END-Print_Pending_Invoices");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\tError: " + ex.Message);
            }


            T_SYNC_Execution_Control newControl = null;

            IMPORTSUITE_DAODataContext _db = new IMPORTSUITE_DAODataContext();

            string processCode = "DELIVERY.INV";


            try
            {
                Console.WriteLine("START-SendPendingMessages");

                //VALIDATE IF PROCESS IS RUNNING
                if (_db.T_SYNC_Execution_Controls.Any(f => f.ProcessCode == processCode && f.EndDatetime == null))
                {
                    Console.WriteLine("\t " + processCode + " Exits");
                    return;
                }

                string curGuid = Guid.NewGuid().ToString().Remove(8);

                newControl = new T_SYNC_Execution_Control
                {
                    ProcessCode = processCode,
                    StartDatetime = DateTime.Now,
                    SessionID = curGuid
                };

                _db.T_SYNC_Execution_Controls.InsertOnSubmit(newControl);

                _db.SubmitChanges();

                Console.WriteLine("\t " + processCode + " Created");

                MailSender.SendPendingMessages();

                //END RUNNING
                try
                {
                    if (newControl != null)
                    {
                        newControl.EndDatetime = DateTime.Now;
                        newControl.Duration = 0;
                        _db.SubmitChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\t " + processCode + " Error-END#2 " + ex.Message);
                }

                Console.WriteLine("END-SendPendingMessages");


            }
            catch (Exception ex)
            {
                if (newControl != null)
                {
                    try
                    {
                        newControl.EndDatetime = DateTime.Now;
                        newControl.Duration = 0;
                        _db.SubmitChanges();
                    }
                    catch (Exception exz)
                    {
                        Console.WriteLine("\t " + processCode + " Error-END#1 " + exz.Message);
                    }
                }

                Console.WriteLine("\tError: " + ex.Message);
            }


            //Check Frequency Time

            DateTime lastRunSync = DateTime.Parse(_db.S_PARAMETERs.Where(f => f.CODE == "RUNSYNC.LAST").First().VALUE);

            Console.WriteLine(lastRunSync.ToString());

            //SYNC: Process is executed each X minutes
            //JM:L added on: OCT 01 2015
            long freqTime = long.Parse(_db.S_PARAMETERs.Where(f => f.CODE == "FREQ.SYNC").First().VALUE);


            if (Math.Abs(DateAndTime.DateDiff(DateInterval.Minute, lastRunSync, DateTime.Now)) < freqTime)
                return;


            try { devHelper.UpdateLastRun("RUNSYNC.LAST"); }
            catch { }

        }
    }
}

 