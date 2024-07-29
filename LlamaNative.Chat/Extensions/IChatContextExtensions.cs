﻿using LlamaNative.Chat.Interfaces;
using LlamaNative.Chat.Models;
using LlamaNative.Tokens.Models;

namespace LlamaNative.Chat.Extensions
{
    public static class IChatContextExtensions
    {
        public static uint CalculateLength(this IChatContext context, string username, string message)
        {
            ChatMessage chatMessage = new(TokenMask.Undefined, username, message);

            return context.CalculateLength(chatMessage);
        }

        public static void Insert(this IChatContext context, int index, TokenMask contentMask, string username, string message, string? externalId = null)
        {
            ChatMessage chatMessage = new(contentMask, username, message, externalId);

            context.Insert(index, chatMessage);
        }

        public static void SendContent(this IChatContext context, TokenMask contentMask, string message, string? externalId = null)
        {
            ChatMessage chatMessage = new(contentMask, null, message, externalId)
            {
                ContentOnly = true
            };

            context.SendMessage(chatMessage);
        }

        public static void SendMessage(this IChatContext context, TokenMask contentMask, string username, string message, string? externalId = null)
        {
            ChatMessage chatMessage = new(contentMask, username, message, externalId);

            context.SendMessage(chatMessage);
        }
    }
}