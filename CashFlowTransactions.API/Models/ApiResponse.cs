using System.Net;

namespace CashFlowTransactions.API.Models
{

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
        public int StatusCode { get; set; }
        public IEnumerable<string>? Errors { get; set; }

        /// <summary>
        /// Cria uma resposta de sucesso
        /// </summary>
        public static ApiResponse<T> Ok(T data, string message = "Operação realizada com sucesso")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        /// <summary>
        /// Cria uma resposta de erro
        /// </summary>
        public static ApiResponse<T> Error(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError, IEnumerable<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = (int)statusCode,
                Errors = errors
            };
        }

        /// <summary>
        /// Cria uma resposta para quando não encontra o recurso
        /// </summary>
        public static ApiResponse<T> NotFound(string message = "Recurso não encontrado")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = (int)HttpStatusCode.NotFound
            };
        }

        /// <summary>
        /// Cria uma resposta para requisição inválida
        /// </summary>
        public static ApiResponse<T> BadRequest(string message, IEnumerable<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Errors = errors
            };
        }
    }
} 