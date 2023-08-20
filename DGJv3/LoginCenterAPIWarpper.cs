using BilibiliDM_PluginFramework;
using LoginCenter.API;
using System;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;


namespace DGJv3
{
    internal class LoginCenterAPIWarpper
    {
        private static LoginCenterAPIWarpper warpper = null;
        private static bool isLoginCenterChecked = false;

        internal static bool CheckLoginCenter()
        {
            if (isLoginCenterChecked)
            {
                return warpper != null;
            }

            try
            {
                warpper = new LoginCenterAPIWarpper();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                isLoginCenterChecked = true;
            }
        }

        internal static bool CheckAuth(DMPlugin plugin)
        {
            if (!CheckLoginCenter())
            {
                return false;
            }
            return warpper.checkAuthorization(plugin) == true;
        }

        internal static async Task<bool> DoAuth(DMPlugin plugin)
        {
            if (!CheckLoginCenter())
            {
                return false;
            }
            return await warpper.doAuthorization(plugin);
        }

        internal static string Send(int roomid, string msg, int color = 16777215, int mode = 1, int rnd = -1, int fontsize = 25)
        {
            if (!CheckLoginCenter())
            {
                return null;
            }
            return warpper.trySendMessage(roomid, msg, color, mode, rnd, fontsize).Result;
        }

        internal static async Task<string> Send_Async(int roomid, string msg, int color = 16777215, int mode = 1, int rnd = -1, int fontsize = 25)
        {
            if (!CheckLoginCenter())
            {
                return null;
            }
            return await SendDanmakuAsync(roomid, msg, LoginCenterAPI.getCookies());
            // return await warpper.trySendMessage(roomid, msg, color, mode, rnd, fontsize);
        }



        public LoginCenterAPIWarpper()
        {
            checkAuthorization();
        }
        public bool checkAuthorization()
        {
            return LoginCenterAPI.checkAuthorization();
        }

        public bool checkAuthorization(DMPlugin plugin)
        {
            return LoginCenterAPI.checkAuthorization(plugin) == LoginCenter.API.AuthorizationResult.Success;
        }

        public Task<string> trySendMessage(int roomid, string msg, int color = 16777215, int mode = 1,
            int rnd = -1, int fontsize = 25)
        {
            return LoginCenterAPI.trySendMessage(roomid, msg, color, mode, rnd, fontsize);
        }

        public static async Task<string> SendDanmakuAsync(int roomId, string danmaku, CookieContainer cookie, int color = 16777215)
        {
            //IDictionary<string, string> Headers = new Dictionary<string, string>
            //{
            //    { "Origin", "https://live.bilibili.com" },
            //    { "Referer", $"https://live.bilibili.com/{GetShortRoomId(roomId)}" }
            //};
            int UnixTimeStamp = (int)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
            while (true)
            {
                string csrf = cookie.GetCookies(new Uri("http://live.bilibili.com/")).OfType<Cookie>().FirstOrDefault(p => p.Name == "bili_jct")?.Value;
                IDictionary<string, object> Postdata = new Dictionary<string, object>
                {
                    { "color", color },
                    { "fontsize", 25 },
                    { "mode", 1 },
                    { "msg", WebUtility.UrlEncode(danmaku) },
                    { "rnd", UnixTimeStamp },
                    { "roomid", roomId },
                    { "csrf_token", csrf },
                    { "csrf", csrf }
                };
                try
                {
                    return await HttpPostAsync("https://api.live.bilibili.com/msg/send", Postdata, 15, cookie: cookie/*, headers: Headers*/); ;
                }
                catch 
                {
                    return null;
                }
            }
        }

        public static async Task<string> HttpPostAsync(string url, IDictionary<string, object> parameters = null, int timeout = 0, string userAgent = null, CookieContainer cookie = null, IDictionary<string, string> headers = null)
        {
            string formdata = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
            return await HttpPostAsync(url, formdata, timeout, userAgent, cookie, headers);
        }

        


        public static async Task<string> HttpPostAsync(string url, string formdata, int timeout = 0, string userAgent = null, CookieContainer cookie = null, IDictionary<string, string> headers = null)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Accept = "*/*";
            request.Method = "POST";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = userAgent ?? "SendDanmaku/DGJv3";
            if (timeout != 0) { request.Timeout = timeout * 1000; request.ReadWriteTimeout = timeout * 1000; }
            else request.ReadWriteTimeout = 10000;
            request.CookieContainer = cookie;
            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    if (key.ToLower() == "accept")
                        request.Accept = headers[key];
                    else if (key.ToLower() == "host")
                        request.Host = headers[key];
                    else if (key.ToLower() == "referer")
                        request.Referer = headers[key];
                    else if (key.ToLower() == "content-type")
                        request.ContentType = headers[key];
                    else
                        request.Headers.Add(key, headers[key]);
                }
            }
            if (!string.IsNullOrEmpty(formdata))
            {
                byte[] data = Encoding.UTF8.GetBytes(formdata);
                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
            using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                return await reader.ReadToEndAsync();
        }

        public async Task<bool> doAuthorization(DMPlugin plugin)
        {
            var result = await LoginCenterAPI.doAuthorization(plugin);
            return result == AuthorizationResult.Success;
        }
    }
}
