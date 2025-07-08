using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Results;
using System.Xml.Linq;
using SOMIOD.Models;
using static System.Net.Mime.MediaTypeNames;
using Application = SOMIOD.Models.Application;
using Container = SOMIOD.Models.Container;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Configuration;
using System.Net.Http;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using System.Web.UI.WebControls;
using System.Resources;
using System.Threading.Tasks;
using Swashbuckle.Swagger.Annotations;

namespace SOMIOD.Controllers
{
    public class SOMIODController : ApiController
    {
        string strConnection = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIOD.Properties.Settings.ConnectionToDB"].ConnectionString;

        public class ValidateResource
        {
            public Application Application { get; set; }
            public Container Container { get; set; }
            public Object RecordOrNotification { get; set; }

            public string ErrorMessage { get; set; }
            public HttpStatusCode ErrorCode { get; set; }

            public ValidateResource(Application application, Container container, Object recordOrNotification,  string errorMessage, HttpStatusCode errorCode)
            {
                Application = application;
                Container = container;
                RecordOrNotification = recordOrNotification;
                ErrorMessage = errorMessage;
                ErrorCode = errorCode;
            }
        }


        #region Locate

        //-------------------------------------------------------------------------------------
        //--------------------------------------- Locate --------------------------------------
        //------------------------------------------------------------------------------------- 
        [HttpGet]
        [System.Web.Http.Description.ApiExplorerSettings(IgnoreApi = true)] //Remover do swagger
        [Route("api/somiod")]
        public IHttpActionResult LocateApplications()
        {
            List<Application> applicationList = new List<Application>();
            IEnumerable<string> headerValues;
            string headerValue = null;
            SqlConnection conn = null;
            SqlDataReader reader = null;
            SqlCommand command = null;

            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault()?.ToUpper();
            }

            if (string.IsNullOrEmpty(headerValue))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("The header value is required (somiod-locate).", "400"), Configuration.Formatters.XmlFormatter);
            }

            if (headerValue != "APPLICATION")
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("The header value is invalid. Expected value 'Application'", "400"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand($"SELECT name FROM {headerValue}", conn);

                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    applicationList.Add(new Application
                    {
                        Name = (string)reader["Name"]
                    });
                }
            }
            catch 
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (reader != null) { reader.Close(); }
                if (command != null) { command.Dispose(); }
            }

            return Content(System.Net.HttpStatusCode.OK, HandlerXML.responseApplications(applicationList), Configuration.Formatters.XmlFormatter);
        }

        [HttpGet]
        [Route("api/somiod/{application}")]
        public IHttpActionResult LocateContainersRecordsNotifications(string application)
        {
            IEnumerable<string> headerValues;
            string headerValue = null;
            SqlConnection conn = null;
            SqlDataReader sqlReader = null;
            SqlCommand command = null;
            List<string> NamesList = null;

            Application applicationInfo = this.verifyApplicationExists(application);

            if (applicationInfo == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault()?.ToUpper();
            }

            if (string.IsNullOrEmpty(headerValue))
            {
                return Content(HttpStatusCode.OK, HandlerXML.responseApplication(applicationInfo), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                NamesList = new List<string>();

                string query = null;
                switch (headerValue)
                {
                    case "CONTAINER":
                        query = "SELECT Container.name FROM Container JOIN Application ON Container.parent = Application.id WHERE Application.name = @application";
                        break;

                    case "RECORD":
                        query = "SELECT Record.name FROM Record JOIN Container ON Record.parent = container.id JOIN application ON container.parent = application.id WHERE application.name = @application";
                        break;

                    case "NOTIFICATION":
                        query = "SELECT Notification.name FROM Notification JOIN Container ON Notification.parent = container.id JOIN application ON container.parent = application.id WHERE application.name = @application";
                        break;

                    default:
                        return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Invalid Header. Expected values are 'Container', 'Record', or 'Notification'.", "400"), Configuration.Formatters.XmlFormatter);
                }

                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand(query, conn);

                command.Parameters.AddWithValue("@application", application);

                sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    string name = sqlReader.GetString(0);
                    NamesList.Add(name);
                }
            }
            catch
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if(conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
                if (command != null) { command.Dispose(); }
            }

            headerValue = headerValue.ToLower(); 

            return Content(HttpStatusCode.OK, HandlerXML.responseContainers(NamesList, char.ToUpper(headerValue[0]) + headerValue.Substring(1)), Configuration.Formatters.XmlFormatter);
        }

        //Função Extra
        [HttpGet]
        [System.Web.Http.Description.ApiExplorerSettings(IgnoreApi = true)] //Remover do swagger
        [Route("api/somiod/{resourceName}/parent")]
        public IHttpActionResult getParent(string resourceName)
        {
            IEnumerable<string> headerValues;
            string headerValue = null;
            string query = null;

            int parentId = -1;
            Container container = null;

            SqlConnection conn = null;
            SqlDataReader reader = null;
            SqlCommand command = null;

            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault()?.ToUpper();
            }

            switch (headerValue)
            {
                case "APPLICATION":
                    return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Applications dosn't have parents.", "404"), Configuration.Formatters.XmlFormatter);

                case "CONTAINER":

                    container = this.verifyContainerExists(resourceName);
                    
                    if(container == null)
                    {
                        return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Container was not found.", "404"), Configuration.Formatters.XmlFormatter);
                    }

                    parentId = container.Parent;

                    query = "SELECT Application.Name AS ApplicationName FROM Application WHERE Application.Id = @parentId";

                    break;

                case "RECORD":
                case "NOTIFICATION":

                    object parentResource = headerValue == "RECORD"
                          ? (object)this.verifyRecordExists(resourceName)
                          : (object)this.verifyNotifcationExists(resourceName);

                    if (parentResource == null)
                    {
                        headerValue = headerValue == "RECORD" ? "Record" : "Notification";
                        return Content(HttpStatusCode.NotFound, HandlerXML.responseError($"{headerValue} was not found.", "404"), Configuration.Formatters.XmlFormatter);
                    }

                    parentId = parentResource is Record record ? record.Parent : ((Notification)parentResource).Parent;
                    
                    query = "SELECT Container.Name AS containerName, Application.Name AS ApplicationName FROM Container JOIN Application ON Container.Parent = Application.Id WHERE Container.Id = @parentId";

                    break;

                default:
                    return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Invalid Header. Expected values are 'Container', 'Record', or 'Notification'.", "400"), Configuration.Formatters.XmlFormatter);
            }


            string applicationName = null;
            string containerName = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand(query, conn);
                
                command.Parameters.AddWithValue("@parentId", parentId);

                reader = command.ExecuteReader();

                if (reader.Read())
                {
                    applicationName = reader["ApplicationName"].ToString();

                    if(headerValue != "CONTAINER")
                    {
                        containerName = reader["containerName"].ToString();
                    }
                }
            }
            catch
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (reader != null) { reader.Close(); }
                if (command != null) { command.Dispose(); }
            }

            return Content(HttpStatusCode.OK, HandlerXML.responseParentsHierarchy(applicationName, containerName), Configuration.Formatters.XmlFormatter);
        }

        #endregion


        #region Applications

        //-------------------------------------------------------------------------------------
        //------------------------------------ Applications -----------------------------------
        //------------------------------------------------------------------------------------- 
        [HttpPost]
        [Route("api/somiod")]
        public IHttpActionResult PostApplication([FromBody] XElement applicationXml)
        {
            SqlCommand command = null;
            SqlConnection conn = null;
            int affectedfRows = -1;
            string applicationName = null;

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(applicationXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"{validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                applicationName = this.verifyApplicationExists(applicationXml.Element("Name")?.Value) != null ? GenerateUniqueName("Application") : applicationXml.Element("Name")?.Value;

                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("INSERT INTO Application(Name) VALUES (@name)", conn);
                command.Parameters.AddWithValue("@name", applicationName);

                affectedfRows = command.ExecuteNonQuery();
            }
            catch
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (command != null) { command.Dispose(); }
            }

            if (affectedfRows > 0)
            {
                return Content(HttpStatusCode.OK, this.verifyApplicationExists(applicationName), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The application could not be created.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }


        [HttpPatch]
        [Route("api/somiod/{application}")]
        public IHttpActionResult PatchApplication(string application, [FromBody] XElement applicationXml)
        {
            SqlCommand command = null;
            SqlConnection conn = null;
            int affectedRows = -1;

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(applicationXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"{validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            if (this.verifyApplicationExists(application) == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            string newName = applicationXml.Element("Name")?.Value;
            if (string.IsNullOrEmpty(newName))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Application XML must contain a valid 'name' element.", "400"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("UPDATE Application SET Name = @newName WHERE Name = @appName", conn);
                command.Parameters.AddWithValue("@newName", newName);
                command.Parameters.AddWithValue("@appName", application);

                affectedRows = command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("Application name already exists", "409"), Configuration.Formatters.XmlFormatter);
                }

                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            catch
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (command != null) { command.Dispose(); }
            }

            if (affectedRows > 0)
            {
                return Content(HttpStatusCode.OK, this.verifyApplicationExists(newName), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The application could not be updated.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }


        [HttpDelete]
        [Route("api/somiod/{application}")]
        public IHttpActionResult DeleteApplication(string application)
        {
            SqlConnection conn = null;
            SqlDataReader reader = null;
            SqlCommand cmd = null;
            SqlCommand cmdDeleteContainer = null;
            SqlCommand cmdDeleteRecord = null;
            SqlCommand cmdDeleteNotification = null;

            Application app = this.verifyApplicationExists(application);

            if (app == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }


            List<int> containersIds = new List<int>();

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                cmd = new SqlCommand("SELECT Id FROM Container WHERE Parent = @appId", conn);
                cmd.Parameters.AddWithValue("@appId", app.Id);

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    containersIds.Add((int)reader["Id"]);
                }

                reader.Close();

                cmdDeleteRecord = new SqlCommand("DELETE FROM Record WHERE Parent = @containerId", conn);
                cmdDeleteRecord.Parameters.Add("@containerId", System.Data.SqlDbType.Int);

                cmdDeleteNotification = new SqlCommand("DELETE FROM Notification WHERE Parent = @containerId", conn);
                cmdDeleteNotification.Parameters.Add("@containerId", System.Data.SqlDbType.Int);

                cmdDeleteContainer = new SqlCommand("DELETE FROM Container WHERE Id = @containerId", conn);
                cmdDeleteContainer.Parameters.Add("@containerId", System.Data.SqlDbType.Int);

                foreach (var containerId in containersIds)
                {
                    cmdDeleteRecord.Parameters["@containerId"].Value = containerId;
                    cmdDeleteRecord.ExecuteNonQuery();

                    cmdDeleteNotification.Parameters["@containerId"].Value = containerId;
                    cmdDeleteNotification.ExecuteNonQuery();

                    cmdDeleteContainer.Parameters["@containerId"].Value = containerId;
                    cmdDeleteContainer.ExecuteNonQuery();
                }

                cmd = new SqlCommand("DELETE FROM Application WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", app.Id);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("The application cannot be deleted because there are related dependencies.", "409"), Configuration.Formatters.XmlFormatter);
                }

                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            catch
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (cmd != null) { cmd.Dispose(); }
                if (cmdDeleteRecord != null) { cmd.Dispose(); }
                if (cmdDeleteNotification != null) { cmd.Dispose(); }
                if (cmdDeleteContainer != null) { cmd.Dispose(); }
            }

            return Content(HttpStatusCode.OK, app, Configuration.Formatters.XmlFormatter);
        }

        #endregion


        #region Containers
        //-------------------------------------------------------------------------------------
        //------------------------------------- Containers ------------------------------------
        //------------------------------------------------------------------------------------- 

        [HttpPost]
        [Route("api/somiod/{application}")]
        public IHttpActionResult PostContainer(string application, [FromBody] XElement containerXml)
        {
            SqlConnection conn = null;
            SqlCommand command = null;
            int affectedfRows = -1;
            string containerName = null;

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(containerXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"{validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            Application app = this.verifyApplicationExists(application);

            if (app == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                containerName = this.verifyContainerExists(containerXml.Element("Name")?.Value) != null ? GenerateUniqueName("Container") : containerXml.Element("Name")?.Value;

                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("INSERT INTO Container(Name,Parent) VALUES (@name,@parantId)", conn);
                command.Parameters.AddWithValue("@name", containerName);
                command.Parameters.AddWithValue("@parantId", app.Id);

                affectedfRows = command.ExecuteNonQuery();
            }
            catch 
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (command != null) { command.Dispose(); }
            }

            if (affectedfRows > 0)
            {
                
                return Content(HttpStatusCode.OK, this.verifyContainerExists(containerName), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The container could not be created.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }

        [HttpGet]
        [Route("api/somiod/{application}/{container}")]
        public IHttpActionResult GetContainer(string application, string container)
        {
            ValidateResource resources = verifyParentOfContainer(application, container);

            if (resources.ErrorCode != HttpStatusCode.OK)
            {
                return Content(resources.ErrorCode, HandlerXML.responseError(resources.ErrorMessage, resources.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            return Content(HttpStatusCode.OK, HandlerXML.responseContainer(resources.Container), Configuration.Formatters.XmlFormatter);
        }


        [HttpPatch]
        [Route("api/somiod/{application}/{container}")]
        public IHttpActionResult PatchContainer(string application, string container, [FromBody] XElement containerXml)
        {
            SqlConnection conn = null;
            SqlCommand command = null;
            int affectedRows = -1;

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(containerXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"{validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            ValidateResource resources = verifyParentOfContainer(application, container);

            if (resources.ErrorCode != HttpStatusCode.OK)
            {
                return Content(resources.ErrorCode, HandlerXML.responseError(resources.ErrorMessage, resources.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }
       
            string newName = containerXml.Element("Name")?.Value;
            string newParentId = containerXml.Element("Parent")?.Value;

            if(string.IsNullOrEmpty(newName))
            {
                newName = resources.Container.Name;
            }

            if (string.IsNullOrEmpty(newParentId))
            {
                newParentId = (resources.Container.Parent).ToString();
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("UPDATE Container SET Name = @newName, Parent = @newParentId WHERE Parent = @parantId AND name = @containerName", conn);
                command.Parameters.AddWithValue("@newName", newName);
                command.Parameters.AddWithValue("@newParentId", int.Parse(newParentId));
                command.Parameters.AddWithValue("@parantId", resources.Application.Id);
                command.Parameters.AddWithValue("@containerName", container);

                affectedRows = command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("Container name already exists", "409"), Configuration.Formatters.XmlFormatter);
                }
                else if (ex.Number == 547)
                {
                    return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Invalid Parent ID (Application Not Found).", "400"), Configuration.Formatters.XmlFormatter);
                }


                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            catch
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (command != null) { command.Dispose(); }
            }

            if (affectedRows > 0)
            {
                return Content(HttpStatusCode.OK, this.verifyContainerExists(newName), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The container could not be updated.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }


        [HttpDelete]
        [Route("api/somiod/{application}/{container}")]
        public IHttpActionResult DeleteContainer(string application, string container)
        {

            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlCommand cmdDeleteNotification = null;
            SqlCommand cmdDeleteRecord = null;

            ValidateResource resources = verifyParentOfContainer(application, container);

            if (resources.ErrorCode != HttpStatusCode.OK)
            {
                return Content(resources.ErrorCode, HandlerXML.responseError(resources.ErrorMessage, resources.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                cmdDeleteRecord = new SqlCommand("DELETE FROM Record WHERE Parent = @containerId", conn);
                cmdDeleteRecord.Parameters.AddWithValue("@containerId", resources.Container.Id);
                cmdDeleteRecord.ExecuteNonQuery();

                cmdDeleteNotification = new SqlCommand("DELETE FROM Notification WHERE Parent = @containerId", conn);
                cmdDeleteNotification.Parameters.AddWithValue("@containerId", resources.Container.Id);
                cmdDeleteNotification.ExecuteNonQuery();


                cmd = new SqlCommand("DELETE FROM Container WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", resources.Container.Id);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("The container cannot be deleted because there are related dependencies.", "409"), Configuration.Formatters.XmlFormatter);
                }

                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            catch
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (cmd != null) { cmd.Dispose(); }
                if (cmdDeleteNotification != null) { cmdDeleteNotification.Dispose(); }
                if (cmdDeleteRecord != null) { cmdDeleteRecord.Dispose(); }

            }

            return Content(HttpStatusCode.OK, resources.Container, Configuration.Formatters.XmlFormatter);
        }

        #endregion


        #region Record

        //-------------------------------------------------------------------------------------
        //--------------------------------------- Record --------------------------------------
        //------------------------------------------------------------------------------------- 

        private IHttpActionResult PostRecord(string application, string container, [FromBody] XElement recordXml)
        {
            
            SqlConnection conn = null;
            SqlCommand command = null;
            int affectedfRows = 0;
            Record record = null;
            string recordName = null;

            ValidateResource resources = verifyParentOfContainer(application, container);

            if (resources.ErrorCode != HttpStatusCode.OK)
            {
                return Content(resources.ErrorCode, HandlerXML.responseError(resources.ErrorMessage, resources.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                recordName = this.verifyRecordExists(recordXml.Element("Name")?.Value) != null ? GenerateUniqueName("Record") : recordXml.Element("Name")?.Value;

                record = new Record
                {
                    Name = recordName,
                    Content = recordXml.Element("Content")?.Value
                };

                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("INSERT INTO Record(Name,Content,Parent) VALUES (@name,@content,@parentId)", conn);
                command.Parameters.AddWithValue("@name", record.Name);
                command.Parameters.AddWithValue("@content", record.Content);
                command.Parameters.AddWithValue("@parentId", resources.Container.Id);

                affectedfRows = command.ExecuteNonQuery();
            }
            catch 
            { 
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if(command != null) { command.Dispose(); }
            }
            
            if (affectedfRows > 0)
            {
                triggerNotification(1, resources.Application, resources.Container, this.verifyRecordExists(record.Name));

                return Content(HttpStatusCode.OK, this.verifyRecordExists(record.Name), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The record could not be created.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }

        [HttpGet]
        [Route("api/somiod/{application}/{container}/record/{name}")]
        public IHttpActionResult GetRecord(string application, string container, string name)
        {
            ValidateResource resources = verifyParentOfContainer(application, container);

            if (resources.ErrorCode != HttpStatusCode.OK)
            {
                return Content(resources.ErrorCode, HandlerXML.responseError(resources.ErrorMessage, resources.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            resources.RecordOrNotification = verifyParentOfRecordAndNotification(resources.Container, "Record", name); ;

            if (resources.RecordOrNotification is ValidateResource validateResource)
            {
                return Content(validateResource.ErrorCode, HandlerXML.responseError(validateResource.ErrorMessage, validateResource.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }
           
            return Content(HttpStatusCode.OK, HandlerXML.responseRecord((Record)resources.RecordOrNotification), Configuration.Formatters.XmlFormatter);
        }


        [HttpDelete]
        [Route("api/somiod/{application}/{container}/record/{name}")]
        public IHttpActionResult DeleteRecord(string application, string container, string name)
        {
            SqlConnection conn = null;
            SqlCommand cmd = null;
            Record record = null;

            ValidateResource resources = verifyParentOfContainer(application, container);

            if (resources.ErrorCode != HttpStatusCode.OK)
            {
                return Content(resources.ErrorCode, HandlerXML.responseError(resources.ErrorMessage, resources.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            resources.RecordOrNotification = verifyParentOfRecordAndNotification(resources.Container, "Record", name); ;

            if (resources.RecordOrNotification is ValidateResource validateResource)
            {
                return Content(validateResource.ErrorCode, HandlerXML.responseError(validateResource.ErrorMessage, validateResource.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                record = (Record)resources.RecordOrNotification;
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                cmd = new SqlCommand("DELETE FROM Record WHERE Name = @name AND Parent = @parantId", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parantId", resources.Container.Id);
                cmd.ExecuteNonQuery();
            }
            catch 
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if(cmd != null) { cmd.Dispose(); }
            }

            triggerNotification(2, resources.Application, resources.Container, record);

            return Content(HttpStatusCode.OK, HandlerXML.responseRecord((Record)resources.RecordOrNotification), Configuration.Formatters.XmlFormatter);
        }
        #endregion


        #region Notifications
        //-------------------------------------------------------------------------------------
        //------------------------------------ Notifications ----------------------------------
        //-------------------------------------------------------------------------------------

        private IHttpActionResult PostNotification(string application, string container, [FromBody] XElement notificationXml)
        {
            SqlConnection conn = null;
            SqlCommand command = null;
            int affectedfRows = 0;
            Notification notification = null;
            string notificationName = null;

            ValidateResource resources = verifyParentOfContainer(application, container);

            if (resources.ErrorCode != HttpStatusCode.OK)
            {
                return Content(resources.ErrorCode, HandlerXML.responseError(resources.ErrorMessage, resources.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                notificationName = this.verifyNotifcationExists(notificationXml.Element("Name")?.Value) != null ? GenerateUniqueName("Notification") : notificationXml.Element("Name")?.Value;

                notification = new Notification
                {
                    Name = notificationName,
                    Event = int.Parse(notificationXml.Element("Event").Value),
                    Endpoint = notificationXml.Element("Endpoint").Value,
                    Enabled = bool.Parse(notificationXml.Element("Enabled").Value)
                };

                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("INSERT INTO Notification(Name,Parent,Event,Endpoint,Enabled) " +
                                                                 "VALUES (@name,@parentId,@event,@endpoint,@enabled)", conn);

                command.Parameters.AddWithValue("@name", notification.Name);
                command.Parameters.AddWithValue("@parentId", resources.Container.Id);
                command.Parameters.AddWithValue("@event", notification.Event);
                command.Parameters.AddWithValue("@endpoint", notification.Endpoint);
                command.Parameters.AddWithValue("@enabled", notification.Enabled);

                affectedfRows = command.ExecuteNonQuery();
            }
            catch
            { 
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if(command != null) { command.Dispose(); }
            }

            if (affectedfRows > 0)
            {
                return Content(HttpStatusCode.Created, this.verifyNotifcationExists(notification.Name), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The notification could not be created.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }


        [HttpGet]
        [Route("api/somiod/{application}/{container}/notification/{name}")]
        public IHttpActionResult GetNotification(string application, string container, string name)
        {
            ValidateResource resources = verifyParentOfContainer(application, container);

            if (resources.ErrorCode != HttpStatusCode.OK)
            {
                return Content(resources.ErrorCode, HandlerXML.responseError(resources.ErrorMessage, resources.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            resources.RecordOrNotification = verifyParentOfRecordAndNotification(resources.Container, "Notification", name); ;

            if (resources.RecordOrNotification is ValidateResource validateResource)
            {
                return Content(validateResource.ErrorCode, HandlerXML.responseError(validateResource.ErrorMessage, validateResource.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            return Content(HttpStatusCode.OK, HandlerXML.responseNotification((Notification)resources.RecordOrNotification), Configuration.Formatters.XmlFormatter);
        }


        [HttpDelete]
        [Route("api/somiod/{application}/{container}/notification/{name}")]
        public IHttpActionResult DeleteNotification(string application, string container, string name)
        {
            SqlConnection conn = null;
            SqlCommand cmd = null;

            ValidateResource resources = verifyParentOfContainer(application, container);

            if (resources.ErrorCode != HttpStatusCode.OK)
            {
                return Content(resources.ErrorCode, HandlerXML.responseError(resources.ErrorMessage, resources.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            resources.RecordOrNotification = verifyParentOfRecordAndNotification(resources.Container, "Notification", name);

            if (resources.RecordOrNotification is ValidateResource validateResource)
            {
                return Content(validateResource.ErrorCode, HandlerXML.responseError(validateResource.ErrorMessage, validateResource.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                resources.RecordOrNotification = (Notification)resources.RecordOrNotification;
            }
           
            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                cmd = new SqlCommand("DELETE FROM Notification WHERE Name = @name AND Parent = @parantId", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parantId", resources.Container.Id);

                cmd.ExecuteNonQuery();
            }
            catch
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (cmd != null) { cmd.Dispose(); }
            }

            return Content(HttpStatusCode.OK, HandlerXML.responseNotification((Notification)resources.RecordOrNotification), Configuration.Formatters.XmlFormatter);
        }

        [HttpPatch]
        [Route("api/somiod/{application}/{container}/notification/{name}")]
        public IHttpActionResult ToggleNotification(string application, string container, string name, [FromBody] XElement notificationXml)
        {
            SqlConnection conn = null;
            SqlCommand command = null;
            int affectedRows = -1;

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(notificationXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"{validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            if (notificationXml.Elements().Count() != 1)
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("The notification state change must contain only the 'Enabled' element.", "400"), Configuration.Formatters.XmlFormatter);
            }
            
            int enabled = int.Parse(notificationXml.Elements().First().Value);

            ValidateResource resources = verifyParentOfContainer(application, container);

            if (resources.ErrorCode != HttpStatusCode.OK)
            {
                return Content(resources.ErrorCode, HandlerXML.responseError(resources.ErrorMessage, resources.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            resources.RecordOrNotification = verifyParentOfRecordAndNotification(resources.Container, "Notification", name);

            if (resources.RecordOrNotification is ValidateResource validateResource)
            {
                return Content(validateResource.ErrorCode, HandlerXML.responseError(validateResource.ErrorMessage, validateResource.ErrorCode.ToString()), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("UPDATE Notification SET Enabled = @enabled WHERE Parent = @parantId AND name = @notificationName", conn);
                command.Parameters.AddWithValue("@enabled", enabled);
                command.Parameters.AddWithValue("@parantId", resources.Container.Id);
                command.Parameters.AddWithValue("@notificationName", name);

                affectedRows = command.ExecuteNonQuery();
            }
            catch
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (command != null) {command.Dispose(); }
            }

            if (affectedRows > 0)
            {
                return Content(HttpStatusCode.OK, this.verifyNotifcationExists(name), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The notification could not be updated.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }

        private void triggerNotification(int evento, Application application, Container container, Record record)
        {
            SqlConnection conn = null;
            SqlCommand cmd = null;
            Notification notification = null;
            SqlDataReader sqlReader = null;
            HttpClient httpClient = null;
            HttpContent httpContent = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                cmd = new SqlCommand("SELECT * FROM Notification WHERE event = @evento AND Parent = @parantId AND Enabled = 1", conn);
                cmd.Parameters.AddWithValue("@evento", evento);
                cmd.Parameters.AddWithValue("@parantId", container.Id);

                sqlReader = cmd.ExecuteReader();

                while (sqlReader.Read())
                {
                    notification = new Notification
                    {
                        Endpoint = (string)sqlReader["Endpoint"],
                    };

                    try
                    {
                        if (notification != null)
                        {
                            if (notification.Endpoint.StartsWith("mqtt", StringComparison.OrdinalIgnoreCase))
                            {
                                var uri = new Uri(notification.Endpoint);

                                string host = uri.Host;
                                int port = uri.Port;
                                string channel = "api/somiod/" + application.Name + "/" + container.Name;

                                MqttClient mcClient = (port > 0) ? new MqttClient(host, port, false, null, null, MqttSslProtocols.None) : new MqttClient(host);

                                string clientId = Guid.NewGuid().ToString();

                                mcClient.Connect(clientId);

                                if (mcClient.IsConnected)
                                {
                                    mcClient.Publish(channel, Encoding.UTF8.GetBytes(record.ToString(evento)), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
                                }
                            }
                            else
                            {
                                string data = record.ToString(evento);

                                httpClient = new HttpClient();
                                    
                                httpContent = new StringContent(data, Encoding.UTF8, "application/xml");

                                HttpResponseMessage response = httpClient.PostAsync(notification.Endpoint, httpContent).Result;
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if(sqlReader != null) { sqlReader.Close(); }
                if(conn != null) { conn.Close(); }
                if(httpClient != null) { httpClient.Dispose(); }
                if(httpContent != null) { httpContent.Dispose(); }
            }
        }

        #endregion


        #region Suport Functions
        //-------------------------------------------------------------------------------------
        //--------------------------------- Suport Functions ----------------------------------
        //------------------------------------------------------------------------------------- 

        [HttpPost]
        [Route("api/somiod/{application}/{container}")]
        public IHttpActionResult PostRecordOrNotification(string application, string container, [FromBody] XElement elementXML)
        {
            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(elementXML);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"{validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                string rootName = elementXML.Name.LocalName;

                if (rootName.Equals("Record", StringComparison.OrdinalIgnoreCase))
                { 
                    return PostRecord(application, container, elementXML);
                }

                if (rootName.Equals("Notification", StringComparison.OrdinalIgnoreCase))
                {
                    if (elementXML.Elements().Count() == 1)
                    {
                        return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("The request must include the 'Name', 'Event', 'Endpoint', and 'Enabled' elements in the notification post.", "400"), Configuration.Formatters.XmlFormatter);
                    }

                    return PostNotification(application, container, elementXML);
                }

                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"Unexpected root element: {rootName}. Expected 'Record' or 'Notification'.", "400"), Configuration.Formatters.XmlFormatter);

            }
            catch
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }

        private Application verifyApplicationExists(string name)
        {
            SqlConnection conn = null;
            SqlCommand command = null;
            SqlDataReader sqlReader = null;
            Application application = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("SELECT * FROM Application WHERE name = @appName", conn);
                command.Parameters.AddWithValue("@appName", name);

                sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    application = new Application
                    {
                        Id = sqlReader.GetInt32(0),
                        Name = sqlReader.GetString(1),
                        CreationDateTime = sqlReader.GetDateTime(2),
                    };
                }

                if (application != null)
                {
                    return application;
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
                if (command != null) { command.Dispose(); }
            }

            return null;
        }

        private Container verifyContainerExists(string name)
        {
            SqlConnection conn = null;
            SqlCommand command = null;
            SqlDataReader sqlReader = null;
            Container container = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("SELECT * FROM Container WHERE name = @containerName", conn);
                command.Parameters.AddWithValue("@containerName", name);

                sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    container = new Container
                    {
                        Id = (int)sqlReader["Id"],
                        Name = (string)sqlReader["Name"],
                        CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                        Parent = (int)sqlReader["Parent"]
                    };
                }

                if (container != null)
                {
                    return container;
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
                if(command != null) { command.Dispose(); }
            }

            return null;
        }

        private Record verifyRecordExists(string name)
        {
            SqlConnection conn = null;
            SqlCommand command = null;
            SqlDataReader sqlReader = null;
            Record record = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("SELECT * FROM Record WHERE name = @recordName", conn);
                command.Parameters.AddWithValue("@recordName", name);

                sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    record = new Record
                    {
                        Id = (int)sqlReader["Id"],
                        Name = (string)sqlReader["Name"],
                        Content = (string)sqlReader["Content"],
                        CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                        Parent = (int)sqlReader["Parent"]
                    };
                }

                if (record != null)
                {
                    return record;
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
                if (command != null) { command.Dispose(); }
            }

            return null;
        }

        private Notification verifyNotifcationExists(string name)
        {
            SqlConnection conn = null;
            SqlCommand command = null;
            SqlDataReader sqlReader = null;
            Notification notification = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                command = new SqlCommand("SELECT * FROM Notification WHERE name = @notificationName", conn);
                command.Parameters.AddWithValue("@notificationName", name);

                sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    notification = new Notification
                    {
                        Id = (int)sqlReader["Id"],
                        Name = (string)sqlReader["Name"],
                        Event = (int)sqlReader["Event"],
                        Endpoint = (string)sqlReader["Endpoint"],
                        Enabled = (bool)sqlReader["Enabled"],
                        Parent = (int)sqlReader["Parent"],
                        CreationDateTime = (DateTime)sqlReader["CreationDateTime"]
                    };
                }

                if (notification != null)
                {
                    return notification;
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
                if(command != null) { command.Dispose(); }
            }

            return null;
        }


        private ValidateResource verifyParentOfContainer(string application, string container)
        {
            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlDataReader sqlReader = null;

            Container ischildren = null;

            Application app = this.verifyApplicationExists(application);

            if (app == null)
            {
                return new ValidateResource(null, null, null, "Application was not found.", HttpStatusCode.NotFound);
            }

            Container cont = this.verifyContainerExists(container);

            if (cont == null)
            {
                return new ValidateResource(null, null, null, "Container was not found.", HttpStatusCode.NotFound);
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                cmd = new SqlCommand("SELECT * FROM Container WHERE name = @name AND Parent = @parantId", conn);
                cmd.Parameters.AddWithValue("@name", container);
                cmd.Parameters.AddWithValue("@parantId", app.Id);

                sqlReader = cmd.ExecuteReader();

                while (sqlReader.Read())
                {
                    ischildren = new Container
                    {
                        Id = (int)sqlReader["Id"],
                        Name = (string)sqlReader["Name"],
                        CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                        Parent = (int)sqlReader["Parent"]
                    };
                }

                if (ischildren != null)
                {
                    return new ValidateResource(app, cont, null, null, HttpStatusCode.OK);
                }
            }
            catch
            {
                return new ValidateResource(null, null, null, "An error occurred while processing your request. Please try again later.", HttpStatusCode.InternalServerError);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
                if (cmd != null) { cmd.Dispose(); }
            }

            return new ValidateResource(null, null, null, "Container does not belong to the specified application.", HttpStatusCode.NotFound);
        }

        private Object verifyParentOfRecordAndNotification(Container container, string resource, string name)
        {
            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlDataReader sqlReader = null;
            object ischildren = null;

            if (resource.Equals("Record", StringComparison.OrdinalIgnoreCase))
            {
                if (this.verifyRecordExists(name) == null)
                {
                    return new ValidateResource(null, null, null, "Record was not found.", HttpStatusCode.NotFound);
                }
            }
            else if (resource.Equals("Notification", StringComparison.OrdinalIgnoreCase))
            {
                if (this.verifyNotifcationExists(name) == null)
                {
                    return new ValidateResource(null, null, null, "Notification was not found.", HttpStatusCode.NotFound);
                }
            }
            
            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                string query = $"SELECT * FROM {resource} WHERE Name = @name AND Parent = @parentId";
                cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parentId", container.Id);
                
                sqlReader = cmd.ExecuteReader();

                if (sqlReader.Read())
                {
                    if (resource.Equals("Record", StringComparison.OrdinalIgnoreCase))
                    {
                        ischildren = new Record
                        {
                            Id = (int)sqlReader["Id"],
                            Name = (string)sqlReader["Name"],
                            Content = (string)sqlReader["Content"],
                            CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                            Parent = (int)sqlReader["Parent"]
                        };
                    }
                    else if (resource.Equals("Notification", StringComparison.OrdinalIgnoreCase))
                    {
                        ischildren = new Notification
                        {
                            Id = (int)sqlReader["Id"],
                            Name = (string)sqlReader["Name"],
                            Event = (int)sqlReader["Event"],
                            Endpoint = (string)sqlReader["Endpoint"],
                            Enabled = (bool)sqlReader["Enabled"],
                            CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                            Parent = (int)sqlReader["Parent"]
                        };
                    }
                }
            }
            catch
            {
                return new ValidateResource(null, null, null, "An error occurred while processing your request. Please try again later.", HttpStatusCode.InternalServerError);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
                if (cmd != null) { cmd.Dispose(); }
            }

            if (ischildren != null)
            {
                return ischildren;
            }
            else
            {
                return new ValidateResource(null, null, null, $"{resource} does not belong to the specified container.", HttpStatusCode.NotFound);
            }
        }

        private string GenerateUniqueName(string baseName)
        {
            return $"{baseName}_{Guid.NewGuid()}";
        }
        #endregion
    }
}