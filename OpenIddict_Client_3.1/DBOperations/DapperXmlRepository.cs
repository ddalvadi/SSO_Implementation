using Dapper;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;

namespace OpenIddict_Client_3._1.DBOperations
{
    public class DapperXmlRepository : IXmlRepository
    {
        private readonly Func<IDbConnection> _connectionFactory;
        private readonly ILogger<DapperXmlRepository> _logger;

        public DapperXmlRepository(Func<IDbConnection> connectionFactory, ILogger<DapperXmlRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            using var conn = _connectionFactory();
            var xmls = conn.Query<string>("SELECT Xml FROM DataProtectionKeys");
            return xmls.Select(XElement.Parse).ToList();
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            var xml = element.ToString(SaveOptions.DisableFormatting);

            using var conn = _connectionFactory();
            conn.Execute(
                "INSERT INTO DataProtectionKeys (FriendlyName, Xml) VALUES (@FriendlyName, @Xml)",
                new { FriendlyName = friendlyName, Xml = xml });

            _logger.LogInformation("Stored data protection key: {FriendlyName}", friendlyName);
        }
    }
}
