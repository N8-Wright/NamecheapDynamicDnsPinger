using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Xml;

namespace NamecheapDynamicDnsPinger;

internal class PingerService : BackgroundService
{
    private readonly string _domain;
    private readonly string _subdomain;
    private readonly string _password;
    private readonly HttpClient _client;
    private readonly ILogger<PingerService> _logger;

    public PingerService(DomainInfo domainInfo, ILogger<PingerService> logger)
    {
        _domain = domainInfo.DomainName;
        _subdomain = domainInfo.Subdomain;
        _password = domainInfo.Password;
        _client = new HttpClient();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var requestUri = $"https://dynamicdns.park-your-domain.com/update?host={_subdomain}&domain={_domain}&password={_password}";
            while (!cancellationToken.IsCancellationRequested)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                var response = await _client.SendAsync(request, cancellationToken);
                var xmlContentString = await response.Content.ReadAsStringAsync(cancellationToken);
                string ipSet = GetIPFromXMLResponse(xmlContentString);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Unable to set dynamic host ip {xmlContentString}");
                }

                _logger.LogInformation($"Successfully set {_subdomain}.{_domain} to {ipSet}");
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }

    private static string GetIPFromXMLResponse(string xmlContentString)
    {
        var xml = new XmlDocument();
        xml.LoadXml(xmlContentString);
        var nodeList = xml.SelectNodes("interface-response");
        if (nodeList is null)
        {
            return string.Empty;
        }

        foreach (XmlNode? node in nodeList)
        {
            if (node is null) continue;
            var ipElement = node["IP"];
            if (ipElement is null) continue;

            return ipElement.InnerText;
        }

        return string.Empty;
    }
}
