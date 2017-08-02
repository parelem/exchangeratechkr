using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using RestSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace CashExchange
{
    public static class RestClientExtensions
    {
        public static async Task<RestResponse> ExecuteAsyncExt(this RestClient client, RestRequest request)
        {
            TaskCompletionSource<IRestResponse> taskCompletion = new TaskCompletionSource<IRestResponse>();
            RestRequestAsyncHandle handle = client.ExecuteAsync(request, r => taskCompletion.SetResult(r));
            return (RestResponse)(await taskCompletion.Task);
        }
    }
    public class Function
    {
        /// <summary>
        /// A function that processes the users request to perform an exchange rate calculation
        /// from USD to requested country's currency
        /// </summary>
        /// <param name="input">Alexa skill request</param>
        /// <param name="context">AWS Lambda context</param>
        /// <returns></returns>
        public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            SkillResponse response = new SkillResponse
            {
                SessionAttributes = new Dictionary<string, object>(),
                Version = "1.0",
                Response = new ResponseBody() { ShouldEndSession = false, OutputSpeech = new PlainTextOutputSpeech() },
            };

            ILambdaLogger log = context.Logger;

            log.Log("Function Handler Begin");
            // check what type of a request it is like an IntentRequest or a LaunchRequest
            var requestType = input.GetRequestType();

            PlainTextOutputSpeech outputMessage = new PlainTextOutputSpeech();
            var text = "Something went wrong";

            outputMessage.Text = text;

            response.Response.OutputSpeech = outputMessage;

            try
            {

                if (input.GetRequestType() == typeof (LaunchRequest))
                {
                    log.LogLine($"Default LaunchRequest made");

                    outputMessage.Text =
                        "Welcome to Exchange rate checker. You can ask us how much dollars are worth in other countries!";
                    response.Response.ShouldEndSession = false;
                }
                else if (requestType == typeof (IntentRequest))
                {
                    log.Log("Get Intent");
                    var intentRequest = input.Request as IntentRequest;
                    log.Log(intentRequest.Intent.Name);

                    switch (intentRequest.Intent.Name)
                    {
                        case BuiltInIntent.Help:
                        {
                            log.Log("Help intent");
                            outputMessage.Text =
                                "Ask how much money is worth in other countries. For example, say How much is five dollars worth in Japan";
                                response.Response.ShouldEndSession = false;
                                break;
                        }
                        case BuiltInIntent.Stop:
                        {
                            log.Log("Stop intent");
                            outputMessage.Text = "Thanks for stopping by";
                            response.Response.ShouldEndSession = true;
                            break;
                        }
                        case BuiltInIntent.Cancel:
                        {
                            log.Log("Cancel intent");
                            outputMessage.Text = "Thanks for stopping by";
                            response.Response.ShouldEndSession = true;
                            break;
                        }
                        default:
                        {
                            string country = "";
                            int amount = default(int);
                            if (input.Session.New)
                            {
                                log.Log("New session found -- checking slots");
                                if (string.IsNullOrEmpty(intentRequest.Intent.Slots["Country"].Value) &&
                                    string.IsNullOrEmpty(intentRequest.Intent.Slots["Amount"].Value))
                                {
                                        log.Log("prompting for full ask");
                                        text =
                                    "Ask how much money is worth in other countries. For example, say How much is five dollars worth in Japan";
                                        response.Response.ShouldEndSession = false;
                                }
                                else if (string.IsNullOrEmpty(intentRequest.Intent.Slots["Country"].Value) ||
                                         string.IsNullOrEmpty(intentRequest.Intent.Slots["Amount"].Value))
                                {
                                    if (string.IsNullOrEmpty(intentRequest.Intent.Slots["Country"].Value))
                                    {
                                        log.Log("prompting for country");
                                        text = "Sorry, I didn't get the country. What country do you want to check conversion for?";
                                            
                                        response.Response.ShouldEndSession = false;
                                    }
                                    else
                                    {
                                        log.Log("adding country to session attributes");
                                        country = intentRequest.Intent.Slots["Country"].Value;
                                        response.SessionAttributes.Add("Country", country);
                                    }

                                    if (string.IsNullOrEmpty(intentRequest.Intent.Slots["Amount"].Value))
                                    {
                                        log.Log("prompting for amount");
                                        text = "Sorry, I didn't get the amount. How much money would you like to convert?";
                                        response.Response.ShouldEndSession = false;
                                    }
                                    else
                                    {
                                        log.Log("adding amount to session attributes");
                                        amount = Convert.ToInt32(intentRequest.Intent.Slots["Amount"].Value);
                                        response.SessionAttributes.Add("Amount", amount);
                                    }
                                }
                                else if(!(string.IsNullOrEmpty(intentRequest.Intent.Slots["Country"].Value) &&
                                    string.IsNullOrEmpty(intentRequest.Intent.Slots["Amount"].Value)))
                                {
                                    log.Log("adding amount to session attributes");
                                    amount = Convert.ToInt32(intentRequest.Intent.Slots["Amount"].Value);
                                    response.SessionAttributes.Add("Amount", amount);
                                    log.Log("adding country to session attributes");
                                    country = intentRequest.Intent.Slots["Country"].Value;
                                    response.SessionAttributes.Add("Country", country);
                                }                                
                            }
                            else
                            {
                                object sessionCountry = null;
                                object sessionAmount = null;

                                if (input.Session.Attributes != null)
                                {
                                    response.SessionAttributes = input.Session.Attributes;
                                    input.Session.Attributes.TryGetValue("Country", out sessionCountry);

                                    input.Session.Attributes.TryGetValue("Amount", out sessionAmount);

                                }

                                if (string.IsNullOrEmpty(intentRequest.Intent.Slots["Country"].Value) &&
                                sessionCountry == null)
                                {
                                    log.Log("prompting for country");
                                    text = "Sorry, I didn't get the country. What country do you want to check conversion for?";

                                    response.Response.ShouldEndSession = false;
                                }
                                else
                                {
                                    log.Log("got country");
                                    country = intentRequest.Intent.Slots["Country"].Value ??
                                              sessionCountry?.ToString();
                                    if (!response.SessionAttributes.ContainsKey("Country"))
                                    {
                                        response.SessionAttributes.Add("Country", country);

                                    }
                                }

                                if (string.IsNullOrEmpty(intentRequest.Intent.Slots["Amount"].Value) &&
                                    sessionAmount == null)
                                {
                                        log.Log("prompting for amount");
                                        text = "Sorry, I didn't get the amount. How much money would you like to convert?";
                                        response.Response.ShouldEndSession = false;
                                    }
                                    else
                                    {
                                        log.Log("got amount");
                                        amount = Convert.ToInt32((intentRequest.Intent.Slots["Amount"].Value ?? sessionAmount?.ToString()));
                                        if (!response.SessionAttributes.ContainsKey("Amount"))
                                        {
                                            response.SessionAttributes.Add("Amount", amount);
                                        }
                                        
                                    }
                            }
                                //country = intentRequest.Intent.Slots["Country"].Value;
                                //amount = Convert.ToInt32(intentRequest.Intent.Slots["Amount"].Value);

                            if (!string.IsNullOrEmpty(country) && amount != default(int))
                            {
                                log.Log("Country: " + country);
                                log.Log("Amount: " + amount);

                                string code = "";
                                string currencyName = "";
                                if (country.Equals("India"))
                                {
                                    code = "INR";
                                    currencyName = "Indian rupee";
                                }
                                else
                                {
                                    RestClient countryClient = new RestClient("https://restcountries.eu/rest/v2/");

                                    RestRequest countryRequest = new RestRequest()
                                    {
                                        Resource = string.Format("name/{0}", country),
                                        RequestFormat = DataFormat.Json
                                    };
                                    log.Log("getting country info");

                                    try
                                    {
                                        var resp = await countryClient.ExecuteAsyncExt(countryRequest);
                                        log.Log(resp.Content);
                                        var countryContent =
                                            JsonConvert.DeserializeObject<List<Country>>(resp.Content).FirstOrDefault();

                                        code = countryContent.currencies.First().code;
                                        currencyName = countryContent.currencies.First().name;
                                    }
                                    catch (Exception ex)
                                    {
                                       log.Log(ex.Message); 
                                    }


                                }
                                log.Log(code);

                                if (!string.IsNullOrEmpty(code))
                                {
                                    try
                                    {
                                        RestClient currencyClient = new RestClient("http://api.fixer.io/");
                                        RestRequest currencyRequest = new RestRequest()
                                        {
                                            Resource = string.Format("latest?base=USD")
                                        };

                                        log.Log("getting currency info");
                                        var exResp = await currencyClient.ExecuteAsyncExt(currencyRequest);
                                        var currencyContent = JsonConvert.DeserializeObject<ExchangeRate>(exResp.Content);
                                        log.Log(exResp.Content);

                                        log.Log("getting value for code");

                                        double multi = 1;

                                        PropertyInfo pinfo = typeof (Rates).GetProperty(code);
                                        if (pinfo != null)
                                        {
                                            object value = pinfo.GetValue(currencyContent.rates, null);
                                            multi = (double) value;
                                        }


                                        var curValue = amount*multi;
                                        log.Log("value: " + curValue);

                                        log.Log("setting text");
                                        text = string.Format("In {0}, {1} dollars is worth {2} {3}", country, amount,
                                            curValue.ToString("F2"),
                                            currencyName);
                                        response.Response.ShouldEndSession = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Log(ex.Message);
                                        text = "Sorry, couldn't find that exchange rate";
                                    }
                                }
                                else
                                {
                                    text =
                                        "Sorry, I didn't understand that country. What country do you want to check conversion for?";
                                    if (response.SessionAttributes.ContainsKey("Country"))
                                    {
                                        response.SessionAttributes.Remove("Country");

                                    }
                                    response.Response.ShouldEndSession = false;
                                }
                            }

                            log.Log("saving text to output");
                            outputMessage.Text = text;

                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Log(ex.Message);
                //log.Log(ex.InnerException.Message);
                log.Log(ex.StackTrace);
                throw;
            }

            response.Response.OutputSpeech = outputMessage;

            log.Log("Function Handler End");

            return response;
        }
    }
}
