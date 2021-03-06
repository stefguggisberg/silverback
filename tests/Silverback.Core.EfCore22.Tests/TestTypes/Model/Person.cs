﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.ComponentModel.DataAnnotations;

namespace Silverback.Tests.Core.EFCore22.TestTypes.Model
{
    public class Person
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }
    }
}