using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DBconn
{
    /// <summary>
    /// 针对MSSQL库
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Helper<T> where T : class, new()
    {
        /// <summary>
        /// 链接数据库字符串
        /// </summary>
        private readonly string _connStr;
        /// <summary>
        /// 数据库连接
        /// </summary>
        private SqlConnection _connection;
        public Helper(string connStr = "DB")
        {
            if (_connStr == null)
            {
                _connStr = ConfigurationManager.ConnectionStrings[connStr].ConnectionString;
            }
            OpenConnect();
        }
        /// <summary>
        /// 打开数据库连接
        /// </summary>
        private void OpenConnect()
        {
            if (_connection != null && _connection.State == ConnectionState.Open) return;
            _connection = new SqlConnection(_connStr);
            _connection.Open();
        }
        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public void CloseConnect()
        {
            if (_connection == null || _connection.State == ConnectionState.Closed) return;
            _connection.Close();
        }

        /// <summary>
        /// 执行查询语句
        /// </summary>
        /// <param name="strSql">SQL语句</param>
        /// <param name="obQuery">SQL参数的值</param>
        /// <returns></returns>
        public SqlDataReader ExecReader(string strSql, object obQuery)
        {
            var command = new SqlCommand(strSql, _connection);
            if (obQuery != null)
            {
                var pis = obQuery.GetType().GetProperties();
                foreach (var p in pis)
                {
                    command.Parameters.Add(new SqlParameter(p.Name, p.GetValue(obQuery, null)));
                }
            }
            var reader = command.ExecuteReader();
            return reader;
        }

        /// <summary>
        /// 执行返回单值的查询语句
        /// </summary>
        /// <param name="strSql">SQL语句</param>
        /// <param name="obQuery">SQL参数的值</param>
        /// <returns></returns>
        public object ExecSingleValue(string strSql, object obQuery)
        {
            var command = new SqlCommand(strSql, _connection);
            if (obQuery == null) return command.ExecuteScalar();
            var pis = obQuery.GetType().GetProperties();
            foreach (var p in pis)
            {
                command.Parameters.Add(new SqlParameter(p.Name, p.GetValue(obQuery, null)));
            }
            return command.ExecuteScalar();
        }

        /// <summary>
        /// 执行非查询语句
        /// </summary>
        /// <param name="strSql">SQL语句</param>
        /// <param name="obQuery">SQL参数的值</param>
        /// <returns></returns>
        public int ExecNoQuery(string strSql, object obQuery)
        {
            var command = new SqlCommand(strSql, _connection);
            if (obQuery == null) return command.ExecuteNonQuery();
            var pis = obQuery.GetType().GetProperties();
            foreach (var p in pis)
            {
                command.Parameters.Add(new SqlParameter(p.Name, p.GetValue(obQuery, null)));
            }
            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <param name="strSql">SQL语句</param>
        /// <param name="obQuery">SQL参数的值</param>
        /// <returns></returns>
        public IList<T> GetList(string strSql, object obQuery)
        {
            //调用执行查询语句函数，返回SqlDataReader
            var reader = ExecReader(strSql, obQuery);
            //定义返回的列表
            var list = new List<T>();
            //定义T类型的实体
            var model = new T();
            //获取T类型实体的属性类型和值
            var pis = model.GetType().GetProperties();
            //获取数据库返回的列数
            var intColCount = reader.FieldCount;
            //遍历SqlDataReader
            while (reader.Read())
            {
                //定义
                var valueNumber = 0;
                //重新实例化T
                model = new T();
                //从数据库拿出一条数据后，循环遍历T类型的属性类型和值
                for (var i = 0; i < intColCount; i++)
                {
                    //判断第一列是否为row_number，此为分页使用
                    if (reader.GetName(i) == "row_number") valueNumber++;
                    //设置T对应属性的值
                    pis[i].SetValue(model, reader.GetValue(valueNumber), null);
                    valueNumber++;
                }
                //将T添加到列表中
                list.Add(model);
            }
            return list;
        }

        /// <summary>
        /// 获取分页
        /// </summary>
        /// <param name="strTotalSql">总共个数的SQL</param>
        /// <param name="obTotalQuery">总共个数的SQL参数的值</param>
        /// <param name="strSql">分页的SQL</param>
        /// <param name="obQuery">分页SQL参数的值</param>
        /// <param name="intPageIndex">分页编号</param>
        /// <param name="intPageSize">分页大小</param>
        /// <returns></returns>
        public PagedList<T> GetPageList(string strTotalSql, object obTotalQuery, string strSql, object obQuery, int intPageIndex, int intPageSize)
        {
            //定义分页对象的编号和大小
            var pageList = new PagedList<T>(intPageIndex, intPageSize)
            {
                IntTotalCount = (int)ExecSingleValue(strTotalSql, obTotalQuery)
            };
            //执行获取单个值的函数，设置分页对象的总元素
            //设置分页对象的分页数
            if (pageList.IntTotalCount % intPageSize == 0) pageList.IntPages = pageList.IntTotalCount / intPageSize;
            else pageList.IntPages = pageList.IntTotalCount / intPageSize + 1;
            //定义列表，调用获取列表的函数获取此分页的元素
            var list = GetList(strSql, obQuery);
            //将列表元素添加到分页对象当中
            pageList.AddRange(list);
            //设置分页对象是否有上一页和下一页
            pageList.HasNextPage = pageList.IntPageIndex < pageList.IntPages;
            pageList.HasPrPage = pageList.IntPageIndex > 1;
            return pageList;
        }
        /// <summary>
        /// 获取单个实体
        /// </summary>
        /// <param name="strSql">SQL语句</param>
        /// <param name="obQuery">SQL参数的值</param>
        /// <returns></returns>
        public T GetTm(string strSql, object obQuery)
        {
            //调用执行查询语句，返回SqlDataReader
            var reader = ExecReader(strSql, obQuery);
            //新建一个T类型
            var model = new T();
            //获取T类型的属性类型和值
            var pis = model.GetType().GetProperties();
            //获取数据库返回数据的列数
            var intColCount = reader.FieldCount;
            //读取数据，填充T
            if (!reader.Read()) return model;
            var valueNumber = 0;
            for (var i = 0; i < intColCount; i++)
            {
                pis[i].SetValue(model, reader.GetValue(valueNumber), null);
                valueNumber++;
            }
            return model;
        }
    }

    public class PagedList<T> : List<T>
    {
        /// <summary>
        /// 分页编号
        /// </summary>
        public int IntPageIndex { get; set; }
        /// <summary>
        /// 分页大小
        /// </summary>
        public int IntPageSize { get; set; }
        /// <summary>
        /// 分页数
        /// </summary>
        public int IntPages { get; set; }
        /// <summary>
        /// 总元素的个数
        /// </summary>
        public int IntTotalCount { get; set; }
        /// <summary>
        /// 此分页元素的个数
        /// </summary>
        public int IntCount { get; set; }
        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage { get; set; }
        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPrPage { get; set; }
        public PagedList(int intPageIndex, int intPageSize)
        {
            IntPageIndex = intPageIndex;
            IntPageSize = intPageSize;
        }
    }
}