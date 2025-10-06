using System;
using System.ComponentModel.DataAnnotations;

namespace Rnwood.Smtp4dev.DbModel
{
    public class UserVersionInfo
    {
        [Key]
        public Guid Id { get; set; }

        public string Username { get; set; }

        public string LastSeenVersion { get; set; }

        public DateTime LastCheckedDate { get; set; }

        public bool WhatsNewDismissed { get; set; }

        public bool UpdateNotificationDismissed { get; set; }
    }
}
