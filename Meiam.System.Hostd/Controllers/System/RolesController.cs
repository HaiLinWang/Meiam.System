using Mapster;
using Meiam.System.Hostd.Authorization;
using Meiam.System.Hostd.Extensions;
using Meiam.System.Interfaces;
using Meiam.System.Model;
using Meiam.System.Model.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Meiam.System.Hostd.Controllers.System
{
    /// <summary>
    /// 角色定义
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RolesController : BaseController
    {
        /// <summary>
        /// 日志管理接口
        /// </summary>
        private readonly ILogger<RolesController> _logger;

        /// <summary>
        /// 会话管理接口
        /// </summary>
        private readonly ITokenManager _tokenManager;


        /// <summary>
        /// 角色定义接口
        /// </summary>
        private readonly ISysRoleService _roleService;

        /// <summary>
        /// 角色权限接口
        /// </summary>
        private readonly ISysRolePowerService _rolePowerService;

        /// <summary>
        /// 用户角色接口
        /// </summary>
        private readonly ISysUserRoleService _userRoleService;

        public RolesController(ITokenManager tokenManager, ISysRoleService roleService, ILogger<RolesController> logger,ISysRolePowerService rolePowerService ,ISysUserRoleService userRoleService)
        {
            _tokenManager = tokenManager;
            _roleService = roleService;
            _logger = logger;
            _rolePowerService = rolePowerService;
            _userRoleService = userRoleService;
        }


        /// <summary>
        /// 查询角色定义列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization]
        public async Task<IActionResult> Query([FromBody] RolesQueryDto parm)
        {
            //开始拼装查询条件
            var predicate = Expressionable.Create<Sys_Role>();

            //判断是否是超级管理员
            var userSession = await _tokenManager.GetSessionInfoAsync();

            predicate = predicate.AndIF(!userSession.Administrator, m => m.Administrator == false);

            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.Name), m => m.Name.Contains(parm.Name));

            var response =await   _roleService.GetPagesAsync(predicate.ToExpression(), parm);

            return toResponse(response);
        }


        /// <summary>
        /// 查询角色定义
        /// </summary>
        /// <param name="id">编码</param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> Get(string id)
        {
            if(!string.IsNullOrEmpty(id))
            {
                return toResponse(await  _roleService.GetIdAsync(id));
            }

            var userSession = await _tokenManager.GetSessionInfoAsync();

            //开始拼装查询条件
            var predicate = Expressionable.Create<Sys_Role>();

            predicate = predicate.AndIF(!userSession.Administrator, m => m.Administrator == false);

            return toResponse(( await  _roleService.GetWhereAsync(predicate.ToExpression())).OrderBy(m => m.CreateTime));
        }


        /// <summary>
        /// 添加角色定义
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_ROLES_CREATE")]
        public async Task<IActionResult> Create([FromBody] RolesCreateDto parm)
        {
            if (await  _roleService.AnyAsync(m => m.Name == parm.Name))
            {
                return toResponse(StatusCodeType.Error, $"添加 {parm.Name} 失败，该数据已存在，不能重复！");
            }

            //从 Dto 映射到 实体
            var role = parm.Adapt<Sys_Role>().ToCreate(await _tokenManager.GetSessionInfoAsync());

            return toResponse(await  _roleService.AddAsync(role));
        }

        /// <summary>
        /// 更新角色定义
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_ROLES_UPDATE")]
        public async Task<IActionResult> Update([FromBody] RolesUpdateDto parm)
        {
            if (await  _roleService.AnyAsync(m => m.Name == parm.Name && m.ID != parm.ID))
            {
                return toResponse(StatusCodeType.Error, $"更新 {parm.Name} 失败，该数据已存在，不能重复！");
            }

            var userSession = await _tokenManager.GetSessionInfoAsync();

            return toResponse(await  _roleService.UpdateAsync(m => m.ID == parm.ID, m => new Sys_Role() {
                Name = parm.Name,
                Remark = parm.Remark,
                UpdateID = userSession.UserID,
                UpdateName = userSession.UserName,
                UpdateTime = DateTime.Now
            }));
        }

        /// <summary>
        /// 删除角色定义
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization(Power = "PRIV_ROLES_DELETE")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return toResponse(StatusCodeType.Error, "删除角色 Id 不能为空");
            }

            if (await _rolePowerService.AnyAsync(m => m.RoleUID == id))
            {
                return toResponse(StatusCodeType.Error, "请先删除角色关联的权限");
            }

            if (await _userRoleService.AnyAsync(m => m.RoleID == id))
            {
                return toResponse(StatusCodeType.Error, "请先删除角色关联的用户");
            }

            var response = await  _roleService.DeleteAsync(id);

            return toResponse(response);
        }
    }
}
