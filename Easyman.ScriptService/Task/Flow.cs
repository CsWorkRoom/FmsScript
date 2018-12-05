using Easyman.Librarys.Cron;
using Easyman.Librarys.DBHelper;
using Easyman.Librarys.Log;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.ScriptService.Task
{
    /// <summary>
    /// 脚本流，继承自计划任务，到时即生成脚本实例，并创建该脚本实例的相关表
    /// </summary>
    public class Flow : BCron
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">任务流ID</param>
        /// <param name="expression">时间表达式</param>
        public Flow(int id, string expression) : base(id, expression)
        {
            WriteLog(0, BLog.LogLevel.INFO, string.Format("脚本流【{0}】已经初始化，将按照时间表达式【{1}】定时执行。如在运行中修改时间表达式，需要至少提前1分钟。", id, expression));
        }

        /// <summary>
        /// 到达时间点之后，执行具体任务（本方法由基类自动调用，不需要在外部来调用）
        /// </summary>
        /// <param name="runTime">当前时间点</param>
        /// <returns></returns>
        protected override bool Execute(DateTime runTime)
        {
            if (Main.IsRun == false)
            {
                WriteLog(0, BLog.LogLevel.INFO, string.Format("服务已经停止，脚本流【{0}】将不会执行任务。", ID));
                return false;
            }

            //WriteLog(0, BLog.LogLevel.DEBUG, string.Format("脚本流【{0}】即将获取执行中的实例，如果有实例处于运行中，将不会创建新实例。", ID));

            var scr = BLL.EM_SCRIPT.Instance.GetScriptSupervene(ID);
            //WriteLog(0, BLog.LogLevel.DEBUG, string.Format("获取是否为并行任务。"));
            if (scr != null && scr.ID > 0)
            {
                WriteLog(0, BLog.LogLevel.DEBUG, string.Format("获取当前任务组【" + ID + "】为并行任务。"));
                try
                {
                    var ipNotLists = global.OpIpNotList("getall");
                    BLog.Write(BLog.LogLevel.INFO, "1输出未在线的ip：" + string.Join(",", ipNotLists.Select(p => p.V)));

                    int effectCount = global.GetEffectMonitKVCount();
                    List<KV> kvs = global.OpMonitKVList("getall");
                    WriteLog(0, BLog.LogLevel.INFO, string.Format("输出内存在写入的拷贝文件数：" + kvs.Count));
                    WriteLog(0, BLog.LogLevel.INFO, string.Format("输出有效的待拷贝文件数：" + effectCount));
                    if (effectCount > 0)
                    { }
                    else
                    {
                        WriteLog(0, BLog.LogLevel.INFO, string.Format("并行任务【自动上传文件】无待上传文件，将不创建任务组实例"));
                        return false;
                    }
                    #region 判断监控文件表中是否有待处理的monit_file. 没有将跳出任务组实例的创建--作废
                    //                   //string sql = string.Format(@"select count(1) from FM_MONIT_FILE where COPY_STATUS=0 or COPY_STATUS=3");
                    //                   #region 再次验证和清理未在线终端
                    //                   //var ipArr = global.ipList.ToArray();
                    //                   //for (int i = 0; i < ipArr.Count(); i++)
                    //                   //{
                    //                   //    if (Request.PingIP(ipArr[i].Value) && global.ipList.ContainsKey(ipArr[i].Key))
                    //                   //    {
                    //                   //        global.ipList.Remove(ipArr[i].Key);//移除已在线的终端
                    //                   //    }
                    //                   //}
                    //                   var ipNotLists = global.OpIpNotList("getall");
                    //                   if (ipNotLists != null && ipNotLists.Count > 0)
                    //                   {
                    //                       int cnt = ipNotLists.Count;
                    //                       for (int i = cnt - 1; i >= 0; i--)
                    //                       {
                    //                           var item = ipNotLists[i];
                    //                           if (Librarys.ApiRequest.Request.PingIP(item.V))
                    //                           {
                    //                               global.OpIpNotList("remove", item);
                    //                           }
                    //                       }
                    //                       ipNotLists = global.OpIpNotList("getall");

                    //                   }
                    //                   #endregion
                    //                   string sql = string.Format(@"       SELECT ID, COMPUTER_ID
                    // FROM (SELECT A.ID, A.COMPUTER_ID, ROW_NUMBER () OVER (ORDER BY A.ID) RN
                    //         FROM FM_MONIT_FILE A
                    //              LEFT JOIN
                    //              (    SELECT DISTINCT REGEXP_SUBSTR ('{0}',
                    //                                                  '[^,]+',
                    //                                                  1,
                    //                                                  LEVEL)
                    //                                      AS COMPUTER_ID
                    //                     FROM DUAL
                    //               CONNECT BY REGEXP_SUBSTR ('{0}',
                    //                                         '[^,]+',
                    //                                         1,                                          
                    //                                         LEVEL)
                    //                             IS NOT NULL) C
                    //                 ON (A.COMPUTER_ID = C.COMPUTER_ID)
                    //            LEFT JOIN FM_FILE_FORMAT F ON (F.ID=A.FILE_FORMAT_ID) 
                    //        WHERE     NVL (C.COMPUTER_ID, 0) = 0  AND F.NAME<>'Folder'
                    //              AND (A.COPY_STATUS = 0 OR A.COPY_STATUS = 3))
                    //WHERE RN <={1}", string.Join(",", ipNotLists.Select(p => p.K).Distinct()), Main.EachSearchUploadCount);

                    //                   //string sql = string.Format(@"  SELECT A.ID,A.COMPUTER_ID
                    //                   //            FROM FM_MONIT_FILE A
                    //                   //             LEFT JOIN FM_file_FORMAT F ON A.FILE_FORMAT_ID=F.ID
                    //                   //           WHERE     (A.COPY_STATUS = 0 OR A.COPY_STATUS = 3) and F.NAME<>'Folder'
                    //                   //                 AND ( ({0} = 0) OR ({0} > 0 AND A.COMPUTER_ID NOT IN ({1})))
                    //                   //                 AND ROWNUM <= {2}
                    //                   //        ORDER BY A.ID", ipNotLists.Count,
                    //                   //               ipNotLists.Count == 0 ? "0" : string.Join(",", ipNotLists.Select(p => p.K).Distinct()), Main.EachSearchUploadCount);
                    //                   DataTable dt = null;
                    //                   using (BDBHelper dbop = new BDBHelper())
                    //                   {
                    //                       dt = dbop.ExecuteDataTable(sql);
                    //                   }
                    //                   if (dt != null && dt.Rows.Count > 0)
                    //                   { }
                    //                   else
                    //                   {
                    //                       WriteLog(0, BLog.LogLevel.INFO, string.Format("并行任务【自动上传文件】无待上传文件，将不创建任务组实例", string.Format("执行的sql语句为：{0}", sql), ID));
                    //                       return false;
                    //                   }
                    #endregion
                }
                catch (Exception ex)
                {
                    WriteLog(0, BLog.LogLevel.INFO, string.Format("监控错误：{0}", ex.Message));
                    return false;
                }

                lock (this)
                {
                    int curNum = 0;
                    WriteLog(0, BLog.LogLevel.DEBUG, string.Format("在创建脚本流实例前的判断：curNum{0},MaxUploadCount{1},已CurUploadCount{2}。", curNum, Main.MaxUploadCount.ToString(), Main.CurUploadCount));
                    while (curNum < Main.MaxUploadCount && Main.CurUploadCount < Main.MaxUploadCount)
                    {
                        WriteLog(0, BLog.LogLevel.DEBUG, string.Format("curNum{0},MaxUploadCount{1},已有的上传CurUploadCount{2}。", curNum, Main.MaxUploadCount.ToString(), Main.CurUploadCount));
                        long scriptCaseID = 0;
                        try
                        {
                            //先尝试取之前未完成的节点(排除并发任务组)+执行中的任务实例
                            //BLL.EM_SCRIPT_CASE.Entity scriptCaseEntity = BLL.EM_SCRIPT_CASE.Instance.GetRunningCase(ID);
                            //if (scriptCaseEntity != null)
                            //{
                            //    WriteLog(scriptCaseEntity.ID, BLog.LogLevel.DEBUG, string.Format("脚本流【{0}】找到了之前未运行完成的实例【{1}】，本次任务将不会创建新实例。", ID, scriptCaseEntity.ID));
                            //    return false;
                            //}
                            //else
                            //{
                            ErrorInfo err = new ErrorInfo();

                            if (CreateScriptCase(ID, ref scriptCaseID, ref err) == true)
                            {
                                WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】成功创建了新的实例【{1}】，等待执行。", ID, scriptCaseID));
                                Main.CurUploadCount++;
                            }
                            else
                            {
                                WriteLog(scriptCaseID, BLog.LogLevel.WARN, err.Message);
                                //标记为失败状态
                                if (scriptCaseID > 0)
                                {
                                    BLL.EM_SCRIPT_CASE.Instance.SetFail(scriptCaseID);
                                }
                                return false;
                            }
                            //}


                            //_dicTaskers.Add(ID, null);
                        }
                        catch (Exception ex)
                        {
                            WriteLog(scriptCaseID, BLog.LogLevel.WARN, "创建脚本流实例发生了未知错误。" + ex.ToString());
                            //标记为失败状态
                            if (scriptCaseID > 0)
                            {
                                BLL.EM_SCRIPT_CASE.Instance.SetFail(scriptCaseID);
                            }
                            return false;
                        }
                        curNum++;
                    }
                }
            }
            else
            {
                #region
                long scriptCaseID = 0;
                try
                {
                    //先尝试取之前未完成的节点(排除并发任务组)+执行中的任务实例
                    BLL.EM_SCRIPT_CASE.Entity scriptCaseEntity = BLL.EM_SCRIPT_CASE.Instance.GetRunningCase(ID);
                    if (scriptCaseEntity != null)
                    {
                        WriteLog(scriptCaseEntity.ID, BLog.LogLevel.DEBUG, string.Format("脚本流【{0}】找到了之前未运行完成的实例【{1}】，本次任务将不会创建新实例。", ID, scriptCaseEntity.ID));
                        return false;
                    }
                    else
                    {
                        ErrorInfo err = new ErrorInfo();

                        if (CreateScriptCase(ID, ref scriptCaseID, ref err,false) == true)
                        {
                            WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】成功创建了新的实例【{1}】，等待执行。", ID, scriptCaseID));
                        }
                        else
                        {
                            WriteLog(scriptCaseID, BLog.LogLevel.WARN, err.Message);
                            //标记为失败状态
                            if (scriptCaseID > 0)
                            {
                                BLL.EM_SCRIPT_CASE.Instance.SetFail(scriptCaseID);
                            }
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(scriptCaseID, BLog.LogLevel.WARN, "创建脚本流实例发生了未知错误。" + ex.ToString());
                    //标记为失败状态
                    if (scriptCaseID > 0)
                    {
                        BLL.EM_SCRIPT_CASE.Instance.SetFail(scriptCaseID);
                    }
                    return false;
                }
                #endregion
            }


            return true;
        }

        /// <summary>
        /// 创建一个脚本流的实例
        /// </summary>
        /// <param name="scriptID">脚本流ID</param>
        /// <param name="scriptCaseID">脚本流实例ID</param>
        /// <param name="err">错误信息</param>
        /// <returns></returns>
        public static bool CreateScriptCase(long scriptID, ref long scriptCaseID, ref ErrorInfo err,bool isSetRuning=true)
        {
            //创建脚本流实例
            scriptCaseID = BLL.EM_SCRIPT_CASE.Instance.AddReturnCaseID(scriptID);
            if (scriptCaseID < 1)
            {
                err.IsError = true;
                err.Message = "创建脚本流实例失败。";
                return false;
            }

            WriteLog(scriptCaseID, BLog.LogLevel.DEBUG, string.Format("脚本流【{0}】添加了任务实例【{1}】，将为它创建节点实例的各项配置。", scriptID, scriptCaseID));

            ////创建节点流位置实例
            //int pCount = BLL.EM_NODE_POSITION_FORCASE.Instance.Add(scriptCaseID, BLL.EM_NODE_POSITION.Instance.GetListByScriptID(scriptID));
            //WriteLog(scriptCaseID, BLog.LogLevel.DEBUG, string.Format("脚本流【{0}】的实例【{1}】成功创建节点位置实例，共有【{2}】个节点位置信息，用于前台页面显示。", scriptID, scriptCaseID, pCount));

            ////创建节点流连线实例
            //int cCount = BLL.EM_CONNECT_LINE_FORCASE.Instance.Add(scriptCaseID, BLL.EM_CONNECT_LINE.Instance.GetListByScriptID(scriptID));
            //WriteLog(scriptCaseID, BLog.LogLevel.DEBUG, string.Format("脚本流【{0}】的实例【{1}】成功创建节点连线实例，共有【{2}】个节点连线信息，用于前台页面显示。", scriptID, scriptCaseID, cCount));

            //创建节点流配置实例
            List<long> nodeList = BLL.EM_SCRIPT_REF_NODE_FORCASE.Instance.AddReturnNodeIDList(scriptID, scriptCaseID);
            if (nodeList.Count < 1)
            {
                err.IsError = true;
                err.Message = string.Format("脚本流【{0}】的实例【{1}】创建节点流实例失败。", scriptID, scriptCaseID);
                return false;
            }
            WriteLog(scriptCaseID, BLog.LogLevel.DEBUG, string.Format("脚本流【{0}】的实例【{1}】成功创建节点顺序实例，共有【{2}】个节点需要按顺序执行。", scriptID, scriptCaseID, nodeList.Count));
            //修改当前实例的状态为“执行中”
            if (isSetRuning)
                BLL.EM_SCRIPT_CASE.Instance.UpdateRunStatus(scriptCaseID, Enums.RunStatus.Excute);

            //复制节点配置
            List<long> nodeCaseList = BLL.EM_SCRIPT_NODE_FORCASE.Instance.AddCaseReturnList(scriptCaseID, nodeList);
            if (nodeCaseList.Count < 1)
            {
                err.IsError = true;
                err.Message = string.Format("脚本流【{0}】的实例【{1}】创建节点配置实例失败。", scriptID, scriptCaseID);
                return false;
            }
            WriteLog(scriptCaseID, BLog.LogLevel.DEBUG, string.Format("脚本流【{0}】的实例【{1}】成功创建节点配置实例，共有【{2}】个节点需要按顺序执行。", scriptID, scriptCaseID, nodeCaseList.Count));

            return true;
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="scriptCaseID">脚本流实例ID</param>
        /// <param name="level">日志等级</param>
        /// <param name="message">日志内容</param>
        /// <param name="sql">SQL语句</param>
        protected static void WriteLog(long scriptCaseID, BLog.LogLevel level, string message, string sql = "")
        {
            //写日志文件
            BLog.Write(level, message);
            try
            {
                ////写数据库表
                //if (scriptCaseID > 0)
                //{
                //    BLL.EM_SCRIPT_CASE_LOG.Instance.Add(scriptCaseID, level.GetHashCode(), message, sql);
                //}
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "写日志到脚本流日志表出错：" + ex.ToString());
            }
        }
    }
}
