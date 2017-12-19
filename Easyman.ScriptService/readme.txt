安装步骤：
0、环境依赖
	需要.net Framework 4.5.2及以上版本
1、修改Easyman.ScriptService.exe.config中的如下配置项：
	1）ServiceName，在部署后不可再修改
	2）DataBaseType，修改后需要重新启动服务
	3）ConnString，修改后需要重新启动服务
	其它配置一般情况下使用默认值即可，根据具体情况针对性修改。
2、以管理员权限运行“安装服务.bat”，即可安装服务。
3、以管理员权限运行“启动服务.bat”，即可运行服务。