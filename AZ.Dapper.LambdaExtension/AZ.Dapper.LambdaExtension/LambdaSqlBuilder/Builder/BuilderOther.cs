﻿using System;
using System.ComponentModel;
using Dapper.LambdaExtension.LambdaSqlBuilder.Resolver.ExpressionTree;

namespace Dapper.LambdaExtension.LambdaSqlBuilder.Builder
{
    partial class Builder
    {
        public void Join(string originalTableName, string joinTableName, string leftField, string rightField,JoinType joinType)
        {
            var joinTypeStr = "JOIN";
            switch (joinType)
            {
                case JoinType.InnerJoin:
                    joinTypeStr = "INNER JOIN";
                    break;
                case JoinType.LeftJoin:
                    joinTypeStr = "LEFT JOIN";
                    break;
                case JoinType.RightJoin:
                    joinTypeStr = "RIGHT JOIN";
                    break;
                default:
                    joinTypeStr = "JOIN";
                    break;
            }

            var joinString = string.Format("{3} {0} ON {1} = {2}",
                                           _adapter.Table(joinTableName,""),
                                           _adapter.Field(originalTableName, leftField),
                                           _adapter.Field(joinTableName, rightField),joinTypeStr);
            _tableNames.Add(joinTableName);
            _joinExpressions.Add(joinString);
            _splitColumns.Add(rightField);
        }

        public void JoinSub(SqlExpBase sqlExp,string originalTableName, string joinTableName, string leftField, string rightField, JoinType joinType)
        {
            var joinTypeStr = "JOIN";
            switch (joinType)
            {
                case JoinType.InnerJoin:
                    joinTypeStr = "INNER JOIN";
                    break;
                case JoinType.LeftJoin:
                    joinTypeStr = "LEFT JOIN";
                    break;
                case JoinType.RightJoin:
                    joinTypeStr = "RIGHT JOIN";
                    break;
                default:
                    joinTypeStr = "JOIN";
                    break;
            }

            var aliasTname = $"join_" + DateTime.Now.Ticks;

            sqlExp.JoinSubAliasTableName = aliasTname;

            var subQueryStr = sqlExp.SqlString;

            var joinString = string.Format("{3} ({0}) {4} ON {1} = {4}.{2}",
                subQueryStr,
                _adapter.Field(originalTableName, leftField),
                 _adapter.Field(rightField), joinTypeStr,aliasTname);
            _tableNames.Add(joinTableName);
            _joinExpressions.Add(joinString);
            _splitColumns.Add(rightField);
        }

        public void OrderBy(string tableName, string fieldName, bool desc = false)
        {
            var order = $"{tableName}.{_adapter.Field(fieldName)}";
            if (desc) order += " DESC";

            _sortList.Add(order);
        }

        public void Select(string tableName)
        {
            var selectionString = string.Format("{0}.*", _adapter.Table(tableName,_schema));
            _selectionList.Add(selectionString);
        }

        public void Select(string tableName, string fieldName)
        {
            _selectionList.Add(_adapter.Field(tableName, fieldName));
        }

        public void Select(string tableName, string fieldName,string aliasName)
        {
            _selectionList.Add(_adapter.Field(tableName, fieldName)+" AS "+aliasName);
        }

        public void Select(string tableName, string fieldName, SelectFunction selectFunction, string aliasName)
        {
            string name = string.IsNullOrEmpty(aliasName) ? fieldName : aliasName;
            name = _adapter.Field(name);

            var fname = fieldName;
            if (fieldName != "*")
            {
                fname = _adapter.Field(tableName, fieldName);
            }
            var selectionString = string.Format("{0}({1}) AS {2}", selectFunction.ToString(), fname, name);
            _selectionList.Add(selectionString);
        }

        public void GroupBy(string tableName, string fieldName)
        {
            _groupingList.Add(_adapter.Field(tableName, fieldName));
        }
    }

  
}
