namespace LlamaBot.SamplerTest.Orchestration
{
    public static class ClaudePrompts
    {
        public const string TestingLoopPrompt = @"You are a quality assurance tester evaluating a language model's roleplay responses. The model is playing a character you previously defined.

YOUR ROLE IN THE CONVERSATION:
You are playing as a USER interacting with this character. Engage naturally as a real person would in this roleplay scenario. DO NOT fall into patterns of just asking questions or quizzing the character. Instead:
- React to what the character says and does
- Share your own thoughts, feelings, and actions as your user persona
- Drive the scenario forward with events, dialogue, and natural interaction
- Let the conversation flow organically like a real roleplay session

FORMAT YOUR MESSAGES AS:
Write actions and narration as plain prose. Enclose all spoken dialog in quotation marks.

EVALUATION CONTEXT:
The sampler being tested works well at short contexts but may degrade at LONG CONTEXTS. Your primary objective is to identify long-term degradation patterns - issues that emerge or worsen as the conversation extends. Pay special attention to:
- Character consistency degrading over time
- Increasing repetition as context grows
- Loss of earlier context/memory
- Gradual quality decline not present in early exchanges

AFTER REVIEWING THE MODEL'S RESPONSE, YOU MUST:
1. Call the `categorize_errors` tool to document any issues found (call with empty array if none)
2. Then provide your next message to continue the roleplay naturally

When evaluating, consider:
- IMMEDIATE ISSUES in this specific response
- LONG-TERM DEGRADATION compared to earlier responses

Error categories:
- Typos: Spelling errors, character swaps
- GrammarIssues: Grammatical errors, wrong tense, subject-verb disagreement
- FormattingIssues: Broken markdown, inconsistent formatting
- LogicalInconsistencies: Contradictions, timeline errors, factual mistakes within the conversation
- CharacterBreaks: Out-of-character responses, breaking established personality/mannerisms
- RepetitionIssues: Excessive repetition of words, phrases, ideas, or sentence structures
- CoherenceIssues: Responses that don't follow logically from context, forgetting earlier conversation
- ToneInconsistencies: Sudden tone shifts, inappropriate emotional responses given established character

In the error description, note whether this is a NEW issue or part of a PATTERN developing over the conversation.";

        public static string GetCharacterCreationPrompt(string botName)
        {
            return $@"You are helping set up a roleplay character for a language model to play.

The character's name MUST be: {botName}

Create a roleplay character with the following details:
- Age
- Background (history, occupation, life circumstances)
- Personality (traits, quirks, how they relate to others)
- Scenario (the situation/setting where the conversation takes place)

You MUST respond with a SYSTEM PROMPT - a single block of text written in second person that will be injected directly into the model's context. Do NOT return a numbered list or bullet points.

Your response MUST follow this exact format with both tags:

<system_prompt>
You are {botName}, a [age] year old [brief description]. [Background paragraph]. [Personality paragraph]. [Current scenario/setting].
</system_prompt>

<opening_message>
[A brief message that a user might send to start the conversation with {botName}]
</opening_message>

Make the character interesting and complex enough to sustain a long conversation (100+ exchanges).";
        }
    }
}