﻿using Microsoft.AspNetCore.Http;
using Yoma.Core.Domain.Exceptions;

namespace Yoma.Core.Domain.Core.Helpers
{
    public static class HttpContextAccessorHelper
    {
        public static string GetUsername(IHttpContextAccessor? httpContextAccessor, bool useSystemDefault)
        {
            var claimsPrincipal = httpContextAccessor?.HttpContext?.User;
            var result = claimsPrincipal?.Identity?.Name;
            if (string.IsNullOrEmpty(result))
            {
                if (!useSystemDefault) throw new SecurityException("Unauthorized: User context not available");
                result = Constants.ModifiedBy_System_Username;
            }

            return result;
        }

        public static bool CanAdminOrganization(IHttpContextAccessor? httpContextAccessor)
        {
            var claimsPrincipal = httpContextAccessor?.HttpContext?.User;
            if (claimsPrincipal == null) return false;

            if (claimsPrincipal.IsInRole(Constants.Role_Admin)) return true;

            return claimsPrincipal.IsInRole(Constants.Role_Admin);

        }
    }
}
