using Meiam.System.Common;
using Meiam.System.Common.Utilities;
using Meiam.System.Core;
using Meiam.System.Hostd.Authorization;
using Meiam.System.Hostd.Extensions;
using Meiam.System.Interfaces;
using Meiam.System.Model;
using Meiam.System.Model.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Meiam.System.Hostd.Controllers.System
{
    /// <summary>
    /// 用户验证
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : BaseController
    {
        /// <summary>
        /// 会话管理接口
        /// </summary>
        private readonly ITokenManager _tokenManager;

        /// <summary>
        /// 日志管理接口
        /// </summary>
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// 用户服务接口
        /// </summary>
        private readonly ISysUsersService _userService;

        /// <summary>
        /// 用户关系接口
        /// </summary>
        private readonly ISysUserRelationService _userRelationService;

        public AuthController(ITokenManager tokenManager, ISysUsersService userService, ILogger<AuthController> logger,
            ISysUserRelationService userRelationService)
        {
            _tokenManager = tokenManager;
            _userService = userService;
            _logger = logger;
            _userRelationService = userRelationService;
        }

        /// <summary>
        /// 获取验证码
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Code()
        {
            var code = CaptchaUtil.GetRandomEnDigitalText();

            var verifyCode = CaptchaUtil.GenerateCaptchaImage(code);

           await RedisServer.Cache.SetAsync($"Captcha:{verifyCode.CaptchaGUID}", verifyCode.CaptchaCode, 1800);

            JObject result = new JObject();

            result.Add("captchaCode", $"data:image/png;base64,{Convert.ToBase64String(verifyCode.CaptchaMemoryStream.ToArray())}");
            result.Add("captchaGUID", verifyCode.CaptchaGUID);

            return toResponse(result);
        }

        /// <summary>
        /// 后台用户登录
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDto parm)
        {

            //var captchaCode = RedisServer.Cache.Get($"Captcha:{parm.Uuid}");

            //RedisServer.Cache.Del($"Captcha:{parm.Uuid}");

            //if (parm.Code.ToUpper() != captchaCode)
            //{        
            //    return toResponse(StatusCodeType.Error, "输入验证码无效");
            //}

            var userInfo = await  _userService.GetFirstAsync(o => o.UserID == parm.UserName.Trim());

            if (userInfo == null)
            {
                return toResponse(StatusCodeType.Error, "用户名或密码错误");
            }

            if (!PasswordUtil.ComparePasswords(userInfo.UserID, userInfo.Password, parm.PassWord.Trim()))
            {
                return toResponse(StatusCodeType.Error, "用户名或密码错误");
            }

            if (!userInfo.Enabled)
            {
                return toResponse(StatusCodeType.Error, "用户未启用，请联系管理员！");
            }

            var userToken = await _tokenManager.CreateSessionAsync(userInfo, SourceType.Web, Convert.ToInt32(AppSettings.Configuration["AppSettings:WebSessionExpire"]));

            return toResponse(userToken);
        }

        /// <summary>
        /// 微信小程序用户登录
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> LoginMiniProgram([FromBody] LoginMiniProgramDto parm)
        {
            var userInfo = await  _userService.GetFirstAsync(o => o.UserID == parm.UserName.Trim());

            if (userInfo == null)
            {
                return toResponse(StatusCodeType.Error, "用户名或密码错误");
            }

            if (!PasswordUtil.ComparePasswords(userInfo.UserID, userInfo.Password, parm.PassWord.Trim()))
            {
                return toResponse(StatusCodeType.Error, "用户名或密码错误");
            }

            if (!userInfo.Enabled)
            {
                return toResponse(StatusCodeType.Error, "用户未启用，请联系管理员！");
            }

            var userToken = await _tokenManager.CreateSessionAsync(userInfo, SourceType.MiniProgram, Convert.ToInt32(AppSettings.Configuration["AppSettings:MiniProgramSessionExpire"]));

            return toResponse(userToken);
        }

        /// <summary>
        /// 用户退出
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> LogOut()
        {
           await _tokenManager.RemoveSessionAsync( _tokenManager.GetSys_Token());

            return toResponse(StatusCodeType.Success);
        }

        /// <summary>
        /// 用户信息获取
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetUserInfo()
        {
            return toResponse(await _tokenManager.GetSessionInfoAsync());
        }

        /// <summary>
        /// 获取用户公司
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetUserCompany()
        {
            return toResponse(await  _userRelationService.GetUserCompanyAsync(await _tokenManager.GetSessionInfoAsync(), true));
        }

        /// <summary>
        /// 获取用户工厂
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetUserFactory()
        {
            return toResponse(await _userRelationService.GetUserFactoryAsync(await _tokenManager.GetSessionInfoAsync(), true));
        }

        /// <summary>
        /// 获取用户车间
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetUserWorkShop()
        {
            return toResponse(await _userRelationService.GetUserWorkShopAsync(await _tokenManager.GetSessionInfoAsync(), true));
        }

        /// <summary>
        /// 获取用户工序
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetUserProductProcess()
        {
            return toResponse(await _userRelationService.GetUserProductProcessAsync(await _tokenManager.GetSessionInfoAsync(), true));
        }

        /// <summary>
        /// 获取用户设备
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorization]
        public async Task<IActionResult> GetUserProductLine()
        {
            return toResponse(await _userRelationService.GetUserProductLineAsync(await _tokenManager.GetSessionInfoAsync(), true));
        }
    }
}