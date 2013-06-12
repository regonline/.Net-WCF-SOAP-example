using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using RegOnline.ConsoleAPISample.RegOnlineAPIProxy;

namespace RegOnline.ConsoleAPISample
{
    internal class Program_CreateCustomFieldResponsesForManyRegistrants
    {
        private static void Main(string[] args)
        {
            string login = string.Empty;
            string password = string.Empty;

            Console.WriteLine("Enter your RegOnline login followed by [Enter] to begin.");
            login = Console.ReadLine();
            Console.WriteLine("Enter your RegOnline password followed by [Enter].");
            password = Console.ReadLine();

            try
            {
                using (
                    RegOnlineAPIProxy.RegOnlineAPISoapClient service =
                        new RegOnlineAPIProxy.RegOnlineAPISoapClient("RegOnline APISoap"))
                {
                    RegOnlineAPIProxy.ResultsOfLoginResults loginResults = service.Login(login, password);

                    if (loginResults.Success
                        && loginResults.Data != null
                        && !string.IsNullOrEmpty(loginResults.Data.APIToken))
                    {
                        string apiToken = loginResults.Data.APIToken;
                        Console.WriteLine("{0}apiToken = {1}", System.Environment.NewLine, apiToken);

                        using (OperationContextScope scope = new OperationContextScope(service.InnerChannel))
                        {
                            //add APIToken HTTP Header
                            HttpRequestMessageProperty apiTokenHeader = new HttpRequestMessageProperty();
                            apiTokenHeader.Headers.Add("APIToken", apiToken);
                            OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] =
                                apiTokenHeader;

                            createCustomFieldResponses(service);

                            service.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error{0}{1}", System.Environment.NewLine, ex);
            }

            Console.WriteLine(System.Environment.NewLine);
            Console.WriteLine("Press [Enter] to exit.");
            Console.ReadLine();
        }
        
        /// <summary>
        /// Given a known list of registrations (by ID) and custom field checkboxes (by ID), 
        /// creates a response for each custom field checkbox for all registrations
        /// </summary>
        private static void createCustomFieldResponses(RegOnlineAPIProxy.RegOnlineAPISoapClient service)
        {
            List<int> regIDs = new List<int>()
                {
                    1,2,3 //{ your list of registrants which need custom field responses to checkboxes created }
                };

            foreach (int registrationID in regIDs)
            {
                // Get the registrant
                Console.WriteLine("calling GetRegistration");
                RegOnlineAPIProxy.ResultsOfListOfRegistration getRegResults = service.GetRegistration(null, registrationID);

                if (getRegResults.Success)
                {
                    // API instance of the current registration
                    RegOnlineAPIProxy.APIRegistration registration = getRegResults.Data[0];

                    List<int> CFIDs = new List<int>()
                        {
                            7,8,9 //{ your list of custom fields which need responses created for each registrant in List<int> regIDs }
                        };

                    List<RegOnlineAPIProxy.APICustomFieldResponse> responses = new List<APICustomFieldResponse>();

                    // create a list of responses to each checkbox in List<int> CFIDs for the current registration
                    foreach (int cfID in CFIDs)
                    {
                        RegOnlineAPIProxy.APICustomFieldResponse newResponse =
                            new RegOnlineAPIProxy.APICustomFieldResponse();
                        newResponse.CFID = cfID;
                        newResponse.RegistrationID = registrationID;
                        newResponse.EventID = registration.EventID;
                        newResponse.Response = "True"; // set response to True for checkbox
                        responses.Add(newResponse);
                    }

                    // push all new custom field responses via API for the current registration
                    RegOnlineAPIProxy.ResultsOfBoolean updateResults =
                        service.UpdateCustomFieldResponsesForRegistration(null, registration.EventID, registrationID, responses.ToArray());

                    // handle API response
                    if (updateResults.Success)
                    {
                        Console.WriteLine(
                            "UpdateCustomFieldResponsesForRegistration succeeded - {0} responses added for reg {1} {2}."
                            , responses.Count, registration.FirstName, registration.LastName);
                    }
                    else
                    {
                        Console.WriteLine(updateResults.Message);
                    }

                    System.Threading.Thread.Sleep(100); // add delay between each registrant to prevent exceeding ROL API usage throttling limit, if necessary
                }
            }
        }
    }
}