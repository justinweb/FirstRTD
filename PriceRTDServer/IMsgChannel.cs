using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KGI.TW.Der.ST.MsgChannel
{
    /// <summary>
    /// 定義以Message型式傳送訊息的介面
    /// </summary>
    /// <remarks>
    /// 因為Tibco在Send時使用的Message的AddField()，不支援object型別，所以Send()函式只能用Dictionary<string,string>了。
    /// </remarks>
    public interface IMsgChannel
    {
        /// <summary>
        /// 接收訊息管道的狀態
        /// </summary>
        /// <remarks>
        /// 參數：bool 狀態
        /// </remarks>       
        event Action<bool> OnStatus;
        /// <summary>
        /// 接收訊息管道傳來的訊息
        /// </summary>
        /// <remarks>
        /// Arg1 : string, 訊息主題
        /// Arg2 : Dictionary<string,string>, 訊息資料
        /// </remarks>
        event Action<string,Dictionary<string, string>> OnMessage; // subject, data

        /// <summary>
        /// 連線，建立通訊管道
        /// </summary>
        void Connect();
        /// <summary>
        /// 斷線
        /// </summary>
        void DisConnect();
        /// <summary>
        /// 訂閱
        /// </summary>
        /// <param name="subject">要訂閱的資訊</param>
        void Subscribe(string subject);
        /// <summary>
        /// 取消訂閱
        /// </summary>
        /// <param name="subject">要取消訂閱的資訊</param>
        void UnSubscribe(string subject);        
        /// <summary>
        /// 傳送訊息
        /// </summary>
        /// <param name="subject">訊息主題</param>
        /// <param name="msgData">訊息內容</param>
        void Send(string subject, Dictionary<string, string> msgData);
    }
}
