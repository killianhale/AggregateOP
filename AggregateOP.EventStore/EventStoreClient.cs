using System;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using ContextRunner;
using ContextRunner.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReasonCodeExceptions;

namespace AggregateOP.EventStore
{
    public class EventStoreClient : IEventStoreClient
    {
        private readonly IContextRunner _runner;
        private readonly ILogger<EventStoreClient> _logger;
        private readonly EventStoreConfig _config;

        public EventStoreClient(ILogger<EventStoreClient> logger, IOptionsMonitor<EventStoreConfig> eventStoreOptions) : this(null, logger, eventStoreOptions) { }

        public EventStoreClient(IContextRunner runner, ILogger<EventStoreClient> logger, IOptionsMonitor<EventStoreConfig> eventStoreOptions)
        {
            _runner = runner ?? new ContextRunner.ContextRunner();
            _logger = logger;
            _config = eventStoreOptions.CurrentValue;
        }

        public async Task ConnectWithContext(Func<IEventStoreConnection, ActionContext, Task> action, string contextSubName = null)
        {
            var contextSuffix = contextSubName != null
                ? $".{contextSubName}"
                : "";

            await _runner.RunAction(async context =>
            {
                try
                {
                    context.State.SetParam("eventStoreConnection", SanitizedConnectionString);

                    using var connection = Connect();
                    await action(connection, context);
                }
                catch (WrongExpectedVersionException ex)
                {
                    throw LogAndReturnException(context.Logger.Debug, new DataConflictException("Stale data! Unable to write to stream.", ex));
                }
                catch (EventStoreConnectionException ex) when (ex is NoResultException || ex is StreamDeletedException)
                {
                    throw LogAndReturnException(context.Logger.Debug, new DataNotFoundException("The requested data was not found.", ex));
                }
                catch (EventStoreConnectionException ex) when (ex is CommandNotExpectedException || ex is ProjectionCommandConflictException
                    || ex is ProjectionCommandFailedException || ex is UserCommandConflictException || ex is UserCommandFailedException)
                {
                    throw LogAndReturnException(context.Logger.Debug, new MiscDataException("There was a problem with the command to EventStore.", ex));
                }
                catch (EventStoreConnectionException ex) when (ex is AccessDeniedException || ex is NotAuthenticatedException)
                {
                    throw LogAndReturnException(context.Logger.Error, new ServiceAuthenticationException("EventStore", ex));
                }
                catch (EventStoreConnectionException ex) when (ex is CannotEstablishConnectionException || ex is ConnectionClosedException || ex is RetriesLimitReachedException)
                {
                    throw LogAndReturnException(context.Logger.Error, new ServiceConnectionException("EventStore", ex));
                }
                catch (EventStoreConnectionException ex) when (ex is OperationTimedOutException)
                {
                    throw LogAndReturnException(context.Logger.Error, new ServiceTimeoutException("EventStore", ex));
                }
                catch (EventStoreConnectionException ex) when (ex is ClusterException || ex is ServerErrorException)
                {
                    throw LogAndReturnException(context.Logger.Error, new InternalServiceException("EventStore", ex));
                }
                catch (EventStoreConnectionException ex)
                {
                    throw LogAndReturnException(context.Logger.Error, new InternalServiceException("EventStore", ex));
                }
                catch (DataNotFoundException ex)
                {
                    throw LogAndReturnException(context.Logger.Debug, ex);
                }
                catch
                {
                    throw;
                }
            }, $"{nameof(EventStoreClient)}{contextSuffix}");
        }

        private IEventStoreConnection Connect()
        {
            if (_config == null)
            {
                throw new InvalidOperationException("No EventStore config found!");
            }

            if (string.IsNullOrEmpty(_config.ConnectionString))
            {
                throw new InvalidOperationException("Invalid EventStore config!");
            }

            var connection = EventStoreConnection.Create(_config.ConnectionString);
            connection.AuthenticationFailed += Connection_AuthenticationFailed;
            connection.Closed += Connection_Closed;
            connection.Connected += Connection_Connected;
            connection.Disconnected += Connection_Disconnected;
            connection.ErrorOccurred += Connection_ErrorOccurred;

            connection.ConnectAsync().Wait();

            return connection;
        }

        private string SanitizedConnectionString
        {
            get
            {
                var connString = _config?.ConnectionString;

                if (connString == null)
                {
                    return connString;
                }

                var connectTo = connString
                    .Split(';')
                    .Select(val => val.Trim())
                    .FirstOrDefault(val => val.StartsWith("ConnectTo="));

                if (connectTo == null || connectTo.IndexOf("@") < 0 || connectTo.Count(c => c == ':') < 2)
                {
                    return connString;
                }

                var parts = connString.Split("@", StringSplitOptions.None);
                parts[0] = parts[0].Substring(0, parts[0].LastIndexOf(':'));

                var sanitized = string.Join('@', parts);

                return sanitized;
            }
        }

        private void Connection_AuthenticationFailed(object sender, ClientAuthenticationFailedEventArgs e)
        {
            _logger.LogError($"The event store connection was unable to authenticate! Connection: {SanitizedConnectionString} Reason: {e.Reason}");
        }

        private void Connection_Closed(object sender, ClientClosedEventArgs e)
        {
            _logger.LogTrace($"The event store connection was closed. Connection: {SanitizedConnectionString} Reason: {e.Reason}");
        }

        private void Connection_Connected(object sender, ClientConnectionEventArgs e)
        {
            _logger.LogTrace($"The event store connection was connected. Connection: {SanitizedConnectionString}");
        }

        private void Connection_Disconnected(object sender, ClientConnectionEventArgs e)
        {
            _logger.LogTrace($"The event store connection was disconnected. Connection: {SanitizedConnectionString}");
        }

        private void Connection_ErrorOccurred(object sender, ClientErrorEventArgs e)
        {
            _logger.LogError($"An error has occured while trying to commmunicate with the event store! Connection: {SanitizedConnectionString} Error: {e.Exception.Message}");
        }

        private void Connection_Reconnecting(object sender, ClientReconnectingEventArgs e)
        {
            _logger.LogError($"The event store connection is reconnecting. Connection: {SanitizedConnectionString}");
        }

        private Exception LogAndReturnException(Action<string> logMethod, Exception ex)
        {
            logMethod?.Invoke(ex.Message);

            return ex;
        }
    }
}
