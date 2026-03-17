// Copyright (c) devops-gruppe-connie. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Core
{
    /// <summary>
    /// A global integer identified by a key.
    /// </summary>
    public class GlobalInteger
    {
        [Key]
        required public string Key { get; set; }

        required public int Value { get; set; }
    }
}