using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Meiam.System.Common.Utilities;
using Meiam.System.Hostd.Authorization;
using Meiam.System.Hostd.Extensions;
using Meiam.System.Interfaces;
using Meiam.System.Model;
using Meiam.System.Model.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Meiam.System.Hostd.Controllers.System
{
    /// <summary>
    /// 用户管理
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UsersController : BaseController
    {
        /// <summary>
        /// 日志管理接口
        /// </summary>
        private readonly ILogger<UsersController> _logger;
        /// <summary>
        /// 会话管理接口
        /// </summary>
        private readonly ITokenManager _tokenManager;

        /// <summary>
        /// 用户服务接口
        /// </summary>
        private readonly ISysUsersService _usersService;

        /// <summary>
        /// 用户权限接口
        /// </summary>
        private readonly ISysUserRelationService _relationService;

        public UsersController(ILogger<UsersController> logger, ITokenManager tokenManager, ISysUsersService usersService, ISysUserRelationService relationService)
        {
            _logger = logger;
            _tokenManager = tokenManager;
            _usersService = usersService;
            _relationService = relationService;
        }


        /// <summary>
        /// 查询用户列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_USERS_VIEW")]
        public async Task<IActionResult> Query([FromBody] UsersQueryDto parm)
        {
            //开始拼装查询条件
            var predicate = Expressionable.Create<Sys_Users>();

            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.QueryText), m => m.UserID.Contains(parm.QueryText)  || m.UserName.Contains(parm.QueryText));

            var response = await _usersService.GetPagesAsync(predicate.ToExpression(), parm);

            return toResponse(response);
        }


        /// <summary>
        /// 查询用户
        /// </summary>
        /// <param name="id">编码</param>
        /// <returns></returns>
        [HttpGet]
        [Authorization(Power = "PRIV_USERS_VIEW")]
        public async Task<IActionResult> Get(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                return toResponse(await _usersService.GetIdAsync(id));
            }
            return toResponse(await _usersService.GetAllAsync());
        }


        /// <summary>
        /// 添加用户
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_USERS_CREATE")]
        public async Task<IActionResult> Create([FromBody] UsersCreateDto parm)
        {
            //判断用户是否已经存在
            if (await _usersService.AnyAsync(m => m.UserID == parm.UserID))
            {
                return toResponse(StatusCodeType.Error, $"添加 {parm.UserID} 失败，该用户已存在，不能重复！");
            }

            //从 Dto 映射到 实体
            var userInfo = parm.Adapt<Sys_Users>().ToCreate(await _tokenManager.GetSessionInfoAsync());

            userInfo.Password = PasswordUtil.CreateDbPassword(userInfo.UserID, userInfo.Password.Trim());

            return toResponse(await _usersService.AddAsync(userInfo));
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_USERS_UPDATE")]
        public async Task<IActionResult> Update([FromBody] UsersUpdateDto parm)
        {
            var userSession = await _tokenManager.GetSessionInfoAsync();

            #region 更新用户信息
            #endregion

            var response =await _usersService.UpdateAsync(m => m.UserID == parm.UserID, m =>  new Sys_Users
            {
                UserID = parm.UserID,
                UserName = parm.UserName,
                NickName = parm.NickName,
                Email = parm.Email,
                Sex = parm.Sex,
                AvatarUrl = parm.AvatarUrl,
                QQ = parm.QQ,
                Phone = parm.Phone,
                ProvinceID = parm.ProvinceID,
                Province = parm.Province,
                CityID = parm.CityID,
                City = parm.City,
                CountyID = parm.CountyID,
                County = parm.County,
                Address = parm.Address,
                Remark = parm.Remark,
                IdentityCard = parm.IdentityCard,
                Birthday = parm.Birthday,
                Enabled = parm.Enabled,
                OneSession = parm.OneSession,
                UpdateID = userSession.UserID,
                UpdateName = userSession.UserName,
                UpdateTime = DateTime.Now
            });

            #region 更新登录会话记录

            await _tokenManager.RefreshSessionAsync(parm.UserID);

            #endregion


            return toResponse(response);
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_USERS_DELETE")]
        public async Task<IActionResult> Delete([FromBody] UsersDeleteDto parm)
        {
            if (parm.UserIds.Count <=0)
            {
                return toResponse(StatusCodeType.Error, "删除角色 Id 不能为空");
            }

            // 删除权限记录
           await _relationService.DeleteAsync(m => parm.UserIds.Contains(m.UserID));
            // 删除用户
            var response =await _usersService.DeleteAsync(m => parm.UserIds.Contains(m.UserID));

            // 删除登录会话记录
            foreach (var userId in parm.UserIds)
            {
                await _tokenManager.RemoveAllSessionAsync(userId);
            }

            return toResponse(response);
        }


        /// <summary>
        /// 批量禁用启用
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_USERS_UPDATE")]
        public async Task<IActionResult> Enable([FromBody] UsersEnableDto parm)
        {
            if (parm.UserIds.Count <= 0)
            {
                return toResponse(StatusCodeType.Error, "更新角色 Id 不能为空");
            }

            // 更新用户禁用
            if (!parm.Status)
            {
                // 删除登录会话记录
                foreach (var userId in parm.UserIds)
                {
                    await _tokenManager.RemoveAllSessionAsync(userId);
                }

                return toResponse(await _usersService.UpdateAsync(m => parm.UserIds.Contains(m.UserID), m => new Sys_Users { Enabled = false }));
            }

            // 更新用户启用
            return toResponse(await    _usersService.UpdateAsync(m => parm.UserIds.Contains(m.UserID), m => new Sys_Users { Enabled = true }));
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_USERS_RESETPASSWD")]
        public async Task<IActionResult> ResetPassword([FromBody] UsersResetPasswordDto parm)
        {
            // 更新用户密码
            var response = _usersService.Update(m => m.UserID == parm.UserID, m => new Sys_Users()
            {
                Password = PasswordUtil.CreateDbPassword(parm.UserID, parm.ConfirmPassword.Trim())
            });

            // 删除登录会话记录
            await _tokenManager.RemoveAllSessionAsync(parm.UserID);

            return toResponse(response);
        }

    }
}
