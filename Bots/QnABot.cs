// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Html2Markdown;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using HtmlAgilityPack;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class QnABot<T> : ActivityHandler where T : Microsoft.Bot.Builder.Dialogs.Dialog
    {
        protected readonly BotState ConversationState;
        protected readonly Microsoft.Bot.Builder.Dialogs.Dialog Dialog;
        protected readonly BotState UserState;
        protected static string CommonQuestionsMember;
        protected static string CommonQuestionsProvider;
        protected static Dictionary<string, string> HowToForMember;
        protected static Dictionary<string, string> HowToForProvider;

        public QnABot(ConversationState conversationState, UserState userState, T dialog)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            // Extract the text from the message activity the user sent.
            var text = turnContext.Activity.Text.ToLowerInvariant();

            // Take the input from the user and create the appropriate response.
            var responseText = await ProcessInputAsync(text);
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                // Respond to the user.
                await turnContext.SendActivityAsync(responseText, cancellationToken: cancellationToken);

                //send follow-up prompts
                var actions = await GetHowToActionsAsync(responseText.Equals("member"));
                await turnContext.SendActivityAsync(actions, cancellationToken: cancellationToken);
            }
            else
            {
                // Run the Dialog with the new message Activity.
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);
                }
            }
        }

        private static async Task<string> ProcessInputAsync(string text)
        {
            switch (text)
            {
                case "provider":
                    {
                        return string.IsNullOrWhiteSpace(CommonQuestionsProvider) ? await GetCommonQuestionsAsync(false) : CommonQuestionsProvider;
                    }
                case "member":
                    {
                        return string.IsNullOrWhiteSpace(CommonQuestionsMember) ? await GetCommonQuestionsAsync() : CommonQuestionsMember;
                    }
            }

            return null;
        }

        // Creates and sends an activity with suggested actions to the user. When the user
        /// clicks one of the buttons the text value from the "CardAction" will be
        /// displayed in the channel just as if the user entered the text. There are multiple
        /// "ActionTypes" that may be used for different situations.
        private static async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            //var commonQuestions = string.IsNullOrWhiteSpace(_commonQuestions) ? await GetCommonQuestionsAsync() : _commonQuestions;
            //var reply = MessageFactory.Text(commonQuestions);
            var reply = GetInitialUserActions();
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private static Activity GetInitialUserActions()
        {
            var reply = MessageFactory.Text("Hello, please tell me who you are so I can better assist you.");
            reply.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>
                {
                    new CardAction { Title = "Member", Type = ActionTypes.ImBack, Value = "Member" },
                    new CardAction { Title = "Provider", Type = ActionTypes.ImBack, Value = "Provider" }
                }
            };

            return reply;
        }

        private static async Task<Activity> GetHowToActionsAsync(bool isMember = true)
        {
            var reply = MessageFactory.Text("Hello member, please choose from options below.");
            var actions = await GetHowToDataAsync(isMember);
            var cardActions = actions.Select(action => new CardAction
            {
                Title = action.Key, Type = ActionTypes.OpenUrl, Value = action.Value
            }).ToList();

            reply.SuggestedActions = new SuggestedActions
            {
                Actions = cardActions
            };

            return reply;
        }

        private static async Task<string> GetCommonQuestionsAsync(bool isMember = true)
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
