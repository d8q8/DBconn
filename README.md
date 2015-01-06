DBconn
======

很久前写的一个ADO.NET的封装库...

进行了一点完善更新...

初步更新...

1>使用方法
<pre>
	枚举数据库类型
	public enum MyType
    {
        Access2003, Access2007, Access2013, Mssql, Mysql, Oracle, Sqlite
    }
    使用很简单,如
    MyType.Access2003
</pre>
2>数据库配置
<pre>
	//调用代码配置在web.config里，数据库路径为：Access，Sqlite为相对路径，其他为全路径
    \[appSettings\]
        <add key="access" value="数据库" />
        <add key="sqlite" value="数据库" />
        <add key="sqlserver" value="server=(local);uid=用户名;pwd=密码;database=数据库" />
        <add key="oracle" value="Provider=MSDAORA.1;Password=密码;User ID=用户名;Data Source=数据库" />
        <add key="mysql" value="server=localhost;user id=用户名;password=密码;database=数据库" />
    </appSettings>
</pre>
3>简单调用方式
<pre>
	Conn _db;
	if (_db == null)
    {
        _db = Db.GetConn(MyType.Sqlite, "sqlite");//Sqlite数据库
        //_db = Db.GetConn(MyType.Access2013, "access2013");//Access数据库
    }
    using (_db)
    {
        const string sql = "select * from admin";
        var dt = _db.MyDt(sql,CommandType.Text,1,20,"news");
        Console.Write("用户名:{0},密码:{1}", dt.Rows[0]["username"], dt.Rows[0]["password"]);
    }
</pre>
4>其他调用方式
<pre>
	//其他调用方式也比较简单,如下
	_db.MyDs(sql);//返回DataSet数据集
	_db.MyDt(sql);//返回DataTable数据表
	_db.MyDv(sql);//返回DataView数据视图
	_db.MyRead(sql);//返回DataReader数据视图
	_db.MyExec(sql);//处理增删改操作,返回int类型
	_db.MyTotal(sql);//处理计算查询结果操作,返回object类型,可自行转化成总数
	_db.MyExist(sql);//判断数据是否存在,返回bool类型
	_db.GetCache(键);//获取缓存
	_db.SetCache(键,值);//设置缓存
	_db.MyParam(参数名称,参数赋值);//设置参数,赋值为字符串
	_db.AddParam(参数名称,参数赋值);//同上,只是赋值为对象
</pre>
5>重载调用方式
<pre>
	//跟上面类似调用方式,只是参数多些,如下
	//MyDt(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param)
	_db.MyDt(sql, 命令类型, 开始页数索引, 每页多少个, 数据集别名, 变参);
	//其他的方法基本都有重载,请自己查看源码吧.
</pre>

祝各位好运,使用简单方便,学习NET立刻简单了,不是一点点,加油,童鞋们,都能成为NET开发者.