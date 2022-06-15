using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1_0613TEST
{ 
    public class SQL 
    {
        //撈出(個人)臉變的體溫資料
        public static string sqlsrech = "SELECT a.t_PK , b.t_Number ,a.t_Time,a.t_Value FROM OGBodyTemperature as a INNER JOIN OGEmp as b ON a.t_EmpPK = b.t_PK where b.t_Number=@Number; ";
        //把差集的資料寫入對方TABLE
        public static string strNeedToAdd = "Insert Into Temperature values (@NewID,@NewAlertDate,@NewEndDate,@NewDay,@NewTime01,@NewTemp01,@NewTime02,@NewTemp02);";
        //找目前臉變有體溫的員工資料
        public static string sql = "SELECT a.t_PK , b.t_Number ,a.t_Time,a.t_Value FROM OGBodyTemperature as a INNER JOIN OGEmp as b ON a.t_EmpPK = b.t_PK; ";
        //找目前對方有上傳的員工資料
        public static string sqlCus = "select*from Temperature ;";
        //撈出對方單人員工的第二次體溫的資料(用來判斷是否有寫入)
        public static string NewNeedToAdd = "select temp2 from Temperature where id = @NewID;";
        //單純寫入第二次的時間體溫
        public static string MustAdd = "update Temperature set time2=@NewTime02,temp2=@NewTemp02 where id=@NewID;";
    }
}