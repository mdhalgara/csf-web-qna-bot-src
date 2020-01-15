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

        public static PromptOptions GetCommonNavOptions()
        {
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text("Hello member, please choose from below options."),
                Choices = new List<Choice>
                {
                    new Choice("Show How To"),
                    new Choice("Login"),
                    new Choice("Forgot Username"),
                    new Choice("Forgot Password"),
                    new Choice("Contact Customer Care")
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

            var howToArticles = await GetHowToDataAsync(siteUrl, isMember);
            foreach (var howToArticle in howToArticles)
            {
                var heroCard = new HeroCard
                {
                    Title = howToArticle.Item1,
                    Images = new List<CardImage>
                    {
                        new CardImage(howToArticle.Item2)
                    },
                    Tap = new CardAction(ActionTypes.OpenUrl, howToArticle.Item1, value: howToArticle.Item3)
                };

                reply.Attachments.Add(heroCard.ToAttachment());
            }

            return reply;
        }

        private static async Task<IEnumerable<Tuple<string, string, string>>> GetHowToDataAsync(string siteUrl, bool isMember = true)
        {
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
                        var linkElements = section.Descendants("a");
                        foreach (var linkElement in linkElements)
                        {
                            //get the img tag
                            var imgElement = linkElement.Element("img");
                            var imageLink = imgElement?.GetAttributeValue("src", string.Empty);
                            var articleLink = linkElement.GetAttributeValue("href", string.Empty);

                            //get display text
                            var displayText = !string.IsNullOrWhiteSpace(linkElement.InnerText) ? linkElement.InnerText.Trim() : linkElement.InnerText;

                            result.Add(Tuple.Create(displayText, !string.IsNullOrWhiteSpace(imageLink) ? $"{siteUrl}{imageLink}" : null,
                                $"{siteUrl}{articleLink}"));
                        }
                    }
                }

                return await Task.FromResult(result);
            }
        }
    }
}
