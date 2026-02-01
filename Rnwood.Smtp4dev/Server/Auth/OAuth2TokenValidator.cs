// <copyright file="OAuth2TokenValidator.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Rnwood.Smtp4dev.Server.Auth;

/// <summary>
/// Service for validating OAuth2/XOAUTH2 tokens against an Identity Provider.
/// </summary>
public class OAuth2TokenValidator
{
    private readonly ILogger log;
    private ConfigurationManager<OpenIdConnectConfiguration> configurationManager;
    private string currentAuthority;
    private string currentAudience;
    private string currentIssuer;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth2TokenValidator"/> class.
    /// </summary>
    /// <param name="log">Logger instance.</param>
    public OAuth2TokenValidator(ILogger log)
    {
        this.log = log;
    }

    /// <summary>
    /// Validates an OAuth2 access token against the configured IDP.
    /// </summary>
    /// <param name="token">The access token to validate.</param>
    /// <param name="authority">The OAuth2 authority URL (e.g., https://login.microsoftonline.com/common/v2.0).</param>
    /// <param name="audience">The expected audience for the token.</param>
    /// <param name="issuer">The expected issuer for the token (optional, will use discovery if not provided).</param>
    /// <returns>A tuple containing validation success status, subject claim, and error message if any.</returns>
    public async Task<(bool IsValid, string Subject, string Error)> ValidateTokenAsync(
        string token, 
        string authority, 
        string audience, 
        string issuer)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, null, "Token is null or empty");
            }

            if (string.IsNullOrWhiteSpace(authority))
            {
                return (false, null, "OAuth2Authority is not configured");
            }

            // Initialize or update configuration manager if authority changed
            if (configurationManager == null || currentAuthority != authority)
            {
                var metadataAddress = authority.TrimEnd('/') + "/.well-known/openid-configuration";
                configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataAddress,
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever());
                currentAuthority = authority;
                currentAudience = audience;
                currentIssuer = issuer;
            }

            // Get OpenID Connect configuration
            var config = await configurationManager.GetConfigurationAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            // Set issuer validation
            if (!string.IsNullOrWhiteSpace(issuer))
            {
                validationParameters.ValidIssuer = issuer;
            }
            else
            {
                validationParameters.ValidIssuers = new[] { config.Issuer };
            }

            // Set audience validation
            if (!string.IsNullOrWhiteSpace(audience))
            {
                validationParameters.ValidAudience = audience;
            }

            var handler = new JwtSecurityTokenHandler();
            
            // Validate token
            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            // Extract subject claim (typically 'sub', 'email', or 'preferred_username')
            var subject = principal.FindFirst("sub")?.Value
                ?? principal.FindFirst("email")?.Value
                ?? principal.FindFirst("preferred_username")?.Value
                ?? principal.FindFirst("upn")?.Value;

            if (string.IsNullOrWhiteSpace(subject))
            {
                return (false, null, "Token does not contain a valid subject claim (sub, email, preferred_username, or upn)");
            }

            log.Information("OAuth2 token validated successfully. Subject: {subject}", subject);
            return (true, subject, null);
        }
        catch (SecurityTokenExpiredException ex)
        {
            log.Warning("OAuth2 token validation failed: Token expired. Error: {error}", ex.Message);
            return (false, null, $"Token expired: {ex.Message}");
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            log.Warning("OAuth2 token validation failed: Invalid signature. Error: {error}", ex.Message);
            return (false, null, $"Invalid token signature: {ex.Message}");
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            log.Warning("OAuth2 token validation failed: Invalid issuer. Error: {error}", ex.Message);
            return (false, null, $"Invalid token issuer: {ex.Message}");
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            log.Warning("OAuth2 token validation failed: Invalid audience. Error: {error}", ex.Message);
            return (false, null, $"Invalid token audience: {ex.Message}");
        }
        catch (Exception ex)
        {
            log.Error(ex, "OAuth2 token validation failed with unexpected error");
            return (false, null, $"Token validation error: {ex.Message}");
        }
    }
}
