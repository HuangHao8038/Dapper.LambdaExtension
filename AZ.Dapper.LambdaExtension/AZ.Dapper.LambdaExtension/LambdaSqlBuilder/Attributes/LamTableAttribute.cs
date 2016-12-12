﻿using System;

namespace AZ.Dapper.LambdaExtension.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LamTableAttribute : Attribute
    {


        public LamTableAttribute(string name,string schema=null)
        {
            this.Name = name;
            this.Schema = schema;
        }

        public string Name { get; set; }

        public string Schema { get; set; }
    }
}
