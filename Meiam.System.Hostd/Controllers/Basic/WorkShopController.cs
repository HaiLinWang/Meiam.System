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
    /// 车间定义
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class WorkShopController : BaseController
    {
        /// <summary>
        /// 日志管理接口
        /// </summary>
        private readonly ILogger<WorkShopController> _logger;
        /// <summary>
        /// 会话管理接口
        /// </summary>
        private readonly ITokenManager _tokenManager;

        /// <summary>
        /// 车间定义接口
        /// </summary>
        private readonly IBaseWorkShopService _workShopService;

        /// <summary>
        /// 数据关系接口
        /// </summary>
        private readonly ISysDataRelationService _dataRelationService;


        public WorkShopController(ILogger<WorkShopController> logger, ITokenManager tokenManager, IBaseWorkShopService workShopService, ISysDataRelationService dataRelationService)
        {
            _logger = logger;
            _tokenManager = tokenManager;
            _workShopService = workShopService;
            _dataRelationService = dataRelationService;
        }


        /// <summary>
        /// 查询车间定义列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization]
        public async Task<IActionResult> Query([FromBody] WorkShopQueryDto parm)
        {
            var response = await  _workShopService.QueryWorkShopPagesAsync(parm);
            return toResponse(response);
        }


        /// <summary>
        /// 根据 Id 查询车间定义
        /// </summary>
        /// <param name="id">编码</param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> Get(string id = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                return toResponse(StatusCodeType.Error, "车间 Id 不能为空");
            }
            return toResponse(await  _workShopService.GetWorkShopAsync(id));
        }

        /// <summary>
        ///  查询所有车间定义
        /// </summary>
        /// <param name="enable">是否启用（不传返回所有）</param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetAll(bool? enable = null)
        {
            return toResponse(await  _workShopService.GetAllWorkShopAsync(enable));
        }


        /// <summary>
        /// 添加车间定义
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_WORKSHOP_CREATE")]
        public async Task<IActionResult> Create([FromBody] WorkShopCreateDto parm)
        {
            try
            {
                var workshop = parm.Adapt<Base_WorkShop>().ToCreate(await _tokenManager.GetSessionInfoAsync());

                if (await  _workShopService.AnyAsync(workshop.ID, parm.WorkShopNo, parm.FactoryUID))
                {
                    return toResponse(StatusCodeType.Error, $"添加车间编码 {parm.WorkShopNo} 已存在，不能重复！");
                }

                //从 Dto 映射到 实体
                _dataRelationService.BeginTran();

                var response = await  _workShopService.AddAsync(workshop);

                //插入关系表
               await _dataRelationService.AddAsync(new Sys_DataRelation
                {
                    ID = GetGUID,
                    Form = workshop.ID,
                    To = parm.FactoryUID,
                    Type = DataRelationType.WorkShop_To_Factory.ToString()
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
        /// 更新车间定义
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_WORKSHOP_UPDATE")]
        public async Task<IActionResult> Update([FromBody] WorkShopUpdateDto parm)
        {
            if (await  _workShopService.AnyAsync(parm.ID, parm.WorkShopNo, parm.FactoryUID))
            {
                return toResponse(StatusCodeType.Error, $"添加车间编码 {parm.WorkShopNo} 已存在，不能重复！");
            }

            try
            {
                _dataRelationService.BeginTran();

                var userSession = _tokenManager.GetSessionInfo();

                var response = await  _workShopService.UpdateAsync(m => m.ID == parm.ID, m => new Base_WorkShop()
                {
                    WorkShopNo = parm.WorkShopNo,
                    WorkShopName = parm.WorkShopName,
                    Enable = parm.Enable,
                    Remark = parm.Remark,
                    UpdateID = userSession.UserID,
                    UpdateName = userSession.UserName,
                    UpdateTime = DateTime.Now
                });

                //删除关系表
              await  _dataRelationService.DeleteAsync(m => m.Form == parm.ID && m.Type == DataRelationType.WorkShop_To_Factory.ToString());

                //插入关系表
              await  _dataRelationService.AddAsync(new Sys_DataRelation
                {
                    ID = GetGUID,
                    Form = parm.ID,
                    To = parm.FactoryUID,
                    Type = DataRelationType.WorkShop_To_Factory.ToString()
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
        /// 删除车间定义
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization(Power = "PRIV_WORKSHOP_DELETE")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return toResponse(StatusCodeType.Error, "删除车间 Id 不能为空");
            }

            if (await _dataRelationService.AnyAsync(m => m.To == id))
            {
                return toResponse(StatusCodeType.Error, "该车间已被关联，无法删除，若要请先删除关联");
            }

            try
            {
                _dataRelationService.BeginTran();
               await _dataRelationService.DeleteAsync(m => m.Form == id && m.Type == DataRelationType.WorkShop_To_Factory.ToString());
                var response = await  _workShopService.DeleteAsync(id);
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
