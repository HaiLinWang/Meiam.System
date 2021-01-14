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

namespace Meiam.System.Hostd.Controllers.Basic
{
    /// <summary>
    /// 工序定义
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductProcessController : BaseController
    {
        /// <summary>
        /// 日志管理接口
        /// </summary>
        private readonly ILogger<ProductProcessController> _logger;
        /// <summary>
        /// 会话管理接口
        /// </summary>
        private readonly ITokenManager _tokenManager;

        /// <summary>
        /// 工序定义接口
        /// </summary>
        private readonly IBaseProductProcessService _processService;

        /// <summary>
        /// 数据关系接口
        /// </summary>
        private readonly ISysDataRelationService _dataRelationService;


        public ProductProcessController(ILogger<ProductProcessController> logger, ITokenManager tokenManager, IBaseProductProcessService processService, ISysDataRelationService dataRelationService)
        {
            _logger = logger;
            _tokenManager = tokenManager;
            _processService = processService;
            _dataRelationService = dataRelationService;
        }


        /// <summary>
        /// 查询工序定义列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization]
        public async Task<IActionResult> Query([FromBody] ProductProcessQueryDto parm)
        {
            var response = await _processService.QueryProcessPagesAsync(parm);
            return toResponse(response);
        }


        /// <summary>
        /// 根据 Id 查询工序定义
        /// </summary>
        /// <param name="id">编码</param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> Get(string id = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                return toResponse(StatusCodeType.Error, "生工序 Id 不能为空");
            }
            return toResponse(await _processService.GetProcessAsync(id));
        }

        /// <summary>
        /// 查询所有工序定义
        /// </summary>
        /// <param name="enable">是否启用（不传返回所有）</param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetAll(bool? enable = null)
        {
            return toResponse(await _processService.GetAllProcessAsync(enable));
        }


        /// <summary>
        /// 添加工序定义
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_WORKSHOP_CREATE")]
        public async Task<IActionResult> Create([FromBody] ProductProcessCreateDto parm)
        {
            try
            {

                var process = parm.Adapt<Base_ProductProcess>().ToCreate(await _tokenManager.GetSessionInfoAsync());

                if (await _processService.AnyAsync(m => m.ProcessNo == parm.ProcessNo))
                {
                    return toResponse(StatusCodeType.Error, $"添加工序编码 {parm.ProcessNo} 已存在，不能重复！");
                }

                //从 Dto 映射到 实体
                _dataRelationService.BeginTran();

                var response = await _processService.AddAsync(process);

                //插入关系表
                await _dataRelationService.AddAsync(new Sys_DataRelation
                {
                    ID = GetGUID,
                    Form = process.ID,
                    To = parm.WorkShopUID,
                    Type = DataRelationType.Process_To_WorkShop.ToString()
                });

                _dataRelationService.CommitTran();

                return toResponse(response);
            }
            catch (Exception ex)
            {
                _dataRelationService.RollbackTran();
                throw ex;
            }
        }

        /// <summary>
        /// 更新工序定义
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_WORKSHOP_UPDATE")]
        public async Task<IActionResult> Update([FromBody] ProductProcessUpdateDto parm)
        {
            if (await _processService.AnyAsync(m => m.ProcessNo == parm.ProcessNo && m.ID != parm.ID))
            {
                return toResponse(StatusCodeType.Error, $"添加工序编码 {parm.ProcessNo} 已存在，不能重复！");
            }

            try
            {
                _dataRelationService.BeginTran();

                var userSession = await _tokenManager.GetSessionInfoAsync();

                var response = await _processService.UpdateAsync(m => m.ID == parm.ID, m => new Base_ProductProcess()
                {
                    ProcessNo = parm.ProcessNo,
                    ProcessName = parm.ProcessName,
                    Enable = parm.Enable,
                    Remark = parm.Remark,
                    UpdateID = userSession.UserID,
                    UpdateName = userSession.UserName,
                    UpdateTime = DateTime.Now
                });

                //删除关系表
                await _dataRelationService.DeleteAsync(m => m.Form == parm.ID && m.Type == DataRelationType.Process_To_WorkShop.ToString());

                //插入关系表
                await _dataRelationService.AddAsync(new Sys_DataRelation
                {
                    ID = GetGUID,
                    Form = parm.ID,
                    To = parm.WorkShopUID,
                    Type = DataRelationType.Process_To_WorkShop.ToString()
                });

                _dataRelationService.CommitTran();

                return toResponse(response);
            }
            catch (Exception ex)
            {
                _dataRelationService.RollbackTran();
                throw ex;
            }
        }

        /// <summary>
        /// 删除工序定义
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization(Power = "PRIV_WORKSHOP_DELETE")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return toResponse(StatusCodeType.Error, "删除工序 Id 不能为空");
            }

            if (await _dataRelationService.AnyAsync(m => m.To == id))
            {
                return toResponse(StatusCodeType.Error, "该工序已被关联，无法删除，若要请先删除关联");
            }

            try
            {
                _dataRelationService.BeginTran();
                await _dataRelationService.DeleteAsync(m => m.Form == id && m.Type == DataRelationType.Process_To_WorkShop.ToString());
                var response = await _processService.DeleteAsync(id);
                _dataRelationService.CommitTran();

                return toResponse(response);
            }
            catch (Exception ex)
            {
                _dataRelationService.RollbackTran();
                throw ex;
            }
        }
    }
}
