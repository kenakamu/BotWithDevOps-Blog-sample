using Microsoft.Bot.Builder.FormFlow;
using System;

namespace O365Bot.Models
{
    [Serializable]
    public class OutlookEvent
    {
        [Prompt("What is the title?")]
        public string Subject { get; set; }
        [Prompt("What is the detail?")]
        public string Description { get; set; }
        [Prompt("When do you start? Use dd/MM/yyyy HH:mm format.")]
        public DateTime Start { get; set; }
        [Prompt("Is this all day event?{||}")]
        public bool IsAllDay { get; set; }
        [Prompt("How many hours?", "Please answer by number")]
        public double Hours { get; set; }
    }
}