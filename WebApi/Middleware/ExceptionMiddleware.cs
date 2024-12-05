using Core.Entities;
using Core.Entities.Error;
using System.Net;
using System.Security.AccessControl;
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
                ApiResponse response = new ApiResponse();
                _logger.LogError(ex, ex.Message);

                

                context.Response.ContentType = "application/json";

                if (ex is ApiException apiEx)
                {
                    context.Response.StatusCode = apiEx.StatusCode;
                    response = new ApiResponse()
                    {
                        IsSuccessful = false,
                        StatusCode = apiEx.StatusCode == 401 ? HttpStatusCode.Unauthorized : HttpStatusCode.InternalServerError,
                        ErrorMessages = new List<string> { ex.Message }
                    };
                } else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    response = _environment.IsDevelopment()
                        ? new ApiResponse()
                        {
                            IsSuccessful = false,
                            StatusCode = HttpStatusCode.InternalServerError,
                            ErrorMessages = new List<string> { ex.Message }
                        }
                        : new ApiResponse()
                        {
                            IsSuccessful = false,
                            StatusCode = HttpStatusCode.InternalServerError,
                            ErrorMessages = new List<string> { ex.Message }
                        };
                }

                

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(response, options);

                await context.Response.WriteAsync(json);

                // Guardar log en archivo de texto
                LogErrorToFile(ex);
            }
        }

        private static readonly object _lock = new object();

        private void LogErrorToFile(Exception ex)
        {
            string directoryPath = @"C:\Logs_Telchi";
            EnsureDirectoryWithPermissions(directoryPath);

            string filePath = Path.Combine(directoryPath, $"LOGs-{DateTime.Now:yyyy-MM-dd}.txt");
            EnsureFileWithPermissions(filePath);

            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error: {ex.Message}\nStack Trace: {ex.StackTrace}\n";

            lock (_lock)
            {
                //File.AppendAllText(filePath, logMessage);
            }
        }

        private void EnsureDirectoryWithPermissions(string directoryPath)
        {
            // Verificar si el directorio ya existe
            if (!Directory.Exists(directoryPath))
            {
                // Crear el directorio
                Directory.CreateDirectory(directoryPath);

                // Configurar permisos
                var directoryInfo = new DirectoryInfo(directoryPath);
                var directorySecurity = directoryInfo.GetAccessControl();

                // Agregar permisos para el usuario del proceso actual
                string user = Environment.UserDomainName + "\\" + Environment.UserName;
                directorySecurity.AddAccessRule(new FileSystemAccessRule(
                    user,                                // Usuario
                    FileSystemRights.FullControl,       // Permisos completos
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, // Aplicar a subdirectorios y archivos
                    PropagationFlags.None,              // Sin propagación
                    AccessControlType.Allow));          // Tipo de acceso: permitir

                // Aplicar los permisos al directorio
                directoryInfo.SetAccessControl(directorySecurity);
            }
        }

        private void EnsureFileWithPermissions(string filePath)
        {
            // Verificar si el archivo ya existe
            if (!File.Exists(filePath))
            {
                // Crear el archivo vacío
                File.Create(filePath).Dispose();

                // Configurar permisos
                var fileInfo = new FileInfo(filePath);
                var fileSecurity = fileInfo.GetAccessControl();

                // Agregar permisos para el usuario del proceso actual
                string user = Environment.UserDomainName + "\\" + Environment.UserName;
                fileSecurity.AddAccessRule(new FileSystemAccessRule(
                    user,                              // Usuario
                    FileSystemRights.FullControl,     // Permisos completos
                    AccessControlType.Allow));        // Tipo de acceso: permitir

                // Aplicar los permisos al archivo
                fileInfo.SetAccessControl(fileSecurity);
            }
        }
    }
}
