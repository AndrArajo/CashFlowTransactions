using CashFlowTransactions.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace CashFlowTransactions.API.Filters
{
    /// <summary>
    /// Filtro que captura exceções não tratadas e retorna uma resposta padronizada em JSON
    /// </summary>
    public class ApiExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ApiExceptionFilter> _logger;
        private readonly IHostEnvironment _environment;

        public ApiExceptionFilter(
            ILogger<ApiExceptionFilter> logger,
            IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, context.Exception.Message);

            var statusCode = HttpStatusCode.InternalServerError;
            var errorMessage = "Ocorreu um erro interno no servidor.";

            if (context.Exception is ArgumentException || context.Exception is FormatException)
            {
                statusCode = HttpStatusCode.BadRequest;
                errorMessage = context.Exception.Message;
            }

            var details = _environment.IsDevelopment()
                ? new List<string> { context.Exception.StackTrace ?? "" }
                : null;

            var response = ApiResponse<object>.Error(
                errorMessage,
                statusCode,
                details);

            context.Result = new ObjectResult(response)
            {
                StatusCode = response.StatusCode
            };

            context.ExceptionHandled = true;
        }
    }
} 