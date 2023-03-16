using System;
using Azure;
using Azure.Data.Tables;

namespace EON.Models
{
  public class ExpiringLinkTableEntity : ITableEntity
  {
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string Url { get; set; }
    public DateTime Expiration { get; set; }
    public bool ExpiresOnAccess { get; set; }
    public string ExpiredRedirectUrl { get; set; }
  }
}