﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvest.Api
{
    public class Contact : BaseModel
    {
        public IdNameModel Client {get; set;}
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneOffice { get; set; }
        public string PhoneMobile { get; set; }
        public string Fax { get; set; }
    }

    public class ContactsResponse : PagedList
    {
        public Contact[] Contacts { get; set; }
    }
}
