#region Namespaces
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using QnABot.Common;
using QnABot.Helpers; 

#endregion

namespace QnABot.Dialog
{
    public class TopLevelDialog : ComponentDialog
    {
        // Define value names for values tracked inside the dialogs.
        private const string UserInfo = "value-userInfo";
        private const string SiteUrl = "https://myteamcare.org";

        public TopLevelDialog()
            : base(nameof(TopLevelDialog))
        {
            InitialDialogId = nameof(TopLevelDialog);

            //AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(FlowState.UserType.ToString()));
            AddDialog(new ChoicePrompt(FlowState.CommonQuestions.ToString()));
            AddDialog(new ChoicePrompt(FlowState.NavOptions.ToString()));
            AddDialog(new ChoicePrompt(FlowState.HowTos.ToString()));
            //AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));
            //AddDialog(new ReviewSelectionDialog());

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                UserTypeStepAsync,
                CommonQuestionsStepAsync,
                CommonNavOptionStepAsync,
                HowToStepAsync
                //AgeStepAsync,
                //StartSelectionStepAsync,
                //AcknowledgementStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> UserTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create an object in which to collect the user's information within the dialog.
            stepContext.Values[UserInfo] = new UserProfile();
            var options = SiteHelper.GetUserTypeChoices();

            //prompt for user type
            return await stepContext.PromptAsync(FlowState.UserType.ToString(), options, cancellationToken);
        }

        private static async Task<DialogTurnResult> CommonQuestionsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Set the user's name to what they entered in response to the name prompt.
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            var userType = stepContext.Context.Activity.Text?.Trim();

            userProfile.IsMember = !string.IsNullOrWhiteSpace(userType) &&
                                   userType.Equals("member", StringComparison.InvariantCultureIgnoreCase);

            // Take the input from the user and create the appropriate response.
            var commonQuestions = await SiteHelper.GetCommonQuestionsAsync(SiteUrl, userProfile.IsMember);
            if (!string.IsNullOrWhiteSpace(commonQuestions))
            {
                // Respond to the user.
                await stepContext.Context.SendActivityAsync(commonQuestions, cancellationToken: cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private static async Task<DialogTurnResult> CommonNavOptionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //get common nav options for all user
            var options = SiteHelper.GetCommonNavOptions();

            //prompt for navigation options to take next.
            return await stepContext.PromptAsync(FlowState.NavOptions.ToString(), options, cancellationToken);
        }

        private static async Task<DialogTurnResult> HowToStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            var selectedNavOption = stepContext.Context.Activity.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(selectedNavOption) &&
                selectedNavOption.Equals("Show How To", StringComparison.InvariantCultureIgnoreCase))
            {
                //send how to items
                var actions = await SiteHelper.GetHowToActionsAsync(SiteUrl, userProfile.IsMember);
                await stepContext.Context.SendActivityAsync(actions, cancellationToken);
            }
            else
            {
                var text = MessageFactory.Text("This selection has been developed.");
                await stepContext.Context.SendActivityAsync(text, cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        //private async Task<DialogTurnResult> StartSelectionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    // Set the user's age to what they entered in response to the age prompt.
        //    var userProfile = (UserProfile)stepContext.Values[UserInfo];
        //    userProfile.Age = (int)stepContext.Result;

        //    if (userProfile.Age < 25)
        //    {
        //        // If they are too young, skip the review selection dialog, and pass an empty list to the next step.
        //        await stepContext.Context.SendActivityAsync(
        //            MessageFactory.Text("You must be 25 or older to participate."),
        //            cancellationToken);
        //        return await stepContext.NextAsync(new List<string>(), cancellationToken);
        //    }
        //    else
        //    {
        //        // Otherwise, start the review selection dialog.
        //        return await stepContext.BeginDialogAsync(nameof(ReviewSelectionDialog), null, cancellationToken);
        //    }
        //}

        //private async Task<DialogTurnResult> AcknowledgementStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    // Set the user's company selection to what they entered in the review-selection dialog.
        //    var userProfile = (UserProfile)stepContext.Values[UserInfo];
        //    userProfile.CompaniesToReview = stepContext.Result as List<string> ?? new List<string>();

        //    // Thank them for participating.
        //    await stepContext.Context.SendActivityAsync(
        //        MessageFactory.Text($"Thanks for participating, {((UserProfile)stepContext.Values[UserInfo]).Name}."),
        //        cancellationToken);

        //    // Exit the dialog, returning the collected user information.
        //    return await stepContext.EndDialogAsync(stepContext.Values[UserInfo], cancellationToken);
        //}
    }
}