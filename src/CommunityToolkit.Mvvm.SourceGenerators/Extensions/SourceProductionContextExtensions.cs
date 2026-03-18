// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommunityToolkit.Mvvm.SourceGenerators.Extensions;

/// <summary>
/// Extension methods for the <see cref="SourceProductionContext"/> type.
/// </summary>
internal static class SourceProductionContextExtensions
{
    /// <summary>
    /// Adds a new source file to a target <see cref="SourceProductionContext"/> instance.
    /// </summary>
    /// <param name="context">The input <see cref="SourceProductionContext"/> instance to use.</param>
    /// <param name="name">The name of the source file to add.</param>
    /// <param name="compilationUnit">The <see cref="CompilationUnitSyntax"/> instance representing the syntax tree to add.</param>
    public static void AddSource(this SourceProductionContext context, string name, CompilationUnitSyntax compilationUnit)
    {
        // Add the UTF8 text for the input compilation unit
        context.AddSource(name, compilationUnit.GetText(Encoding.UTF8));
    }
}
