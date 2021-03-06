﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Dialogs.Tests
{
    [TestClass]
    [TestCategory("Prompts")]
    [TestCategory("Choice Prompts")]
    public class ChoicePromptTests
    {
        private List<Choice> colorChoices = new List<Choice> {
            new Choice { Value = "red" },
            new Choice { Value = "green" },
            new Choice { Value = "blue" }
        };

        private Action<IActivity> StartsWithValidator(string expected)
        {
            return activity =>
            {
                Assert.IsInstanceOfType(activity, typeof(IMessageActivity));
                var msg = (IMessageActivity)activity;
                Assert.IsTrue(msg.Text.StartsWith(expected));
            };
        }

        private Action<IActivity> SuggestedActionsValidator(string expectedText, SuggestedActions expectedSuggestedActions)
        {
            return activity =>
            {
                Assert.IsInstanceOfType(activity, typeof(IMessageActivity));
                var msg = (IMessageActivity)activity;
                Assert.AreEqual(expectedText, msg.Text);
                Assert.AreEqual(expectedSuggestedActions.Actions.Count, msg.SuggestedActions.Actions.Count);
                for (var i = 0; i < expectedSuggestedActions.Actions.Count; i++)
                {
                    Assert.AreEqual(expectedSuggestedActions.Actions[i].Type, msg.SuggestedActions.Actions[i].Type);
                    Assert.AreEqual(expectedSuggestedActions.Actions[i].Value, msg.SuggestedActions.Actions[i].Value);
                    Assert.AreEqual(expectedSuggestedActions.Actions[i].Title, msg.SuggestedActions.Actions[i].Title);
                }
            };
        }

        private Action<IActivity> SpeakValidator(string expectedText, string expectedSpeak)
        {
            return activity =>
            {
                Assert.IsInstanceOfType(activity, typeof(IMessageActivity));
                var msg = (IMessageActivity)activity;
                Assert.AreEqual(expectedText, msg.Text);
                Assert.AreEqual(expectedSpeak, msg.Speak);
            };
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ChoicePromptWithEmptyIdShouldFail()
        {
            var emptyId = "";
            var choicePrompt = new ChoicePrompt(emptyId);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ChoicePromptWithNullIdShouldFail()
        {
            var nullId = "";
            nullId = null;
            var choicePrompt = new ChoicePrompt(nullId);
        }

        [TestMethod]
        public async Task ShouldSendPrompt()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(convoState);

            // Create new DialogSet.
            var dialogs = new DialogSet(dialogState);
            dialogs.Add(new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English));

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext);

                var results = await dc.ContinueAsync();
                if (!turnContext.Responded && !results.HasActive && !results.HasResult)
                {
                    await dc.PromptAsync("ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                            Choices = colorChoices
                        });
                }
            })
            .Send("hello")
            .AssertReply(StartsWithValidator("favorite color?"))
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ShouldSendPromptAsAnInlineList()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(convoState);

            var dialogs = new DialogSet(dialogState);
            dialogs.Add(new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English));

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext);

                var results = await dc.ContinueAsync();
                if (!turnContext.Responded && !results.HasActive && !results.HasResult)
                {
                    await dc.PromptAsync("ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                            Choices = colorChoices
                        });
                }
            })
            .Send("hello")
            .AssertReply("favorite color? (1) red, (2) green, or (3) blue")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ShouldSendPromptAsANumberedList()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(convoState);

            var dialogs = new DialogSet(dialogState);

            // Create ChoicePrompt and change style to ListStyle.List which affects how choices are presented.
            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English);
            listPrompt.Style = ListStyle.List;
            dialogs.Add(listPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext);

                var results = await dc.ContinueAsync();
                if (!turnContext.Responded && !results.HasActive && !results.HasResult)
                {
                    await dc.PromptAsync("ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                            Choices = colorChoices
                        });
                }
            })
            .Send("hello")
            .AssertReply("favorite color?\n\n   1. red\n   2. green\n   3. blue")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ShouldSendPromptUsingSuggestedActions()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(convoState);

            var dialogs = new DialogSet(dialogState);
            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English);
            listPrompt.Style = ListStyle.SuggestedAction;
            dialogs.Add(listPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext);

                var results = await dc.ContinueAsync();
                if (!turnContext.Responded && !results.HasActive && !results.HasResult)
                {
                    await dc.PromptAsync("ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                            Choices = colorChoices
                        });
                }
            })
            .Send("hello")
            .AssertReply(SuggestedActionsValidator("favorite color?",
                new SuggestedActions
                {
                    Actions = new List<CardAction>
                    {
                        new CardAction { Type="imBack", Value="red", Title="red" },
                        new CardAction { Type="imBack", Value="green", Title="green" },
                        new CardAction { Type="imBack", Value="blue", Title="blue" },
                    }
                }))
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ShouldSendPromptWithoutAddingAList()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(convoState);

            var dialogs = new DialogSet(dialogState);

            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English);
            listPrompt.Style = ListStyle.None;
            dialogs.Add(listPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext);

                var results = await dc.ContinueAsync();
                if (!turnContext.Responded && !results.HasActive && !results.HasResult)
                {
                    await dc.PromptAsync("ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                            Choices = colorChoices
                        });
                }
            })
            .Send("hello")
            .AssertReply("favorite color?")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ShouldSendPromptWithoutAddingAListButAddingSsml()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(convoState);

            var dialogs = new DialogSet(dialogState);

            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English);
            listPrompt.Style = ListStyle.None;
            dialogs.Add(listPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext);

                var results = await dc.ContinueAsync();
                if (!turnContext.Responded && !results.HasActive && !results.HasResult)
                {
                    await dc.PromptAsync("ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity {
                                Type = ActivityTypes.Message,
                                Text = "favorite color?",
                                Speak = "spoken prompt"
                            },
                            Choices = colorChoices
                        });
                }
            })
            .Send("hello")
            .AssertReply(SpeakValidator("favorite color?", "spoken prompt"))
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ShouldRecognizeAChoice()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(convoState);

            var dialogs = new DialogSet(dialogState);

            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English);
            listPrompt.Style = ListStyle.None;
            dialogs.Add(listPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext);

                var results = await dc.ContinueAsync();
                if (!turnContext.Responded && !results.HasActive && !results.HasResult)
                {
                    await dc.PromptAsync("ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                            Choices = colorChoices
                        });
                }
                else if (!results.HasActive && results.HasResult)
                {
                    var choiceResult = (FoundChoice)results.Result;
                    await turnContext.SendActivityAsync($"{choiceResult.Value}");
                }
            })
            .Send("hello")
            .AssertReply(StartsWithValidator("favorite color?"))
            .Send("red")
            .AssertReply("red")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ShouldNOTrecognizeOtherText()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(convoState);

            var dialogs = new DialogSet(dialogState);
            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English);
            listPrompt.Style = ListStyle.None;
            dialogs.Add(listPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext);

                var results = await dc.ContinueAsync();
                if (!turnContext.Responded && !results.HasActive && !results.HasResult)
                {
                    await dc.PromptAsync("ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                            RetryPrompt = new Activity { Type = ActivityTypes.Message, Text = "your favorite color, please?" },
                            Choices = colorChoices
                        });
                }
            })
            .Send("hello")
            .AssertReply(StartsWithValidator("favorite color?"))
            .Send("what was that?")
            .AssertReply("your favorite color, please?")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ShouldCallCustomValidator()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(convoState);

            var dialogs = new DialogSet(dialogState);

            PromptValidator<FoundChoice> validator = async (context, promptContext) =>
            {
                await context.SendActivityAsync("validator called");
            };
            var listPrompt = new ChoicePrompt("ChoicePrompt", validator, Culture.English);
            listPrompt.Style = ListStyle.None;
            dialogs.Add(listPrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext);

                var results = await dc.ContinueAsync();
                if (!turnContext.Responded && !results.HasActive && !results.HasResult)
                {
                    await dc.PromptAsync("ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                            Choices = colorChoices
                        });
                }
            })
            .Send("hello")
            .AssertReply(StartsWithValidator("favorite color?"))
            .Send("I'll take the red please.")
            .AssertReply("validator called")
            .StartTestAsync();
        }

        // TODO: Find purpose of this test and reenable if necessary
        /*[TestMethod]
        public async Task ShouldHandleAnUndefinedRequest()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var testProperty = convoState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter()
                .Use(convoState);

            PromptValidator<ChoiceResult> validator = (ITurnContext context, ChoiceResult result) =>
            {
                Assert.IsTrue(false);
                return Task.CompletedTask;
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());
                var prompt = new ChoicePrompt(Culture.English, validator);
                prompt.Style = ListStyle.None;

                var dialogCompletion = await prompt.ContinueAsync(turnContext, state);
                if (!dialogCompletion.IsActive && !dialogCompletion.IsCompleted)
                {
                    await prompt.BeginAsync(turnContext, state,
                        new ChoicePromptOptions
                        {
                            PromptString = "favorite color?",
                            Choices = ChoiceFactory.ToChoices(colorChoices)
                        });
                }
                else if (dialogCompletion.IsActive && !dialogCompletion.IsCompleted)
                {
                    if (dialogCompletion.Result == null)
                    {
                        await turnContext.SendActivityAsync("NotRecognized");
                    }
                }
            })
            .Send("hello")
            .AssertReply(StartsWithValidator("favorite color?"))
            .Send("value shouldn't have been recognized.")
            .AssertReply("NotRecognized")
            .StartTestAsync();
        }*/
        }
    }
