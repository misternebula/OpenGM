namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    // wrapper around c# datetime stuff that emulates JS Date stuff because im really lazy
    // TODO : get rid of this
    public class Date
    {
        private DateTime _utcDate = DateTime.Now;

        public int GetMilliseconds() => _utcDate.ToLocalTime().Millisecond;

        public long GetTime() => new DateTimeOffset(_utcDate).ToUnixTimeMilliseconds();

        public void SetTime(double ms)
        {
            _utcDate = DateTime.UnixEpoch;
            _utcDate = _utcDate.AddMilliseconds(ms);
        }

        public int GetFullYear() => _utcDate.ToLocalTime().Year;
        public int GetUTCFullYear() => _utcDate.Year;

        public int GetMonth() => _utcDate.ToLocalTime().Month - 1;
        public int GetUTCMonth() => _utcDate.Month - 1;

        public int GetDate() => _utcDate.ToLocalTime().Day;
        public int GetUTCDate() => _utcDate.Day;

        public int GetDay() => (int)_utcDate.ToLocalTime().DayOfWeek;
        public int GetUTCDay() => (int)_utcDate.DayOfWeek;

        public int GetHours() => _utcDate.ToLocalTime().Hour;
        public int GetUTCHours() => _utcDate.Hour;

        public int GetMinutes() => _utcDate.ToLocalTime().Minute;
        public int GetUTCMinutes() => _utcDate.Minute;

        public int GetSeconds() => _utcDate.ToLocalTime().Second;
        public int GetUTCSeconds() => _utcDate.Second;
    }
}
