using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace protogen.site
{
    public class Monaco
    {
        private readonly IJSInProcessRuntime Runtime;

        public Monaco(IJSRuntime runtime)
        {
            Runtime = runtime as IJSInProcessRuntime;
        }

        public ValueTask InitializeAsync() => Runtime.InvokeVoidAsync("monacoInterop.initializeAsync");

        public void CreateEditor(string elementId, string initialCode, string language, bool readOnly = false) => Runtime.InvokeVoid("monacoInterop.createEditor", elementId, initialCode, language, readOnly);

        public string GetCode(string elementId) => Runtime.Invoke<string>("monacoInterop.getCode", elementId);

        public void SetCode(string elementId, string code) => Runtime.InvokeVoid("monacoInterop.setCode", elementId, code);
    }
}