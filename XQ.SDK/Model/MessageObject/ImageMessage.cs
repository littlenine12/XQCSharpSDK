﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using XQ.SDK.Enum;
using XQ.SDK.Interface;
using XQ.SDK.Model.Json;
using XQ.SDK.XQ;

namespace XQ.SDK.Model.MessageObject
{
    /// <summary>
    ///     图片消息
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    public class ImageMessage : IToSendString
    {
        private static readonly Lazy<Regex>
            PrivateImage =
                new Lazy<Regex>(() =>
                    new Regex("{[0-9]{5,15}[-][0-9]{5,15}[-]([0-9A-Fa-f]{32})}.(jpg|png|gif|bmp|jpeg).*",
                        RegexOptions.IgnoreCase | RegexOptions.ECMAScript)),
            GroupImage = new Lazy<Regex>(() => new Regex(
                "{([0-9A-Fa-f]{8})[-]([0-9A-Fa-f]{4})[-]([0-9A-Fa-f]{4})[-]([0-9A-Fa-f]{4})[-]([0-9A-Fa-f]{12})}.(jpg|png|gif|bmp|jpeg).*",
                RegexOptions.IgnoreCase | RegexOptions.ECMAScript));

        private readonly string _sendString;
        private readonly ImageMessageType _type;

        private readonly XqApi _xqApi;

        public readonly string RobotQq;


        private ImageMessage(XqApi xqApi, string robotqq, string toSendString, ImageMessageType type)
        {
            _sendString = toSendString;
            _type = type;
            _xqApi = xqApi;
            RobotQq = robotqq;
        }

        private string Rawmsg => _sendString.Substring(5, _sendString.Length - 6);

        public string ToSendString()
        {
            return _sendString;
        }

        /// <summary>
        ///     将群聊图片消息进行转换，使其可在私聊中发送
        ///     代码逻辑来自w4123/CQXQ
        /// </summary>
        public PlainMessage GroupToPrivate()
        {
            var rslt = GroupImage.Value.Match(_sendString);
            if (!rslt.Success) return "";
            var m = rslt.Groups;
            var guid = m[1].Value;
            return
                $"[pic={{{guid.Substring(0, 8)}-{guid.Substring(8, 4)}-{guid.Substring(12, 4)}-{guid.Substring(16, 4)}-{guid.Substring(20)}}}.{m[2].Value}]";
        }

        /// <summary>
        ///     将私聊图片消息进行转换，使其可在群聊中发送
        ///     代码逻辑来自w4123/CQXQ
        /// </summary>
        /// <param name="robot">botQQ</param>
        public PlainMessage PrivateToGroup(string robot)
        {
            var rslt = PrivateImage.Value.Match(_sendString);
            if (!rslt.Success) return "";
            var m = rslt.Groups;
            return
                $"[pic={{{robot}-1234567879-{m[1].Value}{m[2].Value}{m[3].Value}{m[4].Value}{m[5].Value}}}.{m[6].Value}]";
        }

        /// <summary>
        ///     从文件绝对路径构造ImageMessage
        ///     代码逻辑来自w4123/CQXQ
        /// </summary>
        /// <param name="xqApi">Api</param>
        /// <param name="robot">botQQ</param>
        /// <param name="filename">文件绝对路径</param>
        /// <returns></returns>
        public static ImageMessage FromFileName(XqApi xqApi, string robot, string filename)
        {
            return new ImageMessage(xqApi, robot, $"[pic={filename}]", ImageMessageType.FromFile);
        }

        /// <summary>
        ///     上传图片
        /// </summary>
        /// <param name="xqApi">Api</param>
        /// <param name="robot">botQQ</param>
        /// <param name="imageType">消息类型</param>
        /// <param name="groupOrQq">发送对象的群号或QQ</param>
        /// <param name="file">要上传的图片</param>
        public static ImageMessage UpLoadPic(XqApi xqApi, string robot, MessageType imageType, string groupOrQq,
            byte[] file)
        {
            return xqApi.UpLoadPic(robot, imageType, groupOrQq, file);
        }

        /// <summary>
        ///     从网络链接构造ImageMessage
        /// </summary>
        /// <param name="xqApi"></param>
        /// <param name="robotqq"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static ImageMessage FromUrl(XqApi xqApi, string robotqq, string url)
        {
            return new ImageMessage(xqApi, robotqq, $"[pic={url}]", ImageMessageType.FromWebUrl);
        }

        /// <summary>
        ///     转换为秀图
        /// </summary>
        public PlainMessage ToShowPic(ShowPicType type)
        {
            return new PlainMessage(_sendString.Replace("[pic=", "[ShowPic=") + $",type={(int) type}]");
        }

        /// <summary>
        ///     转换为闪照
        /// </summary>
        public PlainMessage ToFlashPic()
        {
            return new PlainMessage(_sendString.Replace("[pic=", "[FlashPic="));
        }

        /// <summary>
        ///     获取消息中所有图片消息
        ///     若转发可能需要进行格式转换
        /// </summary>
        /// <param name="robotqq"></param>
        /// <param name="message"></param>
        /// <param name="xqApi"></param>
        public static List<ImageMessage> GetFromMessage(XqApi xqApi, string robotqq, string message)
        {
            try
            {
                return !message.Contains("[pic=")
                    ? null
                    : (from Match item in Regex.Matches(message,
                            @"([pic])(.)+?(?=\])")
                        select $"[{item.Value}]").Select(i =>
                        new ImageMessage(xqApi, robotqq, i, ImageMessageType.FromMessage))
                    .ToList();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     获取图片Ocr
        ///     只能获取消息中得到的图片
        ///     从文件路径或url构造的ImageMessage无效
        /// </summary>
        /// <returns>图片Ocr后的信息</returns>
        public OcrItemCollection GetImageOcrResult()
        {
            return _xqApi.GetImageOcrResult(this);
        }

        /// <summary>
        ///     从文件路径或url构造的ImageMessage转byte[]
        ///     从消息获得的ImageMessage无法转换,但可先下载再转换
        /// </summary>
        public byte[] ToBytes()
        {
            Stream fs = null;
            BinaryReader binaryWriter = null;
            try
            {
                fs = _type switch
                {
                    ImageMessageType.FromFile => new FileStream(Rawmsg, FileMode.Open, FileAccess.Read),
                    ImageMessageType.FromWebUrl => GetFromWeb(Rawmsg),
                    ImageMessageType.FromMessage => throw new ArgumentOutOfRangeException(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                binaryWriter = new BinaryReader(fs);
                return binaryWriter.ReadBytes((int) fs.Length);
            }
            finally
            {
                binaryWriter?.Close();
                fs?.Close();
                binaryWriter?.Dispose();
                fs?.Dispose();
            }
        }

        private static Stream GetFromWeb(string url)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };
            return httpClient.SendAsync(request).Result.Content.ReadAsStreamAsync().Result;
        }

        /// <summary>
        ///     获取图片下载链接
        ///     只能获取消息中得到的图片下载链接
        ///     从文件路径或url构造ImageMessage的无效
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="group">群号，在图片为群聊图片时填写</param>
        public string GetImageDownloadLink(string group, MessageType type)
        {
            return _xqApi.GetPicLink(type, group, this);
        }

        /// <summary>
        ///     下载消息中的图片到本地
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="group">群号，在图片为群聊图片时填写</param>
        /// <param name="filename">相对先驱.exe的相对路径</param>
        public ImageMessage Download(string group, MessageType type, string filename)
        {
            if (_type != ImageMessageType.FromMessage) throw new ArgumentOutOfRangeException();

            Image i = null;
            try
            {
                i = Image.FromStream(GetFromWeb(GetImageDownloadLink(group, type)));
                i.Save(filename);
                return FromFileName(_xqApi, RobotQq, $@"{AppContext.BaseDirectory}\{filename}");
            }
            finally
            {
                i?.Dispose();
            }
        }

        public override string ToString()
        {
            return ToSendString();
        }
    }
}