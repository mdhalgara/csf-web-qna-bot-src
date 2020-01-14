#region Namespaces
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using QnABot.Helpers; 

#endregion

namespace QnABot.Bots
{
    public class DialogAndWelcomeBot<T> : DialogBot<T> where T : Microsoft.Bot.Builder.Dialogs.Dialog
    {
        public DialogAndWelcomeBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, dialog, logger)
        {
        }

        protected override async Task OnMembersAddedAsync(
            IList<ChannelAccount> membersAdded,
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    //intentionally not do anything.
                    //var reply = SiteHelper.GetUserTypeChoices();
                    //await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            }
        }
    }
}
