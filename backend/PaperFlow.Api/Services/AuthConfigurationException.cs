namespace PaperFlow.Api.Services;

public sealed class AuthConfigurationException(string message) : InvalidOperationException(message);
