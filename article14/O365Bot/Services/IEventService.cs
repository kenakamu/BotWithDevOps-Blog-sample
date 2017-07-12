using Microsoft.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace O365Bot.Services
{
    public interface IEventService
    {
        Task<List<Event>> GetEvents();
        Task CreateEvent(Event @event);
        Task<Event> GetEvent(string id);
    }
}