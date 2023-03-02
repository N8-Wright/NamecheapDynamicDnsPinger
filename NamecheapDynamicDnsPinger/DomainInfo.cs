namespace NamecheapDynamicDnsPinger;

internal class DomainInfo
{
    public DomainInfo(string domainName, string subdomain, string password)
    {
        DomainName = domainName;
        Subdomain = subdomain;
        Password = password;
    }

    public string DomainName { get; set; }

    public string Subdomain { get; set; }

    public string Password { get; set; }
}
