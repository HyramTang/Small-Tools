<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="RemeberPassword._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>

    <!--网站登录跳转 -->
    <script type="text/javascript"><!--
    function jmpLogin() {
        //跳转
        document.jumpFrm.action = "";
        var userName = document.getElementById("userName").value;
        var userPassword = document.getElementById("userPassword").value;
        if (userName == "") {
            alert("用户名不能为空,请填写！");
            return;
        } else if (userPassword == "") {
            alert("密码不能为空,请输入！");
            return;
        }

        var jmpLocation = document.getElementById("jmpLocation").value;
        //document.all.memberid.checked==true
        if (document.all.rmbPassword.checked) {
            //alert("begin to rmb password!!!");
            setCookie("userName", userName, 24, "/");
            setCookie("userPassword", userPassword, 24, "/");
            //alert("OK!COOKIE");
        }

        if (jmpLocation == 1) {
            document.jumpFrm.action = "http://10.41.7.41:7001/defaultroot/LogonAction.do";
            jumpFrm.submit();
        }
        if (jmpLocation == 2) {
            document.jumpFrm.action = "http://10.41.7.40:8123/defaultroot/LogonAction.do";
            jumpFrm.submit();
        }
        if (jmpLocation == 3) {
            // http://mail.hnsl.gov.cn/remote.php?LoginName=PMAIL_USER&Password=PMAIL_PASS
            document.getElementById("LoginName").value = document.getElementById("userName").value;
            document.getElementById("password").value = document.getElementById("userPassword").value;
            document.jumpFrm.action = "http://mail.hnsl.gov.cn/remote.php";
            jumpFrm.submit();

        }
    }


    //获取cookie信息
    function getRememberInfo() {
        // alert("---获取cookie信息---");

        try {
            var userName = "";
            var userPassword = "";
            userName = getCookieValue("userName");
            userPassword = getCookieValue("userPassword");
            document.getElementById("userName").value = userName;
            document.getElementById("userPassword").value = userPassword;
        } catch (err) {
            alert("NO RMB PASSWORD!");
        }
    }



    //新建cookie。
    //hours为空字符串时,cookie的生存期至浏览器会话结束。hours为数字0时,建立的是一个失效的cookie,这个cookie会覆盖已经建立过的同名、同path的cookie（如果这个cookie存在）。
    function setCookie(name, value, hours, path) {
        var name = escape(name);
        var value = escape(value);
        var expires = new Date();
        expires.setTime(expires.getTime() + hours * 3600000);
        path = path == "" ? "" : ";path=" + path;
        _expires = (typeof hours) == "string" ? "" : ";expires=" + expires.toUTCString();
        document.cookie = name + "=" + value + _expires + path;
    }
    //获取cookie值
    function getCookieValue(name) {
        var name = escape(name);
        //读cookie属性，这将返回文档的所有cookie
        var allcookies = document.cookie;
        //查找名为name的cookie的开始位置
        name += "=";
        var pos = allcookies.indexOf(name);
        //如果找到了具有该名字的cookie，那么提取并使用它的值
        if (pos != -1) { //如果pos值为-1则说明搜索"version="失败
            var start = pos + name.length; //cookie值开始的位置
            var end = allcookies.indexOf(";", start); //从cookie值开始的位置起搜索第一个";"的位置,即cookie值结尾的位置
            if (end == -1) end = allcookies.length; //如果end值为-1说明cookie列表里只有一个cookie
            var value = allcookies.substring(start, end); //提取cookie的值
            return unescape(value); //对它解码
        }
        else return ""; //搜索失败，返回空字符串
    }
    //删除cookie
    function deleteCookie(name, path) {
        var name = escape(name);
        var expires = new Date(0);
        path = path == "" ? "" : ";path=" + path;
        document.cookie = name + "=" + ";expires=" + expires.toUTCString() + path;
    }
    </script>
</head>
<body>
    <!--网站登录跳转 -->
    <form name="jumpFrm" id="jumpFrm" action="" method="POST" target="_blank">
        <table width="100%" border="0" cellspacing="0" cellpadding="0">
            <tr>
                <td width="69" height="26" align="right">用户名：</td>
                <td>
                    <input type="text" name="userName" id="userName" class="text_input2" style="width: 126px; height: 15px;" /></td>
            </tr>
            <tr>
                <td height="26" align="right">密码：</td>
                <input type="hidden" name="domainAccount" value="whir">
                <td>
                    <input type="password" name="userPassword" id="userPassword" class="text_input2" style="width: 50px; height: 15px;" />

                    记住密码：
                    <input type="checkbox" name="rmbPassword" id="rmbPassword" />
                </td>
            </tr>
            <tr>
                <td height="26" align="right">选择系统：</td>
                <td>

                    <select name="jmpLocation" style="width: 126px; height: 20px;">
                        <option value="1" selected="selected">综合办公平台</option>
                        <option value="2">内网门户后台</option>
                        <option value="3">邮件系统</option>
                    </select>

                </td>
            </tr>
            <tr>
                <td height="28" align="right" valign="bottom"></td>
                <td valign="bottom">&nbsp;
                    <input type="button" name="Submit22" value="登 录" class="btn_2" onclick="jmpLogin();" />
                    &nbsp;
                    <input type="reset" name="Submit2" value="重 置" class="btn_2" />
                </td>
            </tr>
        </table>

        <input type="hidden" name="Password" id="password" value="" />
        <input type="hidden" name="LoginName" id="LoginName" />

    </form>
</body>
</html>
