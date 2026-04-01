using Maui_Task.Shared.Helpers;
// FILE: Middleware/ExceptionHandlingMiddleware.cs
// STATUS: NEW
// CHANGES: Global exception handling middleware (#4) — replaces repetitive try-catch in every controller

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Maui_Task.Web.Helpers;

namespace Maui_Task.Web.Middleware
{
    /// <summary>
    /// Catches unhandled exceptions from controllers/services and returns consistent
    /// <see cref="ApiResponse{T}"/> error payloads with appropriate HTTP status codes.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = exception switch
            {
                KeyNotFoundException knf => (HttpStatusCode.NotFound, knf.Message),
                UnauthorizedAccessException uae => (HttpStatusCode.Forbidden, uae.Message),
                InvalidOperationException ioe => (HttpStatusCode.BadRequest, ioe.Message),
                ArgumentException ae => (HttpStatusCode.BadRequest, ae.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            // Log the full exception for 500s; just warn for expected exceptions
            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogWarning("Handled exception ({StatusCode}) on {Method} {Path}: {Message}",
                    (int)statusCode, context.Request.Method, context.Request.Path, exception.Message);
            }

            var response = ApiResponse<object>.Fail(message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }
}
