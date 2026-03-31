namespace Litee.Engine.Model;

using System;

public class Url
{
    public string Value { get; }

    public Url(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL can't be empty.", nameof(url));
        }

        // ReSharper disable once ComplexConditionExpression
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("URL wrong format.", nameof(url));
        }

        Value = url;
    }

    public override string ToString() => Value;
}