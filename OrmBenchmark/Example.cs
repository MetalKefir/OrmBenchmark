public class ProcessService
{
    private readonly IImapManager _imap;
    private readonly ILogger<ProcessService> _logger;

    public ProcessService(IImapManager imap, ILogger<ProcessService> logger)
    {
        _imap = imap;
        _logger = logger;
    }

    public async Task ProcessMailAsync(CancellationToken ct)
    {
        try
        {
            // 1) Получаем непрочитаные письма
            var mails = await _imap.FetchUnreadAsync(ct);

            // 2) Обрабатываем каждое
            var processedUids = new List<UniqueId>();
            foreach (var (uid, msg) in mails)
            {
                try
                {
                    // ваша бизнес-логика
                    _logger.LogInformation("Processing mail from {From}, subject: {Subject}", msg.From, msg.Subject);
                    // ... 
                    
                    processedUids.Add(uid);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message UID {Uid}", uid);
                    // решите: продолжить или прервать весь процесс
                }
            }

            // 3) Помечаем обработанные прочитанными
            if (processedUids.Any())
            {
                await _imap.MarkAsReadAsync(processedUids, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ProcessMailAsync");
            // например, отправить alert, retry и т.д.
        }
        finally
        {
            await _imap.DisconnectAsync(ct);
        }
    }
}
