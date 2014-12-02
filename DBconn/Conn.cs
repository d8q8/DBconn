using System;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Web;
using System.Windows.Forms;

namespace DBconn
{

    #region 不同数据库接口类
    /// <summary>
    /// 不同数据库接口封装
    /// </summary>
    interface ISDb
    {
        void Close();
        void Dispose();
        IDataReader GetDataReader(string sql, CommandType ctype);
        IDataReader GetDataReader(string sql, CommandType ctype, params IDataParameter[] param);
        DataSet GetDataSet(string sql, CommandType ctype, string dataname, params IDataParameter[] param);
        DataSet GetDataSet(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param);
        DataSet GetDataSet(string sql, CommandType ctype, int startindex, int pagesize, string dataname);
        DataSet GetDataSet(string sql, CommandType ctype, string dataname);
        int GetExecuteNonQuery(string sql, CommandType ctype, params IDataParameter[] param);
        int GetExecuteNonQuery(string sql, CommandType ctype);
        object GetExecuteScalar(string sql, CommandType ctype, params IDataParameter[] param);
        object GetExecuteScalar(string sql, CommandType ctype);
        void Open();
        void CacheParameters(string cacheKey, params IDataParameter[] commandParameters);
        IDataParameter[] GetCachedParameters(string cacheKey);
        IDataParameter MyParams(string name, object value);
    }
    #endregion

    #region 枚举数据库类型
    /// <summary>
    /// 获取当前数据库
    /// </summary>
    public enum MyType
    {
        Access2003, Access2007, Access2013, Mssql, Mysql, Oracle, Sqlite
    }
    #endregion

    #region 通用数据库操作
    /// <summary>
    /// 通用数据库操作
    /// </summary>
    public sealed class Conn : IDisposable
    {

        #region 数据库配置
        //调用代码配置在web.config里，数据库路径为：Access，Sqlite为相对路径，其他为全路径
        //<appSettings>
        //    <add key="access" value="数据库"/>
        //    <add key="sqlite" value="数据库"/>
        //    <add key="sqlserver" value="server=(local);uid=用户名;pwd=密码;database=数据库"/>
        //    <add key="oracle" value="Provider=MSDAORA.1;Password=密码;User ID=用户名;Data Source=数据库"/>
        //    <add key="mysql" value="server=localhost;user id=用户名;password=密码;database=数据库"/>
        //</appSettings>
        private readonly string _dal;
        private readonly MyType _mytype;

        public Conn(MyType mt = MyType.Access2003, string connstr = "access")
        {
            _mytype = mt;
            _dal = connstr;
            var connStr = ConfigurationManager.AppSettings[connstr].ToString(CultureInfo.InvariantCulture);
            //本地连接串处理
            string filepath;
            if (Application.StartupPath == Environment.CurrentDirectory)
            {
                filepath = Application.StartupPath + "\\" + connStr;
            }
            else
            {
                filepath = HttpContext.Current.Server.MapPath(connStr);
            }

            switch (mt)
            {
                case MyType.Access2003:
                    _dal = string.Format(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};", filepath);
                    break;
                case MyType.Access2007:
                    _dal = string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Persist Security Info=False", filepath);
                    break;
                case MyType.Access2013:
                    _dal = string.Format(@"Provider=Microsoft.ACE.OLEDB.15.0;Data Source={0};Persist Security Info=False", filepath);
                    break;
                case MyType.Sqlite:
                    _dal = string.Format(@"Data Source={0};Pooling=true;FailIfMissing=false;", filepath);
                    break;
                default:
                    _dal = connStr;
                    break;
            }
        }
        #endregion

        #region 判断当前使用哪种数据库
        /// <summary>
        /// 判断当前使用何种数据库
        /// </summary>
        /// <returns></returns>
        private ISDb IsSql()
        {
            switch (_mytype)
            {
                case MyType.Access2003:
                case MyType.Access2007:
                case MyType.Access2013:
                    return new Access(_dal);
                case MyType.Sqlite:
                    return new Sqlite(_dal);
                case MyType.Mssql:
                    return new MsSql(_dal);
                case MyType.Mysql:
                    return new MySql(_dal);
                case MyType.Oracle:
                    return new Oracle(_dal);
                default:
                    return new Access(_dal);
            }
        }
        #endregion

        #region 处理数据函数(不带参数)

        /// <summary>
        /// 获取DataSet数据列表（带分页）
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <returns>返回带分页自定义内存表</returns>
        private DataSet GetDataSet(string sql, CommandType ctype, int startindex, int pagesize, string dataname) => IsSql().GetDataSet(sql, ctype, startindex, pagesize, dataname);

        /// <summary>
        /// 获取DataSet数据列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="dataname">内存表</param>
        /// <returns>返回自定义内存表</returns>
        private DataSet GetDataSet(string sql, CommandType ctype, string dataname) => IsSql().GetDataSet(sql, ctype, dataname);

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <returns>返回执行的行数</returns>
        private int GetExecuteNonQuery(string sql, CommandType ctype) => IsSql().GetExecuteNonQuery(sql, ctype);

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <returns>返回object对象</returns>
        private object GetExecuteScalar(string sql, CommandType ctype) => IsSql().GetExecuteScalar(sql, ctype);

        /// <summary>
        /// 判断是否存在值
        /// </summary>
        /// <param name="sql">计算查询结果语句</param>
        /// <param name="ctype">请求类型</param>
        /// <returns>查询结果（true/false）</returns>
        public bool GetExists(string sql, CommandType ctype) => GetExists(sql, ctype, null);

        /// <summary>
        /// 获取数据记录集列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <returns>返回记录集列表</returns>
        private IDataReader GetDataReader(string sql, CommandType ctype) => IsSql().GetDataReader(sql, ctype);

        #endregion

        #region 处理数据函数(带参数)

        /// <summary>
        /// 获取DataSet数据列表（带分页）
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回带分页自定义内存表</returns>
        private DataSet GetDataSet(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param) => IsSql().GetDataSet(sql, ctype, startindex, pagesize, dataname, param);

        /// <summary>
        /// 获取DataSet数据列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回自定义内存表</returns>
        private DataSet GetDataSet(string sql, CommandType ctype, string dataname, params IDataParameter[] param) => IsSql().GetDataSet(sql, ctype, dataname, param);

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="param">参数</param>
        /// <returns>返回执行的行数</returns>
        private int GetExecuteNonQuery(string sql, CommandType ctype, params IDataParameter[] param) => IsSql().GetExecuteNonQuery(sql, ctype, param);

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="param">参数</param>
        /// <returns>返回object对象</returns>
        private object GetExecuteScalar(string sql, CommandType ctype, params IDataParameter[] param) => IsSql().GetExecuteScalar(sql, ctype, param);

        /// <summary>
        /// 判断是否存在值
        /// </summary>
        /// <param name="sql">计算查询结果语句</param>
        /// <param name="ctype">请求类型</param>
        /// <param name="param">参数</param>
        /// <returns>查询结果（true/false）</returns>
        public bool GetExists(string sql, CommandType ctype, params IDataParameter[] param)
        {
            var obj = GetExecuteScalar(sql, ctype, param);
            var i = ((Equals(obj, null)) || (Equals(obj, DBNull.Value))) ? 0 : int.Parse(obj.ToString());
            return (i != 0);
        }

        /// <summary>
        /// 获取数据记录集列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="param">参数</param>
        /// <returns>返回记录集列表</returns>
        private IDataReader GetDataReader(string sql, CommandType ctype, params IDataParameter[] param) => IsSql().GetDataReader(sql, ctype, param);

        #endregion

        #region 引用方法(不带参数+可调枚举类型：SQL语句文本或存储过程)

        /// <summary>
        /// 获取DataSet数据列表（带分页）
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataSet MyDs(string sql, CommandType ctype, int startindex, int pagesize, string dataname) => GetDataSet(sql, ctype, startindex, pagesize, dataname);

        /// <summary>
        /// 获取DataSet数据列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="dataname">内存表</param>
        /// <returns>返回自定义内存表</returns>
        public DataSet MyDs(string sql, CommandType ctype, string dataname) => GetDataSet(sql, ctype, dataname);

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <returns>返回执行的行数</returns>
        public int MyExec(string sql, CommandType ctype) => GetExecuteNonQuery(sql, ctype);

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <returns>返回object对象</returns>
        public object MyTotal(string sql, CommandType ctype) => GetExecuteScalar(sql, ctype);

        /// <summary>
        /// 判断数据是否存在
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <returns>返回bool表达式</returns>
        public bool MyExist(string sql, CommandType ctype) => GetExists(sql, ctype);

        /// <summary>
        /// 获取数据记录集列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <returns>返回记录集列表</returns>
        public IDataReader MyRead(string sql, CommandType ctype) => GetDataReader(sql, ctype);

        #endregion

        #region 引用方法(带参数+可调枚举类型：SQL语句文本或存储过程)

        /// <summary>
        /// 获取DataSet数据列表（带分页）
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataSet MyDs(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param) => GetDataSet(sql, ctype, startindex, pagesize, dataname, param);

        /// <summary>
        /// 获取DataTable数据列表（带分页）
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataTable MyDt(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param) => MyDs(sql, ctype, startindex, pagesize, dataname, param).Tables[0];

        /// <summary>
        /// 获取DataView数据列表（带分页）
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataView MyDv(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param) => MyDt(sql, ctype, startindex, pagesize, dataname, param).DefaultView;

        /// <summary>
        /// 获取DataSet数据列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回自定义内存表</returns>
        public DataSet MyDs(string sql, CommandType ctype, string dataname, params IDataParameter[] param) => GetDataSet(sql, ctype, dataname, param);

        /// <summary>
        /// 获取DataTable数据列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回自定义内存表</returns>
        public DataTable MyDt(string sql, CommandType ctype, string dataname, params IDataParameter[] param) => MyDs(sql, ctype, dataname, param).Tables[0];

        /// <summary>
        /// 获取DataView数据列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回自定义内存表</returns>
        public DataView MyDv(string sql, CommandType ctype, string dataname, params IDataParameter[] param) => MyDt(sql, ctype, dataname, param).DefaultView;

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="param">参数</param>
        /// <returns>返回执行的行数</returns>
        public int MyExec(string sql, CommandType ctype, params IDataParameter[] param) => GetExecuteNonQuery(sql, ctype, param);

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="param">参数</param>
        /// <returns>返回object对象</returns>
        public object MyTotal(string sql, CommandType ctype, params IDataParameter[] param) => GetExecuteScalar(sql, ctype, param);

        /// <summary>
        /// 判断数据是否存在
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="param">参数</param>
        /// <returns>返回bool表达式</returns>
        public bool MyExist(string sql, CommandType ctype, params IDataParameter[] param) => GetExists(sql, ctype, param);

        /// <summary>
        /// 获取数据记录集列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ctype">类型</param>
        /// <param name="param">参数</param>
        /// <returns>返回记录集列表</returns>
        public IDataReader MyRead(string sql, CommandType ctype, params IDataParameter[] param) => GetDataReader(sql, ctype, param);

        #endregion

        #region 引用方法(不带参数+不可调枚举类型：SQL文本命令)

        /// <summary>
        /// 获取DataSet数据列表（带分页），带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataSet MyDs(string sql, int startindex, int pagesize, string dataname="ds") => MyDs(sql, CommandType.Text, startindex, pagesize, dataname);

        /// <summary>
        /// 获取DataSet数据列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dataname">内存表</param>
        /// <returns>返回自定义内存表</returns>
        public DataSet MyDs(string sql, string dataname="ds") => MyDs(sql, CommandType.Text, dataname);

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回执行的行数</returns>
        public int MyExec(string sql) => MyExec(sql, CommandType.Text);

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回object对象</returns>
        public object MyTotal(string sql) => MyTotal(sql, CommandType.Text);

        /// <summary>
        /// 判断数据是否存在
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回bool表达式</returns>
        public bool MyExist(string sql) => MyExist(sql, CommandType.Text);

        /// <summary>
        /// 获取数据记录集列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回记录集列表</returns>
        public IDataReader MyRead(string sql) => MyRead(sql, CommandType.Text);

        #endregion

        #region 引用方法(带参数+不可调枚举类型：SQL文本命令)

        /// <summary>
        /// 获取DataSet数据列表（带分页），带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataSet MyDs(string sql, int startindex, int pagesize, string dataname, params IDataParameter[] param) => MyDs(sql, CommandType.Text, startindex, pagesize, dataname, param);

        /// <summary>
        /// 获取DataSet数据列表（带分页）
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="param">参数</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataSet MyDs(string sql, int startindex, int pagesize, params IDataParameter[] param) => MyDs(sql, CommandType.Text, startindex, pagesize, "ds", param);

        /// <summary>
        /// 获取DataSet数据列表，带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回自定义内存表</returns>
        public DataSet MyDs(string sql, string dataname, params IDataParameter[] param) => MyDs(sql, CommandType.Text, dataname, param);

        /// <summary>
        /// 获取DataSet数据列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param">参数</param>
        /// <returns>返回自定义内存表</returns>
        public DataSet MyDs(string sql, params IDataParameter[] param) => MyDs(sql, CommandType.Text, "ds", param);

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param">参数</param>
        /// <returns>返回执行的行数</returns>
        public int MyExec(string sql, params IDataParameter[] param) => MyExec(sql, CommandType.Text, param);

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param">参数</param>
        /// <returns>返回object对象</returns>
        public object MyTotal(string sql, params IDataParameter[] param) => MyTotal(sql, CommandType.Text, param);

        /// <summary>
        /// 判断数据是否存在
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param"></param>
        /// <returns>返回bool表达式</returns>
        public bool MyExist(string sql, params IDataParameter[] param) => MyExist(sql, CommandType.Text, param);

        /// <summary>
        /// 获取数据记录集列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param">参数</param>
        /// <returns>返回记录集列表</returns>
        public IDataReader MyRead(string sql, params IDataParameter[] param) => MyRead(sql, CommandType.Text, param);

        #endregion

        #region 引用缓存(带参数+形参或存储过程)
        /// <summary>
        /// 获取缓存参数
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public IDataParameter[] GetCache(string cacheKey) => IsSql().GetCachedParameters(cacheKey);

        /// <summary>
        /// 设置缓存参数
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="commandParameters"></param>
        public void SetCache(string cacheKey, params IDataParameter[] commandParameters) => IsSql().CacheParameters(cacheKey, commandParameters);

        /// <summary>
        /// 创建传参转化
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IDataParameter MyParam(string name, string value) => IsSql().MyParams(name, value);

        #endregion

        #region 引用参数(键值关系)
        public IDataParameter AddParam(string name, object value) => IsSql().MyParams(name, value);

        #endregion

        #region 引用方法（DataTable与DataView 带参数）
        /// <summary>
        /// 获取DataTable数据列表（带分页），带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataTable MyDt(string sql, int startindex, int pagesize, string dataname, params IDataParameter[] param) => MyDs(sql, CommandType.Text, startindex, pagesize, dataname, param).Tables[0];

        /// <summary>
        /// 获取DataTable数据列表（带分页）
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="param">参数</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataTable MyDt(string sql, int startindex, int pagesize, params IDataParameter[] param) => MyDt(sql, startindex, pagesize, "ds", param);

        /// <summary>
        /// 获取DataTable数据列表，带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回自定义内存表</returns>
        public DataTable MyDt(string sql, string dataname, params IDataParameter[] param) => MyDs(sql, CommandType.Text, dataname, param).Tables[0];

        /// <summary>
        /// 获取DataTable数据列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param">参数</param>
        /// <returns>返回自定义内存表</returns>
        public DataTable MyDt(string sql, params IDataParameter[] param) => MyDt(sql, "ds", param);

        /// <summary>
        /// 获取DataView数据列表（带分页），带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataView MyDv(string sql, int startindex, int pagesize, string dataname, params IDataParameter[] param) => MyDt(sql, startindex, pagesize, dataname, param).DefaultView;

        /// <summary>
        /// 获取DataView数据列表（带分页）
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="param">参数</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataView MyDv(string sql, int startindex, int pagesize, params IDataParameter[] param) => MyDv(sql, startindex, pagesize, "ds", param);

        /// <summary>
        /// 获取DataView数据列表，带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dataname">内存表</param>
        /// <param name="param">参数</param>
        /// <returns>返回自定义内存表</returns>
        public DataView MyDv(string sql, string dataname, params IDataParameter[] param) => MyDt(sql, dataname, param).DefaultView;

        /// <summary>
        /// 获取DataView数据列表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param">参数</param>
        /// <returns>返回自定义内存表</returns>
        public DataView MyDv(string sql, params IDataParameter[] param) => MyDv(sql, "ds", param);

        #endregion

        #region 引用方法（DataTable与DataView 不带参数）
        /// <summary>
        /// 获取DataTable数据列表（带分页），带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataTable MyDt(string sql, int startindex, int pagesize, string dataname="ds") => MyDt(sql, startindex, pagesize, dataname, null);

        /// <summary>
        /// 获取DataTable数据列表，带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dataname">内存表</param>
        /// <returns>返回自定义内存表</returns>
        public DataTable MyDt(string sql, string dataname="ds") => MyDt(sql, dataname, null);

        /// <summary>
        /// 获取DataView数据列表（带分页），带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="startindex">页面传递参数</param>
        /// <param name="pagesize">每页分配记录数</param>
        /// <param name="dataname">内存表</param>
        /// <returns>返回带分页自定义内存表</returns>
        public DataView MyDv(string sql, int startindex, int pagesize, string dataname="ds") => MyDv(sql, startindex, pagesize, dataname, null);

        /// <summary>
        /// 获取DataView数据列表，带数据名称
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="dataname">内存表</param>
        /// <returns>返回自定义内存表</returns>
        public DataView MyDv(string sql, string dataname="ds") => MyDv(sql, dataname, null);

        #endregion

        public void Dispose()
        {
            
        }
    }
    #endregion

}
