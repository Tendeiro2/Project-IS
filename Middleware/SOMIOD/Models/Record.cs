using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml.Linq;

namespace SOMIOD.Models
{
    public class Record
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public DateTime CreationDateTime { get; set; }
        public int Parent { get; set; }

        public string ToString(int evento)
        {
            var notificationXml = new XElement("Notification",  
                new XElement("Record",
                    new XElement("Id", Id),
                    new XElement("Name", Name),
                    new XElement("Content", Content),
                    new XElement("CreationDateTime", CreationDateTime),
                    new XElement("Parent", Parent)
                ),
                new XElement("Event", evento)  
            );

            return notificationXml.ToString(); 
        }
    }
}