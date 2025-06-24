private async Task<IMailFolder> OpenInboxAsync(CancellationToken cancellationToken)
{
    try
    {
        var inbox = _client.Inbox;
        if (!inbox.IsOpen)
            await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
        return inbox;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to open INBOX folder");
        throw;
    }
}

public async Task ConnectAsync(CancellationToken cancellationToken = default)
{
    if (_isConnected) return;

    var timeout = Policy
        .TimeoutAsync(TimeSpan.FromSeconds(10), TimeoutStrategy.Optimistic, (context, timespan, task, ex) =>
        {
            _logger.LogWarning("IMAP: Operation timed out after {Timeout}", timespan);
            return Task.CompletedTask;
        });

    var retry = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (ex, delay, attempt, ctx) =>
            {
                _logger.LogWarning(ex, "IMAP: Retry {Attempt} after {Delay}", attempt, delay);
            });

    var policyWrap = Policy.WrapAsync(retry, timeout);

    await policyWrap.ExecuteAsync(async ct =>
    {
        _logger.LogInformation("IMAP: Connecting to {Host}:{Port}", _settings.Host, _settings.Port);
        await _client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl, ct);

        _logger.LogInformation("IMAP: Authenticating as {User}", _settings.UserName);
        await _client.AuthenticateAsync(_settings.UserName, _settings.Password, ct);

        _isConnected = true;
        _logger.LogInformation("IMAP: Connected and authenticated");
    }, cancellationToken);
}
