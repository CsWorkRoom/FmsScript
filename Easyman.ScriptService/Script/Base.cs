using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.Log;
using Easyman.Librarys.DBHelper;
using System.Data;
using System.IO;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Net;
using System.Diagnostics;
using Easyman.Librarys;
using Easyman.Librarys.ApiRequest;
using System.Data.SqlClient;
using System.Collections;
using Easyman.Common;
using RemoteAccess;
using System.Reflection;

namespace Easyman.ScriptService.Script
{
    /// <summary>
    /// 脚本执行基类，包含了对界面自定义函数的定义与实现。
    /// 生成的脚本继承自本类
    /// </summary> 
    public class Base: RemoteLoaderFactory, IRemoteInterface
    {
        /// <summary>
        /// 脚本节点实例ID
        /// </summary>
        private long _scriptNodeCaseID = 0;
        /// <summary>
        /// 脚本执行中是否有错（如果有错，则提前中止）
        /// </summary>
        private bool _isError = false;
        /// <summary>
        /// 脚本执行中是否有预警
        /// </summary>
        private bool _isWarn = false;
        /// <summary>
        /// 错误信息
        /// </summary>
        private string _errorMessage = string.Empty;
        /// <summary>
        /// 预警信息
        /// </summary>
        private string _warnMessage = string.Empty;
        /// <summary>
        /// 数据库访问实体
        /// </summary>
        private BLL.EM_DB_SERVER.Entity _dbServer;

        /// <summary>
        /// 获取到的待执行的文件列表
        /// </summary>
        private List<long> _monitList;
        /// <summary>
        /// 下载文件的临时文件夹
        /// </summary>
        protected string _downDbPath = System.AppDomain.CurrentDomain.BaseDirectory + "\\UpFiles\\DownDB\\";

        /// <summary>
        /// 设置脚本节点实例ID
        /// </summary>
        /// <param name="scriptNodeCaseID">脚本节点实例ID</param>
        public void SetScriptNodeCaseID(long scriptNodeCaseID)
        {
            _scriptNodeCaseID = scriptNodeCaseID;
        }

        /// <summary>
        /// 获取到的待执行的文件列表
        /// </summary>
        /// <param name="scriptNodeCaseID">脚本节点实例ID</param>
        public void SetMonitFileList(List<long> monitList)
        {
            _monitList = monitList;
            //log(string.Format("1成功赋值文件列表：" + monitList != null ? string.Join(",", monitList) : "空"));
        }

        //调用动态函数
        public object Invoke(string strMethod, object[] Parameters)
        {
            return this.GetType().InvokeMember(strMethod, BindingFlags.InvokeMethod, null, this, Parameters);
        }

        /// <summary>
        /// 获取错误信息
        /// </summary>
        /// <returns></returns>
        public string GetErrorMessage()
        {
            return _errorMessage;
        }

        /// <summary>
        /// 获取预警信息
        /// </summary>
        /// <returns></returns>
        public string GetWarnMessage()
        {
            return _warnMessage;
        }

        /// <summary>
        /// 写错误信息
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        /// <param name="logLevel">日志等级</param>
        /// <param name="sql">SQL语句或脚本</param>
        protected void WriteErrorMessage(string errorMessage, int logLevel = 3, string sql = "")
        {
            _isError = true;
            _errorMessage = errorMessage + "\r\n本节点中的后续代码，除了写日志之外，将不再执行。";
            log(errorMessage, logLevel, sql);
        }

        protected void WriteWarnMessage(string warnMessage, int logLevel = 3, string sql = "")
        {
            _isWarn = true;
            _warnMessage = warnMessage;
            log(warnMessage, logLevel, sql);
        }

        /// <summary>
        /// 选定当前要运行的数据库
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        public bool setnowdb(string dbName)
        {
            if (_isError == true)
            {
                return false;
            }

            if (string.IsNullOrEmpty(dbName))
            {
                WriteErrorMessage("指定的数据库名不能为空", 2);
                return false;
            }

            _dbServer = BLL.EM_DB_SERVER.Instance.GetDbByName(dbName);

            if (_dbServer == null)
            {
                WriteErrorMessage("未找到数据库【" + dbName + "】", 2);
                return false;
            }

            log(string.Format("数据库已经成功切换为：【{0}】", dbName));

            return true;
        }

        /// <summary>
        /// 设置当前数据库byID
        /// </summary>
        /// <param name="dbid">数据库服务器ID</param>
        public bool setnowdbid(long? dbid)
        {
            if (_isError == true)
            {
                return false;
            }

            if (dbid == null || dbid < 0)
            {
                WriteErrorMessage("指定的数据库ID不能为空或小于0", 2);
                return false;
            }

            _dbServer = BLL.EM_DB_SERVER.Instance.GetEntityByKey<BLL.EM_DB_SERVER.Entity>(dbid);

            if (_dbServer == null)
            {
                WriteErrorMessage("未找到数据库【" + dbid + "】", 2);
                return false;
            }

            //log(string.Format("数据库已经成功切换为：【{0}】", dbid));

            return true;
        }


        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="message">日志内容</param>
        /// <param name="logLevel">日志等级</param>
        /// <param name="sql">SQL脚本</param>
        public bool log(string message, int logLevel = 4, string sql = "")
        {
            try
            {
                return BLL.EM_SCRIPT_NODE_CASE_LOG.Instance.Add(_scriptNodeCaseID, logLevel, message, sql) > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 写日志（默认日志等级为4级）
        /// </summary>
        /// <param name="message">日志内容</param>
        /// <param name="sql">SQL脚本</param>
        public bool log(string message, string sql)
        {
            return log(message, 4, sql);
        }

        /// <summary>
        /// 执行SQL语句
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>执行成功返回true，执行失败返回false</returns>
        public bool exec(string sql)
        {
            if (_isError == true)
            {
                return false;
            }

            return execute(sql);
        }

        /// <summary>
        /// 执行SQL语句
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>执行成功返回true，执行失败返回false</returns>
        public bool execute(string sql)
        {
            if (_isError == true)
            {
                return false;
            }

            //验证当前是否打开数据库
            if (_dbServer == null)
            {
                WriteErrorMessage(string.Format(@"未设置执行数据库，请在语句【{0}】之前指定数据库setnowdb(""数据库名"");", sql), 3);
                return false;
            }

            try
            {
                log(string.Format("将在【{1}】执行语句【{0}】", sql, _dbServer.BYNAME), sql);
                using (BDBHelper dbHelper = new BDBHelper(_dbServer.DB_TYPE, _dbServer.IP, _dbServer.PORT, _dbServer.USER, _dbServer.PASSWORD, _dbServer.DATA_CASE, _dbServer.DATA_CASE))
                {
                    dbHelper.ExecuteNonQuery(sql);
                }
                log("执行成功！", sql);
            }
            catch (Exception e)
            {
                WriteErrorMessage(string.Format("在【{0}】执行语句失败\r\n错误原因【{1}】", _dbServer.BYNAME, e.ToString()), 3, sql);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断数据库中表是否存在
        /// </summary>
        /// <param name="tableNames">表名，可以为多张表，表名以逗号分隔</param>
        /// <returns>所有表均存在则返回为true，反之返回false</returns>
        public bool is_table_exists(string tableNames)
        {
            if (_isError == true)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(tableNames))
            {
                WriteErrorMessage("表名为空，【is_table_exists】语句将不会查找任何表。", 3);
                return false;
            }

            //验证当前是否打开数据库
            if (_dbServer == null)
            {
                WriteErrorMessage(@"未设置执行数据库，请在调用函数【is_table_exists】之前指定数据库setnowdb(""数据库名"");", 3);
                return false;
            }

            try
            {
                using (BDBHelper dbHelper = new BDBHelper(_dbServer.DB_TYPE, _dbServer.IP, _dbServer.PORT, _dbServer.USER, _dbServer.PASSWORD, _dbServer.DATA_CASE, _dbServer.DATA_CASE))
                {
                    foreach (string tableName in tableNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string t = tableName;
                        if (t.Contains(".") == false)
                        {
                            t = _dbServer.USER + "." + tableName;
                        }

                        if (dbHelper.TableIsExists(t) == false)
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteErrorMessage(string.Format(@"在调用函数【is_table_exists】检查表【{0}】时出错，错误信息为：\r\n{1}", tableNames, ex.ToString()), 3);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 清空表
        /// </summary>
        /// <param name="tableNames">表名，可以为多张表，表名以逗号分隔</param>
        public bool truncate_table(string tableNames)
        {
            if (_isError == true)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(tableNames))
            {
                WriteErrorMessage("表名为空，【truncate_table】语句将不会删除任何表记录。", 3);
                return false;
            }

            //验证当前是否打开数据库
            if (_dbServer == null)
            {
                WriteErrorMessage(@"未设置执行数据库，请在调用函数【truncate_table】之前指定数据库setnowdb(""数据库名"");", 3);
                return false;
            }

            try
            {
                using (BDBHelper dbHelper = new BDBHelper(_dbServer.DB_TYPE, _dbServer.IP, _dbServer.PORT, _dbServer.USER, _dbServer.PASSWORD, _dbServer.DATA_CASE, _dbServer.DATA_CASE))
                {
                    foreach (string tableName in tableNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string t = tableName;
                        if (t.Contains(".") == false)
                        {
                            t = _dbServer.USER + "." + tableName;
                        }

                        if (dbHelper.TableIsExists(t))
                        {
                            if (dbHelper.Truncate(t) == false)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteErrorMessage(string.Format(@"在调用函数【truncate_table】清空表【{0}】所有记录时出错，错误信息为：\r\n【{1}】", tableNames, ex.ToString()), 3);
                return false;
            }

            log(string.Format(@"成功调用函数【truncate_table】清空表【{0}】所有记录", tableNames));
            return true;
        }

        /// <summary>
        /// 删除表
        /// </summary>
        /// <param name="tableNames">表名，可以为多张表，表名以逗号分隔</param>
        public bool drop_table(string tableNames)
        {
            if (_isError == true)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(tableNames))
            {
                WriteErrorMessage("表名为空，【drop_table】语句将不会删除任何表。", 3);
                return false;
            }

            //验证当前是否打开数据库
            if (_dbServer == null)
            {
                WriteErrorMessage(@"未设置执行数据库，请在调用函数【drop_table】之前指定数据库setnowdb(""数据库名"");", 3);
                return false;
            }

            try
            {
                using (BDBHelper dbHelper = new BDBHelper(_dbServer.DB_TYPE, _dbServer.IP, _dbServer.PORT, _dbServer.USER, _dbServer.PASSWORD, _dbServer.DATA_CASE, _dbServer.DATA_CASE))
                {
                    foreach (string tableName in tableNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string t = tableName;
                        if (t.Contains(".") == false)
                        {
                            t = _dbServer.USER + "." + tableName;
                        }

                        if (dbHelper.TableIsExists(t))
                        {
                            if (dbHelper.Drop(t) == false)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteErrorMessage(string.Format(@"在调用函数【drop_table】删除表【{0}】时出错，错误信息为：\r\n【{1}】", tableNames, ex.ToString()), 3);
                return false;
            }

            log(string.Format(@"成功调用函数【drop_table】删除表【{0}】", tableNames));

            return true;
        }

        /// <summary>
        /// 执行SQL语句，返回执行语句结果
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>返回执行语句结果</returns>
        public object execute_scalar(string sql)
        {
            if (_isError == true)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                WriteErrorMessage("SQL语句为空，【execute_scalar】语句将不会执行任何内容。", 3);
                return null;
            }

            //验证当前是否打开数据库
            if (_dbServer == null)
            {
                log(@"未设置执行数据库，请在调用函数【execute_scalar】之前指定数据库setnowdb(""数据库名"");", 3, sql);
                return null;
            }
            object obj;
            try
            {
                using (BDBHelper dbHelper = new BDBHelper(_dbServer.DB_TYPE, _dbServer.IP, _dbServer.PORT, _dbServer.USER, _dbServer.PASSWORD, _dbServer.DATA_CASE, _dbServer.DATA_CASE))
                {
                    obj = dbHelper.ExecuteScalar(sql);
                }
            }
            catch (Exception ex)
            {
                WriteErrorMessage(string.Format(@"在调用函数【execute_scalar】时出错，错误信息为：\r\n【{0}】", ex.ToString()), 3, sql);
                return null;
            }

            log(string.Format(@"成功调用函数【execute_scalar】"), 4, sql);
            return obj;
        }

        /// <summary>
        /// 查询数据并写到本地文件
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="fileName">文件名</param>
        /// <param name="pageSize">分页大小，每一页写一个文件</param>
        /// <returns>输出文件所在的路径</returns>
        public string down_db(string sql, string fileName, int pageSize = 1000000)
        {
            if (_isError == true)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                WriteErrorMessage("SQL语句为空，【down_db】语句将不会执行任何内容。", 3);
                return string.Empty;
            }

            //验证当前是否打开数据库
            if (_dbServer == null)
            {
                log(@"未设置执行数据库，请在调用函数【down_db】之前指定数据库setnowdb(""数据库名"");", 3, sql);
                return string.Empty;
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
                WriteErrorMessage(string.Format(@"导出数据到文件，在创建及清空目录【{0}】时出错，错误信息为：\r\n{1}", path, ex.ToString()), 3, sql);
                return string.Empty;
            }

            DataTable dt;
            int pageIndex = 0;
            int i = 0;
            DateTime begin = DateTime.Now;
            try
            {
                using (BDBHelper adbHelper = new BDBHelper(_dbServer.DB_TYPE, _dbServer.IP, _dbServer.PORT, _dbServer.USER, _dbServer.PASSWORD, _dbServer.DATA_CASE, _dbServer.DATA_CASE))
                {
                    //分页查询
                    while (true)
                    {
                        pageIndex++;
                        dt = adbHelper.ExecuteDataTablePage(sql, pageSize, pageIndex);

                        if (dt == null)
                        {
                            break;
                        }

                        if (pageIndex == 1)
                        {
                            //写建表脚本
                            string createFileName = path + fileName + "_Create.txt";
                            string createSql = adbHelper.MakeCreateTableSql(fileName, dt);
                            File.WriteAllText(createFileName, createSql, Encoding.UTF8);
                        }

                        if (dt.Rows.Count < 1)
                        {
                            break;
                        }

                        //写文件
                        string dataFileName = path + fileName + "_" + pageIndex + ".txt";
                        adbHelper.WriteDataTableIntoFile(dt, dataFileName);
                        i += dt.Rows.Count;
                        if (dt.Rows.Count < pageSize)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteErrorMessage(string.Format(@"在调用函数【down_db】时出错，错误信息为：\r\n【{0}】", ex.ToString()), 3, sql);
                return string.Empty;
            }

            TimeSpan ts = DateTime.Now - begin;
            log(string.Format(@"成功调用函数【down_db】,将查询结果写入【{0}】条记录到文件【{1}】，用时【{2}】毫秒（包含查询时间）", i, path, ts.TotalMilliseconds), 4, sql);
            return path;
        }


        /// <summary>
        /// 导出表写入文件并将文件压缩打包，生成的文件路径：~/UpFiles/DownDB/{tableName}.zip
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="zipFileName">压缩文件名（不包含后缀名）</param>
        /// <returns>文件全名,含路径</returns>
        public string down_db_to_file(string sql, string zipFileName)
        {
            if (_isError == true)
            {
                return string.Empty;
            }

            var path = down_db(sql, zipFileName);
            if (string.IsNullOrWhiteSpace(path))
            {
                WriteErrorMessage(string.Format(@"从数据库查询数据并写入径【{0}】失败，将不会生成zip压缩包。", path), 3, sql);
                return string.Empty;
            }

            var zipFileFullName = _downDbPath + zipFileName + ".zip";
            try
            {
                Librarys.Compress.BZip.ZipFileMain(path, zipFileFullName);
                log(string.Format("生成ZIP文件成功，文件名为【{0}】", zipFileFullName));
            }
            catch (Exception ex)
            {
                WriteErrorMessage(string.Format(@"压缩路径【{0}】下的文件失败，错误信息为：\r\n{1}", path, ex.ToString()), 3, sql);
                return string.Empty;
            }

            return zipFileFullName;
        }

        /// <summary>
        /// 数据库里面表到表的数据复制
        /// </summary>
        /// <param name="sql">SQL查询语句</param>
        /// <param name="destTableName">生成的目标表名</param>
        /// <param name="destDBName">导入的目的数据库</param>
        /// <param name="isCreatTable">0表示不创建表;1(默认值)表示自动创建表(在导数据之前，要删除已经存在表</param>
        /// <param name="pageSize">页面大小（分批查询导入，每批次记录条数，超过10万则会自动使用先写文件再导文件的方式）</param>
        /// <returns>复制记录条数</returns>
        public int down_db_to_db(string sql, string destTableName, string destDBName, int isCreatTable = 1, int pageSize = 50000)
        {
            if (_isError == true)
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                WriteErrorMessage("SQL语句为空，【down_db_to_db】语句将不会执行任何内容。", 3);
                return 0;
            }

            if (string.IsNullOrWhiteSpace(destTableName) || string.IsNullOrWhiteSpace(destDBName))
            {
                WriteErrorMessage("目标表名或目的数据库名为空，【down_db_to_db】语句将不会执行任何内容。", 3, sql);
                return 0;
            }

            //验证当前是否打开数据库
            if (_dbServer == null)
            {
                WriteErrorMessage(@"未设置执行数据库，请在调用函数【down_db_to_db】之前指定数据库setnowdb(""数据库名"");");
                return 0;
            }
            //获取目的数据库
            BLL.EM_DB_SERVER.Entity destDBServer = BLL.EM_DB_SERVER.Instance.GetDbByName(destDBName);
            //验证当前是否打开数据库
            if (destDBServer == null)
            {
                WriteErrorMessage(string.Format(@"目的数据库【{0}】不存在，【down_db_to_db】将不会进行任何操作;", destDBName));
                return 0;
            }

            DataTable dt;
            int pageIndex = 0;
            int i = 0;
            DateTime begin = DateTime.Now;
            DateTime bg = DateTime.Now;
            TimeSpan tq = bg - begin;
            TimeSpan ti = bg - begin;
            try
            {
                if (destTableName.IndexOf('.') <= 0)
                {
                    destTableName = destDBServer.USER + "." + destTableName;
                }

                using (BDBHelper dbHelperDest = new BDBHelper(destDBServer.DB_TYPE, destDBServer.IP, destDBServer.PORT, destDBServer.USER, destDBServer.PASSWORD, destDBServer.DATA_CASE, destDBServer.DATA_CASE))
                {
                    bool isNeedCreateTable = false;
                    if (dbHelperDest.TableIsExists(destTableName) == true)
                    {
                        //创建新表
                        if (isCreatTable == 1)
                        {
                            isNeedCreateTable = true;
                            log(string.Format(@"将删除目的数据库【{0}】的表【{1}】", destDBName, destTableName), 4);
                            try
                            {
                                dbHelperDest.Drop(destTableName);
                            }
                            catch (Exception e)
                            {
                                WriteErrorMessage(string.Format(@"删除目的数据库【{0}】的表【{1}】出错，错误信息为：\r\n【{2}】", destDBName, destTableName, e.ToString()), 3);
                                return 0;
                            }
                        }
                    }
                    else
                    {
                        isNeedCreateTable = true;
                    }

                    //查询
                    using (BDBHelper dbHelper = new BDBHelper(_dbServer.DB_TYPE, _dbServer.IP, _dbServer.PORT, _dbServer.USER, _dbServer.PASSWORD, _dbServer.DATA_CASE, _dbServer.DATA_CASE))
                    {
                        while (Main.IsRun)
                        {
                            pageIndex++;
                            bg = DateTime.Now;
                            dt = dbHelper.ExecuteDataTablePage(sql, pageSize, pageIndex);
                            tq = DateTime.Now - bg;

                            if (dt == null || dt.Rows.Count < 1)
                            {
                                break;
                            }

                            //创建表
                            if (isNeedCreateTable == true)
                            {
                                isNeedCreateTable = false;
                                bool isCreatedTable = false;
                                try
                                {
                                    isCreatedTable = dbHelperDest.CreateTableFromDataTable(destTableName, dt);
                                }
                                catch (Exception ex)
                                {
                                    _errorMessage = ex.ToString();
                                }

                                if (isCreatedTable == true)
                                {
                                    log(string.Format(@"已在目的数据库【{0}】根据查询结果创建表【{1}】", destDBName, destTableName), 4);
                                }
                                else
                                {
                                    WriteErrorMessage(string.Format(@"在目的数据库【{0}】根据查询结果创建表【{1}】失败，错误信息：【{2}】\r\n", destDBName, destTableName, _errorMessage), 3);
                                    return 0;
                                }
                            }

                            try
                            {
                                //导入
                                bg = DateTime.Now;
                                int n = 0;
                                if (dt.Rows.Count < 100000)
                                {
                                    n = dbHelperDest.LoadDataInDataTable(destTableName, dt);
                                }
                                else
                                {
                                    n = dbHelperDest.LoadDataInDataTableWithFile(destTableName, dt);
                                }

                                ti = DateTime.Now - bg;
                                i += n;
                                log(string.Format(@"将第【{0}】页查询结果【{1}】条记录导入到目的数据库【{2}】表【{3}】成功导入【{4}】条，其中，查询用时【{5}】毫秒，导入用时【{6}】毫秒，已经累计导入【{7}】条", pageIndex, dt.Rows.Count, destDBName, destTableName, n, tq.TotalMilliseconds, ti.TotalMilliseconds, i), 4);
                            }
                            catch (Exception ee)
                            {
                                WriteErrorMessage(string.Format(@"将第【{0}】页查询结果导入到目的数据库【{1}】表【{2}】失败，错误信息：\r\n【{3}】", pageIndex, destDBName, destTableName, ee.ToString()), 3, sql);
                                break;
                            }

                            if (dt.Rows.Count < pageSize)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteErrorMessage(string.Format(@"在调用函数【down_db_to_db】时出错，错误信息为：\r\n【{0}】", ex.ToString()), 3, sql);
                return 0;
            }
            TimeSpan ts = DateTime.Now - begin;
            log(string.Format(@"成功调用函数【down_db_to_db】,将查询结果导入【{0}】条记录到表【{1}】，用时【{2}】毫秒（包含查询时间）", i, destTableName, ts.TotalMilliseconds), 4, sql);

            return i;
        }

        /// <summary>
        /// 使用异步方式将查询结果写入指定表
        /// </summary>
        /// <param name="sql">SQL查询语句</param>
        /// <param name="destTableName">生成的目标表名</param>
        /// <param name="destDBName">导入的目的数据库</param>
        /// <param name="isCreatTable">0表示不创建表;1(默认值)表示自动创建表(在导数据之前，要删除已经存在表</param>
        /// <param name="pageSize">页面大小（分批查询导入，每批次记录条数，超过10万则会自动使用先写文件再导文件的方式）</param>
        /// <returns>复制记录条数</returns>
        public int down_db_to_db_async(string sql, string destTableName, string destDBName, int isCreatTable = 1, int pageSize = 50000)
        {
            if (_isError == true)
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                WriteErrorMessage("SQL语句为空，【down_db_to_db】语句将不会执行任何内容。", 3);
                return 0;
            }

            if (string.IsNullOrWhiteSpace(destTableName) || string.IsNullOrWhiteSpace(destDBName))
            {
                WriteErrorMessage("目标表名或目的数据库名为空，【down_db_to_db】语句将不会执行任何内容。", 3, sql);
                return 0;
            }

            //验证当前是否打开数据库
            if (_dbServer == null)
            {
                WriteErrorMessage(@"未设置执行数据库，请在调用函数【down_db_to_db】之前指定数据库setnowdb(""数据库名"");");
                return 0;
            }
            //获取目的数据库
            BLL.EM_DB_SERVER.Entity destDBServer = BLL.EM_DB_SERVER.Instance.GetDbByName(destDBName);
            //验证当前是否打开数据库
            if (destDBServer == null)
            {
                WriteErrorMessage(string.Format(@"目的数据库【{0}】不存在，【down_db_to_db】将不会进行任何操作;", destDBName));
                return 0;
            }

            int pageIndex = 0;
            _isAsyncLoadQueryEnd = false;
            _asyncLoadRowsCount = 0;
            bool isNeedCreateTable = false;

            DateTime begin = DateTime.Now;
            DateTime bg = DateTime.Now;
            TimeSpan tq = bg - begin;
            TimeSpan ti = bg - begin;
            try
            {
                if (destTableName.IndexOf('.') <= 0)
                {
                    destTableName = destDBServer.USER + "." + destTableName;
                }

                _bwAsyncLoadTask = new BackgroundWorker();
                _bwAsyncLoadTask.WorkerSupportsCancellation = true;
                _bwAsyncLoadTask.DoWork += AsyncLoadTask_DoWork;
                _bwAsyncLoadTask.RunWorkerAsync();

                //查询
                using (BDBHelper dbHelper = new BDBHelper(_dbServer.DB_TYPE, _dbServer.IP, _dbServer.PORT, _dbServer.USER, _dbServer.PASSWORD, _dbServer.DATA_CASE, _dbServer.DATA_CASE))
                {
                    while (Main.IsRun)
                    {
                        pageIndex++;
                        //BLog.Write(BLog.LogLevel.DEBUG, "将查询第" + pageIndex + "页，并异步将结果导入到表：" + destTableName);
                        bg = DateTime.Now;
                        DataTable dt = dbHelper.ExecuteDataTablePage(sql, pageSize, pageIndex);
                        tq = DateTime.Now - bg;
                        //BLog.Write(BLog.LogLevel.DEBUG, "已经查询第" + pageIndex + "页，并异步将结果导入到表：" + destTableName);

                        if (dt == null || dt.Rows.Count < 1)
                        {
                            break;
                        }
                        //移除分页所用的字段
                        dt.Columns.Remove("BROW_NUM");

                        log(string.Format(@"查询第【{0}】页，有【{1}】条记录，查询用时【{2}】毫秒，稍后将异步导入到数据库【{3}】表【{4}】中。", pageIndex, dt.Rows.Count, tq.TotalMilliseconds, destDBName, destTableName), 4);

                        //第一页将有可能需要建表
                        if (pageIndex == 1)
                        {
                            using (BDBHelper dbHelperDest = new BDBHelper(destDBServer.DB_TYPE, destDBServer.IP, destDBServer.PORT, destDBServer.USER, destDBServer.PASSWORD, destDBServer.DATA_CASE, destDBServer.DATA_CASE))
                            {
                                if (dbHelperDest.TableIsExists(destTableName) == true)
                                {
                                    //创建新表
                                    if (isCreatTable == 1)
                                    {
                                        isNeedCreateTable = true;
                                        log(string.Format(@"将删除目的数据库【{0}】的表【{1}】", destDBName, destTableName), 4);
                                        try
                                        {
                                            dbHelperDest.Drop(destTableName);
                                        }
                                        catch (Exception e)
                                        {
                                            WriteErrorMessage(string.Format(@"删除目的数据库【{0}】的表【{1}】出错，错误信息为：\r\n【{2}】", destDBName, destTableName, e.ToString()), 3);
                                            return 0;
                                        }
                                    }
                                }
                                else
                                {
                                    isNeedCreateTable = true;
                                }
                                //创建表
                                if (isNeedCreateTable == true)
                                {
                                    isNeedCreateTable = false;
                                    bool isCreatedTable = false;
                                    try
                                    {
                                        isCreatedTable = dbHelperDest.CreateTableFromDataTable(destTableName, dt);
                                    }
                                    catch (Exception ex)
                                    {
                                        _errorMessage = ex.ToString();
                                    }

                                    if (isCreatedTable == true)
                                    {
                                        log(string.Format(@"已在目的数据库【{0}】根据查询结果创建表【{1}】", destDBName, destTableName), 4);
                                    }
                                    else
                                    {
                                        WriteErrorMessage(string.Format(@"在目的数据库【{0}】根据查询结果创建表【{1}】失败，错误信息：【{2}】\r\n", destDBName, destTableName, _errorMessage), 3);
                                        return 0;
                                    }
                                }
                            }
                        }

                        //异步导入
                        LoadDataAsync(destDBServer, destTableName, dt, pageIndex);

                        //避免过多数据压到内存
                        while (_queueAsyncLoadTask.Count > 3)
                        {
                            //BLog.Write(BLog.LogLevel.DEBUG, "异步导入：已经有【" + _queueAsyncLoadTask.Count + "】页数据在等待导入中，导入速度慢于查询速度，将暂停查询。");
                            Thread.Sleep(1000);
                            continue;
                        }

                        if (dt.Rows.Count < pageSize)
                        {
                            break;
                        }
                    }
                }
                //查询完毕
                _isAsyncLoadQueryEnd = true;

                //避免永远等待
                DateTime waiteEnd = DateTime.Now.AddMinutes(10);
                //等待执行完成
                while (_queueAsyncLoadTask.Count > 0 && _isAsyncLoadWriteEnd == false && DateTime.Now < waiteEnd)
                {
                    Thread.Sleep(100);
                    continue;
                }
                _bwAsyncLoadTask.CancelAsync();
                _bwAsyncLoadTask.Dispose();

                TimeSpan ts = DateTime.Now - begin;
                log(string.Format(@"成功调用函数【down_db_to_db】,将查询结果异步导入【{0}】条记录到表【{1}】，用时【{2}】毫秒（包含查询时间）", _asyncLoadRowsCount, destTableName, ts.TotalMilliseconds), 4, sql);

                return _asyncLoadRowsCount;
            }
            catch (Exception ex)
            {
                WriteErrorMessage(string.Format(@"在调用函数【down_db_to_db】时出错，错误信息为：\r\n【{0}】", ex.ToString()), 3, sql);
                return 0;
            }
        }

        /// <summary>
        /// 使用流的方式查询并将结果异步写入指定表
        /// </summary>
        /// <param name="sql">SQL查询语句</param>
        /// <param name="destTableName">生成的目标表名</param>
        /// <param name="destDBName">导入的目的数据库</param>
        /// <param name="isCreatTable">0表示不创建表;1(默认值)表示自动创建表(在导数据之前，要删除已经存在表</param>
        /// <param name="pageSize">页面大小（分批查询导入，每批次记录条数，超过10万则会自动使用先写文件再导文件的方式）</param>
        /// <returns>复制记录条数</returns>
        public int down_db_to_db_flow(string sql, string destTableName, string destDBName, int isCreatTable = 1, int pageSize = 50000)
        {
            if (_isError == true)
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                WriteErrorMessage("SQL语句为空，【down_db_to_db】语句将不会执行任何内容。", 3);
                return 0;
            }

            if (string.IsNullOrWhiteSpace(destTableName) || string.IsNullOrWhiteSpace(destDBName))
            {
                WriteErrorMessage("目标表名或目的数据库名为空，【down_db_to_db】语句将不会执行任何内容。", 3, sql);
                return 0;
            }

            //验证当前是否打开数据库
            if (_dbServer == null)
            {
                WriteErrorMessage(@"未设置执行数据库，请在调用函数【down_db_to_db】之前指定数据库setnowdb(""数据库名"");");
                return 0;
            }
            //获取目的数据库
            BLL.EM_DB_SERVER.Entity destDBServer = BLL.EM_DB_SERVER.Instance.GetDbByName(destDBName);
            //验证当前是否打开数据库
            if (destDBServer == null)
            {
                WriteErrorMessage(string.Format(@"目的数据库【{0}】不存在，【down_db_to_db】将不会进行任何操作;", destDBName));
                return 0;
            }

            DataTable dt = new DataTable();
            //表结构
            DataTable dtSchema = new DataTable();
            int pageIndex = 0;
            int readRowsCount = 0;
            _isAsyncLoadQueryEnd = false;
            _asyncLoadRowsCount = 0;
            bool isNeedCreateTable = false;

            DateTime begin = DateTime.Now;
            DateTime bg = DateTime.Now;
            TimeSpan tq = bg - begin;
            TimeSpan ti = bg - begin;
            try
            {
                if (destTableName.IndexOf('.') <= 0)
                {
                    destTableName = destDBServer.USER + "." + destTableName;
                }

                _bwAsyncLoadTask = new BackgroundWorker();
                _bwAsyncLoadTask.WorkerSupportsCancellation = true;
                _bwAsyncLoadTask.DoWork += AsyncLoadTask_DoWork;
                _bwAsyncLoadTask.RunWorkerAsync();

                //查询
                using (BDBHelper dbHelper = new BDBHelper(_dbServer.DB_TYPE, _dbServer.IP, _dbServer.PORT, _dbServer.USER, _dbServer.PASSWORD, _dbServer.DATA_CASE, _dbServer.DATA_CASE))
                {
                    using (IDataReader reader = dbHelper.ExecuteReader(sql))
                    {
                        //设置表结构
                        for (int c = 0; c < reader.FieldCount; c++)
                        {
                            dt.Columns.Add(reader.GetName(c), reader.GetFieldType(c));
                        }

                        bool isCanRead = reader.IsClosed == false && reader.Read();

                        int i = 0;
                        bg = DateTime.Now;
                        //遍历记录
                        while (Main.IsRun && reader.IsClosed == false && isCanRead)
                        {
                            if (_isError == true)
                            {
                                BLog.Write(BLog.LogLevel.WARN, "流式查询过程中程序出现错误而停止导入，已经查询" + pageIndex + "页，" + readRowsCount + "条记录");
                                break;
                            }
                            //赋值
                            DataRow dr = dt.NewRow();
                            for (int c = 0; c < reader.FieldCount; c++)
                            {
                                dr[c] = reader.GetValue(c);
                            }
                            dt.Rows.Add(dr);

                            i++;
                            readRowsCount++;
                            isCanRead = reader.Read();

                            if (i >= pageSize || isCanRead == false)
                            {
                                tq = DateTime.Now - bg;
                                pageIndex++;
                                log(string.Format(@"使用流的方式查询第【{0}】页，有【{1}】条记录，查询用时【{2}】毫秒，稍后将异步导入到数据库【{3}】表【{4}】中。", pageIndex, dt.Rows.Count, tq.TotalMilliseconds, destDBName, destTableName), 4);

                                //第一页将有可能需要建表
                                if (pageIndex == 1)
                                {
                                    using (BDBHelper dbHelperDest = new BDBHelper(destDBServer.DB_TYPE, destDBServer.IP, destDBServer.PORT, destDBServer.USER, destDBServer.PASSWORD, destDBServer.DATA_CASE, destDBServer.DATA_CASE))
                                    {
                                        if (dbHelperDest.TableIsExists(destTableName) == true)
                                        {
                                            //创建新表
                                            if (isCreatTable == 1)
                                            {
                                                isNeedCreateTable = true;
                                                log(string.Format(@"将删除目的数据库【{0}】的表【{1}】", destDBName, destTableName), 4);
                                                try
                                                {
                                                    dbHelperDest.Drop(destTableName);
                                                }
                                                catch (Exception e)
                                                {
                                                    WriteErrorMessage(string.Format(@"删除目的数据库【{0}】的表【{1}】出错，错误信息为：\r\n【{2}】", destDBName, destTableName, e.ToString()), 3);
                                                    return 0;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            isNeedCreateTable = true;
                                        }
                                        //创建表
                                        if (isNeedCreateTable == true)
                                        {
                                            isNeedCreateTable = false;
                                            bool isCreatedTable = false;
                                            try
                                            {
                                                isCreatedTable = dbHelperDest.CreateTableFromDataTable(destTableName, dt);
                                            }
                                            catch (Exception ex)
                                            {
                                                _errorMessage = ex.ToString();
                                            }

                                            if (isCreatedTable == true)
                                            {
                                                log(string.Format(@"已在目的数据库【{0}】根据查询结果创建表【{1}】", destDBName, destTableName), 4);
                                            }
                                            else
                                            {
                                                WriteErrorMessage(string.Format(@"在目的数据库【{0}】根据查询结果创建表【{1}】失败，错误信息：【{2}】\r\n", destDBName, destTableName, _errorMessage), 3);
                                                return 0;
                                            }
                                        }
                                    }
                                }

                                //异步导入
                                LoadDataAsync(destDBServer, destTableName, dt, pageIndex);

                                //避免过多数据压到内存
                                while (_queueAsyncLoadTask.Count > 3)
                                {
                                    BLog.Write(BLog.LogLevel.DEBUG, "异步导入：已经有【" + _queueAsyncLoadTask.Count + "】页数据在等待导入中，导入速度慢于查询速度，将暂停查询。");
                                    Thread.Sleep(1000);
                                    continue;
                                }

                                dt.Rows.Clear();
                                i = 0;

                                bg = DateTime.Now;
                            }

                            if (isCanRead == false)
                            {
                                break;
                            }
                        }
                    }
                }
                //查询完毕
                _isAsyncLoadQueryEnd = true;
                //避免永远等待
                DateTime waiteEnd = DateTime.Now.AddMinutes(10);
                //等待执行完成
                while (_queueAsyncLoadTask.Count > 0 && _isAsyncLoadWriteEnd == false && DateTime.Now < waiteEnd)
                {
                    Thread.Sleep(100);
                    continue;
                }
                _bwAsyncLoadTask.CancelAsync();
                _bwAsyncLoadTask.Dispose();

                TimeSpan ts = DateTime.Now - begin;
                log(string.Format(@"成功调用函数【down_db_to_db】,将查询结果异步导入【{0}】条记录到表【{1}】，用时【{2}】毫秒（包含查询时间）", _asyncLoadRowsCount, destTableName, ts.TotalMilliseconds), 4, sql);

                return _asyncLoadRowsCount;
            }
            catch (Exception ex)
            {
                WriteErrorMessage(string.Format(@"在调用函数【down_db_to_db】时出错，错误信息为：\r\n【{0}】", ex.ToString()), 3, sql);
                return 0;
            }
        }

        /// <summary>
        /// 异步导入数据任务
        /// </summary>
        protected class AsyncLoadTask
        {
            public string DestTableName;
            public BLL.EM_DB_SERVER.Entity DestDBServer;
            public DataTable Table;
            public int PageIndex;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="destDBServer"></param>
            /// <param name="destTableName"></param>
            /// <param name="dt"></param>
            /// <param name="pageIndex"></param>
            public AsyncLoadTask(BLL.EM_DB_SERVER.Entity destDBServer, string destTableName, DataTable dt, int pageIndex)
            {
                DestTableName = destTableName;
                DestDBServer = destDBServer;
                //需要复制一份
                Table = dt.Clone();
                foreach (DataRow dr in dt.Rows)
                {
                    Table.ImportRow(dr);
                }
                PageIndex = pageIndex;
            }
        }

        private bool _isAsyncLoadQueryEnd = false;
        private bool _isAsyncLoadWriteEnd = false;
        private int _asyncLoadRowsCount = 0;
        private ConcurrentQueue<AsyncLoadTask> _queueAsyncLoadTask = new ConcurrentQueue<AsyncLoadTask>();
        private BackgroundWorker _bwAsyncLoadTask;

        /// <summary>
        /// 后台线程不断处理队列中的数据，将其导入到数据库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AsyncLoadTask_DoWork(object sender, DoWorkEventArgs e)
        {
            AsyncLoadTask asyncTask;
            int pageCount = 0;
            while (Main.IsRun)
            {
                if (_isError == true)
                {
                    BLog.Write(BLog.LogLevel.WARN, "异步导入数据过程中程序出现错误而停止导入，已经导入" + pageCount + "页，" + _asyncLoadRowsCount + "条记录");
                    return;
                }

                if (_queueAsyncLoadTask.Count < 1)
                {
                    if (_isAsyncLoadQueryEnd == true)
                    {
                        BLog.Write(BLog.LogLevel.DEBUG, "异步导入数据结束，共有" + pageCount + "页，" + _asyncLoadRowsCount + "条记录");
                        _isAsyncLoadWriteEnd = true;
                        return;
                    }

                    Thread.Sleep(100);
                    continue;
                }

                if (_queueAsyncLoadTask.TryDequeue(out asyncTask) == false)
                {
                    Thread.Sleep(100);
                    continue;
                }

                pageCount = asyncTask.PageIndex;
                //BLog.Write(BLog.LogLevel.DEBUG, string.Format("准备将异步导入第【{0}】页数据，到表【{1}】，还有【{2}】页等待导入。", asyncTask.PageIndex, asyncTask.DestTableName, _queueAsyncLoadTask.Count));
                try
                {
                    using (BDBHelper dbHelperDest = new BDBHelper(asyncTask.DestDBServer.DB_TYPE, asyncTask.DestDBServer.IP, asyncTask.DestDBServer.PORT, asyncTask.DestDBServer.USER, asyncTask.DestDBServer.PASSWORD, asyncTask.DestDBServer.DATA_CASE, asyncTask.DestDBServer.DATA_CASE))
                    {
                        //导入
                        DateTime bg = DateTime.Now;
                        int n = 0;
                        if (asyncTask.Table.Rows.Count < 100000)
                        {
                            n = dbHelperDest.LoadDataInDataTable(asyncTask.DestTableName, asyncTask.Table);
                        }
                        else
                        {
                            n = dbHelperDest.LoadDataInDataTableWithFile(asyncTask.DestTableName, asyncTask.Table);
                        }

                        TimeSpan ti = DateTime.Now - bg;
                        _asyncLoadRowsCount += n;
                        log(string.Format(@"第【{0}】页查询结果【{1}】条记录，已经异步导入到目的数据库【{2}】表【{3}】成功导入【{4}】条，导入用时【{5}】毫秒，已经累计导入【{6}】条，还有【{7}】页等待导入中", asyncTask.PageIndex, asyncTask.Table.Rows.Count, asyncTask.DestDBServer.BYNAME, asyncTask.DestTableName, n, ti.TotalMilliseconds, _asyncLoadRowsCount, _queueAsyncLoadTask.Count), 4);
                    }
                }
                catch (Exception ee)
                {
                    WriteErrorMessage(string.Format(@"将第【{0}】页查询结果异步导入到目的数据库【{1}】表【{2}】失败，内存中还有【{3}】页未导入，错误信息：\r\n【{4}】", asyncTask.PageIndex, asyncTask.DestDBServer.BYNAME, asyncTask.DestTableName, _queueAsyncLoadTask.Count, ee.ToString()), 3);
                    break;
                }
            }
        }

        /// <summary>
        /// 添加待处理数据到队列，如果等待数据过多，则返回false提示需要等待
        /// </summary>
        /// <param name="destDBServer"></param>
        /// <param name="destTableName"></param>
        /// <param name="dt"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        private void LoadDataAsync(BLL.EM_DB_SERVER.Entity destDBServer, string destTableName, DataTable dt, int pageIndex)
        {
            _queueAsyncLoadTask.Enqueue(new AsyncLoadTask(destDBServer, destTableName, dt, pageIndex));
        }

        /// <summary>
        /// 根据DataTable创建表
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public bool CreateTableFromDataTable(string tableName, DataTable dt)
        {
            if (_isError == true)
            {
                return false;
            }

            //验证当前是否打开数据库
            if (_dbServer == null)
            {
                log(@"未设置执行数据库，请在调用函数【CreateTableFromDataTable】之前指定数据库setnowdb(""数据库名"");", 3);
                return false;
            }

            bool ret = false;
            try
            {
                using (BDBHelper dbHelper = new BDBHelper(_dbServer.DB_TYPE, _dbServer.IP, _dbServer.PORT, _dbServer.USER, _dbServer.PASSWORD, _dbServer.DATA_CASE, _dbServer.DATA_CASE))
                {
                    ret = dbHelper.CreateTableFromDataTable(tableName, dt);
                }
            }
            catch (Exception ex)
            {
                WriteErrorMessage(string.Format(@"在调用函数【CreateTableFromDataTable】时出错，错误信息为：\r\n【{0}】", ex.ToString()), 3);
                return false;
            }

            log(string.Format(@"成功调用函数【CreateTableFromDataTable】"), 4);
            return ret;
        }

        #region 常用函数定义

        /// <summary>
        /// 获取基准时间的日期，精确到天
        /// </summary>
        /// <param name="days">偏移时间天数，默认为0，即当天日期</param>
        /// <returns>格式yyyyMMdd</returns>
        public static string day(int days = 0)
        {
            return DateTime.Today.AddDays(days).ToString("yyyyMMdd");
        }

        /// <summary>
        /// 获取日期为当月的第几天
        /// </summary>
        /// <param name="days">偏移时间天数，默认为0，即当天日期的号数</param>
        /// <returns>即当天日期的号数</returns>
        public static int day_of_month(int days = 0)
        {
            return DateTime.Today.AddDays(days).Day;
        }

        /// <summary>
        /// 获取日期为当月的第几天
        /// </summary>
        /// <param name="days">偏移时间天数，默认为0，即当天日期的号数</param>
        /// <returns>即当天日期的号数，两位的字符串</returns>
        public static string day_of_month2(int days = 0)
        {
            return DateTime.Today.AddDays(days).Day.ToString("00");
        }

        /// <summary>
        /// 获取基准时间当月的最后一天的日期
        /// </summary>
        /// <param name="months">
        /// 输入参数有三种情况：
        /// 1.参数为空时，获得当前月最后一天的值
        /// 2.参数为N时，获得的时间是当前月向后偏移N月的最后一天的值
        /// 3.参数为-N时，获得的时间是当前月向后偏移N月的最后一天的值
        /// </param>
        /// <returns>yyyyMMdd格式的字符串</returns>
        public static string last_day(int months = 0)
        {
            DateTime date = DateTime.Today.AddMonths(months + 1);
            return date.AddDays(-date.Day).ToString("yyyyMMdd");
        }

        /// <summary>
        /// 获取基准时间的日期，精确到月
        /// </summary>
        /// <param name="months">
        /// 输入参数有三种情况：
        /// 1.参数为空时，获得当前月的值
        /// 2.参数为N时，获得的时间是当前月向后偏移N月后的值
        /// 3.参数为-N时，获得的时间是当前月向后偏移N月后的值
        /// </param>
        /// <returns>yyyyMM格式的年月值</returns>
        public static string month(int months = 0)
        {
            return DateTime.Today.AddMonths(months).ToString("yyyyMM");
        }

        /// <summary>
        /// 获取基准时间中当年的第几个月
        /// </summary>
        /// <param name="months">
        /// 输入参数有三种情况：
        /// 1.参数为空时，获得当前月的值
        /// 2.参数为N时，获得的时间是当前月向后偏移N月后的值
        /// 3.参数为-N时，获得的时间是当前月向后偏移N月后的值
        /// </param>
        /// <returns>月</returns>
        public static int month_of_year(int months = 0)
        {
            return DateTime.Today.AddMonths(months).Month;
        }

        /// <summary>
        /// 获取基准时间中当年的第几个月
        /// </summary>
        /// <param name="months">
        /// 输入参数有三种情况：
        /// 1.参数为空时，获得当前月的值
        /// 2.参数为N时，获得的时间是当前月向后偏移N月后的值
        /// 3.参数为-N时，获得的时间是当前月向后偏移N月后的值
        /// </param>
        /// <returns>月（使用两位数字返回）</returns>
        public static string month_of_year2(int months = 0)
        {
            return DateTime.Today.AddMonths(months).Month.ToString("00");
        }

        /// <summary>
        /// 获取基准时间的日期，精确到年
        /// </summary>
        /// <param name="years">
        /// 输入参数有三种情况：
        /// 1.参数为空时，获得当前时间的年份
        /// 2.参数为N时，获得的时间是当前年份向后偏移N年后的值
        /// 3.参数为-N时，获得的时间是当前年份向前偏移N年后的值
        /// </param>
        /// <returns>年份（四位数字）</returns>
        public static int year(int years)
        {
            return DateTime.Today.Year + years;
        }

        #endregion

        #region 文件监控

        public void MonitorStart(string ip, string folderName)
        {


            log(string.Format(@"开启对{0}的{1}文件夹监控...", ip, folderName));
            string url = Librarys.Config.BConfig.GetConfigToString("MonitServiceIP");
            string postData = string.Format("ip={0}&folderName={1}&scriptNodeCaseId={2}", ip, folderName, _scriptNodeCaseID.ToString());
            //log("参数说明:" + postData);//20180701注释
            string surl = url + (postData == "" ? "" : "?") + postData;
            //log("访问路径:" + surl);//20180701注释
            var mess = Request.GetHttp(url, postData);
            if (mess.Contains("结果:false"))
            {
                WriteErrorMessage(mess, 3);
            }
            else if (mess.Contains("结果:warn"))
            {
                WriteWarnMessage(mess, 3);
            }
            else
            {
                log(string.Format(@"对{0}的{1}文件夹监控结果:{2}", ip, folderName, mess));
            }

        }
        #endregion

        #region 数据库操作

        public bool DatabaseBackupOrcale(string cmdStr)
        {
            return ExeCmd(cmdStr);
        }

        public bool DatabaseRestoreOracle(string cmdStr)
        {
            return ExeCmd(cmdStr);
        }

        private bool ExeCmd(string cmdStr)
        {
            try
            {
                log(string.Format(@"装载命令:{0}", cmdStr));
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe"; //bat文件路径        

                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;// 不创建新窗口 
                p.StartInfo.RedirectStandardInput = true;// 重定向输入
                p.StartInfo.RedirectStandardOutput = true;// 重定向标准输出  
                p.Start();
                log(string.Format(@"执行命令", cmdStr));
                //如果调用程序路径中有空格时，cmd命令执行失败，可以用双引号括起来 ，在这里两个引号表示一个引号（转义）
                string str = string.Format(@"{0} {1}", cmdStr, "&exit");

                p.StandardInput.WriteLine(str);
                StreamReader reader = p.StandardOutput;//截取输出流

                string line = reader.ReadLine();//每次读取一行
                StringBuilder logBat = new StringBuilder();
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (line != "")
                    {
                        logBat.AppendLine(line);
                    }

                }

                p.WaitForExit();//启用则以同步方式执行命令
                p.Close();
                log("执行结果:" + logBat.ToString());
                return true;
            }
            catch (Exception ex)
            {
                WriteErrorMessage("执行命令异常:" + ex.Message, 3);
                return false;
            }
        }


        /// <summary>
        /// sqlserver备份
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <param name="database"></param>
        public void DatabaseBackupSqlServer(string ip, string user, string pwd, string database, string folder)
        {
            // string masterIp = Librarys.Config.BConfig.GetConfigToString("MasterServiceIP");
            string ymd = DateTime.Now.ToString("yyyyMMdd");
            // string cmdText = string.Format(@"backup database {0} to disk='{1}\{0}_{2}_{3}.bak' with INIT", database, masterIp, ip, ymd);
            string filter = "DataBase$$lcz$$cs";
            string fileName = string.Format("{0}_{1}_{2}_{3}", database, ip, ymd, filter);
            string cmdText = string.Format(@"backup database {0} to disk='\\127.0.0.1\{1}\{2}.bak' with INIT", database, folder, fileName);
            BakReductSql(cmdText, true, ip, user, pwd, database);


            string url = Librarys.Config.BConfig.GetConfigToString("DataBaseUpFileIP");
            string postData = string.Format("ip={0}&folderName={1}&fileName={2}", ip, folder, fileName);
            var mess = Request.GetHttp(url, postData);
            if (mess.Contains("false"))
            {
                WriteErrorMessage(mess, 3);
            }
            else
            {
                log(string.Format(@"SqlServer:对{0}的{1}数据库备份结果:{2}", ip, database, mess));
            }

        }
        /// <summary>
        /// 数据库还原
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <param name="database"></param>
        public void DatabaseRestoreSqlServer(string ip, string user, string pwd, string database, string yearmonthday, string folder)
        {
            //string masterIp = Librarys.Config.BConfig.GetConfigToString("MasterServiceIP");
            // string cmdText = string.Format(@"restore  database {0} from disk='{1}\{0}_{2}_{3}.bak' With Replace", database, masterIp,ip, yearmonthday);
            string ymd = DateTime.Now.ToString("yyyyMMdd");
            string filter = "DataBase$$lcz$$cs";
            string fileName = string.Format("{0}_{1}_{2}_{3}", database, ip, ymd, filter);
            string url = Librarys.Config.BConfig.GetConfigToString("DataBaseDownFileIP");
            string postData = string.Format("ip={0}&folderName={1}&fileName={2}", ip, folder, fileName);
            var mess = Request.GetHttp(url, postData);
            if (mess.Contains("false"))
            {
                WriteErrorMessage(mess, 3);
            }
            else
            {
                log(string.Format(@"SqlServer:对{0}的{1}数据库还原结果:{2}", ip, database, mess));
            }

            string cmdText = string.Format(@"restore  database {0} from disk='\\127.0.0.1\{1}\{2}.bak' With Replace", database, folder, fileName);
            BakReductSql(cmdText, false, ip, user, pwd, database);
        }

        /// <summary>
        /// 对数据库的备份和恢复操作，Sql语句实现
        /// </summary>
        /// <param name="cmdText">实现备份或恢复的Sql语句</param>
        /// <param name="isBak">该操作是否为备份操作，是为true否，为false</param>
        private void BakReductSql(string cmdText, bool isBak, string ip, string user, string pwd, string database)
        {
            SqlCommand cmdBakRst = new SqlCommand();
            string connStr = string.Format(@"Data Source={0};Initial Catalog=master;uid={1};pwd={2};", ip, user, pwd);
            SqlConnection conn = new SqlConnection(connStr);
            try
            {
                conn.Open();
                cmdBakRst.Connection = conn;
                cmdBakRst.CommandType = CommandType.Text;
                if (!isBak)     //如果是恢复操作
                {
                    string setOffline = "Alter database " + database + " Set Offline With rollback immediate ";
                    string setOnline = " Alter database " + database + " Set Online With Rollback immediate";
                    cmdBakRst.CommandText = setOffline + cmdText + setOnline;
                }
                else
                {
                    cmdBakRst.CommandText = cmdText;
                }
                cmdBakRst.ExecuteNonQuery();
                if (!isBak)
                {
                    log("恭喜你，数据成功恢复为所选文档的状态！", cmdText);
                }
                else
                {
                    log("恭喜，你已经成功备份当前数据！", cmdText);
                }
            }
            catch (SqlException sexc)
            {
                WriteErrorMessage("失败，可能是对数据库操作失败，原因：" + sexc.Message, 3);
            }
            catch (Exception ex)
            {
                WriteErrorMessage("对不起，操作失败，可能原因：" + ex.Message, 3);
            }
            finally
            {
                cmdBakRst.Dispose();
                conn.Close();
                conn.Dispose();
            }
        }
        #endregion

        #region 拷贝文件

        /// <summary>
        /// 全局的文件拷贝方法
        /// </summary>
        public void CopyFileToServer()
        {
            try
            {
                
                //log("调用[" + kv.K + "],ip[" + kv.V + "]");
                //if (kv != null && kv.K > 0)
                //{
                //    UpMonitFile2(kv);//上传指定的文件到服务器
                //}

                //lcz 这里是把同一个ip下的5个文件传到方法里统一拷贝
                if (_monitList!=null&&_monitList.Count > 0)
                {
                    log("从内存中获得随机多条文件信息【" + string.Join(",", _monitList) + "】");

                    // var vList = _monitList.Select(p => p.V).Distinct();
                    //foreach (var v in _monitList)
                    //{
                    //    UpMonitFile3(_monitList.Where(p => p.V == v).ToList());//上传指定的文件到服务器
                    //}

                    UpMonitFile3(_monitList);

                    //UpMonitFile2(kv);//上传指定的文件到服务器
                    //UpMonitFile3(kvLs);//上传指定的文件到服务器
                }
                else
                {
                    log("内存中无待监控文件列表");
                }
            }
            catch (Exception ex)
            {
                //log("监控异常：" + ex.Message);
                WriteErrorMessage("监控异常：" + ex.Message, 2);//错误信息
            }
        }
        #endregion

        #region 自动上传文件--允许指定终端ip和共享目录（先注释）
        //public void CopyFileToServer(string ip = null, string folder = null)
        //{
        //    try
        //    {
        //        string sql = "";
        //        KeyValuePair<long, string> _dicMonitId = new KeyValuePair<long, string>();//初始化

        //        #region 获得待监控的列表+当前待上传的monitId
        //        lock (this)//锁定查询语句
        //        {
        //            //当内存中没有数量时，查询待添加的N条记录（来自配置文件的MaxUploadCount）
        //            if (global.monitFileIdList == null || global.monitFileIdList.Count == 0)
        //            {
        //                #region 再次验证和清理未在线终端
        //                var ipArr = global.ipList.ToArray();
        //                for (int i = 0; i < ipArr.Count(); i++)
        //                {
        //                    if (Request.PingIP(ipArr[i].Value)&& global.ipList.ContainsKey(ipArr[i].Key))
        //                    {
        //                        global.ipList.Remove(ipArr[i].Key);//移除已在线的终端
        //                    }
        //                }
        //                #endregion

        //                string parMsg = null;
        //                #region 根据传入的参数获取需要的查询sql
        //                if (string.IsNullOrEmpty(ip))//全局监控
        //                {
        //                    //获取前MaxUploadCount条记录（排除未在线终端）
        //                    sql = string.Format(@"  SELECT A.ID, B.IP, A.COMPUTER_ID
        //                        FROM FM_MONIT_FILE A LEFT JOIN FM_COMPUTER B ON (A.COMPUTER_ID = B.ID)
        //                       WHERE     (A.COPY_STATUS = 0 OR A.COPY_STATUS = 3)
        //                             AND ( ({0} = 0) OR ({0} > 0 AND A.COMPUTER_ID NOT IN ({1})))
        //                             AND ROWNUM <= {2}
        //                    ORDER BY A.ID", global.ipList.Count,
        //                   global.ipList.Count == 0 ? "0" : string.Join(",", global.ipList.Keys), Main.MaxUploadCount);
        //                    parMsg = "全域终端";
        //                }
        //                else
        //                {
        //                    if (string.IsNullOrEmpty(folder))//监控指定ip终端
        //                    {
        //                        if (global.ipList.ContainsValue(ip))
        //                        {
        //                            string msg = string.Format(@"该监控终端【{0}】未在线，将退出监控。", ip);
        //                        WriteWarnMessage(msg);
        //                            //log(msg);//后期将改为警告日志
        //                            return;
        //                        }
        //                        else
        //                        {
        //                            //根据终端ip获取待监控的列表
        //                            sql = string.Format(@"SELECT A.ID, B.IP, A.COMPUTER_ID
        //                                    FROM FM_MONIT_FILE A LEFT JOIN FM_COMPUTER B ON (A.COMPUTER_ID = B.ID)
        //                                   WHERE     (A.COPY_STATUS = 0 OR A.COPY_STATUS = 3)
        //                                         AND B.IP='{0}'
        //                                         AND ROWNUM <= {1}
        //                                ORDER BY A.ID", ip.Trim(), Main.MaxUploadCount);
        //                            parMsg = "终端【" + ip.Trim() + "】";
        //                        }
        //                    }
        //                    else//监控指定ip终端的指定文件夹
        //                    {
        //                        sql = string.Format(@"  SELECT A.ID, B.IP, A.COMPUTER_ID
        //                                    FROM FM_MONIT_FILE A
        //                                         LEFT JOIN FM_COMPUTER B ON (A.COMPUTER_ID = B.ID)
        //                                         LEFT JOIN FM_FOLDER C ON (A.FOLDER_ID = C.ID)
        //                                   WHERE     (A.COPY_STATUS = 0 OR A.COPY_STATUS = 3)
        //                                         AND B.IP = '{0}'
        //                                         AND C.NAME = '{1}'
        //                                         AND ROWNUM <= {2}
        //                                ORDER BY A.ID", ip.Trim(), folder.Trim(), Main.MaxUploadCount);
        //                        parMsg = "终端【" + ip.Trim() + "】共享文件夹【" + folder.Trim() + "】";
        //                    }
        //                }
        //                #endregion

        //                #region 获取MaxUploadCount条待拷贝记录(排除未在线终端)
        //                //采集待插入的文件列表
        //                //采集未在线的终端列表

        //                StringBuilder sb = new StringBuilder();
        //                StringBuilder sbNotAlive = new StringBuilder();
        //                using (BDBHelper dbop = new BDBHelper())
        //                {
        //                    DataTable dt = dbop.ExecuteDataTable(sql);//执行sql
        //                    if (dt != null && dt.Rows.Count > 0)
        //                    {
        //                        for (int i = 0; i < dt.Rows.Count; i++)
        //                        {
        //                            //校验ip
        //                            string curIp = dt.Rows[i][1].ToString().Trim();
        //                            if (!string.IsNullOrEmpty(curIp) && Request.PingIP(curIp))
        //                            {
        //                                //global.list.Add(Convert.ToInt64(dt.Rows[i][0]));//添加到待处理文件集合
        //                                //添加到待处理文件集合
        //                                if (!global.monitFileIdList.ContainsKey(Convert.ToInt64(dt.Rows[i][0])))
        //                                {
        //                                    global.monitFileIdList.Add(Convert.ToInt64(dt.Rows[i][0]), dt.Rows[i][1].ToString());
        //                                    sb.Append(dt.Rows[i][0] + ",");
        //                                }
        //                            }
        //                            else//ip不在线
        //                            {
        //                                if (!global.ipList.ContainsValue(dt.Rows[i][1].ToString()))//添加到列表
        //                                {
        //                                    global.ipList.Add(Convert.ToInt64(dt.Rows[i][2]), dt.Rows[i][1].ToString());
        //                                    sbNotAlive.Append(dt.Rows[i][1] + " , ");
        //                                }
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        string msg = "未在库中查询到需要拷贝的文件，当前" + parMsg + "不存在需拷贝文件";
        //                        WriteWarnMessage(msg);
        //                        //log(msg);
        //                        return;
        //                    }
        //                }
        //                #endregion

        //                log("内存中无监控的文件列表，从数据库中去获取", string.Format(@"执行查询的sql:\r\n{0}。\r\n查询的结果为：{1}", sql, sb));
        //                log("获取到未在线的ip【" + sbNotAlive + "】,当前未在线的ip列表为【" + string.Join(" , ", global.ipList.Values.ToArray()) + "】");
        //            }

        //            #region 挑选待上传集合中最后一个文件并从集合中移除：_dicMonitId
        //            _dicMonitId = global.monitFileIdList.Last();//从内存中获取元素
        //            global.monitFileIdList.Remove(_dicMonitId.Key);//移除元素
        //            #endregion
        //        }
        //        #endregion

        //        UpMonitFile(_dicMonitId);//上传指定的文件_dicMonitId到服务器
        //    }
        //    catch (Exception ex)
        //    {
        //        //log("监控异常：" + ex.Message);
        //        WriteErrorMessage("监控异常：" + ex.Message, 2);//错误信息
        //    }
        //}
        #endregion

        /// <summary>
        /// 上传指定的monitId文件到服务端
        /// </summary>
        /// <param name=""></param>
        public void UpMonitFile(KV kv)
        {
            try
            {
                if (kv.K > 0)
                    log("获得监控文件编号【" + kv.K + "】");
                else
                {
                    string msg = "未获取到需要拷贝的记录，当前不存在需要拷贝文件";
                    WriteWarnMessage(msg);
                    //log(msg);
                    return;
                }
                #region 检验文件的ip是否通畅
                if (!Request.PingIP(kv.V))
                {
                    throw new Exception("文件编号【" + kv.K + "】的ip【" + kv.V + "】不在线，未能成功上传。");
                }
                #endregion

                string api = Librarys.Config.BConfig.GetConfigToString("MonitCopyFileIP");
                log("开始复制文件，编号【" + kv.K + "】");
                log("调用拷贝接口服务，接口地址：\r\n" + api);
                //开启一个文件的复制
                string result = Request.GetHttp(api, "monitFileId=" + kv.K);
                if (!string.IsNullOrEmpty(result))
                {
                    result = "监控文件编号【" + kv.K + "】拷贝失败：" + result;
                    WriteErrorMessage(result, 2);//错误信息
                    //global.monitFileIdList.Remove(kv.K);
                    //global.monitKVList.Remove(kv);
                    //log(result, 2);
                    return;
                }
                else
                {
                    log("监控文件编号【" + kv.K + "】拷贝成功。");
                }
                //global.monitFileIdList.Remove(kv.K);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void UpMonitFile2(KV kv)
        {
            try
            {
                #region 检验文件的ip是否通畅
                if (!Request.PingIP(kv.V))
                {
                    throw new Exception("文件编号【" + kv.K + "】的ip【" + kv.V + "】不在线，未能成功上传。");
                }
                #endregion

                log("获得监控文件编号【" + kv.K + "】,开始进行拷贝");
                string sql = string.Format(@"SELECT A.SERVER_PATH,
                       A.CLIENT_PATH,
                       A.FILE_LIBRARY_ID,
                       B.IP, 
                       B.USER_NAME,
                       B.PWD
                  FROM FM_MONIT_FILE A LEFT JOIN FM_COMPUTER B ON (A.COMPUTER_ID = B.ID)
                 WHERE A.ID = {0}", kv.K);
                DataTable dt = null;
                using (BDBHelper dbop = new BDBHelper())
                {
                    dt = dbop.ExecuteDataTable(sql);
                }
                if (dt != null && dt.Rows.Count > 0)
                {
                    string toPath = dt.Rows[0]["SERVER_PATH"].ToString();
                    string fromPath = dt.Rows[0]["CLIENT_PATH"].ToString();
                    string pwd = GetDecryptPwd(dt.Rows[0]["PWD"].ToString());
                    using (SharedTool tool = new SharedTool(dt.Rows[0]["USER_NAME"].ToString(), pwd, dt.Rows[0]["IP"].ToString()))
                    {
                        log("文件路径" + fromPath);
                        log("文件路径" + toPath);
                        //if (File.Exists(@fromPath))
                        //{
                        //File.Copy(fromPath, toPath, true);//从客户端拷贝文件到服务端(覆盖式拷贝)
                        try
                        {
                            Request.CopyFile(fromPath, toPath, 1024 * 1024 * 5);
                            log("监控文件编号【" + kv.K + "】拷贝成功。");
                            using (BDBHelper dbop = new BDBHelper())
                            {
                                dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=1,COPY_STATUS_TIME=sysdate where id= {0}", kv.K));
                                dbop.ExecuteNonQuery(string.Format(@"update FM_FILE_LIBRARY set IS_COPY=1 where id={0}", dt.Rows[0]["FILE_LIBRARY_ID"].ToString()));
                            }
                        }
                        catch (Exception ex)
                        {
                            var result = "监控文件编号【" + kv.K + "】拷贝失败：" + ex.Message;
                            WriteErrorMessage(result, 2);//错误信息
                            using (BDBHelper dbop = new BDBHelper())
                            {
                                dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=3,COPY_STATUS_TIME=sysdate where id= {0}", kv.K));
                            }
                        }


                        //}
                        //else
                        //{
                        //    //log("文件路径：文件不存在" + fromPath);
                        //    WriteErrorMessage("文件路径：文件不存在" + fromPath, 2);//错误信息
                        //    using (BDBHelper dbop = new BDBHelper())
                        //    {
                        //        dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=4,COPY_STATUS_TIME=sysdate where id= {0}", kv.K));

                        //    }
                        //}
                    }
                }

            }
            catch (Exception ex)
            {
                log("监控文件编号【" + kv.K + "】拷贝失败。" + ex.Message);
                using (BDBHelper dbop = new BDBHelper())
                {
                    dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=3,COPY_STATUS_TIME=sysdate where id= {0}", kv.K));
                }
            }

        }

        //批量拷贝。
        //这个地方注意kvLs下面的文件应该都是在同一台客户机ip，好用于一个连接多个文件拷贝
        public void UpMonitFile3(List<long> kvLs)
        {
            try
            {
                #region 检验文件的ip是否通畅
                //if (!Request.PingIP(kvLs[0].V))
                //{
                //    throw new Exception("文件编号【" + kvLs[0].K + "】的ip【" + kvLs[0].V + "】不在线，未能成功上传。");
                //}
                #endregion

             //   log("获得监控文件编号【" + string.Join(",", kvLs.Select(p => p.K)) + "】,ip【"+ kvLs[0].V + "】开始进行拷贝");
                string sql = string.Format(@"SELECT A.SERVER_PATH,A.ID,
                       A.CLIENT_PATH,
                       A.FILE_LIBRARY_ID,
                       B.IP, 
                       B.USER_NAME,
                       B.PWD,
                       A.MD5
                  FROM FM_MONIT_FILE A LEFT JOIN FM_COMPUTER B ON (A.COMPUTER_ID = B.ID)
                 WHERE A.ID in ({0})", string.Join(",", kvLs).TrimEnd(','));
                DataTable dt = null;
                using (BDBHelper dbop = new BDBHelper())
                {
                    dt = dbop.ExecuteDataTable(sql);
                }
                if (dt != null && dt.Rows.Count > 0)
                {
                    string pwd = GetDecryptPwd(dt.Rows[0]["PWD"].ToString());
                    string user = dt.Rows[0]["USER_NAME"].ToString();
                    string ip = dt.Rows[0]["IP"].ToString();

                    using (SharedTool tool = new SharedTool(user, pwd, ip))
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            log("获得监控文件编号【" + dt.Rows[i]["ID"].ToString() + "】,开始进行拷贝");

                            string toPath = dt.Rows[i]["SERVER_PATH"].ToString();
                            string fromPath = dt.Rows[i]["CLIENT_PATH"].ToString();


                            log("文件客户端路径:" + fromPath+ ";文件服务端路径:"+ toPath);

                            //var kv = kvLs[i];
                            try
                            {
                                if(!File.Exists(toPath))//是否已拷贝（否）
                                {
                                    Request.CopyFile(fromPath, toPath, 1024 * 1024 * 5);
                                    //log("监控文件编号【" + kv.K + "】拷贝成功。");
                                    log("监控文件【" + dt.Rows[i]["ID"].ToString() + "】拷贝成功。");
                                }
                                else
                                {
                                    //log("监控文件【" + dt.Rows[i]["ID"].ToString() + "】已被其他线程拷贝至服务端。");
                                    WriteWarnMessage("监控文件【" + dt.Rows[i]["ID"].ToString() + "】已被其他线程拷贝至服务端。", 2);
                                }
                                using (BDBHelper dbop = new BDBHelper())
                                {
                                   // log(string.Format("当前监控文件ID:{0},MD5:{1}。", dt.Rows[i]["ID"].ToString(), dt.Rows[i]["MD5"].ToString()));//20180701注释
                                    dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=1,COPY_STATUS_TIME=sysdate where ID={0}", dt.Rows[i]["ID"].ToString()));
                                    //dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=1,COPY_STATUS_TIME=sysdate where MD5= '{0}'", dt.Rows[i]["MD5"].ToString()));
                                    dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=1,TEST_COPY_STATUS_TIME=sysdate where  MD5= '{0}'", dt.Rows[i]["MD5"].ToString()));
                                    //dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=1,COPY_STATUS_TIME=sysdate where MD5='{0}' and COPY_STATUS!=1", dt.Rows[i]["MD5"].ToString()));
                                    //dbop.ExecuteNonQuery(string.Format(@"update FM_FILE_LIBRARY set IS_COPY=1 where id={0}", dt.Rows[i]["FILE_LIBRARY_ID"].ToString()));
                                    dbop.ExecuteNonQuery(string.Format(@"update FM_FILE_LIBRARY set IS_COPY=1 where MD5='{0}'", dt.Rows[i]["MD5"].ToString()));
                                }
                            }
                            catch (Exception ex)
                            {
                                //这里要分两种情况：1、文件确实不存在；2、计算机的连接数已满 . lcz处理下
                                //1、错误内容：已达到计算机的连接数最大值，无法再同此远程计算机连接
                                //2、错误内容：Could not find file '\\10.0.0.60\Project\xxxx'.
                                string errMsg = ex.Message;
                                string copyStatus = "3";

                                if (errMsg.Contains("Could not find file"))//无源文件
                                {
                                    copyStatus = "4";
                                }
                                else if (errMsg.Contains("Could not find a part of the path"))//源文件所在目录路径不存在
                                {
                                    copyStatus = "7";
                                }
                                else if (errMsg.Contains("已达到计算机的连接数最大值"))
                                {
                                    if (File.Exists(toPath))
                                    {
                                        File.Delete(toPath);
                                    }

                                    copyStatus = "0";
                                }
                                //无法向后查找重写先前在附加模式下打开的文件中存在的数据。
                                //发现问题：报该错误的文件都很大的而且不停在增加，应该是拷贝以分段流的方式引起的错误
                                //处理方式：先删除服务端相同的md5的文件，在进行复制（等下一波对它的复制）
                                else if (errMsg.Contains("Unable seek backward to overwrite data that previously existed in a file opened in Append mode"))
                                {
                                    File.Delete(toPath);
                                    log("监控文件编号【" + dt.Rows[i]["ID"].ToString() + "】出现以下错误。系统已从服务端删除文件【" + toPath + "】");
                                    copyStatus = "3";
                                }
                                //because it is being used by another process
                                else if (errMsg.Contains("because it is being used by another process"))
                                {
                                    log("监控文件编号【" + dt.Rows[i]["ID"].ToString() + "】被占用。状态将标注为被占用【6】");
                                    copyStatus = "6";
                                }
                                else//链接数已满或其他未知错误
                                {
                                    copyStatus = "3";
                                }

                                var result = "监控文件编号【" + dt.Rows[i]["ID"].ToString() + "】拷贝失败：" + errMsg;
                                //WriteErrorMessage(result, 2);//错误信息
                                WriteWarnMessage(result, 2);
                                using (BDBHelper dbop = new BDBHelper())
                                {
                                    dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS={0},COPY_STATUS_TIME=sysdate where id= {1}",copyStatus, dt.Rows[i]["ID"].ToString()));
                                }
                            }
                        }

                        #region 废弃
                        //if (File.Exists(@fromPath))
                        //{
                        //File.Copy(fromPath, toPath, true);//从客户端拷贝文件到服务端(覆盖式拷贝)



                        //}
                        //else
                        //{
                        //    //log("文件路径：文件不存在" + fromPath);
                        //    WriteErrorMessage("文件路径：文件不存在" + fromPath, 2);//错误信息
                        //    using (BDBHelper dbop = new BDBHelper())
                        //    {
                        //        dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=4,COPY_STATUS_TIME=sysdate where id= {0}", kv.K));

                        //    }
                        //}
                        #endregion
                    }
                }

            }
            catch (Exception ex)
            {
                string errMsg = "拷贝失败:" + ex.Message;
                WriteWarnMessage(errMsg, 2);
                //log("监控文件编号【" + kv.K + "】拷贝失败。" + ex.Message);
                //using (BDBHelper dbop = new BDBHelper())
                //{
                //    dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=3,COPY_STATUS_TIME=sysdate where id= {0}", kv.K));
                //}
            }

        }

        /// <summary>
        /// 获得解密后的密码
        /// </summary>
        /// <param name="pwd"></param>
        /// <returns></returns>
        private string GetDecryptPwd(string pwd)
        {
            string aesPwd = pwd;
            try
            {
                var p = Common.Helper.EncryptHelper.AesDecrpt(pwd);
                aesPwd = p;
            }
            catch
            {
            }
            return aesPwd;
        }



        #region 删除重复文件
        public void DeleteFileMD5()
        {
            log(string.Format(@"开启对不正确的文件删除..."));
            string url = Librarys.Config.BConfig.GetConfigToString("DeleteFileIP");
            string postData = string.Format("ip=1");
            log("参数说明:" + postData);
            string surl = url + (postData == "" ? "" : "?") + postData;
            log("访问路径:" + surl);
            var mess = Request.GetHttp(url, postData);
            if (mess.Contains("结果:false"))
            {
                WriteErrorMessage(mess, 3);
            }
            else if (mess.Contains("结果:warn"))
            {
                WriteWarnMessage(mess, 3);
            }
            else
            {
                log(string.Format(@"删除重复文件结果:{0}", mess));
            }

        }

        #endregion
        #region 重复拷贝文件遍历

        public void fileInfo()
        {
            log(string.Format(@"开启对已生成文件的遍历..."));
            string url = Librarys.Config.BConfig.GetConfigToString("ReclyFileIP");
            string postData = string.Format("ip=1");
            log("参数说明:" + postData);
            string surl = url + (postData == "" ? "" : "?") + postData;
            log("访问路径:" + surl);
            var mess = Request.GetHttp(url, postData);
            if (mess.Contains("结果:false"))
            {
                WriteErrorMessage(mess, 3);
            }
            else if (mess.Contains("结果:warn"))
            {
                WriteWarnMessage(mess, 3);
            }
            else
            {
                log(string.Format(@"生成文件的遍历结果:{0}",mess));
            }

        }
        #endregion
    }
}