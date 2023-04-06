namespace xxTalker.Shared.Models
{
    public interface IDataAccessProvider
    {
        Task<Talker?> GetTalkerAsync(string accountId);
        Task<List<Talker>> GetTalkersAsync();
        Task<List<Talker>> GetLastActiveTalkersAsync(int takeNum);
        Task DeleteTalkerAsync(string accountId);


        Task AddMessageAsync(TalkerMessage talkerMessage);
        Task UpdateMessageAsync(TalkerMessage talkerMessage);
        Task<TalkerMessage?> GetMessageAsync(int messageId);
        Task<List<TalkerMessage>> GetMessagesAsync();
        Task<TalkerMessage?> GetLastSenderMessageAsync(TalkerMessage message);
    }
}
