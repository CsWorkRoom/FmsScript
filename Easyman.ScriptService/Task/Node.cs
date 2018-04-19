using Easyman.Librarys.ApiRequest;
using Easyman.Librarys.DBHelper;
using Easyman.Librarys.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.ScriptService.Task
{
    /// <summary>
    /// 节点，负责执行脚本任务中当前可以执行的节点，每一个节点开启一个线程
    /// 为了确保数据库连接数可控，通过配置文件限定线程数。
    /// </summary>
    public class Node
    {
        private bool _isAuto = true;
        private long _scriptNodeCaseID;
        private int _maxTryTimes = 0;
        BLL.EM_SCRIPT_NODE_CASE.Entity _nodeCaseEntity;
        private BackgroundWorker _bw;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="nodeCaseID">节点实例ID</param>
        /// <param name="maxTryTimes">出错最大尝试次数</param>
        /// <param name="isAuto">是否自动添加</param>
        public Node(long nodeCaseID, int maxTryTimes, bool isAuto = true)
        {
            _scriptNodeCaseID = nodeCaseID;
            _maxTryTimes = maxTryTimes;
            _isAuto = isAuto;
        }

        /// <summary>
        /// 开始执行任务
        /// </summary>
        /// <param name="isLastNodeCase">是否为程序上次运行添加的节点实例，这种节点可能正处于运行中程序被停止了</param>
        /// <returns></returns>
        public bool Start(bool isLastNodeCase = false)
        {
            try
            {
                //验证运行节点实例数量
                if (Main.RunningNodeCount >= Main.MaxExecuteNodeCount)
                {
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("当前已经有【{0}】个节点实例运行，超过系统设定的最大数【{1}】，本节点实例将暂时不被执行。", Main.RunningNodeCount, Main.MaxExecuteNodeCount));
                    return false;
                }

                //读取当前节点
                _nodeCaseEntity = BLL.EM_SCRIPT_NODE_CASE.Instance.GetEntityByKey<BLL.EM_SCRIPT_NODE_CASE.Entity>(_scriptNodeCaseID);

                if (_nodeCaseEntity == null)
                {
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("没有获取脚本流节点实例ID【{0}】的实体对象，将不被执行。", _scriptNodeCaseID));
                    return false;
                }

                //已经停止的节点，不再执行
                if (_nodeCaseEntity.RUN_STATUS == (short)Enums.RunStatus.Stop)
                {
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】的运行状态为【停止】，本节点将不被执行。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));
                    return false;
                }

                //当前状态不等于等待执行
                if (_nodeCaseEntity.RUN_STATUS != (short)Enums.RunStatus.Wait)
                {
                    //上次未完成的节点实例，可以继续执行
                    if (isLastNodeCase == false)
                    {
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】的运行状态不为【等待执行】，本节点将不被执行。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));
                        return false;
                    }
                }

                //添加到内存
                if (Main.AddNodeTask(_scriptNodeCaseID) == false)
                {
                    //WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】已经于【{4}】开始运行，本次将不被执行。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID, Main.GetNodeTaskStartTime(_scriptNodeCaseID).ToString("yyyy-MM-dd HH:mm:ss.fff")));
                    return false;
                }

                //更新当前节点状态
                int i = BLL.EM_SCRIPT_NODE_CASE.Instance.UpdateRunStatus(_scriptNodeCaseID, Enums.RunStatus.Excute);
                if (i < 0)
                {
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("更新脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】的运行状态为【执行中】失败，本节点将不被执行。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));
                    return false;
                }

                WriteLog(_scriptNodeCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】的运行状态已经更新为【执行中】，下面将执行节点脚本内容。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));

                _bw = new BackgroundWorker();
                _bw.WorkerSupportsCancellation = true;
                _bw.DoWork += DoWork;
                _bw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("执行节点实例【{0}】出现了未知异常，1错误信息为：\r\n{1}", _scriptNodeCaseID, ex.ToString()));
                return false;
            }

            return true;
        }

        /// <summary>
        /// 生成代码、编译及运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            int reTryTimes = 0;//初始化为0
            List<KV> monitList = null;
            while (Main.IsRun)
            {
                try
                {
                    ErrorInfo err = new ErrorInfo();
                    string code = Script.Transfer.Trans(_nodeCaseEntity, ref err);
                    if (err.IsError == true)
                    {
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】生成脚本代码失败，错误信息为：\r\n{4}", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID, err.Message));
                        //从内存记录中移除
                        Main.RemoveNodeTask(_nodeCaseEntity.ID);
                        return;
                    }
                    BLL.EM_SCRIPT_CASE.Entity entityScriptCase= BLL.EM_SCRIPT_CASE.Instance.GetCase(_nodeCaseEntity.SCRIPT_CASE_ID);
                    //如果是并行任务，文件拷贝
                    if (entityScriptCase.IS_SUPERVENE==1)
                       monitList= GetMonitList();
                    //保存源代码到数据库lcz*****
                    int i = BLL.EM_SCRIPT_NODE_CASE.Instance.UpdateCompileContent(_nodeCaseEntity.ID, code);
                    log(string.Format("成功赋值文件列表：" + ((monitList != null && monitList.Count() > 0) ? string.Join(",", monitList) : "空")));
                    if (entityScriptCase.IS_SUPERVENE == 1 && (monitList == null || monitList.Count()==0))
                    {
                        BLL.EM_SCRIPT_NODE_CASE.Instance.SetStop(_nodeCaseEntity.ID, Enums.ReturnCode.Warn.GetHashCode());
                        Main.RemoveNodeTask(_nodeCaseEntity.ID);
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】未查询到待复制的文件编号", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));
                        return;
                    }
                    bool isSuccess = Script.Execute.NewRun(code, _nodeCaseEntity, monitList, ref err);
                    if (isSuccess)
                    {
                        //结束运行状态
                        if (err.IsWarn)
                        {
                            BLL.EM_SCRIPT_NODE_CASE.Instance.SetStop(_nodeCaseEntity.ID, Enums.ReturnCode.Warn.GetHashCode());
                        }
                        else
                        {
                            BLL.EM_SCRIPT_NODE_CASE.Instance.SetStop(_nodeCaseEntity.ID, Enums.ReturnCode.Success.GetHashCode());
                        }
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】已经成功执行。预警状态:{4}", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID, err.IsWarn.ToString()));
                        //从内存记录中移除
                        Main.RemoveNodeTask(_nodeCaseEntity.ID);

                        if (monitList != null && monitList.Count > 0)
                        {
                            global.OpMonitKVList("remove", null, 0, monitList);
                        }
                        return;
                    }
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】执行失败;失败信息:{4}", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID, err.Message.ToString()));
                    //记录重试次数
                    reTryTimes = BLL.EM_SCRIPT_NODE_CASE.Instance.RecordTryTimes(_nodeCaseEntity.ID);

                    //超过最大尝试次数
                    if (reTryTimes >= _maxTryTimes)
                    {
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】作了最后一次尝试，仍然执行失败，本脚本流将不再执行。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));

                        BLL.EM_SCRIPT_NODE_CASE.Instance.SetStop(_nodeCaseEntity.ID, Enums.ReturnCode.Fail.GetHashCode());
                        //BLL.EM_SCRIPT_CASE.Instance.SetFail(_nodeCaseEntity.SCRIPT_CASE_ID);
                        //Main.CurUploadCount--;

                        //从内存记录中移除
                        Main.RemoveNodeTask(_nodeCaseEntity.ID);
                        if (monitList != null && monitList.Count > 0)
                        {
                            global.OpMonitKVList("remove", null, 0, monitList);
                        }
                        return;
                    }
                    else
                    {
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】第【{4}】次尝试执行失败，将再次尝试。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID, reTryTimes));
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("执行节点实例【{0}】出现了未知异常，错误信息为：\r\n{1}", _scriptNodeCaseID, ex.ToString()));
                    BLL.EM_SCRIPT_NODE_CASE.Instance.SetStop(_nodeCaseEntity.ID, Enums.ReturnCode.Fail.GetHashCode());
                    //BLL.EM_SCRIPT_CASE.Instance.SetFail(_nodeCaseEntity.SCRIPT_CASE_ID);
                    //从内存记录中移除
                    Main.RemoveNodeTask(_nodeCaseEntity.ID);
                    if (monitList != null && monitList.Count > 0)
                    {
                        global.OpMonitKVList("remove", null, 0, monitList);
                    }
                    //Main.CurUploadCount--;
                    return;
                }
            }
        }


        #region 获取待处理的文件列表
        private static object symObj = new object();
        public List<KV> GetMonitList()
        {
            string sql = "";
            KeyValuePair<long, string> _dicMonitId = new KeyValuePair<long, string>();//初始化
            KV kv = new KV();
            List<KV> kvLs = new List<KV>();//获取几条
            lock (this)
            {
                int count = global.GetMonitKVCount();
                if (count > 0) //&& count >= Main.EachSearchUploadCount/2
                {
                    kvLs = global.OpMonitKVList("take", null, Main.EachUploadCount);
                }
                else
                {
                    #region 获得待监控的列表+当前待上传的monitId
                    lock (symObj)//锁定查询语句
                    {
                        //当内存中没有数量时，查询待添加的N条记录（来自配置文件的MaxUploadCount）
                        var monitKVLists = global.OpMonitKVList("getall");
                        if (monitKVLists == null || monitKVLists.Count == 0) //|| monitKVLists.Count<Main.EachSearchUploadCount / 2
                        //if (global.monitFileIdList == null || global.monitFileIdList.Count == 0)
                        {
                            var ipNotLists = global.OpIpNotList("getall");
                            log("输出未在线的ip：" + string.Join(",", ipNotLists.Select(p => p.V)));                      

                            #region 获取MaxUploadCount条待拷贝记录(排除未在线终端)
                            //采集待插入的文件列表
                            //采集未在线的终端列表

                            //lcz, 这个地方的sql可以只返回同一客户机ip的，便于下面的一个连接多个文件拷贝
                            //获取不返回一个ip的文件，在从monitKVList中获取5个一样ip的终端去处理
                            sql = string.Format(@"SELECT A.ID, B.IP, A.COMPUTER_ID
                                                  FROM (SELECT ID, COMPUTER_ID
                                                          FROM (SELECT A.ID,
                                                                       A.COMPUTER_ID,
                                                                       ROW_NUMBER () OVER (ORDER BY A.ID) RN
                                                                  FROM FM_MONIT_FILE A
                                                                       LEFT JOIN (    SELECT DISTINCT REGEXP_SUBSTR ('{0}',
                                                                                                                     '[^,]+',
                                                                                                                     1,
                                                                                                                     LEVEL)
                                                                                                         AS COMPUTER_ID
                                                                                        FROM DUAL
                                                                                  CONNECT BY REGEXP_SUBSTR ('{0}',
                                                                                                            '[^,]+',
                                                                                                            1,
                                                                                                            LEVEL)
                                                                                                IS NOT NULL) C
                                                                          ON (A.COMPUTER_ID = C.COMPUTER_ID)
                                                                        LEFT JOIN FM_FILE_FORMAT F ON (F.ID=A.FILE_FORMAT_ID)   
                                                                 WHERE     NVL (C.COMPUTER_ID, 0) = 0 AND F.NAME<>'Folder'
                                                                       AND (A.COPY_STATUS = 0 OR A.COPY_STATUS = 3))
                                                         WHERE RN <{1}) A
                                                       LEFT JOIN FM_COMPUTER B ON (A.COMPUTER_ID = B.ID)", string.Join(",", ipNotLists.Select(p => p.K).Distinct()), Main.EachSearchUploadCount);

                   
                            StringBuilder sb = new StringBuilder();//待处理
                                                                   //StringBuilder sbNotAlive = new StringBuilder();//未在线
                            List<string> notAliveList = new List<string>();//当前查询的未在线
                            DataTable dt = null;
                            using (BDBHelper dbop = new BDBHelper())
                            {
                                dt = dbop.ExecuteDataTable(sql);
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    string updateSql = string.Format(@"update FM_MONIT_FILE set COPY_STATUS=5 where id in({0})", string.Join(",", dt.AsEnumerable().Select(r => r["ID"]).Distinct().ToArray()).TrimEnd(','));
                                    dbop.ExecuteNonQuery(updateSql);
                                }
                            }
                            log("查询出的数量为：【" + dt.Rows.Count + "】");
                            if (dt != null && dt.Rows.Count > 0)
                            {
                                List<string> hasAliveIps = new List<string>();//当前批次的在线ip

                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    sb.Append(dt.Rows[i][0] + ",");
                                    //校验ip
                                    string curIp = dt.Rows[i][1].ToString().Trim();
                                    //log("当前ip【" + curIp + "】");
                                    var curKv = new KV { K = Convert.ToInt64(dt.Rows[i][2].ToString()), V = dt.Rows[i][1].ToString() };//不在线的ip

                                    if (string.IsNullOrEmpty(curIp))
                                    {
                                        log("ip[" + curIp + "]为空");
                                    }
                                    else if (hasAliveIps.Contains(curIp))
                                    {
                                        global.OpMonitKVList("add", new KV { K = Convert.ToInt64(dt.Rows[i][0].ToString()), V = dt.Rows[i][1].ToString() });
                                        //log("ip[" + curIp + "]在已在线列表中");
                                    }
                                    else
                                    {
                                        if (ipNotLists.Exists(p => p.K == curKv.K))
                                        {
                                            //log("ip[" + curIp + "]未在线2");
                                            using (BDBHelper dbop = new BDBHelper())
                                            {
                                                string updateSql = string.Format(@"update FM_MONIT_FILE set COPY_STATUS=0 where id ={0}", dt.Rows[i][0].ToString());
                                                dbop.ExecuteNonQuery(updateSql);
                                            }
                                            if (!notAliveList.Contains(curKv.V))
                                                notAliveList.Add(curKv.V);
                                        }
                                        else if (!Request.PingIP(curIp))
                                        {
                                            //log("ip[" + curIp + "]未在线");
                                            using (BDBHelper dbop = new BDBHelper())
                                            {
                                                string updateSql = string.Format(@"update FM_MONIT_FILE set COPY_STATUS=0 where id ={0}", dt.Rows[i][0].ToString());
                                                dbop.ExecuteNonQuery(updateSql);
                                            }
                                            global.OpIpNotList("add", curKv);
                                            notAliveList.Add(dt.Rows[i][1].ToString());
                                            if (!ipNotLists.Exists(p => p.K == curKv.K))
                                            {
                                                ipNotLists.Add(curKv);
                                            }
                                        }
                                        else
                                        {
                                            global.OpMonitKVList("add", new KV { K = Convert.ToInt64(dt.Rows[i][0].ToString()), V = dt.Rows[i][1].ToString() });
                                            hasAliveIps.Add(curIp);
                                            log("ip[" + curIp + "]在线");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                string msg = "未在库中查询到需要拷贝的文件，当前不存在需拷贝文件";
                                //log(msg);
                                log(msg, 3, string.Format(@"执行查询的sql:\r\n{0}。", sql));
                                return null;
                            }
                            log("再次输出未在线ip：" + string.Join(",", global.OpIpNotList("getall").Select(p => p.V)));
                            #endregion

                            log("内存中无监控的文件列表，从数据库中去获取",4, string.Format(@"执行查询的sql:\r\n{0}。\r\n查询的结果为：{1}", sql, sb));
                            log("获取到未在线的ip【" + (notAliveList.Count > 0 ? string.Join(",", notAliveList.Distinct()) : "") + "】,当前未在线的ip列表为【" + string.Join(" , ", global.ipNotList.Select(p => p.V)) + "】");
                        }
                        //_dicMonitId = global.monitFileIdList.Last();//从内存中获取元素
                        //global.monitFileIdList.Remove(_dicMonitId.Key);//移除元素

                        //var monitKVLists = global.OpMonitKVList("getall");
                        //if (monitKVLists != null && monitKVLists.Count > 0)
                        if (global.GetMonitKVCount() > 0)
                        {
                            kvLs = global.OpMonitKVList("take", null, Main.EachUploadCount);
                        }

                    }
                    #endregion
                }

            }
            return kvLs;
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="message">日志内容</param>
        /// <param name="logLevel">日志等级</param>
        /// <param name="sql">SQL脚本</param>
        public  bool log(string message, int logLevel = 4, string sql = "")
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
        #endregion

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="scriptNodeCaseID">脚本节点实例ID</param>
        /// <param name="level">日志等级</param>
        /// <param name="message">日志内容</param>
        /// <param name="sql">SQL语句</param>
        protected static void WriteLog(long scriptNodeCaseID, BLog.LogLevel level, string message, string sql = "")
        {
            //写日志文件
            BLog.Write(level, message);
            try
            {
                //写数据库表
                if (scriptNodeCaseID > 0)
                {
                    BLL.EM_SCRIPT_NODE_CASE_LOG.Instance.Add(scriptNodeCaseID, level.GetHashCode(), message, sql);
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "写日志到脚本节点日志表出错：" + ex.ToString());
            }
        }
    }
}