using IBM.Data.DB2;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Service.Domain;
using Easyman.Service.Common;

namespace Easyman.Service.Server
{
    public class ExtScriptHelper
    {

        //当前链接的数据库
        public DBServer dbServer = null;

        public bool _isRun = true;//默认为【运行中】

        public long? nodeCaseID = null;//当前节点实例ID

        public string errMsg = "";

        public DateTime runData = DateTime.Now;

        /// <summary>
        /// 下载文件的临时文件夹
        /// </summary>
        public string _downDbPath = System.AppDomain.CurrentDomain.BaseDirectory + "\\UpFiles\\DownDB\\";

        //public static readonly string SqlSelectIsTableExists = "SELECT 1 A FROM ALL_TABLES WHERE TABLE_NAME = {1}p_table";
        /// <summary>
        /// 判断表是否存在:{0}schema,{1}表名
        /// </summary>
        public static readonly string SqlSelectIsTableWithOwnerExists = "SELECT COUNT(*) FROM ALL_TABLES WHERE TABLE_NAME = '{1}' AND OWNER = '{0}'";

        //public static readonly string SqlSelectIsTableExistsDB2 = "SELECT 1 A FROM SYSCAT.TABLES WHERE TABNAME = {1}p_table WITH UR";
        /// <summary>
        /// 判断表是否存在:{0}schema,{1}表名
        /// </summary>
        public static readonly string SqlSelectIsTableWithOwnerExistsDB2 = "SELECT COUNT(*) FROM SYSCAT.TABLES WHERE TABNAME = '{1}' AND TABSCHEMA = '{0}' WITH UR";

        //public static readonly string SqlSelectIsIndexExists = "SELECT 1 A FROM ALL_INDEXES WHERE INDEX_NAME = {1}p_index";

        //public static readonly string SqlSelectIsIndexWithOwnerExists = "SELECT 1 FROM ALL_INDEXES WHERE INDEX_NAME = {1}p_index AND OWNER = {1}p_owner";

        //public static readonly string SqlSelectIsIndexExistsDB2 = "SELECT 1 FROM SYSCAT.INDEXES WHERE INDNAME = {1}p_index";

        //public static readonly string SqlSelectIsIndexWithOwnerExistsDB2 = "SELECT 1 FROM SYSCAT.INDEXES WHERE INDNAME = {1}p_index AND INDSCHEMA = {1}p_owner";


        /// <summary>
        /// 初始化节点实例相关数据
        /// </summary>
        /// <param name="nCaseID">节点实例ID</param>
        public void Initialize(long? nCaseID)
        {
            ErrorInfo err = new ErrorInfo();
            //将节点实例从【等待】修改为【执行中】
            ScriptManager.ModifyScriptNodeCase(nCaseID, PubEnum.RunStatus.Excute, ref err);

            //设置实例为【运行】状态
            if (err.IsError)
            {
                DealErr("修改实例为执行中【失败】：" + err.Message);
            }
            else
            {
                _isRun = true;
                log("修改实例状态为【执行中】成功!");
            }

            //由于不设定起始数据库,交由setnowdb初始化

            nodeCaseID = nCaseID;//节点实例ID
        }

        /// <summary>
        /// 错误信息处理
        /// </summary>
        /// <param name="err"></param>
        /// <param name="msg"></param>
        public void DealErr(string msg)
        {
            _isRun = false;
            errMsg = msg;
            log("错误消息：" + msg);
        }

        /// <summary>
        /// 设置当前数据库（只保存连接基本信息，不建立连接）
        /// </summary>
        public void setnowdb(string dbNickName = null)
        {
            if (!Vilidate()) return;

            if (string.IsNullOrEmpty(dbNickName))
            {
                errMsg = "指定的数据库名不能为空";

                DealErr(errMsg);
                return;
            }

            //在设置新数据库链接时，先释放之前的连接资源Dispose()
            if (dbServer != null)
            {
                Dispose();
            }
            else
            {
                dbServer = new DBServer();
            }

            ErrorInfo err = new ErrorInfo();
            var dbs = ScriptManager.GetDBServer(dbNickName, ref err);
            if (dbs == null)
            {
                errMsg = "未找到数据库【" + dbNickName + "】";
                DealErr(errMsg);
                return;
            }
            if (err.IsError)
            {
                DealErr(err.Message);
                return;
            }

            //复制类属性值
            dbServer = Fun.ClassToCopy(dbs, dbServer);
            //转为大写
            dbServer.DB_TYPE = dbServer.DB_TYPE.ToUpper();
            //获取链接字符串
            dbServer.ConnectionStr = GetDbConnStr(dbServer);
            log(string.Format("【{0}】数据库切换成功", dbNickName));
        }

        /// <summary>
        /// 设置当前数据库byID
        /// </summary>
        public void setnowdbid(long? dbid)
        {
            if (!Vilidate()) return;
            if (dbid == null)
            {
                errMsg = "传入的数据库id不能为空";

                DealErr(errMsg);
                return;
            }

            //在设置新数据库链接时，先释放之前的连接资源Dispose()
            if (dbServer != null)
            {
                Dispose();
            }
            else
            {
                dbServer = new DBServer();
            }

            ErrorInfo err = new ErrorInfo();
            var dbs = ScriptManager.GetDBServerByID(dbid, ref err);
            if (dbs == null)
            {
                errMsg = "未找到编号为【" + dbid.ToString() + "】的数据库";
                DealErr(errMsg);
                return;
            }
            if (err.IsError)
            {
                DealErr(err.Message);
                return;
            }
            //复制类属性值
            dbServer = Fun.ClassToCopy(dbs, dbServer);
            //转为大写
            dbServer.DB_TYPE = dbServer.DB_TYPE.ToUpper();
            //获取链接字符串
            dbServer.ConnectionStr = GetDbConnStr(dbServer);

            log(string.Format("编号为【{0}】的数据库【{1}】切换成功", dbid, dbServer.BYNAME));
        }

        /// <summary>
        /// 获取当前节点实例的状态
        /// 在每个方法前均需要验证状态
        /// </summary>
        /// <returns></returns>
        public short? CaseRunStatus()
        {
            ErrorInfo err = new ErrorInfo();
            var nodeCase = ScriptManager.GetScriptNodeCase(nodeCaseID, ref err);
            if (err.IsError)
            {
                errMsg = "获取不到当前节点实例：" + err.Message;
                DealErr(errMsg);
                return null;
            }
            else
            {
                return nodeCase.RUN_STATUS;
            }
        }

        /// <summary>
        /// 释放连接资源
        /// </summary>
        public void Dispose()
        {
            dbServer = null;
        }

        /// <summary>
        /// 释放指定数据库连接资源
        /// </summary>
        public void Dispose(DBServer dbServer)
        {
            dbServer = null;
        }


        /// <summary>
        /// 向节点实例写日志
        /// 注：日志的写入中不需要加入isRun的验证
        /// </summary>
        /// <param name="msg"></param>
        public void log(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                ErrorInfo err = new ErrorInfo();
                ScriptManager.LogForNodeCase(msg, "", nodeCaseID, ref err);
            }
        }

        /// <summary>
        /// 向节点实例写日志
        /// 注：日志的写入中不需要加入isRun的验证
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="sql"></param>
        public void log(string msg, string sql)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                ErrorInfo err = new ErrorInfo();
                ScriptManager.LogForNodeCase(msg, sql, nodeCaseID, ref err);
            }
        }

        /// <summary>
        /// 获取错误信息
        /// </summary>
        /// <returns></returns>
        public string GetError()
        {
            return errMsg;
        }

        /// <summary>
        /// 启动下一组节点实例
        /// </summary>
        public void StartNextNodeCase()
        {
            ErrorInfo err = new ErrorInfo();
            bool isSuc = ScriptManager.AddNextScritNodeCase(nodeCaseID, ref err);
            if (isSuc)
            {
                log("启动下一组节点实例【成功】！");
            }
            else
            {
                DealErr("启动下一组节点实例【失败】：" + err.Message);
            }
        }

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        private string GetDbConnStr(DBServer db)
        {
            switch (db.DB_TYPE.ToUpper())
            {
                case "DB2":
                    return string.Format("Server={0}:{1};Database={2};UID={3};PWD={4};Connection Timeout =3600", db.IP, db.PORT, db.DATA_CASE, db.USER, db.PASSWORD);
                case "ORACLE":
                    return string.Format("Data Source={0}:{1}/{2};User Id={3};Password={4};Connection Timeout =3600", db.IP, db.PORT, db.DATA_CASE, db.USER, db.PASSWORD);
                default:
                    log(string.Format("未知的数据库类型【{0}】", db.DB_TYPE.ToUpper()));
                    return string.Empty;
            }
        }

        /// <summary>
        /// 验证节点实例是否运行中
        /// </summary>
        /// <returns></returns>
        public bool Vilidate()
        {
            if (!_isRun)
            {
                return false;
            }

            short? status = CaseRunStatus();

            if (status == (short)PubEnum.RunStatus.Stop)
            {
                _isRun = false;
                errMsg = "节点实例已停止，后续内容停止执行";
                log(errMsg);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 删除sql语句最后的分号
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public string DeleteSem(string sql)
        {
            return sql.TrimEnd(new char[] { ' ', ',' });
        }

        /// <summary>
        /// 执行SQL命令
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int execute(string sql)
        {
            if (!Vilidate())
            {
                log(string.Format(@"未能执行语句【{0}】，因为节点已经处理未运行状态。", sql));
                return 0;
            }
            //验证当前是否打开数据库
            if (dbServer == null)
            {
                log(string.Format(@"未设置执行数据库，请在语句【{0}】之前指定数据库setnowdb(""数据库名"");", sql));
                return 0;
            }
            sql = DeleteSem(sql);
            int reObj = 0;
            try
            {
                switch (dbServer.DB_TYPE)
                {
                    case "DB2":
                        reObj = DbHelper.DB2Helper.ExecuteNonQuery(dbServer.ConnectionStr, sql);
                        break;
                    case "ORACLE":
                        reObj = DbHelper.OracleHelper.ExecuteNonQuery(dbServer.ConnectionStr, sql);
                        break;
                    default:
                        log(string.Format("未知的源数据库类型【{0}】", dbServer.DB_TYPE));
                        return 0;
                }
                log(string.Format("在【{0}】执行SQL语句【{1}】成功，返回结果为【{2}】", dbServer.BYNAME, sql, reObj), sql);
            }
            catch (Exception e)
            {
                errMsg = string.Format("在【{0}】执行语句【{1}】失败,错误原因：\r\n{2}", dbServer.BYNAME, sql, e.ToString());
                DealErr(errMsg);
                Dispose(dbServer);
            }
            return reObj;
        }

        /// <summary>
        /// 在指定数据库执行sql
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public int execute(string sql, DBServer db)
        {
            if (!Vilidate())
            {
                log(string.Format(@"未能执行语句【{0}】，因为节点已经处理未运行状态。", sql));
                return 0;
            }

            //验证当前是否存在数据库实例
            if (db == null)
            {
                log(string.Format(@"未设置执行数据库，请在语句【{0}】之前指定数据库setnowdb(""数据库名"");", sql));
                return 0;
            }
            sql = DeleteSem(sql);

            int reObj = 0;
            try
            {
                string connStr = GetDbConnStr(db);
                switch (db.DB_TYPE.ToUpper())
                {
                    case "DB2":
                        reObj = DbHelper.DB2Helper.ExecuteNonQuery(connStr, sql);
                        break;
                    case "ORACLE":
                        reObj = DbHelper.OracleHelper.ExecuteNonQuery(connStr, sql);
                        break;
                }
            }
            catch (Exception e)
            {
                errMsg = string.Format("在【{0}】执行语句【{1}】失败,错误原因：\r\n{2}", db.BYNAME, sql, e.ToString());
                DealErr(errMsg);
                Dispose(db);
            }
            return reObj;
        }

        /// <summary>
        /// 执行函数简写
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int exec(string sql)
        {
            return execute(sql);
        }

        /// <summary>
        /// 返回执行返回值
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public object execute_scalar(string sql)
        {
            if (!Vilidate())
            {
                log(string.Format(@"未能执行语句【{0}】，因为节点已经处理未运行状态。", sql));
                return null;
            }
            //验证当前是否打开数据库
            if (dbServer == null)
            {
                log(string.Format(@"未设置执行数据库，请在语句【{0}】之前指定数据库setnowdb(""数据库名"");", sql));
                return 0;
            }
            sql = DeleteSem(sql);

            object reObj = 0;
            try
            {
                switch (dbServer.DB_TYPE)
                {
                    case "DB2":
                        reObj = DbHelper.DB2Helper.ExecuteScalar(dbServer.ConnectionStr, sql);
                        break;
                    case "ORACLE":
                        reObj = DbHelper.OracleHelper.ExecuteScalar(dbServer.ConnectionStr, sql);
                        break;
                    default:
                        log(string.Format("未知的源数据库类型【{0}】", dbServer.DB_TYPE));
                        return 0;
                }
                log(string.Format("执行操作语句【{0}】成功", sql));
            }
            catch (Exception e)
            {
                errMsg = string.Format("在【{0}】执行语句【{1}】失败,错误原因：\r\n{2}", dbServer.BYNAME, sql, e.ToString());
                DealErr(errMsg);
                Dispose(dbServer);
            }
            return reObj;
        }

        /// <summary>
        /// 在指定数据库上执行
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        private object execute_scalar(string sql, DBServer db)
        {
            if (!Vilidate())
            {
                log(string.Format(@"未能执行语句【{0}】，因为节点已经处理未运行状态。", sql));
                return 0;
            }
            sql = DeleteSem(sql);

            object reObj = 0;
            string connStr = GetDbConnStr(db);
            try
            {
                switch (db.DB_TYPE.ToUpper())
                {
                    case "DB2":
                        reObj = DbHelper.DB2Helper.ExecuteScalar(connStr, sql);
                        break;
                    case "ORACLE":
                        reObj = DbHelper.OracleHelper.ExecuteScalar(connStr, sql);
                        break;
                    default:
                        log(string.Format("未知的源数据库类型【{0}】", db.DB_TYPE.ToUpper()));
                        return 0;
                }
                log(string.Format("执行操作语句成功【{0}】在【{1}】", sql, db.BYNAME));
            }
            catch (Exception e)
            {
                errMsg = string.Format("在【{0}】执行语句【{1}】失败,错误原因：\r\n{2}", dbServer.BYNAME, sql, e.ToString());
                DealErr(errMsg);
                Dispose(dbServer);
            }
            return reObj;
        }



        /// <summary>
        /// 生成创建表的脚本
        /// </summary>
        /// <param name="filedList"></param>
        /// <param name="tableName"></param>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public string MakeCreateTableSql(List<TableFiled> filedList, string tableName, string dbType)
        {
            string reStr = @"
create Table {0}
(
{1}
)
            ";
            IList<string> filed = new List<string>();
            string tmpFormat = "{0} {1}";
            foreach (var t in filedList)
            {
                switch (t.CSharpType)
                {
                    case "Int16":
                        switch (dbType)
                        {
                            case "DB2":
                                filed.Add(string.Format(tmpFormat, t.Code, "SMALLINT"));
                                break;
                            case "ORACLE":
                                filed.Add(string.Format(tmpFormat, t.Code, "NUMBER(4)"));
                                break;
                            default:
                                filed.Add(string.Format(tmpFormat, t.Code, "NUMBER(10)"));
                                break;
                        }
                        break;
                    case "Int32":
                        switch (dbType)
                        {
                            case "DB2":
                                filed.Add(string.Format(tmpFormat, t.Code, "INT"));
                                break;
                            case "ORACLE":
                                filed.Add(string.Format(tmpFormat, t.Code, "NUMBER(10)"));
                                break;
                            default:
                                filed.Add(string.Format(tmpFormat, t.Code, "INT"));
                                break;
                        }
                        break;
                    case "Int64":
                        switch (dbType)
                        {
                            case "DB2":
                                filed.Add(string.Format(tmpFormat, t.Code, "BIGINT"));
                                break;
                            case "ORACLE":
                                filed.Add(string.Format(tmpFormat, t.Code, "NUMBER(20)"));
                                break;
                            default:
                                filed.Add(string.Format(tmpFormat, t.Code, "INT"));
                                break;
                        }
                        break;
                    case "DateTime":
                        switch (dbType)
                        {
                            case "DB2":
                                filed.Add(string.Format(tmpFormat, t.Code, "TIMESTAMP"));
                                break;
                            case "ORACLE":
                                filed.Add(string.Format(tmpFormat, t.Code, "TIMESTAMP"));
                                break;
                            default:
                                filed.Add(string.Format(tmpFormat, t.Code, "datetime"));
                                break;
                        }
                        break;
                    default:
                        if (t.Length == 0) t.Length = 100;
                        if (t.Length < 3000)
                        {
                            switch (dbType)
                            {
                                case "DB2":
                                    filed.Add(string.Format(tmpFormat, t.Code, string.Format("VARCHAR({0})", t.Length * 2)));
                                    break;
                                case "ORACLE":
                                    filed.Add(string.Format(tmpFormat, t.Code, string.Format("VARCHAR2({0})", t.Length * 2)));
                                    break;
                                default:
                                    filed.Add(string.Format(tmpFormat, t.Code, string.Format("VARCHAR({0})", t.Length * 2)));
                                    break;
                            }
                        }
                        else
                        {
                            switch (dbType)
                            {
                                case "DB2":
                                    filed.Add(string.Format(tmpFormat, t.Code, "CLOB"));
                                    break;
                                case "ORACLE":
                                    filed.Add(string.Format(tmpFormat, t.Code, "NCLOB"));
                                    break;
                                default:
                                    filed.Add(string.Format(tmpFormat, t.Code, "TEXT"));
                                    break;
                            }
                        }
                        break;
                }
            }
            reStr = string.Format(reStr, tableName, string.Join(",\r\n", filed));
            return reStr;
        }

        /// <summary>
        /// 导出表，生成的文件路径：~/UpFiles/DownDB/{tableName}.zip
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="zipFileName">压缩文件名</param>
        /// <returns>文件路径</returns>
        public string down_db_to_file(string sql, string zipFileName)
        {
            if (!Vilidate())
            {
                log(string.Format(@"未能执行语句【{0}】，因为节点已经处理未运行状态。", sql));
                return null;
            }
            var path = down_db(sql, zipFileName);
            var zipFile = _downDbPath + zipFileName + ".zip";
            try
            {
                Easyman.Service.Common.ZipHelper.ZipFileMain(path, zipFile);
                log(string.Format("生成ZIP文件成功路径为【{0}】", zipFile));
            }
            catch (Exception e)
            {
                errMsg = string.Format("压缩文件失败，路径为【{0}】", path);
                errMsg += string.Format("\r\n错误原因【{0}】", e.Message);
                DealErr(errMsg);
            }

            return zipFile;
        }

        /// <summary>
        /// 查询数据并写到另外的表
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="fileName"></param>
        /// <param name="pageSize"></param>
        /// <returns>输出文件所在的路径</returns>
        public string down_db(string sql, string fileName, int pageSize = 1000000)
        {
            if (!Vilidate())
            {
                log(string.Format(@"未能执行语句【{0}】，因为节点已经处理未运行状态。", sql));
                return null;
            }

            string path = _downDbPath + fileName + "\\";

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                foreach (var file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                errMsg = string.Format("导出数据到文件，在创建及清空目录【{0}】时出错，错误信息为：{1}", sql, ex.ToString());
                DealErr(errMsg);

                return "";
            }

            //数据库查询类
            object dbHelper = new object();
            DbDataReader dr = null;
            try
            {
                switch (dbServer.DB_TYPE)
                {
                    case "DB2":
                        dbHelper = new DbHelper.DB2Helper(dbServer.ConnectionStr);
                        dr = ((DbHelper.DB2Helper)dbHelper).ExecuteReader(dbServer.ConnectionStr, sql);
                        break;
                    case "ORACLE":
                        dbHelper = new DbHelper.OracleHelper(dbServer.ConnectionStr);
                        dr = ((DbHelper.OracleHelper)dbHelper).ExecuteReader(dbServer.ConnectionStr, sql);
                        break;
                    default:
                        log(string.Format("未知的源数据库类型【{0}】", dbServer.DB_TYPE));
                        return string.Empty;
                }

                var tableFiled = new List<TableFiled>();
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    tableFiled.Add(new TableFiled { Code = dr.GetName(i), DataType = dr.GetDataTypeName(i), CSharpType = dr.GetFieldType(i).Name });
                }
                Fun.WriteAllText(path + fileName + "_Create.txt", MakeCreateTableSql(tableFiled, fileName, dbServer.DB_TYPE));
                Fun.WriteAllText(path + fileName + "_TableFiled.txt", Fun.DecodeToStr(tableFiled));

                //开始导数据
                IList<string> allData = new List<string>();
                int rowNum = 0;
                int pageNum = 1;
                while (dr.Read())
                {
                    rowNum++;
                    IList<string> rowData = new List<string>();
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        if (dr[i] == null || dr[i] == DBNull.Value)
                        {
                            rowData.Add(null);
                        }
                        else
                        {
                            rowData.Add(dr[i].ToString().Replace(",", "，，"));
                        }
                    }
                    allData.Add(string.Join(",", rowData));
                    if (rowNum == pageSize)
                    {
                        File.WriteAllLines(path + fileName + "_" + pageNum + ".txt", allData);
                        log(string.Format("下载完成第【{0}】页数据", pageNum));
                        allData = new List<string>();
                        rowNum = 0;
                        pageNum++;
                    }
                }
                log(string.Format("结束下载，共【{0}】页，【{1}】条数据", pageNum, (pageNum - 1) * pageSize + rowNum));
                if (rowNum != 0)
                {
                    File.WriteAllLines(path + fileName + "_" + pageNum + ".txt", allData);
                    allData = new List<string>();
                }
                dr.Close();
                dr.Dispose();

            }
            catch (Exception e)
            {
                errMsg = string.Format("执行查询语句【{0}】失败,错误原因\r\n{1}", sql, e.ToString());
                DealErr(errMsg);

                return "";
            }
            finally
            {
                //关闭数据库连接
                switch (dbServer.DB_TYPE)
                {
                    case "DB2":
                        ((DbHelper.DB2Helper)dbHelper).Dispose();
                        break;
                    case "ORACLE":
                        ((DbHelper.OracleHelper)dbHelper).Dispose();
                        break;
                }

                //释放连接资源
                Dispose(dbServer);
            }

            return path;
        }

        /// <summary>
        /// 下载表到数据库
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="tableName">生成的表名</param>
        /// <param name="dbNickName">导入的数据库</param>
        /// <param name="isCreat">0(默认值)表示不创建表;1:表示自动创建表(在导数据之前，要删除已经存在表);</param>
        /// <param name="pageSize">设置每页导出的大小。值越大速度越快(对服务器压力大，内容要足够大) 默认值：1000000</param>
        public void down_db_to_db(string sql, string tableName, string dbNickName, int isCreatTable = 1, int pageSize = 1000000)
        {
            if (!Vilidate())
            {
                log(string.Format(@"未能执行语句【{0}】，因为节点已经处理未运行状态。", sql));
                return;
            }
            ErrorInfo err = new ErrorInfo();

            //数据库查询类（源数据库）
            object dbHelperFrom = new object();
            //数据库查询类（目的数据库）
            object dbHelperTo = new object();
            DbDataReader dr = null;
            #region 读取数据
            try
            {
                //获取导入库
                EM_DB_SERVER toDBServerEntity = ScriptManager.GetDBServer(dbNickName, ref err);
                if (toDBServerEntity == null)
                {
                    log(string.Format("目的【{0}】数据库不存在", dbNickName));
                    return;
                }
                //导入库
                DBServer toDBServer = new DBServer();
                toDBServer = Fun.ClassToCopy(toDBServerEntity, toDBServer);
                toDBServer.DB_TYPE = toDBServer.DB_TYPE.ToUpper();

                //源数据库
                switch (dbServer.DB_TYPE)
                {
                    case "DB2":
                        dbHelperFrom = new DbHelper.DB2Helper(dbServer.ConnectionStr);
                        dr = ((DbHelper.DB2Helper)dbHelperFrom).ExecuteReader(dbServer.ConnectionStr, sql);
                        break;
                    case "ORACLE":
                        dbHelperFrom = new DbHelper.OracleHelper(dbServer.ConnectionStr);
                        dr = ((DbHelper.OracleHelper)dbHelperFrom).ExecuteReader(dbServer.ConnectionStr, sql);
                        break;
                    default:
                        log(string.Format("未知的源数据库类型【{0}】", dbServer.DB_TYPE));
                        return;
                }
                log(string.Format("数据库【{0}】执行查询语句【{1}】成功，下面将准备导入目标库", dbServer.BYNAME, sql));
                //字段信息
                var allFiled = new List<TableFiled>();
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    allFiled.Add(new TableFiled { Code = dr.GetName(i), DataType = dr.GetDataTypeName(i), CSharpType = dr.GetFieldType(i).Name });
                }

                #region 导入数据
                int totalRowsCount = 0;
                IList<IList<object>> allRows = new List<IList<object>>();
                int rowNum = 0;
                int pageNum = 1;
                DateTime begin = DateTime.Now;
                TimeSpan ts = DateTime.Now - begin;
                DateTime bi = DateTime.Now;
                TimeSpan tsi = DateTime.Now - bi;

                var isRight = true;
                while (dr.Read())
                {
                    if (!Vilidate())
                    {
                        break;
                    }

                    rowNum++;
                    //行数据
                    IList<object> rowData = new List<object>();
                    #region 读取行数据
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        if (dr[i] == null || dr[i] == DBNull.Value)
                        {
                            rowData.Add(null);
                        }
                        else
                        {
                            rowData.Add(dr[i]);
                        }
                    }
                    allRows.Add(rowData);
                    #endregion
                    //log("999");
                    //如果行数据等于页数，则开始导入
                    if (rowNum == pageSize)
                    {
                        int parNum = allFiled.Count;
                        //转成可插入的格式
                        IList<IList<object>> allData = new List<IList<object>>();
                        #region 行转列
                        foreach (var x in allFiled)
                        {
                            allData.Add(new object[allRows.Count]);
                        }
                        object[] allPar = new object[parNum];
                        for (int a = 0; a < allRows.Count(); a++)
                        {
                            for (int i = 0; i < parNum; i++)
                            {
                                allData[i][a] = allRows[a][i];
                            }
                        }
                        #endregion

                        #region 如果是第一页则创建表
                        if (pageNum == 1)
                        {
                            for (var i = 0; i < allFiled.Count; i++)
                            {
                                if (allFiled[i].CSharpType == "String")
                                {

                                    string maxSqlStr = string.Format("SELECT  MAX(LENGTH({0})) FROM ({1})", allFiled[i].Code, sql);
                                    object maxLength = 0;
                                    switch (dbServer.DB_TYPE)
                                    {
                                        case "DB2":
                                            maxLength = DbHelper.DB2Helper.ExecuteScalar(dbServer.ConnectionStr, maxSqlStr);
                                            break;
                                        case "ORACLE":
                                            maxLength = DbHelper.OracleHelper.ExecuteScalar(dbServer.ConnectionStr, maxSqlStr);
                                            break;
                                    }
                                    if (maxLength == null || maxLength == DBNull.Value)
                                    {
                                        maxLength = 50;
                                    }
                                    allFiled[i].Length = Convert.ToInt32(maxLength) * 2;
                                }
                            }
                            if (isCreatTable == 1)
                            {
                                var createScript = MakeCreateTableSql(allFiled, tableName, toDBServer.DB_TYPE);
                                drop_table(tableName, toDBServer);
                                log(string.Format("在数据【{1}】上创建表【{0}】", tableName, dbNickName));
                                execute(createScript, toDBServer);
                            }
                        }
                        #endregion
                        if (!Vilidate())
                        {
                            log(string.Format(@"未能全部执行语句【{0}】，因为节点在处理过程中已经处理未运行状态。", sql));
                            break;
                        }

                        bi = DateTime.Now;
                        isRight = InsertInto(tableName, dbNickName, allFiled, allData, allRows.Count, pageNum);
                        tsi = DateTime.Now - bi;
                        allData.Clear();
                        allRows.Clear();
                        totalRowsCount += pageSize;
                        ts = DateTime.Now - begin;
                        log(string.Format("完成导入第【{0}】页【{1}】条数据，读取及导入共用时【{2}】毫秒（其中批量导入用时【{3}】毫秒），累计已经导入【{4}】条数据", pageNum, pageSize, ts.TotalMilliseconds, tsi.TotalMilliseconds, totalRowsCount));

                        rowNum = 0;
                        begin = DateTime.Now;

                        var nodec = ScriptManager.GetScriptNodeCase(nodeCaseID, ref err);
                        if (!isRight || nodec == null || nodec.RUN_STATUS == (short)PubEnum.RunStatus.Stop)
                        {
                            DealErr(string.Format(@"第【{0}】页数据导入失败", pageNum));
                            break;
                        }
                        pageNum++;
                    }
                }

                #endregion

                if (!Vilidate())
                {
                    log(string.Format(@"未能全部执行语句【{0}】，因为节点在处理过程中已经处理未运行状态。", sql));

                    return;
                }

                //余下不足一页的记录
                if (rowNum != 0)
                {
                    int parNum = allFiled.Count;
                    IList<IList<object>> allData = new List<IList<object>>();
                    foreach (var x in allFiled)
                    {
                        allData.Add(new object[allRows.Count]);
                    }
                    object[] allPar = new object[parNum];
                    for (int a = 0; a < allRows.Count(); a++)
                    {
                        for (int i = 0; i < parNum; i++)
                        {
                            allData[i][a] = allRows[a][i];
                        }
                    }

                    if (pageNum == 1)
                    {
                        for (var i = 0; i < allFiled.Count; i++)
                        {
                            if (allFiled[i].CSharpType == "String")
                            {
                                var allUseCol = allData[i].Where(x => x != null && x != DBNull.Value);
                                if (allUseCol.Count() > 0)
                                {
                                    allFiled[i].Length = allUseCol.Max(x => x.ToString().Length);
                                }
                                else
                                {
                                    allFiled[i].Length = 500;
                                }
                            }
                        }
                        if (isCreatTable == 1)
                        {
                            var createScript = MakeCreateTableSql(allFiled, tableName, toDBServer.DB_TYPE.ToUpper());
                            drop_table(tableName, toDBServer);
                            log(string.Format("在数据【{1}】上创建表【{0}】", tableName, dbNickName));
                            execute(createScript, toDBServer);
                        }
                    }
                    isRight = InsertInto(tableName, dbNickName, allFiled, allData, allRows.Count, pageNum);
                    allData.Clear();
                }
                if (isRight)
                {
                    totalRowsCount += rowNum;
                    ts = DateTime.Now - begin;
                    log(string.Format("结束导数，共【{0}】页，本页【{1}】条数据，用时【{2}】毫秒，累计已经导入【{3}】条数据", pageNum, pageNum, ts.TotalMilliseconds, totalRowsCount));
                }
                else
                {
                    errMsg = string.Format("导数失败，共【{0}】页，【{1}】条数据", pageNum, (pageNum - 1) * pageSize + rowNum);
                    DealErr(errMsg);
                }

                dr.Close();
                dr.Dispose();
            }
            catch (Exception e)
            {
                errMsg = string.Format("数据库【{0}】执行查询语句【{1}】失败，错误原因：\r\n{2}", dbServer.BYNAME, sql, e.ToString());
                DealErr(errMsg);
                //释放连接资源
                Dispose(dbServer);
                return;
            }
            finally
            {
                //关闭数据库连接
                switch (dbServer.DB_TYPE)
                {
                    case "DB2":
                        ((DbHelper.DB2Helper)dbHelperFrom).Dispose();
                        break;
                    case "ORACLE":
                        ((DbHelper.OracleHelper)dbHelperFrom).Dispose();
                        break;
                }

                //释放连接资源
                Dispose(dbServer);
            }

            #endregion
        }

        /// <summary>
        /// 批量插入数据
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dbNickName"></param>
        /// <param name="allFiled"></param>
        /// <param name="allData"></param>
        /// <param name="rowsNum"></param>
        /// <param name="nowPage"></param>
        /// <returns></returns>
        public bool InsertInto(string tableName, string dbNickName, IList<TableFiled> allFiled, IList<IList<object>> allData, int rowsNum, int nowPage)
        { 
            ErrorInfo err = new ErrorInfo();
            var dbe = ScriptManager.GetDBServer(dbNickName, ref err);
            var db = Fun.ClassToCopy(dbe, new DBServer());
            db.DB_TYPE = db.DB_TYPE.ToUpper();
            string connStr = GetDbConnStr(db);
            try
            {
                object oldNum = 0, newNum = 0;
                switch (db.DB_TYPE.ToUpper())
                {
                    case "DB2":
                        IList<DB2Parameter> DB2P = new List<DB2Parameter>();
                        for (int i = 0; i < allFiled.Count; i++)
                        {
                            var t = allFiled[i];
                            DB2Parameter deptNoParam = new DB2Parameter(t.Code, DbHelper.DB2Helper.GetDbType(t.CSharpType));
                            deptNoParam.Direction = ParameterDirection.Input;
                            deptNoParam.Value = allData[i];
                            DB2P.Add(deptNoParam);
                        }
                        var insertSql = string.Format("insert into {0} ({1}) values({2})", tableName, string.Join(",", allFiled.Select(x => x.Code)), "@" + string.Join(",@", allFiled.Select(x => x.Code)));
                        oldNum = DbHelper.DB2Helper.ExecuteScalar(connStr, string.Format("select count(1) T from {0}", tableName));
                        DbHelper.DB2Helper.Import(rowsNum, connStr, insertSql, DB2P);
                        newNum = DbHelper.DB2Helper.ExecuteScalar(connStr, string.Format("select count(1) T from {0}", tableName));

                        DB2P.Clear();

                        break;
                    case "ORACLE":
                        IList<OracleParameter> OracleP = new List<OracleParameter>();
                        for (int i = 0; i < allFiled.Count; i++)
                        {
                            var t = allFiled[i];
                            OracleParameter deptNoParam = new OracleParameter(t.Code, DbHelper.OracleHelper.GetDbType(t.CSharpType));
                            deptNoParam.Direction = ParameterDirection.Input;
                            deptNoParam.Value = allData[i];
                            OracleP.Add(deptNoParam);
                        }
                        insertSql = string.Format("insert into {0} ({1}) values({2})", tableName, string.Join(",", allFiled.Select(x => x.Code)), ":" + string.Join(",:", allFiled.Select(x => x.Code)));
                        oldNum = DbHelper.OracleHelper.ExecuteScalar(connStr, string.Format("select count(1) T from {0}", tableName));
                        DbHelper.OracleHelper.Import(rowsNum, connStr, insertSql, OracleP);
                        newNum = DbHelper.OracleHelper.ExecuteScalar(connStr, string.Format("select count(1) T from {0}", tableName));

                        break;
                    default:
                        log(string.Format("未知的源数据库类型【{0}】", db.DB_TYPE.ToUpper()));
                        return false;
                }

                if (Convert.ToDecimal(newNum) != Convert.ToDecimal(oldNum) + rowsNum)
                {
                    string sqlerr = string.Format("导入数据失败，已导入{0}条;导入第{1}页{2}条数据失败", oldNum, nowPage, rowsNum);
                    throw new Exception(sqlerr);
                }
                allData.Clear();
                return true;
            }
            catch (Exception e)
            {
                errMsg = string.Format("导入第【{0}】页数据失败", nowPage);
                errMsg += string.Format("\r\n失败原因：{0}", e.Message);
                DealErr(errMsg);
                //Dispose(ent);//释放资源
                return false;
            }
        }


        /// <summary>
        /// 快速删除表
        /// </summary>
        /// <param name="tables">可以是多表，以逗号分开</param>
        public void drop_table(string tables)
        {
            log(string.Format("将要在当前数据库上删除表【{0}】", tables));
            //truncate_table(tables);
            int i = 0;
            foreach (string table in SplitString(tables))
            {
                string schema = dbServer.USER;
                string scTable = GetSchemaTableName(table, schema);
                if (is_table_exists(scTable))
                {
                    i += execute("DROP TABLE " + scTable);
                }
            }

            log(string.Format("在当前数据库上成功删除了【{0}】张表【{1}】", i, tables));
        }

        /// <summary>
        /// 删除表
        /// </summary>
        /// <param name="tables"></param>
        /// <param name="db"></param>
        public void drop_table(string tables, DBServer db)
        {
            log(string.Format("将要删除在【{0}】的表【{1}】", db.BYNAME, tables));
            int i = 0;
            foreach (string table in SplitString(tables))
            {
                string schema = db.USER;
                string scTable = table;
                if (is_table_exists(scTable, db))
                {
                    i += execute("DROP TABLE " + scTable, db);
                }
            }
            log(string.Format("在【{0}】上成功删除了【{1}】张表【{2}】", db.BYNAME, i, tables));
        }

        /// <summary>
        /// 快速清空资料表
        /// </summary>
        /// <param name="tables">可以是多表，以逗号分开</param>
        public void truncate_table(string tables)
        {
            log(string.Format("将要在当前数据库上清除表【{0}】", tables));
            int i = 0;
            int j = 0;
            foreach (string table in SplitString(tables))
            {
                string schema = dbServer.USER;
                string scTable = GetSchemaTableName(table, schema);
                if (is_table_exists(scTable))
                {
                    try
                    {
                        switch (dbServer.DB_TYPE)
                        {
                            case "DB2":
                                i += execute("ALTER TABLE " + scTable + " ACTIVATE NOT LOGGED INITIALLY WITH EMPTY TABLE");
                                break;
                            case "Sql":
                                break;
                            default:
                                i += execute("TRUNCATE TABLE " + scTable);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        _isRun = false;
                        log(string.Format("清除表【{0}】资料失败，失败原因{1}", scTable, e.ToString()));
                        j++;
                    }
                }
            }

            log(string.Format("在当前数据库上成功清除了【{0}】张表【{1}】，有【{2}】张失败。", i, tables, j));
        }

        /// <summary>
        /// 表空表
        /// </summary>
        /// <param name="tables"></param>
        /// <param name="ent"></param>
        public void truncate_table(string tables, DBServer ent)
        {
            log(string.Format("将要在【{0}】当前数据库上清除表【{1}】", ent.BYNAME, tables));
            int i = 0;
            int j = 0;

            foreach (string table in SplitString(tables))
            {
                string schema = ent.USER;
                string scTable = GetSchemaTableName(table, schema);
                if (is_table_exists(tables, ent))
                {
                    try
                    {
                        switch (ent.DB_TYPE.ToUpper())
                        {
                            case "DB2":
                                i += execute("ALTER TABLE " + scTable + " ACTIVATE NOT LOGGED INITIALLY WITH EMPTY TABLE", ent);
                                break;
                            case "Sql":
                                break;
                            default:
                                i += execute("TRUNCATE TABLE " + scTable, ent);
                                break;
                        }
                        log(string.Format("清除表资料{0}成功", scTable));
                    }
                    catch (Exception e)
                    {
                        _isRun = false;
                        log(string.Format("清除表【{0}】资料失败，失败原因{1}", scTable, e.ToString()));
                        j++;
                    }
                }
            }

            log(string.Format("在【{0}】上成功清除了【{1}】张表【{2}】，有【{3}】张失败。", ent.BYNAME, i, tables, j));
        }

        public string GetSchemaTableName(string tableName, string schema)
        {
            string reStr = "";
            string[] temp = tableName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length > 1)
            {
                schema = temp[temp.Length - 2];
                reStr = string.Format("{0}.{1}", temp[temp.Length - 2], temp[temp.Length - 1]);
            }
            else if (!string.IsNullOrEmpty(schema))
            {
                reStr = string.Format("{0}.{1}", schema, tableName);
            }
            return reStr;
        }

        /// <summary>
        /// 获取表名
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public string GetTableName(string tableName, out string schema)
        {
            string reStr = "";
            string[] temp = tableName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            schema = dbServer.USER;
            if (temp.Length > 1)
            {
                schema = temp[temp.Length - 2];
                reStr = temp[temp.Length - 1];
            }
            else if (!string.IsNullOrEmpty(dbServer.USER))
            {
                reStr = tableName;
            }
            return reStr;
        }

        /// <summary>
        /// 判断表是否存在
        /// </summary>
        /// <param name="tables">可以是多表，以逗号分开</param>
        /// <returns></returns>
        public bool is_table_exists(string tables)
        {
            // Oracle系统表存放的是大写的表名，查找时须转换为大写
            foreach (string table in SplitString(tables.ToUpper()))
            {
                string schema = dbServer.USER;
                string scTable = GetSchemaTableName(table, schema);
                object obj = execute_scalar(GetTableExistsCommand(scTable));
                int i = Convert.ToInt32(obj);
                if (i < 1)
                {
                    log("表【" + table + "】不存在。");
                    return false;
                }
            }

            log("表【" + tables + "】存在。");
            return true;
        }

        /// <summary>
        /// 判断表是否存在
        /// </summary>
        /// <param name="tables">表名，多个表名以逗号分隔</param>
        /// <param name="db"></param>
        /// <returns></returns>
        public bool is_table_exists(string tables, DBServer db)
        {
            // Oracle系统表存放的是大写的表名，查找时须转换为大写
            foreach (string table in SplitString(tables.ToUpper()))
            {
                string schema = db.USER;
                string scTable = table;

                object obj = execute_scalar(GetTableExistsCommand(scTable, db), db);
                int i = Convert.ToInt32(obj);
                if (i < 1)
                {
                    log("表【" + table + "】不存在。");
                    return false;
                }
            }
            log("表【" + tables + "】存在。");
            return true;
        }

        /// <summary>
        /// 获取序列（序列以表名为前缀，_SEQ为后缀）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public int GetSeqID(string tableName)
        {
            string sql = "SELECT   " + tableName + "_SEQ.NEXTVAL   FROM   DUAL";
            switch (dbServer.DB_TYPE)
            {
                case "DB2":
                    sql = "SELECT   " + tableName + "_SEQ.NEXTVAL   FROM   SYSIBM.DUAL";
                    break;
                case "Oracle":
                    sql = "SELECT   " + tableName + "_SEQ.NEXTVAL   FROM   DUAL";
                    break;
                case "Sql":
                    return 0;
            }
            object obj = execute_scalar(sql);
            return Convert.ToInt32(obj);
        }

        /// <summary>
        /// 生成查询表是否存在的SQL语句
        /// </summary>
        /// <param name="table"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        private string GetTableExistsCommand(string table, DBServer db)
        {
            string result;
            string schema = db.USER;
            string tableName = table;
            switch (db.DB_TYPE.ToUpper())
            {
                case "DB2":
                    result = string.Format(SqlSelectIsTableWithOwnerExistsDB2, schema, tableName);
                    break;
                default:
                    result = string.Format(SqlSelectIsTableWithOwnerExists, schema, tableName);
                    break;
            }
            return result;
        }

        /// <summary>
        /// 生成查询表是否存在的SQL语句
        /// </summary>
        /// <param name="table">表名</param>
        /// <returns></returns>
        private string GetTableExistsCommand(string table)
        {
            string result;
            string schema = "";
            string tableName = GetTableName(table, out schema);

            switch (dbServer.DB_TYPE)
            {
                case "DB2":
                    result = string.Format(SqlSelectIsTableWithOwnerExistsDB2, schema, tableName);
                    break;
                default:
                    result = string.Format(SqlSelectIsTableWithOwnerExists, schema, tableName);
                    break;
            }
            return result;
        }

        /// <summary>
        /// 根据分割字符串
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns></returns>
        protected string[] SplitString(string input)
        {
            List<string> result = new List<string>();

            if (input != null)
            {
                foreach (string s in input.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    result.Add(s.Trim());
                }
            }
            return result.ToArray();
        }


        #region 获取时间格式

        /// <summary>
        /// 获取当前日期格式"yyyyMMdd"
        /// </summary>
        /// <param name="value">增加的天数，可为负数</param>
        /// <returns></returns>
        public string day(int value = 0)
        {
            return runData.AddDays(value).ToString("yyyyMMdd");
        }
        /// <summary>
        /// 获取当前月份格式"yyyyMM"
        /// </summary>
        /// <param name="value">增加的月份，可为负数</param>
        /// <returns></returns>
        public string month(int value = 0)
        {
            return runData.AddMonths(value).ToString("yyyyMM");
        }
        /// <summary>
        /// 获取当前年份格式"yyyy"
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string years(int value = 0)
        {
            return runData.AddYears(value).ToString("yyyy");
        }

        /// <summary>
        /// 月份最后一天
        /// </summary>
        /// <param name="value">当前月增加月份</param>
        /// <returns></returns>
        public string last_day(int value = 0)
        {
            DateTime temp = runData;
            temp = new DateTime(temp.Year, temp.Month, 1);
            return temp.AddMonths(value + 1).AddDays(-1).ToString("yyyyMMdd");
        }

        #endregion
    }
}
