
using System;
//using Easyman.ScriptService.Script;
namespace Easyman.ScriptService.Script
{
    public class ScripRunner : Base
    {
        public bool Run()
        {
            
            try
            {
                //载入脚本内容
                //*************************第二个节点*************************
                //----------------------------------------------------------------------------
                log("导入当月主资费数据");
                //----------------------------------------------------------------------------
                //选中远程库
                setnowdb("site_cscdm06");
                //查询并导入数据
                int i = down_db_to_db(
                @"SELECT A.ID_NO,
	A.LOGIN_NO,
	A.PROD_PRCID,
		B.PROD_PRC_NAME,
	A.OP_TIME,
	A.OP_CODE,
	TO_CHAR(A.EFF_DATE,'YYYYMMDD') EFF_DATE,
	TO_CHAR(A.EXP_DATE,'YYYYMMDD') EXP_DATE,
		A.LOGIN_ACCEPT,
		A.REMARK,
	ROW_NUMBER() OVER(PARTITION BY A.ID_NO, A.PROD_PRCID ORDER BY A.OP_TIME DESC) RN
FROM   ZY_USER.ODS_PD_USERPRC_INFO_" + day(-2) + @" A LEFT JOIN
	DB2INST1.ODS_PD_PRC_DICT_" + day(-2) + @" B
ON     A.PROD_PRCID=B.PROD_PRCID
WHERE  A.PROD_MAIN_FLAG = '0'
AND    TO_CHAR(A.OP_TIME,'YYYYMM') ='" + month() + @"'
AND    A.OP_CODE = '1104' --产品变更
AND    TO_CHAR(A.EXP_DATE,'YYYYMMDD') >= '" + month() + @"01'
AND    SUBSTR(A.LOGIN_NO,1,2)<>'mb'",
                "JH002_TMP_DY",
                "资阳DB2-TEST",
                0,
                10000
                );
                //导入完毕
                log("共导入了" + i + "条数据到表： JH002_TMP_DY");
                return true;
            }
            catch (Exception err)
            {
                //脚本执行失败处理
                WriteErrorMessage(err.ToString(), 3);
                return false;
            }
        }
        //加载自定义函数

    }
}