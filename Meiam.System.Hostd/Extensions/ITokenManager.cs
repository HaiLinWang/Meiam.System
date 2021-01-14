using Meiam.System.Core;

using Meiam.System.Hostd.Common;
using Meiam.System.Interfaces;
using Meiam.System.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Meiam.System.Hostd.Extensions
{
    public interface ITokenManager
    {

        #region 同步方法
        #region Session 操作
        /// <summary>
        /// 创建 Session
        /// </summary>
        public string CreateSession(Sys_Users userInfo, SourceType source, int hours);


        /// <summary>
        /// 更新Session
        /// </summary>
        /// <param name="userSession">用户Session</param>
        public void UpdateSession(string userSession);

        /// <summary>
        /// 刷新用户所有Session信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public void RefreshSession(string userId);
        /// <summary>
        /// 清除指定 Session
        /// </summary>
        public void RemoveSession(string userSession);

        /// <summary>
        /// 清除用户所有 Session
        /// </summary>
        /// <param name="userId"></param>
        public void RemoveAllSession(string userId);

        #endregion

        #region Session 获取信息

        /// <summary>
        /// 获取Session
        /// </summary>
        /// <returns></returns>
        public string GetSys_Token();

        /// <summary>
        /// 当前登录用户信息
        /// </summary>
        /// <returns></returns>
        public UserSessionVM GetSessionInfo();

        /// <summary>
        /// 判断用户是否登录
        /// </summary>
        /// <returns></returns>
        public bool IsAuthenticated();

        /// <summary>
        /// 获取 Session 内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetSessionItem<T>(string key);


        /// <summary>
        /// 获取 Session 内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetSessionItem<T>(string session, string key);


        #endregion
        #endregion
        #region 异步方法
        #region Session 操作
        /// <summary>
        /// 创建 Session
        /// </summary>
         public Task<string>  CreateSessionAsync(Sys_Users userInfo, SourceType source, int hours);


        /// <summary>
        /// 更新Session
        /// </summary>
        /// <param name="userSession">用户Session</param>
        public Task UpdateSessionAsync(string userSession);

        /// <summary>
        /// 刷新用户所有Session信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Task RefreshSessionAsync(string userId);
        /// <summary>
        /// 清除指定 Session
        /// </summary>
        public Task RemoveSessionAsync(string userSession);

        /// <summary>
        /// 清除用户所有 Session
        /// </summary>
        /// <param name="userId"></param>
        public Task RemoveAllSessionAsync(string userId);

        #endregion

        #region Session 获取信息

        ///// <summary>
        ///// 获取Session
        ///// </summary>
        ///// <returns></returns>
        // public Task<string>  GetSys_TokenAsync();

        /// <summary>
        /// 当前登录用户信息
        /// </summary>
        /// <returns></returns>
        public Task<UserSessionVM>  GetSessionInfoAsync();

        /// <summary>
        /// 判断用户是否登录
        /// </summary>
        /// <returns></returns>
        public Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// 获取 Session 内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<T> GetSessionItemAsync<T>(string key);


        /// <summary>
        /// 获取 Session 内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<T> GetSessionItemAsync<T>(string session, string key);


        #endregion
        #endregion

    }
}
