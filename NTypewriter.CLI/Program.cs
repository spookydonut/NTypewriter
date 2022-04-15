using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Buildalyzer;
using Buildalyzer.Workspaces;
using CommandLine;
using DocumentationGenerator;
using Microsoft.CodeAnalysis;
using NTypewriter.CodeModel.Roslyn;
using NTypewriter;
using NTypewriter.CodeModel;
using NTypewriter.CodeModel.Functions;


namespace NTypewriter.CLI
{
    class Program
    {
        public class Options
        {
            [Value(0, MetaName="proj")]
            public string ProjFile { get; set; }
            
            [Value(1, MetaName="projname")]
            public string ProjName { get; set; }
            
            [Value(2)]
            public string Template { get; set; }
            [Value(3)]
            public string OutputDir { get; set; }
        }
        
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(RunOptions);
        }
        
        static async Task RunOptions(Options opts)
        {
            // 1) Load project
            AnalyzerManager manager = new AnalyzerManager();
            IProjectAnalyzer analyzer = manager.GetProject(opts.ProjFile);
            AdhocWorkspace workspace = analyzer.GetWorkspace(false);
            
            var project = workspace.CurrentSolution.Projects.First(x => x.Name == opts.ProjName);
            
            // 2) Add xml documentation
            project = AddXmlDocumentation(project, typeof(ICodeModel));
            project = AddXmlDocumentation(project, typeof(ActionFunctions));
            project = AddXmlDocumentation(project, typeof(Scriban.Functions.StringFunctions));
            
            // 3) Compile
            var compilation = await project.GetCompilationAsync();
            
            // 4) Create CodeModel
            var codeModelConfiguration = new CodeModelConfiguration() { OmitSymbolsFromReferencedAssemblies = false };
            var codeModel = new CodeModel.Roslyn.CodeModel(compilation, codeModelConfiguration);

            // 5) Load template
            string template = File.ReadAllText(opts.Template);
            
            // 6) Add custom functions
            var ntypewriterConfig = new Configuration();
            ntypewriterConfig.AddCustomFunctions(typeof(NTEConfig));

            // 7) Render
            var result = await NTypeWriter.Render(template, codeModel, ntypewriterConfig);

            if (!result.HasErrors)
            {
                var renderedItem = result.Items.First();
                var path = Path.Combine(opts.OutputDir, renderedItem.Name);
                File.WriteAllText(path, renderedItem.Content);
            }
            else
            {
                foreach (var msg in result.Messages)
                {
                    Console.WriteLine(msg.Message);
                }
            }     
        }
        
        private static Project AddXmlDocumentation(Project project, Type type)
        {
            var assemblyPath = type.Assembly.Location;
            var assemblyXmlDocPath = Path.ChangeExtension(assemblyPath, "xml");
            var assemblyDocProvider = XmlDocumentationProvider.CreateFromFile(assemblyXmlDocPath);
            project = project.AddMetadataReference(MetadataReference.CreateFromFile(assemblyPath, documentation: assemblyDocProvider));
            return project;
        }
    }
}