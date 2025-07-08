using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace SOMIOD.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreationDateTime { get; set; }
        public int Parent { get; set; }
        public int Event { get; set; }
        public string Endpoint { get; set; }
        public bool Enabled { get; set; }


        public override string ToString()
        {
            var notificationXml = new XElement("Notification",
                new XElement("Name", Name),
                new XElement("Event", Event),
                new XElement("Endpoint", Endpoint),
                new XElement("Enabled", Enabled)
            );

            return notificationXml.ToString();
        }

    }
}