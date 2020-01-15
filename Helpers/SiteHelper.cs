#region Namespaces

using System;
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
using Microsoft.Extensions.DependencyInjection;

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

        public static async Task<string> GetCommonQuestionsAsync(string siteUrl, bool isMember = true)
        {
            if (string.IsNullOrWhiteSpace(siteUrl))
            {
                throw new ArgumentNullException(nameof(siteUrl));
            }

            using (var httpClient = new HttpClient())
            {
                var requestUrl = $"{siteUrl}/help";
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

        public static async Task<IMessageActivity> GetHowToActionsAsync(string siteUrl, bool isMember = true)
        {
            if (string.IsNullOrWhiteSpace(siteUrl))
            {
                throw new ArgumentNullException(nameof(siteUrl));
            }

            var attachments = new List<Attachment>();
            var reply = MessageFactory.Attachment(attachments);
            
            //MessageFactory.Text("Hello member, please choose from options below.");

            var howToArticles = await GetHowToDataAsync(isMember);
            foreach (var howToArticle in howToArticles)
            {
                var heroCard = new HeroCard
                {
                    Images = new List<CardImage>
                    {
                        new CardImage(howToArticle.Item2, null, new CardAction(ActionTypes.OpenUrl, howToArticle.Item1, value: howToArticle.Item3))
                    }
                }.ToAttachment();

                reply.Attachments.Add(heroCard);
            }

            return reply;
        }

        private static async Task<IEnumerable<Tuple<string, string, string>>> GetHowToDataAsync(bool isMember = true)
        {
            var sb = new StringBuilder();
            var result = new List<Tuple<string, string, string>>();
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
                            //get the img tag
                            var imgElement = element.Element("img");
                            //get display text
                            var displayText = !string.IsNullOrWhiteSpace(element.InnerText) ? element.InnerText.Trim() : element.InnerText;
                            result.Add(Tuple.Create(displayText, imgElement.GetAttributeValue("img", string.Empty),
                                element.GetAttributeValue("href", string.Empty)));
                        }
                    }
                }

                return await Task.FromResult(result);
            }
        }
    }
}
