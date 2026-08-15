namespace CodeHub.Core.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Database.Interfaces;
    using CodeHub.Core.Models;
    using CodeHub.Core.Requests;
    using CodeHub.Core.Responses;

    /// <summary>
    /// SQLite implementation of request-history data access.
    /// </summary>
    public class SqliteRequestHistoryMethods : IRequestHistoryMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteRequestHistoryMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task CreateAsync(RequestHistoryEntry entry, CancellationToken token = default)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            await _Db.ExecuteQueryAsync(
                "INSERT INTO request_history (id, method, path, url, statuscode, durationms, sourceip, requestheaders, requestbody, requestbodybytes, requestbodytruncated, responseheaders, responsebody, responsebodybytes, responsebodytruncated, createdutc, completedutc) VALUES (" +
                Sanitizer.Quote(entry.Id) + ", " +
                Sanitizer.Quote(entry.Method) + ", " +
                Sanitizer.Quote(entry.Path) + ", " +
                Sanitizer.Quote(entry.Url) + ", " +
                entry.StatusCode + ", " +
                entry.DurationMs.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " +
                Sanitizer.Quote(entry.SourceIp) + ", " +
                Sanitizer.Quote(JsonSerializer.Serialize(entry.RequestHeaders)) + ", " +
                Sanitizer.Quote(entry.RequestBody) + ", " +
                entry.RequestBodyBytes + ", " +
                Sanitizer.Bool(entry.RequestBodyTruncated) + ", " +
                Sanitizer.Quote(JsonSerializer.Serialize(entry.ResponseHeaders)) + ", " +
                Sanitizer.Quote(entry.ResponseBody) + ", " +
                entry.ResponseBodyBytes + ", " +
                Sanitizer.Bool(entry.ResponseBodyTruncated) + ", " +
                Sanitizer.Timestamp(entry.CreatedUtc) + ", " +
                Sanitizer.Timestamp(entry.CompletedUtc) + ");",
                false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<RequestHistoryEntry> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM request_history WHERE id=" + Sanitizer.Quote(id) + ";", false, token).ConfigureAwait(false);
            if (table.Rows.Count == 0) return null;
            return FromRow(table.Rows[0], true);
        }

        /// <inheritdoc />
        public async Task<RequestHistoryPage> EnumerateAsync(RequestHistoryFilter filter, CancellationToken token = default)
        {
            if (filter == null) filter = new RequestHistoryFilter();
            string where = BuildWhere(filter);

            DataTable countTable = await _Db.ExecuteQueryAsync(
                "SELECT COUNT(*) AS cnt FROM request_history " + where + ";", false, token).ConfigureAwait(false);
            int total = countTable.Rows.Count > 0 ? countTable.Rows[0].GetInt("cnt") : 0;

            int offset = (filter.PageNumber - 1) * filter.PageSize;
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT id, method, path, url, statuscode, durationms, sourceip, createdutc, completedutc FROM request_history " +
                where + " ORDER BY createdutc DESC LIMIT " + filter.PageSize + " OFFSET " + offset + ";",
                false, token).ConfigureAwait(false);

            RequestHistoryPage page = new RequestHistoryPage
            {
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = total
            };
            foreach (DataRow row in table.Rows) page.Items.Add(FromRow(row, false));
            return page;
        }

        /// <inheritdoc />
        public async Task<RequestHistorySummary> SummarizeAsync(RequestHistoryFilter filter, CancellationToken token = default)
        {
            if (filter == null) filter = new RequestHistoryFilter();
            DateTime from = (filter.FromUtc ?? DateTime.UtcNow.AddDays(-1)).ToUniversalTime();
            DateTime to = (filter.ToUtc ?? DateTime.UtcNow).ToUniversalTime();
            int bucketMinutes = filter.BucketMinutes;

            RequestHistorySummary summary = new RequestHistorySummary();

            List<RequestHistoryBucket> buckets = new List<RequestHistoryBucket>();
            for (DateTime start = from; start < to; start = start.AddMinutes(bucketMinutes))
            {
                buckets.Add(new RequestHistoryBucket
                {
                    BucketStartUtc = start,
                    BucketEndUtc = start.AddMinutes(bucketMinutes)
                });
            }

            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT statuscode, durationms, createdutc FROM request_history WHERE createdutc >= " +
                Sanitizer.Timestamp(from) + " AND createdutc < " + Sanitizer.Timestamp(to) + ";",
                false, token).ConfigureAwait(false);

            double totalDuration = 0;
            List<double> bucketDurations = new List<double>();
            for (int i = 0; i < buckets.Count; i++) bucketDurations.Add(0);

            foreach (DataRow row in table.Rows)
            {
                int status = row.GetInt("statuscode");
                double duration = row.GetDouble("durationms");
                DateTime created = row.GetDateTimeRequired("createdutc");
                bool success = status < 400;

                summary.TotalCount++;
                if (success) summary.TotalSuccess++;
                else summary.TotalFailure++;
                totalDuration += duration;

                int index = (int)((created - from).TotalMinutes / bucketMinutes);
                if (index >= 0 && index < buckets.Count)
                {
                    if (success) buckets[index].SuccessCount++;
                    else buckets[index].FailureCount++;
                    bucketDurations[index] += duration;
                }
            }

            for (int i = 0; i < buckets.Count; i++)
            {
                int count = buckets[i].SuccessCount + buckets[i].FailureCount;
                buckets[i].AverageDurationMs = count > 0 ? bucketDurations[i] / count : 0;
            }

            summary.AverageDurationMs = summary.TotalCount > 0 ? totalDuration / summary.TotalCount : 0;
            summary.Buckets = buckets;
            return summary;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            await _Db.ExecuteQueryAsync(
                "DELETE FROM request_history WHERE id=" + Sanitizer.Quote(id) + ";", false, token).ConfigureAwait(false);
            return true;
        }

        /// <inheritdoc />
        public async Task<int> PruneAsync(DateTime olderThanUtc, CancellationToken token = default)
        {
            await _Db.ExecuteQueryAsync(
                "DELETE FROM request_history WHERE createdutc < " + Sanitizer.Timestamp(olderThanUtc) + ";",
                false, token).ConfigureAwait(false);
            return 0;
        }

        #endregion

        #region Private-Methods

        private static string BuildWhere(RequestHistoryFilter filter)
        {
            List<string> clauses = new List<string>();
            if (!String.IsNullOrEmpty(filter.Method)) clauses.Add("method=" + Sanitizer.Quote(filter.Method.ToUpperInvariant()));
            if (filter.StatusCode.HasValue) clauses.Add("statuscode=" + filter.StatusCode.Value);
            if (!String.IsNullOrEmpty(filter.PathContains)) clauses.Add("path LIKE " + Sanitizer.Quote("%" + filter.PathContains + "%"));
            if (filter.FromUtc.HasValue) clauses.Add("createdutc >= " + Sanitizer.Timestamp(filter.FromUtc.Value));
            if (filter.ToUtc.HasValue) clauses.Add("createdutc <= " + Sanitizer.Timestamp(filter.ToUtc.Value));
            if (clauses.Count == 0) return String.Empty;
            return "WHERE " + String.Join(" AND ", clauses);
        }

        private static RequestHistoryEntry FromRow(DataRow row, bool includeBodies)
        {
            RequestHistoryEntry entry = new RequestHistoryEntry
            {
                Id = row.GetString("id"),
                Method = row.GetString("method"),
                Path = row.GetString("path"),
                Url = row.GetString("url"),
                StatusCode = row.GetInt("statuscode"),
                DurationMs = row.GetDouble("durationms"),
                SourceIp = row.GetString("sourceip"),
                CreatedUtc = row.GetDateTimeRequired("createdutc"),
                CompletedUtc = row.GetDateTime("completedutc")
            };

            if (includeBodies)
            {
                entry.RequestHeaders = DeserializeHeaders(row.GetString("requestheaders"));
                entry.RequestBody = row.GetString("requestbody");
                entry.RequestBodyBytes = row.GetLong("requestbodybytes");
                entry.RequestBodyTruncated = row.GetBool("requestbodytruncated");
                entry.ResponseHeaders = DeserializeHeaders(row.GetString("responseheaders"));
                entry.ResponseBody = row.GetString("responsebody");
                entry.ResponseBodyBytes = row.GetLong("responsebodybytes");
                entry.ResponseBodyTruncated = row.GetBool("responsebodytruncated");
            }

            return entry;
        }

        private static Dictionary<string, string> DeserializeHeaders(string json)
        {
            if (String.IsNullOrEmpty(json)) return new Dictionary<string, string>();
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }
        }

        #endregion
    }
}
