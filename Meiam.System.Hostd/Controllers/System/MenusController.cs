using Mapster;
using Meiam.System.Hostd.Authorization;
using Meiam.System.Hostd.Extensions;
using Meiam.System.Interfaces;
using Meiam.System.Model;
using Meiam.System.Model.Dto;
using Meiam.System.Model.View;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Meiam.System.Hostd.Controllers.System
{
    /// <summary>
    /// 菜单接口
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MenusController : BaseController
    {
        /// <summary>
        /// 会话管理接口
        /// </summary>
        private readonly ITokenManager _tokenManager;
        /// <summary>
        /// 日志管理接口
        /// </summary>
        private readonly ILogger<MenusController> _logger;

        /// <summary>
        /// 菜单接口
        /// </summary>
        private readonly ISysMenuService _menuService;

        public MenusController(ITokenManager tokenManager, ISysMenuService menuService, ILogger<MenusController> logger)
        {
            _tokenManager = tokenManager;
            _menuService = menuService;
            _logger = logger;
        }


        /// <summary>
        /// 查询菜单列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization]
        public async Task<IActionResult> Query([FromBody] MenusQueryDto parm)
        {
            if (string.IsNullOrEmpty(parm.Name))
            {
                //获取系统所有菜单
                var allMenus = await  _menuService.GetAllAsync();

                return toResponse(ResolveMenuTree(allMenus));
            }

            //开始拼装查询条件
            var predicate = Expressionable.Create<Sys_Menu>();

            predicate = predicate.And(m => m.Name.Contains(parm.Name));

            //获取系统所有菜单
            var serachMenus = (await  _menuService.GetWhereAsync(predicate.ToExpression(), m => m.SortIndex)).Select(m => new MenuListVM
            {
                ID = m.ID,
                Name = m.Name,
                Icon = m.Icon,
                Path = m.Path,
                Component = m.Component,
                SortIndex = m.SortIndex,
                ViewPower = m.ViewPower,
                ParentUID = m.ParentUID,
                Remark = m.Remark,
                Hidden = m.Hidden,
                System = m.System,
                isFrame = m.isFrame,
                KeepAlive = m.KeepAlive,
                Children = null,
                CreateTime = m.CreateTime,
                UpdateTime = m.UpdateTime,
                CreateID = m.CreateID,
                CreateName = m.CreateName,
                UpdateID = m.UpdateID,
                UpdateName = m.UpdateName
            });

            return toResponse(serachMenus);
        }


        /// <summary>
        /// 查询菜单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> Get(string id)
        {
            var response = await  _menuService.GetIdAsync(id);

            return toResponse(response);
        }

        /// <summary>
        /// 添加菜单
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_MENUS_CREATE")]
        public async Task<IActionResult> Create([FromBody] MenusCreateDto parm)
        {
            //从 Dto 映射到 实体
            var menu = parm.Adapt<Sys_Menu>();

            //如果 ParentUID 是 -1 更新为 null
            menu.ParentUID = menu.ParentUID == "-1" ? null : menu.ParentUID;

            menu.ToCreate(await _tokenManager.GetSessionInfoAsync());

            return toResponse(await  _menuService.AddAsync(menu));
        }

        /// <summary>
        /// 更新菜单
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorization(Power = "PRIV_MENUS_UPDATE")]
        public async Task<IActionResult> Update([FromBody] MenusUpdateDto parm)
        {
            var userSession = await _tokenManager.GetSessionInfoAsync();

            parm.ParentUID = parm.ParentUID == "-1" ? null : parm.ParentUID;

            return toResponse(await  _menuService.UpdateAsync(m => m.ID == parm.ID, m => new Sys_Menu()
            {
                Name = parm.Name,
                Icon = parm.Icon,
                Path = parm.Path,
                Component = parm.Component,
                SortIndex = parm.SortIndex,
                ViewPower = parm.ViewPower,
                ParentUID = parm.ParentUID,
                Remark = parm.Remark,
                System = parm.System,
                isFrame = parm.isFrame,
                Hidden = parm.Hidden,
                KeepAlive = parm.KeepAlive,
                UpdateID = userSession.UserID,
                UpdateName = userSession.UserName,
                UpdateTime = DateTime.Now
            }));
        }

        /// <summary>
        /// 删除菜单
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization(Power = "PRIV_MENUS_DELETE")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return toResponse(StatusCodeType.Error, "删除菜单 Id 不能为空");
            }

            bool hasChildren = await  _menuService.AnyAsync(m => m.ParentUID == id);

            if (hasChildren)
            {
                return toResponse(StatusCodeType.Error , "检测到下级含有子菜单，请先删除子菜单！");
            }

            var response = await  _menuService.DeleteAsync(id);

            return toResponse(response);
        }

        /// <summary>
        /// 获取用户菜单
        /// </summary>
        /// <param name="system">所属系统</param>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetUserMenus(int system = 0)
        {
            var _userSession = await _tokenManager.GetSessionInfoAsync();

            var menus = await  _menuService.GetWhereAsync(m => (string.IsNullOrEmpty(m.ViewPower) || _userSession.UserPower.Contains(m.ViewPower)) && m.System == system, m => m.SortIndex);

            return toResponse(ResolveUserMenuTree(menus));
        }
    }
}
