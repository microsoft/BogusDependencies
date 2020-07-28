// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

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
        private static readonly string OutputDelimeter = Environment.NewLine + "\t";
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
            if (!compilation.Options.Errors.IsEmpty)
            {
                // ignore processing when errors were found
                return;
            }

            IEnumerable<MetadataReference> references = compilation.References;
            IEnumerable<MetadataReference> usedReferences = compilation.GetUsedAssemblyReferences();

            HashSet<string> unusedRefNames = new HashSet<string>(references.Select(reference => reference.Display), StringComparer.OrdinalIgnoreCase);
            unusedRefNames.ExceptWith(usedReferences.Select(reference => reference.Display));

            if (unusedRefNames.Count > 0)
            {
                string nameOrPath = compilation.Options.ModuleName;
                Location location = Location.None;
                if (compilation.Options.SourceReferenceResolver is SourceFileResolver sfr)
                {
                    nameOrPath = sfr.BaseDirectory;
                    string projFile = Directory.EnumerateFiles(sfr.BaseDirectory, "*.*proj", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (!string.IsNullOrEmpty(projFile))
                    {
                        location = Location.Create(Path.Combine(nameOrPath, projFile), TextSpan.FromBounds(0, 1), new LinePositionSpan());
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(Rule, location, nameOrPath, unusedRefNames.Count, OutputDelimeter + string.Join(OutputDelimeter, unusedRefNames)));
            }
        }
    }
}
