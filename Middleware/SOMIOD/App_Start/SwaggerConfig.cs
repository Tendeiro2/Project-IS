using System.Web.Http;
using WebActivatorEx;
using SOMIOD;
using Swashbuckle.Application;
using System.Web.Hosting;
using System.Xml;
using System.Web.Services.Description;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Swashbuckle.Swagger;
using System.Web.Http.Description;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Xml.Linq;
using SOMIOD.Models;
using System.Net;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]
namespace SOMIOD
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "SOMIOD");
                    c.OperationFilter<ApplyXmlFormatFilter>(); 
                    c.OperationFilter<ApplyOperationIdFilter>();  
                })
                .EnableSwaggerUi(c =>
                {
                    c.DocumentTitle("Swagger UI - SOMIOD");
                });

            var xmlFormatter = GlobalConfiguration.Configuration.Formatters.XmlFormatter;
            GlobalConfiguration.Configuration.Formatters.Clear();
            GlobalConfiguration.Configuration.Formatters.Add(xmlFormatter);
        }


        public class ApplyXmlFormatFilter : Swashbuckle.Swagger.IOperationFilter
        {
            public void Apply(Swashbuckle.Swagger.Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
            {
                operation.produces.Clear();
                operation.produces.Add("application/xml");
                operation.consumes.Clear();
                operation.consumes.Add("application/xml");
            }
        }

        public class ApplyOperationIdFilter : Swashbuckle.Swagger.IOperationFilter
        {
            public void Apply(Swashbuckle.Swagger.Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
            {
                //----------------------------- Applications -----------------------------
                if (operation.operationId == "SOMIOD_PostApplication")
                {
                    operation.summary = "Cria uma nova aplicação";

                    var applicationXmlParam = operation.parameters.FirstOrDefault(p => p.name == "applicationXml");

                    if (applicationXmlParam != null)
                    {
                        string xml = "<Application>\n<Name>DefaultApp</Name>\n</Application>";

                        string escapedXml = System.Net.WebUtility.HtmlEncode(xml);

                        applicationXmlParam.@default = escapedXml;
                    }
                }
                if (operation.operationId == "SOMIOD_LocateContainersRecordsNotifications")
                {
                    operation.summary = "Obter informações de uma aplicação";

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }
                }
                else if (operation.operationId == "SOMIOD_PatchApplication")
                {
                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");
                    var applicationParam2 = operation.parameters.FirstOrDefault(p => p.name == "applicationXml");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    if (applicationParam2 != null)
                    {
                        string xml = "<Application>\n<Name>DefaultApp</Name>\n</Application>";

                        string escapedXml = System.Net.WebUtility.HtmlEncode(xml);

                        applicationParam2.@default = escapedXml;
                    }

                    operation.summary = "Atualizar uma Aplicação";
                }
                else if (operation.operationId == "SOMIOD_DeleteApplication")
                {
                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    operation.summary = "Apagar uma Aplicação";
                }
                //----------------------------- Containers -----------------------------
                else if(operation.operationId == "SOMIOD_PostContainer")
                {
                    operation.summary = "Cria um novo container";

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    var containerXmlParam = operation.parameters.FirstOrDefault(p => p.name == "containerXml");

                    if (containerXmlParam != null)
                    {
                        string xml = "<Container>\n<Name>DefaultContainer</Name>\n</Container>";

                        string escapedXml = System.Net.WebUtility.HtmlEncode(xml);

                        containerXmlParam.@default = escapedXml;
                    }
                }
                else if(operation.operationId == "SOMIOD_GetContainer")
                {
                    operation.summary = "Obter informações de um container";

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    var containerParam = operation.parameters.FirstOrDefault(p => p.name == "container");

                    if (containerParam != null)
                    {
                        containerParam.@default = "DefaultContainer";
                    }
                }
                else if(operation.operationId == "SOMIOD_PatchContainer")
                {
                    operation.summary = "Atualizar um Container";

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    var containerParam = operation.parameters.FirstOrDefault(p => p.name == "container");

                    if (containerParam != null)
                    {
                        containerParam.@default = "DefaultContainer";
                    }

                    var containerXmlParam = operation.parameters.FirstOrDefault(p => p.name == "containerXml");

                    if (containerXmlParam != null)
                    {
                        string xml = "<Container>\n<Name>DefaultContainer</Name>\n<Parent>35</Parent>\n</Container>";

                        string escapedXml = System.Net.WebUtility.HtmlEncode(xml);

                        containerXmlParam.@default = escapedXml;
                    }
                }
                else if(operation.operationId == "SOMIOD_DeleteContainer")
                {
                    operation.summary = "Apagar um Container";

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    var containerParam = operation.parameters.FirstOrDefault(p => p.name == "container");

                    if (containerParam != null)
                    {
                        containerParam.@default = "DefaultContainer";
                    }
                }
                //----------------------------- Record -----------------------------
                else if(operation.operationId == "SOMIOD_PostRecordOrNotification")
                {
                    string xml2 = "Este endpoint cria records e notifications, ele identifica o root node passado no body.\n\n\nXML para criar uma notification:\n\n\n<Notification>\n\n<Name>DefaultNotification</Name>\n\n<Event>1</Event>\n\n<Endpoint>mqtt://127.0.0.1</Endpoint>\n\n<Enabled>true</Enabled>\n\n</Notification>";

                    string escapedXml2 = System.Net.WebUtility.HtmlEncode(xml2);

                    operation.summary = "Criar um novo record ou uma nova notification";
                    operation.description = escapedXml2;

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    var containerParam = operation.parameters.FirstOrDefault(p => p.name == "container");

                    if (containerParam != null)
                    {
                        containerParam.@default = "DefaultContainer";
                    }

                    var containerXmlParam = operation.parameters.FirstOrDefault(p => p.name == "elementXML");

                    if (containerXmlParam != null)
                    {
                        string xml = "<Record>\n<Name>DefaultRecord</Name>\n<Content>On</Content>\n</Record>\n\n";

                        string escapedXml = System.Net.WebUtility.HtmlEncode(xml);

                        containerXmlParam.@default = escapedXml;
                    }
                }
                else if(operation.operationId == "SOMIOD_GetRecord")
                {
                    operation.summary = "Obter informações de um record";

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    var containerParam = operation.parameters.FirstOrDefault(p => p.name == "container");

                    if (containerParam != null)
                    {
                        containerParam.@default = "DefaultContainer";
                    }

                    var recordParam = operation.parameters.FirstOrDefault(p => p.name == "name");

                    if (containerParam != null)
                    {
                        recordParam.@default = "DefaultRecord";
                    }
                }
                else if(operation.operationId == "SOMIOD_DeleteRecord")
                {
                    operation.summary = "Apagar um Record";

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    var containerParam = operation.parameters.FirstOrDefault(p => p.name == "container");

                    if (containerParam != null)
                    {
                        containerParam.@default = "DefaultContainer";
                    }

                    var recordParam = operation.parameters.FirstOrDefault(p => p.name == "name");

                    if (containerParam != null)
                    {
                        recordParam.@default = "DefaultRecord";
                    }
                }
                //----------------------------- Notification -----------------------------
                else if (operation.operationId == "SOMIOD_GetNotification")
                {
                    operation.summary = "Obter informações de uma notification";

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    var containerParam = operation.parameters.FirstOrDefault(p => p.name == "container");

                    if (containerParam != null)
                    {
                        containerParam.@default = "DefaultContainer";
                    }

                    var recordParam = operation.parameters.FirstOrDefault(p => p.name == "name");

                    if (containerParam != null)
                    {
                        recordParam.@default = "DefaultNotification";
                    }
                }
                else if (operation.operationId == "SOMIOD_DeleteNotification")
                {
                    operation.summary = "Apagar uma notification";

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    var containerParam = operation.parameters.FirstOrDefault(p => p.name == "container");

                    if (containerParam != null)
                    {
                        containerParam.@default = "DefaultContainer";
                    }

                    var recordParam = operation.parameters.FirstOrDefault(p => p.name == "name");

                    if (containerParam != null)
                    {
                        recordParam.@default = "DefaultNotification";
                    }
                }
                else if (operation.operationId == "SOMIOD_ToggleNotification")
                {
                    operation.summary = "Atualizar uma notification (enabled)";

                    var applicationParam = operation.parameters.FirstOrDefault(p => p.name == "application");

                    if (applicationParam != null)
                    {
                        applicationParam.@default = "DefaultApp";
                    }

                    var containerParam = operation.parameters.FirstOrDefault(p => p.name == "container");

                    if (containerParam != null)
                    {
                        containerParam.@default = "DefaultContainer";
                    }

                    var recordParam = operation.parameters.FirstOrDefault(p => p.name == "name");

                    if (containerParam != null)
                    {
                        recordParam.@default = "DefaultNotification";
                    }

                    var containerXmlParam = operation.parameters.FirstOrDefault(p => p.name == "notificationXml");

                    if (containerXmlParam != null)
                    {
                        string xml = "<Notification>\n<Enabled>0</Enabled>\n</Notification>";

                        string escapedXml = System.Net.WebUtility.HtmlEncode(xml);

                        containerXmlParam.@default = escapedXml;
                    }
                }
            }
        }
    }
}
