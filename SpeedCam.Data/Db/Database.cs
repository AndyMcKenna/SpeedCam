using Dapper;
using SpeedCam.Data.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace SpeedCam.Data.Db
{
    public class Database : IDatabase
    {
        private string ConnectionString;

        public Database(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public void EntryInsert(Entry entry)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var identity = connection.Query<int>(@"INSERT INTO Entry (DateAdded, Direction, Speed) VALUES (@dateAdded, @direction, @speed); SELECT CAST(SCOPE_IDENTITY() as INT)", entry).First();
                entry.Id = identity;
            }
        }

        public void EntryDelete(int id)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                connection.Execute("DELETE FROM Entry WHERE Id = @id", new { id });
            }
        }

        public IEnumerable<Entry> EntrySearch(DateTime start, DateTime end, decimal minimumSpeed, decimal maximumSpeed, int limit = 10)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                return connection.Query<Entry>(@"SELECT * FROM Entry WHERE DateAdded BETWEEN @start AND @end AND Speed BETWEEN @minimumSpeed AND @maximumSpeed", new { start, end, minimumSpeed, maximumSpeed});
            }
        }

        public Config GetConfig()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                return connection.QueryFirst<Config>("SELECT * FROM Config");
            }
        }

        public void LogInsert(Log log)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var identity = connection.Query<int>(@"INSERT INTO Log (DateAdded, Message, StackTrace) VALUES (@dateAdded, @message, @stackTrace); SELECT CAST(SCOPE_IDENTITY() as INT)", log).First();
                log.Id = identity;
            }
        }

        public DateChunk GetNextDateChunk()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var exportingCount = connection.QueryFirst<int>(@"SELECT COUNT(*) FROM DateChunk WHERE ExportDone = 0");
                if (exportingCount > 0)
                    return null;

                var latest = connection.QueryFirst<DateChunk>(@"SELECT * FROM DateChunk ORDER BY StartDate DESC");
                var nextChunk = new DateChunk
                {
                    StartDate = latest.StartDate.AddMinutes(latest.LengthMinutes),
                    ProcessingTime = 0,
                    LengthMinutes = 0,
                    ExportDone = false
                };

                return nextChunk;
            }
        }

        public void UpdateDateChunk(DateChunk chunk)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                connection.Execute("UPDATE DateChunk SET ProcessingTime = @processingTime, LengthMinutes = @lengthMinutes, ExportDone = @exportDone, DateProcessed = @dateProcessed WHERE Id = @id", chunk);
            }
        }

        public void InsertDateChunk(DateChunk chunk)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var id = connection.QueryFirst<int>("INSERT INTO DateChunk (StartDate, ProcessingTime, LengthMinutes, ExportDone, DateProcessed) VALUES (@startDate, @processingTime, @lengthMinutes, @exportDone, @dateProcessed);SELECT CAST(SCOPE_IDENTITY() as INT)", chunk);
                chunk.Id = id;
            }
        }

        public MakeUp GetNextMakeUp()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var results = connection.Query<MakeUp>("SELECT * FROM MakeUp WHERE InProgress = 0 ORDER BY StartDate");
                return results.FirstOrDefault();
            }
        }

        public void InsertMakeUp(MakeUp makeUp)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var id = connection.QueryFirst("INSERT INTO MakeUp (StartDate, LengthMinutes, InProgress) VALUES (@startDate, @lengthMinutes, @inProgress);SELECT CAST(SCOPE_IDENTITY() as INT)", makeUp);
                makeUp.Id = id;
            }
        }

        public void UpdateMakeUp(MakeUp makeUp)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                connection.Execute("UPDATE MakeUp SET InProgress = @inProgress WHERE Id = @id", makeUp);
            }
        }

        public void DeleteMakeUp(MakeUp makeUp)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                connection.Execute("DELETE FROM MakeUp WHERE Id = @id", makeUp);
            }
        }
    }
}
