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
    /// 权限定义
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PowersController : BaseController
    {
        /// <summary>
        /// 会话管理接口
        /// </summary>
        private readonly ITokenManager _tokenManager;

        /// <summary>
        /// 日志管理接口
        /// </summary>
        private readonly ILogger<PowersController> _logger;

        /// <summary>
        /// 权限定义接口
        /// </summary>
        private readonly ISysPowerService _powerService;

        /// <summary>
        /// 角色权限接口
        /// </summary>
        private readonly ISysRolePowerService _rolePowerService;

        public PowersController(ITokenManager tokenManager, ISysPowerService powerService, ISysRolePowerService rolePowerService, ILogger<PowersController> logger)
        {
            _tokenManager = tokenManager;
            _powerService = powerService;
            _rolePowerService = rolePowerService;
            _logger = logger;
        }


        /// <summary>
        /// 查询权限定义列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization]
        public async Task<IActionResult> Query([FromBody] PowersQueryDto parm)
        {
            //开始拼装查询条件
            var predicate = Expressionable.Create<Sys_Power>();

            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.Name), m => m.Name.Contains(parm.Name) || m.Page.Contains(parm.Name));

            var response = await _powerService.GetPagesAsync(predicate.ToExpression(), parm);

            return toResponse(response);
        }


        /// <summary>
        /// 查询权限定义
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> Get(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                return toResponse(await _powerService.GetIdAsync(id));
            }
            return toResponse(await _powerService.GetAllAsync());
        }

        /// <summary>
        /// 添加权限定义
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_POWERS_CREATE")]
        public async Task<IActionResult> Create([FromBody] PowersCreateDto parm)
        {
            if (await _powerService.AnyAsync(m => m.Name == parm.Name))
            {
                return toResponse(StatusCodeType.Error, $"添加 {parm.Name} 失败，该数据已存在，不能重复！");
            }

            //从 Dto 映射到 实体
            var power = parm.Adapt<Sys_Power>().ToCreate(await _tokenManager.GetSessionInfoAsync());

            return toResponse(await _powerService.AddAsync(power));
        }

        /// <summary>
        /// 更新权限定义
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_POWERS_UPDATE")]
        public async Task<IActionResult> Update([FromBody] PowersUpdateDto parm)
        {
            if (await _powerService.AnyAsync(m => m.Name == parm.Name && m.ID != parm.ID))
            {
                return toResponse(StatusCodeType.Error, $"更新 {parm.Name} 失败，该数据已存在，不能重复！");
            }

            var userSession = await _tokenManager.GetSessionInfoAsync();

            return toResponse(await _powerService.UpdateAsync(m => m.ID == parm.ID, m => new Sys_Power()
            {
                Name = parm.Name,
                Remark = parm.Remark,
                Page = parm.Page,
                Description = parm.Description,
                UpdateID = userSession.UserID,
                UpdateName = userSession.UserName,
                UpdateTime = DateTime.Now
            }));
        }

        /// <summary>
        /// 删除权限定义
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization(Power = "PRIV_POWERS_DELETE")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return toResponse(StatusCodeType.Error, "删除权限 Id 不能为空");
            }

            try
            {
                _rolePowerService.BeginTran();
                await _rolePowerService.DeleteAsync(o => o.PowerUID == id);
                await _powerService.DeleteAsync(id);
                _rolePowerService.CommitTran();
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
