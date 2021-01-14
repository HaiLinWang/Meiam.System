using Meiam.System.Hostd.Authorization;
using Meiam.System.Hostd.Extensions;
using Meiam.System.Interfaces;
using Meiam.System.Model;
using Meiam.System.Model.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Meiam.System.Hostd.Controllers.System
{
    /// <summary>
    /// 角色权限
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RolePowersController : BaseController
    {
        /// <summary>
        /// 日志管理接口
        /// </summary>
        private readonly ILogger<RolePowersController> _logger;

        /// <summary>
        /// 会话管理接口
        /// </summary>
        private readonly ITokenManager _tokenManager;

        /// <summary>
        /// 角色权限接口
        /// </summary>
        private readonly ISysRolePowerService _rolePowerService;

        /// <summary>
        /// 权限定义接口
        /// </summary>
        private readonly ISysPowerService _powerService;

        /// <summary>
        /// 用户角色接口
        /// </summary>
        private readonly ISysUserRoleService _userRoleService;


        public RolePowersController(ILogger<RolePowersController> logger, ITokenManager tokenManager, ISysRolePowerService rolePowerService, ISysPowerService powerService, ISysUserRoleService userRoleService)
        {
            _logger = logger;
            _tokenManager = tokenManager;
            _rolePowerService = rolePowerService;
            _powerService = powerService;
            _userRoleService = userRoleService;
        }

        /// <summary>
        /// 按分组汇总查询权限
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetPowersGroup()
        {
            var powers = (await _powerService.GetAllAsync()).OrderBy(m => m.CreateTime);

            var response = powers.GroupBy(m => m.Page).Select(m => new
            {
                Page = m.Key,
                Powers = powers.Where(p => p.Page == m.Key).OrderBy(m => m.CreateTime).Select(m => new
                {
                    m.ID,
                    m.Name,
                    m.Description,
                    Checked = false
                }),
            });

            return toResponse(response);
        }

        /// <summary>
        /// 查询角色权限
        /// </summary>
        /// <param name="roleId">角色id</param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetRolePowers(string roleId)
        {
            if (string.IsNullOrEmpty(roleId))
            {
                return toResponse(StatusCodeType.Error, "roleId 不能为空");
            }
            return toResponse((await _rolePowerService.GetWhereAsync(m => m.RoleUID == roleId)).Select(m => m.PowerUID));
        }


        /// <summary>
        /// 更新角色权限
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_ROLEPOWERS_UPDATE")]
        public async Task<IActionResult> UpdateRolePowers([FromBody] RolePowersUpdateDto parm)
        {
            if (string.IsNullOrEmpty(parm.RoleId))
            {
                return toResponse(StatusCodeType.Error, "roleId 不能为空");
            }

            //生成插入对象
            List<Sys_RolePower> rolePowers = new List<Sys_RolePower>();
            foreach (var power in parm.PowerIds)
            {
                rolePowers.Add(new Sys_RolePower
                {
                    ID = GetGUID,
                    PowerUID = power,
                    RoleUID = parm.RoleId
                });
            }

            //执行更新过程
            try
            {
                _rolePowerService.BeginTran();
                // 先删除角色对应的权限
                await _rolePowerService.DeleteAsync(o => o.RoleUID == parm.RoleId);
                // 再插入传递进来的权限
                await _rolePowerService.AddAsync(rolePowers);
                _rolePowerService.CommitTran();


                //更新登录会话记录
                var userIds = (await _userRoleService.GetWhereAsync(m => m.RoleID == parm.RoleId)).Select(m => m.UserID);

                foreach (var userId in userIds)
                {
                    await _tokenManager.RefreshSessionAsync(userId);
                }

                return toResponse(StatusCodeType.Success);

            }
            catch (Exception ex)
            {
                _rolePowerService.RollbackTran();
                throw ex;
            }
        }
    }
}
