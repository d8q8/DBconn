using System;
using System.Collections;
using System.Data;
using System.Data.OleDb;

namespace DBconn
{
    public sealed class Access : ISDb, IDisposable
    {
        private OleDbConnection _connSql;
        private readonly string _dataSql;
        private bool _disposed;
        /// <summary>
        /// ��ʼ��Access���ݿ�
        /// </summary>
        /// <param name="connstr"></param>
        public Access(string connstr)
        {
            _dataSql = connstr;
        }
        /// <summary>
        /// ����ת��
        /// </summary>
        /// <param name="name">����</param>
        /// <param name="value">ֵ</param>
        /// <returns></returns>
        public IDataParameter MyParams(string name, object value)
        {
            return new OleDbParameter(name, value);
        }

        #region �������ݿ����
        /// <summary>
        /// �����ݿ�
        /// </summary>
        public void Open()
        {
            _connSql = new OleDbConnection(_dataSql);
            if (_connSql.State == ConnectionState.Closed)
            {
                _connSql.Open();
            }
        }
        /// <summary>
        /// �ر����ݿⲢ�ͷ���Դ
        /// </summary>
        public void Close()
        {
            _connSql.Close();
            _connSql.Dispose();
            _connSql = null;
        }
        /// <summary>
        /// ��������
        /// </summary>
        ~Access()
        {
            Dispose(false);
        }
        /// <summary>
        /// ϵͳ����
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// �ж��Ƿ��ͷ�
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    if (_connSql != null && _connSql.State == ConnectionState.Open)
                    {
                        try
                        {
                            Close();
                        }
                        catch
                        {
                            //throw new Exception(e.Message);
                        }
                    }
                }
            }
            _disposed = true; 
        }
        #endregion

        #region ����������װ��

        /// <summary>
        /// ��ȡDataSet�����б�����ҳ��
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="startindex">ҳ�洫�ݲ���</param>
        /// <param name="pagesize">ÿҳ�����¼��</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>���ش���ҳ�Զ����ڴ��</returns>
        public DataSet GetDataSet(string sql, CommandType ctype, int startindex, int pagesize, string dataname)
        {
            return GetDataSet(sql, ctype, startindex, pagesize, dataname, null);
        }

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet GetDataSet(string sql, CommandType ctype, string dataname)
        {
            return GetDataSet(sql, ctype, dataname, null);
        }

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        public int GetExecuteNonQuery(string sql, CommandType ctype)
        {
            return GetExecuteNonQuery(sql, ctype, null);
        }

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>����object����</returns>
        public object GetExecuteScalar(string sql, CommandType ctype)
        {
            return GetExecuteScalar(sql, ctype, null);
        }

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        public IDataReader GetDataReader(string sql, CommandType ctype)
        {
            return GetDataReader(sql, ctype, null);
        }

        #endregion

        #region ��������װ��

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
        public DataSet GetDataSet(string sql, CommandType ctype, int startindex, int pagesize, string dataname, params IDataParameter[] param)
        {
            Open();
            var cmd = new OleDbCommand();
            PrepareCommand(cmd, _connSql, null, ctype, sql, param);
            using (var dap = new OleDbDataAdapter(cmd))
            {
                var ds = new DataSet();
                try
                {
                    ds.Clear();
                    dap.Fill(ds, (startindex - 1) * pagesize, pagesize, dataname);
                    dap.Dispose();
                    cmd.Parameters.Clear();
                    cmd.Dispose();
                    return ds;
                }
                catch (OleDbException ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// ��ȡDataSet�����б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="dataname">�ڴ��</param>
        /// <param name="param">����</param>
        /// <returns>�����Զ����ڴ��</returns>
        public DataSet GetDataSet(string sql, CommandType ctype, string dataname, params IDataParameter[] param)
        {
            Open();
            var cmd = new OleDbCommand();
            PrepareCommand(cmd, _connSql, null, ctype, sql, param);
            using (var dap = new OleDbDataAdapter(cmd))
            {
                var ds = new DataSet();
                try
                {
                    dap.Fill(ds, dataname);
                    dap.Dispose();
                    cmd.Parameters.Clear();
                    cmd.Dispose();
                    return ds;
                }
                catch (OleDbException ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// ִ��sql���
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        public int GetExecuteNonQuery(string sql, CommandType ctype, params IDataParameter[] param)
        {
            Open();
            int i;
            var cmd = new OleDbCommand();
            try
            {
                PrepareCommand(cmd, _connSql, null, ctype, sql, param);
                i = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                cmd.Dispose();
            }
            finally
            {
                Close();
            }
            return i;
        }

        /// <summary>
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object����
        /// </summary>
        /// <param name="sql">�����ѯ������</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>��ѯ�����object��</returns>
        public object GetExecuteScalar(string sql, CommandType ctype, params IDataParameter[] param)
        {
            Open();
            var cmd = new OleDbCommand();
            try
            {
                PrepareCommand(cmd, _connSql, null, ctype, sql, param);
                var obj = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                cmd.Dispose();
                return Equals(obj, null) || Equals(obj, DBNull.Value) ? null : obj;
            }
            finally
            {
                Close();
            }
        }

        /// <summary>
        /// ��ȡ���ݼ�¼���б�
        /// </summary>
        /// <param name="sql">sql���</param>
        /// <param name="ctype">����</param>
        /// <param name="param">����</param>
        /// <returns>���ؼ�¼���б�</returns>
        public IDataReader GetDataReader(string sql, CommandType ctype, params IDataParameter[] param)
        {
            Open();
            var cmd = new OleDbCommand();
            PrepareCommand(cmd, _connSql, null, ctype, sql, param);
            var dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            cmd.Parameters.Clear();
            cmd.Dispose();
            return dr;
        }

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="cmd">��ʾҪ������Դִ�е�SQL��洢����</param>
        /// <param name="conn">��ʾ������Դ�������Ǵ򿪵�</param>
        /// <param name="trans">��ʾ������Դ��SQL����,���ܱ��̳�</param>
        /// <param name="cmdType">ָ����ν��������ַ���</param>
        /// <param name="cmdText">�ַ���</param>
        /// <param name="cmdParms">����</param>
        private static void PrepareCommand(OleDbCommand cmd, OleDbConnection conn, OleDbTransaction trans, CommandType cmdType, string cmdText, params IDataParameter[] cmdParms)
        {
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = cmdType;
            if (cmdParms == null) return;
            foreach (var parameter in cmdParms)
            {
                if ((parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Input) &&
                    (parameter.Value == null))
                {
                    parameter.Value = DBNull.Value;
                }
                cmd.Parameters.Add(parameter);
            }
        }

        // ����Cache�����Hashtable����
        private readonly Hashtable _parmCache = Hashtable.Synchronized(new Hashtable());
        /// <summary>
        /// �ڻ�������Ӳ�������
        /// </summary>
        /// <param name="cacheKey">������Key</param>
        /// <param name="cmdParms">��������</param>
        public void CacheParameters(string cacheKey, params IDataParameter[] cmdParms)
        {
            _parmCache[cacheKey] = cmdParms;
        }

        /// <summary>
        /// ��ȡ����Ĳ�������
        /// </summary>
        /// <param name="cacheKey">���һ����key</param>
        /// <returns>���ر�����Ĳ�������</returns>
        public IDataParameter[] GetCachedParameters(string cacheKey)
        {
            var cachedParms = (OleDbParameter[])_parmCache[cacheKey];
            if (cachedParms == null) return null;
            var clonedParms = new OleDbParameter[cachedParms.Length];
            for (int i = 0, j = cachedParms.Length; i < j; i++)
                clonedParms[i] = (OleDbParameter)((ICloneable)cachedParms[i]).Clone();
            return clonedParms;
        }

        #endregion

    }
}
