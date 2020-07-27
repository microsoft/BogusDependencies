// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Build.BogusDependencies.Analyzers.UsedAssemblyReferencesAnalyzer
{
    /// <summary>
    /// Provides the implementation of the analyzer to identify unused references.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UsedAssemblyReferencesAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic id of the analyzer.
        /// </summary>
        public const string DiagnosticId = "RE0001";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "References";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        /// <summary>
        /// The supported diagnosticts.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterCompilationAction(AnalyzeReferences);
        }

        private static void AnalyzeReferences(CompilationAnalysisContext context)
        {
            Compilation compilation = context.Compilation;
            IEnumerable<MetadataReference> references = compilation.References;
            IEnumerable<MetadataReference> usedReferences = compilation.GetUsedAssemblyReferences();

            HashSet<string> unusedRefNames = new HashSet<string>(references.Select(reference => reference.Display), StringComparer.OrdinalIgnoreCase);
            unusedRefNames.ExceptWith(usedReferences.Select(reference => reference.Display));

            context.ReportDiagnostic(Diagnostic.Create(Rule, null, compilation.AssemblyName, string.Join(", ", unusedRefNames)));
        }
    }
}
