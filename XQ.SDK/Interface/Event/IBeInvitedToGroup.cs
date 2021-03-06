﻿using XQ.SDK.EventArgs;

namespace XQ.SDK.Interface
{
    /// <summary>
    ///     被邀请入群事件接口
    /// </summary>
    public interface IBeInvitedToGroup : IXqEvent
    {
        /// <summary>
        ///     处理被邀请入群事件
        /// </summary>
        void BeInvitedToGroupRequest(BeInvitedToGroupEventArgs e);
    }
}