using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace multicorp_bot.Helpers
{
    
    /// <summary>
    /// A class used to log telemety into Azure Application Insights.  It creates pretty charts and can log most things.
    /// This class is provisioned in the startup as a singleton object, then reference throughout the app.
    /// </summary>
    public class TelemetryHelper
    {
        private const string DEFAULT_INSTRUMENT_KEY = "f3552a4b-4698-4742-9ec9-044fd132d10a";
        private TelemetryConfiguration _config = null;
        private TelemetryClient _client = null;
        private string _environment = string.Empty;
        private static readonly TelemetryHelper _helper = new TelemetryHelper();
        public static TelemetryHelper Singleton => _helper;

        public TelemetryHelper()
        {
            try
            {
                // Gets the key from the environment variables.
                string key = Environment.GetEnvironmentVariable("TelemetryAppKey");
                if (string.IsNullOrEmpty(key))
                {
                    key = DEFAULT_INSTRUMENT_KEY;
                }

                // Create the telemetry configuration
                TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
                configuration.InstrumentationKey = key;
                this._config = configuration;

                // Create the environment property we'll read later, to correlate logs.
                string env = Environment.GetEnvironmentVariable("TelemetryAppKey");
                if (string.IsNullOrEmpty(env))
                {
                    env = "DEV";
                }
                this._environment = Environment.GetEnvironmentVariable("TelemetryENV");

                // Creates the telemetry client, which needs to be a singleton, so this whole class is.
                var telemetryClient = new TelemetryClient(this._config);
                this._client = telemetryClient;
                this._client.TrackTrace("Telemetry Loaded");
                this._client.Flush();
            }
            catch(Exception ex)
            {
                // don't log stuff, just silently error out.
                throw ex;
            }
        }

        /// <summary>
        /// Logs an event with name and description.
        /// </summary>
        /// <param name="eventName">examples: multibot-help, !Bank, etc.</param>
        /// /// <param name="member">Sendin a DiscordMember class object, otherwise null.</param>
        public void LogEvent(string eventName, string description = "", CommandContext command = null, DiscordMember member = null, Dictionary<string, int> metrics = null)
        {
            try
            {
                EventTelemetry tel = new EventTelemetry(eventName);
                tel.Properties.Add("Description", description);
                tel.Properties.Add("Environment", this._environment);
                if(metrics != null)
                {
                    foreach(KeyValuePair<string,int> pair in metrics)
                    {
                        tel.Metrics.Add(pair.Key, pair.Value);
                    }
                }
                if(member != null)
                {
                    tel.Properties.Add("Discord-Other-Username", member.Username);
                    tel.Properties.Add("Discord-Other-Display-Name", member.DisplayName);
                }
                if (command != null)
                {
                    tel.Properties.Add("Discord-Username", command.User.Username);
                    tel.Properties.Add("Discord-Display-Name", command.User.Email);
                    tel.Properties.Add("Discord-Guild", command.Guild.Name);
                    tel.Properties.Add("Discord-Client-Name", command.Client.CurrentApplication.Name);
                }
                this._client.TrackEvent(tel);
                this._client.Flush();
            }
            catch(Exception ex)
            {
                LogException(eventName, ex);
            }
        }

        public void LogTrace(string message, SeverityLevel level = SeverityLevel.Verbose)
        {
            try
            {
                TraceTelemetry tel = new TraceTelemetry(message, level);
                tel.Properties.Add("Environment", this._environment);
                this._client.TrackTrace(tel);
                this._client.Flush();
            }
            catch (Exception ex)
            {
                LogException(message, ex);
            }
        }

        public void LogException(string message, Exception exTrace, SeverityLevel level = SeverityLevel.Error)
        {
            ExceptionTelemetry tel = new ExceptionTelemetry(exTrace);
            tel.Properties.Add("Environment", this._environment);
            tel.Properties.Add("Message", message);
            tel.SeverityLevel = level;
            this._client.TrackException(tel);
            this._client.Flush();
        }

    }

}
