# LlamaBot Extended Features

This document outlines the additional features and enhancements that LlamaBot provides beyond the core Llama.cpp functionality.

---

## Overview

LlamaBot is a C# wrapper and extension framework built on top of Llama.cpp that provides:
- Advanced custom sampling algorithms
- Discord bot integration
- Multi-character support with configurable chat templates
- Sophisticated repetition and presence penalty systems
- Dynamic entropy-based sampling strategies

---

## Advanced Sampling Algorithms

### 1. Targeted Entropy Sampler
**File:** `LlamaNative/Sampling/Samplers/Mirostat/TargetedEntropySampler.cs`

A sampler that dynamically adjusts token selection to maintain a target probability/entropy level:

- **Adaptive Target Calculation**: Uses a sliding window (`QueueSize`) of recently selected tokens to compute the next target probability
- **Negative Feedback Loop**: Automatically adjusts target to converge on the configured entropy level
- **Word Preservation**: Can be configured to greedily select word continuations above a certain probability threshold

### 2. Targeted Temperature Sampler
**File:** `LlamaNative/Sampling/Samplers/Mirostat/TargetedTemperatureSampler.cs`

An advanced temperature-based sampler with dynamic adjustment:

- **Per-Token Temperature Scaling**: Applies different temperature adjustments based on distance from target probability
- **Scale-Based Distribution**: Uses exponential scaling to create a distribution centered around the target
- **Tail-Free Sampling Integration**: Combines with TFS (Tail-Free Sampling) for improved token selection

### 3. Power Law Targeted Sampler
**File:** `LlamaNative/Sampling/Samplers/Mirostat/PowerLawTargetedSampler.cs`

Uses a Lorentzian (Power Law) distribution to reshape candidates:

```
Key Parameters:
- DistributionWidth: Controls the sharpness of the peak (default: 0.3)
- PeakLogitValue: Maximum logit value at target probability (default: 5.0)
- TailHeaviness: Controls falloff rate; lower values = heavier tails (default: 2.0, Cauchy distribution)
- TailDecay: Decay factor for running average (default: 0.65)
```

### 4. Unbounded Quadratic Sampler
**File:** `LlamaNative/Sampling/Samplers/Mirostat/UnboundedQuadraticSampler.cs`

The most sophisticated sampler, using an adaptive sharpness formula:

```
Formula: Logit = PEAK - SHARPNESS * dist² / (1 + |dist|)
```

**Key Advantages:**
- **Quadratic near target, linear far away**: Provides smooth transition from peaked to gradual falloff
- **Unbounded negative logits**: Unlike Power Law, allows for proper exponential suppression after softmax
- **No floor**: Logits approach -∞ as distance increases

```
Key Parameters:
- Sharpness: Controls steepness of falloff (default: 10.0)
- DistributionWidth: Width of the distribution (default: 0.3)
- TailDecay: Exponential weighted average decay (default: 0.65)
```

### 5. Enhanced Mirostat v1 Implementation
**File:** `LlamaNative/Sampling/Samplers/Mirostat/MirostatOneSampler.cs`

An enhanced Mirostat v1 implementation with:

- **Word Preservation Mode**: Option to only use top-k sampling for new words, not continuations
- **Per-Channel State Isolation**: Mu value and word cache stored in settings object for multi-conversation support

---

## Repetition and Presence Penalties

### 1. Complex Presence Sampler
**File:** `LlamaNative/Sampling/Samplers/FrequencyAndPresence/ComplexPresenceSampler.cs`

A sophisticated repetition penalty system that considers sequence patterns:

- **Group-Based Penalties**: Penalizes tokens that would continue repeated patterns
- **Length-Based Scaling**: Penalty increases with the length of the repeated sequence
- **Parallel Processing**: Uses multi-threaded processing for efficiency
- **Minimum Group Length**: Configurable threshold for what constitutes a "repetition"

```
Parameters:
- MinGroupLength: Minimum sequence length to consider a repetition
- GroupScale: Multiplier per repeated group found
- LengthScale: Multiplier per token in the repeated sequence
- RepeatTokenPenaltyWindow: Number of recent tokens to consider
```

### 2. Subsequence Blocking Sampler
**File:** `LlamaNative/Sampling/Samplers/Repetition/SubsequenceBlockingSampler.cs`

Prevents the model from starting responses with previously used starting patterns:

- **Pattern Detection**: Identifies when the current context ends with a configurable pattern (e.g., assistant header)
- **Token Banning**: Bans tokens that historically followed the same pattern
- **Exclusion List**: Configurable tokens that are exempt from blocking
- **Response Start Block**: Limits how many "starter" tokens to ban

### 3. Repetition Blocking Sampler
**File:** `LlamaNative/Sampling/Samplers/Repetition/RepetitionBlockingSampler.cs`

Simple configurable maximum repetition limit for tokens.

---

## Character Set Filtering

**File:** `LlamaNative/Sampling/Samplers/CharacterSetSampler.cs`

Restricts output to specific character sets:

- **Whitelist/Blacklist Mode**: Can include or exclude specific character sets
- **English Character Set**: Currently supports English (Latin + Latin-1 Supplement)
- **Vocabulary Caching**: Pre-computes which tokens contain non-allowed characters

---

## Dynamic Sampler Base Infrastructure

**File:** `LlamaNative/Sampling/Samplers/Mirostat/BaseDynamicSampler.cs`

A sophisticated base class providing:

### Word Preservation System
- **Word Completion Detection**: Identifies tokens that continue existing words vs. start new ones
- **PascalCase Handling**: Recognizes capitalized mid-sentence words as new word starts
- **PreserveWordMaxP**: Above this probability, word continuations are greedily selected
- **PreserveWordMinP**: Minimum probability cutoff for word continuation candidates

### Selection History Management
- **Queue-Based History**: Maintains a sliding window of recently selected token probabilities
- **Running Average Calculation**: Used by dynamic samplers to adjust behavior

### Candidate Filtering
- **Min-P Enforcement**: Both on original and post-sampler probabilities
- **Per-Token MinP**: Configurable minimum probability per specific token ID
- **Guaranteed Fallback**: Always keeps at least one candidate

---

## Logit Manipulation System

**Directory:** `LlamaNative/Logit/Models/`

### LogitBias
Additive or multiplicative bias applied to specific token logits:
- **Lifetime Management**: Temporary or persistent biases
- **Bias Types**: Support for different bias application methods

### LogitClamp
Constrains logit values:
- **PreventChange**: Locks logit to initial value
- **PreventDecrease**: Only allows logit increases
- **PreventIncrease**: Only allows logit decreases

### LogitPenalty
Applies multiplicative penalties to tokens.

---

## Chat System

### ChatTemplate
**File:** `LlamaNative.Chat/Models/ChatTemplate.cs`

Flexible chat template system supporting:

- **Role-Specific Headers**: Separate templates for User, Assistant, System messages
- **Think Headers**: Support for "thinking" prompts for reasoning models
- **Message Prefix/Suffix**: Customizable message wrapping
- **Stop Token Configuration**: Per-template stop token definitions
- **Masked Strings**: Token masking for training/evaluation purposes

### ChatContext
**File:** `LlamaNative.Chat/Models/ChatContext.cs`

Full conversation management:

- **Message Splitting**: Automatic splitting of long responses
- **Context Refresh**: Efficient context management for long conversations
- **Interrupt Support**: Ability to interrupt ongoing generation
- **User Prediction**: Predicts likely next user based on conversation pattern

### Channel Settings
**File:** `LlamaNative.Chat/Models/ChannelSettings.cs`

Per-channel configuration:

- **Custom Prompts**: Override prompts per channel
- **Name Overrides**: User display name customization
- **Per-User Thoughts**: Hidden context that influences bot behavior per user
- **Serializable Sampler Settings**: JSON-serialized sampler configuration per channel

---

## Discord Integration

**Directory:** `LlamaBot/Discord/`

Full Discord bot implementation with:

- **LlamaBotClient**: Main bot client handling message processing
- **Multi-Character Support**: Multiple character configurations with different models/templates
- **Auto-Respond Mode**: Configurable automatic response behavior
- **Command System**: Slash commands for bot control

### Available Commands
- `/avatar` - Set bot avatar
- `/clear` - Clear conversation context
- `/clone` - Clone character configuration
- `/continue` - Continue generation
- `/delete` - Delete messages
- `/download` - Download conversation
- `/generate` - Generate response
- `/interrupt` - Stop generation
- `/name` - Set display name
- `/prompt` - Set/view prompt
- `/regenerate` - Regenerate last response
- `/resend` - Resend with modifications
- `/setting` - Modify settings
- `/think` - Set thinking prompt
- `/tokenize` - Show tokenization

---

## Multi-Model Support

**Directory:** `LlamaBot/Characters/`

Pre-configured character templates for various models:
- Cohere
- GLM (GLM 4.6, 4.7)
- Google (Gemma)
- Llama3, Llama4
- Mistral
- NVIDIA
- Qwen

Each includes appropriate chat templates, sampling settings, and model-specific configurations.

---

## Min-P Implementation

**File:** `LlamaNative/Sampling/Samplers/MinPSampler.cs`

Enhanced Min-P sampling with:

- **Original Probability Enforcement**: Min-P applied to original (pre-sampling) probabilities
- **Per-Token MinPs**: Dictionary of token-specific minimum probabilities
- **Efficient Candidate Trimming**: In-place array manipulation for performance
- **Top Token Protection**: Never trims the most likely token

---

## Sampler Testing Framework

**Directory:** `LlamaBot.SamplerTest/`

Automated testing framework for evaluating samplers:

- **Anthropic Integration**: Uses Claude to generate test conversations
- **Result Recording**: Saves conversation results as JSON
- **Character-Based Testing**: Tests different character configurations
- **Conversation Orchestration**: Automated multi-turn conversation testing

---

## Key Configuration Classes

### BaseDynamicSamplerSettings
Common settings for dynamic samplers:
```csharp
- FactorPreservedWords: Include preserved words in target calculation
- GreedyExclude/GreedyInclude: Token-specific greedy sampling rules
- MaxPs/MinPs: Per-token probability thresholds
- PreserveWordMaxP/MinP: Word preservation thresholds
- QueueSize: History size for dynamic adjustment
```

### TargetedEntropySamplerSettings
```csharp
- Target: Base target probability (default: 0.4)
- MinTarget/MaxTarget: Clamp range for calculated target
```

### UnboundedQuadraticSamplerSettings
```csharp
- Sharpness: Distribution sharpness (default: 10.0)
- DistributionWidth: Peak width (default: 0.3)
- PeakLogitValue: Maximum logit at target (default: 5.0)
- TailDecay: Running average decay (default: 0.65)
```

---

## Summary

LlamaBot significantly extends Llama.cpp with:

1. **5+ Custom Sampling Algorithms**: Including novel approaches like Unbounded Quadratic and Power Law samplers
2. **Sophisticated Repetition Control**: Pattern-aware penalties and subsequence blocking
3. **Word-Aware Sampling**: Preserves word integrity during generation
4. **Discord Bot Framework**: Full-featured bot with multi-character support
5. **Flexible Chat Templates**: Support for various model formats and roles
6. **Per-Channel State**: Isolated sampler state for concurrent conversations
7. **Testing Framework**: Automated sampler evaluation using Claude
8. **Character Set Filtering**: Language/character restrictions
9. **Dynamic Logit Manipulation**: Bias, clamp, and penalty systems

These features make LlamaBot particularly suitable for:
- Creative writing applications
- Multi-user chat environments
- Applications requiring consistent personality/style
- Experiments with novel sampling strategies
- Production Discord bot deployments
