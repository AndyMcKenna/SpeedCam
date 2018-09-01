using SpeedCam.Data.Entities;
using System;
using System.Collections.Generic;

namespace SpeedCam.Data.Db
{
    public interface IDatabase
    {
        void EntryInsert(Entry entry);
        void EntryDelete(int id);
        IEnumerable<Entry> EntrySearch(DateTime start, DateTime end, decimal minimumSpeed,  decimal maximumSpeed, int limit = 10);

        Config GetConfig();

        void LogInsert(Log log);

        DateChunk GetNextDateChunk();
        void UpdateDateChunk(DateChunk chunk);
        void InsertDateChunk(DateChunk chunk);

        MakeUp GetNextMakeUp();
        void InsertMakeUp(MakeUp makeUp);
        void UpdateMakeUp(MakeUp makeUp);
        void DeleteMakeUp(MakeUp makeUp);
    }
}
