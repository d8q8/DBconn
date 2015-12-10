using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Web;
using System.Windows.Forms;

namespace DBconn
{

    #region ��ͬ���ݿ�ӿ���
    /// <summary>
    /// ��ͬ���ݿ�ӿڷ�װ
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

    #region ö�����ݿ�����
    /// <summary>
    /// ��ȡ��ǰ���ݿ�
    /// </summary>
    public enum MyType
    {
        Access2003, Access2007, Access2013, Mssql, Mysql, Oracle, Sqlite
    }
    #endregion

    #region ͨ�����ݿ����
    /// <summary>
    /// ͨ�����ݿ����
    /// </summary>
    public sealed class Conn : IDisposable
    {

        #region ���ݿ�����
        //���ô���������web.config����ݿ�·��Ϊ��Access��SqliteΪ���·��������Ϊȫ·��
        //<appSettings>
        //    <add key="access" value="���ݿ�"/>
        //    <add key="sqlite" value="���ݿ�"/>
        //    <add key="sqlserver" value="server=(local);uid=�û���;pwd=����;database=���ݿ�"/>
        //    <add key="oracle" value="Provider=MSDAORA.1;Password=����;User ID=�û���;Data Source=���ݿ�"/>
        //    <add key="mysql" value="server=localhost;user id=�û���;password=����;database=���ݿ�"/>
        //</appSettings>
        //<connectionStrings>
        //    <add name="sqlserver" connectionString="data source=.;initial catalog=���ݿ�;user id=�û���;MultipleActiveResultSets=True;App=EntityFramework" providerName="System.Data.SqlClient" />
        //</connectionStrings>
        private readonly string _dal;
        private readonly MyType _mytype; 

        public Conn(MyType mt = MyType.Access2003, string connstr = "access")
        {
            _mytype = mt;
            _dal = connstr;
            //var connStr = ConfigurationManager.AppSettings[connstr].ToString(CultureInfo.InvariantCulture);
            var connStr = ConfigurationManager.ConnectionStrings[connstr].ConnectionString;
            //�������Ӵ�����
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

        #region �жϵ�ǰʹ���������ݿ�
        /// <summary>
        /// �жϵ�ǰʹ�ú������ݿ�
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
                //case MyType.Oracle:
                //    return new Oracle(_dal);
                default:
                    return new Access(_dal);
            }
        }
        #endregion

        #region �������ݺ���(��������)

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        private DataSet GetDataSet(string sql, CommandType ctype, int startindex, int pagesize, string dataname)
        {
            return IsSql().GetDataSet(sql, ctype, startindex, pagesize, dataname);
        }

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        private DataSet GetDataSet(string sql, CommandType ctype, string dataname)
        {
            return IsSql().GetDataSet(sql, ctype, dataname);
        }

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����ִ�е�����</returns>
        private int GetExecuteNonQuery(string sql, CommandType ctype)
        {
            return IsSql().GetExecuteNonQuery(sql, ctype);
        }

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����object����</returns>
        private object GetExecuteScalar(string sql, CommandType ctype)
        {
            return IsSql().GetExecuteScalar(sql, ctype);
        }

        /// <summary>
        /// �ж��Ƿ����ֵ
        /// </summary>
        /// <param name="sql">�����ѯ������</param>
        /// <param name="ctype">��������</param>
        /// <returns>��ѯ�����true/false��</returns>
        public bool GetExists(string sql, CommandType ctype)
        {
            return GetExists(sql, ctype, null);
        }

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        private IDataReader GetDataReader(string sql, CommandType ctype)
        {
            return IsSql().GetDataReader(sql, ctype);
        }

        #endregion

        #region �������ݺ���(������)

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        private DataSet GetDataSet(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param)
        {
            return IsSql().GetDataSet(sql, ctype, startindex, pagesize, dataname, param);
        }

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        private DataSet GetDataSet(string sql, CommandType ctype, string dataname, params IDataParameter[] param)
        {
            return IsSql().GetDataSet(sql, ctype, dataname, param);
        }

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>����ִ�е�����</returns>
        private int GetExecuteNonQuery(string sql, CommandType ctype, params IDataParameter[] param)
        {
            return IsSql().GetExecuteNonQuery(sql, ctype, param);
        }

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>����object����</returns>
        private object GetExecuteScalar(string sql, CommandType ctype, params IDataParameter[] param)
        {
            return IsSql().GetExecuteScalar(sql, ctype, param);
        }

        /// <summary>
        /// �ж��Ƿ����ֵ
        /// </summary>
        /// <param name="sql">�����ѯ������</param>
        /// <param name="ctype">��������</param>
        /// <param name="param">����</param>
        /// <returns>��ѯ�����true/false��</returns>
        public bool GetExists(string sql, CommandType ctype, params IDataParameter[] param)
        {
            var obj = GetExecuteScalar(sql, ctype, param);
            var i = (Equals(obj, null) || Equals(obj, DBNull.Value)) ? 0 : int.Parse(obj.ToString());
            return (i != 0);
        }

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        private IDataReader GetDataReader(string sql, CommandType ctype, params IDataParameter[] param)
        {
            return IsSql().GetDataReader(sql, ctype, param);
        }

        #endregion

        #region ���÷���(��������+�ɵ�ö�����ͣ�SQL����ı���洢����)

        /// <summary>
        /// ��ȡList�����б�����ҳ��
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="ctype"></param>
        /// <param name="startindex"></param>
        /// <param name="pagesize"></param>
        /// <param name="dataname"></param>
        /// <returns>���ش���ҳ�Զ���List��</returns>
        public IList<T> MyDsList<T>(string sql, CommandType ctype, int startindex, int pagesize, string dataname)
        {
            var ds = GetDataSet(sql, ctype, startindex, pagesize, dataname);
            return Db.DataSetToIList<T>(ds, dataname);
        }

        /// <summary>
        /// ��ȡList�����б�
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="ctype"></param>
        /// <param name="dataname"></param>
        /// <returns>�����Զ���List��</returns>
        public IList<T> MyDsList<T>(string sql, CommandType ctype, string dataname)
        {
            var ds = GetDataSet(sql, ctype, dataname);
            return Db.DataSetToIList<T>(ds, dataname);
        }

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataSet MyDs(string sql, CommandType ctype, int startindex, int pagesize, string dataname)
        {
            return GetDataSet(sql, ctype, startindex, pagesize, dataname);
        }

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet MyDs(string sql, CommandType ctype, string dataname)
        {
            return GetDataSet(sql, ctype, dataname);
        }

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����ִ�е�����</returns>
        public int MyExec(string sql, CommandType ctype = CommandType.Text)
        {
            return GetExecuteNonQuery(sql, ctype);
        }

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����object����</returns>
        public object MyTotal(string sql, CommandType ctype = CommandType.Text)
        {
            return GetExecuteScalar(sql, ctype);
        }

        /// <summary>
        /// �ж������Ƿ����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����bool���ʽ</returns>
        public bool MyExist(string sql, CommandType ctype = CommandType.Text)
        {
            return GetExists(sql, ctype);
        }

        /// <summary>
        /// ��ȡ���ݼ�¼��List�б�
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="ctype"></param>
        /// <returns></returns>
        public IList<T> MyReadList<T>(string sql, CommandType ctype = CommandType.Text)
        {
            var dr = GetDataReader(sql, ctype);
            return Db.DataReaderToList<T>(dr);
        }

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        public IDataReader MyRead(string sql, CommandType ctype = CommandType.Text)
        {
            return GetDataReader(sql, ctype);
        }

        /// <summary>
        /// ��ȡ����ģ�ͼ�¼
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="ctype"></param>
        /// <returns></returns>
        public T MyModel<T>(string sql, CommandType ctype = CommandType.Text)
        {
            var dr = GetDataReader(sql, ctype);
            return Db.ReaderToModel<T>(dr);
        }

        #endregion

        #region ���÷���(������+�ɵ�ö�����ͣ�SQL����ı���洢����)

        /// <summary>
        /// ��ȡList�����б�����ҳ��
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="ctype"></param>
        /// <param name="startindex"></param>
        /// <param name="pagesize"></param>
        /// <param name="dataname"></param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ���List��</returns>
        public IList<T> MyDsList<T>(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param)
        {
            var ds = GetDataSet(sql, ctype, startindex, pagesize, dataname, param);
            return Db.DataSetToIList<T>(ds, dataname);
        }

        /// <summary>
        /// ��ȡList�����б�
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="ctype"></param>
        /// <param name="dataname"></param>
        /// <param name="param">����</param>
        /// <returns>�����Զ���List��</returns>
        public IList<T> MyDsList<T>(string sql, CommandType ctype, string dataname, params IDataParameter[] param)
        {
            var ds = GetDataSet(sql, ctype, dataname, param);
            return Db.DataSetToIList<T>(ds, dataname);
        }

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataSet MyDs(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param)
        {
            return GetDataSet(sql, ctype, startindex, pagesize, dataname, param);
        }

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet MyDs(string sql, CommandType ctype, string dataname, params IDataParameter[] param)
        {
            return GetDataSet(sql, ctype, dataname, param);
        }

        /// <summary>
        /// ��ȡDataTable�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataTable MyDt(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param)
        {
            return MyDs(sql, ctype, startindex, pagesize, dataname, param).Tables[0];
        }

        /// <summary>
        /// ��ȡDataTable�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataTable MyDt(string sql, CommandType ctype, string dataname, params IDataParameter[] param)
        {
            return MyDs(sql, ctype, dataname, param).Tables[0];
        }

        /// <summary>
        /// ��ȡDataView�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataView MyDv(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param)
        {
            return MyDt(sql, ctype, startindex, pagesize, dataname, param).DefaultView;
        }

        /// <summary>
        /// ��ȡDataView�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataView MyDv(string sql, CommandType ctype, string dataname, params IDataParameter[] param)
        {
            return MyDt(sql, ctype, dataname, param).DefaultView;
        }

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>����ִ�е�����</returns>
        public int MyExec(string sql, CommandType ctype, params IDataParameter[] param)
        {
            return GetExecuteNonQuery(sql, ctype, param);
        }

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>����object����</returns>
        public object MyTotal(string sql, CommandType ctype, params IDataParameter[] param)
        {
            return GetExecuteScalar(sql, ctype, param);
        }

        /// <summary>
        /// �ж������Ƿ����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>����bool���ʽ</returns>
        public bool MyExist(string sql, CommandType ctype, params IDataParameter[] param)
        {
            return GetExists(sql, ctype, param);
        }

        /// <summary>
        /// ��ȡ���ݼ�¼��List�б�
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="ctype"></param>
        /// <param name="param">����</param>
        /// <returns></returns>
        public IList<T> MyReadList<T>(string sql, CommandType ctype, params IDataParameter[] param)
        {
            var dr = GetDataReader(sql, ctype, param);
            return Db.DataReaderToList<T>(dr);
        }

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        public IDataReader MyRead(string sql, CommandType ctype, params IDataParameter[] param)
        {
            return GetDataReader(sql, ctype, param);
        }

        /// <summary>
        /// ��ȡ����ģ�ͼ�¼
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="ctype"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public T MyModel<T>(string sql, CommandType ctype, params IDataParameter[] param)
        {
            var dr = GetDataReader(sql, ctype, param);
            return Db.ReaderToModel<T>(dr);
        }

        #endregion

        #region ���÷���(��������+���ɵ�ö�����ͣ�SQL�ı�����)

        /// <summary>
        /// ��ȡList�����б�����ҳ��������������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="startindex"></param>
        /// <param name="pagesize"></param>
        /// <param name="dataname"></param>
        /// <returns></returns>
        public IList<T> MyDsList<T>(string sql, int startindex, int pagesize, string dataname = "ds")
        {
            return MyDsList<T>(sql, CommandType.Text, startindex, pagesize, dataname);
        }

        /// <summary>
        /// ��ȡList�����б�����������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="dataname"></param>
        /// <returns></returns>
        public IList<T> MyDsList<T>(string sql, string dataname = "ds")
        {
            return MyDsList<T>(sql, CommandType.Text, dataname);
        }

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��������������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataSet MyDs(string sql, int startindex, int pagesize, string dataname = "ds")
        {
            return MyDs(sql, CommandType.Text, startindex, pagesize, dataname);
        }

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet MyDs(string sql, string dataname = "ds")
        {
            return MyDs(sql, CommandType.Text, dataname);
        }

        #endregion

        #region ���÷���(������+���ɵ�ö�����ͣ�SQL�ı�����)

        /// <summary>
        /// ��ȡList�����б�����ҳ��������������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="startindex"></param>
        /// <param name="pagesize"></param>
        /// <param name="dataname"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public IList<T> MyDsList<T>(string sql, int startindex, int pagesize, string dataname, params IDataParameter[] param)
        {
            return MyDsList<T>(sql, CommandType.Text, startindex, pagesize, dataname, param);
        }

        /// <summary>
        /// ��ȡList�����б�����ҳ��
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="startindex"></param>
        /// <param name="pagesize"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public IList<T> MyDsList<T>(string sql, int startindex, int pagesize, params IDataParameter[] param)
        {
            return MyDsList<T>(sql, CommandType.Text, startindex, pagesize, "ds", param);
        }

        /// <summary>
        /// ��ȡList�����б�����������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="dataname"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public IList<T> MyDsList<T>(string sql, string dataname, params IDataParameter[] param)
        {
            return MyDsList<T>(sql, CommandType.Text, dataname, param);
        }

        /// <summary>
        /// ��ȡList�����б�
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public IList<T> MyDsList<T>(string sql, params IDataParameter[] param)
        {
            return MyDsList<T>(sql, CommandType.Text, "ds", param);
        }

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��������������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataSet MyDs(string sql, int startindex, int pagesize, string dataname, params IDataParameter[] param)
        {
            return MyDs(sql, CommandType.Text, startindex, pagesize, dataname, param);
        }

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataSet MyDs(string sql, int startindex, int pagesize, params IDataParameter[] param)
        {
            return MyDs(sql, CommandType.Text, startindex, pagesize, "ds", param);
        }

        /// <summary>
        /// ��ȡDataSet�����б�����������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet MyDs(string sql, string dataname, params IDataParameter[] param)
        {
            return MyDs(sql, CommandType.Text, dataname, param);
        }

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet MyDs(string sql, params IDataParameter[] param)
        {
            return MyDs(sql, CommandType.Text, "ds", param);
        }

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>����ִ�е�����</returns>
        public int MyExec(string sql, params IDataParameter[] param)
        {
            return MyExec(sql, CommandType.Text, param);
        }

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>����object����</returns>
        public object MyTotal(string sql, params IDataParameter[] param)
        {
            return MyTotal(sql, CommandType.Text, param);
        }

        /// <summary>
        /// �ж������Ƿ����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param"></param>
        /// <returns>����bool���ʽ</returns>
        public bool MyExist(string sql, params IDataParameter[] param)
        {
            return MyExist(sql, CommandType.Text, param);
        }

        /// <summary>
        /// ��ȡ���ݼ�¼��List�б�
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public IList<T> MyReadList<T>(string sql, params IDataParameter[] param)
        {
            return MyReadList<T>(sql, CommandType.Text, param);
        }

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        public IDataReader MyRead(string sql, params IDataParameter[] param)
        {
            return MyRead(sql, CommandType.Text, param);
        }

        /// <summary>
        /// ��ȡ����ģ�ͼ�¼
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public T MyModel<T>(string sql, params IDataParameter[] param)
        {
            return MyModel<T>(sql, CommandType.Text, param);
        }

        #endregion

        #region ���û���(������+�βλ�洢����)
        /// <summary>
        /// ��ȡ�������
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public IDataParameter[] GetCache(string cacheKey)
        {
            return IsSql().GetCachedParameters(cacheKey);
        }

        /// <summary>
        /// ���û������
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="commandParameters"></param>
        public void SetCache(string cacheKey, params IDataParameter[] commandParameters)
        {
            IsSql().CacheParameters(cacheKey, commandParameters);
        }
        #endregion

        #region ���ò���(��ֵ��ϵ)
        /// <summary>
        /// ��������ת��
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IDataParameter MyParam(string name, string value)
        {
            return IsSql().MyParams(name, value);
        }
        /// <summary>
        /// ���ò���,����ֵ
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IDataParameter AddParam(string name, object value)
        {
            return IsSql().MyParams(name, value);
        }

        #endregion

        #region ���÷�����DataTable��DataView ��������
        /// <summary>
        /// ��ȡDataTable�����б�����ҳ��������������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataTable MyDt(string sql, int startindex, int pagesize, string dataname, params IDataParameter[] param)
        {
            return MyDs(sql, CommandType.Text, startindex, pagesize, dataname, param).Tables[0];
        }

        /// <summary>
        /// ��ȡDataTable�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataTable MyDt(string sql, int startindex, int pagesize, params IDataParameter[] param)
        {
            return MyDt(sql, startindex, pagesize, "ds", param);
        }

        /// <summary>
        /// ��ȡDataTable�����б�����������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataTable MyDt(string sql, string dataname, params IDataParameter[] param)
        {
            return MyDs(sql, CommandType.Text, dataname, param).Tables[0];
        }

        /// <summary>
        /// ��ȡDataTable�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataTable MyDt(string sql, params IDataParameter[] param)
        {
            return MyDt(sql, "ds", param);
        }

        /// <summary>
        /// ��ȡDataView�����б�����ҳ��������������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataView MyDv(string sql, int startindex, int pagesize, string dataname, params IDataParameter[] param)
        {
            return MyDt(sql, startindex, pagesize, dataname, param).DefaultView;
        }

        /// <summary>
        /// ��ȡDataView�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataView MyDv(string sql, int startindex, int pagesize, params IDataParameter[] param)
        {
            return MyDv(sql, startindex, pagesize, "ds", param);
        }

        /// <summary>
        /// ��ȡDataView�����б�����������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataView MyDv(string sql, string dataname, params IDataParameter[] param)
        {
            return MyDt(sql, dataname, param).DefaultView;
        }

        /// <summary>
        /// ��ȡDataView�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataView MyDv(string sql, params IDataParameter[] param)
        {
            return MyDv(sql, "ds", param);
        }

        #endregion

        #region ���÷�����DataTable��DataView ����������
        /// <summary>
        /// ��ȡDataTable�����б�����ҳ��������������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataTable MyDt(string sql, int startindex, int pagesize, string dataname = "ds")
        {
            return MyDt(sql, startindex, pagesize, dataname, null);
        }

        /// <summary>
        /// ��ȡDataTable�����б�����������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataTable MyDt(string sql, string dataname = "ds")
        {
            return MyDt(sql, dataname, null);
        }

        /// <summary>
        /// ��ȡDataView�����б�����ҳ��������������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataView MyDv(string sql, int startindex, int pagesize, string dataname = "ds")
        {
            return MyDv(sql, startindex, pagesize, dataname, null);
        }

        /// <summary>
        /// ��ȡDataView�����б�����������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataView MyDv(string sql, string dataname = "ds")
        {
            return MyDv(sql, dataname, null);
        }

        #endregion

        public void Dispose()
        {

        }
    }
    #endregion

}
