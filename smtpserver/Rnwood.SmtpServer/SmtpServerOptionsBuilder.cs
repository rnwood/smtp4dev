// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Rnwood.SmtpServer
{
    //public class SmtpServerOptionsBuilder
    //{

    //    private List<Action<SmtpServerOptionsFromBuilder>> actions = new List<Action<SmtpServerOptionsFromBuilder>>();

    //    public ISmtpServerOptions Build()
    //    {
    //        SmtpServerOptionsFromBuilder result = new SmtpServerOptionsFromBuilder()
    //        {
    //            DomainName = Dns.GetHostName(),
    //            PortNumber = 25
    //        };

    //        foreach(var action in actions)
    //        {
    //            action(result);
    //        }

    //        return result;
    //    }

    //    public SmtpServerOptionsBuilder WithPortNumber(int portNumber)
    //    {
    //        actions.Add(o=> o.PortNumber = portNumber);
    //        return this;
    //    }
    //    public SmtpServerOptionsBuilder WithPortNumber(StandardSmtpPort port)
    //    {
    //        actions.Add(o => o.PortNumber = (int)port);
    //        return this;
    //    }

    //    public SmtpServerOptionsBuilder WithDomainName(string domainName)
    //    {
    //        actions.Add(o => o.DomainName= domainName);
    //        return this;
    //    }

    //    public SmtpServerOptionsBuilder WithImplicitTls(X509Certificate2 certificate)
    //    {
    //        actions.Add(o => o.TlsMode = TlsMode.ImplicitTls);
    //        actions.Add(o => o.TlsCertificate = certificate);
    //        return this;
    //    }

    //    public SmtpServerOptionsBuilder WithStartTls(X509Certificate2 certificate)
    //    {
    //        actions.Add(o => o.TlsMode = TlsMode.StartTls);
    //        actions.Add(o => o.TlsCertificate = certificate);
    //        return this;
    //    }

    //    public SmtpServerOptionsBuilder WithIPAddress(bool allInterfaces, bool useIpV6)
    //    {
    //        if (allInterfaces)
    //        {
    //            actions.Add(o => o.IpAddress = useIpV6 ? IPAddress.IPv6Any : IPAddress.Any);
    //        } else
    //        {
    //            actions.Add(o => o.IpAddress = useIpV6 ? IPAddress.IPv6Loopback : IPAddress.Loopback);
    //        }
    //        return this;
    //    }

    //    public SmtpServerOptionsBuilder WithIPAddress(IPAddress ipAddress)
    //    {
    //        actions.Add(o => o.IpAddress = ipAddress);
    //        return this;
    //    }
    //}


    //internal class SmtpServerOptionsFromBuilder : ISmtpServerOptions
    //{
    //    public int PortNumber { get; internal set; }

    //    public string DomainName { get; internal set; }

    //    public TlsMode TlsMode { get; internal set; }

    //    public X509Certificate TlsCertificate { get; internal set; }

    //    public IPAddress IpAddress { get; internal set; }


    //}

}
