using System;
using DBconn;

namespace DBConnTest
{

    class Program
    {
        static Conn _db;
        static void Main(string[] args)
        {
            if (_db == null)
            {
                _db = Db.GetConn(MyType.Sqlite, "sqlite");
                //_db = Db.GetConn(MyType.Access2013, "access2013");
            }
            using (_db)
            {
                const string sql = "select * from admin";
                var dt = _db.MyDt(sql);
                Console.Write("用户名:{0},密码:{1}", dt.Rows[0]["username"], dt.Rows[0]["password"]);
            }

            //暂停
            Console.ReadLine();
        }
    }
}
