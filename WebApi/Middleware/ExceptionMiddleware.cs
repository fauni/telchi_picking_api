using Core.Entities;
using System.Net;
using System.Text.Json;

namespace WebApi.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                // Guardar log en archivo de texto
                LogErrorToFile(ex);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = _environment.IsDevelopment()
                    ? new ApiResponse()
                    {
                        IsSuccessful = false,
                        StatusCode = HttpStatusCode.InternalServerError,
                        ErrorMessages = new List<string> { ex.Message }
                    }
                    : new ApiResponse()
                    {
                        IsSuccessful = false,
                        StatusCode = HttpStatusCode.InternalServerError
                    };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(response, options);

                await context.Response.WriteAsync(json);
            }
        }

        private void LogErrorToFile(Exception ex)
        {
            // Crear directorio si no existe
            string directoryPath = @"C:\Logs_Telchi";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Crear el nombre del archivo con la fecha actual
            string filePath = Path.Combine(directoryPath, $"log-{DateTime.Now:yyyy-MM-dd}.txt");

            // Crear el mensaje de log con fecha y hora
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error: {ex.Message}\nStack Trace: {ex.StackTrace}\n";

            // Escribir en el archivo (agregar si ya existe)
            File.AppendAllText(filePath, logMessage);
        }
    }
}
