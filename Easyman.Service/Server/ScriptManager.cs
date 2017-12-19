using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Service.Domain;
using Easyman.Service.Common;

namespace Easyman.Service.Server
{
    public class ScriptManager
    {
        #region 向EM_SCRIPT_CASE_LOG、EM_SCRIPT_NODE_CASE_LOG写入执行日志

        /// <summary>
        /// 写入一条[脚本流实例]日志
        /// </summary>
        /// <param name="logMsg">日志内容</param>
        /// <param name="sqlMsg">sql内容-含错误语句</param>
        /// <param name="scriptCaseID">脚本节点实例ID</param>
        /// <param name="err">错误类对象</param>
        /// <returns></returns>
        public static bool LogForScriptCase(string logMsg, string sqlMsg, long? scriptCaseID, ref ErrorInfo err)
        {
            if (scriptCaseID == null) return false;

            using (DBEntities db = new DBEntities())
            {
                //本项目为强制设置节点实例和其日志表的主外键关系

                //向脚本节点实例日志表中写入日志(对象初始化器)
                EM_SCRIPT_CASE_LOG _log = new EM_SCRIPT_CASE_LOG
                {
                    ID = Fun.GetSeqID<EM_SCRIPT_CASE_LOG>(),//获取自增主键ID
                    SCRIPT_CASE_ID = scriptCaseID,
                    LOG_TIME = DateTime.Now,


                    LOG_MSG = logMsg,
                    SQL_MSG = sqlMsg
                };
                db.EM_SCRIPT_CASE_LOG.Add(_log);
                try
                {
                    //保存日志
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    err.Message = e.Message;
                    err.IsError = true;
                    err.Excep = e;
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// 写入一条[脚本节点实例]日志
        /// </summary>
        /// <param name="logMsg">日志内容</param>
        /// <param name="sqlMsg">sql内容-含错误语句</param>
        /// <param name="nodeCaseID">脚本节点实例ID</param>
        /// <param name="err">错误类对象</param>
        /// <returns></returns>
        public static bool LogForNodeCase(string logMsg, string sqlMsg, long? nodeCaseID, ref ErrorInfo err)
        {
            if (nodeCaseID == null) return false;

            using (DBEntities db = new DBEntities())
            {
                //本项目为强制设置节点实例和其日志表的主外键关系

                //向脚本节点实例日志表中写入日志(对象初始化器)
                EM_SCRIPT_NODE_CASE_LOG _log = new EM_SCRIPT_NODE_CASE_LOG
                {
                    ID = Fun.GetSeqID<EM_SCRIPT_NODE_CASE_LOG>(),//获取自增主键ID
                    SCRIPT_NODE_CASE_ID = nodeCaseID,
                    LOG_TIME = DateTime.Now,
                    LOG_MSG = logMsg,
                    SQL_MSG = sqlMsg
                };
                db.EM_SCRIPT_NODE_CASE_LOG.Add(_log);
                try
                {
                    //保存日志
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    err.Message = e.Message;
                    err.IsError = true;
                    err.Excep = e;
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region 脚本处理相关函数

        /// <summary>
        /// 启动一个脚本流实例
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="statusModel"></param>
        /// <param name="err"></param>
        public static EM_SCRIPT_CASE StartScriptCase(long? scriptID, PubEnum.StatusModel statusModel, ref ErrorInfo err)
        {
            string msg = "";
            using (DBEntities db = new DBEntities())
            {
                if (scriptID == null)
                {
                    err.IsError = true;
                    err.Message = "传入的scriptID值不能为空";
                    return null;
                }

                //根据scriptID获取脚本流对象
                EM_SCRIPT _script = GetScripByID(scriptID, db, ref err);
                if (err.IsError) return null;

                //验证当前脚本流能否启动一个脚本流实例
                //状态不为‘等待’或‘执行中’时，允许添加实例
                EM_SCRIPT_CASE _scriptCase = GetEffectScriptCase(scriptID, db);
                if (_scriptCase == null)
                {
                    #region 添加脚本流实例
                    var _scriptCa = AddScriptCase(_script, statusModel, db, ref err);
                    long? _scriptCaseID = _scriptCa.ID;
                    //写入脚本流实例日志
                    msg = "添加脚本流【" + _script.NAME + "】实例，实例ID为【" + _scriptCaseID.ToString() + "】";
                    //写入日志
                    LogForScriptCase(msg, "", _scriptCaseID, ref err);
                    #endregion

                    #region 添加脚本流实例的第一组脚本节点实例
                    //获取脚本流的首组脚本节点
                    List<EM_SCRIPT_REF_NODE> _scRefNodeList = db.EM_SCRIPT.Find(scriptID).EM_SCRIPT_REF_NODE.Where(p => p.PARENT_NODE_ID == null).ToList();
                    //为首组脚本添加节点实例
                    foreach (var _scNode in _scRefNodeList)
                    {
                        //添加一个节点实例
                        var snc = AddScriptNodeCase(_scNode.CURR_NODE_ID, _scriptCaseID, db, ref err,1);
                        var _snCaseID = snc.ID;
                        //写入脚本节点实例日志信息
                        if (!err.IsError)
                        {
                            msg = string.Format("添加实例为【{1}】脚本流【{0}】的首组脚本节点【{2}】实例，实例编号为【{3}】",
                                _script.NAME, _scriptCaseID.ToString(),
                                db.EM_SCRIPT_NODE.Find(_scNode.CURR_NODE_ID).NAME, _snCaseID);
                            //msg += "\r\n等待调度服务启动！";
                            //写入日志
                            LogForNodeCase(msg, "", _snCaseID, ref err);
                            LogForScriptCase(msg, "", _scriptCaseID, ref err);
                        }
                    }
                    //为当前脚本流【复制】节点配置及内容
                    LogForScriptCase("开始为脚本流【"+ _script .NAME+ "】实例【"+ _scriptCaseID .ToString()+ "】复制脚本节点及配置信息", "", _scriptCaseID, ref err);
                    CopyScriptNodeForCase(_scriptCaseID, scriptID, _script.NAME, db, ref err);
                    if (err.IsError)
                    {
                        LogForScriptCase("复制脚本流【" + _script.NAME + "】实例【" + _scriptCaseID.ToString() + "】脚本节点及配置信息【失败】：" + err.Message, "", _scriptCaseID, ref err);
                    }
                    else
                    {
                        LogForScriptCase("复制脚本流【" + _script.NAME + "】实例【" + _scriptCaseID.ToString() + "】脚本节点及配置信息【成功】！", "", _scriptCaseID, ref err);
                    }
                    #endregion

                    try
                    {
                        db.SaveChanges();
                        LogForScriptCase("启动脚本流【" + _script.NAME + "】实例【"+ _scriptCaseID .ToString()+ "】【成功】!", "", _scriptCaseID, ref err);
                        return _scriptCa;
                    }
                    catch (Exception e)
                    {
                        err.IsError = true;
                        err.Message = "启动脚本流【" + _script.NAME + "】实例【"+ _scriptCaseID.ToString() + "】【失败】：" + e.Message;
                        //写日志
                        LogForScriptCase(err.Message, err.Message, _scriptCaseID, ref err);
                        return null;
                    }
                }
                else
                {
                    err.IsError = true;
                    err.Message = "添加脚本流实例【失败】！脚本流【" + _script.NAME + "】已存在正运行的实例！";
                    return null;
                }
            }
        }

        /// <summary>
        /// 复制脚本流节点配置信息（FOR ScriptCase）
        /// </summary>
        /// <param name="scriptCaseID">脚本流实例ID</param>
        /// <param name="scriptID">脚本流ID</param>
        /// <param name="db"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static void CopyScriptNodeForCase(long? scriptCaseID, long? scriptID, string scriptName, DBEntities db, ref ErrorInfo err)
        {
            if (scriptID == null || scriptCaseID == null)
            {
                err.IsError = true;
                err.Message = "传入的scriptID和scriptCaseID值不能为空";
                return;
            }
            var srnList = db.EM_SCRIPT_REF_NODE.Where(p => p.SCRIPT_ID == scriptID).ToList();
            if (srnList == null || srnList.Count == 0)
            {
                err.IsError = true;
                err.Message = string.Format(@"未找到脚本流【{0}】的节点配置信息", scriptName);
                return;
            }

            #region 逐个复制脚本节点流程配置
            foreach (var srn in srnList)
            {
                EM_SCRIPT_REF_NODE_FORCASE srnCase = new EM_SCRIPT_REF_NODE_FORCASE();
                Fun.ClassToCopy(srn, srnCase);
                srnCase.ID = Fun.GetSeqID<EM_SCRIPT_REF_NODE_FORCASE>();//避免ID重复
                srnCase.SCRIPT_CASE_ID = scriptCaseID;
                
                db.EM_SCRIPT_REF_NODE_FORCASE.Add(srnCase);
            }

            #endregion 

            #region 逐个复制连接线信息
            var lineList = db.EM_CONNECT_LINE.Where(p => p.SCRIPT_ID == scriptID).ToList();
            if (lineList != null && lineList.Count > 0)
            {
                foreach (var line in lineList)
                {
                    EM_CONNECT_LINE_FORCASE lineForcase = new EM_CONNECT_LINE_FORCASE();
                    Fun.ClassToCopy(line, lineForcase);
                    lineForcase.ID = Fun.GetSeqID<EM_CONNECT_LINE_FORCASE>();
                    lineForcase.SCRIPT_CASE_ID = scriptCaseID;
                    db.EM_CONNECT_LINE_FORCASE.Add(lineForcase);
                }
            }
            #endregion

            #region 逐个复制脚本节点内容
            var nodeIDList = srnList.Select(p => p.CURR_NODE_ID).Distinct();
            foreach (var nid in nodeIDList)
            {
                var node = db.EM_SCRIPT_NODE.Find(nid);
                EM_SCRIPT_NODE_FORCASE snforc = new EM_SCRIPT_NODE_FORCASE();
                Fun.ClassToCopy(node, snforc);
                snforc.ID = Fun.GetSeqID<EM_SCRIPT_NODE_FORCASE>();//避免ID重复
                snforc.SCRIPT_NODE_ID = nid;
                snforc.SCRIPT_CASE_ID = scriptCaseID;
                db.EM_SCRIPT_NODE_FORCASE.Add(snforc);
            }
            #endregion

            #region 逐个复制节点位置信息
            var positionList = db.EM_NODE_POSITION.Where(p => p.SCRIPT_ID == scriptID).ToList();
            if (positionList != null && positionList.Count > 0)
            {
                foreach (var pos in positionList)
                {
                    EM_NODE_POSITION_FORCASE posForcase = new EM_NODE_POSITION_FORCASE();
                    Fun.ClassToCopy(pos, posForcase);
                    posForcase.ID = Fun.GetSeqID<EM_NODE_POSITION_FORCASE>();
                    posForcase.SCRIPT_CASE_ID = scriptCaseID;
                    db.EM_NODE_POSITION_FORCASE.Add(posForcase);
                }
            }

            #endregion
            //try
            //{
            //    db.SaveChanges();
            //    return true;
            //}
            //catch (Exception e)
            //{
            //    err.IsError = false;
            //    err.Message = e.Message;
            //    err.Excep = e;
            //    return false;
            //}
        }

        public static bool AddNextScritNodeCase(long? scripNodeCaseID, ref ErrorInfo err)
        {
            using (DBEntities db = new DBEntities())
            {
                var snodeCase = db.EM_SCRIPT_NODE_CASE.Find(scripNodeCaseID);
                return AddNextScritNodeCase(snodeCase.SCRIPT_NODE_ID, snodeCase.SCRIPT_CASE_ID, scripNodeCaseID, ref err);
            }
        }

        /// <summary>
        /// 添加其依赖脚本节点的实例（根据当前脚本节点实例和脚本节点ID）
        /// </summary>
        /// <param name="scriptNodeID"></param>
        /// <param name="scriptCaseID"></param>
        /// <param name="scripNodeCaseID"></param>
        /// <param name="db"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static bool AddNextScritNodeCase(long? scriptNodeID, long? scriptCaseID, long? scripNodeCaseID, ref ErrorInfo err)
        {
            string msg = "";
            if (scriptCaseID == null || scriptNodeID == null || scripNodeCaseID == null)
            {
                err.IsError = true;
                err.Message = "传入的scriptNodeID、scriptCaseID、scripNodeCaseID值不能为空";
                return false;
            }
            using (DBEntities db = new DBEntities())
            {
                //获取当前脚本流实例
                EM_SCRIPT_CASE _scriptCase = db.EM_SCRIPT_CASE.Find(scriptCaseID);
                if (_scriptCase == null)
                {
                    err.IsError = true;
                    err.Message = "未找到脚本实例[" + scriptCaseID.ToString() + "]";
                    return false;
                }
                //上级节点(脚本流实例)
                var snode = db.EM_SCRIPT_NODE_FORCASE.FirstOrDefault(p => p.SCRIPT_NODE_ID == scriptNodeID && p.SCRIPT_CASE_ID == _scriptCase.ID);
                var snodeCase = db.EM_SCRIPT_NODE_CASE.Find(scripNodeCaseID);//上级节点实例

                //根据上级节点获取其子节点集合
                List<EM_SCRIPT_REF_NODE_FORCASE> _scriptNodeList = db.EM_SCRIPT_REF_NODE_FORCASE.Where
                    (p => p.SCRIPT_ID == _scriptCase.SCRIPT_ID &&
                        p.PARENT_NODE_ID == scriptNodeID &&
                        p.SCRIPT_CASE_ID == scriptCaseID).ToList();

                if (_scriptNodeList != null && _scriptNodeList.Count > 0)
                {
                    //foreach (var _nextSNode in _scriptNodeList)
                    //{
                    for (int i = 0; i < _scriptNodeList.Count;i++ )
                    {
                        var _nextSNode = _scriptNodeList[i];
                        var scriptNode = db.EM_SCRIPT_NODE_FORCASE.FirstOrDefault(p => p.SCRIPT_CASE_ID == scriptCaseID && p.SCRIPT_NODE_ID == _nextSNode.CURR_NODE_ID);
                        //获取_nextSNode节点的父节点
                        var nodeParentList = db.EM_SCRIPT_REF_NODE_FORCASE.
                            Where(p => p.CURR_NODE_ID == _nextSNode.CURR_NODE_ID &&
                                p.SCRIPT_ID == _scriptCase.SCRIPT_ID &&
                                p.SCRIPT_CASE_ID == scriptCaseID).Select(p => p.PARENT_NODE_ID).ToList();

                        //_nextSNode的父节点实例数（当前脚本流实例）
                        //var caseCount = db.EM_SCRIPT_NODE_CASE.Where(p => p.SCRIPT_CASE_ID == scriptCaseID && nodeParentList.Contains(p.SCRIPT_NODE_ID)).Count();

                        var caseCount = db.EM_SCRIPT_NODE_CASE.Where(p => p.SCRIPT_CASE_ID == scriptCaseID &&
                            nodeParentList.Contains(p.SCRIPT_NODE_ID) &&
                            p.RUN_STATUS == (short)PubEnum.RunStatus.Stop &&
                            p.RETURN_CODE == (short)PubEnum.ReturnCode.Success).Count();

                        //父节点数=父节点实例数，则添加子实例
                        if (caseCount != 0 && nodeParentList.Count == caseCount)
                        {
                            //添加子节点实例
                            var _ncase = AddScriptNodeCase(_nextSNode.CURR_NODE_ID, scriptCaseID, db, ref err);
                            var _snCaseID = _ncase.ID;
                            if (err.IsError)
                            {
                                LogForScriptCase("添加脚本节点【" + snode.NAME + "】的下级节点【" + scriptNode.NAME + "】的实例【失败】：" + err.Message, "", scriptCaseID, ref err);
                                return false;
                            }
                            msg = string.Format("添加脚本节点【{0}】的下级节点【{1}】的实例，实例编号为【{2}】",
                                snode.NAME, scriptNode.NAME, _snCaseID.ToString());
                            //msg += "\r\n等待调度服务执行！";
                            //写入日志
                            LogForNodeCase(msg, "", _snCaseID, ref err);
                            LogForScriptCase(msg, "", scriptCaseID, ref err);

                            try
                            {
                                db.SaveChanges();//集中保存
                                msg = string.Format(@"脚本节点【{0}】的下级节点【{1}】实例【{2}】添加【成功】", snode.NAME, scriptNode.NAME, _snCaseID.ToString());
                                //写入日志
                                LogForNodeCase(msg, "", snodeCase.ID, ref err);
                                LogForScriptCase(msg, "", scriptCaseID, ref err);
                            }
                            catch (Exception e)
                            {
                                msg = string.Format(@"脚本节点【{0}】的依赖节点【{1}】实例【{2}】添加【失败】：{3}", snode.NAME, scriptNode.NAME, _snCaseID.ToString(), e.Message);
                                //写入日志
                                LogForNodeCase(msg, "", snodeCase.ID, ref err);
                                LogForScriptCase(msg, "", scriptCaseID, ref err);
                            }
                        }
                        else//未满足，不新增节点实例
                        {
                            msg = string.Format("脚本节点【{0}】的实例添加【失败】：其父节点实例未全部执行，等待父节点实例全部完毕",
                                scriptNode.NAME);
                            //写入脚本流日志
                            LogForScriptCase(msg, "", scriptCaseID, ref err);
                        }
                    }
                }
                else//未找到子节点
                {
                    //判断当前脚本流是否执行完毕。判断数量是否相等，考虑调用当前方法前提代码部分
                    var nodeListCount = db.EM_SCRIPT_NODE_FORCASE.Count(p => p.SCRIPT_CASE_ID == scriptCaseID);
                    var caseListCount = db.EM_SCRIPT_NODE_CASE.Count(p => p.SCRIPT_CASE_ID == scriptCaseID &&
                        p.RETURN_CODE == (short)PubEnum.ReturnCode.Success);
                    //脚本流的所有执行成功节点实例数量=脚本流所涉及的节点数
                    if (nodeListCount == caseListCount)
                    {
                        //修改脚本流实例状态
                        ModifyScriptCase(scriptCaseID, PubEnum.RunStatus.Stop, PubEnum.ReturnCode.Success, ref err);
                        if (err.IsError)
                        {
                            LogForScriptCase("将脚本流【" + _scriptCase.NAME + "】实例编号【" + _scriptCase.ID + "】状态修改为【执行成功】：但修改【失败】", "", scriptCaseID, ref err);
                        }
                        msg = string.Format(@"恭喜！脚本流【{0}】实例编号【{1}】所有节点实例执行【成功】", _scriptCase.NAME, _scriptCase.ID);
                        //写入日志
                        LogForNodeCase(msg, "", snodeCase.ID, ref err);
                        LogForScriptCase(msg, "", scriptCaseID, ref err);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 给指定脚本节点添加一个实例
        /// </summary>
        /// <param name="scriptNodeID"></param>
        /// <param name="scriptCaseID"></param>
        /// <param name="db"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static EM_SCRIPT_NODE_CASE AddScriptNodeCase(long? scriptNodeID, long? scriptCaseID, DBEntities db, ref ErrorInfo err, int isFirst = 0)
        {
            if (scriptCaseID == null || scriptNodeID == null)
            {
                err.IsError = true;
                err.Message = "传入的脚本节点ID或脚本流实例ID为空";
                return null;
            }

            var _scriptNode = new ScriptNode();

            if (isFirst == 1)
            {
                var sn = db.EM_SCRIPT_NODE.Find(scriptNodeID);
                if (sn == null)
                {
                    err.IsError = true;
                    err.Message = "未找到传入的脚本节点[" + scriptNodeID.ToString() + "]";
                    return null;
                }
                else
                {
                    _scriptNode = Fun.ClassToCopy(sn, _scriptNode);
                }
            }
            else
            {
                //var sn = db.EM_SCRIPT_NODE_FORCASE.Find(scriptNodeID);
                var sn = db.EM_SCRIPT_NODE_FORCASE.FirstOrDefault(p => p.SCRIPT_CASE_ID == scriptCaseID && p.SCRIPT_NODE_ID == scriptNodeID);
                if (sn == null)
                {
                    err.IsError = true;
                    err.Message = "未找到传入的脚本节点[" + scriptNodeID.ToString() + "]";
                    return null;
                }
                else
                {
                    _scriptNode = Fun.ClassToCopy(sn, _scriptNode);
                }
            }
            EM_SCRIPT_NODE_CASE scriptNodeCase = new EM_SCRIPT_NODE_CASE
            {
                ID = Fun.GetSeqID<EM_SCRIPT_NODE_CASE>(),
                SCRIPT_CASE_ID = scriptCaseID,
                SCRIPT_ID = db.EM_SCRIPT_CASE.Find(scriptCaseID).SCRIPT_ID,
                SCRIPT_NODE_ID = scriptNodeID,
                DB_SERVER_ID = _scriptNode.DB_SERVER_ID,
                SCRIPT_MODEL = _scriptNode.SCRIPT_MODEL,
                CONTENT = _scriptNode.CONTENT,
                REMARK = _scriptNode.REMARK,
                E_TABLE_NAME = _scriptNode.E_TABLE_NAME,
                C_TABLE_NAME = _scriptNode.C_TABLE_NAME,
                TABLE_TYPE = _scriptNode.TABLE_TYPE,
                TABLE_MODEL = _scriptNode.TABLE_MODEL,
                CREATE_TIME = DateTime.Now,
                TABLE_SUFFIX = DateTime.Now.Ticks,//赋值方式改为：去获取脚本流实例的ID
                RUN_STATUS = (short)PubEnum.RunStatus.Wait,
            };

            db.EM_SCRIPT_NODE_CASE.Add(scriptNodeCase);
            return scriptNodeCase;

            //try
            //{
            //    db.SaveChanges();
            //    return scriptNodeCase.ID;
            //}
            //catch (Exception e)
            //{
            //    err.IsError = true;
            //    err.Message = e.Message;
            //    err.Excep = e;
            //    return null;
            //}
        }

        /// <summary>
        /// 添加一个脚本流实例
        /// </summary>
        /// <param name="script">脚本流对象</param>
        /// <param name="statusModel">启动模式</param>
        /// <param name="db">DBEntities</param>
        /// <param name="err">ErrorInfo</param>
        /// <returns></returns>
        public static EM_SCRIPT_CASE AddScriptCase(EM_SCRIPT script, PubEnum.StatusModel statusModel, DBEntities db, ref ErrorInfo err)
        {
            EM_SCRIPT_CASE _scriptCase = new EM_SCRIPT_CASE
            {
                ID = Fun.GetSeqID<EM_SCRIPT_CASE>(),
                NAME = script.NAME,
                SCRIPT_ID = script.ID,
                START_TIME = DateTime.Now,
                START_MODEL = (short)statusModel,
                RUN_STATUS = (short)PubEnum.RunStatus.Wait,
                RETRY_TIME = script.RETRY_TIME
            };
            db.EM_SCRIPT_CASE.Add(_scriptCase);

            return _scriptCase;

            //try
            //{
            //    db.SaveChanges();
            //}
            //catch (Exception e)
            //{
            //    err.IsError = true;
            //    err.Message = e.Message;
            //    err.Excep = e;
            //    return _scriptCase.ID;
            //}

            //return null;
        }

        /// <summary>
        /// 修改脚本流实例状态2
        /// </summary>
        /// <param name="scriptCaseID"></param>
        /// <param name="runStatus"></param>
        /// <param name="returnCode"></param>
        /// <param name="err"></param>
        public static void ModifyScriptCase(long? scriptCaseID, PubEnum.RunStatus runStatus, PubEnum.ReturnCode returnCode, ref ErrorInfo err, PubEnum.IsHaveFail IsHaveFail = 0)
        {
            if (scriptCaseID == null)
            {
                err.IsError = true;
                err.Message = "传入的scriptCaseID不能为空";
                return;
            }
            using (DBEntities db = new DBEntities())
            {
                var scase = db.EM_SCRIPT_CASE.Find(scriptCaseID);
                scase.RETURN_CODE = (short)returnCode;
                scase.RUN_STATUS = (short)runStatus;
                scase.END_TIME = DateTime.Now;
                scase.IS_HAVE_FAIL = (short)IsHaveFail;
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    err.IsError = true;
                    err.Message = e.Message;
                }
            }
        }
        /// <summary>
        /// 修改脚本流实例状态1
        /// </summary>
        /// <param name="scriptCaseID"></param>
        /// <param name="runStatus"></param>
        /// <param name="err"></param>
        public static void ModifyScriptCase(long? scriptCaseID, PubEnum.RunStatus runStatus, ref ErrorInfo err, PubEnum.IsHaveFail IsHaveFail = 0)
        {
            if (scriptCaseID == null)
            {
                err.IsError = true;
                err.Message = "传入的scriptCaseID不能为空";
                return;
            }
            using (DBEntities db = new DBEntities())
            {
                var scase = db.EM_SCRIPT_CASE.Find(scriptCaseID);
                scase.IS_HAVE_FAIL = (short)IsHaveFail;
                scase.RUN_STATUS = (short)runStatus;
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    err.IsError = true;
                    err.Message = e.Message;
                }
            }
        }

        /// <summary>
        /// 修改脚本节点实例状态
        /// </summary>
        /// <param name="scriptNodeCaseID"></param>
        /// <param name="runStatus"></param>
        /// <param name="returnCode"></param>
        /// <param name="err"></param>
        public static EM_SCRIPT_NODE_CASE ModifyScriptNodeCase(long? scriptNodeCaseID, PubEnum.RunStatus runStatus, PubEnum.ReturnCode returnCode, ref ErrorInfo err)
        {
            if (scriptNodeCaseID == null)
            {
                err.IsError = true;
                err.Message = "传入的scriptNodeCaseID不能为空";
                return null;
            }
            using (DBEntities db = new DBEntities())
            {
                var scase = db.EM_SCRIPT_NODE_CASE.Find(scriptNodeCaseID);
                scase.RETURN_CODE = (short)returnCode;
                scase.RUN_STATUS = (short)runStatus;
                scase.END_TIME = DateTime.Now;
                if (runStatus == PubEnum.RunStatus.Stop && returnCode == PubEnum.ReturnCode.Fail)
                {
                    scase.RETRY_TIME = scase.RETRY_TIME == null ? 1 : scase.RETRY_TIME + 1;
                }
                try
                {
                    db.SaveChanges();
                    return scase;
                }
                catch (Exception e)
                {
                    err.IsError = true;
                    err.Message = e.Message;
                    return null;
                }
            }
        }

        /// <summary>
        /// 修改脚本节点实例状态
        /// </summary>
        /// <param name="scriptCaseID"></param>
        /// <param name="runStatus"></param>
        /// <param name="err"></param>
        public static void ModifyScriptNodeCase(long? scriptNodeCaseID, PubEnum.RunStatus runStatus, ref ErrorInfo err)
        {
            if (scriptNodeCaseID == null)
            {
                err.IsError = true;
                err.Message = "传入的scriptNodeCaseID不能为空";
                return;
            }
            using (DBEntities db = new DBEntities())
            {
                var scase = db.EM_SCRIPT_NODE_CASE.Find(scriptNodeCaseID);
                scase.RUN_STATUS = (short)runStatus;
                if (runStatus == PubEnum.RunStatus.Wait)
                {
                    //状态修改为等待时，设置启动时间
                    scase.START_TIME = DateTime.Now;
                    scase.RETURN_CODE = null;
                    scase.END_TIME = null;
                }
                if (runStatus == PubEnum.RunStatus.Excute)
                {
                    scase.RETURN_CODE = null;
                    scase.END_TIME = null;
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    err.IsError = true;
                    err.Message = e.Message;
                }
            }
        }
        #endregion

        #region 相关查询

        /// <summary>
        /// 根据脚本ID获取脚本对象
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="db"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static EM_SCRIPT GetScripByID(long? scriptID, DBEntities db, ref ErrorInfo err)
        {
            //验证scriptID是否为空
            if (scriptID == null)
            {
                err.IsError = true;
                err.Message = "传入的ScriptID值不能为空";
                return null;
            }

            EM_SCRIPT _script = db.EM_SCRIPT.Find(scriptID);
            if (_script == null)
            {
                err.IsError = true;
                err.Message = "未找到主键值[" + scriptID.ToString() + "]的脚本流";
                return null;
            }
            return _script;
        }

        /// <summary>
        /// 根据脚本ID获取脚本对象
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="db"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static EM_SCRIPT GetScripByID(long? scriptID,  ref ErrorInfo err)
        {
            //验证scriptID是否为空
            if (scriptID == null)
            {
                err.IsError = true;
                err.Message = "传入的ScriptID值不能为空";
                return null;
            }
            using (DBEntities db = new DBEntities())
            {
                EM_SCRIPT _script = db.EM_SCRIPT.Find(scriptID);
                if (_script == null)
                {
                    err.IsError = true;
                    err.Message = "未找到主键值[" + scriptID.ToString() + "]的脚本流";
                    return null;
                }
                return _script;
            }
        }

        /// <summary>
        /// 根据脚本流实例ID获取脚本实例
        /// </summary>
        /// <param name="scriptCaseID"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static EM_SCRIPT_CASE GetScriptCase(long? scriptCaseID, ref ErrorInfo err)
        {
            //验证scriptID是否为空
            if (scriptCaseID == null)
            {
                err.IsError = true;
                err.Message = "传入的scriptCaseID值不能为空";
                return null;
            }
            using (DBEntities db = new DBEntities())
            {
                EM_SCRIPT_CASE _scriptCase = db.EM_SCRIPT_CASE.Find(scriptCaseID);
                if (_scriptCase == null)
                {
                    err.IsError = true;
                    err.Message = "未找到主键值[" + scriptCaseID.ToString() + "]的脚本流实例";
                    return null;
                }
                return _scriptCase;
            }
        }

        /// <summary>
        /// 根据脚本流ID获取其正在运行中的脚本流实例
        /// </summary>
        /// <param name="scriptID">脚本流ID</param>
        /// <param name="db">DBEntities</param>
        /// <returns></returns>
        public static EM_SCRIPT_CASE GetEffectScriptCase(long? scriptID, DBEntities db)
        {
            //是否有处于等待或执行中的脚本流实例
            EM_SCRIPT_CASE _scriptCase;
            if (scriptID == null)
            {
                _scriptCase = db.EM_SCRIPT_CASE.FirstOrDefault(p => p.RUN_STATUS.Value == (short)PubEnum.RunStatus.Excute
                    || p.RUN_STATUS.Value == (short)PubEnum.RunStatus.Wait);
            }
            else
            {
                _scriptCase = db.EM_SCRIPT_CASE.FirstOrDefault(p => p.SCRIPT_ID == scriptID && (p.RUN_STATUS.Value == (short)PubEnum.RunStatus.Excute
                   || p.RUN_STATUS.Value == (short)PubEnum.RunStatus.Wait));
            }

            return _scriptCase;
        }

        /// <summary>
        /// 根据脚本流ID获取其正在运行中的脚本流实例
        /// </summary>
        /// <param name="scriptID">脚本流ID</param>
        /// <param name="db">DBEntities</param>
        /// <returns></returns>
        public static EM_SCRIPT_CASE GetEffectScriptCase(long? scriptID)
        {
            using (DBEntities db = new DBEntities())
            {
                //是否有处于等待或执行中的脚本流实例
                EM_SCRIPT_CASE _scriptCase;
                if (scriptID == null)
                {
                    _scriptCase = db.EM_SCRIPT_CASE.FirstOrDefault(p => p.RUN_STATUS.Value == (short)PubEnum.RunStatus.Excute
                        || p.RUN_STATUS.Value == (short)PubEnum.RunStatus.Wait);
                }
                else
                {
                    _scriptCase = db.EM_SCRIPT_CASE.FirstOrDefault(p => p.SCRIPT_ID == scriptID && (p.RUN_STATUS.Value == (short)PubEnum.RunStatus.Excute
                       || p.RUN_STATUS.Value == (short)PubEnum.RunStatus.Wait));
                }

                return _scriptCase;
            }
        }
        
        /// <summary>
        /// 获取全部脚本列表
        /// </summary>
        /// <returns></returns>
        public static IList<EM_SCRIPT> GetAllScriptList()
        {
            using (DBEntities db = new DBEntities())
            {
                return db.EM_SCRIPT.ToList();
            }
        }

        /// <summary>
        /// 根据脚本流实例ID获取当前节点实例
        /// </summary>
        /// <param name="scriptCaseID"></param>
        /// <returns></returns>
        public static IList<EM_SCRIPT_NODE_CASE> GetAllNodeCaseByScriptCaseID(long? scriptCaseID)
        {
            if (scriptCaseID == null)
            {
                return null;
            }
            using (DBEntities db = new DBEntities())
            {
                return db.EM_SCRIPT_NODE_CASE.Where(p => p.SCRIPT_CASE_ID == scriptCaseID).ToList();
            }
        }

        /// <summary>
        /// 根据节点实例ID获取实例
        /// </summary>
        /// <param name="nodeCaseID">节点实例</param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static EM_SCRIPT_NODE_CASE GetScriptNodeCase(long? nodeCaseID,ref ErrorInfo err)
        {
            if (nodeCaseID == null)
            {
                err.IsError = true;
                err.Message = "传入的nodeCaseID不能为空";
                return null;
            }
            using (DBEntities db = new DBEntities())
            {
                var nodeCase= db.EM_SCRIPT_NODE_CASE.Find(nodeCaseID);
                if (nodeCase == null)
                {
                    err.IsError = true;
                    err.Message = "未找到编号为【"+nodeCaseID+"】的脚本实例";
                    return null;
                }
                return nodeCase;
            }
        }
        /// <summary>
        /// 获取【等待】的脚本节点实例集合
        /// </summary>
        /// <returns></returns>
        public static IList<EM_SCRIPT_NODE_CASE> GetWaitNodeCaseList()
        {
            using (DBEntities db = new DBEntities())
            {
                //var dd = db.EM_SCRIPT.ToList();
                if (db.EM_SCRIPT_NODE_CASE.Count() > 0)
                {
                    return db.EM_SCRIPT_NODE_CASE.Where(p => p.RUN_STATUS == (short)PubEnum.RunStatus.Wait).ToList();

                }
                return null;
            }
        }
        /// <summary>
        /// 根据数据库别名获取数据库对象
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static EM_DB_SERVER GetDBServer(string dbName,ref ErrorInfo  err)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                err.IsError = true;
                err.Message = "设置数据库名不能为空";
                return null;
            }
            else
            {
                using(DBEntities db=new DBEntities())
                {
                    return db.EM_DB_SERVER.FirstOrDefault(p => p.BYNAME == dbName);
                }
            }
        }

        /// <summary>
        /// 根据数据库ID获取数据库对象
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static EM_DB_SERVER GetDBServerByID(long? dbid, ref ErrorInfo err)
        {
            if (dbid==null)
            {
                err.IsError = true;
                err.Message = "设置数据库编号不能为空";
                return null;
            }
            else
            {
                using (DBEntities db = new DBEntities())
                {
                    return db.EM_DB_SERVER.Find(dbid);
                }
            }
        }
        #endregion

        #region 解析脚本内容

        /// <summary>
        /// 建表或命令段差异处理
        /// 建表时：以 @{CURR_TB} 来替代当前所建表
        /// </summary>
        /// <param name="nodeCase"></param>
        /// <returns></returns>
        public static string AnalyseCode(EM_SCRIPT_NODE_CASE nodeCase)
        {
            string retStr = "";
            string code = "";
            string tbName = "";
            if (nodeCase == null) return "";

            //如果为建表脚本
            if (nodeCase.SCRIPT_MODEL == (short)PubEnum.ScriptModel.CreateTb)
            {
                tbName = nodeCase.E_TABLE_NAME.ToUpper();
                if (nodeCase.TABLE_TYPE == (short)PubEnum.TableType.Private)
                {
                    tbName = nodeCase.E_TABLE_NAME.ToUpper() + "_" + nodeCase.TABLE_SUFFIX;
                }

                code = nodeCase.CONTENT.ToUpper().Replace("@{CURR_TB}", tbName);
                //加入验证表是否存在
                retStr = string.Format(@"if(is_table_exists(""{0}""))
                        drop_table(""{0}"");", tbName);
                retStr += string.Format(@"execute(""{0}"");", code);
            }
            else
            {
                return nodeCase.CONTENT;
            }

            return retStr;
        }

        /// <summary>
        /// 替换当前脚本流实例涉及的节点表
        /// </summary>
        /// <param name="nodeCase"></param>
        /// <param name="content"></param>
        public static string ReplaceTableNode(EM_SCRIPT_NODE_CASE nodeCase,string content)
        {
            //获取当前实例下的节点实例
            IList<EM_SCRIPT_NODE_CASE> nodeCaseList = GetAllNodeCaseByScriptCaseID(nodeCase.SCRIPT_CASE_ID);
            if (nodeCaseList != null && nodeCaseList.Count > 0)
            {
                for (int i = 0; i < nodeCaseList.Count; i++)
                {
                    var ncase = nodeCaseList[i];
                    if (ncase.SCRIPT_MODEL == (short)PubEnum.ScriptModel.CreateTb)
                    {
                        string tbName = ncase.E_TABLE_NAME;
                        if (ncase.TABLE_TYPE ==(short) PubEnum.TableType.Private)
                        {
                            tbName = ncase.E_TABLE_NAME + "_" + ncase.TABLE_SUFFIX;
                        }
                        //替换占位符表
                        content = content.Replace("@{" + ncase.E_TABLE_NAME + "}", tbName);
                    }
                }
            }
            return content;
        }

        #endregion

        #region 手工触发任务处理

        /// <summary>
        /// 返回待办的手工列表
        /// </summary>
        /// <returns></returns>
        public static IList<EM_HAND_RECORD> GetAllHandList()
        {
            using (DBEntities db = new DBEntities())
            {
                return db.EM_HAND_RECORD.Where(p => p.IS_CANCEL == (short)PubEnum.IsCancel.NoCancel).ToList();
            }
        }
        /// <summary>
        /// 作废手工记录
        /// </summary>
        /// <param name="handID"></param>
        /// <param name="isc"></param>
        /// <param name="cancelR"></param>
        /// <param name="err"></param>
        public static void ModifyHandRecord(long? handID, PubEnum.IsCancel isc, string cancelR, ref ErrorInfo err)
        {
            if (handID == null)
            {
                err.IsError = true;
                err.Message = "传入的handID值不能为空";
                return;
            }
            using (DBEntities db = new DBEntities())
            {
                var hand = db.EM_HAND_RECORD.Find(handID);
                if (hand != null)
                {
                    hand.IS_CANCEL = (short)isc;
                    hand.CANCEL_REASON = cancelR;
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        err.IsError = true;
                        err.Message = e.Message;
                        err.Excep = e;
                    }
                }
                else
                {
                    err.IsError = true;
                    err.Message = "未找到编号【" + hand.ToString() + "】的记录";
                    return;
                }
            }
        }
        /// <summary>
        /// 启动手工记录
        /// </summary>
        /// <param name="handID"></param>
        /// <param name="objCaseID"></param>
        /// <param name="isc"></param>
        /// <param name="err"></param>
        /// <param name="cancelR"></param>
        public static void ModifyHandRecord(long? handID, long? objCaseID, PubEnum.IsCancel isc, ref ErrorInfo err, string cancelR = "")
        {
            if (handID == null || objCaseID == null)
            {
                err.IsError = true;
                err.Message = "传入的handID、objCaseID值不能为空";
                return;
            }
            using (DBEntities db = new DBEntities())
            {
                var hand = db.EM_HAND_RECORD.Find(handID);
                if (hand != null)
                {
                    hand.IS_CANCEL = (short)isc;
                    hand.CANCEL_REASON = cancelR;
                    hand.START_TIME = DateTime.Now;
                    hand.OBJECT_CASE_ID = objCaseID;
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        err.IsError = true;
                        err.Message = e.Message;
                        err.Excep = e;
                    }
                }
                else
                {
                    err.IsError = true;
                    err.Message = "未找到编号【" + hand.ToString() + "】的记录";
                    return;
                }
            }
        }
        #endregion

    }
}
