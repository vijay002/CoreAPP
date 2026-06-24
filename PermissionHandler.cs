using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Reva.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Patterns;
using Reva.Services.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Diagnostics;

namespace Reva.HelpDesk
{
    public class AuthorizationRequirement : IAuthorizationRequirement { }

    public class PermissionHandler : AuthorizationHandler<AuthorizationRequirement>
    {

        private readonly IUnitOfWork _context;
        private readonly IAccountManager _accountManager;
        private readonly IHttpContextAccessor _httpContextAccessor;


        //public PermissionHandler(IUnitOfWork dataAccessService,
        //    IAccountManager accountManager,
        //    IHttpContextAccessor httpContextAccessor)
        //{
        //    _httpContextAccessor = httpContextAccessor;
        //}

        public PermissionHandler(IUnitOfWork dataAccessService,
            IAccountManager accountManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = dataAccessService;
            _accountManager = accountManager;


            _httpContextAccessor = httpContextAccessor;
            //var endpoint = _httpContextAccessor.HttpContext.GetEndpoint();
            //var descriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            //var controllerName = descriptor.ControllerName;

        }


        //protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionHandler requirement)
        //{
        //    var tenant = _httpContextAccessor.HttpContext.GetRouteData().Values[ExceptionHandlerMiddleware.TenantCodeKey].ToString();
        //}

        protected async  override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            AuthorizationRequirement requirement)
        {

            //var endpoint = _httpContextAccessor.HttpContext.GetEndpoint();
            //var descriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            //var controllerName = descriptor.ControllerName;

            //var tenant = _httpContextAccessor.HttpContext.GetRouteData().Values[ExceptionHandlerMiddleware.TenantCodeKey].ToString();

            var filterContext = context.Resource as AuthorizationFilterContext;
            var routeInfo = context.Resource as RouteEndpoint;
            var response = filterContext?.HttpContext.Response;


            var verb = this._httpContextAccessor.HttpContext.Request.Method;
            var routeKey = string.Empty;
            if (filterContext.RouteData.Values.Count > 0)
            {
                var controllername = filterContext.RouteData.Values["controller"];
                var actionName = filterContext.RouteData.Values["action"];

                var isAuthenticated = context.User.Identity.IsAuthenticated;
                //var aMenu = await _accountManager.GetMenuItemsAsync(context.User);
                var IsPermitted = await _accountManager.GetMenuItemsAsync(context.User, controllername.ToString(), actionName.ToString());
                //context.User.Claims
                if (isAuthenticated && actionName != null && IsPermitted)// -- temp Stopeed
                {
                    context.Succeed(requirement);
                }


            }

            //if (context.Resource is FilterContext endpoint)
            //{
            //    var cad = endpoint.Metadata.OfType<ControllerActionDescriptor>().FirstOrDefault();

            //    var controllerFullName = cad.ControllerTypeInfo.FullName;
            //    var actionName = cad.ActionName;
            //    var bindings = cad.Parameters;
            //    var actionParams = ".";

            //    if (bindings.Any())
            //    {
            //        bindings.ToList().ForEach(p => actionParams += p.ParameterType.Name + ".");
            //    }

            //    routeKey = $"{controllerFullName}.{actionName}{actionParams}{verb}";
            //}


            //if (context.Resource is RouteEndpoint endpoint)
            //{
            //    //endpoint.RoutePattern.RequiredValues.TryGetValue("controller", out var _controller);
            //    //endpoint.RoutePattern.RequiredValues.TryGetValue("action", out var _action);

            //    //endpoint.RoutePattern.RequiredValues.TryGetValue("page", out var _page);
            //    //endpoint.RoutePattern.RequiredValues.TryGetValue("area", out var _area);

            //    var isAuthenticated = context.User.Identity.IsAuthenticated;

            //    //if (isAuthenticated && _controller != null && _action != null &&
            //    //    await _accountManager.GetMenuItemsAsync(context.User, _controller.ToString(), _action.ToString()))
            //    //{
            //    //    context.Succeed(requirement);
            //    //}


            //}


            //if (context.User != null)
            //{
            //    var isAuthenticated = context.User.Identity.IsAuthenticated;
            //    var aMenu = await _accountManager.GetMenuItemsAsync(context.User);
            //    //context.User.Claims
            //    if (isAuthenticated )
            //    {
            //        context.Succeed(requirement);
            //    }
            //}



        }
    }
}
