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
    /// 字典定义
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OptionsController : BaseController
    {
        /// <summary>
        /// 会话管理接口
        /// </summary>
        private readonly ITokenManager _tokenManager;

        /// <summary>
        /// 日志管理接口
        /// </summary>
        private readonly ILogger<OptionsController> _logger;

        /// <summary>
        /// 字典定义接口
        /// </summary>
        private readonly ISysOptionsService _optionService;

        public OptionsController(ITokenManager tokenManager, ISysOptionsService optionService, ILogger<OptionsController> logger)
        {
            _tokenManager = tokenManager;
            _optionService = optionService;
            _logger = logger;
        }


        /// <summary>
        /// 查询字典定义列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization]
        public async Task<IActionResult> Query([FromBody] OptionsQueryDto parm)
        {
            //开始拼装查询条件
            var predicate = Expressionable.Create<Sys_Options>();

            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.QueryText), m => m.Option.Contains(parm.QueryText));

            var response =await   _optionService.GetPagesAsync(predicate.ToExpression(), parm);

            return toResponse(response);
        }


        /// <summary>
        /// 查询字典定义
        /// </summary>
        /// <param name="id">编码</param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> Get(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                return toResponse( await  _optionService.GetIdAsync(id));
            }
            return toResponse(await  _optionService.GetAllAsync());
        }

        /// <summary>
        /// 根据分组查询字典
        /// </summary>
        /// <param name="option">字典分组</param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetOption(string option)
        {
            if (string.IsNullOrEmpty(option))
            {
                return toResponse(StatusCodeType.Error, "group 不能为空");
            }

            var response = (await  _optionService.GetWhereAsync(m => m.Option == option)).OrderBy(m => m.Option).OrderBy(m => m.SortIndex);

            return toResponse(response);
        }

        /// <summary>
        /// 添加字典定义
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_OPTIONS_CREATE")]
        public async Task<IActionResult> Create([FromBody] OptionsCreateDto parm)
        {
            //从 Dto 映射到 实体
            var options = parm.Adapt<Sys_Options>().ToCreate(await _tokenManager.GetSessionInfoAsync());

            return toResponse(await  _optionService.AddAsync(options));
        }

        /// <summary>
        /// 更新字典定义
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_OPTIONS_UPDATE")]
        public async Task<IActionResult> Update([FromBody] OptionsUpdateDto parm)
        {
            var userSession = await _tokenManager.GetSessionInfoAsync();

            return toResponse(await  _optionService.UpdateAsync(m => m.ID == parm.ID, m => new Sys_Options()
            {
                Option = parm.Option,
                Label = parm.Label,
                Value = parm.Value,
                SortIndex = parm.SortIndex,
                Remark = parm.Remark,
                UpdateID = userSession.UserID,
                UpdateName = userSession.UserName,
                UpdateTime = DateTime.Now
            }));
        }

        /// <summary>
        /// 删除字典定义
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization(Power = "PRIV_OPTIONS_DELETE")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return toResponse(StatusCodeType.Error, "删除字典 Id 不能为空");
            }

            var response = await  _optionService.DeleteAsync(id);

            return toResponse(response);
        }
    }
}
