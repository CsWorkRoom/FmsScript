using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.Service.Domain
{
    public class PubEnum
    {
        /// <summary>
        /// 脚本流/脚本节点的实例状态(等待、执行中、停止)
        /// 受设计限制，不支持任务暂停
        /// </summary>
        public enum RunStatus
        {
            [Description("【等待】已添加实例，还未被调度服务执行")]
            Wait = 1,
            [Description("【执行中】实例正被调度服务执行中")]
            Excute = 2,
            [Description("【停止】实例执行完毕")]
            Stop = 0
        }

        /// <summary>
        /// 脚本流/脚本节点的执行结果标识(成功、失败)
        /// </summary>
        public enum ReturnCode
        {
            [Description("【成功】实例执行成功")]
            Success = 1,
            [Description("【失败】实例执行失败")]
            Fail = 0
        }

        /// <summary>
        /// 节点脚本模式
        /// </summary>
        public enum ScriptModel
        {
            [Description("【建表】任务节点=建表语句")]
            CreateTb = 1,
            [Description("【命令段】任务节点=命令语句段")]
            OrderCode = 2
        }

        /// <summary>
        /// 是否有失败节点
        /// </summary>
        public enum IsHaveFail
        {
            [Description("有失败节点")]
            HaveFail = 1,
            [Description("无失败节点")]
            NoFail = 0
        }

        /// <summary>
        /// 表类型
        /// </summary>
        public enum TableType
        {
            [Description("【公有表】表名不变")]
            Public = 1,
            [Description("【私有表】表名在实例中会加入时间戳")]
            Private = 0
        }

        /// <summary>
        /// 建表模式
        /// </summary>
        public enum TableModel
        {
            [Description("【复制】")]
            CopyTb = 2,
            [Description("【新建】")]
            NewTb = 1
        }

        /// <summary>
        /// 脚本流状态（关闭、开启）
        /// </summary>
        public enum ScriptStatus
        {
            [Description("【开启】脚本流任务")]
            Open = 1,
            [Description("【关闭】脚本流任务")]
            Close = 0
        }

        /// <summary>
        /// 记录删除状态（未删除、已删除）
        /// </summary>
        public enum IsDelete
        {
            [Description("【未删除】记录")]
            Yes =1,
            [Description("【已经删除】记录")]
            No=0
        }

        /// <summary>
        /// 是否处理(手工触发)
        /// </summary>
        public enum IsCancel
        {
            [Description("【已处理】")]
            Cancel = 1,
            [Description("【有效】")]
            NoCancel = 0
        }

        /// <summary>
        /// 脚本节点维护模式
        /// </summary>
        public enum OpStatus
        {
            [Description("【新增】脚本节点")]
            Insert = 1,
            [Description("【修改】脚本节点内容被修改")]
            Modify = 2,
            [Description("【删除】脚本节点被删除")]
            Delete = 0
        }

        /// <summary>
        /// 启动模式
        /// </summary>
        public enum StatusModel
        {
            [Description("脚本流实例【手工】被添加")]
            Hand = 1,
            [Description("脚本流实例【自动】被添加")]
            Anto = 0
        }

        /// <summary>
        /// 函数编译状态
        /// </summary>
        public enum CompileStatus
        {
            [Description("编译【通过】")]
            Success = 1,
            [Description("编译【失败】")]
            Fail = 0
        }
        /// <summary>
        /// 脚本流/脚本流节点
        /// </summary>
        public enum HandType
        {
            [Description("脚本流")]
            Script=1,
            [Description("脚本流节点")]
            ScriptNode = 2
        }
    }
}
