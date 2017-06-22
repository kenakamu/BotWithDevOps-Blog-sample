using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace O365Bot.Services
{
    public interface IEventService
    {
        Task<List<Event>> GetEvents();
    }
}