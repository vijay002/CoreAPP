using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Internal;
using rCRM.Application.Interfaces.Repositories;
using rCRM.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace rCRM.Persistence.Services
{
    public class AuthorizationRequirement: IAuthorizationRequirement
    {
        //public AuthorizationRequirement(string sessionHeaderName)
        //{
        //    SessionHeaderName = sessionHeaderName;
        //}

        //public string SessionHeaderName { get; }
    }


    public class PermissionHandler : AuthorizationHandler<AuthorizationRequirement>   //IAuthorizationHandler //: AuthorizationHandler<AuthorizationRequirement>
    {
        private readonly IUnitOfWork _context;
        private readonly IAccountManager _accountManager;
        //private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IActionContextAccessor _actionContextAccessor;
        public PermissionHandler(IUnitOfWork dataAccessService,
            IAccountManager accountManager,
            //IHttpContextAccessor httpContextAccessor
            IActionContextAccessor actionContextAccessor
            )
        {
            _context = dataAccessService;
            _accountManager = accountManager;

            
            //_httpContextAccessor = httpContextAccessor;

            _actionContextAccessor = actionContextAccessor;

            //var endpoint = _httpContextAccessor.HttpContext.GetEndpoint();
            //var descriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            //var controllerName = descriptor.ControllerName;

        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationRequirement requirement)
        {
            if (await IsUserAuthorizedAsync(context))
            {
                context.Succeed(requirement);
            }

            //TODO: Use the following if targeting a version of
            //.NET Framework older than 4.6:
            //      return Task.FromResult(0);

            //ontext.Succeed(requirement);
            //return Task.CompletedTask;
        }

        private async Task<bool> IsUserAuthorizedAsync(AuthorizationHandlerContext context)
        {
            // Now the id route value can be accessed directly...
            //var id = this._actionContextAccessor.ActionContext.RouteData.Values["id"]; 
           // var controllername = this._actionContextAccessor.ActionContext.RouteData.Values["controller"];
           // var actionName = this._actionContextAccessor.ActionContext.RouteData.Values["action"];

            if (context.Resource is HttpContext httpContext)
            {
                var endpoint = httpContext.GetEndpoint();

                //var authDatum = endpoint?.Metadata.GetOrderedMetadata<AuthorizationRequirement>()?? Array.Empty<AuthorizationRequirement>();
                var controllername = httpContext.GetRouteValue("controller");
                var actionName = httpContext.GetRouteValue("action");

                var isAuthenticated = context.User.Identity.IsAuthenticated;
                var IsPermitted = await _accountManager.GetMenuItemsAsync(context.User, controllername.ToString(), actionName.ToString());
                //context.User.Claims
                if (isAuthenticated && actionName != null && IsPermitted)
                {
                    //context.Succeed(requirement);
                    return true;
                }
            }
            // Use the dbContext to compare the id against the database...
            // Return the result
            return false;
        }

        //public Task HandleAsync(AuthorizationHandlerContext context)
        //{
        //    var pendingRequirements = context.PendingRequirements.ToList();

        //    var obj = context.Resource;


        //    //foreach (var requirement in pendingRequirements)
        //    //{
        //    //    if (requirement is ReadPermission)
        //    //    {
        //    //        if (IsOwner(context.User, context.Resource)
        //    //            || IsSponsor(context.User, context.Resource))
        //    //        {
        //    //            context.Succeed(requirement);
        //    //        }
        //    //    }
        //    //    else if (requirement is EditPermission || requirement is DeletePermission)
        //    //    {
        //    //        if (IsOwner(context.User, context.Resource))
        //    //        {
        //    //            context.Succeed(requirement);
        //    //        }
        //    //    }
        //    //}

        //    return Task.CompletedTask;
        //}

        //protected async override Task<Task> HandleRequirementAsync(AuthorizationHandlerContext context,
        //    AuthorizationRequirement requirement)
        //{
        //    var filterContext = context.Resource as AuthorizationFilterContext;
        //    var routeInfo = context.Resource as RouteEndpoint;
        //    var response = filterContext?.HttpContext.Response;

        //    //var pendingRequirements = context.PendingRequirements.ToList();
        //    //var tenant = _httpContextAccessor.HttpContext.GetRouteData().Values[ExceptionHandlerMiddleware.TenantCodeKey].ToString();


        //    var verb = this._httpContextAccessor.HttpContext.Request.Method;
        //    var routeKey = string.Empty;


        //    var httpRequest = _httpContextAccessor.HttpContext!.Request;
        //    var authContext = _contextFactory.CreateContext(requirements, user, resource);
        //    var handlers = await _handlers.GetHandlersAsync(authContext);

        //    //if (context.Resource is Endpoint endpoint)
        //    //{
        //    //    if (endpoint.Metadata.OfType<IFilterMetadata>().Any(filter => filter is AuthorizationFilterContext))
        //    //    {
        //    //        context.Succeed(requirement);
        //    //        return Task.CompletedTask;
        //    //    }
        //    //}


        //    if (this._actionContextAccessor.ActionContext.RouteData.Values.Count > 0)
        //    {
        //        var controllername = this._actionContextAccessor.ActionContext.RouteData.Values["controller"];
        //        var actionName = this._actionContextAccessor.ActionContext.RouteData.Values["action"];

        //        var isAuthenticated = context.User.Identity.IsAuthenticated;
        //        //var aMenu = await _accountManager.GetMenuItemsAsync(context.User);
        //        var IsPermitted = await _accountManager.GetMenuItemsAsync(context.User, controllername.ToString(), actionName.ToString());
        //        //context.User.Claims
        //        if (isAuthenticated && actionName != null && IsPermitted)
        //        {
        //            context.Succeed(requirement);
        //        }
        //    }

        //    return Task.CompletedTask;
        //}
        private static bool IsOwner(ClaimsPrincipal user, object? resource)
        {
            // Code omitted for brevity
            return true;
        }

        private static bool IsSponsor(ClaimsPrincipal user, object? resource)
        {
            // Code omitted for brevity
            return true;
        }
    }



    public class RolePolicyRequirement : IAuthorizationRequirement
    {
        //holds the array of roles
        public string[]? roles { get; }
        public RolePolicyRequirement()
        {
            
        }
        public RolePolicyRequirement(string? roles)
        {
            this.roles = roles == null ? null : roles.Split(",").ToArray<string>();
        }
    }
    public class RolePolicyHandler : AuthorizationHandler<RolePolicyRequirement>
    {
        private readonly IActionContextAccessor actionContextAccessor;
        private readonly IUnitOfWork _context;
        private readonly IAccountManager _accountManager;
        public RolePolicyHandler(IUnitOfWork dataAccessService,
            IAccountManager accountManager, IActionContextAccessor actionContextAccessor)
        {
            //this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));
            _context = dataAccessService;
            _accountManager = accountManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context
        , RolePolicyRequirement requirementVal)
        {
            

            //var filterContext = context.Resource as AuthorizationFilterContext;
            var id = this.actionContextAccessor.ActionContext.RouteData.Values["id"];

            if (this.actionContextAccessor.ActionContext.RouteData.Values.Count > 0)
            {
                var controllername = this.actionContextAccessor.ActionContext.RouteData.Values["controller"];
                var actionName = this.actionContextAccessor.ActionContext.RouteData.Values["action"];

                var isAuthenticated = context.User.Identity.IsAuthenticated;
                //var aMenu = await _accountManager.GetMenuItemsAsync(context.User);
                var IsPermitted = await _accountManager.GetMenuItemsAsync(context.User, controllername.ToString(), actionName.ToString());
                //context.User.Claims
                if (isAuthenticated && actionName != null && IsPermitted)
                {
                    context.Succeed(requirementVal);
                }
            }



            /*
            //if (context?.User?.Identity?.IsAuthenticated == false)
            //{
            //    context.Fail();
            //    return Task.CompletedTask;
            //}


            //getting custom_roles from our claims
            var roleClaims = context?.User.Claims.FirstOrDefault(x => x.Type == "custom_role")?.Value.ToString();
            //check if the claims contains the required roles
            if (roleClaims != null && (roleClaims.ToString().Split(",").ToArray().Any(requirementVal.roles.Contains)))
            {
                //contains the required claims
                context?.Succeed(requirementVal);
                return Task.CompletedTask;
            }
            //unauthorized
            context?.Fail();

          
            return Task.CompletedTask;  
            */
        }

        private bool IsUserAuthorized(AuthorizationHandlerContext context)
        {
            // Now the id route value can be accessed directly...
            var id = this.actionContextAccessor.ActionContext.RouteData.Values["id"];

            // Use the dbContext to compare the id against the database...

            // Return the result
            return true;
        }

    }



}
