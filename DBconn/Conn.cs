using System;
using System.Configuration;
using System.Data;
using System.Globalization;
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
        private readonly string _dal;
        private readonly MyType _mytype;

        public Conn(MyType mt = MyType.Access2003, string connstr = "access")
        {
            _mytype = mt;
            _dal = connstr;
            var connStr = ConfigurationManager.AppSettings[connstr].ToString(CultureInfo.InvariantCulture);
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
                case MyType.Oracle:
                    return new Oracle(_dal);
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
        private DataSet GetDataSet(string sql, CommandType ctype, int startindex, int pagesize, string dataname) => IsSql().GetDataSet(sql, ctype, startindex, pagesize, dataname);

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        private DataSet GetDataSet(string sql, CommandType ctype, string dataname) => IsSql().GetDataSet(sql, ctype, dataname);

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����ִ�е�����</returns>
        private int GetExecuteNonQuery(string sql, CommandType ctype) => IsSql().GetExecuteNonQuery(sql, ctype);

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����object����</returns>
        private object GetExecuteScalar(string sql, CommandType ctype) => IsSql().GetExecuteScalar(sql, ctype);

        /// <summary>
        /// �ж��Ƿ����ֵ
        /// </summary>
        /// <param name="sql">�����ѯ������</param>
        /// <param name="ctype">��������</param>
        /// <returns>��ѯ�����true/false��</returns>
        public bool GetExists(string sql, CommandType ctype) => GetExists(sql, ctype, null);

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        private IDataReader GetDataReader(string sql, CommandType ctype) => IsSql().GetDataReader(sql, ctype);

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
        private DataSet GetDataSet(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param) => IsSql().GetDataSet(sql, ctype, startindex, pagesize, dataname, param);

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        private DataSet GetDataSet(string sql, CommandType ctype, string dataname, params IDataParameter[] param) => IsSql().GetDataSet(sql, ctype, dataname, param);

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>����ִ�е�����</returns>
        private int GetExecuteNonQuery(string sql, CommandType ctype, params IDataParameter[] param) => IsSql().GetExecuteNonQuery(sql, ctype, param);

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>����object����</returns>
        private object GetExecuteScalar(string sql, CommandType ctype, params IDataParameter[] param) => IsSql().GetExecuteScalar(sql, ctype, param);

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
            var i = ((Equals(obj, null)) || (Equals(obj, DBNull.Value))) ? 0 : int.Parse(obj.ToString());
            return (i != 0);
        }

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        private IDataReader GetDataReader(string sql, CommandType ctype, params IDataParameter[] param) => IsSql().GetDataReader(sql, ctype, param);

        #endregion

        #region ���÷���(��������+�ɵ�ö�����ͣ�SQL����ı���洢����)

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataSet MyDs(string sql, CommandType ctype, int startindex, int pagesize, string dataname) => GetDataSet(sql, ctype, startindex, pagesize, dataname);

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet MyDs(string sql, CommandType ctype, string dataname) => GetDataSet(sql, ctype, dataname);

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����ִ�е�����</returns>
        public int MyExec(string sql, CommandType ctype) => GetExecuteNonQuery(sql, ctype);

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����object����</returns>
        public object MyTotal(string sql, CommandType ctype) => GetExecuteScalar(sql, ctype);

        /// <summary>
        /// �ж������Ƿ����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����bool���ʽ</returns>
        public bool MyExist(string sql, CommandType ctype) => GetExists(sql, ctype);

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        public IDataReader MyRead(string sql, CommandType ctype) => GetDataReader(sql, ctype);

        #endregion

        #region ���÷���(������+�ɵ�ö�����ͣ�SQL����ı���洢����)

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
        public DataSet MyDs(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param) => GetDataSet(sql, ctype, startindex, pagesize, dataname, param);

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
        public DataTable MyDt(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param) => MyDs(sql, ctype, startindex, pagesize, dataname, param).Tables[0];

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
        public DataView MyDv(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param) => MyDt(sql, ctype, startindex, pagesize, dataname, param).DefaultView;

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet MyDs(string sql, CommandType ctype, string dataname, params IDataParameter[] param) => GetDataSet(sql, ctype, dataname, param);

        /// <summary>
        /// ��ȡDataTable�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataTable MyDt(string sql, CommandType ctype, string dataname, params IDataParameter[] param) => MyDs(sql, ctype, dataname, param).Tables[0];

        /// <summary>
        /// ��ȡDataView�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataView MyDv(string sql, CommandType ctype, string dataname, params IDataParameter[] param) => MyDt(sql, ctype, dataname, param).DefaultView;

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>����ִ�е�����</returns>
        public int MyExec(string sql, CommandType ctype, params IDataParameter[] param) => GetExecuteNonQuery(sql, ctype, param);

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>����object����</returns>
        public object MyTotal(string sql, CommandType ctype, params IDataParameter[] param) => GetExecuteScalar(sql, ctype, param);

        /// <summary>
        /// �ж������Ƿ����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>����bool���ʽ</returns>
        public bool MyExist(string sql, CommandType ctype, params IDataParameter[] param) => GetExists(sql, ctype, param);

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        public IDataReader MyRead(string sql, CommandType ctype, params IDataParameter[] param) => GetDataReader(sql, ctype, param);

        #endregion

        #region ���÷���(��������+���ɵ�ö�����ͣ�SQL�ı�����)

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��������������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataSet MyDs(string sql, int startindex, int pagesize, string dataname="ds") => MyDs(sql, CommandType.Text, startindex, pagesize, dataname);

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet MyDs(string sql, string dataname="ds") => MyDs(sql, CommandType.Text, dataname);

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <returns>����ִ�е�����</returns>
        public int MyExec(string sql) => MyExec(sql, CommandType.Text);

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <returns>����object����</returns>
        public object MyTotal(string sql) => MyTotal(sql, CommandType.Text);

        /// <summary>
        /// �ж������Ƿ����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <returns>����bool���ʽ</returns>
        public bool MyExist(string sql) => MyExist(sql, CommandType.Text);

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <returns>���ؼ�¼���б�</returns>
        public IDataReader MyRead(string sql) => MyRead(sql, CommandType.Text);

        #endregion

        #region ���÷���(������+���ɵ�ö�����ͣ�SQL�ı�����)

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��������������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataSet MyDs(string sql, int startindex, int pagesize, string dataname, params IDataParameter[] param) => MyDs(sql, CommandType.Text, startindex, pagesize, dataname, param);

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataSet MyDs(string sql, int startindex, int pagesize, params IDataParameter[] param) => MyDs(sql, CommandType.Text, startindex, pagesize, "ds", param);

        /// <summary>
        /// ��ȡDataSet�����б�����������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet MyDs(string sql, string dataname, params IDataParameter[] param) => MyDs(sql, CommandType.Text, dataname, param);

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet MyDs(string sql, params IDataParameter[] param) => MyDs(sql, CommandType.Text, "ds", param);

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>����ִ�е�����</returns>
        public int MyExec(string sql, params IDataParameter[] param) => MyExec(sql, CommandType.Text, param);

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>����object����</returns>
        public object MyTotal(string sql, params IDataParameter[] param) => MyTotal(sql, CommandType.Text, param);

        /// <summary>
        /// �ж������Ƿ����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param"></param>
        /// <returns>����bool���ʽ</returns>
        public bool MyExist(string sql, params IDataParameter[] param) => MyExist(sql, CommandType.Text, param);

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        public IDataReader MyRead(string sql, params IDataParameter[] param) => MyRead(sql, CommandType.Text, param);

        #endregion

        #region ���û���(������+�βλ�洢����)
        /// <summary>
        /// ��ȡ�������
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public IDataParameter[] GetCache(string cacheKey) => IsSql().GetCachedParameters(cacheKey);

        /// <summary>
        /// ���û������
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="commandParameters"></param>
        public void SetCache(string cacheKey, params IDataParameter[] commandParameters) => IsSql().CacheParameters(cacheKey, commandParameters);

        /// <summary>
        /// ��������ת��
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IDataParameter MyParam(string name, string value) => IsSql().MyParams(name, value);

        #endregion

        #region ���ò���(��ֵ��ϵ)
        public IDataParameter AddParam(string name, object value) => IsSql().MyParams(name, value);

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
        public DataTable MyDt(string sql, int startindex, int pagesize, string dataname, params IDataParameter[] param) => MyDs(sql, CommandType.Text, startindex, pagesize, dataname, param).Tables[0];

        /// <summary>
        /// ��ȡDataTable�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataTable MyDt(string sql, int startindex, int pagesize, params IDataParameter[] param) => MyDt(sql, startindex, pagesize, "ds", param);

        /// <summary>
        /// ��ȡDataTable�����б�����������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataTable MyDt(string sql, string dataname, params IDataParameter[] param) => MyDs(sql, CommandType.Text, dataname, param).Tables[0];

        /// <summary>
        /// ��ȡDataTable�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataTable MyDt(string sql, params IDataParameter[] param) => MyDt(sql, "ds", param);

        /// <summary>
        /// ��ȡDataView�����б�����ҳ��������������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataView MyDv(string sql, int startindex, int pagesize, string dataname, params IDataParameter[] param) => MyDt(sql, startindex, pagesize, dataname, param).DefaultView;

        /// <summary>
        /// ��ȡDataView�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="param">����</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataView MyDv(string sql, int startindex, int pagesize, params IDataParameter[] param) => MyDv(sql, startindex, pagesize, "ds", param);

        /// <summary>
        /// ��ȡDataView�����б�����������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataView MyDv(string sql, string dataname, params IDataParameter[] param) => MyDt(sql, dataname, param).DefaultView;

        /// <summary>
        /// ��ȡDataView�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataView MyDv(string sql, params IDataParameter[] param) => MyDv(sql, "ds", param);

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
        public DataTable MyDt(string sql, int startindex, int pagesize, string dataname="ds") => MyDt(sql, startindex, pagesize, dataname, null);

        /// <summary>
        /// ��ȡDataTable�����б�����������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataTable MyDt(string sql, string dataname="ds") => MyDt(sql, dataname, null);

        /// <summary>
        /// ��ȡDataView�����б�����ҳ��������������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataView MyDv(string sql, int startindex, int pagesize, string dataname="ds") => MyDv(sql, startindex, pagesize, dataname, null);

        /// <summary>
        /// ��ȡDataView�����б�����������
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataView MyDv(string sql, string dataname="ds") => MyDv(sql, dataname, null);

        #endregion

        public void Dispose()
        {
            
        }
    }
    #endregion

}
