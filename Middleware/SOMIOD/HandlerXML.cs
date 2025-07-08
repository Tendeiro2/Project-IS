using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.ComponentModel.DataAnnotations;
using System.IO;


namespace SOMIOD
{
    public class HandlerXML
    {
        #region XML Validation
        string xmlFilePath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Middleware", "SOMIOD", "XMLValidator.xsd"); 
        string validationMessage = "Valid";

        public string ValidateXML(XElement xmlElement)
        {
            XmlDocument doc = new XmlDocument();

            if (xmlElement == null)
            {
                validationMessage = "ERROR: XML can't be null.";
                return validationMessage;
            }

            try
            {
                using (System.IO.StringReader sr = new System.IO.StringReader(xmlElement.ToString()))
                {
                    doc.LoadXml(sr.ReadToEnd());
                }

                doc.Schemas.Add(null, xmlFilePath);

                doc.Validate(ValidationEventHandler);
            }
            catch (XmlException ex)
            {
                validationMessage = $"ERROR: {ex.ToString()}";
            }
            catch (XmlSchemaValidationException ex)
            {
                validationMessage = $"SCHEMA VALIDATION ERROR: {ex.Message}";
            }
            catch (Exception ex)
            {
                validationMessage = $"Unexpected error: {ex.Message}";
            }

            return validationMessage;
        }

        private void ValidationEventHandler(object sender, ValidationEventArgs args)
        {
            switch (args.Severity)
            {
                case XmlSeverityType.Error:
                    validationMessage = $"ERROR: {args.Message}";
                    break;
                case XmlSeverityType.Warning:
                    validationMessage = $"WARNING: {args.Message}";
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region XML Responses
        public static XmlDocument responseApplications(List<Application> applicationList)
        {
            var xmlDoc = new XmlDocument();

            var applicationsNode = xmlDoc.CreateElement("Applications");
            xmlDoc.AppendChild(applicationsNode);

            foreach (var app in applicationList)
            {
                var applicationNode = xmlDoc.CreateElement("Application");

                var nameNode = xmlDoc.CreateElement("Name");
                nameNode.InnerText = app.Name;

                applicationNode.AppendChild(nameNode);
                applicationsNode.AppendChild(applicationNode);
            }

            return xmlDoc;
        }

        public static XmlDocument responseContainers(List<string> names, string rootElementName)
        {
            var xmlDoc = new XmlDocument();
            var applicationNode = xmlDoc.CreateElement("Application");

            foreach (var name in names)
            {
                var rootElement = xmlDoc.CreateElement(rootElementName);
                applicationNode.AppendChild(rootElement);
                var nameElement = xmlDoc.CreateElement("Name");
                nameElement.InnerText = name;
                rootElement.AppendChild(nameElement);

            }

            xmlDoc.AppendChild(applicationNode);
            return xmlDoc;
        }

        public static XmlDocument responseApplication(Application application)
        {
            var xmlDoc = new XmlDocument();

            var applicationNode = xmlDoc.CreateElement("Application");

            var idNode = xmlDoc.CreateElement("Id");
            idNode.InnerText = application.Id.ToString();
            applicationNode.AppendChild(idNode);

            var nameNode = xmlDoc.CreateElement("Name");
            nameNode.InnerText = application.Name;
            applicationNode.AppendChild(nameNode);

            var creationDateNode = xmlDoc.CreateElement("CreationDateTime");
            creationDateNode.InnerText = application.CreationDateTime.ToString("o");
            applicationNode.AppendChild(creationDateNode);

            xmlDoc.AppendChild(applicationNode);

            return xmlDoc;
        }

        public static XmlDocument responseContainer(Container container)
        {
            var xmlDoc = new XmlDocument();

            var containerNode = xmlDoc.CreateElement("Container");

            var containerIdNode = xmlDoc.CreateElement("Id");
            containerIdNode.InnerText = container.Id.ToString();
            containerNode.AppendChild(containerIdNode);

            var containerNameNode = xmlDoc.CreateElement("Name");
            containerNameNode.InnerText = container.Name;
            containerNode.AppendChild(containerNameNode);

            var containerCreationDateNode = xmlDoc.CreateElement("CreationDateTime");
            containerCreationDateNode.InnerText = container.CreationDateTime.ToString("o");
            containerNode.AppendChild(containerCreationDateNode);

            var containerParentNode = xmlDoc.CreateElement("Parent");
            containerParentNode.InnerText = container.Parent.ToString();
            containerNode.AppendChild(containerParentNode);

            xmlDoc.AppendChild(containerNode);

            return xmlDoc;
        }

        public static XmlDocument responseRecord(Record record)
        {
            var xmlDoc = new XmlDocument();

            var recordNode = xmlDoc.CreateElement("Record");

            var recordIdNode = xmlDoc.CreateElement("Id");
            recordIdNode.InnerText = record.Id.ToString();
            recordNode.AppendChild(recordIdNode);

            var recordNameNode = xmlDoc.CreateElement("Name");
            recordNameNode.InnerText = record.Name;
            recordNode.AppendChild(recordNameNode);

            var recordContentNode = xmlDoc.CreateElement("Content");
            recordContentNode.InnerText = record.Content;
            recordNode.AppendChild(recordContentNode);

            var recordCreationDateNode = xmlDoc.CreateElement("CreationDateTime");
            recordCreationDateNode.InnerText = record.CreationDateTime.ToString("o");
            recordNode.AppendChild(recordCreationDateNode);

            var recordParentNode = xmlDoc.CreateElement("Parent");
            recordParentNode.InnerText = record.Parent.ToString();
            recordNode.AppendChild(recordParentNode);

            xmlDoc.AppendChild(recordNode);

            return xmlDoc;
        }

        public static XmlDocument responseNotification(Notification notification)
        {
            var xmlDoc = new XmlDocument();

            var notificationNode = xmlDoc.CreateElement("Notification");

            var notificationIdNode = xmlDoc.CreateElement("Id");
            notificationIdNode.InnerText = notification.Id.ToString();
            notificationNode.AppendChild(notificationIdNode);

            var notificationNameNode = xmlDoc.CreateElement("Name");
            notificationNameNode.InnerText = notification.Name;
            notificationNode.AppendChild(notificationNameNode);

            var notificationEventNode = xmlDoc.CreateElement("Event");
            notificationEventNode.InnerText = notification.Event.ToString();
            notificationNode.AppendChild(notificationEventNode);

            var notificationEndpointNode = xmlDoc.CreateElement("Endpoint");
            notificationEndpointNode.InnerText = notification.Endpoint;
            notificationNode.AppendChild(notificationEndpointNode);

            var notificationEnabledNode = xmlDoc.CreateElement("Enabled");
            notificationEnabledNode.InnerText = notification.Enabled.ToString();
            notificationNode.AppendChild(notificationEnabledNode);

            var notificationParentNode = xmlDoc.CreateElement("Parent");
            notificationParentNode.InnerText = notification.Parent.ToString();
            notificationNode.AppendChild(notificationParentNode);

            var notificationCreationDateTimeNode = xmlDoc.CreateElement("CreationDateTime");
            notificationCreationDateTimeNode.InnerText = notification.CreationDateTime.ToString("o");
            notificationNode.AppendChild(notificationCreationDateTimeNode);

            xmlDoc.AppendChild(notificationNode);

            return xmlDoc;
        }

        public static XmlDocument responseParentsHierarchy(string applicationName, string containerName)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement applicationNode = xmlDoc.CreateElement("Application");
            xmlDoc.AppendChild(applicationNode);

            XmlElement applicationNameNode = xmlDoc.CreateElement("Name");
            applicationNameNode.InnerText = applicationName;
            applicationNode.AppendChild(applicationNameNode);

            if (!string.IsNullOrEmpty(containerName))
            {
                XmlElement containerNode = xmlDoc.CreateElement("Container");

                XmlElement containerNameNode = xmlDoc.CreateElement("Name");
                containerNameNode.InnerText = containerName;
                containerNode.AppendChild(containerNameNode);

                applicationNode.AppendChild(containerNode);
            }

            return xmlDoc;
        }

        #endregion

        #region Error XML Responses
        public static XmlDocument responseError(string message, string errorCode)
        {
            var xmlDoc = new XmlDocument();

            var errorNode = xmlDoc.CreateElement("Error");

            var errorMessageNode = xmlDoc.CreateElement("Message");
            errorMessageNode.InnerText = message;
            errorNode.AppendChild(errorMessageNode);

            var errorCodeNode = xmlDoc.CreateElement("ErrorCode");
            errorCodeNode.InnerText = errorCode;
            errorNode.AppendChild(errorCodeNode);

            xmlDoc.AppendChild(errorNode);

            return xmlDoc;
        }
        #endregion
    }
}