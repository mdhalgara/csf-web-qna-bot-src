#region Namespaces
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Html2Markdown;
using HtmlAgilityPack;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

#endregion

namespace QnABot.Helpers
{
    public static class SiteHelper
    {
        private static string CommonQuestionsMember;
        private static string CommonQuestionsProvider;
        private static Dictionary<string, string> HowToForMember;
        private static Dictionary<string, string> HowToForProvider;

        public static PromptOptions GetUserTypeChoices()
        {
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text("Hello, please tell me who you are so I can better assist you."),
                Choices = new List<Choice>
                {
                    new Choice("Member"),
                    new Choice("Provider")
                }, 
                Style = ListStyle.SuggestedAction
            };

            return options;
        }

        public static async Task<string> GetCommonQuestionsAsync(bool isMember = true)
        {
            using (var httpClient = new HttpClient())
            {
                const string requestUrl = "https://myteamcare.org/help";
                var selector = isMember ? "//div[@data-tab-id='members']/div" : "//div[@data-tab-id='providers']/div";

                var response = await httpClient.GetStringAsync(requestUrl);
                if (!string.IsNullOrWhiteSpace(response))
                {
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(response);

                    var content = htmlDocument.DocumentNode.SelectSingleNode(selector);
                    var removeDivChild = content.FirstChild;
                    content.RemoveChild(removeDivChild);

                    response = content.InnerHtml;
                }

                //create a HTML to markdown converter
                var converter = new Converter();
                var markdown = converter.Convert(response);
                return await Task.FromResult(markdown);
            }
        }

        public static async Task<Activity> GetHowToActionsAsync(bool isMember = true)
        {
            var reply = MessageFactory.Text("Hello member, please choose from options below.");
            var actions = await GetHowToDataAsync(isMember);
            var cardActions = actions.Select(action => new CardAction
            {
                Title = action.Key,
                Type = ActionTypes.OpenUrl,
                Value = action.Value
            }).ToList();

            reply.SuggestedActions = new SuggestedActions
            {
                Actions = cardActions
            };

            return reply;
        }

        private static async Task<Dictionary<string, string>> GetHowToDataAsync(bool isMember = true)
        {
            var sb = new StringBuilder();
            var dict = new Dictionary<string, string>();
            using (var httpClient = new HttpClient())
            {
                const string requestUrl = "https://myteamcare.org/help";
                var selector = isMember ? "//div[@id='members']/div/section" : "//div[@id='providers']/div/section";

                var response = await httpClient.GetStringAsync(requestUrl);
                if (!string.IsNullOrWhiteSpace(response))
                {
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(response);

                    var section = htmlDocument.DocumentNode.SelectSingleNode(selector);
                    if (section.HasChildNodes)
                    {
                        var elements = section.Descendants("a");
                        foreach (var element in elements)
                        {
                            //remove img tag
                            var imgElement = element.Element("img");
                            if (imgElement != null)
                            {
                                element.RemoveChild(imgElement);
                            }
                            else
                            {
                                break;
                            }

                            //get display text
                            var displayText = !string.IsNullOrWhiteSpace(element.InnerText) ? element.InnerText.Trim() : element.InnerText;
                            dict.Add(displayText, element.GetAttributeValue("href", string.Empty));
                        }
                    }

                    response = sb.ToString();
                    //create a HTML to markdown converter
                    var converter = new Converter();
                    var markdown = converter.Convert(response);
                }

                return await Task.FromResult(dict);
            }
        }
    }
}
