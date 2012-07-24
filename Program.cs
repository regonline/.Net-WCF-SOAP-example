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
                            OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = apiTokenHeader;

                            #region GetEvents example

                            // Call GetEvents for events with active status created after 1/1/2011, sorted by ID ascending.
                            // With the API Token passed as an HTTP Header (above) instead of a SOAP Header, the first parameter for GetEvents below can be null.
                            RegOnlineAPIProxy.ResultsOfListOfEvent getEventResults = service.GetEvents(null, "IsActive && AddDate >= DateTime(2011, 1, 1)", "ID ASC");

                            if (getEventResults.Success)
                            {
                                foreach (var result in getEventResults.Data)
                                {
                                    Console.WriteLine("Event: {0} (ID: {1})", result.Title, result.ID);
                                }

                                Console.WriteLine(System.Environment.NewLine);
                            }
                            else
                            {
                                Console.WriteLine("{0}{1}{0}Error retreiving GetEvents results: {2}{0}{1}{0}"
                                    , System.Environment.NewLine, "*****", getEventResults.Message);
                            }

                            #endregion

                            Console.WriteLine("Would you like to upsert a registrant's custom field response for a multiple choice custom field?  Press y followed by [Enter] to continue or [Enter] to exit.");
                            bool updateCustomFieldResponse = (Console.ReadLine() == "y");

                            if (updateCustomFieldResponse)
                            {
                                UpsertMultipleChoiceCustomFieldResponse(service);                                
                            }
                            else
                            {
                                return;
                            }

                            service.Close();
                        }
                    }
                    else
                    {
                        Console.WriteLine("{0}{1}{0}Login failure: {2}{0}{1}{0}"
                            , System.Environment.NewLine, "*****", loginResults.Message);
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
        /// Prompts for registration ID and custom field ID then updates or inserts 
        /// a response for the provided custom field to a new list item
        /// </summary>
        private static void UpsertMultipleChoiceCustomFieldResponse(RegOnlineAPIProxy.RegOnlineAPISoapClient service)
        {
            // Prompt for registration ID
            int registrationID = 0;
            do
            {
                Console.WriteLine("Enter the ID of the registrant you wish to update a response for.");
                string providedRegID = Console.ReadLine();

                if (!int.TryParse(providedRegID, out registrationID) || (registrationID <= 0))
                {
                    Console.WriteLine("Invalid registration ID.");
                }

            } while (registrationID <= 0);

            // Prompt for custom field ID
            int cfID = 0;
            do
            {
                Console.WriteLine("Enter the ID of the custom field that the registrant you are updating selected a multiple choice item for.");
                string providedCFID = Console.ReadLine();

                if (!int.TryParse(providedCFID, out cfID) || (cfID <= 0))
                {
                    Console.WriteLine("Invalid custom field ID.");
                }

            } while (cfID <= 0);

            // Get the registrant
            Console.WriteLine("calling GetRegistration");
            RegOnlineAPIProxy.ResultsOfListOfRegistration getRegResults = service.GetRegistration(null, registrationID);

            if (getRegResults.Success)
            {
                // API instance of the provided registration
                RegOnlineAPIProxy.APIRegistration registration = getRegResults.Data[0];

                // Get responses for the provided registrant
                Console.WriteLine("calling GetAgendaItemResponsesForRegistration");
                RegOnlineAPIProxy.ResultsOfListOfCustomFieldResponse getResponsesResults = service.GetAgendaItemResponsesForRegistration(null, registration.EventID, registrationID, string.Empty);

                if (getResponsesResults.Success)
                {
                    RegOnlineAPIProxy.APICustomFieldResponse responseToUpsert = getResponsesResults.Data.Where(r => r.CFID == cfID).FirstOrDefault();

                    // Now display list items to select from
                    // With the API Token passed as an HTTP Header (above) instead of a SOAP Header, the first parameter for GetEvents below can be null.
                    RegOnlineAPIProxy.ResultsOfListOfCustomFieldListItem getCFLIResults = service.GetCustomFieldListItems(null, registration.EventID, cfID, "IsVisible", "Order ASC");

                    if (getCFLIResults.Success)
                    {
                        // display available list items
                        Console.WriteLine(System.Environment.NewLine);
                        foreach (var result in getCFLIResults.Data)
                        {
                            Console.WriteLine("Item: {0} (ID: {1})", result.NameOnForm, result.ID);
                        }
                        Console.WriteLine(System.Environment.NewLine);

                        if (responseToUpsert == null)
                        {
                            // create a new response
                            responseToUpsert = new RegOnlineAPIProxy.APICustomFieldResponse();                            
                            responseToUpsert.CFID = cfID;
                            responseToUpsert.RegistrationID = registrationID;
                            responseToUpsert.EventID = registration.EventID;
                            
                            Console.WriteLine("Registrant {0} {1} does not currently have a selection for this multiple choice custom field.",
                                registration.FirstName, registration.LastName);
                            Console.WriteLine("Select an item from the list above to create a response to and enter the ID followed by [Enter].");                            
                        }
                        else
                        {
                            // existing response found
                            Console.WriteLine("The currently selected multiple choice item is {0} (ID: {1}).", 
                                responseToUpsert.ItemDescription, responseToUpsert.ItemID);
                            Console.WriteLine("Select an item from the list above to update this response to and enter the ID followed by [Enter].");
                        }

                        // get list item to upsert response with
                        int newItemID = 0;
                        do
                        {
                            string providedItemID = Console.ReadLine();

                            if (!int.TryParse(providedItemID, out newItemID) || (newItemID <= 0))
                            {
                                Console.WriteLine("Invalid list item ID.");
                            }

                        } while (newItemID <= 0);

                        // set the Response value to upsert the selected item ID / Description
                        responseToUpsert.Response = newItemID.ToString();

                        RegOnlineAPIProxy.ResultsOfBoolean updateResults =
                            service.UpdateCustomFieldResponsesForRegistration(null, registration.EventID, registrationID, new RegOnlineAPIProxy.APICustomFieldResponse[] { responseToUpsert });

                        if (updateResults.Success)
                        {
                            Console.WriteLine("UpdateCustomFieldResponsesForRegistration succeeded.");
                        }
                    }
                }
            }
        }
    }
}