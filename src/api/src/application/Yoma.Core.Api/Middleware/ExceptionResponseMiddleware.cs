﻿using System.Net;
using Yoma.Core.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using Yoma.Core.Domain.Core.Models;

namespace Yoma.Core.Api.Middleware
{
    public class ExceptionResponseMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; //default

            List<ErrorResponseItem> errorResponse;
            switch (ex)
            {
                case FluentValidation.ValidationException:
                    var myEx = (FluentValidation.ValidationException)ex;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                    errorResponse = myEx.Errors.Select(o => new ErrorResponseItem() { Type = ex.GetType().Name, Message = o.ErrorMessage }).ToList();
                    return context.Response.WriteAsJsonAsync(errorResponse);

                case BusinessException:
                case ValidationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case SecurityException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                case DataCollisionException:
                case DataInconsistencyException:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            errorResponse = new List<ErrorResponseItem>()
            {
                new ErrorResponseItem { Type = ex.GetType().Name, Message =ex.Message }
            };

            return context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}
