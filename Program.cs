using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace RegOnline.ConsoleAPISample
{
    class Program
    {
        static void Main(string[] args)
        {
            string login = string.Empty;
            string password = string.Empty;

            Console.WriteLine("Enter your RegOnline login followed by [Enter] to begin.");
            login = Console.ReadLine();
            Console.WriteLine("Enter your RegOnline password followed by [Enter].");
            password = Console.ReadLine();

            try
            {
                using (RegOnlineAPIProxy.RegOnlineAPISoapClient service = new RegOnlineAPIProxy.RegOnlineAPISoapClient("RegOnline APISoap"))
                {
                    RegOnlineAPIProxy.ResultsOfLoginResults loginResults = service.Login(login, password);

                    if (loginResults.Success)
                    {
                        string apiToken = loginResults.Data.APIToken;
                        Console.WriteLine("apiToken = {0}", apiToken);

                        using (OperationContextScope scope = new OperationContextScope(service.InnerChannel))
                        {
                            //add APIToken HTTP Header
                            HttpRequestMessageProperty apiTokenHeader = new HttpRequestMessageProperty();
                            apiTokenHeader.Headers.Add("APIToken", apiToken);
                            OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = apiTokenHeader;

                            // Call GetEvents for events with active status created after 1/1/2011, sorted by ID ascending.
                            // With the API Token passed as an HTTP Header (above) instead of a SOAP Header, the first parameter for GetEvents below can be null.
                            RegOnlineAPIProxy.ResultsOfListOfEvent getEventResults = service.GetEvents(null, "IsActive && AddDate >= DateTime(2011, 1, 1)", "ID ASC");

                            if (getEventResults.Success)
                            {
                                foreach (var result in getEventResults.Data)
                                {
                                    Console.WriteLine("Event: {0} (ID: {1})", result.Title, result.ID);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error retreiving GetEvents results: {0}", getEventResults.Message);
                            }

                            service.Close();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Login failure: {0}", loginResults.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error{0}{1}", System.Environment.NewLine, ex);
            }

            Console.WriteLine("Press any key to exit.");
            Console.Read();
        }
    }
}