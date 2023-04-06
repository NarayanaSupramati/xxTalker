using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using xxTalker.Shared.Models;

namespace xxTalker.Shared.DataAccess
{
    public class DataAccessPostgreSqlProvider : IDataAccessProvider
    {
        private readonly DomainModelPostgreSqlContext _context;

        public DataAccessPostgreSqlProvider(DomainModelPostgreSqlContext context)
        {
            _context = context;
        }

        #region "Talker"

        public async Task<Talker?> GetTalkerAsync(string accountId)
        {
            return await _context.Talkers.Include(i => i.Messages).FirstOrDefaultAsync(t => t.AccountId == accountId);
        }

        public async Task<List<Talker>> GetTalkersAsync()
        {
            return await _context.Talkers.ToListAsync();
        }

        public async Task<List<Talker>> GetLastActiveTalkersAsync(int takeNum)
        {
            var uniqueTalkers = await _context.Messages.GroupBy(m => new { m.ReceiverAccount }).Select(g => new
            {
                ReceiverAccount = g.Key.ReceiverAccount,
                LastDate = g.Max(row => row.MessageDate)
            }).OrderByDescending(o => o.LastDate).Take(takeNum).Select(s => s.ReceiverAccount).ToListAsync();

            var lastTalkers = await _context.Talkers.Where(w => uniqueTalkers.Contains(w.AccountId)).Include(i => i.Messages).ToListAsync();
            lastTalkers = lastTalkers.OrderBy(o => uniqueTalkers.IndexOf(o.AccountId)).ToList();
            return lastTalkers;
        }

        public async Task DeleteTalkerAsync(string accountId)
        {
            var entity = await _context.Messages.Where(t => t.ReceiverAccount == accountId).ToListAsync();
            _context.Messages.RemoveRange(entity);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region "TalkerMessage"

        public async Task<TalkerMessage?> GetMessageAsync(int messageId)
        {
            return await _context.Messages.FirstOrDefaultAsync(t => t.MessageId == messageId);
        }

        public async Task<List<TalkerMessage>> GetMessagesAsync()
        {
            return await _context.Messages.ToListAsync();
        }

        public async Task AddMessageAsync(TalkerMessage talkerMessage)
        {
            _context.Messages.Add(talkerMessage);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMessageAsync(TalkerMessage talkerMessage)
        {
            _context.Messages.Update(talkerMessage);
            await _context.SaveChangesAsync();
        }

        //spec
        public async Task<TalkerMessage?> GetLastSenderMessageAsync(TalkerMessage talkerMessage)
        {
            return await _context.Messages.Where(w => w.SenderAccount == talkerMessage.SenderAccount && w.ReceiverAccount == talkerMessage.ReceiverAccount).OrderByDescending(o => o.MessageDate).FirstOrDefaultAsync();
        }


        #endregion

    }
}
