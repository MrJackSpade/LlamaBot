﻿using LlamaNative.Extensions;
using LlamaNative.Logit.Models;
using System.Collections;

namespace LlamaNative.Logit.Collections
{
    public class LogitRuleCollection : IEnumerable<LogitRule>
    {
        private readonly Dictionary<string, LogitRule> _keyValuePairs = [];

        public LogitRuleCollection()
        {
        }

        private LogitRuleCollection(IEnumerable<LogitRule> toClone)
        {
            this.Add(toClone);
        }

        public void Add(IEnumerable<LogitRule> rules)
        {
            foreach (LogitRule rule in rules)
            {
                this.Add(rule);
            }
        }

        public void Add(LogitRule rule)
        {
            _keyValuePairs.Add(rule.Key, rule);
        }

        public void AddOrUpdate(IEnumerable<LogitRule> rules)
        {
            foreach (LogitRule rule in rules)
            {
                this.AddOrUpdate(rule);
            }
        }

        public void AddOrUpdate(LogitRule rule)
        {
            _keyValuePairs.AddOrUpdate(rule.Key, rule);
        }

        public LogitRuleCollection Clone()
        {
            return new(this);
        }

        public IEnumerator<LogitRule> GetEnumerator()
        {
            return _keyValuePairs.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _keyValuePairs.Values.GetEnumerator();
        }

        public IEnumerable<T> OfType<T>() where T : LogitRule
        {
            return _keyValuePairs.Values.OfType<T>();
        }

        public void Remove(LogitRuleLifetime lifetime)
        {
            HashSet<string> toRemove = [];

            foreach (LogitRule rule in this)
            {
                if (rule.LifeTime == lifetime)
                {
                    toRemove.Add(rule.Key);
                }
            }

            foreach (string key in toRemove)
            {
                _keyValuePairs.Remove(key);
            }
        }

        public void Remove(string key)
        {
            _keyValuePairs.Remove(key);
        }
    }
}