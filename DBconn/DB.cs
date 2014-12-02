using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DBconn
{

    #region 默认获取数据库引用类
    /// <summary>
    /// 调用数据库，默认为access数据库
    /// </summary>
    public sealed class Db
    {
        #region 判断当前使用数据库及类型，默认为ACCESS2003

        public static Conn GetConn(string connstr)
        {
            return GetConn(MyType.Access2003, connstr);
        }

        public static Conn GetConn(MyType mymt = MyType.Access2003, string connstr = "access")
        {
            return new Conn(mymt, connstr);
        }
        #endregion

        #region 初始化参数
        /// <summary>
        /// SQL语句增、删、改、查、条件、排序组合
        /// </summary>
        public static string SelectState = "Select {1} From {0}";
        public static string InsertState = "Insert Into {0}({1}) Values({2})";
        public static string UpdateState = "Update {0} Set {1}";
        public static string DeleteState = "Delete From {0}";
        public static string WhereState = " Where {0} ";
        public static string OrderByState = " Order By {0} Desc";
        #endregion

        #region 自动生成SQL语句

        /// <summary>
        /// 插入SQL语句
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fields">字段</param>
        /// <returns></returns>
        private static string InsertSql(string tableName, string fields)
        {
            var sql = new StringBuilder();
            var sqlfields = fields;
            var sqlparams = string.Format("@{0}", fields.Contains(",") ? fields.Replace(",", "@,") : fields);
            sql.AppendFormat(SelectState, tableName, sqlfields, sqlparams);
            return sql.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="entity"></param>
        private static void Params(string fields, object entity)
        {
            var c = new Conn();
            if (!fields.Contains(",")) return;
            foreach (var f in fields.Split(','))
            {
                var p = entity.GetType().GetProperty(f);
                if (String.Equals(f, p.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    c.AddParam(f, p.GetValue(entity, null));
                }
            }
        }
        /// <summary>
        /// 更新SQL语句
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private static string UpdateSql(object entity)
        {
            var sql = new StringBuilder();

            return sql.ToString();
        }
        #endregion

        #region 转化实体类对象
        /// <summary>
        /// Reader转Object，从数据库字段转化为实体类对象
        /// </summary>
        /// <param name="reader">数据库记录集</param>
        /// <param name="targetObj">实体类对象</param>
        public static void ReaderToObject(IDataReader reader, object targetObj)
        {
            using (reader)
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var propertyInfo = targetObj.GetType().GetProperty(reader.GetName(i));
                    if (propertyInfo == null) continue;
                    if (reader.GetValue(i) == DBNull.Value) continue;
                    propertyInfo.SetValue(targetObj,
                        propertyInfo.PropertyType.IsEnum
                            ? Enum.ToObject(propertyInfo.PropertyType, reader.GetValue(i))
                            : reader.GetValue(i), null);
                }
            }
        }
        /// <summary>
        /// DataSet转Object
        /// </summary>
        /// <param name="ds">DataSet对象</param>
        /// <param name="targetObj">实体类对象</param>
        public static void DataSetToObject(DataSet ds, object targetObj)
        {
            DataTableToObject(ds.Tables[0], targetObj);
        }
        /// <summary>
        /// DataTable转Object
        /// </summary>
        /// <param name="dt">DataTable对象</param>
        /// <param name="targetObj">实体类对象</param>
        public static void DataTableToObject(DataTable dt, object targetObj)
        {
            foreach (DataRow dr in dt.Rows)
            {
                var propertys = targetObj.GetType().GetProperties();
                foreach (var pi in propertys)
                {
                    var tempName = pi.Name;
                    // 检查DataTable是否包含此列
                    if (!dt.Columns.Contains(tempName)) continue;
                    // 判断此属性是否有Setter
                    if (!pi.CanWrite) continue;
                    var value = dr[tempName];
                    if (value != DBNull.Value)
                    {
                        pi.SetValue(targetObj, value, null);
                    }
                }
            }
        }
        #endregion

        #region 泛型集合与DataSet互相转换
        /// <summary>    
        /// 集合装换DataSet    
        /// </summary>    
        /// <param name="pList">集合</param>    
        /// <returns></returns>      
        public static DataSet ToDataSet(IList pList)
        {
            var result = new DataSet();
            var dataTable = new DataTable();
            if (pList.Count > 0)
            {
                var propertys = pList[0].GetType().GetProperties();
                foreach (var pi in propertys)
                {
                    dataTable.Columns.Add(pi.Name, pi.PropertyType);
                }

                foreach (var array in from object t in pList select propertys.Select(pi => pi.GetValue(t, null)).ToArray())
                {
                    dataTable.LoadDataRow(array, true);
                }
            }
            result.Tables.Add(dataTable);
            return result;
        }

        /// <summary>    
        /// 泛型集合转换DataSet    
        /// </summary>    
        /// <typeparam name="T"></typeparam>    
        /// <param name="list">泛型集合</param>    
        /// <returns></returns>      
        public static DataSet ToDataSet<T>(IList<T> list)
        {
            return ToDataSet(list, null);
        }

        /// <summary>    
        /// 泛型集合转换DataSet    
        /// </summary>    
        /// <typeparam name="T"></typeparam>    
        /// <param name="pList">泛型集合</param>    
        /// <param name="pPropertyName">待转换属性名数组</param>    
        /// <returns></returns>    
        public static DataSet ToDataSet<T>(IList<T> pList, params string[] pPropertyName)
        {
            var propertyNameList = new List<string>();
            if (pPropertyName != null)
                propertyNameList.AddRange(pPropertyName);

            var result = new DataSet();
            var dataTable = new DataTable();
            if (pList.Count > 0)
            {
                var propertys = pList[0].GetType().GetProperties();
                foreach (var pi in propertys)
                {
                    if (propertyNameList.Count == 0)
                    {
                        // 没有指定属性的情况下全部属性都要转换
                        dataTable.Columns.Add(pi.Name, pi.PropertyType);
                    }
                    else
                    {
                        if (propertyNameList.Contains(pi.Name))
                            dataTable.Columns.Add(pi.Name, pi.PropertyType);
                    }
                }

                foreach (var t in pList)
                {
                    var tempList = new List<object>();
                    foreach (var pi in propertys)
                    {
                        if (propertyNameList.Count == 0)
                        {
                            var obj = pi.GetValue(t, null);
                            tempList.Add(obj);
                        }
                        else
                        {
                            if (!propertyNameList.Contains(pi.Name)) continue;
                            var obj = pi.GetValue(t, null);
                            tempList.Add(obj);
                        }
                    }
                    var array = tempList.ToArray();
                    dataTable.LoadDataRow(array, true);
                }
            }
            result.Tables.Add(dataTable);
            return result;
        }

        /// <summary>    
        /// DataSet转换为泛型集合    
        /// </summary>    
        /// <typeparam name="T"></typeparam>    
        /// <param name="pDataSet">DataSet</param>    
        /// <param name="pTableIndex">待转换数据表索引</param>    
        /// <returns></returns>     
        public static IList<T> DataSetToIList<T>(DataSet pDataSet, int pTableIndex)
        {
            if (pDataSet == null || pDataSet.Tables.Count < 0)
                return null;
            if (pTableIndex > pDataSet.Tables.Count - 1)
                return null;
            if (pTableIndex < 0)
                pTableIndex = 0;

            var pData = pDataSet.Tables[pTableIndex];
            // 返回值初始化    
            IList<T> result = new List<T>();
            for (var j = 0; j < pData.Rows.Count; j++)
            {
                var t = (T)Activator.CreateInstance(typeof(T));
                var propertys = t.GetType().GetProperties();
                foreach (var pi in propertys)
                {
                    for (var i = 0; i < pData.Columns.Count; i++)
                    {
                        // 属性与字段名称一致的进行赋值
                        if (!pi.Name.Equals(pData.Columns[i].ColumnName)) continue;
                        // 数据库NULL值单独处理    
                        pi.SetValue(t, pData.Rows[j][i] != DBNull.Value ? pData.Rows[j][i] : null, null);
                        break;
                    }
                }
                result.Add(t);
            }
            return result;
        }

        /// <summary>    
        /// DataSet转换为泛型集合    
        /// </summary>    
        /// <typeparam name="T"></typeparam>    
        /// <param name="pDataSet">DataSet</param>    
        /// <param name="pTableName">待转换数据表名称</param>    
        /// <returns></returns>    
        public static IList<T> DataSetToIList<T>(DataSet pDataSet, string pTableName)
        {
            var tableIndex = 0;
            if (pDataSet == null || pDataSet.Tables.Count < 0)
                return null;
            if (string.IsNullOrEmpty(pTableName))
                return null;
            for (var i = 0; i < pDataSet.Tables.Count; i++)
            {
                // 获取Table名称在Tables集合中的索引值    
                if (!pDataSet.Tables[i].TableName.Equals(pTableName)) continue;
                tableIndex = i;
                break;
            }
            return DataSetToIList<T>(pDataSet, tableIndex);
        }
        #endregion

        #region DataReader转换成实体（或List）
        /// <summary>   
        /// DataReader转换为obj list   
        /// </summary>   
        /// <typeparam name="T">泛型</typeparam>   
        /// <param name="rdr">数据记录集</param>   
        /// <returns>返回泛型类型</returns>   
        public static IList<T> DataReaderToList<T>(IDataReader rdr)
        {
            IList<T> list = new List<T>();
            using (rdr)
            {
                while (rdr.Read())
                {
                    var t = (T)DataReaderToObj<T>(rdr);
                    if (!Equals(t,null))
                    {
                        list.Add(t);
                    }
                }
                return list;
            }

        }

        /// <summary>   
        /// DataReader转换为Obj
        /// </summary>   
        /// <typeparam name="T">泛型</typeparam>   
        /// <param name="rdr">datareader</param>   
        /// <returns>返回泛型类型</returns>   
        public static object DataReaderToObj<T>(IDataReader rdr)
        {
            var t = Activator.CreateInstance<T>();
            var obj = t.GetType();
            using (rdr)
            {
                if (rdr.Read())
                {
                    for (var i = 0; i < rdr.FieldCount; i++)
                    {
                        if (rdr.IsDBNull(i)) continue;
                        var tempValue = rdr.GetValue(i);
                        obj.GetProperty(rdr.GetName(i)).SetValue(t, tempValue, null);
                    }
                    return t;
                }
                else
                    return null;
            }
        }

        #endregion

        #region 数据转化为JSON类型
        /// <summary>
        /// DataTable转成Json 
        /// </summary>
        /// <param name="jsonName"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string DataTableToJson(string jsonName, DataTable dt)
        {
            var json = new StringBuilder();
            json.Append("{\"" + jsonName + "\":[");
            if (dt.Rows.Count > 0)
            {
                for (var i = 0; i < dt.Rows.Count; i++)
                {
                    json.Append("{");
                    for (var j = 0; j < dt.Columns.Count; j++)
                    {
                        json.AppendFormat("\"{0}\":\"{1}\"", dt.Columns[j].ColumnName, dt.Rows[i][j]);
                        if (j < dt.Columns.Count - 1)
                        {
                            json.Append(",");
                        }
                    }
                    json.Append("}");
                    if (i < dt.Rows.Count - 1)
                    {
                        json.Append(",");
                    }
                }
            }
            json.Append("]}");
            return json.ToString();
        }

        /// <summary>
        /// List转成json 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonName"></param>
        /// <param name="il"></param>
        /// <returns></returns>
        public static string ObjectToJson<T>(string jsonName, IList<T> il)
        {
            var json = new StringBuilder();
            json.Append("{\"" + jsonName + "\":[");
            if (il.Count > 0)
            {
                for (var i = 0; i < il.Count; i++)
                {
                    var obj = Activator.CreateInstance<T>();
                    var type = obj.GetType();
                    var pis = type.GetProperties();
                    json.Append("{");
                    for (var j = 0; j < pis.Length; j++)
                    {
                        json.AppendFormat("\"{0}\":\"{1}\"", pis[j].Name, pis[j].GetValue(il[i], null));
                        if (j < pis.Length - 1)
                        {
                            json.Append(",");
                        }
                    }
                    json.Append("}");
                    if (i < il.Count - 1)
                    {
                        json.Append(",");
                    }
                }
            }
            json.Append("]}");
            return json.ToString();
        }

        /// <summary> 
        /// 对象转换为Json字符串 
        /// </summary> 
        /// <param name="jsonObject">对象</param> 
        /// <returns>Json字符串</returns> 
        public static string ToJson(object jsonObject)
        {
            var jsonString = "{";
            var propertyInfo = jsonObject.GetType().GetProperties();
            foreach (var t in propertyInfo)
            {
                var objectValue = t.GetGetMethod().Invoke(jsonObject, null);
                string value;
                if (objectValue is DateTime || objectValue is Guid || objectValue is TimeSpan)
                {
                    value = "'" + objectValue + "'";
                }
                else if (objectValue is string)
                {
                    value = "'" + ToJson(objectValue.ToString()) + "'";
                }
                else
                {
                    var o = objectValue as IEnumerable;
                    value = o != null ? ToJson(o) : ToJson(objectValue.ToString());
                }
                jsonString += "\"" + ToJson(t.Name) + "\":" + value + ",";
            }
            return DeleteLast(jsonString) + "}";
        }
        /// <summary> 
        /// 对象集合转换Json 
        /// </summary> 
        /// <param name="array">集合对象</param> 
        /// <returns>Json字符串</returns> 
        public static string ToJson(IEnumerable array)
        {
            var jsonString = array.Cast<object>().Aggregate("[", (current, item) => current + (ToJson(item) + ","));
            return DeleteLast(jsonString) + "]";
        }
        /// <summary> 
        /// 普通集合转换Json 
        /// </summary> 
        /// <param name="array">集合对象</param> 
        /// <returns>Json字符串</returns> 
        public static string ToArrayString(IEnumerable array)
        {
            var jsonString = "[";
            foreach (var item in array)
            {
                jsonString = ToJson(item.ToString()) + ",";
            }
            return DeleteLast(jsonString) + "]";
        }
        /// <summary> 
        /// 删除结尾字符 
        /// </summary> 
        /// <param name="str">需要删除的字符</param> 
        /// <returns>完成后的字符串</returns> 
        private static string DeleteLast(string str)
        {
            if (str.Length > 1)
            {
                return str.Substring(0, str.Length - 1);
            }
            return str;
        }
        /// <summary> 
        /// Datatable转换为Json 
        /// </summary> 
        /// <param name="table">Datatable对象</param> 
        /// <returns>Json字符串</returns> 
        public static string ToJson(DataTable table)
        {
            string jsonString = "[";
            DataRowCollection drc = table.Rows;
            for (int i = 0; i < drc.Count; i++)
            {
                jsonString += "{";
                foreach (DataColumn column in table.Columns)
                {
                    jsonString += "\"" + ToJson(column.ColumnName) + "\":";
                    if (column.DataType == typeof(DateTime) || column.DataType == typeof(string))
                    {
                        jsonString += "\"" + ToJson(drc[i][column.ColumnName].ToString()) + "\",";
                    }
                    else
                    {
                        jsonString += ToJson(drc[i][column.ColumnName].ToString()) + ",";
                    }
                }
                jsonString = DeleteLast(jsonString) + "},";
            }
            return DeleteLast(jsonString) + "]";
        }
        /// <summary> 
        /// DataReader转换为Json 
        /// </summary> 
        /// <param name="dataReader">DataReader对象</param> 
        /// <returns>Json字符串</returns> 
        public static string ToJson(IDataReader dataReader)
        {
            var jsonString = "[";
            while (dataReader.Read())
            {
                jsonString += "{";

                for (var i = 0; i < dataReader.FieldCount; i++)
                {
                    jsonString += "\"" + ToJson(dataReader.GetName(i)) + "\":";
                    if (dataReader.GetFieldType(i) == typeof(DateTime) || dataReader.GetFieldType(i) == typeof(string))
                    {
                        jsonString += "\"" + ToJson(dataReader[i].ToString()) + "\",";
                    }
                    else
                    {
                        jsonString += ToJson(dataReader[i].ToString()) + ",";
                    }
                }
                jsonString = DeleteLast(jsonString) + "}";
            }
            dataReader.Close();
            return DeleteLast(jsonString) + "]";
        }
        /// <summary> 
        /// DataSet转换为Json 
        /// </summary> 
        /// <param name="dataSet">DataSet对象</param> 
        /// <returns>Json字符串</returns> 
        public static string ToJson(DataSet dataSet)
        {
            var jsonString = dataSet.Tables.Cast<DataTable>().Aggregate("{", (current, table) => current + ("\"" + ToJson(table.TableName) + "\":" + ToJson(table) + ","));
            return DeleteLast(jsonString) + "}";
        }
        /// <summary> 
        /// String转换为Json 
        /// </summary> 
        /// <param name="value">String对象</param> 
        /// <returns>Json字符串</returns> 
        public static string ToJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var temstr = value;
            temstr = temstr.Replace("{", "｛").Replace("}", "｝").Replace(":", "：").Replace(",", "，").Replace("[", "【").Replace("]", "】").Replace(";", "；").Replace("\n", "<br/>").Replace("\r", "");

            temstr = temstr.Replace("\t", "   ");
            temstr = temstr.Replace("'", "\'");
            temstr = temstr.Replace(@"\", @"\\");
            temstr = temstr.Replace("\"", "\"\"");
            return temstr;
        }

        #endregion

        #region 表单转化赋值实体类
        public static T GetPost<T>(string prefix, NameValueCollection form)
        {
            var t = Activator.CreateInstance<T>();
            var pi = t.GetType().GetProperties();//获取属性集合
            foreach (var p in pi.Where(p => form[prefix + p.Name] != null))
            {
                try
                {
                    p.SetValue(t,
                        p.PropertyType.IsGenericType
                            ? Convert.ChangeType(form[prefix + p.Name], p.PropertyType.GetGenericArguments()[0])
                            : Convert.ChangeType(form[prefix + p.Name], p.PropertyType), null);
                }
                catch
                {
                    // ignored
                }
            }
            return t;
        }
        #endregion

        #region 控件转化赋值实体类
        /// <summary>
        /// 根据实体类自动填充数据
        /// </summary>
        /// <param name="obj">实体类</param>
        /// <param name="substr">截取前缀(如txtName,则前缀为txt)</param>
        /// <param name="controls">Page.Controls 页面控件集合</param>
        public static void FillCotrols(object obj, string substr, ControlCollection controls)
        {
            for (var i = 0; i < controls.Count; i++)
            {
                if (controls[i].HasControls())
                {
                    foreach (Control control in controls[i].Controls)
                    {
                        FillContent(obj, substr, control);
                    }
                }
                else
                {
                    FillContent(obj, substr, controls[i]);
                }
            }
        }
        /// <summary>
        /// 根据实体类自动填充数据内容
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="substr"></param>
        /// <param name="control"></param>
        private static void FillContent(object obj, string substr, object control)
        {
            string id;
            PropertyInfo proInfo;

            #region Literal 类型
            //------如果是Literal
            if (control.GetType().Name.Equals(typeof(Literal).Name))
            {
                var txt = control as Literal;
                if (txt != null)
                {
                    id = txt.ID;
                    //移除id前缀，如txtName→Name
                    id = id.Replace(substr, "");
                    //获取对应的model层对象的属性信息
                    proInfo = obj.GetType().GetProperty(id);
                    if (proInfo != null)
                    {
                        txt.Text = proInfo.GetValue(obj, null) == null ? "" : proInfo.GetValue(obj, null).ToString();
                    }
                }
            }
            #endregion

            #region TextBox 类型
            //------如果是Textbox
            if (control.GetType().Name.Equals(typeof(TextBox).Name))
            {
                var txt = control as TextBox;
                if (txt != null)
                {
                    id = txt.ID;
                    //移除id前缀，如txtName→Name
                    id = id.Replace(substr, "");
                    //获取对应的model层对象的属性信息
                    proInfo = obj.GetType().GetProperty(id);
                    if (proInfo != null)
                    {
                        txt.Text = proInfo.GetValue(obj, null) == null ? "" : proInfo.GetValue(obj, null).ToString();
                    }
                }
            }
            #endregion

            #region RadioButton 类型
            //-------如果是RadioButton,则取radiobutton 的groupname.
            if (control.GetType().Name.Equals(typeof(RadioButton).Name))
            {
                var rdo = control as RadioButton;
                if (rdo != null)
                {
                    var groupName = rdo.GroupName;
                    groupName = groupName.Replace(substr, "");
                    var info = obj.GetType().GetProperty(groupName);
                    if (info != null && rdo.Text.Equals(info.GetValue(obj, null).ToString()))
                        rdo.Checked = true;
                }
            }
            #endregion

            #region CheckBox 类型
            //-------如果是CheckBox,则取CheckBox 的groupname.
            if (control.GetType().Name.Equals(typeof(CheckBox).Name))
            {
                var cbox = control as CheckBox;
                if (cbox != null)
                {
                    id = cbox.ID;
                    id = id.Replace(substr, "");
                    var info = obj.GetType().GetProperty(id);
                    if (info != null && cbox.Text.Equals(info.GetValue(obj, null).ToString()))
                        cbox.Checked = true;
                }
            }
            #endregion

            #region DropDownList 类型
            //如果是DropDownList
            if (!control.GetType().Name.Equals(typeof(DropDownList).Name)) return;
            var ddl = control as DropDownList;
            if (ddl == null) return;
            id = ddl.ID;
            id = id.Replace(substr, "");
            proInfo = obj.GetType().GetProperty(id);
            if (proInfo != null)
                ddl.SelectedValue = proInfo.GetValue(obj, null) == null ? "0" : proInfo.GetValue(obj, null).ToString();

            #endregion

        }
        /// <summary>
        /// 根据页面数据自动填充到实体类
        /// </summary>
        /// <param name="obj">实体类</param>
        /// <param name="substr">截取前缀</param>
        /// <param name="controls">Page.Controls</param>
        public static void ReadControls(object obj, string substr, ControlCollection controls)
        {
            for (var i = 0; i < controls.Count; i++)
            {
                if (controls[i].HasControls())
                {
                    foreach (Control control in controls[i].Controls)
                    {
                        ReadContent(obj, substr, control);
                    }
                }
                else
                {
                    ReadContent(obj, substr, controls[i]);
                }

            }
        }
        /// <summary>
        /// 根据页面数据自动填充实体类内容
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="substr"></param>
        /// <param name="control"></param>
        private static void ReadContent(object obj, string substr, object control)
        {
            string objId = null;
            string objValue = null;
            PropertyInfo info;

            #region Literal 类型
            //如果是Literal类型
            if (control.GetType().Name.Equals(typeof(Literal).Name))
            {
                var txt = control as Literal;
                //获取对应实体类的属性名
                if (txt != null)
                {
                    objId = txt.ID.Replace(substr, "");
                    //获取对应实体类该属性的值
                    objValue = txt.Text;
                }

                if (objId != null)
                {
                    info = obj.GetType().GetProperty(objId);
                    if (info != null)
                    {
                        if (info.PropertyType.Name == "String")
                        {
                            if (objValue != null) info.SetValue(obj, objValue, null);
                        }
                        else
                        {
                            objValue = objValue == "" ? "0" : objValue;
                            info.SetValue(obj, Convert.ToInt32(objValue), null);
                        }
                    }
                }
            }
            #endregion

            #region TextBox 类型
            //如果是TexBox类型
            if (control.GetType().Name.Equals(typeof(TextBox).Name))
            {
                var txt = control as TextBox;
                //获取对应实体类的属性名
                if (txt != null)
                {
                    objId = txt.ID.Replace(substr, "");
                    //获取对应实体类该属性的值
                    objValue = txt.Text;
                }

                if (objId != null)
                {
                    info = obj.GetType().GetProperty(objId);
                    if (info != null)
                    {
                        if (info.PropertyType.Name == "String")
                            info.SetValue(obj, objValue, null);
                        else
                        {
                            objValue = objValue == "" ? "0" : objValue;
                            info.SetValue(obj, Convert.ToInt32(objValue), null);
                        }
                    }
                }
            }
            #endregion

            #region RadioButton 类型
            //如果是RadioButton
            if (control.GetType().Name.Equals(typeof(RadioButton).Name))
            {
                var rdo = control as RadioButton;
                if (rdo != null)
                {
                    objId = rdo.GroupName.Replace(substr, "");
                    objValue = rdo.Text;
                    if (rdo.Checked)
                    {
                        info = obj.GetType().GetProperty(objId);
                        if (info != null)
                        {
                            //如果属性的类型为string
                            if (info.PropertyType.Name == "String")
                                info.SetValue(obj, objValue, null);
                            else
                            {
                                info.SetValue(obj, Convert.ToInt32(objValue), null);
                            }
                        }
                    }
                }
            }
            #endregion

            #region CheckBox 类型
            //如果是CheckBox
            if (control.GetType().Name.Equals(typeof(CheckBox).Name))
            {
                var cbox = control as CheckBox;
                if (cbox != null)
                {
                    objId = cbox.ID.Replace(substr, "");
                    objValue = cbox.Checked ? "1" : "0";
                    if (cbox.Checked)
                    {
                        info = obj.GetType().GetProperty(objId);
                        if (info != null)
                        {
                            //如果属性的类型为string
                            if (info.PropertyType.Name == "String")
                                info.SetValue(obj, objValue, null);
                            else
                            {
                                info.SetValue(obj, Convert.ToInt32(objValue), null);
                            }
                        }
                    }
                }
            }
            #endregion

            #region DropDownList 类型
            //如果是下拉列表DropDownList
            if (!control.GetType().Name.Equals(typeof (DropDownList).Name)) return;
            var ddl = control as DropDownList;
            if (ddl != null)
            {
                objId = ddl.ID.Replace(substr, "");
                objValue = ddl.SelectedValue;
            }
            if (objId == null) return;
            info = obj.GetType().GetProperty(objId);
            if (info == null) return;
            //如果属性的类型为string
            if (info.PropertyType.Name == "String")
                info.SetValue(obj, objValue, null);
            else
            {
                info.SetValue(obj, Convert.ToInt32(objValue), null);
            }

            #endregion

        }
        #endregion

        #region 插入与更新数据
        ///// <summary>   
        ///// 通过泛型插入数据   
        ///// </summary>   
        ///// <typeparam name="T">类名称</typeparam>   
        ///// <param name="obj">类对象,如果要插入空值，请使用@NULL</param>   
        ///// <returns>插入的新记录ID</returns>   
        //public static int Insert<T>(Conn db, T obj)
        //{
        //    StringBuilder strSQL = new StringBuilder();
        //    strSQL = GetInsertSQL(obj);
        //    // 插入到数据库中   
        //    return db.MyExec(strSQL.ToString());
        //}

        ///// <summary>   
        ///// 通过泛型更新数据   
        ///// </summary>   
        ///// <typeparam name="T">类名称</typeparam>   
        ///// <param name="obj">类对象,如果要更新空值，请使用@NULL</param>   
        ///// <returns>更新结果,大于0为更新成功</returns>   
        //public static int Update<T>(Conn db, T obj)
        //{
        //    StringBuilder strSQL = new StringBuilder();
        //    strSQL = GetUpdateSQL(obj);
        //    if (String.IsNullOrEmpty(strSQL.ToString()))
        //    {
        //        return 0;
        //    }
        //    // 更新到数据库中   
        //    return db.MyExec(strSQL.ToString());
        //}

        ///// <summary>   
        ///// 获取实体的插入语句   
        ///// </summary>   
        ///// <typeparam name="T">泛型</typeparam>   
        ///// <param name="obj">实体对象</param>   
        ///// <returns>返回插入语句</returns>   
        //public static StringBuilder GetInsertSQL<T>(T obj)
        //{
        //    string tableKey = GetPropertyValue(obj, BaseSet.PrimaryKey);
        //    string keyValue = GetPropertyValue(obj, tableKey);
        //    string tableName = GetPropertyValue(obj, BaseSet.TableName);
        //    Type t = obj.GetType();//获得该类的Type   
        //    StringBuilder strSQL = new StringBuilder();
        //    strSQL.Append("insert into " + tableName + "(");
        //    string fields = "";
        //    string values = "";
        //    //再用Type.GetProperties获得PropertyInfo[]   
        //    foreach (PropertyInfo pi in t.GetProperties())
        //    {
        //        object name = pi.Name;//用pi.GetValue获得值   
        //        // 替换Sql注入符   
        //        string value1 = Convert.ToString(pi.GetValue(obj, null)).Replace("'", "''");
        //        //string dataType = pi.PropertyType.ToString().ToLower();   
        //        string properName = name.ToString().ToLower();
        //        if (!string.IsNullOrEmpty(value1) && properName != tableKey.ToLower() && properName != BaseSet.PrimaryKey.ToLower() && properName != BaseSet.TableName.ToLower() && value1 != BaseSet.DateTimeLongNull && value1 != BaseSet.DateTimeShortNull)
        //        {
        //            // 判断是否为空   
        //            if (value1 == BaseSet.NULL)
        //            {
        //                value1 = "";
        //            }
        //            fields += Convert.ToString(name) + ",";
        //            values += "'" + value1 + "',";
        //        }
        //    }
        //    // 去掉最后一个,   
        //    fields = fields.TrimEnd(',');
        //    values = values.TrimEnd(',');
        //    // 拼接Sql串   
        //    strSQL.Append(fields);
        //    strSQL.Append(") values (");
        //    strSQL.Append(values);
        //    strSQL.Append(")");
        //    strSQL.Append(";SELECT @@IDENTITY;");
        //    return strSQL;
        //}

        ///// <summary>   
        ///// 获取实体的更新SQL串   
        ///// </summary>   
        ///// <typeparam name="T">泛型</typeparam>   
        ///// <param name="obj">实体对象</param>   
        ///// <returns>返回插入语句</returns>   
        //private static StringBuilder GetUpdateSQL<T>(T obj)
        //{

        //    string tableKey = GetPropertyValue(obj, BaseSet.PrimaryKey);
        //    string keyValue = GetPropertyValue(obj, tableKey);
        //    string tableName = GetPropertyValue(obj, BaseSet.TableName);
        //    StringBuilder strSQL = new StringBuilder();
        //    if (string.IsNullOrEmpty(keyValue))
        //    {
        //        return strSQL;
        //    }
        //    Type t = obj.GetType();//获得该类的Type   
        //    strSQL.Append("update " + tableName + " set ");
        //    string subSQL = "";
        //    string condition = " where " + tableKey + "='" + keyValue.Replace("'", "''") + "'";
        //    //再用Type.GetProperties获得PropertyInfo[]   
        //    foreach (PropertyInfo pi in t.GetProperties())
        //    {
        //        object name = pi.Name;//用pi.GetValue获得值   
        //        // 替换Sql注入符   
        //        string value1 = Convert.ToString(pi.GetValue(obj, null)).Replace("'", "''");
        //        //string dataType = pi.PropertyType.ToString().ToLower();   
        //        string properName = name.ToString().ToLower();
        //        if (!string.IsNullOrEmpty(value1) && properName != tableKey.ToLower() && properName != BaseSet.PrimaryKey.ToLower() && properName != BaseSet.TableName.ToLower() && value1 != BaseSet.DateTimeLongNull && value1 != BaseSet.DateTimeShortNull)
        //        {
        //            // 判断是否为空   
        //            if (value1 == BaseSet.NULL)
        //            {
        //                value1 = "";
        //            }
        //            subSQL += Convert.ToString(name) + "='" + value1 + "',";
        //        }
        //    }
        //    subSQL = subSQL.TrimEnd(',');
        //    strSQL.Append(subSQL);
        //    strSQL.Append(condition);
        //    return strSQL;
        //}
        #endregion

    }
    #endregion

    #region 数值判断
    public class BaseSet
    {
        public static string GetVal<T>(T t)
        {
            var tempstr = string.Empty;
            var pi = t.GetType().GetProperties();
            foreach (var p in pi)
            {
                switch (p.PropertyType.ToString())
                {
                    case "System.String":
                        tempstr = Null;
                        break;
                    case "System.Int32":
                        tempstr = IntNull;
                        break;
                    case "System.DateTime":
                        tempstr = DateTimeLongNull;
                        break;
                    case "System.Decimal":
                        tempstr = IntNull;
                        break;
                }
            }

            return tempstr;
        }

        public static string Null => "";
        public static string IntNull => "0";
        public static string BoolNull => "false";
        public static string DateTimeShortNull => "0001-1-1 0:00:00";
        public static string DateTimeWin7ShortNull => "0001/1/1 0:00:00";
        public static string DateTimeWin7LongNull => "0001/01/01 00:00:00";
        public static string DateTimeLongNull => "0001-01-01 00:00:00";
        public static string PrimaryKey => "PrimaryKey";
        public static string TableName => "TableName";
    }
    #endregion

}
