using Easyman.Librarys.ApiRequest;
using Easyman.Librarys.DBHelper;
using Easyman.Librarys.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Easyman.ScriptService.Task
{
    /// <summary>
    /// 具体任务扫描，两项工作：
    ///     1）定时扫描未完成的脚本流实例，将当前可执行节点写到执行任务实例表
    ///     2）如果脚本流实例所有节点都执行完成，则更新该脚本流实例的结果和状态
    /// </summary>
    public static class Scanner
    {
        /// <summary>
        /// 后台线程，不断扫描需要执行的节点实例
        /// </summary>
        private static BackgroundWorker _bw;

        /// <summary>
        /// 后台线程，不断扫描需要写入待拷贝文件
        /// </summary>
        private static BackgroundWorker _bw2;

        /// <summary>
        /// 限定和处理等待中的待监控文件夹的线程
        /// </summary>
        private static BackgroundWorker _bw3;

        /// <summary>
        /// 开始启动
        /// </summary>
        /// <returns></returns>
        public static void Start()
        {
            try
            {
                #region 原节点扫码线程
                BLog.Write(BLog.LogLevel.INFO, "节点扫描线程即将启动。");
                _bw = new BackgroundWorker();
                _bw.WorkerSupportsCancellation = true;
                _bw.DoWork += DoWork;
                _bw.RunWorkerAsync();
                BLog.Write(BLog.LogLevel.INFO, "节点扫描线程已经启动。");
                #endregion

                #region 添加待拷贝列表线程
                BLog.Write(BLog.LogLevel.INFO, "写入待拷贝文件线程即将启动。");
                _bw2 = new BackgroundWorker();
                _bw2.WorkerSupportsCancellation = true;
                _bw2.DoWork += DoWork2;
                _bw2.RunWorkerAsync();
                BLog.Write(BLog.LogLevel.INFO, "写入待拷贝文件线程已启动。");
                #endregion

                #region 文件夹监控任务的数量限定及处理
                BLog.Write(BLog.LogLevel.INFO, "限定和处理等待中的待监控文件夹的线程即将启动。");
                _bw3 = new BackgroundWorker();
                _bw3.WorkerSupportsCancellation = true;
                _bw3.DoWork += DoWork3;
                _bw3.RunWorkerAsync();
                BLog.Write(BLog.LogLevel.INFO, "限定和处理等待中的待监控文件夹的线程已启动。");
                #endregion
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "节点扫描线程启动失败。" + ex.ToString());
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public static void Stop()
        {
            try
            {
                BLog.Write(BLog.LogLevel.INFO, "节点扫描线程即将停止。");
                _bw.CancelAsync();
                _bw.Dispose();
                BLog.Write(BLog.LogLevel.INFO, "节点扫描线程已经停止。");

                BLog.Write(BLog.LogLevel.INFO, "待拷贝文件线程即将停止。");
                _bw2.CancelAsync();
                _bw2.Dispose();
                BLog.Write(BLog.LogLevel.INFO, "待拷贝文件线程已经停止。");

                BLog.Write(BLog.LogLevel.INFO, "限定和处理等待中的待监控文件夹的线程即将停止。");
                _bw3.CancelAsync();
                _bw3.Dispose();
                BLog.Write(BLog.LogLevel.INFO, "限定和处理等待中的待监控文件夹的线程已经停止。");
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "节点扫描线程停止失败。" + ex.ToString());
            }
        }

        /// <summary>
        /// 往待拷贝列表中加入文件编号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DoWork2(object sender, DoWorkEventArgs e)
        {
            while (Main.IsRun)
            {
                try
                {
                    #region 再次验证和清理未在线终端
                    //var ipArr = global.ipList.ToArray();
                    //for (int i = 0; i < ipArr.Count(); i++)
                    //{
                    //    if (Request.PingIP(ipArr[i].Value) && global.ipList.ContainsKey(ipArr[i].Key))
                    //    {
                    //        global.ipList.Remove(ipArr[i].Key);//移除已在线的终端
                    //    }
                    //}
                    var ipNotLists = global.OpIpNotList("getall");
                    if (ipNotLists != null && ipNotLists.Count > 0)
                    {
                        int cnt = ipNotLists.Count;
                        for (int i = cnt - 1; i >= 0; i--)
                        {
                            var item = ipNotLists[i];
                            if (Librarys.ApiRequest.Request.OldPingIP(item.V))
                            {
                                global.OpIpNotList("remove", item);
                            }
                        }
                        ipNotLists = global.OpIpNotList("getall");
                        BLog.Write(BLog.LogLevel.INFO, "输出未在线的ip：" + string.Join(",", ipNotLists.Select(p => p.V)));
                    }
                    #endregion

                    BLog.Write(BLog.LogLevel.INFO, "已在列表中的数量：" + global.GetMonitKVCount());
                    if (global.GetEffectMonitKVCount() < 200)
                    //if (global.GetMonitKVCount() < 200)
                    {
                        //var ipNotLists = global.OpIpNotList("getall");

                        #region 获取MaxUploadCount条待拷贝记录(排除未在线终端)
                        //采集待插入的文件列表
                        //采集未在线的终端列表

                        //lcz, 这个地方的sql可以只返回同一客户机ip的，便于下面的一个连接多个文件拷贝
                        //获取不返回一个ip的文件，在从monitKVList中获取5个一样ip的终端去处理
                        //string sql = string.Format(@"SELECT A.ID, B.IP, A.COMPUTER_ID
                        //                          FROM (SELECT ID, COMPUTER_ID
                        //                                  FROM (SELECT A.ID,
                        //                                               A.COMPUTER_ID,
                        //                                               ROW_NUMBER () OVER (ORDER BY A.ID) RN
                        //                                          FROM FM_MONIT_FILE A
                        //                                               LEFT JOIN (    SELECT DISTINCT REGEXP_SUBSTR ('{0}',
                        //                                                                                             '[^,]+',
                        //                                                                                             1,
                        //                                                                                             LEVEL)
                        //                                                                                 AS COMPUTER_ID
                        //                                                                FROM DUAL
                        //                                                          CONNECT BY REGEXP_SUBSTR ('{0}',
                        //                                                                                    '[^,]+',
                        //                                                                                    1,
                        //                                                                                    LEVEL)
                        //                                                                        IS NOT NULL) C
                        //                                                  ON (A.COMPUTER_ID = C.COMPUTER_ID)
                        //                                                LEFT JOIN FM_FILE_FORMAT F ON (F.ID=A.FILE_FORMAT_ID)   
                        //                                         WHERE     NVL (C.COMPUTER_ID, 0) = 0 AND F.NAME<>'Folder'
                        //                                               AND (A.COPY_STATUS = 0 OR A.COPY_STATUS = 3))
                        //                                 WHERE RN <={1}) A
                        //                               LEFT JOIN FM_COMPUTER B ON (A.COMPUTER_ID = B.ID)", string.Join(",", ipNotLists.Select(p => p.K).Distinct()), Main.EachSearchUploadCount);

                        string sql = string.Format(@"SELECT A.ID, B.IP, A.COMPUTER_ID
  FROM (SELECT A.ID, A.COMPUTER_ID
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
               LEFT JOIN FM_FILE_FORMAT F ON (F.ID = A.FILE_FORMAT_ID)
         WHERE     NVL (C.COMPUTER_ID, 0) = 0
               AND F.NAME <> 'Folder'
               AND (A.COPY_STATUS = 0 OR A.COPY_STATUS = 3)
               AND ROWNUM <= {1}) A
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
                        //log("查询出的数量为：【" + dt.Rows.Count + "】");
                        BLog.Write(BLog.LogLevel.INFO, "查询出的数量为：【" + dt.Rows.Count + "】");
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
                                    BLog.Write(BLog.LogLevel.INFO, "文件编号:" + dt.Rows[i][0] + "为空");
                                    //log("ip[" + curIp + "]为空");//20180701注释
                                    //BLog.Write(BLog.LogLevel.INFO, "ip[" + curIp + "]为空");
                                }
                                else if (hasAliveIps.Contains(curIp))
                                {
                                    BLog.Write(BLog.LogLevel.INFO, "文件编号:" + dt.Rows[i][0] + "IP在线");
                                    global.OpMonitKVList("add", new KV { K = Convert.ToInt64(dt.Rows[i][0].ToString()), V = dt.Rows[i][1].ToString() });//20180701注释
                                    //log("ip[" + curIp + "]在已在线列表中");
                                }
                                else
                                {
                                    if (ipNotLists.Exists(p => p.K == curKv.K))
                                    {
                                        BLog.Write(BLog.LogLevel.INFO, "文件编号:" + dt.Rows[i][0] + "IP不在线");
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
                                        BLog.Write(BLog.LogLevel.INFO, "文件编号2:" + dt.Rows[i][0] + "IP不在线");
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
                                        BLog.Write(BLog.LogLevel.INFO, "文件编号:" + dt.Rows[i][0] + "添加文件");
                                        //log("ip[" + curIp + "]在线");
                                        //BLog.Write(BLog.LogLevel.INFO, "ip[" + curIp + "]在线");//20180701注释
                                    }
                                }
                            }
                            //log("再次输出未在线ip：" + string.Join(",", global.OpIpNotList("getall").Select(p => p.V)));
                            #endregion

                            //log("内存中无监控的文件列表，从数据库中去获取", 4, string.Format(@"执行查询的sql:\r\n{0}。\r\n查询的结果为：{1}", sql, sb));
                            BLog.Write(BLog.LogLevel.INFO, "内存中无监控的文件列表，从数据库中去获取." + string.Format(@"执行查询的sql:\r\n{0}。\r\n查询的结果为：{1}", sql, sb));
                            BLog.Write(BLog.LogLevel.INFO, "获取到未在线的ip【" + (notAliveList.Count > 0 ? string.Join(",", notAliveList.Distinct()) : "") + "】,当前未在线的ip列表为【" + string.Join(" , ", global.ipNotList.Select(p => p.V)) + "】");
                            //log("获取到未在线的ip【" + (notAliveList.Count > 0 ? string.Join(",", notAliveList.Distinct()) : "") + "】,当前未在线的ip列表为【" + string.Join(" , ", global.ipNotList.Select(p => p.V)) + "】");
                        }
                        else
                        {
                            //string msg = "未在库中查询到需要拷贝的文件，当前不存在需拷贝文件";
                            //log(msg);
                            //log(msg, 3, string.Format(@"执行查询的sql:\r\n{0}。", sql));
                            BLog.Write(BLog.LogLevel.INFO, string.Format(@"执行查询的sql:\r\n{0}。", sql));
                            //return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    BLog.Write(BLog.LogLevel.ERROR, "查询添加待拷贝文件出错：" + ex.ToString());
                }
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// 限定和处理待
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DoWork3(object sender, DoWorkEventArgs e)
        {
            while (Main.IsRun)
            {
                try
                {
                    BLog.Write(BLog.LogLevel.INFO, "开始处理限定的监控文件夹任务");

                    #region 查询当前非并行执行中的任务实例的数量，如果数量小于MonitFolderCount，则补齐执行中的数量。
                    //修改等待中的任务为执行中(补齐差量)
                    string sql = string.Format(@"SELECT COUNT (1)
                          FROM EM_SCRIPT_CASE
                         WHERE IS_SUPERVENE <> 1 AND RUN_STATUS = 2");
                    object obj = null;
                    using (BDBHelper dbop = new BDBHelper())
                    {
                        obj = dbop.ExecuteScalar(sql);//获得执行中的非并行任务数
                    }
                    BLog.Write(BLog.LogLevel.INFO, "获取到执行中任务数：" + obj);

                    if (obj != null && Convert.ToInt32(obj) < Main.MaxMonitCount)//当执行中的数量小于MaxMonitCount
                    {
                        int difCount = Main.MaxMonitCount - Convert.ToInt32(obj);//差量
                        sql = string.Format(@"SELECT COUNT(1)
                                FROM (SELECT A.ID,
                                            ROW_NUMBER () OVER (ORDER BY ID) RN
                                        FROM EM_SCRIPT_CASE A WHERE RUN_STATUS = 1)
                                WHERE RN <= {0}", difCount);
                        object o2 = null;
                        using (BDBHelper dbop = new BDBHelper())
                        {
                            o2 = dbop.ExecuteScalar(sql);
                        }
                        BLog.Write(BLog.LogLevel.INFO, "按差量获取等待中任务数：" + o2);
                        if (o2 != null && Convert.ToInt32(o2) > 0)
                        {
                            sql = string.Format(@"MERGE INTO EM_SCRIPT_CASE A
                                     USING (SELECT ID
                                              FROM (SELECT ID,
                                                           ROW_NUMBER ()
                                                              OVER ( ORDER BY ID)
                                                              RN
                                                      FROM EM_SCRIPT_CASE
                                                     WHERE RUN_STATUS = 1)
                                             WHERE RN <= {0}) B
                                        ON (A.ID = B.ID)
                                WHEN MATCHED
                                THEN
                                   UPDATE SET RUN_STATUS = 2", difCount);

                            using (BDBHelper dbop = new BDBHelper())
                            {
                                dbop.ExecuteNonQuery(sql);//修改等待的任务为执行中
                            }
                            BLog.Write(BLog.LogLevel.INFO, "执行把等待中任务改为执行中");
                        }
                    }
                    #endregion

                }
                catch (Exception ex)
                {
                    BLog.Write(BLog.LogLevel.ERROR, "限定监控的文件夹任务出现异常：" + ex.ToString());
                }
                Thread.Sleep(100000);//100秒执行一次
            }
        }

        /// <summary>
        /// 不断扫描脚本实例表，对于需要运行的实例，为其添加节点实例并运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DoWork(object sender, DoWorkEventArgs e)
        {
            while (Main.IsRun)
            {
                long scriptCaseID = 0;
                try
                {
                    IList<BLL.EM_SCRIPT_CASE.Entity> runningCaseList = BLL.EM_SCRIPT_CASE.Instance.GetRunningCaseList();
                    if (runningCaseList != null && runningCaseList.Count > 0)
                    {
                        foreach (var scriptCase in runningCaseList)
                        {
                            scriptCaseID = scriptCase.ID;
                            ErrorInfo err = new ErrorInfo();
                            bool isSuccess = AddAndRunNode(scriptCase.SCRIPT_ID, scriptCaseID, scriptCase.RETRY_TIME, ref err);
                            if (isSuccess == false)
                            {
                                WriteLog(scriptCaseID, BLog.LogLevel.WARN, string.Format("添加脚本实例【{0}】的节点并启动节点实例失败，错误信息为：{1}", scriptCaseID, err.Message));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(scriptCaseID, BLog.LogLevel.ERROR, "扫描并添加待执行节点出现未知错误，错误信息为：" + ex.ToString());
                }

                //Thread.Sleep(3000);
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// 添加并运行节点实例
        /// </summary>
        /// <param name="scriptID">脚本ID</param>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <param name="maxTryTimes">出错最大尝试次数</param>
        /// <param name="err">错误信息</param>
        /// <returns></returns>
        private static bool AddAndRunNode(long scriptID, long scriptCaseID, int maxTryTimes, ref ErrorInfo err)
        {
            //已添加的节点实例
            Dictionary<long, BLL.EM_SCRIPT_NODE_CASE.Entity> nodeCaseList = BLL.EM_SCRIPT_NODE_CASE.Instance.GetDictionaryByScriptCaseID(scriptCaseID);
            //已添加的节点ID
            Dictionary<long, bool> dicExecutedNodeID = new Dictionary<long, bool>();
            //脚本流运行结果
            Enums.ReturnCode returnCode = Enums.ReturnCode.Success;
            //脚本流实例是否已经完成
            bool isComplete = true;
            //运行中的节点数
            int runningCount = 0;
            //启动之前未完成的节点
            foreach (var kvp in nodeCaseList)
            {
                if (kvp.Value.RUN_STATUS != (short)Enums.RunStatus.Stop)
                {
                    isComplete = false;
                    Node node = new Node(kvp.Value.ID, maxTryTimes);
                    bool isStarted = node.Start(true);
                    if (isStarted == true)
                    {
                        WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】之前未完成的实例【{3}】已经重新启动。", scriptID, scriptCaseID, kvp.Value.SCRIPT_NODE_ID, kvp.Value.ID));
                        runningCount++;
                    }
                }
                else if (kvp.Value.RUN_STATUS == (short)Enums.RunStatus.Stop)
                {
                    //已经结束的节点，记录状态，只要有一个失败则为失败
                    if (kvp.Value.RETURN_CODE == (short)Enums.ReturnCode.Fail)
                    {
                        returnCode = Enums.ReturnCode.Fail;
                    }
                    else if (kvp.Value.RETURN_CODE == (short)Enums.ReturnCode.Warn)
                    {
                        returnCode = Enums.ReturnCode.Warn;
                    }
                }

                //记录已经成功完成的节点
                if (dicExecutedNodeID.ContainsKey(kvp.Value.SCRIPT_NODE_ID) == false)
                {
                    dicExecutedNodeID.Add(kvp.Value.SCRIPT_NODE_ID, (kvp.Value.RUN_STATUS == (short)Enums.RunStatus.Stop) && (kvp.Value.RETURN_CODE == (short)Enums.ReturnCode.Success));
                }
            }

            if (runningCount > 0)
            {
                WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】启动了【{2}】个之前未完成的节点实例。", scriptID, scriptCaseID, runningCount));
            }

            //所有节点及依赖关系
            Dictionary<long, List<long>> dicAllNodes = BLL.EM_SCRIPT_REF_NODE_FORCASE.Instance.GetNodeAndParents(scriptCaseID);

            int addCount = 0;
            //没有失败节点的情况下，添加新节点
            if (returnCode != Enums.ReturnCode.Fail)
            {
                foreach (var kvp in dicAllNodes)
                {
                    if (dicExecutedNodeID.ContainsKey(kvp.Key) == false)
                    {
                        //无父节点或所有父节点均已经执行完成
                        bool isNeedAdd = kvp.Value.Count < 1;
                        if (kvp.Value.Count > 0)
                        {
                            isNeedAdd = true;
                            foreach (long pNodeID in kvp.Value)
                            {
                                if (dicExecutedNodeID.ContainsKey(pNodeID) == false)
                                {
                                    isNeedAdd = false;
                                    break;
                                }
                                if (dicExecutedNodeID[pNodeID] == false)
                                {
                                    isNeedAdd = false;
                                    break;
                                }
                            }
                        }

                        //需要添加节点
                        if (isNeedAdd == true)
                        {
                            isComplete = false;
                            long nodeCaseID = BLL.EM_SCRIPT_NODE_CASE.Instance.AddReturnCaseID(scriptID, scriptCaseID, kvp.Key);
                            if (nodeCaseID > 0)
                            {
                                WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】成功添加节点实例【{3}】。", scriptID, scriptCaseID, kvp.Key, nodeCaseID));
                                //启动节点
                                Node node = new Node(nodeCaseID, maxTryTimes);

                                bool isStarted = node.Start();
                                if (isStarted == true)
                                {
                                    addCount++;
                                }
                            }
                            else
                            {
                                err.IsError = true;
                                err.Message = string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】添加节点实例【{3}】失败。", scriptID, scriptCaseID, kvp.Key, nodeCaseID);
                                return false;
                            }
                        }
                    }
                }
                if (addCount > 0)
                {
                    WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】添加了【{2}】个新的节点实例准备执行。", scriptID, scriptCaseID, addCount));
                }
            }

            //完成或者有失败节点时停止脚本流
            if (isComplete || returnCode == Enums.ReturnCode.Fail)
            {
                var sc = BLL.EM_SCRIPT_CASE.Instance.GetCase(scriptCaseID);
                BLL.EM_SCRIPT_CASE.Instance.SetStop(scriptCaseID, returnCode);
                if (sc != null && sc.IS_SUPERVENE.Value == 1)
                {
                    lock (dicAllNodes)
                    {
                        if (Main.CurUploadCount > 0)
                            Main.CurUploadCount--;
                        WriteLog(0, BLog.LogLevel.DEBUG, string.Format("完成一个并行任务，删除后当前MaxUploadCount{0},CurUploadCount{1}。", Main.MaxUploadCount.ToString(), Main.CurUploadCount));
                    }
                }
                else
                {
                    lock (dicAllNodes)
                    {
                        if (Main.CurMonitCount > 0)
                            Main.CurMonitCount--;
                        WriteLog(0, BLog.LogLevel.DEBUG, string.Format("完成一个监控任务，删除后当前CurMonitCount{0}", Main.CurMonitCount.ToString()));
                    }
                }

                WriteLog(scriptCaseID, BLog.LogLevel.DEBUG, string.Format("脚本流【{0}】的实例【{1}】所有节点已经执行完成，执行结果【{2}】。", scriptID, scriptCaseID, returnCode.ToString()));
            }
            return true;
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="scriptCaseID">脚本流实例ID</param>
        /// <param name="level">日志等级</param>
        /// <param name="message">日志内容</param>
        /// <param name="sql">SQL语句</param>
        private static void WriteLog(long scriptCaseID, BLog.LogLevel level, string message, string sql = "")
        {
            //写日志文件
            BLog.Write(level, message);
            try
            {
                //写数据库表
                if (scriptCaseID > 0)
                {
                    BLL.EM_SCRIPT_CASE_LOG.Instance.Add(scriptCaseID, level.GetHashCode(), message, sql);
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "写日志到脚本流日志表出错：" + ex.ToString());
            }
        }
    }
}
