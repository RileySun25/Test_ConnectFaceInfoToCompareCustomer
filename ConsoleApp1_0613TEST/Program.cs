using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using ConsoleApp1_0613TEST;

namespace ConsoleApp1_0613TEST
{
    class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        static List<string> serchSelf = new List<string>(); //他們自己的資料
        static List<string> serchID = new List<string>();  //全部讀取出的資料
        static List<string> serchNo = new List<string>(); //員工工號
        static DateTime dateTime = DateTime.Now; static String timeNow = dateTime.ToString(); //系統時間
        static SqlConnection DBOur; static SqlConnection DBcus;
        static void Main(string[] args)
        {
            DBConnect();
            if (dateTime < DateTime.Parse(DateTime.Now.ToShortDateString() + " 12:00:00"))
            {
                _logger.Info("時間為中午十二點之前，檢查第一次體溫");
                serchID.Clear(); serchNo.Clear(); serchSelf.Clear();
                Global.Time.Clear();Global.Value.Clear();Global.Number.Clear();//全清空
                FindDiffert(); //找目前臉變與對方資料的差集
                WriteToSame(); //寫入第一次差集的資料
                DBOur.Close(); DBcus.Close();  //關閉DB連線
            }
            else
            {
                _logger.Info("時間為中午十二點之後，檢查兩次體溫");
                serchID.Clear(); serchNo.Clear();serchSelf.Clear(); 
                Global.Time.Clear(); Global.Value.Clear(); Global.Number.Clear();//全清空
                FindDiffert(); //找目前的差集
                WriteToSame(); //寫入差集資料
                CheckFirstTime(); //檢查無在差集中但卻沒有第二次體溫並寫入
                DBOur.Close(); DBcus.Close(); //DB連線全部關掉
            }
        }
        static void DBConnect() 
        {//建立兩個DB連線
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder();
            scsb.DataSource = @"."; scsb.InitialCatalog = "OGSystem";scsb.IntegratedSecurity = true;
            var DBOurString = scsb.ToString();
            DBOur = new SqlConnection(DBOurString);

            SqlConnectionStringBuilder scsb02 = new SqlConnectionStringBuilder();
            scsb.DataSource = @".";scsb.InitialCatalog = "LabTest";scsb.IntegratedSecurity = true;
            var DBOurString02 = scsb.ToString();
            DBcus = new SqlConnection(DBOurString02);
            try
            {
                Console.WriteLine("Openning Connection ...");
                DBOur.Open();
                DBcus.Open();
                Console.WriteLine("門將DB連線成功!TEST連線成功!");
                _logger.Info("門將DB連線成功!TEST連線成功!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                _logger.Error("Connection failed!");
            }
        }
        static void FindDiffert() 
        {
                ExcuteSqlCmdOurOnce(SQL.sql);
                ExcuteSqlCmdCusOnce(SQL.sqlCus);
        }
        static void WriteToSame()
        {
            //差集塞到LIST併寫入
            List<string> NeetToAddList = new List<string>();
            NeetToAddList = serchNo.Except(serchSelf).ToList();
            NeetToAddList.Clear();
            foreach (string item in NeetToAddList)
            {
                int d = 0;
                string serchName = item;
                while (d <= NeetToAddList.Count - 1)
                {
                    SqlCommand cmsSerch = new SqlCommand(SQL.sqlsrech, DBOur);
                    cmsSerch.Parameters.AddWithValue("@Number", serchName);
                    SqlDataReader reader03 = cmsSerch.ExecuteReader();
                    int e = 0;
                    while (reader03.Read())
                    {
                        Global.Number.Add(reader03["t_Number"].ToString());
                        Global.Time.Add((DateTime)reader03["t_Time"]);
                        Global.Value.Add((Decimal)reader03["t_Value"]);
                        e++;
                    }
                    if (e <= 0)
                    {
                        Console.WriteLine("沒有搜到關鍵字資料");
                    }
                    if (dateTime < DateTime.Parse(DateTime.Now.ToShortDateString() + " 12:00:00"))
                    {
                        SqlCommand cmd03 = new SqlCommand(SQL.strNeedToAdd, DBcus);
                        cmd03.Parameters.AddWithValue("@NewID", Global.Number[d]);
                        cmd03.Parameters.AddWithValue("@NewAlertDate", dateTime);
                        cmd03.Parameters.AddWithValue("@NewEndDate", dateTime);
                        cmd03.Parameters.AddWithValue("@NewDay", 1);
                        cmd03.Parameters.AddWithValue("@NewTime01", Global.Time[d]);
                        cmd03.Parameters.AddWithValue("@NewTemp01", Global.Value[d]);
                        cmd03.Parameters.AddWithValue("@NewTime02", null);
                        cmd03.Parameters.AddWithValue("@NewTemp02", null);
                    }
                    else {
                        SqlCommand cmd03 = new SqlCommand(SQL.strNeedToAdd, DBcus);
                        cmd03.Parameters.AddWithValue("@NewID", Global.Number[d]);
                        cmd03.Parameters.AddWithValue("@NewAlertDate", dateTime);
                        cmd03.Parameters.AddWithValue("@NewEndDate", dateTime);
                        cmd03.Parameters.AddWithValue("@NewDay", 1);
                        cmd03.Parameters.AddWithValue("@NewTime01", Global.Time[d]);
                        cmd03.Parameters.AddWithValue("@NewTemp01", Global.Value[d]);
                        cmd03.Parameters.AddWithValue("@NewTime02", Global.Time[d]);
                        cmd03.Parameters.AddWithValue("@NewTemp02", Global.Value[d]);
                    }
                    serchName = "";
                    d++;
                    reader03.Close();
                }
                if (d <= 0)
                {
                    _logger.Info("無找到差集，有體溫員工資料與對方有上傳的員工資料相符");
                }
            }
        }
        static void CheckFirstTime() 
        {
            int counter = 0;
            foreach (string ID in serchSelf)
            {
                SqlCommand cmd04 = new SqlCommand(SQL.NewNeedToAdd, DBcus);
                cmd04.Parameters.AddWithValue("@NewID", ID);
                SqlDataReader reader04 = cmd04.ExecuteReader();
                int t = 0;
                while (reader04.Read())
                {
                    Global.Temp02 = reader04["temp2"].ToString();
                    t++;
                }
                reader04.Close();

                if (Global.Temp02 == "NULL" || Global.Temp02 == "")
                {
                    SqlCommand cmd05 = new SqlCommand(SQL.sqlsrech, DBOur);
                    cmd05.Parameters.AddWithValue("@Number", ID);
                    SqlDataReader reader05 = cmd05.ExecuteReader();
                    while (reader05.Read())
                     {
                        Global.Time02 = Convert.ToDateTime(reader05["t_Time"]);
                        Global.Value02 = Convert.ToDecimal(reader05["t_Value"]);
                     }
                        reader05.Close();
                        SqlCommand cmd06 = new SqlCommand(SQL.MustAdd, DBcus);
                        cmd06.Parameters.AddWithValue("@NewTime02", Global.Time02);
                        cmd06.Parameters.AddWithValue("@NewTemp02", Global.Value02);
                        cmd06.Parameters.AddWithValue("@NewID", ID);
                }
                else
                {
                    _logger.Info("目前對方的資料表皆有temp02的體溫!");
                }
                counter++;
            }
            if (counter <= 0)
            {
                Console.WriteLine("serchSelf沒有資料");
            }
        }
        static void ExcuteSqlCmdOurOnce(string sql) 
        {
            try
            {
                SqlCommand cmdOur = new SqlCommand(sql, DBOur);
                SqlDataReader reader = cmdOur.ExecuteReader();
                int count = 0;
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        serchID.Add(reader["t_PK"].ToString());
                        serchNo.Add(reader["t_Number"].ToString());
                        count++;
                        Console.WriteLine("First讀取目前體溫有的員工資料中_" + reader["t_Number"]);
                    }
                    if (count <= 0)
                    {
                        Console.WriteLine("First未讀取到任何資料");
                        _logger.Info("First未讀取到任何資料");
                    }
                    _logger.Info("First讀取目前體溫有的員工資料完畢");
                }
                reader.Close();
                cmdOur.Dispose();
            }
            catch (Exception ex) 
            {
                _logger.Error(ex);
            }
        }
        static void ExcuteSqlCmdCusOnce(string sqlCus)
        {
            try
            {
                SqlCommand cmdCus = new SqlCommand(sqlCus, DBcus);
                SqlDataReader readerCus = cmdCus.ExecuteReader();
                int i = 0;
                if (readerCus.HasRows)
                {
                    while (readerCus.Read())
                    {
                        serchSelf.Add(readerCus["id"].ToString());
                        i++;
                        Console.WriteLine("First讀取對方有上傳的員工資料中_" + readerCus["id"]);
                    }
                    if (i <= 0)
                    { _logger.Info("First未讀取到對方任何有上傳的員工資料"); }
                    _logger.Info("First讀取對方有上傳體溫有的員工資料完畢");
                }
                readerCus.Close();cmdCus.Dispose();
            }
            catch (Exception ex) { _logger.Error(ex);}
        }
    }
  
  }

