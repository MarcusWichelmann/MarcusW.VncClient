using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;
using ReactiveUI;
using Microsoft.Extensions.Logging;

namespace BlazorVncClient.Services
{
    public class InteractiveAuthenticationHandler : IAuthenticationHandler
    {
        private readonly ILogger<InteractiveAuthenticationHandler> _logger;
        
        public Interaction<PasswordPromptRequest, PasswordPromptResponse> EnterPasswordInteraction { get; } = new Interaction<PasswordPromptRequest, PasswordPromptResponse>();
        public Interaction<CredentialsPromptRequest, CredentialsPromptResponse> EnterCredentialsInteraction { get; } = new Interaction<CredentialsPromptRequest, CredentialsPromptResponse>();

        public InteractiveAuthenticationHandler(ILogger<InteractiveAuthenticationHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<TInput> ProvideAuthenticationInputAsync<TInput>(RfbConnection connection, ISecurityType securityType, IAuthenticationInputRequest<TInput> request)
            where TInput : class, IAuthenticationInput
        {
            _logger.LogInformation("Authentication requested for security type '{SecurityType}' (ID: {SecurityTypeId})", 
                securityType.Name, securityType.Id);

            if (typeof(TInput) == typeof(PasswordAuthenticationInput))
            {
                return await HandlePasswordAuthenticationAsync<TInput>(securityType);
            }

            if (typeof(TInput) == typeof(CredentialsAuthenticationInput))
            {
                return await HandleCredentialsAuthenticationAsync<TInput>(securityType);
            }

            throw new InvalidOperationException($"Authentication input type '{typeof(TInput).Name}' is not supported by the interactive authentication handler.");
        }

        private async Task<TInput> HandlePasswordAuthenticationAsync<TInput>(ISecurityType securityType)
            where TInput : class, IAuthenticationInput
        {
            var promptRequest = new PasswordPromptRequest
            {
                SecurityTypeName = securityType.Name,
                SecurityTypeId = securityType.Id,
                Title = GetPasswordPromptTitle(securityType),
                Message = GetPasswordPromptMessage(securityType)
            };

            try
            {
                var response = await EnterPasswordInteraction.Handle(promptRequest).FirstAsync();
                
                if (response.IsCancelled)
                {
                    _logger.LogInformation("Password authentication was cancelled by user");
                    throw new OperationCanceledException("Authentication was cancelled by the user.");
                }

                if (string.IsNullOrEmpty(response.Password))
                {
                    _logger.LogWarning("Empty password provided for authentication");
                    // Allow empty passwords in case the server accepts them
                }

                _logger.LogDebug("Password authentication input provided");
                return (TInput)Convert.ChangeType(new PasswordAuthenticationInput(response.Password ?? string.Empty), typeof(TInput));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password authentication prompt");
                throw new InvalidOperationException("Failed to get password authentication input.", ex);
            }
        }

        private async Task<TInput> HandleCredentialsAuthenticationAsync<TInput>(ISecurityType securityType)
            where TInput : class, IAuthenticationInput
        {
            var promptRequest = new CredentialsPromptRequest
            {
                SecurityTypeName = securityType.Name,
                SecurityTypeId = securityType.Id,
                Title = GetCredentialsPromptTitle(securityType),
                Message = GetCredentialsPromptMessage(securityType)
            };

            try
            {
                var response = await EnterCredentialsInteraction.Handle(promptRequest).FirstAsync();
                
                if (response.IsCancelled)
                {
                    _logger.LogInformation("Credentials authentication was cancelled by user");
                    throw new OperationCanceledException("Authentication was cancelled by the user.");
                }

                if (string.IsNullOrEmpty(response.Username) && string.IsNullOrEmpty(response.Password))
                {
                    _logger.LogWarning("Empty credentials provided for authentication");
                }

                _logger.LogDebug("Credentials authentication input provided for user '{Username}'", response.Username);
                return (TInput)Convert.ChangeType(new CredentialsAuthenticationInput(response.Username ?? string.Empty, response.Password ?? string.Empty), typeof(TInput));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during credentials authentication prompt");
                throw new InvalidOperationException("Failed to get credentials authentication input.", ex);
            }
        }

        private static string GetPasswordPromptTitle(ISecurityType securityType)
        {
            return securityType.Name switch
            {
                "VncAuth" => "VNC Authentication",
                "VeNCrypt" => "VeNCrypt Authentication",
                _ => $"{securityType.Name} Authentication"
            };
        }

        private static string GetPasswordPromptMessage(ISecurityType securityType)
        {
            return securityType.Name switch
            {
                "VncAuth" => "Enter the VNC server password:",
                "VeNCrypt" => "Enter the VeNCrypt password:",
                _ => $"Enter the password for {securityType.Name} authentication:"
            };
        }

        private static string GetCredentialsPromptTitle(ISecurityType securityType)
        {
            return securityType.Name switch
            {
                "VeNCrypt" => "VeNCrypt User Authentication",
                _ => $"{securityType.Name} User Authentication"
            };
        }

        private static string GetCredentialsPromptMessage(ISecurityType securityType)
        {
            return securityType.Name switch
            {
                "VeNCrypt" => "VeNCrypt requires both username and password for authentication:",
                _ => $"Enter your username and password for {securityType.Name} authentication:"
            };
        }
    }

    // Request/Response classes for better type safety and extensibility
    public class PasswordPromptRequest
    {
        public string SecurityTypeName { get; set; } = string.Empty;
        public byte SecurityTypeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class PasswordPromptResponse
    {
        public string? Password { get; set; }
        public bool IsCancelled { get; set; }
    }

    public class CredentialsPromptRequest
    {
        public string SecurityTypeName { get; set; } = string.Empty;
        public byte SecurityTypeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class CredentialsPromptResponse
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool IsCancelled { get; set; }
    }
}
