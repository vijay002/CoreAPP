using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reva.Domain;
using Reva.Domain.Account;
using Reva.Domain.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Reva.Services.Account
{
    public class AccountManager : EntityBaseRepository<ApplicationUser>,IAccountManager
    {
        private  ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        

        //public AccountManager(ApplicationDbContext context) : base(context)
        //{
        //    _context = context;
        //}

        public AccountManager(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IHttpContextAccessor httpAccessor)
            :base(context)
        {
            _context = context;
            //_context.CurrentUserId = httpAccessor.HttpContext?.User.FindFirst(OpenIdConnectConstants.Claims.Subject)?.Value?.Trim();
            _userManager = userManager;
            _roleManager = roleManager;
        }


        #region Users


        public async Task<(bool Succeeded, string[] Errors)> CreateUserAsync(ApplicationUser user, IEnumerable<string> roles, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return (false, result.Errors.Select(e => e.Description).ToArray());


            user = await _userManager.FindByNameAsync(user.UserName);

            try
            {
                result = await this._userManager.AddToRolesAsync(user, roles.Distinct());
            }
            catch
            {
                await DeleteUserAsync(user);
                throw;
            }

            if (!result.Succeeded)
            {
                await DeleteUserAsync(user);
                return (false, result.Errors.Select(e => e.Description).ToArray());
            }

            return (true, new string[] { });
        }

        public async Task<(bool Succeeded, string[] Errors)> UpdateUserAsync(ApplicationUser user)
        {
            return await UpdateUserAsync(user, null);
        }


        public async Task<(bool Succeeded, string[] Errors)> UpdateUserAsync(ApplicationUser user, IEnumerable<string> roles)
        {

            //await _userManager.UpdateSecurityStampAsync(user); //20201225

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return (false, result.Errors.Select(e => e.Description).ToArray());


            if (roles != null)
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var rolesToRemove = userRoles.Except(roles).ToArray();
                var rolesToAdd = roles.Except(userRoles).Distinct().ToArray();

                if (rolesToRemove.Any())
                {
                    result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!result.Succeeded)
                        return (false, result.Errors.Select(e => e.Description).ToArray());

                }

                

                if (rolesToAdd.Any())
                {
                    //result = await _userManager.AddToRolesAsync(user, rolesToAdd);
                    //if (!result.Succeeded)
                    //    return (false, result.Errors.Select(e => e.Description).ToArray());


                    
                    foreach (var item in rolesToAdd)
                    {
                        var role = await _roleManager.FindByIdAsync(item);
                        var userResult = await _userManager.AddToRoleAsync(user, role.Name);
                        if (!userResult.Succeeded)
                            return (false, userResult.Errors.Select(e => e.Description).ToArray());
                    }

                }
            }

            return (true, new string[] { });
        }


        //[HttpGet("users/me")]
        //[ProducesResponseType(200, Type = typeof(UserViewModel))]
        //public async Task<IActionResult> GetCurrentUser()
        //{
        //    return await GetUserByUserName(this.User.Identity.Name);
        //}


        //[HttpGet("users/{id}", Name = GetUserByIdActionName)]
        //[ProducesResponseType(200, Type = typeof(UserViewModel))]
        //[ProducesResponseType(403)]
        //[ProducesResponseType(404)]
        //public async Task<IActionResult> GetUserById(string id)
        //{
        //    if (!(await _authorizationService.AuthorizeAsync(this.User, id, AccountManagementOperations.Read)).Succeeded)
        //        return new ChallengeResult();


        //    UserViewModel userVM = await GetUserViewModelHelper(id);

        //    if (userVM != null)
        //        return Ok(userVM);
        //    else
        //        return NotFound(id);
        //}


        //[HttpGet("users/username/{userName}")]
        //[ProducesResponseType(200, Type = typeof(UserViewModel))]
        //[ProducesResponseType(403)]
        //[ProducesResponseType(404)]
        //public async Task<IActionResult> GetUserByUserName(string userName)
        //{
        //    ApplicationUser appUser = await _accountManager.GetUserByUserNameAsync(userName);

        //    if (!(await _authorizationService.AuthorizeAsync(this.User, appUser?.Id ?? "", AccountManagementOperations.Read)).Succeeded)
        //        return new ChallengeResult();

        //    if (appUser == null)
        //        return NotFound(userName);

        //    return await GetUserById(appUser.Id);
        //}

        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<ApplicationUser> GetUserByUserNameAsync(string userName)
        {
            return await _userManager.FindByNameAsync(userName);
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }


        public async Task<(ApplicationUser User, string[] Roles)?> GetUserAndRolesAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Id == userId)
                .SingleOrDefaultAsync();

            if (user == null)
                return null;

            var userRoleIds = user.Roles.Select(r => r.RoleId).ToList();

            var roles = await _context.Roles
                .Where(r => userRoleIds.Contains(r.Id))
                .Select(r => r.Name)
                .ToArrayAsync();

            return (user, roles);
        }


        public async Task<List<(ApplicationUser User, string[] Roles)>> GetUsersAndRolesAsync(int page, int pageSize)
        {
            IQueryable<ApplicationUser> usersQuery = _context.Users
                .Include(u => u.Roles)
                .OrderBy(u => u.UserName);

            if (page != -1)
                usersQuery = usersQuery.Skip((page - 1) * pageSize);

            if (pageSize != -1)
                usersQuery = usersQuery.Take(pageSize);

            var users = await usersQuery.ToListAsync();

            var userRoleIds = users.SelectMany(u => u.Roles.Select(r => r.RoleId)).ToList();

            var roles = await _context.Roles
                .Where(r => userRoleIds.Contains(r.Id))
                .ToArrayAsync();

            return users
                .Select(u => (u, roles.Where(r => u.Roles.Select(ur => ur.RoleId).Contains(r.Id)).Select(r => r.Name).ToArray()))
                .ToList();
        }



        //public async Task<Tuple<ApplicationUser, string[]>> GetUserAndRolesAsync(string userId)
        //{
        //    var user = await _context.Users
        //        .Include(u => u.Roles)
        //        .Where(u => u.Id == userId)
        //        .FirstOrDefaultAsync();

        //    if (user == null)
        //        return null;

        //    var userRoleIds = user.Roles.Select(r => r.RoleId).ToList();

        //    var roles = await _context.Roles
        //        .Where(r => userRoleIds.Contains(r.Id))
        //        .Select(r => r.Name)
        //        .ToArrayAsync();

        //    return Tuple.Create(user, roles);
        //}

        //public async Task<List<(ApplicationUser User, string[] Roles)>> GetUsersAndRolesAsync(int page, int pageSize)
        //{
        //    IQueryable<ApplicationUser> usersQuery = _context.Users
        //        .Include(u => u.Roles)
        //        .OrderBy(u => u.UserName);

        //    if (page != -1)
        //        usersQuery = usersQuery.Skip((page - 1) * pageSize);

        //    if (pageSize != -1)
        //        usersQuery = usersQuery.Take(pageSize);

        //    var users = await usersQuery.ToListAsync();

        //    var userRoleIds = users.SelectMany(u => u.Roles.Select(r => r.RoleId)).ToList();

        //    var roles = await _context.Roles
        //        .Where(r => userRoleIds.Contains(r.Id))
        //        .ToArrayAsync();

        //    return users
        //        .Select(u => (u, roles.Where(r => u.Roles.Select(ur => ur.RoleId).Contains(r.Id)).Select(r => r.Name).ToArray()))
        //        .ToList();
        //}

        #endregion

        #region Roles
        public async Task<List<ApplicationRole>> GetRoles()
        {
            //var roles = await _context.Roles.Select(r => r.Name).ToArrayAsync();
            return await _context.Roles.ToListAsync(); 
        }

        public async Task<ApplicationRole> GetRoleByIdAsync(string roleId)
        {
            return await _roleManager.FindByIdAsync(roleId);
        }

      
        public async Task<ApplicationRole> GetRoleByNameAsync(string roleName)
        {
            return await _roleManager.FindByNameAsync(roleName);
        }


        public async Task<ApplicationRole> GetRoleLoadRelatedAsync(string roleName)
        {
            var role = await _context.Roles
                .Include(r => r.Claims)
                .Include(r => r.Users)
                .Where(r => r.Name == roleName)
                .FirstOrDefaultAsync();

            return role;
        }


        public async Task<Tuple<bool, string[]>> CreateRoleAsync(ApplicationRole role, IEnumerable<string> claims)
        {
            if (claims == null)
                claims = new string[] { };

            string[] invalidClaims = claims.Where(c => ApplicationPermissions.GetPermissionByValue(c) == null).ToArray();
            if (invalidClaims.Any())
                return Tuple.Create(false, new[] { "The following claim types are invalid: " + string.Join(", ", invalidClaims) });


            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());


            role = await _roleManager.FindByNameAsync(role.Name);

            foreach (string claim in claims.Distinct())
            {
                result = await this._roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, ApplicationPermissions.GetPermissionByValue(claim)));

                if (!result.Succeeded)
                {
                    await DeleteRoleAsync(role);
                    return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());
                }
            }

            return Tuple.Create(true, new string[] { });
        }

        public async Task<Tuple<bool, string[]>> UpdateRoleAsync(ApplicationRole role, IEnumerable<string> claims)
        {
            if (claims != null)
            {
                string[] invalidClaims = claims.Where(c => ApplicationPermissions.GetPermissionByValue(c) == null).ToArray();
                if (invalidClaims.Any())
                    return Tuple.Create(false, new[] { "The following claim types are invalid: " + string.Join(", ", invalidClaims) });
            }


            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
                return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());


            if (claims != null)
            {
                var roleClaims = (await _roleManager.GetClaimsAsync(role)).Where(c => c.Type == CustomClaimTypes.Permission);
                var roleClaimValues = roleClaims.Select(c => c.Value).ToArray();

                var claimsToRemove = roleClaimValues.Except(claims).ToArray();
                var claimsToAdd = claims.Except(roleClaimValues).Distinct().ToArray();

                if (claimsToRemove.Any())
                {
                    foreach (string claim in claimsToRemove)
                    {
                        result = await _roleManager.RemoveClaimAsync(role, roleClaims.Where(c => c.Value == claim).FirstOrDefault());
                        if (!result.Succeeded)
                            return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());
                    }
                }

                if (claimsToAdd.Any())
                {
                    foreach (string claim in claimsToAdd)
                    {
                        result = await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, ApplicationPermissions.GetPermissionByValue(claim)));
                        if (!result.Succeeded)
                            return Tuple.Create(false, result.Errors.Select(e => e.Description).ToArray());
                    }
                }
            }

            return Tuple.Create(true, new string[] { });
        }


        public async Task<Tuple<bool, string[]>> DeleteRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);

            if (role != null)
                return await DeleteRoleAsync(role);

            return Tuple.Create(true, new string[] { });
        }


        public async Task<Tuple<bool, string[]>> DeleteRoleAsync(ApplicationRole role)
        {
            var result = await _roleManager.DeleteAsync(role);
            return Tuple.Create(result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
        }


        public async Task<bool> TestCanDeleteRoleAsync(string roleId)
        {
            return !await _context.UserRoles.Where(r => r.RoleId == roleId).AnyAsync();
        }



        //public async Task<Tuple<bool, string[]>> DeleteUserAsync(string userId)
        //{
        //    var user = await _userManager.FindByIdAsync(userId);

        //    if (user != null)
        //        return await DeleteUserAsync(user);

        //    return Tuple.Create(true, new string[] { });
        //}


        //public async Task<Tuple<bool, string[]>> DeleteUserAsync(ApplicationUser user)
        //{
        //    var result = await _userManager.DeleteAsync(user);
        //    return Tuple.Create(result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
        //}

        //public async Task<bool> TestCanDeleteRoleAsync(string roleId)
        //{
        //    return !await _context.UserRoles.Where(r => r.RoleId == roleId).AnyAsync();
        //}


        //public async Task<bool> TestCanDeleteUserAsync(string userId)
        //{
        //    //if (await _context.Orders.Where(o => o.CashierId == userId).AnyAsync())
        //    //    return false;

        //    //canDelete = !await ; //Do other tests...

        //    return true;
        //}



        public async Task<(bool Succeeded, string[] Errors)> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
                return await DeleteUserAsync(user);

            return (true, new string[] { });
        }


        public async Task<(bool Succeeded, string[] Errors)> DeleteUserAsync(ApplicationUser user)
        {
            var result = await _userManager.DeleteAsync(user);
            return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
        }






        public async Task<bool> TestCanDeleteUserAsync(string userId)
        {
            //if (await _context.Orders.Where(o => o.CashierId == userId).AnyAsync())
            //    return false;

            //canDelete = !await ; //Do other tests...

            return true;
        }


        #endregion



        #region AspNet Role Feature

        private async Task<List<string>> GetUserRoleIds(ClaimsPrincipal ctx)
        {
            var userId = GetUserId(ctx);
            var data = await (from role in _context.UserRoles.AsNoTracking()
                              where role.UserId == userId
                              select role.RoleId).ToListAsync();

            return data;
        }

        private string GetUserId(ClaimsPrincipal user)
        {
            return ((ClaimsIdentity)user.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        #endregion


        public async Task<bool> GetMenuItemsAsync(ClaimsPrincipal ctx, string ctrl, string act)
        {
            var result = false;
            var roleIds = await GetUserRoleIds(ctx);
            var data = await (from menu in _context.AspNetRoleFeatures.AsNoTracking()
                              where roleIds.Contains(menu.RoleId)
                              select menu)
                              .Select(m => m.AspNetFeature).Distinct().ToListAsync();

            foreach (var item in data)
            {
                result = (item.ControllerName == ctrl && (item.ActionName == string.Empty || item.ActionName == act));
                if (result)
                    break;
            }

            return result;
        }


        public async Task<List<AspNetFeature>> GetMenuItemsAsync(ClaimsPrincipal principal)
        {
            var isAuthenticated = principal.Identity.IsAuthenticated;
            if (!isAuthenticated)
                return new List<AspNetFeature>();

            var roleIds = await GetUserRoleIds(principal);
            var data = await (from menu in _context.AspNetRoleFeatures
                              where roleIds.Contains(menu.RoleId)
                              select menu)
                              .Select(m => new AspNetFeature
                              {
                                   
                                  Id = m.AspNetFeature.Id,
                                  Name = m.AspNetFeature.Name,
                                  Area = m.AspNetFeature.Area,
                                  ActionName = m.AspNetFeature.ActionName,
                                  ControllerName = m.AspNetFeature.ControllerName,
                                  IsExternal = m.AspNetFeature.IsExternal,
                                  ExternalUrl = m.AspNetFeature.ExternalUrl,
                                  DisplayOrder = m.AspNetFeature.DisplayOrder,
                                  ParentFeatureId = m.AspNetFeature.ParentFeatureId,
                                  //Visible = m.AspNetFeature.Visible,
                                  Visible = m.AspNetFeature.Permitted,
                                  Full = m.AspNetFeature.Full,
                                  Add = m.AspNetFeature.Add,
                                  Edit = m.AspNetFeature.Edit,
                                  Delete = m.AspNetFeature.Delete
                              }).Distinct().ToListAsync();

            return data;
        }




        /// <summary>
        /// Get Permission By RoleId 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<List<AspNetFeature>> GetPermissionsByRoleIdAsync(string id)
        {
            var items = await (from m in _context.AspNetFeatures
                               join rm in _context.AspNetRoleFeatures
                                on new { X1 = m.Id, X2 = id } equals new { X1 = rm.AspNetFeatureId, X2 = rm.RoleId }
                                into rmp
                               from rm in rmp.DefaultIfEmpty()
                               orderby m.DisplayOrder 
                               select new AspNetFeature
                               {
                                   Id = m.Id,
                                   Name = m.Name,
                                   Area = m.Area,
                                   ActionName = m.ActionName,
                                   ControllerName = m.ControllerName,
                                   IsExternal = m.IsExternal,
                                   ExternalUrl = m.ExternalUrl,
                                   DisplayOrder = m.DisplayOrder,
                                   ParentFeatureId = m.ParentFeatureId,
                                   Visible = rm.Visible == null ? false :  Convert.ToBoolean( rm.Visible),
                                   Add = rm.Add == null ? false : Convert.ToBoolean(rm.Add),
                                   Delete = rm.Delete == null ? false : Convert.ToBoolean(rm.Delete),
                                   Edit = rm.Delete == null ? false : Convert.ToBoolean(rm.Edit),
                                   Full = rm.Full == null? false: Convert.ToBoolean(rm.Full),
                                   //,,Permitted = m.Id == new Guid(id)
                                    Permitted = rm.Visible == null ? false : Convert.ToBoolean(rm.Visible),
                               })
                               .AsNoTracking()
                               .ToListAsync();

            return items;
        }

        /// <summary>
        /// Set Permissions 
        /// </summary>
        /// <param name="id">RoleId </param>
        /// <param name="permissionIds">Permissionable guid list</param>
        /// 
        /// <returns></returns>
        public async Task<bool> SetPermissionsByRoleIdAsync(string id, IEnumerable<Guid> permissionIds, List<AspNetFeature> aspNetFeatures)
        {
            var existing = await _context.AspNetRoleFeatures.Where(x => x.RoleId == id).ToListAsync();
            _context.RemoveRange(existing);

            foreach (var item in permissionIds)
            {
                var objPermission = aspNetFeatures.FirstOrDefault(x => x.Id == item);
                await _context.AspNetRoleFeatures.AddAsync(new AspNetRoleFeature
                {
                    RoleId = id,
                    AspNetFeatureId = item,
                    //NavigationMenuId = item,
                    Full = objPermission.Full,
                    Add = objPermission.Add,
                    Edit = objPermission.Edit,
                    Delete = objPermission.Delete,
                    // Visible = objPermission.Visible,
                    Visible = objPermission.Permitted,

                });
            }

            var result = await _context.SaveChangesAsync();

            return result > 0;
        }




        

        //private async Task<List<string>> GetUserRoleIds(ClaimsPrincipal ctx)
        //{
        //    var userId = GetUserId(ctx);
        //    var data = await (from role in _context.UserRoles
        //                      where role.UserId == userId
        //                      select role.RoleId).ToListAsync();

        //    return data;
        //}

        //private string GetUserId(ClaimsPrincipal user)
        //{
        //    return ((ClaimsIdentity)user.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //}


    }
}
