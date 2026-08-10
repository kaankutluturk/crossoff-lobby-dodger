using System.Net.Http.Headers;
using System.Text.Json;

namespace CrossOff.LobbyDodger;

public sealed class BlacklistService : IDisposable
{
    private const int MaximumBytes = 1_000_000;
    private const int MaximumEntries = 10_000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly string _cachePath;

    public BlacklistService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("CrossOffLobbyDodger", "0.1"));

        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CrossOffLobbyDodger");
        Directory.CreateDirectory(directory);
        _cachePath = Path.Combine(directory, "blacklist-cache.json");
    }

    public BlacklistDocument Current { get; private set; } = new();

    public async Task<BlacklistUpdateResult> RefreshAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidDataException("The blacklist URL must be an absolute HTTPS URL.");
            }

            using HttpResponseMessage response = await _httpClient.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength is > MaximumBytes)
            {
                throw new InvalidDataException("The blacklist exceeds the 1 MB limit.");
            }

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var limitedStream = new MemoryStream();
            var buffer = new byte[8192];
            int total = 0;

            while (true)
            {
                int read = await responseStream.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                total += read;
                if (total > MaximumBytes)
                {
                    throw new InvalidDataException("The blacklist exceeds the 1 MB limit.");
                }

                await limitedStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            byte[] jsonBytes = limitedStream.ToArray();
            BlacklistDocument document = ParseAndValidate(jsonBytes);
            Current = document;

            string temporaryPath = _cachePath + ".tmp";
            await File.WriteAllBytesAsync(temporaryPath, jsonBytes, cancellationToken);
            File.Move(temporaryPath, _cachePath, true);

            return new BlacklistUpdateResult(document, false, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or
                                          TaskCanceledException or
                                          IOException or
                                          JsonException or
                                          InvalidDataException)
        {
            BlacklistDocument? cached = TryLoadCache();
            if (cached is not null)
            {
                Current = cached;
                return new BlacklistUpdateResult(cached, true, exception.Message);
            }

            return new BlacklistUpdateResult(Current, false, exception.Message);
        }
    }

    private BlacklistDocument? TryLoadCache()
    {
        try
        {
            return File.Exists(_cachePath)
                ? ParseAndValidate(File.ReadAllBytes(_cachePath))
                : null;
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException)
        {
            return null;
        }
    }

    private static BlacklistDocument ParseAndValidate(ReadOnlySpan<byte> json)
    {
        BlacklistDocument document = JsonSerializer.Deserialize<BlacklistDocument>(json, JsonOptions)
            ?? throw new InvalidDataException("The blacklist response was empty.");

        if (document.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported blacklist schema: {document.SchemaVersion}.");
        }

        if (document.Entries.Count > MaximumEntries)
        {
            throw new InvalidDataException($"The blacklist contains more than {MaximumEntries} entries.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (BlacklistEntry entry in document.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) ||
                string.IsNullOrWhiteSpace(entry.Group) ||
                entry.Aliases.Count is 0 or > 64 ||
                entry.Aliases.Any(static alias => NameMatcher.Normalize(alias).Length < 3) ||
                string.IsNullOrWhiteSpace(entry.Reason) ||
                !Uri.TryCreate(entry.EvidenceUrl, UriKind.Absolute, out Uri? evidenceUri) ||
                (evidenceUri.Scheme != Uri.UriSchemeHttps && evidenceUri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidDataException($"Blacklist entry '{entry.Id}' is incomplete or invalid.");
            }

            if (!ids.Add(entry.Id))
            {
                throw new InvalidDataException($"Duplicate blacklist ID: {entry.Id}.");
            }
        }

        return document;
    }

    public void Dispose() => _httpClient.Dispose();
}

public sealed record BlacklistUpdateResult(
    BlacklistDocument Document,
    bool UsedCache,
    string? Error);
