// <copyright file="SoapHttpClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Client;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Soap HTTP Client.
/// </summary>
public sealed partial class SoapHttpClient : ISoapHttpClient
{
    private static readonly XNamespace SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace XsiNs = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace XsdNs = "http://www.w3.org/2001/XMLSchema";

    private readonly HttpClient httpClient;
    private readonly ILogger<SoapHttpClient> logger;

    public SoapHttpClient(
        HttpClient httpClient,
        ILogger<SoapHttpClient> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    private Action<string, string?>? VerboseOutputAction { get; set; }

    private string? BaseUrl { get; set; }

    private string? SoapAction { get; set; }

    private Dictionary<string, string> Headers { get; set; } = new();

    private string? MediaType { get; set; }

    private bool IncludeXmlDeclaration { get; set; }

    public ISoapHttpClient WithVerboseOutput(Action<string, string?>? verboseOutputAction)
    {
        VerboseOutputAction = verboseOutputAction;

        return this;
    }

    public ISoapHttpClient WithBaseUrl(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        this.BaseUrl = baseUrl;

        return this;
    }

    public ISoapHttpClient WithSoapAction(string soapAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(soapAction);

        this.SoapAction = soapAction;

        return this;
    }

    public ISoapHttpClient WithMediaType(string mediaType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);

        this.MediaType = mediaType;

        return this;
    }

    public ISoapHttpClient WithHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Headers[name] = value;

        return this;
    }

    public ISoapHttpClient WithXmlDeclaration(bool includeXmlDeclaration)
    {
        IncludeXmlDeclaration = includeXmlDeclaration;

        return this;
    }

    public async Task<SoapResponse> PostAsync(
        string relativeUrl,
        XElement payload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException("Base URL must be set before making a soap request.");
        }

        if (string.IsNullOrWhiteSpace(SoapAction))
        {
            throw new InvalidOperationException("Soap action must be set before making a soap request.");
        }

        if (string.IsNullOrWhiteSpace(MediaType))
        {
            throw new InvalidOperationException("Media type must be set before making a soap request.");
        }

        var absoluteUrl = new Uri($"{BaseUrl}/{relativeUrl}", UriKind.Absolute);

        var soapEnvelope = BuildSoapEnvelope(payload);

        var httpContent = IncludeXmlDeclaration
            ? new StringContent(GetSoapEnvelopeWithXmlDeclaration(soapEnvelope), Encoding.UTF8, MediaType)
            : new StringContent(soapEnvelope.ToString(), Encoding.UTF8, MediaType);

        EmmitVerboseOutput("Created Soap Envelope:", await httpContent.ReadAsStringAsync(cancellationToken));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, absoluteUrl);

        httpRequest.Content = httpContent;
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaType) { CharSet = "utf-8" };

        httpRequest.Headers.Add("SOAPAction", SoapAction);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));

        foreach (var header in Headers)
        {
            httpRequest.Headers.Add(header.Key, header.Value);
        }

        LogCallingSoapEndpoint(logger, absoluteUrl.ToString());

        EmmitVerboseOutput("Sending SOAP Request to:", absoluteUrl.ToString());

        var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);

        try
        {
            httpResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new SoapResponseException($"HTTP Response Error: {ex.Message}", ex);
        }

        var soapResponse = await ExtractSoapResponse(httpResponse, cancellationToken);

        LogSuccessfullyCalledSoapEndpoint(logger, absoluteUrl.ToString());

        EmmitVerboseOutput("Successfully sent SOAP Request to:", absoluteUrl.ToString());

        return soapResponse;
    }

    private static XElement BuildSoapEnvelope(XElement payload)
    {
        var soapEnvelope = new XElement(
            SoapNs + "Envelope",
            new XAttribute(XNamespace.Xmlns + "xsi", XsiNs),
            new XAttribute(XNamespace.Xmlns + "xsd", XsdNs),
            new XAttribute(XNamespace.Xmlns + "soap", SoapNs),
            new XElement(SoapNs + "Body", payload));

        return soapEnvelope;
    }

    private static string GetSoapEnvelopeWithXmlDeclaration(XElement soapEnvelope)
    {
        var envelopeBuilder = new StringBuilder();

        envelopeBuilder.Append(new XDeclaration("1.0", "utf-8", null));
        envelopeBuilder.AppendLine();
        envelopeBuilder.Append(soapEnvelope);

        return envelopeBuilder.ToString();
    }

    private async Task<SoapResponse> ExtractSoapResponse(
        HttpResponseMessage httpResponse,
        CancellationToken cancellationToken = default)
    {
        if (httpResponse.StatusCode == HttpStatusCode.NoContent ||
            httpResponse.Content == null ||
            httpResponse.Content.Headers.ContentLength is 0)
        {
            return new SoapResponse() { HasContent = false, HasBody = false, BodyContent = null };
        }

        await using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

        var bodyElement = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Body");

        if (bodyElement == null)
        {
            return new SoapResponse() { HasContent = true, HasBody = false, BodyContent = null };
        }

        var bodyContent = bodyElement.Elements().FirstOrDefault();

        if (bodyContent == null)
        {
            return new SoapResponse() { HasContent = true, HasBody = true, BodyContent = null };
        }

        var soapFaultElement = bodyContent.Elements().FirstOrDefault(e => e.Name.LocalName == "Fault");

        var hasSoapFault = soapFaultElement != null;

        var soapFaultCode = soapFaultElement?.Elements().FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value;

        var soapFaultDetails =
            soapFaultElement?.Elements().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value;

        EmmitVerboseOutput("Response Body Received:", bodyContent.ToString());

        return new SoapResponse()
        {
            HasContent = true,
            HasBody = true,
            BodyContent = bodyContent,
            HasSoapFault = hasSoapFault,
            SoapFaultCode = soapFaultCode,
            SoapFaultDetails = soapFaultDetails,
        };
    }

    private void EmmitVerboseOutput(string description, string? data)
    {
        VerboseOutputAction?.Invoke(description, data);
    }
}
