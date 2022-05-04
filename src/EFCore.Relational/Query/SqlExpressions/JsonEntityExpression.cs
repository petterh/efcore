﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Query.SqlExpressions
{
    /// <summary>
    /// TODO
    /// </summary>
    public class JsonEntityExpression : SqlExpression
    {
        private readonly List<string> _jsonPath;

        private readonly Dictionary<INavigation, RelationalEntityShaperExpression> _ownedNavigationMap = new();

        /// <summary>
        /// TODO
        /// </summary>
        public virtual IReadOnlyDictionary<IProperty, SqlExpression> KeyPropertyExpressionMap { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public virtual ColumnExpression JsonColumn { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public virtual IEntityType EntityType { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public JsonEntityExpression(
            ColumnExpression jsonColumn,
            IEntityType entityType,
            Type type,
            RelationalTypeMapping? typeMapping,
            IReadOnlyDictionary<IProperty, SqlExpression> keyPropertyExpressionMap)
            : this(jsonColumn, entityType, type, typeMapping, keyPropertyExpressionMap, new List<string>())
        {
        }

        /// <summary>
        /// TODO
        /// </summary>
        public JsonEntityExpression(
            ColumnExpression jsonColumn,
            IEntityType entityType,
            Type type,
            RelationalTypeMapping? typeMapping,
            IReadOnlyDictionary<IProperty, SqlExpression> keyPropertyExpressionMap,
            List<string> jsonPath)
            : base(type, typeMapping)
        {
            JsonColumn = jsonColumn;
            EntityType = entityType;
            _jsonPath = jsonPath;
            KeyPropertyExpressionMap = keyPropertyExpressionMap;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public RelationalEntityShaperExpression BindNavigation(INavigation navigation)
        {
            if (_ownedNavigationMap.TryGetValue(navigation, out var result))
            {
                return result;
            }

            var pathSegment = navigation.Name;
            var entityType = navigation.TargetEntityType;

            var newPath = _jsonPath.ToList();
            newPath.Add(pathSegment);

            var newKeyPropertyExpressionMap = new Dictionary<IProperty, SqlExpression>();

            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey == null || primaryKey.Properties.Count != KeyPropertyExpressionMap.Count)
            {
                throw new InvalidOperationException("shouldnt happen");
            }

            // TODO: do this properly this is sooooo hacky rn
            var oldValues = KeyPropertyExpressionMap.Values.ToList();
            for (var i = 0; i < primaryKey.Properties.Count; i++)
            {
                newKeyPropertyExpressionMap[primaryKey.Properties[i]] = oldValues[i];
            }

            var jsonEntityExpression = new JsonEntityExpression(JsonColumn, entityType, Type, TypeMapping, newKeyPropertyExpressionMap, newPath);

            return new RelationalEntityShaperExpression(entityType, jsonEntityExpression, nullable: true);
        }

        /// <summary>
        /// TODO
        /// </summary>
        public void AddNavigationBinding(INavigation navigation, RelationalEntityShaperExpression entityShaperExpression)
        {
            _ownedNavigationMap[navigation] = entityShaperExpression;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public SqlExpression BindProperty(IProperty property)
        {
            if (KeyPropertyExpressionMap.TryGetValue(property, out var keyMapping))
            {
                return keyMapping;
            }

            // maumar - make this nicer
            var jsonPath = string.Join(".", _jsonPath);
            if (!string.IsNullOrEmpty(jsonPath))
            {
                jsonPath = "$." + jsonPath;
            }
            else
            {
                jsonPath = "$";
            }


            jsonPath += "." + property.Name;

            // TODO: make into constant rather than fragment later
            // also this is sql server specific
            // also make it into jsonpath access expression or some such
            return new SqlUnaryExpression(
                ExpressionType.Convert,
                new SqlFunctionExpression(
                    "JSON_VALUE",
                    new SqlExpression[] { JsonColumn, new SqlFragmentExpression("'" + jsonPath + "'") },
                    true,
                    new bool[] { true, false },
                    property.ClrType, null),
                property.ClrType,
                null);
        }

        /// <summary>
        /// TODO
        /// </summary>
        public IReadOnlyList<string> GetPath()
            => _jsonPath.AsReadOnly();

        /// <inheritdoc />
        protected override Expression VisitChildren(ExpressionVisitor visitor)
            => this;

        /// <summary>
        /// TODO
        /// </summary>
        protected override void Print(ExpressionPrinter expressionPrinter)
        {
            expressionPrinter.Append("JsonEntityExpression(entity: " + EntityType.Name + "  Path: " + string.Join(".", _jsonPath) + ")");
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is JsonEntityExpression jsonEntityExpression)
            {
                var result = true;
                result = result && JsonColumn.Equals(jsonEntityExpression.JsonColumn);
                result = result && _jsonPath.Count == jsonEntityExpression._jsonPath.Count;

                if (result)
                {
                    result = result && _jsonPath.Zip(jsonEntityExpression._jsonPath, (l, r) => l == r).All(x => true);
                }

                return result;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc />
        public override int GetHashCode()
            => base.GetHashCode(); // TODO
    }
}
