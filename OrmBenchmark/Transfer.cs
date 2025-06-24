using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace YourNamespace.Messaging
{
    // DTO для настроек IMAP
    public class ImapSettings
    {
        public string Host { get; set; }         // imap.example.com
        public int Port { get; set; }            // 993
        public bool UseSsl { get; set; } = true; // обычно true
        public string UserName { get; set; }
        public string Password { get; set; }
        public string InboxFolder { get; set; } = "INBOX";
    }

    // Интерфейс, чтобы можно было мокать в unit-тестах
    public interface IImapManager : IDisposable
    {
        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<(UniqueId Uid, MimeMessage Message)>> FetchUnreadAsync(CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(IEnumerable<UniqueId> uids, CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
    }

    public class ImapManager : IImapManager
    {
        private readonly ImapSettings _settings;
        private readonly ILogger<ImapManager> _logger;
        private readonly ImapClient _client;
        private bool _isConnected = false;
        private bool _disposed = false;

        public ImapManager(
            IOptions<ImapSettings> opts,
            ILogger<ImapManager> logger)
        {
            _settings = opts.Value;
            _logger = logger;
            _client = new ImapClient();
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_isConnected) return;

            try
            {
                _logger.LogInformation("IMAP: Connecting to {Host}:{Port}", _settings.Host, _settings.Port);
                await _client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl, cancellationToken);

                _logger.LogInformation("IMAP: Authenticating as {UserName}", _settings.UserName);
                await _client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);

                _isConnected = true;
                _logger.LogInformation("IMAP: Connected and authenticated");
            }
            catch (ImapCommandException icex)
            {
                _logger.LogError(icex, "IMAP command error during connect/authenticate");
                throw;
            }
            catch (ImapProtocolException ipex)
            {
                _logger.LogError(ipex, "IMAP protocol error during connect/authenticate");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during IMAP connect/authenticate");
                throw;
            }
        }

        public async Task<IEnumerable<(UniqueId Uid, MimeMessage Message)>> FetchUnreadAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
                await ConnectAsync(cancellationToken);

            try
            {
                var inbox = _client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

                // Поиск всех непрочитанных писем
                var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);
                if (!uids.Any())
                {
                    _logger.LogInformation("IMAP: No unread messages found");
                    return Enumerable.Empty<(UniqueId, MimeMessage)>();
                }

                _logger.LogInformation("IMAP: Found {Count} unread messages", uids.Count);
                // Выкачиваем все сообщения сразу
                var messages = await inbox.FetchAsync(uids, MessageSummaryItems.Full | MessageSummaryItems.UniqueId, cancellationToken);

                var result = new List<(UniqueId, MimeMessage)>();
                foreach (var summary in messages)
                {
                    try
                    {
                        var mime = await inbox.GetMessageAsync(summary.UniqueId, cancellationToken);
                        result.Add((summary.UniqueId, mime));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to fetch message UID {Uid}", summary.UniqueId);
                        // Решаем: подавить и продолжить или пробросить дальше?
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IMAP: Error fetching unread messages");
                throw;
            }
        }

        public async Task MarkAsReadAsync(IEnumerable<UniqueId> uids, CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
                await ConnectAsync(cancellationToken);

            try
            {
                var inbox = _client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

                // Помечаем флагом Seen
                await inbox.AddFlagsAsync(uids, MessageFlags.Seen, true, cancellationToken);
                _logger.LogInformation("IMAP: Marked {Count} messages as read", uids.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IMAP: Error marking messages as read");
                throw;
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected) return;

            try
            {
                await _client.DisconnectAsync(true, cancellationToken);
                _logger.LogInformation("IMAP: Disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IMAP: Error on disconnect");
            }
            finally
            {
                _isConnected = false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_client.IsConnected)
                    _client.Disconnect(true);
            }
            catch
            {
                // ничего
            }
            _client.Dispose();
            _disposed = true;
        }
    }
}
