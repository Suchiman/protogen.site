using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace protogen.site.Pages
{
    public class IndexModel : ComponentBase
    {
        [Inject] public Monaco Monaco { get; set; }
        [Inject] public NavigationManager UriHelper { get; set; }
        [Inject] public HttpClient Http { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        private IJSInProcessRuntime JSInProcess => (IJSInProcessRuntime)JSRuntime;

        public string Tooling { get; set; } = "protogen:C#";
        public string LangVer { get; set; } = "";
        public bool OneOfEnum { get; set; }
        public bool ListSet { get; set; }
        public string Names { get; set; }

        public GenerateResult CodeGenResult { get; set; }

        public string LibVersion => _libVersion;
        private static readonly string _libVersion;

        static IndexModel()
        {
            var tmVer = GetVersion(typeof(TypeModel));
            var cgVer = GetVersion(typeof(CodeGenerator));
            _libVersion = tmVer == cgVer ? tmVer : (tmVer + "/" + cgVer);
        }

        private static string GetVersion(Type type)
        {
            var assembly = type.GetTypeInfo().Assembly;
            return assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
                ?? assembly.GetName().Version.ToString();
        }

        public class GenerateResult
        {
            public CodeFile[] Files { get; set; }
            public Error[] ParserExceptions { get; set; }
            public string Exception { get; set; }
        }

        public void Generate()
        {
            CodeGenResult = null;

            string schema = Monaco.GetCode("protocontainer");
            if (string.IsNullOrWhiteSpace(schema))
            {
                return;
            }

            Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            options["langver"] = LangVer;
            if (OneOfEnum)
            {
                options["oneof"] = "enum";
            }
            if (ListSet)
            {
                options["listset"] = "yes";
            }

            NameNormalizer nameNormalizer = null;
            switch (Names)
            {
                case "auto":
                    nameNormalizer = NameNormalizer.Default;
                    break;
                case "original":
                    nameNormalizer = NameNormalizer.Null;
                    break;
            }
            var result = new GenerateResult();
            try
            {
                using (var reader = new StringReader(schema))
                {
                    var set = new FileDescriptorSet
                    {
                        //ImportValidator = path => ValidateImport(path),
                    };
                    //set.AddImportPath(Path.Combine(_host.WebRootPath, "protoc"));
                    set.Add("my.proto", true, reader);

                    set.Process();
                    var errors = set.GetErrors();

                    if (errors.Length > 0)
                    {
                        result.ParserExceptions = errors;
                    }
                    CodeGenerator codegen;
                    switch (Tooling)
                    {
                        case "protogen:VB":
#pragma warning disable 0618
                            codegen = VBCodeGenerator.Default;
#pragma warning restore 0618
                            break;
                        case "protogen:C#":
                        default:
                            codegen = CSharpCodeGenerator.Default;
                            break;
                    }
                    result.Files = codegen.Generate(set, nameNormalizer, options).ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                result.Exception = ex.Message;
            }
            CodeGenResult = result;
            Monaco.SetCode("csharpcontainer", CodeGenResult?.Files?.FirstOrDefault()?.Text ?? "");
            JSInProcess.InvokeVoid("processResults", CodeGenResult);
        }

        private Dictionary<string, string> legalImports = null;

        private readonly static char[] DirSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        //private bool ValidateImport(string path) => ResolveImport(path) != null;
        //private string ResolveImport(string path)
        //{
        //    // only allow the things that we actively find under "protoc" on the web root,
        //    // remembering to normalize our slashes; this means that c:\... or ../../ etc will
        //    // all fail, as they are not in "legalImports"
        //    if (legalImports == null)
        //    {
        //        var tmp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        //        var root = Path.Combine(_host.WebRootPath, "protoc");
        //        foreach (var found in Directory.EnumerateFiles(root, "*.proto", SearchOption.AllDirectories))
        //        {
        //            if (found.StartsWith(root))
        //            {
        //                tmp.Add(found.Substring(root.Length).TrimStart(DirSeparators)
        //                    .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), found);
        //            }
        //        }
        //        legalImports = tmp;
        //    }
        //    return legalImports.TryGetValue(path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
        //        out string actual) ? actual : null;
        //}

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Monaco.InitializeAsync();
                Monaco.CreateEditor("protocontainer", "", "proto3lang");
                Monaco.CreateEditor("csharpcontainer", "", "csharp", true);

                var index = UriHelper.Uri.IndexOf("#g");
                if (index > -1)
                {
                    string gisttId = UriHelper.Uri.Substring(index + 2);
                    var result = await Http.GetJsonAsync<Gist>("https://api.github.com/gists/" + gisttId);
                    Monaco.SetCode("protocontainer", result?.Files?.Values?.FirstOrDefault()?.Content ?? "");
                }
            }
        }

        public class Gist
        {
            public Dictionary<string, GistFile> Files { get; set; }
        }

        public class GistFile
        {
            public string Content { get; set; }
        }
    }
}
