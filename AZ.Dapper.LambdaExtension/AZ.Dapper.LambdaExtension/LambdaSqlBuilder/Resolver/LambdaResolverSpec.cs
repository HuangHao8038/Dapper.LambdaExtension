﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Dapper.LambdaExtension.Helpers;
using Dapper.LambdaExtension.LambdaSqlBuilder.Builder;
using Dapper.LambdaExtension.LambdaSqlBuilder.Entity;
using Dapper.LambdaExtension.LambdaSqlBuilder.Resolver.ExpressionTree;

namespace Dapper.LambdaExtension.LambdaSqlBuilder.Resolver
{
    partial class LambdaResolver
    {
        public void QueryByIsIn<T>(bool isNot, Expression<Func<T, object>> expression, SqlExpBase sqlQuery)
        {
            var fieldName = GetColumnName(expression);
            _builder.AddCondition(isNot, GetTableName<T>(), fieldName, sqlQuery);
        }

        public void QueryByIsIn<T>(bool isNot, Expression<Func<T, object>> expression, IEnumerable<object> values)
        {
            var fieldName = GetColumnName(expression);
            _builder.AddCondition(isNot, GetTableName<T>(), fieldName, values);
        }

        public void Join<T1, T2>(Expression<Func<T1, T2, bool>> expression, JoinType joinType)
        {
            var joinExpression = GetBinaryExpression(expression.Body);
            var leftExpression = GetMemberExpression(joinExpression.Left);
            var rightExpression = GetMemberExpression(joinExpression.Right);

            Join<T1, T2>(leftExpression, rightExpression,joinType);
        }

        public void Join<T1, T2, TKey>(Expression<Func<T1, TKey>> leftExpression, Expression<Func<T2, TKey>> rightExpression,JoinType joinType)
        {
            Join<T1, T2>(GetMemberExpression(leftExpression.Body), GetMemberExpression(rightExpression.Body),joinType);
        }

        public void Join<T1, T2>(MemberExpression leftExpression, MemberExpression rightExpression,JoinType joinType)
        {
            _builder.Join(GetTableName<T1>(), GetTableName<T2>(), GetColumnName(leftExpression), GetColumnName(rightExpression), joinType);
        }

        public void JoinSubQuery<T1, T2>(SqlExp<T2> t2SqlExp, Expression<Func<T1, T2, bool>> expression, JoinType joinType)
        {
            var joinExpression = GetBinaryExpression(expression.Body);
            var leftExpression = GetMemberExpression(joinExpression.Left);
            var rightExpression = GetMemberExpression(joinExpression.Right);
            JoinSubQuery<T1,T2>(t2SqlExp, leftExpression, rightExpression, joinType);
        }

        public void JoinSubQuery<T1, T2, TKey>(SqlExp<T2> t2SqlExp,Expression<Func<T1, TKey>> leftExpression, Expression<Func<T2, TKey>> rightExpression, JoinType joinType)
        {
            JoinSubQuery<T1, T2>(t2SqlExp,GetMemberExpression(leftExpression.Body), GetMemberExpression(rightExpression.Body), joinType);
        }

        public void JoinSubQuery<T1, T2>(SqlExp<T2> t2SqlExp, MemberExpression leftExpression, MemberExpression rightExpression, JoinType joinType)
        {
            _builder.JoinSub(t2SqlExp,GetTableName<T1>(), GetTableName<T2>(), GetColumnName(leftExpression), GetColumnName(rightExpression), joinType);
        }

        public void SubQuery<T2>(SqlExp<T2> t2SqlExp)
        {
            _builder._isSubQuery = true;
            _builder.QuerySub(t2SqlExp);
        }

        public void OrderBy<T>(Expression<Func<T, object>> expression, bool desc = false)
        {
            var body = expression.Body;
            if (body.NodeType == ExpressionType.New)
            {
                var arguments = (body as NewExpression).Arguments;
                foreach (MemberExpression memberExp in arguments)
                {
                    if (_builder._isSubQuery)
                    {
                        OrderBySubQuery<T>(_builder.JoinSubAliasTableName,memberExp, desc);
                    }
                    else
                    {
                        OrderBy<T>(memberExp, desc);
                    }
                }
                   
            }
            else
            {
                if (_builder._isSubQuery)
                {
                    OrderBySubQuery<T>(_builder.JoinSubAliasTableName, GetMemberExpression(body), desc);
                }
                else
                {
                    OrderBy<T>(GetMemberExpression(body), desc);
                }
            }
        }

        public void OrderBySubQuery<T2>(string subAlias,Expression<Func<T2, object>> expression, bool desc = false)
        {
            var body = expression.Body;
            if (body.NodeType == ExpressionType.New)
            {
                var arguments = (body as NewExpression).Arguments;
                foreach (MemberExpression memberExp in arguments)
                    OrderBySubQuery<T2>(subAlias,memberExp, desc);
            }
            else
            {
                OrderBySubQuery<T2>(subAlias,GetMemberExpression(body), desc);
            }
        }

        internal void OrderBy<T>(MemberExpression expression, bool desc)
        {
            var fieldName = GetColumnName(GetMemberExpression(expression));
            _builder.OrderBy(GetTableName<T>(), fieldName, desc);
        }

        internal void OrderBySubQuery<T2>(string subAlias,MemberExpression expression, bool desc)
        {
            var fieldName = GetColumnName(GetMemberExpression(expression));
            _builder.OrderBy(subAlias, fieldName, desc);
        }

        public void Select<T>(Expression<Func<T, object>> expression)
        {
            Select<T>(expression.Body);
        }
        public void Select<T,TAlias>(Expression<Func<T, object>> expression, Expression<Func<TAlias, object>> aliasExpression)
        {
            var aliasName = GetColumnName(aliasExpression);
            Select<T>(expression.Body, aliasName);
        }

        public void SelectSubQuery<T2>(Expression<Func<T2, object>> expression,string subAlias)
        {
            SelectSubQuery<T2>(expression.Body,subAlias);
        }

        public void Select<T>(Expression<Func<T, SqlColumnEntity>> expression)
        {
            Select<T>(expression.Body);
        }


        private void Select<T>(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Parameter:
                    _builder.Select(GetTableName(expression.Type));
                    break;
                case ExpressionType.Convert:
                case ExpressionType.MemberAccess:
                    Select<T>(GetMemberExpression(expression));
                    break;
                case ExpressionType.New:
                    foreach (MemberExpression memberExp in (expression as NewExpression).Arguments)
                        Select<T>(memberExp);
                    break;
                default:
                    throw new ArgumentException("Invalid expression");
            }
        }

        private void Select<T2>(Expression expression,string alias)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Parameter:
                    _builder.Select(GetTableName(expression.Type));
                    break;
                case ExpressionType.Convert:
                case ExpressionType.MemberAccess:
                    Select<T2>(GetMemberExpression(expression),alias);
                    break;
                case ExpressionType.New:
                    foreach (MemberExpression memberExp in (expression as NewExpression).Arguments)
                        Select<T2>(memberExp,alias);
                    break;
                default:
                    throw new ArgumentException("Invalid expression");
            }
        }

        private void SelectSubQuery<T2>(Expression expression,string subAlias)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Parameter:
                    _builder.Select(subAlias);
                    break;
                case ExpressionType.Convert:
                case ExpressionType.MemberAccess:
                    SelectSubQuery<T2>(GetMemberExpression(expression),subAlias);
                    break;
                case ExpressionType.New:
                    foreach (MemberExpression memberExp in (expression as NewExpression).Arguments)
                        SelectSubQuery<T2>(memberExp,subAlias);
                    break;
                default:
                    throw new ArgumentException("Invalid expression");
            }
        }

        private void Select<T>(MemberExpression expression)
        {

            if (!EnvHelper.IsNetFX)
            {
                if (expression.Type.GetTypeInfo().IsClass && expression.Type != typeof(String))
                {
                    _builder.Select(GetTableName(expression.Type));
                }
                else
                    _builder.Select(GetTableName<T>(), GetColumnName(expression));
            }
            else
            {
                if (expression.Type.GetTypeInfo().IsClass && expression.Type != typeof(String))
                {
                    _builder.Select(GetTableName(expression.Type));
                }
                else
                    _builder.Select(GetTableName<T>(), GetColumnName(expression));
            }

 
        }

        private void Select<T>(MemberExpression expression,string alias)
        {

            if (!EnvHelper.IsNetFX)
            {
                if (expression.Type.GetTypeInfo().IsClass && expression.Type != typeof(String))
                {
                    _builder.Select(GetTableName(expression.Type));
                }
                else
                    _builder.Select(GetTableName<T>(), GetColumnName(expression),alias);
            }
            else
            {
                if (expression.Type.GetTypeInfo().IsClass && expression.Type != typeof(String))
                {
                    _builder.Select(GetTableName(expression.Type));
                }
                else
                    _builder.Select(GetTableName<T>(), GetColumnName(expression),alias);
            }


        }

        private void SelectSubQuery<T2>(MemberExpression expression,string subAlias)
        {

            if (!EnvHelper.IsNetFX)
            {
                if (expression.Type.GetTypeInfo().IsClass && expression.Type != typeof(String))
                {
                    _builder.Select(subAlias);
                }
                else
                    _builder.Select(subAlias, GetColumnName(expression));
            }
            else
            {
                if (expression.Type.GetTypeInfo().IsClass && expression.Type != typeof(String))
                {
                    _builder.Select(subAlias);
                }
                else
                    _builder.Select(subAlias, GetColumnName(expression));
            }


        }

        public void SelectWithFunction<T>(Expression<Func<T, object>> expression, SelectFunction selectFunction, string aliasName)
        {
            var fieldName = "*";

            if (expression != null)
            {
                fieldName = GetColumnName(GetMemberExpression(expression.Body));
            }

            _builder.Select(GetTableName<T>(), fieldName, selectFunction, aliasName);
        }

        public void SelectWithFunction<T>(Expression<Func<T, object>> expression, SelectFunction selectFunction, string aliasName,string tableAlias)
        {
            var fieldName = "*";

            if (expression != null)
            {
                fieldName = GetColumnName(GetMemberExpression(expression.Body));
            }

            _builder.Select(tableAlias, fieldName, selectFunction, aliasName);
        }

        public void SelectWithFunction<T,TMain>(Expression<Func<T, object>> expression, SelectFunction selectFunction, Expression<Func<TMain, object>> aliasProp)
        {
            var fieldName = "*";

            if (expression != null)
            {
                fieldName = GetColumnName(GetMemberExpression(expression.Body));
            }

            var aliasFieldName = "";

            if (aliasProp != null)
            {
                aliasFieldName = GetColumnName(GetMemberExpression<TMain>(aliasProp.Body));
            }

            _builder.Select(GetTableName<T>(), fieldName, selectFunction, aliasFieldName);
        }

        public void SelectWithFunctionSubQuery<T, TMain>(Expression<Func<T, object>> expression, SelectFunction selectFunction, Expression<Func<TMain, object>> aliasProp,string subAlias)
        {
            var fieldName = "*";

            if (expression != null)
            {
                fieldName = GetColumnName(GetMemberExpression(expression.Body));
            }

            var aliasFieldName = "";

            if (aliasProp != null)
            {
                aliasFieldName = GetColumnName(GetMemberExpression<TMain>(aliasProp.Body));
            }

            _builder.Select(subAlias, fieldName, selectFunction, aliasFieldName);
        }

        public void GroupBy<T>(Expression<Func<T, object>> expression)
        {
            var body = expression.Body;
            if (body.NodeType == ExpressionType.New)
            {
                var arguments = (body as NewExpression).Arguments;
                foreach (MemberExpression memberExp in arguments)
                    GroupBy<T>(memberExp);
            }
            else
            {
                GroupBy<T>(GetMemberExpression(body));
            }
        }

        public void GroupBySubQuery<T2>(Expression<Func<T2, object>> expression,string subAlias)
        {
            var body = expression.Body;
            if (body.NodeType == ExpressionType.New)
            {
                var arguments = (body as NewExpression).Arguments;
                foreach (MemberExpression memberExp in arguments)
                    GroupBySubQuery<T2>(memberExp,subAlias);
            }
            else
            {
                GroupBySubQuery<T2>(GetMemberExpression(body),subAlias);
            }
        }
        internal void GroupBy<T>(MemberExpression expression)
        {
            var fieldName = GetColumnName(GetMemberExpression(expression));
            _builder.GroupBy(GetTableName<T>(), fieldName);
        }

        internal void GroupBySubQuery<T2>(MemberExpression expression,string subAlias)
        {
            var fieldName = GetColumnName(GetMemberExpression(expression));
            _builder.GroupBy(subAlias, fieldName);
        }
    }
}
