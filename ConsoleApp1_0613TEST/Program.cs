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
        static DateTime dateTime = DateTime.Now; //系統時間
        static String timeNow = dateTime.ToString();
        static SqlConnection conn;
        static SqlConnection conn02;

        public class Global  
        {
            public static List<string> Number = new List<string>();
            public static List<DateTime> Time = new List<DateTime>();
            public static List<Decimal> Value = new List<Decimal>();
            public static DateTime Time02;
            public static Decimal Value02;
            public static String Temp02;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("系統時間"+ timeNow);
            Console.WriteLine("Getting Connection ...");

            DBConnect();
            //中午之前檢查早上
            if (dateTime < DateTime.Parse(DateTime.Now.ToShortDateString() + " 12:00:00"))
            {
                _logger.Info("時間為中午十二點之前，檢查第一次體溫");
                Console.WriteLine("時間為上午，執行檢查一次");
                serchID.Clear(); serchNo.Clear(); serchSelf.Clear();Global.Time.Clear();Global.Value.Clear();Global.Number.Clear();//全清空

                FindDiffert(); //找目前臉變與對方資料的差集
                WriteToSameFirstTime(); //寫入第一次差集的資料
                conn.Close(); conn02.Close();  //關閉DB連線
            }
            else
            {
                _logger.Info("時間為中午十二點之後，檢查兩次體溫");
                Console.WriteLine("時間超過中午十二點，執行檢查兩次");
                serchID.Clear(); serchNo.Clear();serchSelf.Clear(); Global.Time.Clear(); Global.Value.Clear(); Global.Number.Clear();//全清空

                FindDiffert(); //找目前的差集
                WriteToSameSecondTime(); //寫入差集資料
                CheckFirstTime(); //檢查無在差集中但卻沒有第二次體溫並寫入
                conn.Close(); conn02.Close(); //DB連線全部關掉
            }
            Console.Read();
        }
        static void DBConnect() 
        {//建立兩個DB連線
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder();
            scsb.DataSource = @".";
            scsb.InitialCatalog = "OGSystem";
            scsb.IntegratedSecurity = true;
            var connString = scsb.ToString();
            conn = new SqlConnection(connString);

            SqlConnectionStringBuilder scsb02 = new SqlConnectionStringBuilder();
            scsb.DataSource = @".";
            scsb.InitialCatalog = "LabTest";
            scsb.IntegratedSecurity = true;
            var connString02 = scsb.ToString();
            conn02 = new SqlConnection(connString02);
            try
            {
                Console.WriteLine("Openning Connection ...");
                conn.Open();
                conn02.Open();
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
            try
            {
                string str = "SELECT a.t_PK , b.t_Number ,a.t_Time,a.t_Value FROM OGBodyTemperature as a INNER JOIN OGEmp as b ON a.t_EmpPK = b.t_PK; ";
                string str02 = "select*from Temperature ;";
                SqlCommand cmd = new SqlCommand(str, conn);
                SqlCommand cmd02 = new SqlCommand(str02, conn02);
                SqlDataReader reader = cmd.ExecuteReader();
                SqlDataReader reader02 = cmd02.ExecuteReader();
                int i = 0;
                while (reader02.Read())   //把東西讀出來
                {
                    serchSelf.Add(reader02["id"].ToString());
                    i++;
                    Console.WriteLine("First讀取對方有上傳的員工資料中_" + reader02["id"]);
                }
                _logger.Info("First讀取對方有上傳體溫有的員工資料完畢");
                if (i <= 0)
                {
                    _logger.Info("First未讀取到對方任何有上傳的員工資料");
                    Console.WriteLine("First未讀取到對方的任何資料");
                }
                int y = 0;
                reader02.Close();
                while (reader.Read())
                {
                    serchID.Add(reader["t_PK"].ToString());
                    serchNo.Add(reader["t_Number"].ToString());
                    y++;
                    Console.WriteLine("First讀取目前體溫有的員工資料中_" + reader["t_Number"]);
                }
                _logger.Info("First讀取目前體溫有的員工資料完畢");
                if (y <= 0)
                {
                    Console.WriteLine("First未讀取到任何資料");
                    _logger.Info("First未讀取到任何資料");
                }
                reader.Close();
            }catch (Exception) {
                _logger.Error("呼叫FindDiffert方法執行錯誤");
            }
        }
        static void WriteToSameFirstTime() 
        {//差集塞到LIST併寫入
            List<string> NeetToAddList = new List<string>();
            NeetToAddList = serchNo.Except(serchSelf).ToList();
            foreach (string item in NeetToAddList)
            {
                int d = 0;
                string serchName = item;
                while (d <= NeetToAddList.Count - 1)
                {
                    string strSerch = "SELECT a.t_PK , b.t_Number ,a.t_Time,a.t_Value FROM OGBodyTemperature as a INNER JOIN OGEmp as b ON a.t_EmpPK = b.t_PK where b.t_Number=@Number; ";
                    SqlCommand cmsSerch = new SqlCommand(strSerch, conn);
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
                    string strNeedToAdd = "Insert Into Temperature values (@NewID,@NewAlertDate,@NewEndDate,@NewDay,@NewTime01,@NewTemp01,@NewTime02,@NewTemp02);";
                    SqlCommand cmd03 = new SqlCommand(strNeedToAdd, conn02);
                    cmd03.Parameters.AddWithValue("@NewID", Global.Number[d]);
                    cmd03.Parameters.AddWithValue("@NewAlertDate", dateTime);
                    cmd03.Parameters.AddWithValue("@NewEndDate", dateTime);
                    cmd03.Parameters.AddWithValue("@NewDay", 1);
                    cmd03.Parameters.AddWithValue("@NewTime01", Global.Time[d]);
                    cmd03.Parameters.AddWithValue("@NewTemp01", Global.Value[d]);
                    cmd03.Parameters.AddWithValue("@NewTime02", null);
                    cmd03.Parameters.AddWithValue("@NewTemp02", null);
                    int rows = cmd03.ExecuteNonQuery();
                    Console.WriteLine("資料新增成功!" + rows + "筆資料");
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
        static void WriteToSameSecondTime()
        {
            //差集塞到LIST併寫入
            List<string> NeetToAddList = new List<string>();
            NeetToAddList = serchNo.Except(serchSelf).ToList();
            foreach (string item in NeetToAddList)
            {
                int d = 0;
                string serchName = item;
                while (d <= NeetToAddList.Count - 1)
                {
                    string strSerch = "SELECT a.t_PK , b.t_Number ,a.t_Time,a.t_Value FROM OGBodyTemperature as a INNER JOIN OGEmp as b ON a.t_EmpPK = b.t_PK where b.t_Number=@Number; ";
                    SqlCommand cmsSerch = new SqlCommand(strSerch, conn);
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
                    string strNeedToAdd = "Insert Into Temperature values (@NewID,@NewAlertDate,@NewEndDate,@NewDay,@NewTime01,@NewTemp01,@NewTime02,@NewTemp02);";
                    SqlCommand cmd03 = new SqlCommand(strNeedToAdd, conn02);
                    cmd03.Parameters.AddWithValue("@NewID", Global.Number[d]);
                    cmd03.Parameters.AddWithValue("@NewAlertDate", dateTime);
                    cmd03.Parameters.AddWithValue("@NewEndDate", dateTime);
                    cmd03.Parameters.AddWithValue("@NewDay", 1);
                    cmd03.Parameters.AddWithValue("@NewTime01", Global.Time[d]);
                    cmd03.Parameters.AddWithValue("@NewTemp01", Global.Value[d]);
                    cmd03.Parameters.AddWithValue("@NewTime02", Global.Time[d]);
                    cmd03.Parameters.AddWithValue("@NewTemp02", Global.Value[d]);
                    int rows = cmd03.ExecuteNonQuery();
                    Console.WriteLine("資料新增成功!" + rows + "筆資料");
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
                string NewNeedToAdd = "select temp2 from Temperature where id = @NewID;";
                SqlCommand cmd04 = new SqlCommand(NewNeedToAdd, conn02);
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
                    string sqlNeed = "SELECT a.t_Time,a.t_Value FROM OGBodyTemperature as a INNER JOIN OGEmp as b ON a.t_EmpPK = b.t_PK where b.t_Number=@NewID02;";
                    SqlCommand cmd05 = new SqlCommand(sqlNeed, conn);
                    cmd05.Parameters.AddWithValue("@NewID02", ID);
                    SqlDataReader reader05 = cmd05.ExecuteReader();
                    while (reader05.Read())
                    {
                        Global.Time02 = Convert.ToDateTime(reader05["t_Time"]);
                        Global.Value02 = Convert.ToDecimal(reader05["t_Value"]);
                    }
                    reader05.Close();
                    string MustAdd = "update Temperature set time2=@NewTime02,temp2=@NewTemp02 where id=@NewID;";
                    SqlCommand cmd06 = new SqlCommand(MustAdd, conn02);
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
    }
  
  }

